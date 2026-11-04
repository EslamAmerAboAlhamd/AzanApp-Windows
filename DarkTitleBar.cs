using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace AzanApp
{
    public static class DarkTitleBar
    {
        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(
            IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_OLD = 19;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_CAPTION_COLOR = 35;
        private const int DWMWA_TEXT_COLOR = 36;

        public static void Apply(Window window)
        {
            window.SourceInitialized += (s, e) => ApplyNow(window);
            if (new WindowInteropHelper(window).Handle != IntPtr.Zero)
                ApplyNow(window);
        }

        private static void ApplyNow(Window window)
        {
            try
            {
                IntPtr hwnd = new WindowInteropHelper(window).Handle;
                if (hwnd == IntPtr.Zero) return;

                int on = 1;
                if (DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref on, sizeof(int)) != 0)
                    DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_OLD, ref on, sizeof(int));

                int black = 0x000000;
                int white = 0xFFFFFF;
                DwmSetWindowAttribute(hwnd, DWMWA_CAPTION_COLOR, ref black, sizeof(int));
                DwmSetWindowAttribute(hwnd, DWMWA_TEXT_COLOR, ref white, sizeof(int));
            }
            catch { }
        }
    }
}