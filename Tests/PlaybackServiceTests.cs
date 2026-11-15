using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Classify.Core.Domain;
using Classify.Core.Enums;
using Classify.Core.Interfaces.Infrastructure;
using Classify.Core.Interfaces.Service;
using Classify.Services.Playback;
using Classify.Services.Queue;
using Microsoft.Extensions.DependencyInjection;

namespace Tests;

/// <summary>
/// Queue state is process-wide, so tests that use <see cref="QueueService"/> run one at a time.
/// </summary>
[CollectionDefinition("QueueService", DisableParallelization = true)]
public sealed class QueueServiceCollection;

/// <summary>
/// Verifies queue movement order and audio-file loading in <see cref="PlaybackService"/>.
/// </summary>
[Collection("QueueService")]
public class PlaybackServiceTests
{
    /// <summary>
    /// Verifies that play does nothing when the queue has no current movement.
    /// </summary>
    [Fact]
    public async Task PlayAsync_WithoutCurrentMovement_DoesNothing()
    {
        await using PlaybackFixture fixture = await PlaybackFixture.CreateAsync();
        QueueService queue = new();
        queue.Clear();
        FakeAudioPlaybackService audio = new();
        PlaybackService playback = fixture.CreatePlayback(queue, audio);

        await playback.PlayAsync();

        Assert.Empty(audio.Actions);
    }

    /// <summary>
    /// Verifies that play loads the current movement once, then resumes the loaded file.
    /// </summary>
    [Fact]
    public async Task PlayAsync_LoadsCurrentMovementOnce()
    {
        await using PlaybackFixture fixture = await PlaybackFixture.CreateAsync();
        FakeAudioPlaybackService audio = new();
        PlaybackService playback = fixture.CreatePlayback(fixture.Queue, audio);

        await playback.PlayAsync();
        await playback.PlayAsync();

        Assert.Equal(new[] { "load:" + fixture.FirstPath, "play", "play" }, audio.Actions);
        Assert.True(audio.HasMedia);
    }

    /// <summary>
    /// Verifies that pause leaves the file loaded and stop unloads it.
    /// </summary>
    [Fact]
    public async Task Pause_LeavesFileLoaded_AndStopUnloadsIt()
    {
        await using PlaybackFixture fixture = await PlaybackFixture.CreateAsync();
        FakeAudioPlaybackService audio = new();
        PlaybackService playback = fixture.CreatePlayback(fixture.Queue, audio);
        await playback.PlayAsync();
        audio.Actions.Clear();

        playback.Pause();
        Assert.True(audio.HasMedia);
        Assert.Equal(new[] { "pause" }, audio.Actions);

        playback.Stop();
        Assert.False(audio.HasMedia);
        Assert.Equal(new[] { "pause", "stop" }, audio.Actions);
    }

    /// <summary>
    /// Verifies that skip forward plays the next movement in the nibble, skipping movements with no audio file,
    /// and then the first movement of the next nibble.
    /// </summary>
    [Fact]
    public async Task SkipForwardAsync_AdvancesWithinNibbleThenToNextNibble()
    {
        await using PlaybackFixture fixture = await PlaybackFixture.CreateAsync();
        FakeAudioPlaybackService audio = new();
        PlaybackService playback = fixture.CreatePlayback(fixture.Queue, audio);
        await playback.PlayAsync();
        audio.Actions.Clear();

        await playback.SkipForwardAsync();

        Assert.Equal(0, fixture.Queue.CurrentNibbleIndex);
        Assert.Equal(2, fixture.Queue.CurrentMovementIndex);
        Assert.Equal(new[] { "stop", "load:" + fixture.SecondPath, "play" }, audio.Actions);

        await playback.SkipForwardAsync();

        Assert.Equal(1, fixture.Queue.CurrentNibbleIndex);
        Assert.Equal(0, fixture.Queue.CurrentMovementIndex);
        Assert.EndsWith(fixture.ThirdPath, audio.LoadedPath);
    }

    /// <summary>
    /// Verifies that choosing a queue movement unloads the current file and plays the selected one.
    /// </summary>
    [Fact]
    public async Task PlayAtAsync_PlaysTheSelectedMovement()
    {
        await using PlaybackFixture fixture = await PlaybackFixture.CreateAsync();
        FakeAudioPlaybackService audio = new();
        PlaybackService playback = fixture.CreatePlayback(fixture.Queue, audio);
        await playback.PlayAsync();
        audio.Actions.Clear();

        await playback.PlayAtAsync(0, 2);

        Assert.Equal(0, fixture.Queue.CurrentNibbleIndex);
        Assert.Equal(2, fixture.Queue.CurrentMovementIndex);
        Assert.Equal(new[] { "stop", "load:" + fixture.SecondPath, "play" }, audio.Actions);

        audio.Actions.Clear();
        await playback.PlayAtAsync(1, 0);

        Assert.Equal(1, fixture.Queue.CurrentNibbleIndex);
        Assert.Equal(0, fixture.Queue.CurrentMovementIndex);
        Assert.Equal(fixture.ThirdPath, audio.LoadedPath);
        Assert.Equal(new[] { "stop", "load:" + fixture.ThirdPath, "play" }, audio.Actions);
    }

    /// <summary>
    /// Verifies that reaching the end of a file skips to the next playable movement, and stops on the last one.
    /// </summary>
    [Fact]
    public async Task PlaybackCompleted_SkipsForwardOrStopsAtEnd()
    {
        await using PlaybackFixture fixture = await PlaybackFixture.CreateAsync();
        FakeAudioPlaybackService audio = new();
        PlaybackService playback = fixture.CreatePlayback(fixture.Queue, audio);
        await playback.PlayAsync();
        audio.Actions.Clear();

        audio.CompletePlayback();

        Assert.Equal(0, fixture.Queue.CurrentNibbleIndex);
        Assert.Equal(2, fixture.Queue.CurrentMovementIndex);
        Assert.Equal(fixture.SecondPath, audio.LoadedPath);
        Assert.Equal(new[] { "stop", "load:" + fixture.SecondPath, "play" }, audio.Actions);

        audio.CompletePlayback();
        audio.Actions.Clear();
        audio.CompletePlayback();

        Assert.Equal(1, fixture.Queue.CurrentNibbleIndex);
        Assert.Equal(0, fixture.Queue.CurrentMovementIndex);
        Assert.False(audio.HasMedia);
        Assert.Equal(new[] { "stop" }, audio.Actions);
    }

    /// <summary>
    /// Verifies that skip back restarts the current file after five seconds, and plays the previous file before that.
    /// </summary>
    [Fact]
    public async Task SkipBackwardAsync_RestartsOrMovesToPreviousMovement()
    {
        await using PlaybackFixture fixture = await PlaybackFixture.CreateAsync();
        FakeAudioPlaybackService audio = new();
        PlaybackService playback = fixture.CreatePlayback(fixture.Queue, audio);
        await playback.PlayAsync();
        await playback.SkipForwardAsync();
        audio.Actions.Clear();
        audio.Position = TimeSpan.FromSeconds(10);

        await playback.SkipBackwardAsync();

        Assert.Equal(0, fixture.Queue.CurrentNibbleIndex);
        Assert.Equal(2, fixture.Queue.CurrentMovementIndex);
        Assert.Equal(new[] { "seek:0" }, audio.Actions);

        audio.Position = TimeSpan.FromSeconds(2);
        await playback.SkipBackwardAsync();

        Assert.Equal(0, fixture.Queue.CurrentNibbleIndex);
        Assert.Equal(0, fixture.Queue.CurrentMovementIndex);
        Assert.Equal(fixture.FirstPath, audio.LoadedPath);
        Assert.Contains("play", audio.Actions);
    }

    /// <summary>
    /// Verifies that a player state change is forwarded and <see cref="PlaybackService.IsPlaying"/> follows it.
    /// </summary>
    [Fact]
    public async Task PlaybackStateChanged_ForwardsWhetherAudioIsPlaying()
    {
        await using PlaybackFixture fixture = await PlaybackFixture.CreateAsync();
        FakeAudioPlaybackService audio = new();
        PlaybackService playback = fixture.CreatePlayback(fixture.Queue, audio);
        int notifications = 0;
        playback.PlaybackStateChanged += (object? sender, EventArgs e) => notifications++;

        await playback.PlayAsync();

        Assert.True(playback.IsPlaying);
        Assert.Equal(1, notifications);

        playback.Pause();

        Assert.False(playback.IsPlaying);
        Assert.Equal(2, notifications);
    }

    /// <summary>
    /// Verifies that a position change is forwarded and the service reports the player's position and duration.
    /// </summary>
    [Fact]
    public async Task PlaybackPositionChanged_ForwardsPlayerPosition()
    {
        await using PlaybackFixture fixture = await PlaybackFixture.CreateAsync();
        FakeAudioPlaybackService audio = new()
        {
            Duration = TimeSpan.FromSeconds(90),
        };
        PlaybackService playback = fixture.CreatePlayback(fixture.Queue, audio);
        int notifications = 0;
        playback.PlaybackPositionChanged += (object? sender, EventArgs e) => notifications++;
        await playback.PlayAsync();

        playback.Seek(12);

        Assert.Equal(TimeSpan.FromSeconds(12), playback.Position);
        Assert.Equal(TimeSpan.FromSeconds(90), playback.Duration);
        Assert.Equal(1, notifications);
    }

    /// <summary>
    /// Verifies that seeking by seconds and by percentage uses the loaded file duration.
    /// </summary>
    [Fact]
    public async Task Seek_ChangesPositionWithinDuration()
    {
        await using PlaybackFixture fixture = await PlaybackFixture.CreateAsync();
        FakeAudioPlaybackService audio = new()
        {
            Duration = TimeSpan.FromSeconds(30),
        };
        PlaybackService playback = fixture.CreatePlayback(fixture.Queue, audio);
        await playback.PlayAsync();
        audio.Position = TimeSpan.FromSeconds(10);

        playback.Seek(15);
        Assert.Equal(TimeSpan.FromSeconds(25), audio.Position);

        playback.Seek(-100);
        Assert.Equal(TimeSpan.Zero, audio.Position);

        audio.Position = TimeSpan.FromSeconds(10);
        playback.Seek(1000);
        Assert.Equal(TimeSpan.FromSeconds(30), audio.Position);

        playback.SeekToPercentage(0.5f);
        Assert.Equal(TimeSpan.FromSeconds(15), audio.Position);
        Assert.Throws<ArgumentOutOfRangeException>(() => playback.SeekToPercentage(1.5f));
    }

    /// <summary>
    /// Builds a two-nibble queue whose middle movement has no audio file.
    /// </summary>
    private sealed class PlaybackFixture : IAsyncDisposable
    {
        private readonly string _libraryPath;
        private readonly IServiceProvider _services;
        private readonly IAppSettings _appSettings;

        private PlaybackFixture(string libraryPath, IServiceProvider services, IAppSettings appSettings, QueueService queue, string firstPath, string secondPath, string thirdPath)
        {
            _libraryPath = libraryPath;
            _services = services;
            _appSettings = appSettings;
            Queue = queue;
            FirstPath = firstPath;
            SecondPath = secondPath;
            ThirdPath = thirdPath;
        }

        public QueueService Queue { get; }

        public string FirstPath { get; }

        public string SecondPath { get; }

        public string ThirdPath { get; }

        public static async Task<PlaybackFixture> CreateAsync()
        {
            string libraryPath = Directory.CreateTempSubdirectory("classify-playback").FullName;
            string firstPath = Path.Combine(libraryPath, "one.flac");
            string secondPath = Path.Combine(libraryPath, "two.flac");
            string thirdPath = Path.Combine(libraryPath, "three.flac");
            await File.WriteAllBytesAsync(firstPath, [0]);
            await File.WriteAllBytesAsync(secondPath, [0]);
            await File.WriteAllBytesAsync(thirdPath, [0]);

            IServiceProvider services = SqliteInMemory.BuildTestServices();
            FakeAppSettings appSettings = new()
            {
                CurrentLibraryPath = libraryPath,
            };

            using IUnitOfWork unitOfWork = services.GetRequiredService<IUnitOfWork>();
            Composer composer = new() { Name = "Composer" };
            await unitOfWork.Composers.AddAsync(composer);
            await unitOfWork.SaveChangesAsync();

            Work work = new() { ComposerId = composer.Id, Name = "Work" };
            await unitOfWork.Works.AddAsync(work);
            await unitOfWork.SaveChangesAsync();

            Recording recording = new() { WorkId = work.Id, Conductor = "Conductor" };
            await unitOfWork.Recordings.AddAsync(recording);
            await unitOfWork.SaveChangesAsync();

            Movement firstMovement = new() { WorkId = work.Id, Name = "First", Order = 1 };
            Movement missingMovement = new() { WorkId = work.Id, Name = "Missing", Order = 2 };
            Movement secondMovement = new() { WorkId = work.Id, Name = "Second", Order = 3 };
            Movement thirdMovement = new() { WorkId = work.Id, Name = "Third", Order = 1 };
            await unitOfWork.Movements.AddAsync(firstMovement);
            await unitOfWork.Movements.AddAsync(missingMovement);
            await unitOfWork.Movements.AddAsync(secondMovement);
            await unitOfWork.Movements.AddAsync(thirdMovement);
            await unitOfWork.SaveChangesAsync();

            AudioFile firstFile = new() { Path = "one.flac", Status = IngestionStatus.Complete };
            AudioFile secondFile = new() { Path = "two.flac", Status = IngestionStatus.Complete };
            AudioFile thirdFile = new() { Path = "three.flac", Status = IngestionStatus.Complete };
            await unitOfWork.AudioFiles.AddAsync(firstFile);
            await unitOfWork.AudioFiles.AddAsync(secondFile);
            await unitOfWork.AudioFiles.AddAsync(thirdFile);
            await unitOfWork.SaveChangesAsync();

            await unitOfWork.PerformedMovements.AddAsync(new PerformedMovement
            {
                RecordingId = recording.Id,
                MovementId = firstMovement.Id,
                AudioFileId = firstFile.Id,
                Order = 1,
            });
            await unitOfWork.PerformedMovements.AddAsync(new PerformedMovement
            {
                RecordingId = recording.Id,
                MovementId = secondMovement.Id,
                AudioFileId = secondFile.Id,
                Order = 3,
            });
            await unitOfWork.PerformedMovements.AddAsync(new PerformedMovement
            {
                RecordingId = recording.Id,
                MovementId = thirdMovement.Id,
                AudioFileId = thirdFile.Id,
                Order = 1,
            });
            await unitOfWork.SaveChangesAsync();

            Nibble firstNibble = new() { RecordingId = recording.Id };
            Nibble secondNibble = new() { RecordingId = recording.Id };
            QueueService queue = new();
            queue.Clear();
            queue.Enqueue(firstNibble,
            [
                new NibbleMovement { MovementId = firstMovement.Id, Order = 1 },
                new NibbleMovement { MovementId = missingMovement.Id, Order = 2 },
                new NibbleMovement { MovementId = secondMovement.Id, Order = 3 },
            ]);
            queue.Enqueue(secondNibble,
            [
                new NibbleMovement { MovementId = thirdMovement.Id, Order = 1 },
            ]);

            return new PlaybackFixture(libraryPath, services, appSettings, queue, firstPath, secondPath, thirdPath);
        }

        public PlaybackService CreatePlayback(IQueueService queue, IAudioPlaybackService audio)
        {
            return new PlaybackService(
                queue,
                audio,
                _appSettings,
                () => _services.GetRequiredService<IUnitOfWork>());
        }

        public ValueTask DisposeAsync()
        {
            if (_services is IDisposable disposable)
            {
                disposable.Dispose();
            }

            if (Directory.Exists(_libraryPath))
            {
                Directory.Delete(_libraryPath, true);
            }

            return ValueTask.CompletedTask;
        }
    }

    /// <summary>
    /// Supplies the library directory used to resolve audio files.
    /// </summary>
    private sealed class FakeAppSettings : IAppSettings
    {
        public string CurrentLibraryPath { get; set; } = string.Empty;

        public string DatabasePath { get; set; } = string.Empty;

        public AppAppearance Appearance { get; set; }

        public event EventHandler<string>? DatabasePathChanged
        {
            add { }
            remove { }
        }

        public string AcceptedFileExtensions { get; set; } = ".flac";
    }

    /// <summary>
    /// Records player calls without loading LibVLC.
    /// </summary>
    private sealed class FakeAudioPlaybackService : IAudioPlaybackService
    {
        public List<string> Actions { get; } = [];

        public event EventHandler? PlaybackStateChanged;

        public event EventHandler? PlaybackCompleted;

        /// <summary>
        /// Raises <see cref="PlaybackCompleted"/> as the audio player does at the end of a file.
        /// </summary>
        public void CompletePlayback()
        {
            PlaybackCompleted?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler? PlaybackPositionChanged;

        public bool IsPlaying { get; private set; }

        public bool HasMedia { get; private set; }

        public TimeSpan Position { get; set; }

        public TimeSpan Duration { get; set; } = TimeSpan.FromMinutes(3);

        public string? LoadedPath { get; private set; }

        public Task<bool> IsPlayableAudioFileAsync(string path, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(File.Exists(path));
        }

        public Task<AudioPlaybackInfo?> GetAudioFileInfoAsync(string path, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<AudioPlaybackInfo?>(null);
        }

        public void Load(string path)
        {
            LoadedPath = path;
            HasMedia = true;
            Position = TimeSpan.Zero;
            Actions.Add("load:" + path);
        }

        public void Play()
        {
            if (!HasMedia)
            {
                throw new InvalidOperationException("No audio file is loaded.");
            }

            Actions.Add("play");
            SetPlaying(true);
        }

        public void Seek(TimeSpan position)
        {
            Position = position;
            Actions.Add("seek:" + position.TotalSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture));
            PlaybackPositionChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Pause()
        {
            Actions.Add("pause");
            SetPlaying(false);
        }

        public void Stop()
        {
            HasMedia = false;
            LoadedPath = null;
            Position = TimeSpan.Zero;
            Actions.Add("stop");
            SetPlaying(false);
        }

        /// <summary>
        /// Updates the playing flag and raises <see cref="PlaybackStateChanged"/> when it changes.
        /// </summary>
        private void SetPlaying(bool isPlaying)
        {
            if (IsPlaying == isPlaying)
            {
                return;
            }

            IsPlaying = isPlaying;
            PlaybackStateChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
