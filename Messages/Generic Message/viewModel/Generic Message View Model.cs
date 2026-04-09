using Completionist_GUI_Patcher.Utility;
using RLC = Completionist_GUI_Patcher.Utility.RelayCommand;

namespace Completionist_GUI_Patcher.Messages.Generic_Message.viewModel
{
    internal class Generic_Message_View_Model : ObservableObject
    {
        private string _title = string.Empty;
        private string _body = string.Empty;
        private string _buttonTextOk = "Ok";

        public Action? ConfirmOk { get; set; }

        public RLC? OkButtonClicked => new(execute => OnOkClicked());

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

        public string ButtonTextOk
        {
            get => _buttonTextOk;
            set { _buttonTextOk = value; OnPropertyChanged(nameof(ButtonTextOk)); }
        }

        public Generic_Message_View_Model()
        {
        }

        public Generic_Message_View_Model(string title, string body, string buttonTextOk = "Ok")
        {
            Title = title;
            Body = body;
            ButtonTextOk = buttonTextOk;
        }

        private void OnOkClicked()
        {
            ConfirmOk?.Invoke();
        }
    }
}
