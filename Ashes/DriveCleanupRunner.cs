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
    /// Resolves and runs the bundled DriveCleanup executable (Uwe Sieber's
    /// freeware), which removes non-present drive-related devices and their
    /// registry leftovers (USBSTOR / SCSI disks / volumes / MountedDevices /
    /// MountPoints2, etc.). The exe is expected in a "drivecleanup" subfolder
    /// next to the application executable.
    ///
    /// NOTE: DriveCleanup requires administrator privileges to actually clean
    /// (it impersonates a system token to edit the protected ...\Enum key).
    /// The app manifest already requests requireAdministrator, so no extra
    /// elevation is needed here. Without admin it silently runs in test mode.
    /// </summary>
    public static class DriveCleanupRunner
    {
        private static readonly Lazy<string> _exePath = new Lazy<string>(ResolveExePath);

        public static string ExePath => _exePath.Value;

        /// <summary>
        /// True when the current process architecture is one DriveCleanup ships
        /// a build for. Uwe Sieber only provides x86 (DriveCleanup32.exe) and
        /// x64 (DriveCleanup64.exe) binaries — there is no ARM64 build — so on
        /// ARM the cleanup feature is unsupported and should be blocked before
        /// the window is even opened.
        /// </summary>
        public static bool IsArchitectureSupported =>
            RuntimeInformation.ProcessArchitecture is Architecture.X86 or Architecture.X64;

        /// <summary>
        /// Picks DriveCleanup32.exe (x86) or DriveCleanup64.exe (x64) from the
        /// sibling "drive_cleanup" folder next to the app exe. There is no ARM
        /// build, so callers must gate on <see cref="IsArchitectureSupported"/>
        /// first; on an unsupported arch this returns the x64 path purely so any
        /// error message stays meaningful.
        /// </summary>
        private static string ResolveExePath()
        {
            string baseDir = AppContext.BaseDirectory;
            string toolDir = Path.Combine(baseDir, "drive_cleanup");

            string fileName =
                RuntimeInformation.ProcessArchitecture == Architecture.X86
                    ? "DriveCleanup32.exe"
                    : "DriveCleanup64.exe";

            return Path.Combine(toolDir, fileName);
        }

        public static bool IsAvailable => File.Exists(ExePath);

        /// <summary>
        /// Builds the DriveCleanup argument string.
        /// </summary>
        /// <param name="testMode">-t : list what would be removed, change nothing.</param>
        /// <param name="usbMassStorage">-u : only USB mass storage devices.</param>
        /// <param name="disks">-d : only disk devices.</param>
        /// <param name="volumes">-v : only storage volume devices.</param>
        /// <param name="cdrom">-c : only CDROM devices.</param>
        /// <param name="floppy">-f : only floppy devices.</param>
        /// <param name="wpd">-w : only USB drive's WPD devices.</param>
        /// <remarks>
        /// If no type flag is set, DriveCleanup cleans ALL supported types.
        /// -n suppresses the "press a key" wait so the process exits cleanly
        /// when we're capturing its output.
        /// </remarks>
        public static string BuildArgs(
            bool testMode,
            bool usbMassStorage = false,
            bool disks = false,
            bool volumes = false,
            bool cdrom = false,
            bool floppy = false,
            bool wpd = false)
        {
            var parts = new System.Collections.Generic.List<string> { "-n" };

            if (testMode) parts.Add("-t");
            if (usbMassStorage) parts.Add("-u");
            if (disks) parts.Add("-d");
            if (volumes) parts.Add("-v");
            if (cdrom) parts.Add("-c");
            if (floppy) parts.Add("-f");
            if (wpd) parts.Add("-w");

            return string.Join(" ", parts);
        }

        /// <summary>
        /// Runs DriveCleanup with the given arguments, streaming output back
        /// line by line via <paramref name="onOutput"/>. Returns the exit code,
        /// which is the number of removed (or, in test mode, removable) devices.
        /// Cancellation kills the process.
        ///
        /// Unlike SDelete, DriveCleanup writes plain console text (no \b progress
        /// counter and no UTF-16), so we read it line-buffered with the console's
        /// default output encoding.
        /// </summary>
        public static async Task<int> RunAsync(
            string arguments,
            Action<string> onOutput,
            CancellationToken cancellationToken = default)
        {
            if (!IsAvailable)
                throw new FileNotFoundException(
                    "DriveCleanup 실행 파일을 찾을 수 없습니다: " + ExePath);

            var psi = new ProcessStartInfo
            {
                FileName = ExePath,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = AppContext.BaseDirectory
            };

            // DriveCleanup emits OEM/ANSI console text. On Korean Windows the
            // OEM code page is 949; using the OS OEM code page keeps any
            // non-ASCII intact. Fall back silently if the code page is missing.
            try
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                var oem = Encoding.GetEncoding(GetOemCodePage());
                psi.StandardOutputEncoding = oem;
                psi.StandardErrorEncoding = oem;
            }
            catch
            {
                // Leave defaults; ASCII output (the common case) is unaffected.
            }

            using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };

            process.OutputDataReceived += (s, e) =>
            {
                if (e.Data != null) onOutput?.Invoke(e.Data);
            };
            process.ErrorDataReceived += (s, e) =>
            {
                if (e.Data != null) onOutput?.Invoke(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            using (cancellationToken.Register(() =>
            {
                try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
                catch { /* already gone */ }
            }))
            {
                await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            }

            return process.ExitCode;
        }

        [DllImport("kernel32.dll")]
        private static extern uint GetOEMCP();

        private static int GetOemCodePage()
        {
            try { return (int)GetOEMCP(); }
            catch { return 437; } // US OEM as a safe default
        }
    }
}
