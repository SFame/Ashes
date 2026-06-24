using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ashes
{
    /// <summary>
    /// Resolves the correct bundled SDelete executable for the current
    /// architecture and runs it, streaming stdout/stderr back live.
    /// </summary>
    public static class SDeleteRunner
    {
        // Lazily resolved once, on first use.
        private static readonly Lazy<string> _exePath = new Lazy<string>(ResolveExePath);

        public static string ExePath => _exePath.Value;

        /// <summary>
        /// Picks sdelete.exe (x86), sdelete64.exe (x64) or sdelete64a.exe (ARM64)
        /// based on the running process architecture. The files are expected to sit
        /// in a "sdelete" subfolder next to the application executable.
        /// </summary>
        private static string ResolveExePath()
        {
            string baseDir = AppContext.BaseDirectory;
            string toolDir = Path.Combine(baseDir, "sdelete");

            string fileName;
            switch (RuntimeInformation.ProcessArchitecture)
            {
                case Architecture.X86:
                    fileName = "sdelete.exe";
                    break;
                case Architecture.Arm64:
                    fileName = "sdelete64a.exe";
                    break;
                case Architecture.X64:
                default:
                    fileName = "sdelete64.exe";
                    break;
            }

            string full = Path.Combine(toolDir, fileName);

            // Fall back to x64 build if the arch-specific one is missing.
            if (!File.Exists(full))
            {
                string fallback = Path.Combine(toolDir, "sdelete64.exe");
                if (File.Exists(fallback))
                    return fallback;
            }

            return full;
        }

        public static bool IsAvailable => File.Exists(ExePath);

        /// <summary>
        /// Runs SDelete with the given arguments. Output is delivered as
        /// "segments": whenever SDelete ends a chunk with newline (\n) or a
        /// carriage return (\r), that chunk is sent to <paramref name="onOutput"/>.
        /// The bool is true when the segment was terminated by \r alone, meaning
        /// SDelete intends to overwrite the same line (a progress update) — the
        /// UI should replace the last line rather than append a new one.
        /// Returns the process exit code. Cancellation kills the process.
        /// </summary>
        public static async Task<int> RunAsync(
            string arguments,
            Action<string, bool> onOutput,
            CancellationToken cancellationToken = default)
        {
            if (!IsAvailable)
                throw new FileNotFoundException(
                    "SDelete 실행 파일을 찾을 수 없습니다: " + ExePath);

            var psi = new ProcessStartInfo
            {
                FileName = ExePath,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                // SDelete writes its output as UTF-16 (Unicode), not UTF-8.
                // Reading it as UTF-8 inserts a null (<0>) after every ASCII
                // character and breaks line detection, so use Unicode here.
                StandardOutputEncoding = Encoding.Unicode,
                StandardErrorEncoding = Encoding.Unicode,
                WorkingDirectory = AppContext.BaseDirectory
            };

            using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };

            process.Start();

            // Read stdout and stderr char-by-char so we can catch SDelete's
            // \r-based in-place progress updates, which BeginOutputReadLine
            // (line-buffered on \n) would swallow until the whole run finished.
            var stdoutTask = PumpStreamAsync(process.StandardOutput, onOutput, cancellationToken);
            var stderrTask = PumpStreamAsync(process.StandardError, onOutput, cancellationToken);

            using (cancellationToken.Register(() =>
            {
                try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
                catch { /* already gone */ }
            }))
            {
                await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            }

            // Let the pumps drain whatever is left in the buffers.
            try { await Task.WhenAll(stdoutTask, stderrTask).ConfigureAwait(false); }
            catch (OperationCanceledException) { /* expected on cancel */ }

            return process.ExitCode;
        }

        /// <summary>
        /// Reads a stream one character at a time, emitting a segment each time
        /// a \n or \r terminator is seen. \r-terminated segments are flagged as
        /// progress updates (overwrite last line); \n-terminated as new lines.
        /// </summary>
        private static async Task PumpStreamAsync(
            System.IO.StreamReader reader,
            Action<string, bool> onOutput,
            CancellationToken token)
        {
            var sb = new StringBuilder();
            var buffer = new char[1];

            // ---- DIAGNOSTIC MODE ----
            // Temporarily dump raw control chars so we can see exactly what
            // SDelete emits. Set to false once we've diagnosed the flooding.
            const bool RAW_DIAGNOSTIC = false;
            if (RAW_DIAGNOSTIC)
            {
                var diag = new StringBuilder();
                while (!token.IsCancellationRequested)
                {
                    int r = await reader.ReadAsync(buffer, 0, 1).ConfigureAwait(false);
                    if (r == 0) break;
                    char ch = buffer[0];
                    if (ch == '\r') diag.Append("<CR>");
                    else if (ch == '\n') diag.Append("<LF>\n"); // real newline so log stays readable
                    else if (ch == '\b') diag.Append("<BS>");
                    else if (ch < 32) diag.Append($"<{(int)ch}>");
                    else diag.Append(ch);

                    // Flush periodically so we see output as it streams.
                    if (diag.Length > 80)
                    {
                        onOutput?.Invoke(diag.ToString(), false);
                        diag.Clear();
                    }
                }
                if (diag.Length > 0) onOutput?.Invoke(diag.ToString(), false);
                return;
            }
            // ---- END DIAGNOSTIC MODE ----

            // SDelete updates its progress counter in place using BACKSPACE
            // characters (\b) to erase the previous number, then writes the new
            // one — it does NOT use \r for progress. Completed lines end in \r\n.
            // So: \b edits the current line buffer in place; on \n we finalise
            // the line; any \b activity means "overwrite the displayed line".
            bool lineDirtyByBackspace = false;

            while (!token.IsCancellationRequested)
            {
                int read = await reader.ReadAsync(buffer, 0, 1).ConfigureAwait(false);
                if (read == 0) break; // end of stream

                char c = buffer[0];

                if (c == '\b')
                {
                    // Erase last char of the current line buffer.
                    if (sb.Length > 0) sb.Length--;
                    lineDirtyByBackspace = true;
                }
                else if (c == '\n')
                {
                    onOutput?.Invoke(sb.ToString(), false); // completed line
                    sb.Clear();
                    lineDirtyByBackspace = false;
                }
                else if (c == '\r')
                {
                    // Carriage return: treat as in-place rewrite of current line.
                    onOutput?.Invoke(sb.ToString(), true);
                    sb.Clear();
                    lineDirtyByBackspace = false;
                }
                else
                {
                    sb.Append(c);
                    // If we just rewrote after backspaces, push an in-place
                    // update so the UI refreshes the progress on one line.
                    if (lineDirtyByBackspace)
                    {
                        onOutput?.Invoke(sb.ToString(), true);
                    }
                }
            }

            // Flush any trailing text without a terminator.
            if (sb.Length > 0)
                onOutput?.Invoke(sb.ToString(), lineDirtyByBackspace);
        }

        /// <summary>
        /// Quotes a path for the command line (handles spaces).
        /// </summary>
        public static string Quote(string path)
        {
            if (string.IsNullOrEmpty(path)) return "\"\"";
            return path.Contains(' ') || path.Contains('\t')
                ? "\"" + path + "\""
                : path;
        }
    }
}