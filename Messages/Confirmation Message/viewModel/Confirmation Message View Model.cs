using Completionist_GUI_Patcher.Utility;
using System.Windows.Threading;
using RLC = Completionist_GUI_Patcher.Utility.RelayCommand;

namespace Completionist_GUI_Patcher.Messages.ConfirmationMessage.ViewModel
{
    public class Confirmation_Message_View_Model : ObservableObject
    {
        private string _title = string.Empty;
        private string _body = string.Empty;
        private string _buttonTextYes = "Yes";
        private string _buttonTextCan = "Cancel";

        private string _TimerText = "5";
        private int _countdown = 0;
        private readonly DispatcherTimer _timer = new();

        public Action? ConfirmYes { get; set; }
        public Action? ConfirmCan { get; set; }

        public RLC? YesButtonClicked => new(execute => OnYesClicked());
        public RLC? CanButtonClicked => new(execute => OnCanClicked());

        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(nameof(Title)); }
        }

        public string Body
        {
            get => _body;
            set { _body = value; OnPropertyChanged(nameof(Body)); }
        }

        public string ButtonTextYes
        {
            get => _buttonTextYes;
            set { _buttonTextYes = value; OnPropertyChanged(nameof(ButtonTextYes)); }
        }

        public string ButtonTextCan
        {
            get => _buttonTextCan;
            set { _buttonTextCan = value; OnPropertyChanged(nameof(ButtonTextCan)); }
        }

        public string TimerText
        {
            get => _TimerText;
            set { _TimerText = value; OnPropertyChanged(nameof(TimerText)); }
        }

        public bool IsButtonVisible => _countdown <= 0;

        public bool IsTimerVisible => _countdown > 0;

        public Confirmation_Message_View_Model()
        {
        }

        public Confirmation_Message_View_Model(string title, string body, string buttonTextYes = "Ok", string buttonTextCan = "Ok", int waitTime = 0)
        {
            Title = title;
            Body = body;
            ButtonTextYes = buttonTextYes;
            ButtonTextCan = buttonTextCan;

            if (waitTime > 0)
            {
                _countdown = waitTime;
                TimerText = _countdown.ToString();

                _timer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(1)
                };
                _timer.Tick += (sender, args) =>
                {
                    if (_countdown > 0)
                        _countdown--;
                    else
                        _timer.Stop();

                    TimerText = _countdown.ToString();
                    OnPropertyChanged(nameof(IsButtonVisible));
                    OnPropertyChanged(nameof(IsTimerVisible));
                };
                _timer.Start();
            }
        }

        private void OnYesClicked()
        {
            ConfirmYes?.Invoke();
        }

        private void OnCanClicked()
        {
            ConfirmCan?.Invoke();
        }
    }
}
