using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace FFLiteGUI.Utils
{
    public static class PathHelper
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetShortPathName(string lpszLongPath, StringBuilder lpszShortPath, int cchBuffer);

        public static string Normalize(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return path;
            return path.Replace('\\', '/');
        }

        public static string GetShortPath(string longPath)
        {
            if (string.IsNullOrEmpty(longPath))
                return longPath;

            if (!File.Exists(longPath) && !Directory.Exists(longPath))
                return longPath;

            int bufferSize = 256;
            StringBuilder buffer = new StringBuilder(bufferSize);
            int result = GetShortPathName(longPath, buffer, bufferSize);
            if (result == 0)
                return longPath;
            if (result > bufferSize)
            {
                buffer = new StringBuilder(result);
                result = GetShortPathName(longPath, buffer, buffer.Length);
                if (result == 0)
                    return longPath;
            }
            return buffer.ToString();
        }

        public static string EscapeForFilter(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;
            string escaped = path.Replace("\\", "/");
            escaped = escaped.Replace(":", "\\:");
            escaped = escaped.Replace("'", "\\'");
            return escaped;
        }

        public static string QuoteForCommand(string path)
        {
            if (string.IsNullOrEmpty(path))
                return "\"\"";
            string normalized = Normalize(path);
            if (normalized.Contains(" ") || normalized.Contains("&") || normalized.Contains("(") || normalized.Contains(")"))
                return $"\"{normalized}\"";
            return normalized;
        }

        public static string Combine(params string[] parts)
        {
            string combined = Path.Combine(parts);
            return Normalize(combined);
        }

        public static string GetExecutablePath(string exeName)
        {
            // 当前程序目录
            string local = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, exeName);
            if (File.Exists(local))
                return local;

            // PATH 环境变量
            string pathEnv = Environment.GetEnvironmentVariable("PATH");
            if (!string.IsNullOrEmpty(pathEnv))
            {
                foreach (string dir in pathEnv.Split(Path.PathSeparator))
                {
                    string full = Path.Combine(dir, exeName);
                    if (File.Exists(full))
                        return full;
                }
            }
            return null;
        }
    }
}
