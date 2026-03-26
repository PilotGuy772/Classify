using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Classify.Core.Domain.Infrastructure;
using Classify.Core.Interfaces.Infrastructure;
using Classify.Core.Interfaces.Repository;
using Classify.Core.Interfaces.Service;
using Classify.Data;
using Classify.Data.Context;
using Classify.Data.Repositories;
using Classify.Data.Seeders;
using Classify.Desktop.ViewModels;
using Classify.Desktop.Views;
using Classify.Services;
using Classify.Services.Ingestion;
using Classify.Services.Ingestion.File;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Classify.Desktop;

public class App : Application
{
    public static IServiceProvider Services { get; private set; }
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        ServiceCollection services = new();
        
        ConfigureServices(services);

        Services = services.BuildServiceProvider();

        try
        {
            await SeedIfEmptyAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return;
        }
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            MainWindowViewModel vm = Services.GetRequiredService<MainWindowViewModel>();
            var mainWindow = new MainWindow
            {
                DataContext = vm
            };

            // Apply platform class to the top-level window so it propagates to children.
            try
            {
                var platformService = Services.GetService<Classify.Core.Interfaces.Infrastructure.IPlatformService>();
                if (platformService is not null)
                {
                    string platformClass = platformService.Current switch
                    {
                        Classify.Core.Interfaces.Infrastructure.PlatformKind.MacOS => "platform-macos",
                        Classify.Core.Interfaces.Infrastructure.PlatformKind.Linux => "platform-linux",
                        Classify.Core.Interfaces.Infrastructure.PlatformKind.Windows => "platform-windows",
                        _ => "platform-unknown"
                    };

                    mainWindow.Classes.Add(platformClass);
                }
            }
            catch
            {
                // Don't crash startup for styling errors.
            }

            desktop.MainWindow = mainWindow;
            vm.Initialize();
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// Seed the database if all repositories are empty.
    /// </summary>
    private static async Task SeedIfEmptyAsync()
    {
        Console.WriteLine("Seeding database...");
        IUnitOfWork uow = Services.GetRequiredService<IUnitOfWork>();
        ClassifyContext context = Services.GetRequiredService<ClassifyContext>();
        await context.Database.MigrateAsync();
        
        if (await uow.AudioFiles.AnyAsync() ||
            await uow.PerformedMovements.AnyAsync() ||
            await uow.Composers.AnyAsync() ||
            await uow.Movements.AnyAsync() ||
            await uow.ProposedMatches.AnyAsync() ||
            await uow.Recordings.AnyAsync() ||
            await uow.Works.AnyAsync())
        {
            Console.WriteLine("Seed not necessary.");
        }
        
        IDatabaseSeeder seed = Services.GetRequiredService<IDatabaseSeeder>();
        await seed.SeedAsync();
        Console.WriteLine("Seeded.");
    }
    
    private static void ConfigureServices(IServiceCollection services)
    {
        // EF Core
        services.AddDbContextFactory<ClassifyContext>(options =>
        {
            options.UseSqlite("Data Source=library.db");
        });

        //services.AddDbContext<ClassifyContext>(options => options.UseSqlite("DataSource=library.db"));
        
        // Repositories
        services.AddScoped<IAudioFileRepository, AudioFileRepository>();
        services.AddScoped<IComposerRepository, ComposerRepository>();
        services.AddScoped<IWorkRepository, WorkRepository>();
        services.AddScoped<IMovementRepository, MovementRepository>();
        services.AddScoped<IRecordingRepository, RecordingRepository>();
        services.AddScoped<IPerformedMovementRepository, PerformedMovementRepository>();
        services.AddScoped<IProposedMatchRepository, ProposedMatchRepository>();
        services.AddTransient<IUnitOfWork, UnitOfWork>();

        // Application services / use cases
        services.AddScoped<IIngestionService, LibraryIngestionService>();
        services.AddScoped<IAudioFileScanner, FileSystemAudioFileScanner>();
        services.AddScoped<IIngestionOrchestrationService, LibraryIngestionOrchestrationService>();

        // Playables
        services.AddScoped<IPlayableResolutionService, Classify.Data.Services.PlayableResolutionService>();
        services.AddScoped<IPlayablePlaylistService, Classify.Data.Services.PlayablePlaylistService>();
        
        // Search services (concrete per-entity)
        services.AddTransient<Classify.Data.Services.ComposerSearchService>();
        services.AddTransient<Classify.Data.Services.WorkSearchService>();
        services.AddTransient<Classify.Data.Services.MovementSearchService>();
        services.AddTransient<Classify.Data.Services.RecordingSearchService>();
        
        // Utility
        services.AddTransient<IDialogService, DialogService>();

        // ViewModels
        services.AddSingleton<MainWindowViewModel>();
        services.AddTransient<HomeViewModel>();
        services.AddTransient<LibraryViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<LibraryScanViewModel>();
        services.AddTransient<PlaylistsViewModel>();
        services.AddTransient<BrowseViewModel>();
        services.AddTransient<FavoritesViewModel>();
        services.AddTransient<ExploreViewModel>();
        services.AddTransient<RadioViewModel>();
        services.AddTransient<AudioFileDetailViewModel>();
        services.AddTransient<ComposerDetailViewModel>();
        services.AddTransient<MovementDetailViewModel>();
        services.AddTransient<RecordingDetailViewModel>();
        services.AddTransient<WorkDetailViewModel>();
        services.AddTransient<ProposedMatchesDialogViewModel>();
        services.AddTransient<ProposedMatchDialogViewModel>();
        services.AddTransient<ProposedMatchDialog>();
        services.AddTransient<ProposedMatchesDialog>();
        
        // App Configuration
        string settingsPath =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Classify",
                "settings.json");

        ConfigurationBuilder builder = new();
        builder.SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile(settingsPath, optional: true);

        IConfiguration config = builder.Build();

        services.AddSingleton(config);
        services.Configure<AppSettings>(config.GetSection("AppSettings"));


#if (DEBUG)
        // DEV only
        services.AddTransient<IDatabaseSeeder, DemoLibrarySeeder>();
#endif
    }
}