using System;
using System.IO;
using System.Threading.Tasks;
using Classify.Core.Domain;
using Classify.Core.Interfaces.Infrastructure;
using Classify.Data.Context;
using Classify.Data.Services;
using Classify.Infrastructure;
using Config.Net;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Tests.Services;

/// <summary>
/// Unit and integration tests for <see cref="DatabaseConnectionService"/>, <see cref="ClassifyDbContextFactory"/>, and <see cref="ObservableAppSettings"/>.
/// </summary>
public class DatabaseConnectionServiceTests : IDisposable
{
    private readonly string _testDirectory;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseConnectionServiceTests"/> class with an isolated test directory.
    /// </summary>
    public DatabaseConnectionServiceTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "classify_db_tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDirectory);
    }

    /// <summary>
    /// Verifies that the context factory defaults to "library.db" when <see cref="IAppSettings.DatabasePath"/> is not configured.
    /// </summary>
    [Fact]
    public void ClassifyDbContextFactory_DefaultsToLibraryDb_WhenDatabasePathIsEmpty()
    {
        IAppSettings settings = new ConfigurationBuilder<IAppSettings>()
            .UseJsonString("{}")
            .Build();

        ClassifyDbContextFactory factory = new(settings);
        using ClassifyContext context = factory.CreateDbContext();

        context.Database.GetConnectionString().Should().Be("Data Source=library.db");
    }

    /// <summary>
    /// Verifies that the context factory uses the custom path specified in <see cref="IAppSettings.DatabasePath"/>.
    /// </summary>
    [Fact]
    public void ClassifyDbContextFactory_UsesCustomDatabasePath_WhenConfigured()
    {
        string customDbPath = Path.Combine(_testDirectory, "custom_library.db");
        IAppSettings settings = new ConfigurationBuilder<IAppSettings>()
            .UseJsonString($"{{\"DatabasePath\": \"{customDbPath.Replace("\\", "\\\\")}\"}}")
            .Build();

        ClassifyDbContextFactory factory = new(settings);
        using ClassifyContext context = factory.CreateDbContext();

        context.Database.GetConnectionString().Should().Be($"Data Source={customDbPath}");
    }

    /// <summary>
    /// Verifies that the context factory creates parent directories if they do not yet exist.
    /// </summary>
    [Fact]
    public void ClassifyDbContextFactory_CreatesParentDirectory_WhenNestedPathDoesNotExist()
    {
        string nestedDir = Path.Combine(_testDirectory, "nested", "subfolder");
        string dbPath = Path.Combine(nestedDir, "app.db");

        IAppSettings settings = new ConfigurationBuilder<IAppSettings>()
            .UseJsonString($"{{\"DatabasePath\": \"{dbPath.Replace("\\", "\\\\")}\"}}")
            .Build();

        ClassifyDbContextFactory factory = new(settings);
        using ClassifyContext context = factory.CreateDbContext();

        Directory.Exists(nestedDir).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that <see cref="DatabaseConnectionService.ConnectAsync"/> applies migrations and initializes tables on the target database.
    /// </summary>
    [Fact]
    public async Task DatabaseConnectionService_ConnectAsync_AppliesMigrationsSuccessfully()
    {
        string dbPath = Path.Combine(_testDirectory, "migrate_test.db");
        IAppSettings rawSettings = new ConfigurationBuilder<IAppSettings>()
            .UseJsonString($"{{\"DatabasePath\": \"{dbPath.Replace("\\", "\\\\")}\"}}")
            .Build();

        ObservableAppSettings settings = new(rawSettings);
        ClassifyDbContextFactory factory = new(settings);
        using DatabaseConnectionService dbService = new(settings, factory);

        await dbService.ConnectAsync();

        using ClassifyContext context = factory.CreateDbContext();
        bool canConnect = await context.Database.CanConnectAsync();
        canConnect.Should().BeTrue();

        Composer composer = new() { Name = "Johann Sebastian Bach" };
        await context.Composers.AddAsync(composer);
        await context.SaveChangesAsync();

        Composer? saved = await context.Composers.FirstOrDefaultAsync(c => c.Name == "Johann Sebastian Bach");
        saved.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that <see cref="DatabaseConnectionService.ChangeDatabaseAsync"/> switches data sources, updates settings, and fires notifications.
    /// </summary>
    [Fact]
    public async Task DatabaseConnectionService_ChangeDatabaseAsync_SwitchesDataSourceAndFiresEvent()
    {
        string dbPath1 = Path.Combine(_testDirectory, "db1.sqlite");
        string dbPath2 = Path.Combine(_testDirectory, "db2.sqlite");

        IAppSettings rawSettings = new ConfigurationBuilder<IAppSettings>()
            .UseJsonString($"{{\"DatabasePath\": \"{dbPath1.Replace("\\", "\\\\")}\"}}")
            .Build();

        ObservableAppSettings settings = new(rawSettings);
        ClassifyDbContextFactory factory = new(settings);
        using DatabaseConnectionService dbService = new(settings, factory);

        await dbService.ConnectAsync();

        // Populate db1 with data
        using (ClassifyContext context1 = factory.CreateDbContext())
        {
            await context1.Composers.AddAsync(new Composer { Name = "Ludwig van Beethoven" });
            await context1.SaveChangesAsync();
        }

        string? notifiedPath = null;
        dbService.DatabasePathChanged += (sender, newPath) => notifiedPath = newPath;

        // Switch to db2
        await dbService.ChangeDatabaseAsync(dbPath2);

        notifiedPath.Should().Be(dbPath2);
        settings.DatabasePath.Should().Be(dbPath2);
        dbService.GetDatabasePath().Should().Be(dbPath2);

        // Verify db2 is a fresh database without db1's records
        using (ClassifyContext context2 = factory.CreateDbContext())
        {
            bool hasBeethoven = await context2.Composers.AnyAsync(c => c.Name == "Ludwig van Beethoven");
            hasBeethoven.Should().BeFalse();
        }
    }

    /// <summary>
    /// Verifies that updating <see cref="ObservableAppSettings.DatabasePath"/> directly triggers disconnection and notifies listeners.
    /// </summary>
    [Fact]
    public void ObservableAppSettings_DatabasePathPropertyChange_NotifiesSubscribers()
    {
        string dbPath1 = Path.Combine(_testDirectory, "initial.db");
        string dbPath2 = Path.Combine(_testDirectory, "updated.db");

        IAppSettings rawSettings = new ConfigurationBuilder<IAppSettings>()
            .UseJsonString($"{{\"DatabasePath\": \"{dbPath1.Replace("\\", "\\\\")}\"}}")
            .Build();

        ObservableAppSettings settings = new(rawSettings);
        ClassifyDbContextFactory factory = new(settings);
        using DatabaseConnectionService dbService = new(settings, factory);

        string? notifiedPathFromService = null;
        dbService.DatabasePathChanged += (sender, newPath) => notifiedPathFromService = newPath;

        settings.DatabasePath = dbPath2;

        notifiedPathFromService.Should().Be(dbPath2);
    }

    /// <summary>
    /// Cleans up temporary test files and directories.
    /// </summary>
    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }
        catch
        {
            // Ignore cleanup failures in test teardown
        }

        GC.SuppressFinalize(this);
    }
}
