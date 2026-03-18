namespace Classify.Core.Domain;

public class Composer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public override string ToString() => Name;
}