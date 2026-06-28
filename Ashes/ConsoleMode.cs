using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace Ashes
{
    /// <summary>
    /// Headless entry path used when Ashes is launched with command-line
    /// arguments (e.g. from an Explorer right-click context menu that passes
    /// "%1"). Instead of opening the WinForms UI, it attaches to / allocates a
    /// console, runs SDelete on the given paths, streams its output, and exits.
    ///
    /// Because the project is built as WinExe (OutputType=WinExe), the process
    /// has NO console of its own. We must AttachConsole to the parent (when
    /// launched from a terminal) or AllocConsole (when launched from Explorer)
    /// before any Console I/O will appear anywhere.
    /// </summary>
    internal static class ConsoleMode
    {
        // ---- fixed options for context-menu / CLI runs ----
        private const int Passes = 3;      // -p 3
        private const bool Recurse = true; // -s (always on for directories)

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeConsole();

        private const int ATTACH_PARENT_PROCESS = -1;

        /// <summary>
        /// Runs the headless console flow. Returns a process exit code.
        /// </summary>
        public static int Run(string[] args)
        {
            // Reuse the parent console if we were started from one (cmd, pwsh,
            // Windows Terminal); otherwise spin up our own window. From an
            // Explorer context menu there's no parent console, so AllocConsole
            // gives us a fresh window that we own for the duration.
            bool ownConsole = false;
            if (!AttachConsole(ATTACH_PARENT_PROCESS))
            {
                ownConsole = AllocConsole();
            }

            try
            {
                return RunCore(args);
            }
            finally
            {
                // Only release the console if we created it. (If we attached to
                // a parent terminal, leave it alone.)
                if (ownConsole)
                    FreeConsole();
            }
        }

        private static int RunCore(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.Title = "Ashes — 안전 삭제";

            // Keep only paths that actually exist; warn about the rest.
            var targets = new List<string>();
            foreach (var raw in args)
            {
                string path = (raw ?? string.Empty).Trim().Trim('"');
                if (string.IsNullOrWhiteSpace(path)) continue;

                if (File.Exists(path) || Directory.Exists(path))
                    targets.Add(path);
                else
                    Console.WriteLine("[건너뜀] 경로를 찾을 수 없습니다: " + path);
            }

            if (targets.Count == 0)
            {
                Console.WriteLine("[오류] 삭제할 유효한 경로가 없습니다.");
                return 2;
            }

            if (!SDeleteRunner.IsAvailable)
            {
                Console.WriteLine("[오류] SDelete 실행 파일을 찾을 수 없습니다:");
                Console.WriteLine("       " + SDeleteRunner.ExePath);
                return 3;
            }

            Console.WriteLine("Ashes — 다음 항목을 복구 불가능하게 삭제합니다 ("
                              + Passes + "회 덮어쓰기):");
            foreach (var t in targets)
                Console.WriteLine("   • " + t);
            Console.WriteLine();

            string sdeleteArgs = BuildArgs(targets);
            Console.WriteLine("> sdelete " + sdeleteArgs);
            Console.WriteLine();

            int code;
            try
            {
                // Run synchronously to completion; stream output straight to the
                // console. \r progress updates are written without a newline so
                // SDelete's in-place counter behaves the same as in a terminal.
                code = SDeleteRunner.RunAsync(sdeleteArgs, OnOutput, CancellationToken.None)
                                    .GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("[오류] " + ex.Message);
                return 1;
            }

            Console.WriteLine();
            Console.WriteLine("[완료] 종료 코드 " + code);
            // "바로 닫기" — no key wait, no delay.
            return code;
        }

        private static void OnOutput(string text, bool isProgress)
        {
            if (string.IsNullOrEmpty(text)) return;

            if (isProgress)
            {
                // In-place progress: rewind to start of line, write, no newline.
                Console.Write('\r');
                Console.Write(text);
            }
            else
            {
                Console.WriteLine(text);
            }
        }

        private static string BuildArgs(List<string> targets)
        {
            var parts = new List<string> { "-accepteula", "-nobanner", "-p", Passes.ToString() };
            if (Recurse) parts.Add("-s");
            foreach (var t in targets)
                parts.Add(SDeleteRunner.Quote(t));
            return string.Join(" ", parts);
        }
    }
}
