using Completionist_GUI_Patcher.Messages.ConfirmationMessage;
using Completionist_GUI_Patcher.Utility;
using Microsoft.Win32;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using GUpd = Completionist_GUI_Patcher.Utility.GitHubUpdater;
using Util = Completionist_GUI_Patcher.Utility.Utility;
using MSGReturn = Completionist_GUI_Patcher.Messages.ConfirmationMessage.Confirmation_Message.Confirmation_Message_Return_Value;

namespace Completionist_GUI_Patcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class Patcher : Window
    {
        private bool mRestoreForDragMove = false;
        private bool _isPinned = false;
        private bool _inputAllowed = false;
        private bool _isLightTheme = false;

        private readonly List<string> _logLines = [];

        private readonly Dictionary<string, string> validFileMappings = new()
        {
            { "InventoryLists", "inventorylists.swf" },
            { "CraftingMenu", "craftingmenu.swf" },
            { "CocksMenu", "constructibleobjectmenu.swf" }
        };

        //---------------------------------------------------
        //---------------------------------------------------
        //---------------------------------------------------

        public Patcher()
        {
            InitializeComponent();
            this.SourceInitialized += Window_SourceInitialized;
            VersionLabel.Text = $"Patcher Version {GUpd.GetCurrentVersion()}";
            this.Loaded += Patcher_Loaded;
        }

        //---------------------------------------------------
        //---------------------------------------------------
        //---------------------------------------------------

        private async void Patcher_Loaded(object sender, RoutedEventArgs e)
        {
            Start.IsEnabled = false;
            SetTheme(false);
            _inputAllowed = false;

            //do app updates
            await GUpd.CheckForUpdates();
            if (GUpd.CanUpdate())
            {
                var cm = new Confirmation_Message(
                    "Patcher Update Found...",
                    $"A new version ({GUpd.GetLatestVersion()}) is available. You are running {GUpd.GetCurrentVersion()}.\nDo you want to update now?",
                    _isLightTheme,
                    null,
                    null,
                    "Update Now",
                    "Cancel",
                    5);

                cm.ShowDialog();
                if (cm.GetUserInputValue() == MSGReturn.kAccept)
                {
                    GUpd.DoUpdate();
                }
            }

            _inputAllowed = await ValidateFFDec();
        }

        private void ThemeToggle_Click(object sender, RoutedEventArgs e)
        {
            _isLightTheme = !_isLightTheme;
            SetTheme(_isLightTheme);
        }

        private void SetTheme(bool isLight)
        {
            if (isLight)
            {
                // Modern light theme (soft, layered, minimal accent)

                this.Resources["MainBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0xF7, 0xF7, 0xFA)); // softer than pure white
                this.Resources["SecondaryBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0xEF, 0xEF, 0xF4)); // subtle separation
                this.Resources["CardBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF)); // cards still white

                this.Resources["MainForegroundBrush"] = new SolidColorBrush(Color.FromRgb(0x1C, 0x1C, 0x1E)); // near-black (easier on eyes)
                this.Resources["SecondaryForegroundBrush"] = new SolidColorBrush(Color.FromRgb(0x6B, 0x6B, 0x73)); // muted text

                // 👇 toned-down orange (used sparingly)
                this.Resources["AccentBrush"] = new SolidColorBrush(Color.FromRgb(0xD9, 0x6A, 0x1F));
                this.Resources["AccentForegroundBrush"] = new SolidColorBrush(Color.FromRgb(0xD9, 0x6A, 0x1F));

                this.Resources["BorderBrush"] = new SolidColorBrush(Color.FromRgb(0xD8, 0xD8, 0xDE)); // softer border

                this.Resources["TextBoxBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF));
                this.Resources["TextBoxForegroundBrush"] = new SolidColorBrush(Color.FromRgb(0x1C, 0x1C, 0x1E));
                this.Resources["TextBoxBorderBrush"] = new SolidColorBrush(Color.FromRgb(0xD0, 0xD0, 0xD8));
                this.Resources["TextBoxFocusBorderBrush"] = new SolidColorBrush(Color.FromRgb(0xD9, 0x6A, 0x1F));

                // 👇 key change: neutral scrollbars (no orange noise)
                this.Resources["ScrollViewerBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0xF3, 0xF3, 0xF7));
                this.Resources["ScrollBarThumbBrush"] = new SolidColorBrush(Color.FromRgb(0xC6, 0xC6, 0xCE));
                this.Resources["ScrollBarTrackBrush"] = new SolidColorBrush(Color.FromRgb(0xE4, 0xE4, 0xEC));
            }
            else
            {
                // Modern dark theme (soft, layered, minimal accent)

                this.Resources["MainBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x18, 0x18, 0x1B)); // Rich dark gray
                this.Resources["SecondaryBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x22, 0x22, 0x26)); // Slight lift
                this.Resources["CardBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x2C, 0x2C, 0x31)); // Card elevation

                this.Resources["MainForegroundBrush"] = new SolidColorBrush(Color.FromRgb(0xE6, 0xE6, 0xE6)); // Softer white
                this.Resources["SecondaryForegroundBrush"] = new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xA8)); // Muted text

                // 👇 toned-down, modern orange
                this.Resources["AccentBrush"] = new SolidColorBrush(Color.FromRgb(0xF5, 0x8F, 0x2A));
                this.Resources["AccentForegroundBrush"] = new SolidColorBrush(Color.FromRgb(0xF5, 0x8F, 0x2A));

                this.Resources["BorderBrush"] = new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x40)); // softer border

                this.Resources["TextBoxBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x24, 0x24, 0x28));
                this.Resources["TextBoxForegroundBrush"] = new SolidColorBrush(Color.FromRgb(0xE6, 0xE6, 0xE6));
                this.Resources["TextBoxBorderBrush"] = new SolidColorBrush(Color.FromRgb(0x3F, 0x3F, 0x46));
                this.Resources["TextBoxFocusBorderBrush"] = new SolidColorBrush(Color.FromRgb(0xF5, 0x8F, 0x2A));

                this.Resources["ScrollViewerBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x20, 0x20, 0x24));
                this.Resources["ScrollBarThumbBrush"] = new SolidColorBrush(Color.FromRgb(0xF5, 0x8F, 0x2A));
                this.Resources["ScrollBarTrackBrush"] = new SolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x2F));
            }
        }

        //---------------------------------------------------
        //---------------------------------------------------
        //---------------------------------------------------

        private async Task<bool> ValidateFFDec()
        {
            string ffdecPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffdec", "ffdec.exe");

            //check for ffdec.
            if (!File.Exists(ffdecPath))
            {
                var cm = new Confirmation_Message(
                    "FFDec Validation Failed...",
                    $"Completionist GUI Patcher relies on a tool called FFDec to function.\nWithout FFDec Installed, no files can be patched.\n\nDo you want to download it now?",
                    _isLightTheme,
                    null,
                    null,
                    "Download",
                    "Cancel",
                    5);

                cm.ShowDialog();
                if (cm.GetUserInputValue() == MSGReturn.kAccept)
                {
                    UpdateLog("Downloading FFDec...");
                    bool success = await DownloadFFDec();
                    if (!success)
                    {
                        UpdateLog("FFDec setup failed. Aborting update check.");
                        ShutDown();
                        return false;
                    }
                }
                else
                {
                    UpdateLog($"Unable to continue without FFDec...");
                    ShutDown();
                    return false;
                }
            }
            return true;
        }

        //---------------------------------------------------
        //---------------------------------------------------
        //---------------------------------------------------

        private void ShutDown()
        {
            int countdown = 5;

            // Display countdown message
            UpdateLog($"Shutting down in {countdown} seconds...");

            // Create a timer to handle the countdown
            DispatcherTimer timer = new()
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timer.Tick += (sender, e) =>
            {
                countdown--;
                UpdateLog($"Shutting down in {countdown} seconds...", true);

                if (countdown <= 0)
                {
                    // Stop the timer and shut down
                    timer.Stop();
                    UpdateLog("Shutting Down...");
                    Application.Current.Shutdown();
                }
            };

            // Start the countdown
            timer.Start();
        }

        //---------------------------------------------------
        //---------------------------------------------------
        //---------------------------------------------------

        public async Task<bool> DownloadFFDec()
        {
            string url = "https://github.com/jindrapetrik/jpexs-decompiler/releases/download/nightly3043/ffdec_22.0.2_nightly3043.zip";
            string downloadsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory);
            string downloadPath = Path.Combine(downloadsPath, "ffdec.zip");
            string extractPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffdec");

            // Ensure downloads directory exists
            if (!Directory.Exists(downloadsPath))
                Directory.CreateDirectory(downloadsPath);

            // Ensure extract directory exists
            if (!Directory.Exists(extractPath))
                Directory.CreateDirectory(extractPath);

            using HttpClient client = new();
            try
            {
                byte[] fileBytes = await client.GetByteArrayAsync(url);
                await File.WriteAllBytesAsync(downloadPath, fileBytes);
                UpdateLog("Download complete. Extracting...");

                // Ensure ffdec folder exists before extracting
                if (!Directory.Exists(extractPath))
                    Directory.CreateDirectory(extractPath);

                System.IO.Compression.ZipFile.ExtractToDirectory(downloadPath, extractPath);
                UpdateLog("FFDec downloaded & extracted successfully.");
                return true;
            }
            catch (Exception ex)
            {
                UpdateLog($"Error downloading or extracting FFDec: {ex.Message}");
                UpdateLog("Attempting to fetch latest nightly build.");

                return await DownloadFFDecLatestNightly();
            }
            finally
            {
                // Cleanup: Only delete the ZIP file, not the entire folder
                if (File.Exists(downloadPath))
                {
                    File.Delete(downloadPath);
                }
            }
        }

        //---------------------------------------------------
        //---------------------------------------------------
        //---------------------------------------------------

        private static readonly JsonSerializerOptions jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public async Task<bool> DownloadFFDecLatestNightly()
        {
            string apiUrl = "https://api.github.com/repos/jindrapetrik/jpexs-decompiler/releases";
            string downloadsPath = AppDomain.CurrentDomain.BaseDirectory;
            string downloadPath = Path.Combine(downloadsPath, "ffdec.zip");
            string extractPath = Path.Combine(downloadsPath, "ffdec");

            try
            {
                using HttpClient client = new();
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; MyApp/1.0)");

                string json = await client.GetStringAsync(apiUrl);
                var releases = JsonSerializer.Deserialize<List<GitHubRelease>>(json, jsonOptions);

                // Find the latest nightly pre-release
                var nightly = releases?
                    .Where(r => r.Prerelease && r.TagName.Contains("nightly", StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(r => r.PublishedAt)
                    .FirstOrDefault();

                if (nightly == null)
                {
                    UpdateLog("No nightly release found.");
                    return false;
                }

                var asset = nightly.Assets.FirstOrDefault(a => a.Name.EndsWith(".zip"));
                if (asset == null)
                {
                    UpdateLog("No ZIP file found in latest nightly release.");
                    return false;
                }

                UpdateLog($"Downloading FFDec {nightly.TagName}...");
                byte[] fileBytes = await client.GetByteArrayAsync(asset.BrowserDownloadUrl);
                await File.WriteAllBytesAsync(downloadPath, fileBytes);

                UpdateLog("Download complete. Extracting...");

                if (Directory.Exists(extractPath))
                    Directory.Delete(extractPath, true);

                System.IO.Compression.ZipFile.ExtractToDirectory(downloadPath, extractPath);
                UpdateLog("FFDec downloaded & extracted successfully.");
                return true;
            }
            catch (Exception ex)
            {
                UpdateLog($"Error downloading or extracting FFDec: {ex.Message}");
                return false;
            }
            finally
            {
                if (File.Exists(downloadPath))
                    File.Delete(downloadPath);
            }
        }

        //---------------------------------------------------
        //---------------------------------------------------
        //---------------------------------------------------

        private void OpenFileDialogAndValidate(string eventName)
        {
            if (!_inputAllowed)
                return;

            OpenFileDialog openFileDialog = new()
            {
                //InitialDirectory = AppDomain.CurrentDomain.BaseDirectory,
                Filter = "SWF files (*.swf)|*.swf",
                Title = "Select a SWF file"
            };

            while (true)
            {
                if (openFileDialog.ShowDialog() == true)
                {
                    string fileName = Path.GetFileName(openFileDialog.FileName);
                    if (validFileMappings.TryGetValue(eventName, out string? validFileName) && fileName.Equals(validFileName, StringComparison.OrdinalIgnoreCase))
                    {
                        if (FindName(eventName) is System.Windows.Controls.TextBox textBox)
                        {
                            textBox.Text = openFileDialog.FileName;
                            LoadOrder!.Text = "Select mods / staging folder";
                            UpdateLog($"Selected File: {textBox.Text}");
                            Start.IsEnabled = true;
                        }
                        return;
                    }
                    else
                    {
                        UpdateLog($"Invalid file selected: {openFileDialog.FileName} instead of {validFileMappings[eventName]}");
                        MessageBox.Show($"Invalid file selected. Please select the correct file: {validFileMappings[eventName]}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    return; // User canceled the dialog
                }
            }
        }

        //---------------------------------------------------
        //---------------------------------------------------
        //---------------------------------------------------

        public void UpdateLog(string message, bool clearLastLine = false)
        {
            if (_logLines.Count == 0)
                Log.Text = string.Empty;

            if (clearLastLine && _logLines.Count > 0)
            {
                // Replace the last line with the new message
                _logLines[^1] = $"{message}\n\n";
            }
            else
            {
                // Add the new message to the log lines
                _logLines.Add($"{message}\n\n");
            }

            Log.Text = string.Join("", _logLines);
        }

        //---------------------------------------------------
        //---------------------------------------------------
        //---------------------------------------------------

        public void ClearLog()
        {
            _logLines.Clear();
            Log!.Text = "Nothing To Show...";
        }

        private void DisplayCurrentWindowDimentions()
        {
            UpdateLog($"Current Window Dimensions: {ActualWidth}x{ActualHeight}");
        }

        //---------------------------------------------------
        //---------------------------------------------------
        //---------------------------------------------------

        private void SelectInventoryLists_Click(object sender, RoutedEventArgs e) => OpenFileDialogAndValidate("InventoryLists");
        private void SelectCraftingMenu_Click(object sender, RoutedEventArgs e) => OpenFileDialogAndValidate("CraftingMenu");
        private void SelectLootMenu_Click(object sender, RoutedEventArgs e) => OpenFileDialogAndValidate("LootMenu");
        private void SelectCocksMenu_Click(object sender, RoutedEventArgs e) => OpenFileDialogAndValidate("CocksMenu");

        //---------------------------------------------------
        //---------------------------------------------------
        //---------------------------------------------------

        private void SelectLoadOrder_Click(object sender, RoutedEventArgs e)
        {
            if (!_inputAllowed)
                return;

            OpenFolderDialog openFolderDialog = new()
            {
                Title = "Select a folder"
            };

            if (openFolderDialog.ShowDialog() == true)
            {
                LoadOrder.Text = openFolderDialog.FolderName;
                InventoryLists!.Text = "Select inventorylists.swf";
                CraftingMenu!.Text = "Select craftingmenu.swf";
                CocksMenu!.Text = "Select constructibleobjectmenu.swf";
                UpdateLog($"Selected Folder: {LoadOrder.Text}");
                Start.IsEnabled = true;
            }
        }

        //---------------------------------------------------
        //---------------------------------------------------
        //---------------------------------------------------

        private void CopyLog_Click(object sender, RoutedEventArgs e)
        {
            var existingLog = Log!.Text;
            Clipboard.SetText(existingLog);
            CopyLog.IsEnabled = false;
            ClearLogButton.IsEnabled = false;
            Log.Text = $"Log copied to clipboard. Restoring in 5 seconds...";

            int countdown = 4;
            DispatcherTimer timer = new()
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timer.Tick += (s, args) =>
            {
                if (countdown > 0)
                {
                    Log.Text = $"Log copied to clipboard. Restoring in {countdown} seconds...";
                    countdown--;
                }
                else
                {
                    timer.Stop();
                    Log.Text = existingLog;
                    CopyLog.IsEnabled = true;
                    ClearLogButton.IsEnabled = true;
                }
            };
            timer.Start();
        }

        //---------------------------------------------------
        //---------------------------------------------------
        //---------------------------------------------------

        private void ClearLogButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_inputAllowed)
                return;

            ClearLog();
        }

        //---------------------------------------------------
        //---------------------------------------------------
        //---------------------------------------------------

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            if (!_inputAllowed)
                return;

            if (LoadOrder!.Text != "Select mods / staging folder")
            {
                try
                {
                    // Get all files recursively
                    string[] files = Directory.GetFiles(LoadOrder.Text, "*", SearchOption.AllDirectories);
                    int filesPatched = 0;
                    int validFilesNames = 0;

                    foreach (string file in files)
                    {
                        if (Path.GetFileName(file).Equals("inventorylists.swf", StringComparison.OrdinalIgnoreCase))
                        {
                            validFilesNames++;
                            UpdateLog($"Selected File: {file}");
                            filesPatched += StandardPatcher.Patch(this, file, "InventoryListEntry");
                        }

                        if (Path.GetFileName(file).Equals("craftingmenu.swf", StringComparison.OrdinalIgnoreCase))
                        {
                            validFilesNames++;
                            UpdateLog($"Selected File: {file}");
                            filesPatched += StandardPatcher.Patch(this, file, "CraftingListEntry");
                        }

                        if (Path.GetFileName(file).Equals("constructibleobjectmenu.swf", StringComparison.OrdinalIgnoreCase))
                        {
                            validFilesNames++;
                            UpdateLog($"Selected File: {file}");
                            filesPatched += StandardPatcher.Patch(this, file, "CraftingListEntry");
                        }
                    }

                    UpdateLog($"Patched {filesPatched}/{validFilesNames} valid files.");
                }
                catch (Exception ex)
                {
                    UpdateLog($"Error while traversing folder: {ex.Message}");
                }
            }

            if (InventoryLists!.Text != "Select inventorylists.swf")
            {
                UpdateLog($"Patching inventorylists.swf");
                StandardPatcher.Patch(this, InventoryLists!.Text, "InventoryListEntry");
            }

            if (CraftingMenu!.Text != "Select craftingmenu.swf")
            {
                UpdateLog($"Patching craftingmenu.swf");
                StandardPatcher.Patch(this, CraftingMenu!.Text, "CraftingListEntry");
            }

            if (CocksMenu!.Text != "Select constructibleobjectmenu.swf")
            {
                UpdateLog($"Patching constructibleobjectmenu.swf");
                StandardPatcher.Patch(this, CocksMenu!.Text, "CraftingListEntry");
            }
        }

        //---------------------------------------------------
        //---------------------------------------------------
        //---------------------------------------------------

        private void Window_SourceInitialized(object? sender, EventArgs e)
        {
            IntPtr handle = new WindowInteropHelper(this).Handle;
            HwndSource.FromHwnd(handle)?.AddHook(Util.WindowProc);
        }

        //---------------------------------------------------
        //---------------------------------------------------
        //---------------------------------------------------

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            const double topRegionHeight = 50;
            Point mousePosition = e.GetPosition(this);

            if (e.LeftButton == MouseButtonState.Pressed && mousePosition.Y <= topRegionHeight)
            {
                if (e.ClickCount == 2)
                {
                    if (ResizeMode != ResizeMode.CanResize &&
                        ResizeMode != ResizeMode.CanResizeWithGrip)
                    {
                        return;
                    }

                    WindowState = WindowState == WindowState.Maximized
                        ? WindowState.Normal
                        : WindowState.Maximized;
                }
                else
                {
                    mRestoreForDragMove = WindowState == WindowState.Maximized;
                    DragMove();
                }
            }
        }

        //---------------------------------------------------
        //---------------------------------------------------
        //---------------------------------------------------

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (mRestoreForDragMove)
            {
                mRestoreForDragMove = false;

                WindowState = WindowState.Normal;
                DragMove();
            }
        }

        //---------------------------------------------------
        //---------------------------------------------------
        //---------------------------------------------------

        private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            mRestoreForDragMove = false;
        }

        //---------------------------------------------------
        //---------------------------------------------------
        //---------------------------------------------------

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        //---------------------------------------------------
        //---------------------------------------------------
        //---------------------------------------------------

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
            }
        }

        //---------------------------------------------------
        //---------------------------------------------------
        //---------------------------------------------------

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        //---------------------------------------------------
        //---------------------------------------------------
        //---------------------------------------------------

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
        }

        //---------------------------------------------------
        //---------------------------------------------------
        //---------------------------------------------------
        
        private void PinWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isPinned = !_isPinned;
            PinWindow.Source = new BitmapImage(new Uri($"pack://application:,,,/Images/{(_isPinned ? "pinned.png" : "unpinned.png")}", UriKind.Absolute));
            PinWindow.ToolTip = _isPinned ? "Unpin Window" : "Pin Window";
            Topmost = _isPinned;
        }
    }
}