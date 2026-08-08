using System.Collections.Generic;

using Classify.Core.Domain;
using Classify.Core.Interfaces.Service;
using Classify.Services.Queue;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Tests;

/// <summary>
/// Unit tests for <see cref="QueueService"/> verifying queue manipulation, playback position tracking, events, and cross-instance state persistence.
/// </summary>
public class QueueServiceTests
{
    /// <summary>
    /// Ensures queue starts clean before each test run.
    /// </summary>
    public QueueServiceTests()
    {
        QueueService service = new();
        service.Clear();
    }

    /// <summary>
    /// Verifies that enqueueing items updates the queue list and sets initial current indices.
    /// </summary>
    [Fact]
    public void Enqueue_AddsItemAndSetsInitialPosition()
    {
        QueueService service = new();
        Nibble nibble = new() { Id = 1, WorkId = 10, RecordingId = 100 };
        NibbleMovement m1 = new() { Id = 1, NibbleId = 1, MovementId = 101, Order = 1 };
        NibbleMovement m2 = new() { Id = 2, NibbleId = 1, MovementId = 102, Order = 2 };

        service.Enqueue(nibble, new List<NibbleMovement> { m1, m2 });

        Assert.Single(service.Items);
        Assert.Equal(0, service.CurrentNibbleIndex);
        Assert.Equal(0, service.CurrentMovementIndex);
        Assert.NotNull(service.CurrentItem);
        Assert.Equal(nibble.Id, service.CurrentItem.Nibble.Id);
        Assert.NotNull(service.CurrentMovement);
        Assert.Equal(m1.Id, service.CurrentMovement.Id);
    }

    /// <summary>
    /// Verifies that Next() advances through movements within a nibble and across nibbles.
    /// </summary>
    [Fact]
    public void Next_AdvancesMovementsAndNibbles()
    {
        QueueService service = new();

        Nibble n1 = new() { Id = 1, WorkId = 10, RecordingId = 100 };
        NibbleMovement n1m1 = new() { Id = 1, NibbleId = 1, MovementId = 101, Order = 1 };
        NibbleMovement n1m2 = new() { Id = 2, NibbleId = 1, MovementId = 102, Order = 2 };

        Nibble n2 = new() { Id = 2, WorkId = 20, RecordingId = 200 };
        NibbleMovement n2m1 = new() { Id = 3, NibbleId = 2, MovementId = 201, Order = 1 };

        service.Enqueue(n1, new List<NibbleMovement> { n1m1, n1m2 });
        service.Enqueue(n2, new List<NibbleMovement> { n2m1 });

        // Start at n1, movement 0 (n1m1)
        Assert.Equal(0, service.CurrentNibbleIndex);
        Assert.Equal(0, service.CurrentMovementIndex);

        // Next -> n1, movement 1 (n1m2)
        bool advanced1 = service.Next();
        Assert.True(advanced1);
        Assert.Equal(0, service.CurrentNibbleIndex);
        Assert.Equal(1, service.CurrentMovementIndex);
        Assert.Equal(n1m2.Id, service.CurrentMovement!.Id);

        // Next -> n2, movement 0 (n2m1)
        bool advanced2 = service.Next();
        Assert.True(advanced2);
        Assert.Equal(1, service.CurrentNibbleIndex);
        Assert.Equal(0, service.CurrentMovementIndex);
        Assert.Equal(n2m1.Id, service.CurrentMovement!.Id);

        // Next at end of queue -> returns false
        bool advanced3 = service.Next();
        Assert.False(advanced3);
        Assert.Equal(1, service.CurrentNibbleIndex);
        Assert.Equal(0, service.CurrentMovementIndex);
    }

    /// <summary>
    /// Verifies that Previous() rewinds through movements and into previous nibble's last movement.
    /// </summary>
    [Fact]
    public void Previous_RewindsMovementsAndNibbles()
    {
        QueueService service = new();

        Nibble n1 = new() { Id = 1, WorkId = 10, RecordingId = 100 };
        NibbleMovement n1m1 = new() { Id = 1, NibbleId = 1, MovementId = 101, Order = 1 };
        NibbleMovement n1m2 = new() { Id = 2, NibbleId = 1, MovementId = 102, Order = 2 };

        Nibble n2 = new() { Id = 2, WorkId = 20, RecordingId = 200 };
        NibbleMovement n2m1 = new() { Id = 3, NibbleId = 2, MovementId = 201, Order = 1 };

        service.Enqueue(n1, new List<NibbleMovement> { n1m1, n1m2 });
        service.Enqueue(n2, new List<NibbleMovement> { n2m1 });

        // Skip to n2
        service.SkipToNibble(1);
        Assert.Equal(1, service.CurrentNibbleIndex);
        Assert.Equal(0, service.CurrentMovementIndex);

        // Previous -> rewinds to n1's last movement (index 1: n1m2)
        bool rewound1 = service.Previous();
        Assert.True(rewound1);
        Assert.Equal(0, service.CurrentNibbleIndex);
        Assert.Equal(1, service.CurrentMovementIndex);
        Assert.Equal(n1m2.Id, service.CurrentMovement!.Id);

        // Previous -> rewinds to n1's first movement (index 0: n1m1)
        bool rewound2 = service.Previous();
        Assert.True(rewound2);
        Assert.Equal(0, service.CurrentNibbleIndex);
        Assert.Equal(0, service.CurrentMovementIndex);
        Assert.Equal(n1m1.Id, service.CurrentMovement!.Id);

        // Previous at start of queue -> returns false
        bool rewound3 = service.Previous();
        Assert.False(rewound3);
    }

    /// <summary>
    /// Verifies that removing an item adjusts current playback indices correctly.
    /// </summary>
    [Fact]
    public void RemoveAt_UpdatesQueueAndAdjustsCurrentIndices()
    {
        QueueService service = new();

        Nibble n1 = new() { Id = 1, WorkId = 10, RecordingId = 100 };
        Nibble n2 = new() { Id = 2, WorkId = 20, RecordingId = 200 };
        Nibble n3 = new() { Id = 3, WorkId = 30, RecordingId = 300 };

        service.Enqueue(n1, new List<NibbleMovement>());
        service.Enqueue(n2, new List<NibbleMovement>());
        service.Enqueue(n3, new List<NibbleMovement>());

        service.SkipToNibble(1); // playing n2 at index 1

        // Remove item at index 0 (n1)
        bool removed = service.RemoveAt(0);
        Assert.True(removed);
        Assert.Equal(2, service.Items.Count);
        Assert.Equal(0, service.CurrentNibbleIndex); // shifted from 1 to 0 (still pointing at n2)
        Assert.Equal(n2.Id, service.CurrentItem!.Nibble.Id);
    }

    /// <summary>
    /// Verifies that the queue state persists across separate service instances resolved from different DI containers.
    /// </summary>
    [Fact]
    public void QueueState_PersistsAcrossDifferentDIContainers()
    {
        // Container 1
        ServiceCollection container1Builder = new();
        container1Builder.AddTransient<IQueueService, QueueService>();
        ServiceProvider container1 = container1Builder.BuildServiceProvider();

        // Container 2
        ServiceCollection container2Builder = new();
        container2Builder.AddTransient<IQueueService, QueueService>();
        ServiceProvider container2 = container2Builder.BuildServiceProvider();

        IQueueService serviceFromContainer1 = container1.GetRequiredService<IQueueService>();
        IQueueService serviceFromContainer2 = container2.GetRequiredService<IQueueService>();

        bool queueChangedFiredOnContainer2 = false;
        serviceFromContainer2.QueueChanged += (sender, args) => queueChangedFiredOnContainer2 = true;

        Nibble nibble = new() { Id = 42, WorkId = 400, RecordingId = 4000 };
        NibbleMovement movement = new() { Id = 1, NibbleId = 42, MovementId = 500, Order = 1 };

        // Enqueue from Container 1
        serviceFromContainer1.Enqueue(nibble, new List<NibbleMovement> { movement });

        // Verify Container 2 reflects the exact same queue item and position
        Assert.Single(serviceFromContainer2.Items);
        Assert.Equal(42, serviceFromContainer2.CurrentItem!.Nibble.Id);
        Assert.True(queueChangedFiredOnContainer2);
    }

    /// <summary>
    /// Verifies that EnqueueNext inserts a item immediately after the currently playing item.
    /// </summary>
    [Fact]
    public void EnqueueNext_InsertsImmediatelyAfterCurrentItem()
    {
        QueueService service = new();

        Nibble n1 = new() { Id = 1, WorkId = 10, RecordingId = 100 };
        Nibble n2 = new() { Id = 2, WorkId = 20, RecordingId = 200 };
        Nibble nNext = new() { Id = 99, WorkId = 990, RecordingId = 9900 };

        service.Enqueue(n1, new List<NibbleMovement>());
        service.Enqueue(n2, new List<NibbleMovement>());

        // Currently playing n1 at index 0
        Assert.Equal(0, service.CurrentNibbleIndex);

        // EnqueueNext nNext
        service.EnqueueNext(nNext, new List<NibbleMovement>());

        // Queue order should be: n1 (index 0), nNext (index 1), n2 (index 2)
        Assert.Equal(3, service.Items.Count);
        Assert.Equal(0, service.CurrentNibbleIndex); // Current playing remains n1 at index 0
        Assert.Equal(nNext.Id, service.Items[1].Nibble.Id);
        Assert.Equal(n2.Id, service.Items[2].Nibble.Id);

        // Next() should advance to nNext
        service.Next();
        Assert.Equal(1, service.CurrentNibbleIndex);
        Assert.Equal(nNext.Id, service.CurrentItem!.Nibble.Id);
    }
}

