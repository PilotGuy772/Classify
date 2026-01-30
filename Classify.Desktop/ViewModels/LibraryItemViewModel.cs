namespace Classify.Desktop.ViewModels;

public class LibraryItemViewModel(int id, string displayText, LibraryItemType type)
{
    public int Id { get; } = id;
    public string DisplayText { get; } = displayText;
    public LibraryItemType Type { get; } = type;
}