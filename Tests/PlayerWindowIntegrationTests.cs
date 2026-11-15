using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Classify.Core.Domain;
using Classify.Core.Interfaces.Infrastructure;
using Classify.Data;
using Classify.Data.Context;
using Classify.Core.Interfaces.Service;
using Classify.Desktop.ViewModels;
using Classify.Services.Queue;
using Moq;
using Xunit;

namespace Tests;

/// <summary>
/// Unit tests for <see cref="PlayerWindowViewModel"/> verifying queue synchronization, metadata resolution, and selection handling.
/// </summary>
[Collection("QueueService")]
public class PlayerWindowIntegrationTests
{
    /// <summary>
    /// Verifies that PlayerWindowViewModel reflects queued nibbles, active movement highlighting, and metadata resolution.
    /// </summary>
    [Fact]
    public async Task RefreshAsync_UpdatesQueueItemsAndActiveMovementHighlight()
    {
        using ClassifyContext context = SqliteInMemory.CreateDbContext();
        UnitOfWork uow = new(new DbContextFactoryMock(context));

        Composer composer = new() { Name = "Ludwig van Beethoven" };
        await uow.Composers.AddAsync(composer);
        await uow.SaveChangesAsync();

        Work work = new() { ComposerId = composer.Id, Name = "Symphony No. 7", CatalogNumber = "Op. 92" };
        await uow.Works.AddAsync(work);
        await uow.SaveChangesAsync();

        Recording recording = new() { WorkId = work.Id, Conductor = "Carlos Kleiber", Ensemble = "Bayerisches Staatsorchester", Year = 1982 };
        await uow.Recordings.AddAsync(recording);
        await uow.SaveChangesAsync();

        Movement m1 = new() { WorkId = work.Id, Name = "Poco sostenuto - Vivace", Order = 1 };
        Movement m2 = new() { WorkId = work.Id, Name = "Allegretto", Order = 2 };
        await uow.Movements.AddAsync(m1);
        await uow.Movements.AddAsync(m2);
        await uow.SaveChangesAsync();

        QueueService queueService = new();
        queueService.Clear();

        PlayerWindowViewModel vm = new(queueService, uow, new Mock<IPlaybackService>().Object);

        Nibble nibble = new() { Id = 1, WorkId = work.Id, RecordingId = recording.Id };
        NibbleMovement nm1 = new() { Id = 1, NibbleId = 1, MovementId = m1.Id, Order = 1 };
        NibbleMovement nm2 = new() { Id = 2, NibbleId = 1, MovementId = m2.Id, Order = 2 };

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
        UnitOfWork uow = new(new DbContextFactoryMock(context));

        Composer composer = new() { Name = "Mozart" };
        await uow.Composers.AddAsync(composer);
        await uow.SaveChangesAsync();

        Work work = new() { ComposerId = composer.Id, Name = "Work 1" };
        await uow.Works.AddAsync(work);
        await uow.SaveChangesAsync();

        Recording recording = new() { WorkId = work.Id, Conductor = "Rec 1" };
        await uow.Recordings.AddAsync(recording);
        await uow.SaveChangesAsync();

        Movement m1 = new() { WorkId = work.Id, Name = "Mv 1", Order = 1 };
        Movement m2 = new() { WorkId = work.Id, Name = "Mv 2", Order = 2 };
        await uow.Movements.AddAsync(m1);
        await uow.Movements.AddAsync(m2);
        await uow.SaveChangesAsync();

        QueueService queueService = new();
        queueService.Clear();

        Mock<IPlaybackService> playback = new();
        playback.Setup(service => service.PlayAtAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback((int nibbleIndex, int movementIndex, CancellationToken _) => queueService.SkipToNibble(nibbleIndex, movementIndex))
            .Returns(Task.CompletedTask);
        PlayerWindowViewModel vm = new(queueService, uow, playback.Object);

        Nibble nibble = new() { Id = 1, WorkId = work.Id, RecordingId = recording.Id };
        NibbleMovement nm1 = new() { Id = 1, NibbleId = 1, MovementId = m1.Id, Order = 1 };
        NibbleMovement nm2 = new() { Id = 2, NibbleId = 1, MovementId = m2.Id, Order = 2 };

        queueService.Enqueue(nibble, new List<NibbleMovement> { nm1, nm2 });
        await vm.RefreshAsync();

        // Select movement 1 via command
        PlayerMovementItemViewModel targetMovementVm = vm.QueueItems[0].Movements[1];
        targetMovementVm.SelectMovementCommand.Execute(null);

        Assert.Equal(0, queueService.CurrentNibbleIndex);
        Assert.Equal(1, queueService.CurrentMovementIndex);
    }

    /// <summary>
    /// Verifies that when a single-movement nibble is enqueued, the movement's overall work order is used for Roman numeral formatting.
    /// </summary>
    [Fact]
    public async Task RefreshAsync_SingleMovementNibble_UsesOverallMovementOrderForRomanOrdinal()
    {
        using ClassifyContext context = SqliteInMemory.CreateDbContext();
        UnitOfWork uow = new(new DbContextFactoryMock(context));

        Composer composer = new() { Name = "Mahler" };
        await uow.Composers.AddAsync(composer);
        await uow.SaveChangesAsync();

        Work work = new() { ComposerId = composer.Id, Name = "Symphony No. 5" };
        await uow.Works.AddAsync(work);
        await uow.SaveChangesAsync();

        Recording recording = new() { WorkId = work.Id, Conductor = "Bernstein" };
        await uow.Recordings.AddAsync(recording);
        await uow.SaveChangesAsync();

        Movement m3 = new() { WorkId = work.Id, Name = "Scherzo", Order = 3 };
        await uow.Movements.AddAsync(m3);
        await uow.SaveChangesAsync();

        NibbleBuilderService builder = new(uow);
        QueueItem? item = await builder.BuildForMovementAsync(m3.Id, recording.Id);
        Assert.NotNull(item);

        QueueService queueService = new();
        queueService.Clear();

        PlayerWindowViewModel vm = new(queueService, uow, new Mock<IPlaybackService>().Object);
        queueService.Enqueue(item.Nibble, item.Movements);
        await vm.RefreshAsync();

        Assert.Equal("III.", vm.CurrentMovementOrdinal);
        Assert.Equal("Scherzo", vm.CurrentMovementName);
        Assert.Equal("III.", vm.QueueItems[0].Movements[0].OrdinalLabel);
    }

    /// <summary>
    /// Verifies that a progress-bar drag pauses playback, previews the pointer time, and seeks and resumes on release.
    /// </summary>
    [Fact]
    public void SeekDrag_PausesUntilReleaseThenSeeksAndResumes()
    {
        List<string> calls = new();
        bool playing = true;
        Mock<IPlaybackService> playback = new();
        playback.SetupGet(service => service.IsPlaying).Returns(() => playing);
        playback.SetupGet(service => service.Position).Returns(TimeSpan.FromSeconds(10));
        playback.SetupGet(service => service.Duration).Returns(TimeSpan.FromSeconds(200));
        playback.Setup(service => service.Pause()).Callback(() =>
        {
            playing = false;
            calls.Add("pause");
        });
        playback.Setup(service => service.SeekToPercentage(It.IsAny<float>())).Callback(() => calls.Add("seek"));
        playback.Setup(service => service.PlayAsync(It.IsAny<CancellationToken>()))
            .Callback(() =>
            {
                playing = true;
                calls.Add("play");
            })
            .Returns(Task.CompletedTask);

        using PlayerWindowViewModel viewModel = CreateSeekViewModel(playback.Object);
        viewModel.BeginSeekDrag(0.5d);
        viewModel.HideSeekHint();
        viewModel.UpdateSeekDrag(0.25d);

        Assert.Equal(25d, viewModel.PlaybackProgress);
        Assert.Equal("0:50", viewModel.CurrentPositionText);
        Assert.Equal("0:50", viewModel.SeekHintText);
        Assert.True(viewModel.IsSeekHintVisible);
        Assert.Equal(new[] { "pause" }, calls);

        viewModel.EndSeekDrag(0.25d, keepHintVisible: true);

        playback.Verify(service => service.SeekToPercentage(0.25f), Times.Once);
        Assert.Equal(new[] { "pause", "seek", "play" }, calls);
        Assert.Equal(25d, viewModel.PlaybackProgress);
        Assert.Equal("0:50", viewModel.CurrentPositionText);
        Assert.True(viewModel.IsSeekHintVisible);
        Assert.True(playing);
    }

    /// <summary>
    /// Verifies that dragging while playback is already paused seeks without starting playback.
    /// </summary>
    [Fact]
    public void SeekDrag_WhenPaused_SeeksWithoutPlaying()
    {
        Mock<IPlaybackService> playback = new();
        playback.SetupGet(service => service.IsPlaying).Returns(false);
        playback.SetupGet(service => service.Position).Returns(TimeSpan.FromSeconds(10));
        playback.SetupGet(service => service.Duration).Returns(TimeSpan.FromSeconds(200));
        playback.Setup(service => service.PlayAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        using PlayerWindowViewModel viewModel = CreateSeekViewModel(playback.Object);
        viewModel.EndSeekDrag(0.5d, keepHintVisible: false);
        viewModel.BeginSeekDrag(1.4d);
        viewModel.EndSeekDrag(-0.2d, keepHintVisible: false);

        playback.Verify(service => service.Pause(), Times.Never);
        playback.Verify(service => service.PlayAsync(It.IsAny<CancellationToken>()), Times.Never);
        playback.Verify(service => service.SeekToPercentage(0f), Times.Once);
        Assert.Equal(0d, viewModel.PlaybackProgress);
        Assert.False(viewModel.IsSeekHintVisible);
    }

    /// <summary>
    /// Verifies that the hover hint shows the time under the pointer without moving the progress bar.
    /// </summary>
    [Fact]
    public void ShowSeekHint_ShowsTimeWithoutChangingProgress()
    {
        Mock<IPlaybackService> playback = new();
        playback.SetupGet(service => service.Position).Returns(TimeSpan.FromSeconds(10));
        playback.SetupGet(service => service.Duration).Returns(TimeSpan.FromSeconds(200));

        using PlayerWindowViewModel viewModel = CreateSeekViewModel(playback.Object);
        double progress = viewModel.PlaybackProgress;

        viewModel.ShowSeekHint(0.5d);

        Assert.Equal("1:40", viewModel.SeekHintText);
        Assert.True(viewModel.IsSeekHintVisible);
        Assert.Equal(progress, viewModel.PlaybackProgress);
        Assert.Equal("0:10", viewModel.CurrentPositionText);

        viewModel.HideSeekHint();

        Assert.False(viewModel.IsSeekHintVisible);
        playback.Verify(service => service.SeekToPercentage(It.IsAny<float>()), Times.Never);
    }

    /// <summary>
    /// Builds a player view model whose queue is empty so refresh does not touch the database.
    /// </summary>
    private static PlayerWindowViewModel CreateSeekViewModel(IPlaybackService playback)
    {
        QueueService queueService = new();
        queueService.Clear();
        return new PlayerWindowViewModel(queueService, new Mock<IUnitOfWork>().Object, playback);
    }

    /// <summary>
    /// Mock IDbContextFactory for UnitOfWork testing.
    /// </summary>
    private sealed class DbContextFactoryMock(ClassifyContext context) : Microsoft.EntityFrameworkCore.IDbContextFactory<ClassifyContext>
    {
        public ClassifyContext CreateDbContext() => context;
    }
}
