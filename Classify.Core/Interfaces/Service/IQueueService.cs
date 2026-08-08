using System;
using System.Collections.Generic;
using Classify.Core.Domain;

namespace Classify.Core.Interfaces.Service;

/// <summary>
/// Defines the contract for an in-memory queue service that manages queued nibbles and tracks current playback position.
/// </summary>
public interface IQueueService
{
    /// <summary>
    /// Occurs when the queue contents (items added, removed, or cleared) change.
    /// </summary>
    event EventHandler? QueueChanged;

    /// <summary>
    /// Occurs when the currently playing nibble or movement position changes.
    /// </summary>
    event EventHandler? CurrentItemChanged;

    /// <summary>
    /// Gets the current list of queued items.
    /// </summary>
    IReadOnlyList<QueueItem> Items { get; }

    /// <summary>
    /// Gets the zero-based index of the currently playing nibble in the queue, or -1 if queue is empty.
    /// </summary>
    int CurrentNibbleIndex { get; }

    /// <summary>
    /// Gets the zero-based index of the currently playing movement within the current nibble, or -1 if queue is empty.
    /// </summary>
    int CurrentMovementIndex { get; }

    /// <summary>
    /// Gets the currently playing <see cref="QueueItem"/>, or <c>null</c> if queue is empty.
    /// </summary>
    QueueItem? CurrentItem { get; }

    /// <summary>
    /// Gets the currently playing <see cref="NibbleMovement"/>, or <c>null</c> if queue is empty.
    /// </summary>
    NibbleMovement? CurrentMovement { get; }

    /// <summary>
    /// Adds a nibble and its movements to the end of the queue.
    /// </summary>
    /// <param name="nibble">The nibble to enqueue.</param>
    /// <param name="movements">The movements belonging to the nibble.</param>
    void Enqueue(Nibble nibble, IEnumerable<NibbleMovement> movements);

    /// <summary>
    /// Adds a queue item to the end of the queue.
    /// </summary>
    /// <param name="item">The queue item to add.</param>
    void Enqueue(QueueItem item);

    /// <summary>
    /// Adds a range of queue items to the end of the queue.
    /// </summary>
    /// <param name="items">The queue items to add.</param>
    void EnqueueRange(IEnumerable<QueueItem> items);

    /// <summary>
    /// Inserts a nibble and its movements to play next in the queue (immediately following the currently playing item).
    /// </summary>
    /// <param name="nibble">The nibble to insert.</param>
    /// <param name="movements">The movements belonging to the nibble.</param>
    void EnqueueNext(Nibble nibble, IEnumerable<NibbleMovement> movements);

    /// <summary>
    /// Inserts a queue item to play next in the queue (immediately following the currently playing item).
    /// </summary>
    /// <param name="item">The queue item to insert.</param>
    void EnqueueNext(QueueItem item);

    /// <summary>
    /// Inserts a range of queue items to play next in the queue (immediately following the currently playing item).
    /// </summary>
    /// <param name="items">The queue items to insert.</param>
    void EnqueueNextRange(IEnumerable<QueueItem> items);

    /// <summary>
    /// Removes the queue item at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the item to remove.</param>
    /// <returns><c>true</c> if the item was successfully removed; otherwise, <c>false</c>.</returns>
    bool RemoveAt(int index);

    /// <summary>
    /// Clears all items from the queue and resets current playback position.
    /// </summary>
    void Clear();

    /// <summary>
    /// Advances playback position to the next movement, moving to the next nibble if at the end of the current nibble.
    /// </summary>
    /// <returns><c>true</c> if advanced successfully; <c>false</c> if already at the end of the queue.</returns>
    bool Next();

    /// <summary>
    /// Rewinds playback position to the previous movement, moving to the previous nibble's last movement if at start of current nibble.
    /// </summary>
    /// <returns><c>true</c> if rewound successfully; <c>false</c> if already at the beginning of the queue.</returns>
    bool Previous();

    /// <summary>
    /// Sets the current playback position directly to the specified nibble and movement indices.
    /// </summary>
    /// <param name="nibbleIndex">The zero-based index of the target nibble.</param>
    /// <param name="movementIndex">The zero-based index of the target movement within the nibble.</param>
    /// <returns><c>true</c> if position was valid and set; otherwise, <c>false</c>.</returns>
    bool SkipToNibble(int nibbleIndex, int movementIndex = 0);
}
