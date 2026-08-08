namespace Classify.Core.Domain;

/// <summary>
/// Represents a join entry connecting a <see cref="Nibble"/> and a <see cref="Movement"/> with an explicit performance order.
/// </summary>
public class NibbleMovement
{
    /// <summary>
    /// Gets or sets the unique identifier for the nibble movement join entry.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the parent nibble.
    /// </summary>
    public int NibbleId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the associated movement.
    /// </summary>
    public int MovementId { get; set; }

    /// <summary>
    /// Gets or sets the relative order of the movement within the nibble.
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Returns a string representation of the nibble movement entry.
    /// </summary>
    /// <returns>A string describing the nibble movement entry.</returns>
    public override string ToString() => $"NibbleMovement #{Id} (Nibble: {NibbleId}, Movement: {MovementId}, Order: {Order})";
}
