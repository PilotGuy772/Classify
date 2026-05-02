using Classify.Core.Domain;
using Classify.Core.Interfaces.Service;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.Services.Playables;

public class PlayableServicesTests
{
    private sealed class FakePlayable : IPlayable
    {
        public string Name { get; init; } = string.Empty;
        public string? PhotoKey { get; init; }
        public object BaseObject => this;
    }

    [Fact]
    public void Recording_IPlayable_NameAndBaseObject()
    {
        Recording r1 = new Recording { Id = 10, Conductor = string.Empty };
        string name1 = r1.Name;
        name1.Should().Be("Recording #10");

        Recording r2 = new Recording { Id = 11, Conductor = "Szell" };
        string name2 = r2.Name;
        name2.Should().Be("Szell");

        object baseObj = r2.BaseObject;
        baseObj.Should().BeSameAs(r2);
    }

    [Fact]
    public async Task Recording_ResolveAudioIds_IsOrderedByPerformedMovementOrder()
    {
        IServiceProvider provider = SqliteInMemory.BuildTestServices();
        using IServiceScope scope = provider.CreateScope();

        Classify.Data.Context.ClassifyContext db = scope.ServiceProvider.GetRequiredService<Classify.Data.Context.ClassifyContext>();

        Composer c = new Composer { Name = "Mozart" };
        db.Add(c);
        await db.SaveChangesAsync();

        Work w = new Work { Name = "Requiem", ComposerId = c.Id, CatalogNumber = "K.626" };
        db.Add(w);
        await db.SaveChangesAsync();

        Movement m1 = new Movement { Name = "Introitus", Order = 1, WorkId = w.Id };
        Movement m2 = new Movement { Name = "Kyrie", Order = 2, WorkId = w.Id };
        Recording r = new Recording { Conductor = "Karajan", WorkId = w.Id };
        AudioFile a1 = new AudioFile { Path = "a1.flac" };
        AudioFile a2 = new AudioFile { Path = "a2.flac" };

        db.Add(m1);
        db.Add(m2);
        db.Add(r);
        db.Add(a1);
        db.Add(a2);
        await db.SaveChangesAsync();

        // Intentionally reverse the movement order relative to AudioFile IDs
        db.Set<PerformedMovement>().Add(new PerformedMovement
        {
            RecordingId = r.Id,
            MovementId = m1.Id,
            AudioFileId = a2.Id,
            Order = 2
        });
        db.Set<PerformedMovement>().Add(new PerformedMovement
        {
            RecordingId = r.Id,
            MovementId = m2.Id,
            AudioFileId = a1.Id,
            Order = 1
        });
        await db.SaveChangesAsync();

        IPlayableResolutionService resolver = scope.ServiceProvider.GetRequiredService<IPlayableResolutionService>();

        Recording playable = new Recording { Id = r.Id, WorkId = r.WorkId, Conductor = r.Conductor };
        IReadOnlyList<int> ids = await resolver.ResolveAudioFileIdsAsync(playable);

        ids.Should().Equal(new[] { a1.Id, a2.Id });
    }

    [Fact]
    public async Task PlaylistBuilder_ReturnsPathsInResolverOrder()
    {
        IServiceProvider provider = SqliteInMemory.BuildTestServices();
        using IServiceScope scope = provider.CreateScope();

        Classify.Data.Context.ClassifyContext db = scope.ServiceProvider.GetRequiredService<Classify.Data.Context.ClassifyContext>();

        Composer c = new Composer { Name = "Bach" };
        db.Add(c);
        await db.SaveChangesAsync();

        Work w = new Work { Name = "Goldberg Variations", ComposerId = c.Id, CatalogNumber = "BWV 988" };
        db.Add(w);
        await db.SaveChangesAsync();

        Movement m1 = new Movement { Name = "Aria", Order = 1, WorkId = w.Id };
        Movement m2 = new Movement { Name = "Variation 1", Order = 2, WorkId = w.Id };
        Recording r = new Recording { Conductor = "Gould", WorkId = w.Id };
        AudioFile first = new AudioFile { Path = "/music/first.mp3" };
        AudioFile second = new AudioFile { Path = "/music/second.mp3" };

        db.Add(m1);
        db.Add(m2);
        db.Add(r);
        db.Add(first);
        db.Add(second);
        await db.SaveChangesAsync();

        db.Set<PerformedMovement>().Add(new PerformedMovement
        {
            RecordingId = r.Id,
            MovementId = m1.Id,
            AudioFileId = first.Id,
            Order = 1
        });
        db.Set<PerformedMovement>().Add(new PerformedMovement
        {
            RecordingId = r.Id,
            MovementId = m2.Id,
            AudioFileId = second.Id,
            Order = 2
        });
        await db.SaveChangesAsync();

        IPlayablePlaylistService playlists = scope.ServiceProvider.GetRequiredService<IPlayablePlaylistService>();

        Recording playable = new Recording { Id = r.Id, WorkId = r.WorkId, Conductor = r.Conductor };
        IReadOnlyList<string> paths = await playlists.BuildPlaylistPathsAsync(playable);

        paths.Should().Equal(new[] { first.Path, second.Path });
    }

    [Fact]
    public async Task PlayableResolutionService_FallbackAndSummary_PreservesNameAndPhotoKey()
    {
        IServiceProvider provider = SqliteInMemory.BuildTestServices();
        using IServiceScope scope = provider.CreateScope();

        IPlayableResolutionService resolver = scope.ServiceProvider.GetRequiredService<IPlayableResolutionService>();

        // Use the helper FakePlayable defined at class scope for the fallback branch
        FakePlayable fp = new FakePlayable { Name = "My Playable", PhotoKey = "photo-123" };

        IReadOnlyList<int> audioIds = await resolver.ResolveAudioFileIdsAsync(fp);
        audioIds.Should().BeEmpty();

        PlayableSummary summary = await resolver.ResolveSummaryAsync(fp);
        summary.DisplayName.Should().Be("My Playable");
        summary.PhotoKey.Should().Be("photo-123");
        summary.BaseObject.Should().BeSameAs(fp.BaseObject);
    }
}

