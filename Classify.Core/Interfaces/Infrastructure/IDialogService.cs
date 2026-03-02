namespace Classify.Core.Interfaces.Infrastructure;

public interface IDialogService
{
    Task ShowDialogAsync<TViewModel>()
        where TViewModel : class;
    Task ShowDialogAsync<TViewModel, TParam>(TParam parameter)
        where TViewModel : class, IDialog<TParam>;
}