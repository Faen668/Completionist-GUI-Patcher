using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace Completionist_GUI_Patcher.Utility
{
    public class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; } = string.Empty;

        [JsonPropertyName("prerelease")]
        public bool Prerelease { get; set; }

        [JsonPropertyName("published_at")]
        public DateTime PublishedAt { get; set; }

        [JsonPropertyName("assets")]
        public List<GitHubAsset> Assets { get; set; } = [];
    }

    public class GitHubAsset
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("browser_download_url")]
        public string BrowserDownloadUrl { get; set; } = string.Empty;
    }

    public static class Utility
    {
        public static (string modifiedContent, bool success) InsertBaseLinesAfterVariable(string content, string targetLine, string newLines, Patcher instance)
        {
            int targetLinePos = content.IndexOf(targetLine);
            if (targetLinePos != -1)
            {
                content = content.Insert(targetLinePos + targetLine.Length, "\n" + newLines);
                return (content, true);
            }
            else
            {
                instance.UpdateLog($"Unable to find line: {targetLine}");
                return (content, false);
            }
        }

        private static readonly string[] separator = ["\r\n", "\n"];
        public static (string modifiedContent, bool success) InsertBaseLinesAfter(string content, string targetLine, string newLines, Patcher instance)
        {
            string[] lines = content.Split(separator, StringSplitOptions.None);

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains(targetLine))
                {
                    List<string> modifiedLines = lines.ToList();
                    modifiedLines.Insert(i + 1, newLines);

                    return (string.Join("\n", modifiedLines), true);
                }
            }

            instance.UpdateLog($"Unable to find line: {targetLine}");
            return (content, false);
        }

        public static bool WriteStringToFile(string filePath, string content, Patcher instance)
        {
            try
            {
                File.WriteAllText(filePath, content);
                return true;
            }
            catch (Exception ex)
            {
                instance.UpdateLog($"Error writing to file: {ex.Message}");
                return false;
            }
        }

        public static IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_GETMINMAXINFO = 0x0024;

            if (msg == WM_GETMINMAXINFO)
            {
                WmGetMinMaxInfo(hwnd, lParam);
                handled = true;
            }

            return IntPtr.Zero;
        }

        private static void WmGetMinMaxInfo(IntPtr hwnd, IntPtr lParam)
        {
            const int MONITOR_DEFAULTTONEAREST = 0x00000002;

            IntPtr monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);

            if (monitor != IntPtr.Zero)
            {
                MONITORINFO monitorInfo = new MONITORINFO();
                monitorInfo.cbSize = Marshal.SizeOf(typeof(MONITORINFO));
                GetMonitorInfo(monitor, monitorInfo);

                RECT workArea = monitorInfo.rcWork;
                RECT monitorArea = monitorInfo.rcMonitor;

                MINMAXINFO mmi = Marshal.PtrToStructure<MINMAXINFO>(lParam);

                mmi.ptMaxPosition.X = workArea.Left - monitorArea.Left;
                mmi.ptMaxPosition.Y = workArea.Top - monitorArea.Top;
                mmi.ptMaxSize.X = workArea.Right - workArea.Left;
                mmi.ptMaxSize.Y = workArea.Bottom - workArea.Top;

                Marshal.StructureToPtr(mmi, lParam, true);
            }
        }

        // Required structs and imports for working with monitors
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class MONITORINFO
        {
            public int cbSize = Marshal.SizeOf(typeof(MONITORINFO));
            public RECT rcMonitor;
            public RECT rcWork;
            public int dwFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, MONITORINFO lpmi);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, int dwFlags);
    }
}
