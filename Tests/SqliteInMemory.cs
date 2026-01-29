using Classify.Core.Interfaces;
using Classify.Core.Interfaces.Repository;
using Classify.Core.Interfaces.Service;
using Classify.Data;
using Classify.Data.Context;
using Classify.Data.Repositories;
using Classify.Services.Ingestion;
using Classify.Services.Ingestion.File;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Tests;

public static class SqliteInMemory
{
    public static ClassifyContext CreateDbContext()
    {
        SqliteConnection connection = new("DataSource=:memory:");
        connection.Open();

        DbContextOptions<ClassifyContext> options = new DbContextOptionsBuilder<ClassifyContext>()
            .UseSqlite(connection)
            .EnableSensitiveDataLogging()
            .Options;

        ClassifyContext context = new(options);

        context.Database.Migrate();

        return context;
    }
    
    public static IServiceProvider BuildTestServices()
    {
        ServiceCollection services = new();

        SqliteConnection connection = new("DataSource=:memory:");
        connection.Open();

        services.AddDbContextFactory<ClassifyContext>(options =>
        {
            options.UseSqlite(connection);
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

        ServiceProvider provider = services.BuildServiceProvider();

        using IServiceScope scope = provider.CreateScope();
        ClassifyContext db = scope.ServiceProvider.GetRequiredService<ClassifyContext>();
        db.Database.Migrate();

        return provider;
    }

}