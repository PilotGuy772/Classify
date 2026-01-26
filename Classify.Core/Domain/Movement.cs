namespace Classify.Core.Domain;

public class Movement
{
    public int Id { get; set; }
    public int WorkId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
}