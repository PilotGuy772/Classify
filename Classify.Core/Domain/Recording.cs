namespace Classify.Core.Domain;

public class Recording : IPlayable
{
    public int Id { get; set; }
    public int WorkId { get; set; }
    public string Conductor { get; set; } = string.Empty;
    public override string ToString() => Conductor;

    public string Name => string.IsNullOrWhiteSpace(Conductor) ? $"Recording #{Id}" : Conductor;

    public string? PhotoKey => null;

    public object BaseObject => this;

    public IReadOnlyList<int> GetOrderedAudioFileIds() => Array.Empty<int>();
}