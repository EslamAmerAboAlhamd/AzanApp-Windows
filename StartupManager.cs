using System;
using Microsoft.Win32;

namespace AzanApp
{
    public static class StartupManager
    {
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "AzanApp";

        public static void Set(bool enable)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
                if (key == null) return;

                if (enable)
                {
                    string? exe = Environment.ProcessPath;
                    if (string.IsNullOrEmpty(exe)) return;
                    key.SetValue(AppName, $"\"{exe}\" --minimized");
                }
                else
                {
                    key.DeleteValue(AppName, throwOnMissingValue: false);
                }
            }
            catch { }
        }

        public static bool IsEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKey);
                return key?.GetValue(AppName) != null;
            }
            catch { return false; }
        }
    }
}