using System;
using System.Collections.Generic;
using System.Linq;
using Classify.Core.Domain;
using Classify.Core.Interfaces.Service;

namespace Classify.Services.Queue;

/// <summary>
/// Thread-safe in-memory queue service that persists queue state across service instances and DI containers.
/// </summary>
public class QueueService : IQueueService
{
    /// <summary>
    /// Thread-safe shared state container backing all <see cref="QueueService"/> instances.
    /// </summary>
    private static class SharedState
    {
        public static readonly object LockObj = new();
        public static readonly List<QueueItem> ItemsList = [];
        public static int CurrentNibbleIdx = -1;
        public static int CurrentMovementIdx = -1;

        public static event EventHandler? OnQueueChanged;
        public static event EventHandler? OnCurrentItemChanged;

        public static void NotifyQueueChanged()
        {
            OnQueueChanged?.Invoke(null, EventArgs.Empty);
        }

        public static void NotifyCurrentItemChanged()
        {
            OnCurrentItemChanged?.Invoke(null, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Occurs when the queue items change.
    /// </summary>
    public event EventHandler? QueueChanged
    {
        add => SharedState.OnQueueChanged += value;
        remove => SharedState.OnQueueChanged -= value;
    }

    /// <summary>
    /// Occurs when the currently playing nibble or movement changes.
    /// </summary>
    public event EventHandler? CurrentItemChanged
    {
        add => SharedState.OnCurrentItemChanged += value;
        remove => SharedState.OnCurrentItemChanged -= value;
    }

    /// <summary>
    /// Gets the current list of queued items.
    /// </summary>
    public IReadOnlyList<QueueItem> Items
    {
        get
        {
            lock (SharedState.LockObj)
            {
                return new List<QueueItem>(SharedState.ItemsList).AsReadOnly();
            }
        }
    }

    /// <summary>
    /// Gets the zero-based index of the currently playing nibble in the queue, or -1 if queue is empty.
    /// </summary>
    public int CurrentNibbleIndex
    {
        get
        {
            lock (SharedState.LockObj)
            {
                return SharedState.CurrentNibbleIdx;
            }
        }
    }

    /// <summary>
    /// Gets the zero-based index of the currently playing movement within the current nibble, or -1 if queue is empty.
    /// </summary>
    public int CurrentMovementIndex
    {
        get
        {
            lock (SharedState.LockObj)
            {
                return SharedState.CurrentMovementIdx;
            }
        }
    }

    /// <summary>
    /// Gets the currently playing <see cref="QueueItem"/>, or <c>null</c> if queue is empty.
    /// </summary>
    public QueueItem? CurrentItem
    {
        get
        {
            lock (SharedState.LockObj)
            {
                if (SharedState.CurrentNibbleIdx >= 0 && SharedState.CurrentNibbleIdx < SharedState.ItemsList.Count)
                {
                    return SharedState.ItemsList[SharedState.CurrentNibbleIdx];
                }
                return null;
            }
        }
    }

    /// <summary>
    /// Gets the currently playing <see cref="NibbleMovement"/>, or <c>null</c> if queue is empty.
    /// </summary>
    public NibbleMovement? CurrentMovement
    {
        get
        {
            lock (SharedState.LockObj)
            {
                QueueItem? currentItem = CurrentItem;
                if (currentItem != null && SharedState.CurrentMovementIdx >= 0 && SharedState.CurrentMovementIdx < currentItem.Movements.Count)
                {
                    return currentItem.Movements[SharedState.CurrentMovementIdx];
                }
                return null;
            }
        }
    }

    /// <summary>
    /// Adds a nibble and its movements to the end of the queue.
    /// </summary>
    /// <param name="nibble">The nibble to enqueue.</param>
    /// <param name="movements">The movements belonging to the nibble.</param>
    public void Enqueue(Nibble nibble, IEnumerable<NibbleMovement> movements)
    {
        QueueItem item = new(nibble, movements);
        Enqueue(item);
    }

    /// <summary>
    /// Adds a queue item to the end of the queue.
    /// </summary>
    /// <param name="item">The queue item to add.</param>
    public void Enqueue(QueueItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        bool positionChanged = false;
        lock (SharedState.LockObj)
        {
            SharedState.ItemsList.Add(item);
            if (SharedState.CurrentNibbleIdx == -1 && SharedState.ItemsList.Count > 0)
            {
                SharedState.CurrentNibbleIdx = 0;
                SharedState.CurrentMovementIdx = item.Movements.Count > 0 ? 0 : -1;
                positionChanged = true;
            }
        }

        SharedState.NotifyQueueChanged();
        if (positionChanged)
        {
            SharedState.NotifyCurrentItemChanged();
        }
        
        Console.WriteLine("Added range to queue.");
    }

    /// <summary>
    /// Adds a range of queue items to the end of the queue.
    /// </summary>
    /// <param name="items">The queue items to add.</param>
    public void EnqueueRange(IEnumerable<QueueItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        List<QueueItem> newItems = new(items);
        if (newItems.Count == 0)
        {
            return;
        }

        bool positionChanged = false;
        lock (SharedState.LockObj)
        {
            SharedState.ItemsList.AddRange(newItems);
            if (SharedState.CurrentNibbleIdx == -1 && SharedState.ItemsList.Count > 0)
            {
                SharedState.CurrentNibbleIdx = 0;
                SharedState.CurrentMovementIdx = SharedState.ItemsList[0].Movements.Count > 0 ? 0 : -1;
                positionChanged = true;
            }
        }

        SharedState.NotifyQueueChanged();
        if (positionChanged)
        {
            SharedState.NotifyCurrentItemChanged();
        }
        
        Console.WriteLine("Added range to queue (next).");

    }

    /// <summary>
    /// Inserts a nibble and its movements to play next in the queue (immediately following the currently playing item).
    /// </summary>
    /// <param name="nibble">The nibble to insert.</param>
    /// <param name="movements">The movements belonging to the nibble.</param>
    public void EnqueueNext(Nibble nibble, IEnumerable<NibbleMovement> movements)
    {
        QueueItem item = new(nibble, movements);
        EnqueueNext(item);
    }

    /// <summary>
    /// Inserts a queue item to play next in the queue (immediately following the currently playing item).
    /// </summary>
    /// <param name="item">The queue item to insert.</param>
    public void EnqueueNext(QueueItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        EnqueueNextRange(new List<QueueItem> { item });
    }

    /// <summary>
    /// Inserts a range of queue items to play next in the queue (immediately following the currently playing item).
    /// </summary>
    /// <param name="items">The queue items to insert.</param>
    public void EnqueueNextRange(IEnumerable<QueueItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        List<QueueItem> newItems = new(items);
        if (newItems.Count == 0)
        {
            return;
        }

        bool positionChanged = false;
        lock (SharedState.LockObj)
        {
            if (SharedState.ItemsList.Count == 0 || SharedState.CurrentNibbleIdx == -1)
            {
                SharedState.ItemsList.AddRange(newItems);
                SharedState.CurrentNibbleIdx = 0;
                SharedState.CurrentMovementIdx = SharedState.ItemsList[0].Movements.Count > 0 ? 0 : -1;
                positionChanged = true;
            }
            else
            {
                int insertIndex = SharedState.CurrentNibbleIdx + 1;
                SharedState.ItemsList.InsertRange(insertIndex, newItems);
            }
        }

        SharedState.NotifyQueueChanged();
        if (positionChanged)
        {
            SharedState.NotifyCurrentItemChanged();
        }
        
        Console.WriteLine("Added item to queue (next).");
    }

    /// <summary>
    /// Removes the queue item at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the item to remove.</param>
    /// <returns><c>true</c> if the item was successfully removed; otherwise, <c>false</c>.</returns>
    public bool RemoveAt(int index)
    {
        bool positionChanged = false;
        bool removed = false;

        lock (SharedState.LockObj)
        {
            if (index < 0 || index >= SharedState.ItemsList.Count)
            {
                return false;
            }

            SharedState.ItemsList.RemoveAt(index);
            removed = true;

            if (SharedState.ItemsList.Count == 0)
            {
                SharedState.CurrentNibbleIdx = -1;
                SharedState.CurrentMovementIdx = -1;
                positionChanged = true;
            }
            else if (index < SharedState.CurrentNibbleIdx)
            {
                SharedState.CurrentNibbleIdx--;
                positionChanged = true;
            }
            else if (index == SharedState.CurrentNibbleIdx)
            {
                if (SharedState.CurrentNibbleIdx >= SharedState.ItemsList.Count)
                {
                    SharedState.CurrentNibbleIdx = SharedState.ItemsList.Count - 1;
                }
                QueueItem current = SharedState.ItemsList[SharedState.CurrentNibbleIdx];
                SharedState.CurrentMovementIdx = current.Movements.Count > 0 ? 0 : -1;
                positionChanged = true;
            }
        }

        if (removed)
        {
            SharedState.NotifyQueueChanged();
        }
        if (positionChanged)
        {
            SharedState.NotifyCurrentItemChanged();
        }

        return removed;
    }

    /// <summary>
    /// Clears all items from the queue and resets current playback position.
    /// </summary>
    public void Clear()
    {
        lock (SharedState.LockObj)
        {
            SharedState.ItemsList.Clear();
            SharedState.CurrentNibbleIdx = -1;
            SharedState.CurrentMovementIdx = -1;
        }

        SharedState.NotifyQueueChanged();
        SharedState.NotifyCurrentItemChanged();
    }

    /// <summary>
    /// Advances playback position to the next movement, moving to the next nibble if at the end of the current nibble.
    /// </summary>
    /// <returns><c>true</c> if advanced successfully; <c>false</c> if already at the end of the queue.</returns>
    public bool Next()
    {
        bool positionChanged = false;

        lock (SharedState.LockObj)
        {
            if (SharedState.ItemsList.Count == 0 || SharedState.CurrentNibbleIdx == -1)
            {
                return false;
            }

            QueueItem currentItem = SharedState.ItemsList[SharedState.CurrentNibbleIdx];

            if (SharedState.CurrentMovementIdx + 1 < currentItem.Movements.Count)
            {
                SharedState.CurrentMovementIdx++;
                positionChanged = true;
            }
            else if (SharedState.CurrentNibbleIdx + 1 < SharedState.ItemsList.Count)
            {
                SharedState.CurrentNibbleIdx++;
                QueueItem nextItem = SharedState.ItemsList[SharedState.CurrentNibbleIdx];
                SharedState.CurrentMovementIdx = nextItem.Movements.Count > 0 ? 0 : -1;
                positionChanged = true;
            }
            else
            {
                return false;
            }
        }

        if (positionChanged)
        {
            SharedState.NotifyCurrentItemChanged();
        }

        return true;
    }

    /// <summary>
    /// Rewinds playback position to the previous movement, moving to the previous nibble's last movement if at start of current nibble.
    /// </summary>
    /// <returns><c>true</c> if rewound successfully; <c>false</c> if already at the beginning of the queue.</returns>
    public bool Previous()
    {
        bool positionChanged = false;

        lock (SharedState.LockObj)
        {
            if (SharedState.ItemsList.Count == 0 || SharedState.CurrentNibbleIdx == -1)
            {
                return false;
            }

            if (SharedState.CurrentMovementIdx > 0)
            {
                SharedState.CurrentMovementIdx--;
                positionChanged = true;
            }
            else if (SharedState.CurrentNibbleIdx > 0)
            {
                SharedState.CurrentNibbleIdx--;
                QueueItem prevItem = SharedState.ItemsList[SharedState.CurrentNibbleIdx];
                SharedState.CurrentMovementIdx = prevItem.Movements.Count > 0 ? prevItem.Movements.Count - 1 : -1;
                positionChanged = true;
            }
            else
            {
                return false;
            }
        }

        if (positionChanged)
        {
            SharedState.NotifyCurrentItemChanged();
        }

        return true;
    }

    /// <summary>
    /// Sets the current playback position directly to the specified nibble and movement indices.
    /// </summary>
    /// <param name="nibbleIndex">The zero-based index of the target nibble.</param>
    /// <param name="movementIndex">The zero-based index of the target movement within the nibble.</param>
    /// <returns><c>true</c> if position was valid and set; otherwise, <c>false</c>.</returns>
    public bool SkipToNibble(int nibbleIndex, int movementIndex = 0)
    {
        bool positionChanged = false;

        lock (SharedState.LockObj)
        {
            if (nibbleIndex < 0 || nibbleIndex >= SharedState.ItemsList.Count)
            {
                return false;
            }

            QueueItem item = SharedState.ItemsList[nibbleIndex];
            int validMovementIndex = (movementIndex >= 0 && movementIndex < item.Movements.Count) ? movementIndex : (item.Movements.Count > 0 ? 0 : -1);

            if (SharedState.CurrentNibbleIdx != nibbleIndex || SharedState.CurrentMovementIdx != validMovementIndex)
            {
                SharedState.CurrentNibbleIdx = nibbleIndex;
                SharedState.CurrentMovementIdx = validMovementIndex;
                positionChanged = true;
            }
        }

        if (positionChanged)
        {
            SharedState.NotifyCurrentItemChanged();
        }

        return true;
    }
}
