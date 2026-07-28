using Classify.Core.Domain;
using Classify.Core.Interfaces.Infrastructure;
using Classify.Data.Seeders;
using Microsoft.Extensions.DependencyInjection;

namespace Tests;

/// <summary>
/// Contains unit tests to verify the behavior of the database seeder.
/// </summary>
public class SeederTests
{
    /// <summary>
    /// Verifies that the database seeder correctly inserts all composers, works, movements, recordings, and audio files,
    /// including satisfying constraints like minimum numbers, alternative recordings, and overlapping recordings.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task SeedAsync_PopulatesDatabaseCorrectly()
    {
        // Arrange
        IServiceProvider services = SqliteInMemory.BuildTestServices();
        using IServiceScope scope = services.CreateScope();
        IUnitOfWork uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        DemoLibrarySeeder seeder = new DemoLibrarySeeder(uow);

        // Act
        await seeder.SeedAsync();

        // Assert
        List<Composer> composers = (await uow.Composers.GetAllAsync()).ToList();
        List<Work> works = (await uow.Works.GetAllAsync()).ToList();
        List<Movement> movements = (await uow.Movements.GetAllAsync()).ToList();
        List<Recording> recordings = (await uow.Recordings.GetAllAsync()).ToList();
        List<AudioFile> audioFiles = (await uow.AudioFiles.GetAllAsync()).ToList();
        List<PerformedMovement> performedMovements = (await uow.PerformedMovements.GetAllAsync()).ToList();

        // 1. Verify counts
        Assert.True(composers.Count >= 20, $"Expected at least 20 composers, but got {composers.Count}");
        Assert.True(works.Count >= 100, $"Expected at least 100 works, but got {works.Count}");

        // 2. Verify every work has at least one recording
        foreach (Work work in works)
        {
            bool hasRecording = recordings.Any(r => r.WorkId == work.Id);
            Assert.True(hasRecording, $"Work {work.Name} does not have any recordings");
        }

        // 3. Verify some works have more than one recording
        List<int> workIdsWithMultipleRecordings = recordings
            .GroupBy(r => r.WorkId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        Assert.True(workIdsWithMultipleRecordings.Count > 0, "No works have multiple recordings");

        // 4. Verify overlapping recordings (recordings that cover movements from more than one piece)
        bool hasOverlappingRecording = false;
        foreach (Recording recording in recordings)
        {
            List<PerformedMovement> perfMovements = performedMovements.Where(pm => pm.RecordingId == recording.Id).ToList();
            List<int> movementIds = perfMovements.Select(pm => pm.MovementId).ToList();
            List<Movement> matchingMovements = movements.Where(m => movementIds.Contains(m.Id)).ToList();
            List<int> distinctWorkIds = matchingMovements.Select(m => m.WorkId).Distinct().ToList();

            if (distinctWorkIds.Count > 1)
            {
                hasOverlappingRecording = true;
            }
        }
        Assert.True(hasOverlappingRecording, "No overlapping recordings (covering movements from >1 work) were found");

        // 5. Verify every work has movements
        foreach (Work work in works)
        {
            bool hasMovements = movements.Any(m => m.WorkId == work.Id);
            Assert.True(hasMovements, $"Work {work.Name} does not have any movements");
        }

        // 6. Verify performed movements are mapped to audio files
        Assert.True(performedMovements.Count > 0, "No performed movements were created");
        foreach (PerformedMovement pm in performedMovements)
        {
            Assert.True(pm.AudioFileId > 0, $"Performed movement #{pm.Id} is not linked to a valid audio file");
            Assert.True(pm.RecordingId > 0, $"Performed movement #{pm.Id} is not linked to a valid recording");
            Assert.True(pm.MovementId > 0, $"Performed movement #{pm.Id} is not linked to a valid movement");
        }
    }

    /// <summary>
    /// Verifies that a WorkRecording entity can be successfully created, saved to the database,
    /// and retrieved back through the repository.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task WorkRecording_CanBeSavedAndRetrieved()
    {
        // Arrange
        IServiceProvider services = SqliteInMemory.BuildTestServices();
        using IServiceScope scope = services.CreateScope();
        IUnitOfWork uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        Composer composer = new Composer { Name = "Test Composer" };
        await uow.Composers.AddAsync(composer);
        await uow.SaveChangesAsync();

        Work work = new Work { ComposerId = composer.Id, Name = "Test Work", CatalogNumber = "Op. 99" };
        await uow.Works.AddAsync(work);
        await uow.SaveChangesAsync();

        Recording recording = new Recording { WorkId = work.Id, Conductor = "Test Conductor" };
        await uow.Recordings.AddAsync(recording);
        await uow.SaveChangesAsync();

        WorkRecording workRecording = new WorkRecording
        {
            WorkId = work.Id,
            RecordingId = recording.Id
        };

        // Act
        await uow.WorkRecordings.AddAsync(workRecording);
        await uow.SaveChangesAsync();

        // Assert
        List<WorkRecording> retrieved = (await uow.WorkRecordings.GetAllAsync()).ToList();
        Assert.Single(retrieved);
        Assert.Equal(work.Id, retrieved[0].WorkId);
        Assert.Equal(recording.Id, retrieved[0].RecordingId);
        Assert.Equal($"WorkRecording #{retrieved[0].Id}", retrieved[0].Name);
        Assert.Null(retrieved[0].PhotoKey);
    }
}
