using System;
using Classify.Core.Interfaces.Infrastructure;
using Classify.Desktop.ViewModels;
using Classify.Infrastructure;
using Config.Net;
using FluentAssertions;
using Xunit;

namespace Tests.ViewModels;

/// <summary>
/// Unit tests for <see cref="SettingsViewModel"/> and <see cref="SettingFieldViewModel"/> verifying
/// initial data loading, edit tracking, save persistence, and reset behavior.
/// </summary>
public class SettingsViewModelTests
{
    private static ObservableAppSettings CreateTestSettings(
        string libraryPath = "/music/library",
        string dbPath = "/music/library.db",
        string acceptedFileExtensions = ".mp3,.wav,.flac,.ogg")
    {
        IAppSettings rawSettings = new ConfigurationBuilder<IAppSettings>()
            .UseJsonString($"{{\"CurrentLibraryPath\": \"{libraryPath.Replace("\\", "\\\\")}\", \"DatabasePath\": \"{dbPath.Replace("\\", "\\\\")}\", \"AcceptedFileExtensions\": \"{acceptedFileExtensions.Replace("\\", "\\\\")}\"}}")
            .Build();

        return new ObservableAppSettings(rawSettings);
    }

    /// <summary>
    /// Verifies that <see cref="SettingsViewModel"/> loads current setting values from <see cref="IAppSettings"/>
    /// and initializes fields with <see cref="SettingFieldViewModel.IsModified"/> set to false.
    /// </summary>
    [Fact]
    public void SettingsViewModel_LoadsInitialValues_FromAppSettings()
    {
        ObservableAppSettings settings = CreateTestSettings("/initial/music", "/initial/db.sqlite");
        using SettingsViewModel vm = new(settings);

        vm.Fields.Should().HaveCount(4);
        vm.CurrentLibraryPathField.Value.Should().Be("/initial/music");
        vm.CurrentLibraryPathField.SavedValue.Should().Be("/initial/music");
        vm.CurrentLibraryPathField.IsModified.Should().BeFalse();

        vm.DatabasePathField.Value.Should().Be("/initial/db.sqlite");
        vm.DatabasePathField.SavedValue.Should().Be("/initial/db.sqlite");
        vm.DatabasePathField.IsModified.Should().BeFalse();

        vm.AcceptedFileExtensionsField.Value.Should().Be(".mp3,.wav,.flac,.ogg");
        vm.AcceptedFileExtensionsField.SavedValue.Should().Be(".mp3,.wav,.flac,.ogg");
        vm.AcceptedFileExtensionsField.IsModified.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that editing a field's value updates <see cref="SettingFieldViewModel.IsModified"/> to true,
    /// and reverting it back to the saved value sets <see cref="SettingFieldViewModel.IsModified"/> to false.
    /// </summary>
    [Fact]
    public void SettingFieldViewModel_EditingValue_TogglesIsModified()
    {
        ObservableAppSettings settings = CreateTestSettings("/music/path");
        using SettingsViewModel vm = new(settings);

        SettingFieldViewModel field = vm.CurrentLibraryPathField;
        field.IsModified.Should().BeFalse();

        field.Value = "/music/path/edited";
        field.IsModified.Should().BeTrue();

        field.Value = "/music/path";
        field.IsModified.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that executing <see cref="SettingFieldViewModel.ResetCommand"/> restores the user-facing value
    /// to the currently saved setting and clears <see cref="SettingFieldViewModel.IsModified"/> without altering <see cref="IAppSettings"/>.
    /// </summary>
    [Fact]
    public void SettingFieldViewModel_ResetCommand_RestoresSavedValueWithoutUpdatingAppSettings()
    {
        ObservableAppSettings settings = CreateTestSettings("/saved/music/path");
        using SettingsViewModel vm = new(settings);

        SettingFieldViewModel field = vm.CurrentLibraryPathField;
        field.Value = "/modified/path/to/discard";
        field.IsModified.Should().BeTrue();

        field.ResetCommand.Execute(null);

        field.Value.Should().Be("/saved/music/path");
        field.IsModified.Should().BeFalse();
        settings.CurrentLibraryPath.Should().Be("/saved/music/path");
    }

    /// <summary>
    /// Verifies that executing <see cref="SettingFieldViewModel.SaveCommand"/> persists the modified value
    /// to <see cref="IAppSettings"/>, updates <see cref="SettingFieldViewModel.SavedValue"/>, and resets <see cref="SettingFieldViewModel.IsModified"/>.
    /// </summary>
    [Fact]
    public void SettingFieldViewModel_SaveCommand_PersistsValueToAppSettingsAndClearsIsModified()
    {
        ObservableAppSettings settings = CreateTestSettings("/initial/path");
        using SettingsViewModel vm = new(settings);

        SettingFieldViewModel field = vm.CurrentLibraryPathField;
        field.Value = "/new/saved/path";
        field.IsModified.Should().BeTrue();

        field.SaveCommand.Execute(null);

        settings.CurrentLibraryPath.Should().Be("/new/saved/path");
        field.SavedValue.Should().Be("/new/saved/path");
        field.Value.Should().Be("/new/saved/path");
        field.IsModified.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that saving <see cref="SettingsViewModel.DatabasePathField"/> updates <see cref="IAppSettings.DatabasePath"/>.
    /// </summary>
    [Fact]
    public void SettingsViewModel_DatabasePathSave_UpdatesAppSettingsDatabasePath()
    {
        ObservableAppSettings settings = CreateTestSettings(dbPath: "/original/db.sqlite");
        using SettingsViewModel vm = new(settings);

        vm.DatabasePathField.Value = "/custom/target.db";
        vm.DatabasePathField.IsModified.Should().BeTrue();

        vm.DatabasePathField.SaveCommand.Execute(null);

        settings.DatabasePath.Should().Be("/custom/target.db");
        vm.DatabasePathField.SavedValue.Should().Be("/custom/target.db");
        vm.DatabasePathField.IsModified.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that saving the supported file extensions field persists the raw string value.
    /// </summary>
    [Fact]
    public void SettingsViewModel_AcceptedFileExtensionsSave_UpdatesAppSettingsAcceptedFileExtensions()
    {
        ObservableAppSettings settings = CreateTestSettings(acceptedFileExtensions: ".mp3,.wav");
        using SettingsViewModel vm = new(settings);

        vm.AcceptedFileExtensionsField.Value = ".mp3,.wav,.flac";
        vm.AcceptedFileExtensionsField.IsModified.Should().BeTrue();

        vm.AcceptedFileExtensionsField.SaveCommand.Execute(null);

        settings.AcceptedFileExtensions.Should().Be(".mp3,.wav,.flac");
        vm.AcceptedFileExtensionsField.SavedValue.Should().Be(".mp3,.wav,.flac");
        vm.AcceptedFileExtensionsField.IsModified.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that external updates to settings synchronize with <see cref="SettingFieldViewModel.SavedValue"/>.
    /// </summary>
    [Fact]
    public void SettingsViewModel_ExternalSettingChange_UpdatesFieldSavedValue()
    {
        ObservableAppSettings settings = CreateTestSettings(dbPath: "/initial/db.sqlite");
        using SettingsViewModel vm = new(settings);
        settings.DatabasePath = "/external/switch.db";

        vm.DatabasePathField.SavedValue.Should().Be("/external/switch.db");
        vm.DatabasePathField.Value.Should().Be("/external/switch.db");
        vm.DatabasePathField.IsModified.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that setting <see cref="IAppSettings.CurrentLibraryPath"/> automatically updates
    /// <see cref="IAppSettings.DatabasePath"/> to (LibraryPath)/library.db and synchronizes fields in <see cref="SettingsViewModel"/>.
    /// </summary>
    [Fact]
    public void SettingsViewModel_SettingCurrentLibraryPath_AutomaticallyUpdatesDatabasePath()
    {
        ObservableAppSettings settings = CreateTestSettings(libraryPath: "/music/original", dbPath: "/music/original/library.db");
        using SettingsViewModel vm = new(settings);

        vm.CurrentLibraryPathField.Value = "/music/classical_collection";
        vm.CurrentLibraryPathField.SaveCommand.Execute(null);

        string expectedDbPath = System.IO.Path.Combine(System.IO.Path.Combine("/music/classical_collection", ".classify"), "library.db");
        settings.CurrentLibraryPath.Should().Be("/music/classical_collection");
        settings.DatabasePath.Should().Be(expectedDbPath);
        vm.DatabasePathField.SavedValue.Should().Be(expectedDbPath);
        vm.DatabasePathField.Value.Should().Be(expectedDbPath);
        vm.DatabasePathField.IsModified.Should().BeFalse();
    }
}
