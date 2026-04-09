using Completionist_GUI_Patcher.Messages.Generic_Message.viewModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Completionist_GUI_Patcher.Messages.Generic_Message
{
    /// <summary>
    /// Interaction logic for Generic_Message.xaml
    /// </summary>
    public partial class Generic_Message : Window
    {
        public Generic_Message()
        {
            InitializeComponent();
        }

        public Generic_Message(string title, string body, bool isLightTheme, Window? owner = null, Point? screenPosition = null, string buttonTextOk = "Ok")
        {
            InitializeComponent();
            this.Owner = owner ?? Application.Current.MainWindow;
            this.ShowInTaskbar = false;

            // Apply the theme
            SetTheme(isLightTheme);

            Generic_Message_View_Model viewModel = new(title, body, buttonTextOk)
            {
                ConfirmOk = () => this.ConfirmOk(),
            };
            DataContext = viewModel;

            if (screenPosition.HasValue)
            {
                this.WindowStartupLocation = WindowStartupLocation.Manual;
                this.Left = screenPosition.Value.X;
                this.Top = screenPosition.Value.Y;
            }
            else if (this.Owner != null)
            {
                this.WindowStartupLocation = WindowStartupLocation.Manual;
                double ownerLeft = this.Owner.Left;
                double ownerTop = this.Owner.Top;
                double ownerWidth = this.Owner.Width;
                this.Left = ownerLeft + (ownerWidth - this.Width) / 2;
                this.Top = ownerTop + 50;
            }
        }

        private void SetTheme(bool isLight)
        {
            if (isLight)
            {
                this.Resources["MainBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0xF7, 0xF7, 0xFA));
                this.Resources["SecondaryBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0xEF, 0xEF, 0xF4));
                this.Resources["AccentBrush"] = new SolidColorBrush(Color.FromRgb(0xD9, 0x6A, 0x1F));
                this.Resources["AccentForegroundBrush"] = new SolidColorBrush(Color.FromRgb(0xD9, 0x6A, 0x1F));
                this.Resources["MainForegroundBrush"] = new SolidColorBrush(Color.FromRgb(0x1C, 0x1C, 0x1E));
                this.Resources["SecondaryForegroundBrush"] = new SolidColorBrush(Color.FromRgb(0x6B, 0x6B, 0x73));
            }
            else
            {
                this.Resources["MainBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x18, 0x18, 0x1B));
                this.Resources["SecondaryBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x22, 0x22, 0x26));
                this.Resources["AccentBrush"] = new SolidColorBrush(Color.FromRgb(0xF5, 0x8F, 0x2A));
                this.Resources["AccentForegroundBrush"] = new SolidColorBrush(Color.FromRgb(0xF5, 0x8F, 0x2A));
                this.Resources["MainForegroundBrush"] = new SolidColorBrush(Color.FromRgb(0xE6, 0xE6, 0xE6));
                this.Resources["SecondaryForegroundBrush"] = new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xA8));
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            const double topRegionHeight = 20;
            Point mousePosition = e.GetPosition(this);
            if (e.LeftButton == MouseButtonState.Pressed && mousePosition.Y <= topRegionHeight)
                DragMove();
        }

        private void ConfirmOk()
        {
            this.Close();
        }
    }
}
