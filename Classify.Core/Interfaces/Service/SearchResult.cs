namespace Classify.Core.Interfaces.Service;

public sealed class SearchResult
{
	public object Entity { get; init; } = null!;
	public string DisplayText { get; init; } = string.Empty;
}

