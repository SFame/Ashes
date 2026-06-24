using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Ashes
{
    /// <summary>
    /// Flashes a window's taskbar button (the orange highlight) to signal that
    /// a background task has finished, when the window isn't in the foreground.
    /// </summary>
    internal static class TaskbarFlasher
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct FLASHWINFO
        {
            public uint cbSize;
            public IntPtr hwnd;
            public uint dwFlags;
            public uint uCount;
            public uint dwTimeout;
        }

        private const uint FLASHW_STOP = 0;
        private const uint FLASHW_CAPTION = 0x00000001;
        private const uint FLASHW_TRAY = 0x00000002;
        private const uint FLASHW_ALL = FLASHW_CAPTION | FLASHW_TRAY;
        private const uint FLASHW_TIMERNOFG = 0x0000000C; // flash until window comes to front

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

        /// <summary>
        /// Flashes the taskbar button until the user brings the window forward.
        /// Does nothing if the window is already active (no need to alert).
        /// </summary>
        public static void Flash(Form form)
        {
            if (form == null || form.IsDisposed) return;
            if (form.WindowState != FormWindowState.Minimized && Form.ActiveForm == form)
                return; // already in front — don't nag

            var info = new FLASHWINFO
            {
                cbSize = (uint)Marshal.SizeOf<FLASHWINFO>(),
                hwnd = form.Handle,
                dwFlags = FLASHW_ALL | FLASHW_TIMERNOFG,
                uCount = uint.MaxValue,
                dwTimeout = 0
            };
            FlashWindowEx(ref info);
        }

        /// <summary>Stops any ongoing flashing.</summary>
        public static void Stop(Form form)
        {
            if (form == null || form.IsDisposed) return;
            var info = new FLASHWINFO
            {
                cbSize = (uint)Marshal.SizeOf<FLASHWINFO>(),
                hwnd = form.Handle,
                dwFlags = FLASHW_STOP,
                uCount = 0,
                dwTimeout = 0
            };
            FlashWindowEx(ref info);
        }
    }
}