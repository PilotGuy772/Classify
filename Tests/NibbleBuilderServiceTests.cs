using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Classify.Core.Domain;
using Classify.Data;
using Classify.Data.Context;
using Classify.Services.Queue;
using Xunit;

namespace Tests;

/// <summary>
/// Unit tests for <see cref="NibbleBuilderService"/> verifying work resolution, recording fallbacks, and movement ordering.
/// </summary>
public class NibbleBuilderServiceTests
{
    /// <summary>
    /// Verifies that BuildForWorkAsync selects the favorite recording when specified.
    /// </summary>
    [Fact]
    public async Task BuildForWorkAsync_UsesFavoriteRecording()
    {
        using ClassifyContext context = SqliteInMemory.CreateDbContext();
        UnitOfWork uow = new(new DbContextFactoryMock(context));

        Composer composer = new() { Name = "J.S. Bach" };
        await uow.Composers.AddAsync(composer);
        await uow.SaveChangesAsync();

        Work work = new() { ComposerId = composer.Id, Name = "Goldberg Variations", CatalogNumber = "BWV 988" };
        await uow.Works.AddAsync(work);
        await uow.SaveChangesAsync();

        Recording rec1 = new() { WorkId = work.Id, Conductor = "Zubin Mehta" };
        Recording rec2 = new() { WorkId = work.Id, Conductor = "Claudio Abbado" };
        await uow.Recordings.AddAsync(rec1);
        await uow.Recordings.AddAsync(rec2);
        await uow.SaveChangesAsync();

        // Mark rec1 as favorite
        work.FavoriteRecordingId = rec1.Id;
        uow.Works.Update(work);
        await uow.SaveChangesAsync();

        Movement m1 = new() { WorkId = work.Id, Name = "Aria", Order = 1 };
        await uow.Movements.AddAsync(m1);
        await uow.SaveChangesAsync();

        NibbleBuilderService builder = new(uow);
        QueueItem? item = await builder.BuildForWorkAsync(work.Id);

        Assert.NotNull(item);
        Assert.Equal(work.Id, item.Nibble.WorkId);
        Assert.Equal(rec1.Id, item.Nibble.RecordingId);
        Assert.Single(item.Movements);
    }

    /// <summary>
    /// Verifies that BuildForWorkAsync falls back to the first recording by conductor name alphabetically when no favorite is set.
    /// </summary>
    [Fact]
    public async Task BuildForWorkAsync_FallsBackToAlphabeticalConductor()
    {
        using ClassifyContext context = SqliteInMemory.CreateDbContext();
        UnitOfWork uow = new(new DbContextFactoryMock(context));

        Composer composer = new() { Name = "L.v. Beethoven" };
        await uow.Composers.AddAsync(composer);
        await uow.SaveChangesAsync();

        Work work = new() { ComposerId = composer.Id, Name = "Symphony No. 5", CatalogNumber = "Op. 67", FavoriteRecordingId = null };
        await uow.Works.AddAsync(work);
        await uow.SaveChangesAsync();

        Recording recKarajan = new() { WorkId = work.Id, Conductor = "Herbert von Karajan" };
        Recording recAbbado = new() { WorkId = work.Id, Conductor = "Claudio Abbado" };
        Recording recBernstein = new() { WorkId = work.Id, Conductor = "Leonard Bernstein" };

        await uow.Recordings.AddAsync(recKarajan);
        await uow.Recordings.AddAsync(recAbbado);
        await uow.Recordings.AddAsync(recBernstein);
        await uow.SaveChangesAsync();

        Movement m1 = new() { WorkId = work.Id, Name = "Allegro con brio", Order = 1 };
        await uow.Movements.AddAsync(m1);
        await uow.SaveChangesAsync();

        NibbleBuilderService builder = new(uow);
        QueueItem? item = await builder.BuildForWorkAsync(work.Id);

        Assert.NotNull(item);
        // Claudio Abbado is first alphabetically
        Assert.Equal(recAbbado.Id, item.Nibble.RecordingId);
    }

    /// <summary>
    /// Verifies that BuildForRecordingAsync constructs a queue item for the specified recording with ordered movements.
    /// </summary>
    [Fact]
    public async Task BuildForRecordingAsync_ConstructsItemForRecording()
    {
        using ClassifyContext context = SqliteInMemory.CreateDbContext();
        UnitOfWork uow = new(new DbContextFactoryMock(context));

        Composer composer = new() { Name = "Mozart" };
        await uow.Composers.AddAsync(composer);
        await uow.SaveChangesAsync();

        Work work = new() { ComposerId = composer.Id, Name = "Symphony No. 40" };
        await uow.Works.AddAsync(work);
        await uow.SaveChangesAsync();

        Recording rec = new() { WorkId = work.Id, Conductor = "Karl Böhm" };
        await uow.Recordings.AddAsync(rec);
        await uow.SaveChangesAsync();

        Movement m1 = new() { WorkId = work.Id, Name = "Molto Allegro", Order = 1 };
        Movement m2 = new() { WorkId = work.Id, Name = "Andante", Order = 2 };
        await uow.Movements.AddAsync(m1);
        await uow.Movements.AddAsync(m2);
        await uow.SaveChangesAsync();

        NibbleBuilderService builder = new(uow);
        QueueItem? item = await builder.BuildForRecordingAsync(rec.Id);

        Assert.NotNull(item);
        Assert.Equal(rec.Id, item.Nibble.RecordingId);
        Assert.Equal(2, item.Movements.Count);
        Assert.Equal(m1.Id, item.Movements[0].MovementId);
        Assert.Equal(m2.Id, item.Movements[1].MovementId);
    }

    /// <summary>
    /// Verifies that BuildForMovementAsync constructs a queue item containing only the specified movement.
    /// </summary>
    [Fact]
    public async Task BuildForMovementAsync_CreatesItemWithSingleMovementOnly()
    {
        using ClassifyContext context = SqliteInMemory.CreateDbContext();
        UnitOfWork uow = new(new DbContextFactoryMock(context));

        Composer composer = new() { Name = "Brahms" };
        await uow.Composers.AddAsync(composer);
        await uow.SaveChangesAsync();

        Work work = new() { ComposerId = composer.Id, Name = "Symphony No. 1" };
        await uow.Works.AddAsync(work);
        await uow.SaveChangesAsync();

        Recording rec = new() { WorkId = work.Id, Conductor = "Karajan" };
        await uow.Recordings.AddAsync(rec);
        await uow.SaveChangesAsync();

        Movement m1 = new() { WorkId = work.Id, Name = "Un poco sostenuto", Order = 1 };
        Movement m2 = new() { WorkId = work.Id, Name = "Andante sostenuto", Order = 2 };
        await uow.Movements.AddAsync(m1);
        await uow.Movements.AddAsync(m2);
        await uow.SaveChangesAsync();

        NibbleBuilderService builder = new(uow);
        QueueItem? item = await builder.BuildForMovementAsync(m2.Id, rec.Id);

        Assert.NotNull(item);
        Assert.Equal(rec.Id, item.Nibble.RecordingId);
        Assert.Single(item.Movements);
        Assert.Equal(m2.Id, item.Movements[0].MovementId);
    }

    /// <summary>
    /// Mock IDbContextFactory for UnitOfWork testing.
    /// </summary>
    private sealed class DbContextFactoryMock(ClassifyContext context) : Microsoft.EntityFrameworkCore.IDbContextFactory<ClassifyContext>
    {
        public ClassifyContext CreateDbContext() => context;
    }
}
