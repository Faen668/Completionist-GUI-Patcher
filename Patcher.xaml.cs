using Completionist_GUI_Patcher.Utility;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using Util = Completionist_GUI_Patcher.Utility.Utility;

namespace Completionist_GUI_Patcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class Patcher : Window
    {
        private bool mRestoreForDragMove = false;

        private readonly Dictionary<string, string> validFileMappings = new()
        {
            { "InventoryLists", "inventorylists.swf" },
            { "CraftingMenu", "craftingmenu.swf" },
            { "LootMenu", "lootmenu.swf" },
            { "CocksMenu", "constructibleobjectmenu.swf" }
        };

        //---------------------------------------------------
        //---------------------------------------------------
        //---------------------------------------------------

        public Patcher()
        {
            InitializeComponent();
            this.SourceInitialized += Window_SourceInitialized;
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

        public void UpdateLog(string message)
        {
            if (Log!.Text == "Nothing To Show...")
                Log.Text = string.Empty;

            Log.Text = $"{Log.Text}{message}\n\n";
        }

        //---------------------------------------------------
        //---------------------------------------------------
        //---------------------------------------------------

        public void ClearLog(string message)
        {
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
                LootMenu!.Text = "Select lootmenu.swf";
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

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            if (LoadOrder!.Text != "Select mods / staging folder")
            {
                try
                {
                    // Get all files recursively
                    string[] files = Directory.GetFiles(LoadOrder.Text, "*", SearchOption.AllDirectories);

                    foreach (string file in files)
                    {
                        if (Path.GetFileName(file).Equals("inventorylists.swf", StringComparison.OrdinalIgnoreCase))
                        {
                            UpdateLog($"Selected File: {file}");
                            StandardPatcher.Patch(this, file, "InventoryListEntry");
                        }

                        if (Path.GetFileName(file).Equals("craftingmenu.swf", StringComparison.OrdinalIgnoreCase))
                        {
                            UpdateLog($"Selected File: {file}");
                            StandardPatcher.Patch(this, file, "CraftingListEntry");
                        }

                        if (Path.GetFileName(file).Equals("constructibleobjectmenu.swf", StringComparison.OrdinalIgnoreCase))
                        {
                            UpdateLog($"Selected File: {file}");
                            StandardPatcher.Patch(this, file, "CraftingListEntry");
                        }

                        if (Path.GetFileName(file).Equals("lootmenu.swf", StringComparison.OrdinalIgnoreCase))
                        {
                            UpdateLog($"Selected File: {file}");
                            LootMenuPatcher.Patch(this, file);
                        }
                    }
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

            if (LootMenu!.Text != "Select lootmenu.swf")
            {
                UpdateLog($"Patching lootmenu.swf");
                LootMenuPatcher.Patch(this, LootMenu!.Text);
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
    }
}