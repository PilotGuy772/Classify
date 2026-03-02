using System.Windows.Input;

namespace Classify.Core.Domain.Infrastructure;

public class AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null) : ICommand
{
    private readonly Func<Task> _execute = execute ?? throw new ArgumentNullException(nameof(execute));
    private bool _isExecuting;

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => !_isExecuting && (canExecute?.Invoke() ?? true);

    public async Task ExecuteAsync(object? parameter)
    {
        if (!CanExecute(parameter)) return;

        _isExecuting = true;
        RaiseCanExecuteChanged();

        try
        {
            await _execute();
        }
        finally
        {
            _isExecuting = false;
            RaiseCanExecuteChanged();
        }
    }

    public void Execute(object? parameter)
    {
        _ = ExecuteAsync(parameter);
    }

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

public class AsyncRelayCommand<T> : ICommand
{
    private readonly Func<T, Task> _execute;
    private readonly Func<bool>? _canExecute;
    private bool _isExecuting;

    public event EventHandler? CanExecuteChanged;

    public AsyncRelayCommand(Func<T, Task> execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => !_isExecuting && (_canExecute?.Invoke() ?? true);

    public async Task ExecuteAsync(object? parameter)
    {
        if (!CanExecute(parameter)) return;

        _isExecuting = true;
        RaiseCanExecuteChanged();

        try
        {
            if (parameter is T typedParameter)
            {
                await _execute(typedParameter);
            }
        }
        finally
        {
            _isExecuting = false;
            RaiseCanExecuteChanged();
        }
    }

    public void Execute(object? parameter)
    {
        _ = ExecuteAsync(parameter);
    }

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
