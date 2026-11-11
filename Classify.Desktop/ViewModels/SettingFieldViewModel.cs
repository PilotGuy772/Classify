using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Classify.Core.Domain.Infrastructure;
using Classify.Core.Interfaces.Infrastructure;

namespace Classify.Desktop.ViewModels;

/// <summary>
/// Represents an editable setting field with change tracking, persistence, and reset capabilities.
/// </summary>
public class SettingFieldViewModel : ViewModelBase
{
    private readonly Action<string> _onSave;
    private readonly Action<string>? _onRevealInFileManager;
    private readonly Func<string?, string?>? _onPickValue;
    private string _value;
    private string _savedValue;

    /// <summary>
    /// Gets the unique key identifying the setting.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Gets the user-facing label or title of the setting.
    /// </summary>
    public string Label { get; }

    /// <summary>
    /// Gets the description explaining what the setting controls.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets or sets the current user-facing value of the field.
    /// </summary>
    public string Value
    {
        get => _value;
        set
        {
            if (_value != value)
            {
                _value = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(IsModified));
            }
        }
    }

    /// <summary>
    /// Gets the persisted value currently stored in application settings.
    /// </summary>
    public string SavedValue => _savedValue;

    /// <summary>
    /// Gets a value indicating whether the current value differs from the persisted setting.
    /// </summary>
    public bool IsModified => !string.Equals(_value, _savedValue, StringComparison.Ordinal);

    /// <summary>
    /// Gets a value indicating whether the value is treated as a file/directory path that can be revealed.
    /// </summary>
    public bool PathValue => _onRevealInFileManager is not null && Options.Count == 0 && !string.IsNullOrWhiteSpace(_value);

    /// <summary>
    /// Gets a value indicating whether value picking is supported for this setting.
    /// </summary>
    public bool PickerSupported => _onPickValue is not null && Options.Count == 0;

    /// <summary>
    /// Gets the available options for enum-based values such as appearance.
    /// </summary>
    public ObservableCollection<string> Options { get; } = [];

    /// <summary>
    /// Gets the command that saves the modified value to application settings.
    /// </summary>
    public ICommand SaveCommand { get; }

    /// <summary>
    /// Gets the command that restores the user-facing field to the currently saved value.
    /// </summary>
    public ICommand ResetCommand { get; }

    /// <summary>
    /// Gets the command that reveals the current value in the local file manager.
    /// </summary>
    public ICommand RevealInFileManagerCommand { get; }

    /// <summary>
    /// Gets the command that opens a platform picker for a new value.
    /// </summary>
    public ICommand PickValueCommand { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingFieldViewModel"/> class.
    /// </summary>
    /// <param name="key">The setting key.</param>
    /// <param name="label">The setting label.</param>
    /// <param name="description">The setting description.</param>
    /// <param name="initialValue">The initial value from application settings.</param>
    /// <param name="onSave">The callback action executed when saving.</param>
    /// <param name="onRevealInFileManager">Optional callback invoked when revealing the value in finder/explorer.</param>
    /// <param name="onPickValue">Optional callback invoked to choose a new value without going through save/reset.</param>
    public SettingFieldViewModel(
        string key,
        string label,
        string description,
        string? initialValue,
        Action<string> onSave,
        Action<string>? onRevealInFileManager = null,
        Func<string?, string?>? onPickValue = null)
    {
        Key = key;
        Label = label;
        Description = description;
        _savedValue = initialValue ?? string.Empty;
        _value = _savedValue;
        _onSave = onSave ?? throw new ArgumentNullException(nameof(onSave));
        _onRevealInFileManager = onRevealInFileManager;
        _onPickValue = onPickValue;

        if (key == nameof(IAppSettings.Appearance))
        {
            foreach (string option in Enum.GetNames<AppAppearance>())
            {
                Options.Add(option);
            }
        }

        SaveCommand = new RelayCommand(_ => Save());
        ResetCommand = new RelayCommand(_ => Reset());
        RevealInFileManagerCommand = new RelayCommand(_ => RevealInFileManager());
        PickValueCommand = new RelayCommand(_ => PickValue());
    }

    /// <summary>
    /// Persists the current value and updates the saved state.
    /// </summary>
    public void Save()
    {
        _onSave(_value);
        _savedValue = _value;
        RaisePropertyChanged(nameof(SavedValue));
        RaisePropertyChanged(nameof(IsModified));
    }

    /// <summary>
    /// Restores the current user-facing value to the persisted setting value.
    /// </summary>
    public void Reset()
    {
        Value = _savedValue;
    }

    /// <summary>
    /// Reveals the current value in the operating system file manager when supported.
    /// </summary>
    public void RevealInFileManager()
    {
        if (!string.IsNullOrWhiteSpace(_value))
        {
            _onRevealInFileManager?.Invoke(_value);
        }
    }

    /// <summary>
    /// Opens a platform picker to choose a new value and immediately persists it if a choice is confirmed.
    /// </summary>
    public void PickValue()
    {
        if (_onPickValue is null)
        {
            return;
        }

        string? pickedValue = _onPickValue(_value);
        if (string.IsNullOrWhiteSpace(pickedValue))
        {
            return;
        }

        _value = pickedValue;
        Save();
    }

    /// <summary>
    /// Updates the persisted value when the setting is modified externally.
    /// </summary>
    /// <param name="newValue">The updated external setting value.</param>
    public void UpdateSavedValue(string? newValue)
    {
        string sanitized = newValue ?? string.Empty;
        bool wasUnmodified = !IsModified;
        _savedValue = sanitized;
        RaisePropertyChanged(nameof(SavedValue));

        if (wasUnmodified)
        {
            _value = sanitized;
            RaisePropertyChanged(nameof(Value));
        }

        RaisePropertyChanged(nameof(IsModified));
    }
}
