using System;
using System.Runtime.InteropServices;

namespace FFLiteGUI.Utils
{
    public static class Win32Native
    {
        [DllImport("shcore.dll", SetLastError = true)]
        public static extern int SetProcessDpiAwareness(int value);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetProcessDPIAware();

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetShortPathName(string lpszLongPath, System.Text.StringBuilder lpszShortPath, int cchBuffer);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool FreeConsole();

        public const int PROCESS_SYSTEM_DPI_AWARE = 1;
        public const int PROCESS_PER_MONITOR_DPI_AWARE = 2;
        public const int SW_HIDE = 0;
        public const int SW_SHOW = 5;
        public const int STD_OUTPUT_HANDLE = -11;

        public static void EnableHighDpiAwareness()
        {
            try
            {
                if (SetProcessDpiAwareness(PROCESS_PER_MONITOR_DPI_AWARE) != 0)
                {
                    SetProcessDPIAware();
                }
            }
            catch (Exception)
            {
                try
                {
                    SetProcessDPIAware();
                }
                catch
                {
                    // 忽略
                }
            }
        }

        public static void HideConsoleWindow()
        {
            IntPtr handle = GetConsoleWindow();
            if (handle != IntPtr.Zero)
                ShowWindow(handle, SW_HIDE);
        }

        public static void ShowConsoleWindow()
        {
            IntPtr handle = GetConsoleWindow();
            if (handle != IntPtr.Zero)
                ShowWindow(handle, SW_SHOW);
        }
    }
}
