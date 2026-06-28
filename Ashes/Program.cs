using System;
using System.Windows.Forms;

namespace Ashes
{
    internal static class Program
    {
        // [STAThread] is REQUIRED for OLE drag & drop and the common dialogs.
        // Without it, drag & drop silently fails (the cursor shows the no-drop
        // icon), so do not remove it.
        [STAThread]
        static int Main(string[] args)
        {
            // Headless mode: when launched with path arguments (e.g. from an
            // Explorer right-click context menu passing "%1"), skip the GUI and
            // run SDelete in a console instead. Any argument means CLI mode.
            if (args.Length > 0)
            {
                return ConsoleMode.Run(args);
            }

            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
            return 0;
        }
    }
}
