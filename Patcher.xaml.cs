using Completionist_GUI_Patcher.Messages.ConfirmationMessage;
using Completionist_GUI_Patcher.Utility;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Util = Completionist_GUI_Patcher.Utility.Utility;
using GUpd = Completionist_GUI_Patcher.Utility.GitHubUpdater;
using System.Windows.Controls;
using System.Net.Http;
using System.Windows.Threading;

namespace Completionist_GUI_Patcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class Patcher : Window
    {
        private bool mRestoreForDragMove = false;
        private bool _isPinned = false;
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
            VersionLabel.Content = $"Patcher Version {GUpd.GetCurrentVersion()}";
            this.Loaded += Patcher_Loaded;
        }

        //---------------------------------------------------
        //---------------------------------------------------
        //---------------------------------------------------

        private async void Patcher_Loaded(object sender, RoutedEventArgs e)
        {
            //check for ffdec.
            if (!ValidateFFDec())
            {
                var cm = new Confirmation_Message(
                    "FFDec Validation Failed...",
                    $"Completionist GUI Patcher relies on a tool called FFDec to function.\nWithout FFDec Installed, no files can be patched.\n\nDo you want to download it now?",
                    null,
                    null,
                    "Download",
                    "Cancel",
                    5);

                cm.ShowDialog();
                if (cm.GetUserInputValue() == Confirmation_Message.Confirmation_Message_Return_Value.kAccept)
                {
                    UpdateLog("Downloading FFDec...");
                    bool success = await DownloadFFDec();
                    if (!success)
                    {
                        UpdateLog("FFDec setup failed. Aborting update check.");
                        return; // Exit early if FFDec isn't available
                    }
                }
                else
                {
                    UpdateLog($"Unable to continue without FFDec...");

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
            }

            //do app updates
            await GUpd.CheckForUpdates();
            if (GUpd.CanUpdate()) // Now this will correctly return true if an update is found
            {
                var cm = new Confirmation_Message(
                    "Patcher Update Found...",
                    $"A new version ({GUpd.GetLatestVersion()}) is available. You are running {GUpd.GetCurrentVersion()}.\nDo you want to update now?",
                    null,
                    null,
                    "Update Now",
                    "Cancel",
                    5);

                cm.ShowDialog();
                if (cm.GetUserInputValue() == Confirmation_Message.Confirmation_Message_Return_Value.kAccept)
                {
                    GUpd.DoUpdate();
                }
            }
        }

        //---------------------------------------------------
        //---------------------------------------------------
        //---------------------------------------------------

        private static bool ValidateFFDec()
        {
            string ffdecPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffdec", "ffdec.exe");
            return File.Exists(ffdecPath);
        }

        //---------------------------------------------------
        //---------------------------------------------------
        //---------------------------------------------------

        public async Task<bool> DownloadFFDec()
        {
            string url = "https://github.com/jindrapetrik/jpexs-decompiler/releases/download/nightly3026/ffdec_22.0.2_nightly3026.zip";
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
                UpdateLog("FFDec extracted successfully.");
                return true;
            }
            catch (Exception ex)
            {
                UpdateLog($"Error downloading or extracting FFDec: {ex.Message}");
                return false;
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

        private void OpenFileDialogAndValidate(string eventName)
        {
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
            }
        }

        //---------------------------------------------------
        //---------------------------------------------------
        //---------------------------------------------------

        private void CopyLog_Click(object sender, RoutedEventArgs e)
        {
            UpdateLog($"Log Copied To Clipboard...");
            Clipboard.SetText(Log!.Text);
        }

        //---------------------------------------------------
        //---------------------------------------------------
        //---------------------------------------------------

        private void ClearLogButton_Click(object sender, RoutedEventArgs e)
        {
            ClearLog();
        }

        //---------------------------------------------------
        //---------------------------------------------------
        //---------------------------------------------------

        private void Start_Click(object sender, RoutedEventArgs e)
        {
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