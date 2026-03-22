namespace Classify.Core.Domain;

public class Work
{
    public int Id { get; set; }
    public int ComposerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CatalogNumber { get; set; } = string.Empty;
    public override string ToString() => Name;

}