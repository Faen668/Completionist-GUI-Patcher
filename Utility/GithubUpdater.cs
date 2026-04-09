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
        private static bool _isUpdateAvailable = false;

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

        private static async Task ExtractAndUpdate(string zipPath)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string updateNewDir = Path.Combine(baseDir, "update_new");
            string manifestPath = Path.Combine(baseDir, "preupdate_manifest.txt");

            // Clean previous update attempt
            if (Directory.Exists(updateNewDir))
                Directory.Delete(updateNewDir, true);
            if (File.Exists(manifestPath))
                File.Delete(manifestPath);

            // Extract new version to update_new
            ZipFile.ExtractToDirectory(zipPath, updateNewDir);

            // Create manifest of current files (pre-update)
            await CreatePreUpdateManifestAsync(baseDir, "ffdec", "update_new");

            // Create update.bat script (placed in base directory)
            string batchScript = Path.Combine(baseDir, "update.bat");
            string batchContent = $@"@echo off
taskkill /IM Completionist-GUI-Patcher.exe /F
xcopy /Y /E ""{updateNewDir}"" "".""
start """" ""Completionist GUI Patcher.exe""
exit";
            await File.WriteAllTextAsync(batchScript, batchContent);

            // Launch updater and shut down current instance
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/C \"{batchScript}\"",
                WorkingDirectory = baseDir,
                WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
            });

            Application.Current.Shutdown();
        }

        private static async Task CreatePreUpdateManifestAsync(string baseDir, params string[] excludeSubdirs)
        {
            string manifestPath = Path.Combine(baseDir, "preupdate_manifest.txt");

            var excludedPaths = new HashSet<string>(excludeSubdirs.Select(d =>
                Path.GetFullPath(Path.Combine(baseDir, d)).TrimEnd(Path.DirectorySeparatorChar)),
                StringComparer.OrdinalIgnoreCase);

            var allFiles = Directory.GetFiles(baseDir, "*", SearchOption.AllDirectories)
                .Where(f =>
                {
                    string fullDir = Path.GetDirectoryName(f) ?? string.Empty;
                    // Exclude if file lives inside any excluded subdirectory
                    if (excludedPaths.Any(ex => fullDir.StartsWith(ex, StringComparison.OrdinalIgnoreCase)))
                        return false;
                    // Also exclude the manifest itself and temporary update files
                    return !f.Equals(manifestPath, StringComparison.OrdinalIgnoreCase) &&
                           !f.EndsWith("update.zip", StringComparison.OrdinalIgnoreCase) &&
                           !f.EndsWith("update.bat", StringComparison.OrdinalIgnoreCase);
                })
                .Select(f => Path.GetRelativePath(baseDir, f))
                .ToList();

            await File.WriteAllLinesAsync(manifestPath, allFiles);
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
