using System.Windows.Input;

namespace Completionist_GUI_Patcher.Utility
{
    public class RelayCommand(Action<object> execute) : ICommand
    {
        private readonly Action<object> _execute = execute;

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter) => true; // Always returns true

        public void Execute(object? parameter)
        {
            _execute(parameter!);
        }
    }
}
