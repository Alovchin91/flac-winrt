using System;
using System.Windows.Input;

namespace FLAC_WinRT.Example.App.ViewModels
{
    /// <summary>
    /// Defines a command that performs an action if a condition is met.
    /// </summary>
    public sealed class RelayCommand : ICommand
    {
        private readonly Action<object> _action;
        private readonly Predicate<object> _canExecute;

        public RelayCommand(Action action)
            : this(o => action(), null)
        {
        }

        public RelayCommand(Action action, Func<bool> canExecute)
            : this(o => action(), o => canExecute())
        {
        }

        public RelayCommand(Action<object> action)
            : this(action, null)
        {
        }

        public RelayCommand(Action<object> action, Predicate<object> canExecute)
        {
            if (action == null)
            {
                throw new ArgumentNullException("action", "Action must not be null.");
            }
            this._action = action;
            this._canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return this._canExecute == null || this._canExecute(parameter);
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {
            this._action(parameter);
        }

        public void RaiseCanExecuteChanged()
        {
            EventHandler handler = this.CanExecuteChanged;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }
    }
}
