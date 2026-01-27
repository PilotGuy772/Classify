// using Avalonia;
// using System;
//
// namespace Classify.Desktop;
//
// class Program
// {
//     // Initialization code. Don't use any Avalonia, third-party APIs or any
//     // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
//     // yet and stuff might break.
//     [STAThread]
//     public static void Main(string[] args) => BuildAvaloniaApp()
//         .StartWithClassicDesktopLifetime(args);
//
//     // Avalonia configuration, don't remove; also used by visual designer.
//     public static AppBuilder BuildAvaloniaApp()
//         => AppBuilder.Configure<App>()
//             .UsePlatformDetect()
//             .WithInterFont()
//             .LogToTrace();
// }

using System;
using Avalonia;
using Classify.Core.Interfaces;
using Classify.Core.Interfaces.Repository;
using Classify.Core.Interfaces.Service;
using Classify.Data;
using Classify.Data.Context;
using Classify.Data.Repositories;
using Classify.Services.Ingestion;
using Classify.Services.Ingestion.File;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Classify.Desktop;

internal static class Program
{
    public static void Main(string[] args)
    {
        IHost? host = CreateHostBuilder(args).Build();
        
        App.SetServices(host.Services);
        
        host.Start();

        // using (IServiceScope scope = host.Services.CreateScope())
        // {
        //     ClassifyContext db = scope.ServiceProvider.GetRequiredService<ClassifyContext>();
        //     db.Database.Migrate();
        // }

        
        BuildAvaloniaApp(host.Services)
            .StartWithClassicDesktopLifetime(args);
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices(ConfigureServices);

    private static void ConfigureServices(
        HostBuilderContext context,
        IServiceCollection services)
    {
        // EF Core
        services.AddDbContext<ClassifyContext>(options =>
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
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Application services / use cases
        services.AddScoped<IIngestionService, LibraryIngestionService>();
        services.AddScoped<IAudioFileScanner, FileSystemAudioFileScanner>();

        // ViewModels
        services.AddTransient<MainWindow>();
    }

    private static AppBuilder BuildAvaloniaApp(IServiceProvider services)
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}