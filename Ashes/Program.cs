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
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }
}
