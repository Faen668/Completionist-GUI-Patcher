using Completionist_GUI_Patcher.Messages.ConfirmationMessage.ViewModel;
using System.Windows;
using System.Windows.Input;

namespace Completionist_GUI_Patcher.Messages.ConfirmationMessage
{
    public partial class Confirmation_Message : Window
    {
        public enum Confirmation_Message_Return_Value
        {
            kCancel,
            kAccept,
            kAcceptAll
        }

        public Confirmation_Message_Return_Value UserInputResult = Confirmation_Message_Return_Value.kCancel;

        public Confirmation_Message(string title, string body, Window? owner = null, Point? screenPosition = null, string buttonTextYes = "Ok", string buttonTextCan = "Ok", int waitTime = 0)
        {
            InitializeComponent();
            this.Owner = owner ?? Application.Current.MainWindow;
            this.ShowInTaskbar = false;

            Confirmation_Message_View_Model viewModel = new(title, body, buttonTextYes, buttonTextCan, waitTime)
            {
                ConfirmYes = () => this.ConfirmYes(),
                ConfirmCan = () => this.ConfirmCan(),
            };
            DataContext = viewModel;

            if (screenPosition.HasValue)
            {
                this.WindowStartupLocation = WindowStartupLocation.Manual;

                this.Left = screenPosition.Value.X;
                this.Top = screenPosition.Value.Y;

            }
            else
            {
                if (this.Owner != null)
                {
                    this.WindowStartupLocation = WindowStartupLocation.Manual;

                    // Calculate the center-top position relative to the owner window
                    double ownerLeft = this.Owner.Left;
                    double ownerTop = this.Owner.Top;
                    double ownerWidth = this.Owner.Width;

                    // Set the new window's position
                    this.Left = ownerLeft + (ownerWidth - this.Width) / 2; // Center horizontally
                    this.Top = ownerTop + 50; // Offset vertically by 50 pixels
                }
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            const double topRegionHeight = 20;  // You can adjust this value as needed
            Point mousePosition = e.GetPosition(this);

            if (e.LeftButton == MouseButtonState.Pressed && mousePosition.Y <= topRegionHeight)
                DragMove();
        }

        private void ConfirmYes()
        {
            this.UserInputResult = Confirmation_Message_Return_Value.kAccept;
            this.Close();
        }

        private void ConfirmCan()
        {
            this.UserInputResult = Confirmation_Message_Return_Value.kCancel;
            this.Close();
        }

        public Confirmation_Message_Return_Value GetUserInputValue()
        {
            return this.UserInputResult;
        }
    }
}
