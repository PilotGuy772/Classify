using System.Dynamic;
using Classify.Core.Domain;
using Classify.Core.Enums;
using Classify.Core.Interfaces.Infrastructure;

namespace Classify.Data.Seeders;

using Bogus;

public sealed class DemoLibrarySeeder(IUnitOfWork uow) : IDatabaseSeeder
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await uow.Composers.AnyAsync())
            return;

        Faker faker = new();
        Random r = new();

        // Composer
        Composer composer = new()
        {
            //Id = r.Next(),
            Name = faker.Name.FullName()
        };
        await uow.Composers.AddAsync(composer);
        await uow.SaveChangesAsync();


        // Work
        Work work = new()
        {
            //Id = r.Next(),
            ComposerId = composer.Id,
            CatalogNumber = "Op. X",
            Name = faker.Music.Genre() + faker.Music.Genre() + " Symphony"
        };
        await uow.Works.AddAsync(work);
        await uow.SaveChangesAsync();


        // Movements
        List<Movement> movements = Enumerable.Range(1, 4)
            .Select(i => new Movement{
                //Id = r.Next(),
                WorkId = work.Id,
                Name = $"Movement {i}",
                Order = i
            })
            .ToList();

        foreach (Movement m in movements)
            await uow.Movements.AddAsync(m);
        await uow.SaveChangesAsync();


        // Recording
        Recording recording = new()
        {
            //Id = r.Next(),
            Conductor = faker.Name.FullName(),
            WorkId = work.Id
        };
        await uow.Recordings.AddAsync(recording);
        await uow.SaveChangesAsync();


        // Audio files
        List<AudioFile> audioFiles = Enumerable.Range(1, 2)
            .Select(_ => new AudioFile {
                //Id = r.Next(),
                Path = faker.System.FilePath(),
                Hash = faker.Random.ULong(),
                Status = IngestionStatus.Complete
            })
            .ToList();

        foreach (AudioFile af in audioFiles)
            await uow.AudioFiles.AddAsync(af);
        await uow.SaveChangesAsync();


        // Recording ↔ AudioFile
        foreach (AudioFile af in audioFiles)
        {
            foreach (Movement m in movements)
            {
                await uow.PerformedMovements.AddAsync(
                    new PerformedMovement{
                        RecordingId = recording.Id,
                        MovementId = m.Id,
                        AudioFileId = af.Id,
                        Order = m.Order
                    }
                );
            }
        }
        
        await uow.SaveChangesAsync();
    }
}
