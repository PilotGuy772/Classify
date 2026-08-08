using System.Collections.Generic;

namespace Classify.Core.Domain;

/// <summary>
/// Represents an in-memory queue entry linking a <see cref="Nibble"/> and its ordered collection of <see cref="NibbleMovement"/> items.
/// </summary>
public class QueueItem
{
    /// <summary>
    /// Gets the nibble associated with this queue item.
    /// </summary>
    public Nibble Nibble { get; }

    /// <summary>
    /// Gets the ordered list of nibble movements for this queue item.
    /// </summary>
    public IReadOnlyList<NibbleMovement> Movements { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="QueueItem"/> class.
    /// </summary>
    /// <param name="nibble">The nibble for this queue item.</param>
    /// <param name="movements">The ordered movements for this queue item.</param>
    public QueueItem(Nibble nibble, IEnumerable<NibbleMovement> movements)
    {
        ArgumentNullException.ThrowIfNull(nibble);
        ArgumentNullException.ThrowIfNull(movements);

        Nibble = nibble;
        Movements = new List<NibbleMovement>(movements).AsReadOnly();
    }

    /// <summary>
    /// Returns a string representation of the queue item.
    /// </summary>
    /// <returns>A string identifying the queued nibble and movement count.</returns>
    public override string ToString() => $"QueueItem (Nibble #{Nibble.Id}, Movements: {Movements.Count})";
}
