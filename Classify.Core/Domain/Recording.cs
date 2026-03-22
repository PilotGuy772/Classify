namespace Classify.Core.Domain;

public class Recording
{
    public int Id { get; set; }
    public int WorkId { get; set; }
    public string Conductor { get; set; } = string.Empty;
    public override string ToString() => Conductor;

}