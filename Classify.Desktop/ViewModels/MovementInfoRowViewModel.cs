using System.Windows.Input;
using Classify.Core.Domain.Infrastructure;

namespace Classify.Desktop.ViewModels;

/// <summary>
/// One row under the Info Panel "Movements" section.
/// </summary>
public sealed class MovementInfoRowViewModel : ViewModelBase
{
    /// <summary>
    /// Ordinal label shown before the movement name (e.g. "I.", "II.").
    /// </summary>
    public string OrdinalLabel { get; }

    /// <summary>
    /// Display name for the movement.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Domain movement id used by row-level commands.
    /// </summary>
    public int MovementId { get; }

    /// <summary>
    /// Row-level play control (wired to panel stubs).
    /// </summary>
    public ICommand PlayMovementRowCommand { get; }

    /// <summary>
    /// Row-level enqueue control (wired to panel stubs).
    /// </summary>
    public ICommand EnqueueMovementRowCommand { get; }

    /// <summary>
    /// Gets the command to show this movement's info panel.
    /// </summary>
    public ICommand ShowMovementCommand { get; }

    /// <summary>
    /// Creates a movements section row binding model with commands targeting the owning panel.
    /// </summary>
    public MovementInfoRowViewModel(string ordinalLabel, string name, int movementId, WorkInfoPanelViewModel panel)
    {
        OrdinalLabel = ordinalLabel;
        Name = name;
        MovementId = movementId;
        PlayMovementRowCommand = new AsyncRelayCommand(() => panel.PlayMovementStubAsync(this));
        EnqueueMovementRowCommand = new AsyncRelayCommand(() => panel.EnqueueMovementStubAsync(this));
        ShowMovementCommand = new AsyncRelayCommand(() => panel.OpenInfoPanelAsync(LibraryItemType.Movement, movementId));

        MenuOptions.Clear();
        MenuOptions.Add(new MenuOptionViewModel
        {
            Header = "Play Now",
            Icon = TablerIcons.Icons.IconPlayerPlay,
            Command = PlayMovementRowCommand
        });
        MenuOptions.Add(new MenuOptionViewModel
        {
            Header = "Play Next",
            Icon = TablerIcons.Icons.IconCornerUpLeft,
            Command = new AsyncRelayCommand(() => panel.PlayMovementRowNextStubAsync(this))
        });
        MenuOptions.Add(new MenuOptionViewModel
        {
            Header = "Enqueue",
            Icon = TablerIcons.Icons.IconCornerDownLeft,
            Command = EnqueueMovementRowCommand
        });
        MenuOptions.Add(new MenuOptionViewModel
        {
            Header = "Favorite",
            Icon = TablerIcons.Icons.IconHeart,
            Command = new AsyncRelayCommand(() => panel.FavoriteMovementRowStubAsync(this))
        });
        MenuOptions.Add(new MenuOptionViewModel
        {
            Header = "Manage Playlists",
            Icon = TablerIcons.Icons.IconPlaylist,
            Command = new AsyncRelayCommand(() => panel.ManagePlaylistsMovementRowStubAsync(this))
        });
    }
}
