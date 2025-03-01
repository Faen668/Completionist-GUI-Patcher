using Completionist_GUI_Patcher.Messages.ConfirmationMessage;
using Newtonsoft.Json.Linq;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Windows;

namespace Completionist_GUI_Patcher.Utility
{
    public static class GitHubUpdater
    {
        private static readonly HttpClient _httpClient = new();
        private static string _downloadPath = string.Empty;
        private static string _latestReleaseURL = string.Empty;
        private static string _latestRelease = string.Empty;
        public static bool _isUpdateAvailable = false;

        public static bool CanUpdate()
        {
            return _isUpdateAvailable;
        }

        public static async void DoUpdate()
        {
            bool success = await DownloadLatestReleaseAsync(_latestReleaseURL, _downloadPath);

            if (success)
            {
                ExtractAndUpdate(_downloadPath);
            }
        }

        public static async Task<bool> CheckForUpdates()
        {
            var (latestReleaseUrl, latestVersion) = await GetLatestReleaseInfoAsync();

            if (!string.IsNullOrEmpty(latestReleaseUrl) && !string.IsNullOrEmpty(latestVersion))
            {
                string currentVersion = GetCurrentVersion();

                if (IsNewerVersion(latestVersion, currentVersion))
                {
                    _downloadPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "update.zip");
                    _latestReleaseURL = latestReleaseUrl;
                    _latestRelease = latestVersion;
                    _isUpdateAvailable = true;
                    return true;
                }
            }
            return false;
        }

        public static string GetCurrentVersion()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0.0";
        }

        public static string GetLatestVersion()
        {
            return _latestRelease;
        }

        private static bool IsNewerVersion(string latestVersion, string currentVersion)
        {
            try
            {
                Version latest = new(latestVersion.TrimStart('v')); // GitHub tags sometimes have "v" prefix
                Version current = new(currentVersion);
                return latest > current;
            }
            catch
            {
                return false; // If parsing fails, assume no update
            }
        }

        public static async Task<(string downloadUrl, string latestVersion)> GetLatestReleaseInfoAsync()
        {
            string repoOwner = "Faen668";
            string repoName = "Completionist-GUI-Patcher";
            string url = $"https://api.github.com/repos/{repoOwner}/{repoName}/releases/latest";

            _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Mozilla", "5.0")); // Required by GitHub API

            try
            {
                var response = await _httpClient.GetStringAsync(url);
                JObject releaseInfo = JObject.Parse(response);

                string? downloadUrl = releaseInfo["assets"]?[0]?["browser_download_url"]?.ToString();
                string? latestVersion = releaseInfo["tag_name"]?.ToString(); // GitHub release tags are often used as version numbers

                return (downloadUrl ?? string.Empty, latestVersion ?? string.Empty);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching update: {ex.Message}");
                return (string.Empty, string.Empty);
            }
        }

        public static async Task<bool> DownloadLatestReleaseAsync(string url, string downloadPath)
        {
            try
            {
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                using var fs = new FileStream(downloadPath, FileMode.Create, FileAccess.Write, FileShare.None);
                await response.Content.CopyToAsync(fs);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Download failed: {ex.Message}");
                return false;
            }
        }

        private static void ExtractAndUpdate(string zipPath)
        {
            string extractPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "update");

            // Ensure no previous update is interfering
            if (Directory.Exists(extractPath))
            {
                Directory.Delete(extractPath, true);
            }

            ZipFile.ExtractToDirectory(zipPath, extractPath);

            // Run an update script (or directly move files)
            RunUpdateScript(extractPath);

            // Clean up
            File.Delete(zipPath);
        }

        private static void RunUpdateScript(string extractedPath)
        {
            string batchScript = Path.Combine(extractedPath, "update.bat");

            File.WriteAllText(batchScript, @"
@echo off
taskkill /IM Completionist-GUI-Patcher.exe /F
xcopy /Y /E ""update"" "".""
timeout /t 2  // Adding delay here
start """" ""Completionist GUI Patcher.exe""
rd /S /Q ""update""
exit");

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/C \"{batchScript}\"",
                WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
                WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
            });

            Application.Current.Shutdown();
        }
    }
}
