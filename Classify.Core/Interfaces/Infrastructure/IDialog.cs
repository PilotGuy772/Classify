namespace Classify.Core.Interfaces.Infrastructure;

public interface IDialog<TParam>
{
    void Initialize(TParam parameter);
}