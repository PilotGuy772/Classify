using System.Collections.Generic;
using System.Threading.Tasks;
using Classify.Core.Domain;
using Classify.Data;
using Classify.Data.Context;
using Classify.Desktop.ViewModels;
using Classify.Services.Queue;
using Xunit;

namespace Tests;

/// <summary>
/// Unit tests for <see cref="PlayerWindowViewModel"/> verifying queue synchronization, metadata resolution, and selection handling.
/// </summary>
public class PlayerWindowIntegrationTests
{
    /// <summary>
    /// Verifies that PlayerWindowViewModel reflects queued nibbles, active movement highlighting, and metadata resolution.
    /// </summary>
    [Fact]
    public async Task RefreshAsync_UpdatesQueueItemsAndActiveMovementHighlight()
    {
        using ClassifyContext context = SqliteInMemory.CreateDbContext();
        UnitOfWork uow = new UnitOfWork(new DbContextFactoryMock(context));

        Composer composer = new Composer { Name = "Ludwig van Beethoven" };
        await uow.Composers.AddAsync(composer);
        await uow.SaveChangesAsync();

        Work work = new Work { ComposerId = composer.Id, Name = "Symphony No. 7", CatalogNumber = "Op. 92" };
        await uow.Works.AddAsync(work);
        await uow.SaveChangesAsync();

        Recording recording = new Recording { WorkId = work.Id, Conductor = "Carlos Kleiber", Ensemble = "Bayerisches Staatsorchester", Year = 1982 };
        await uow.Recordings.AddAsync(recording);
        await uow.SaveChangesAsync();

        Movement m1 = new Movement { WorkId = work.Id, Name = "Poco sostenuto - Vivace", Order = 1 };
        Movement m2 = new Movement { WorkId = work.Id, Name = "Allegretto", Order = 2 };
        await uow.Movements.AddAsync(m1);
        await uow.Movements.AddAsync(m2);
        await uow.SaveChangesAsync();

        QueueService queueService = new QueueService();
        queueService.Clear();

        PlayerWindowViewModel vm = new PlayerWindowViewModel(queueService, uow);

        Nibble nibble = new Nibble { Id = 1, WorkId = work.Id, RecordingId = recording.Id };
        NibbleMovement nm1 = new NibbleMovement { Id = 1, NibbleId = 1, MovementId = m1.Id, Order = 1 };
        NibbleMovement nm2 = new NibbleMovement { Id = 2, NibbleId = 1, MovementId = m2.Id, Order = 2 };

        queueService.Enqueue(nibble, new List<NibbleMovement> { nm1, nm2 });
        await vm.RefreshAsync();

        Assert.False(vm.IsEmptyQueue);
        Assert.Single(vm.QueueItems);
        Assert.Equal("Symphony No. 7", vm.CurrentWorkTitle);
        Assert.Equal("Ludwig van Beethoven", vm.CurrentComposerName);
        Assert.Equal("Carlos Kleiber, Bayerisches Staatsorchester", vm.CurrentPerformersLine);
        Assert.Equal("I.", vm.CurrentMovementOrdinal);
        Assert.Equal("Poco sostenuto - Vivace", vm.CurrentMovementName);

        // Movement 0 should be highlighted active
        PlayerQueueItemViewModel firstNibbleVm = vm.QueueItems[0];
        Assert.Equal(2, firstNibbleVm.Movements.Count);
        Assert.True(firstNibbleVm.Movements[0].IsCurrentlyPlaying);
        Assert.False(firstNibbleVm.Movements[1].IsCurrentlyPlaying);

        // Advance playback to movement 1
        queueService.Next();
        await vm.RefreshAsync();

        Assert.Equal("II.", vm.CurrentMovementOrdinal);
        Assert.Equal("Allegretto", vm.CurrentMovementName);
        Assert.False(vm.QueueItems[0].Movements[0].IsCurrentlyPlaying);
        Assert.True(vm.QueueItems[0].Movements[1].IsCurrentlyPlaying);
    }

    /// <summary>
    /// Verifies that selecting a nibble or movement in PlayerWindowViewModel updates the current playback position in QueueService.
    /// </summary>
    [Fact]
    public async Task SelectingMovement_UpdatesQueueServiceCurrentPosition()
    {
        using ClassifyContext context = SqliteInMemory.CreateDbContext();
        UnitOfWork uow = new UnitOfWork(new DbContextFactoryMock(context));

        Composer composer = new Composer { Name = "Mozart" };
        await uow.Composers.AddAsync(composer);
        await uow.SaveChangesAsync();

        Work work = new Work { ComposerId = composer.Id, Name = "Work 1" };
        await uow.Works.AddAsync(work);
        await uow.SaveChangesAsync();

        Recording recording = new Recording { WorkId = work.Id, Conductor = "Rec 1" };
        await uow.Recordings.AddAsync(recording);
        await uow.SaveChangesAsync();

        Movement m1 = new Movement { WorkId = work.Id, Name = "Mv 1", Order = 1 };
        Movement m2 = new Movement { WorkId = work.Id, Name = "Mv 2", Order = 2 };
        await uow.Movements.AddAsync(m1);
        await uow.Movements.AddAsync(m2);
        await uow.SaveChangesAsync();

        QueueService queueService = new QueueService();
        queueService.Clear();

        PlayerWindowViewModel vm = new PlayerWindowViewModel(queueService, uow);

        Nibble nibble = new Nibble { Id = 1, WorkId = work.Id, RecordingId = recording.Id };
        NibbleMovement nm1 = new NibbleMovement { Id = 1, NibbleId = 1, MovementId = m1.Id, Order = 1 };
        NibbleMovement nm2 = new NibbleMovement { Id = 2, NibbleId = 1, MovementId = m2.Id, Order = 2 };

        queueService.Enqueue(nibble, new List<NibbleMovement> { nm1, nm2 });
        await vm.RefreshAsync();

        // Select movement 1 via command
        PlayerMovementItemViewModel targetMovementVm = vm.QueueItems[0].Movements[1];
        targetMovementVm.SelectMovementCommand.Execute(null);

        Assert.Equal(0, queueService.CurrentNibbleIndex);
        Assert.Equal(1, queueService.CurrentMovementIndex);
    }

    /// <summary>
    /// Mock IDbContextFactory for UnitOfWork testing.
    /// </summary>
    private sealed class DbContextFactoryMock(ClassifyContext context) : Microsoft.EntityFrameworkCore.IDbContextFactory<ClassifyContext>
    {
        public ClassifyContext CreateDbContext() => context;
    }
}
