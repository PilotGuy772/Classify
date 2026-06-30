using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Classify.Core.Domain;
using Classify.Core.Domain.Infrastructure;
using Classify.Core.Interfaces.Infrastructure;

namespace Classify.Desktop.ViewModels;

/// <summary>
/// Right-side Info Panel content for a selected library performed movement (movement recording).
/// </summary>
public sealed class MovementRecordingInfoPanelViewModel : InfoPanelViewModelBase
{
    private string movementName = string.Empty;
    private string recordingName = string.Empty;
    private string audioFilePath = string.Empty;
    private int parentRecordingId;
    private int parentMovementId;

    /// <summary>
    /// Gets the parent movement name.
    /// </summary>
    public string MovementName
    {
        get => movementName;
        private set
        {
            if (movementName == value) return;
            movementName = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>
    /// Gets the parent recording name.
    /// </summary>
    public string RecordingName
    {
        get => recordingName;
        private set
        {
            if (recordingName == value) return;
            recordingName = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>
    /// Gets the fully qualified path of the associated audio file.
    /// </summary>
    public string AudioFilePath
    {
        get => audioFilePath;
        private set
        {
            if (audioFilePath == value) return;
            audioFilePath = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>
    /// Gets the command to play the parent recording.
    /// </summary>
    public ICommand PlayRecordingCommand { get; }

    /// <summary>
    /// Gets the command to enqueue the parent recording.
    /// </summary>
    public ICommand EnqueueRecordingCommand { get; }

    /// <summary>
    /// Gets the command to show the parent movement's info panel.
    /// </summary>
    public ICommand ShowMovementCommand { get; }

    /// <summary>
    /// Gets the command to show the parent recording's info panel.
    /// </summary>
    public ICommand ShowRecordingCommand { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="MovementRecordingInfoPanelViewModel"/> with direct database access.
    /// </summary>
    /// <param name="unitOfWork">The database unit of work.</param>
    public MovementRecordingInfoPanelViewModel(IUnitOfWork unitOfWork) : base(unitOfWork)
    {
        PlayRecordingCommand = new AsyncRelayCommand(PlayRecordingAsync);
        EnqueueRecordingCommand = new AsyncRelayCommand(EnqueueRecordingAsync);
        ShowMovementCommand = new AsyncRelayCommand(() => OpenInfoPanelAsync(LibraryItemType.Movement, parentMovementId));
        ShowRecordingCommand = new AsyncRelayCommand(() => OpenInfoPanelAsync(LibraryItemType.Recording, parentRecordingId));
    }

    /// <summary>
    /// Loads details of the performed movement, parent movement, parent recording, and audio file path.
    /// </summary>
    /// <param name="performedMovementId">The performed movement identifier.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public override async Task LoadAsync(int performedMovementId)
    {
        PerformedMovement? pm = await unitOfWork.PerformedMovements.GetByIdAsync(performedMovementId);
        if (pm is null)
        {
            Title = string.Empty;
            MovementName = string.Empty;
            RecordingName = string.Empty;
            AudioFilePath = string.Empty;
            parentRecordingId = 0;
            parentMovementId = 0;
            return;
        }

        Title = pm.Name;

        Movement? movement = await unitOfWork.Movements.GetByIdAsync(pm.MovementId);
        MovementName = movement?.Name ?? "—";
        parentMovementId = movement?.Id ?? 0;

        Recording? recording = await unitOfWork.Recordings.GetByIdAsync(pm.RecordingId);
        if (recording is not null)
        {
            parentRecordingId = recording.Id;
            RecordingName = recording.Name;
        }
        else
        {
            parentRecordingId = 0;
            RecordingName = "—";
        }

        AudioFile? audioFile = await unitOfWork.AudioFiles.GetByIdAsync(pm.AudioFileId);
        AudioFilePath = audioFile?.Path ?? "—";
    }

    private Task PlayRecordingAsync()
    {
        return Task.CompletedTask;
    }

    private Task EnqueueRecordingAsync()
    {
        return Task.CompletedTask;
    }
}
