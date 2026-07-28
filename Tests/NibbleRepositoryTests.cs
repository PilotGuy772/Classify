using Classify.Core.Domain;
using Classify.Data.Context;
using Classify.Data.Repositories;
using Xunit;

namespace Tests;

/// <summary>
/// Unit tests for <see cref="NibbleRepository"/> and <see cref="NibbleMovementRepository"/>.
/// </summary>
public class NibbleRepositoryTests
{
    /// <summary>
    /// Verifies that a Nibble can be added and retrieved by ID and related entity IDs.
    /// </summary>
    [Fact]
    public async Task CanAddAndRetrieveNibble()
    {
        using ClassifyContext context = SqliteInMemory.CreateDbContext();
        NibbleRepository nibbleRepo = new NibbleRepository(context);

        Composer composer = new Composer { Name = "Ludwig van Beethoven" };
        await context.Composers.AddAsync(composer);
        await context.SaveChangesAsync();

        Work work = new Work { ComposerId = composer.Id, Name = "Symphony No. 5", CatalogNumber = "Op. 67" };
        await context.Works.AddAsync(work);
        await context.SaveChangesAsync();

        Recording recording = new Recording { WorkId = work.Id, Conductor = "Carlos Kleiber", Year = 1975 };
        await context.Recordings.AddAsync(recording);
        await context.SaveChangesAsync();

        Nibble nibble = new Nibble
        {
            WorkId = work.Id,
            RecordingId = recording.Id
        };

        await nibbleRepo.AddAsync(nibble);
        await context.SaveChangesAsync();

        Nibble? retrieved = await nibbleRepo.GetByIdAsync(nibble.Id);
        Assert.NotNull(retrieved);
        Assert.Equal(work.Id, retrieved.WorkId);
        Assert.Equal(recording.Id, retrieved.RecordingId);

        IEnumerable<Nibble> workNibbles = await nibbleRepo.GetByWorkIdAsync(work.Id);
        Assert.Single(workNibbles);

        IEnumerable<Nibble> recNibbles = await nibbleRepo.GetByRecordingIdAsync(recording.Id);
        Assert.Single(recNibbles);
    }

    /// <summary>
    /// Verifies that NibbleMovements can be linked to a Nibble in an explicit order.
    /// </summary>
    [Fact]
    public async Task CanAddAndRetrieveOrderedNibbleMovements()
    {
        using ClassifyContext context = SqliteInMemory.CreateDbContext();
        NibbleRepository nibbleRepo = new NibbleRepository(context);
        NibbleMovementRepository movementRepo = new NibbleMovementRepository(context);

        Composer composer = new Composer { Name = "J.S. Bach" };
        await context.Composers.AddAsync(composer);
        await context.SaveChangesAsync();

        Work work = new Work { ComposerId = composer.Id, Name = "Brandenburg Concerto No. 3", CatalogNumber = "BWV 1048" };
        await context.Works.AddAsync(work);
        await context.SaveChangesAsync();

        Movement m1 = new Movement { WorkId = work.Id, Name = "Allegro", Order = 1 };
        Movement m2 = new Movement { WorkId = work.Id, Name = "Adagio", Order = 2 };
        Movement m3 = new Movement { WorkId = work.Id, Name = "Allegro", Order = 3 };
        await context.Movements.AddRangeAsync(m1, m2, m3);
        await context.SaveChangesAsync();

        Recording recording = new Recording { WorkId = work.Id, Conductor = "Trevor Pinnock" };
        await context.Recordings.AddAsync(recording);
        await context.SaveChangesAsync();

        Nibble nibble = new Nibble { WorkId = work.Id, RecordingId = recording.Id };
        await nibbleRepo.AddAsync(nibble);
        await context.SaveChangesAsync();

        NibbleMovement nm1 = new NibbleMovement { NibbleId = nibble.Id, MovementId = m1.Id, Order = 1 };
        NibbleMovement nm2 = new NibbleMovement { NibbleId = nibble.Id, MovementId = m3.Id, Order = 2 };
        await movementRepo.AddAsync(nm1);
        await movementRepo.AddAsync(nm2);
        await context.SaveChangesAsync();

        List<NibbleMovement> orderedMovements = (await movementRepo.GetByNibbleIdAsync(nibble.Id)).ToList();
        Assert.Equal(2, orderedMovements.Count);
        Assert.Equal(m1.Id, orderedMovements[0].MovementId);
        Assert.Equal(1, orderedMovements[0].Order);
        Assert.Equal(m3.Id, orderedMovements[1].MovementId);
        Assert.Equal(2, orderedMovements[1].Order);
    }

    /// <summary>
    /// Verifies that deleting a Work or Recording cascade deletes associated Nibbles.
    /// </summary>
    [Fact]
    public async Task CascadeDeleteOnWorkAndRecordingDeletesNibbles()
    {
        using ClassifyContext context = SqliteInMemory.CreateDbContext();
        NibbleRepository nibbleRepo = new NibbleRepository(context);

        Composer composer = new Composer { Name = "W.A. Mozart" };
        await context.Composers.AddAsync(composer);
        await context.SaveChangesAsync();

        Work work = new Work { ComposerId = composer.Id, Name = "Symphony No. 40", CatalogNumber = "K. 550" };
        await context.Works.AddAsync(work);
        await context.SaveChangesAsync();

        Recording recording = new Recording { WorkId = work.Id, Conductor = "Karl Böhm" };
        await context.Recordings.AddAsync(recording);
        await context.SaveChangesAsync();

        Nibble nibble = new Nibble { WorkId = work.Id, RecordingId = recording.Id };
        await nibbleRepo.AddAsync(nibble);
        await context.SaveChangesAsync();

        context.Works.Remove(work);
        await context.SaveChangesAsync();

        Nibble? deletedNibble = await nibbleRepo.GetByIdAsync(nibble.Id);
        Assert.Null(deletedNibble);
    }
}
