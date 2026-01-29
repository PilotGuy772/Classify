using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Classify.Core.Interfaces;
using Classify.Core.Interfaces.Infrastructure;
using Classify.Core.Interfaces.Repository;
using Classify.Core.Interfaces.Service;
using Classify.Data;
using Classify.Data.Context;
using Classify.Data.Repositories;
using Classify.Desktop.ViewModels;
using Classify.Services.Ingestion;
using Classify.Services.Ingestion.File;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Classify.Desktop;

public class App : Application
{
    public static IServiceProvider Services { get; private set; }
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        ServiceCollection services = new();
        
        ConfigureServices(services);

        Services = services.BuildServiceProvider();

        //HomeViewModel test = Services.GetRequiredService<HomeViewModel>();
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            MainWindowViewModel vm = Services.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = new MainWindow
            {
                DataContext = vm
            };
            vm.Initialize();
        }

        base.OnFrameworkInitializationCompleted();
    }
    
    private static void ConfigureServices(IServiceCollection services)
    {
        // EF Core
        services.AddDbContextFactory<ClassifyContext>(options =>
        {
            options.UseSqlite("Data Source=library.db");
        });

        // Repositories
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

        // ViewModels
        services.AddSingleton<MainWindowViewModel>();

        services.AddTransient<HomeViewModel>();
        services.AddTransient<LibraryViewModel>();
        services.AddTransient<SettingsViewModel>();
    }
}