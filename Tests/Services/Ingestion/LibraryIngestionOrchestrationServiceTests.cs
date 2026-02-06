using System.Threading;
using Classify.Core.Domain;
using Classify.Core.Domain.Infrastructure;
using Classify.Core.Enums;
using Classify.Core.Interfaces.Infrastructure;
using Classify.Core.Interfaces.Service;
using Classify.Services.Ingestion;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;

namespace Tests.Services.Ingestion;

public class LibraryIngestionOrchestrationServiceTests
{
    [Fact]
    public async Task StartScanAsync_CallsScannerAndSetsCompletedOnSuccess()
    {
        // Arrange
        Mock<IIngestionService> ingestionMock = new();
        ingestionMock
            .Setup(s => s.ScanLibraryAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        IServiceProvider services = SqliteInMemory.BuildTestServices();
        using IServiceScope scope = services.CreateScope();
        IUnitOfWork uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        IIngestionOrchestrationService orchestration = new LibraryIngestionOrchestrationService(
            ingestionMock.Object,
            uow,
            Options.Create(new AppSettings { LibraryPath = "/music" })
        );

        LibraryScanState? observedState = null;
        orchestration.ScanStateChanged += s => observedState = s;

        // Act
        await orchestration.StartScanAsync(CancellationToken.None);

        // Assert
        ingestionMock.Verify(s => s.ScanLibraryAsync("/music"), Times.Once);
        observedState.Should().Be(LibraryScanState.Completed);
        orchestration.State.Should().Be(LibraryScanState.Completed);
    }

    [Fact]
    public async Task StartScanAsync_SetsCanceledWhenScannerThrowsOperationCanceled()
    {
        // Arrange
        Mock<IIngestionService> ingestionMock = new();
        ingestionMock
            .Setup(s => s.ScanLibraryAsync(It.IsAny<string>()))
            .ThrowsAsync(new OperationCanceledException())
            .Verifiable();

        IServiceProvider services = SqliteInMemory.BuildTestServices();
        using IServiceScope scope = services.CreateScope();
        IUnitOfWork uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        IIngestionOrchestrationService orchestration = new LibraryIngestionOrchestrationService(
            ingestionMock.Object,
            uow,
            Options.Create(new AppSettings { LibraryPath = "/music" })
        );

        LibraryScanState? observedState = null;
        orchestration.ScanStateChanged += s => observedState = s;

        // Act
        await orchestration.StartScanAsync(CancellationToken.None);

        // Assert
        observedState.Should().Be(LibraryScanState.Canceled);
        orchestration.State.Should().Be(LibraryScanState.Canceled);
    }

    [Fact]
    public async Task StartScanAsync_SetsFailedOnExceptionAndRethrows()
    {
        // Arrange
        Mock<IIngestionService> ingestionMock = new();
        ingestionMock
            .Setup(s => s.ScanLibraryAsync(It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        IServiceProvider services = SqliteInMemory.BuildTestServices();
        using IServiceScope scope = services.CreateScope();
        IUnitOfWork uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        IIngestionOrchestrationService orchestration = new LibraryIngestionOrchestrationService(
            ingestionMock.Object,
            uow,
            Options.Create(new AppSettings { LibraryPath = "/music" })
        );

        // Act / Assert
        await FluentActions.Invoking(() => orchestration.StartScanAsync(CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("boom");

        orchestration.State.Should().Be(LibraryScanState.Failed);
    }

    [Fact]
    public async Task AcceptProposedMatchAsync_CreatesEntitiesWhenIdsAreNull()
    {
        // Arrange - use real DB via sqlite in memory
        IServiceProvider services = SqliteInMemory.BuildTestServices();
        using IServiceScope scope = services.CreateScope();
        IUnitOfWork uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        // add audio file
        AudioFile audio = new() { Path = "/music/a.flac", Hash = 1, Status = IngestionStatus.Seen };
        await uow.AudioFiles.AddAsync(audio);
        await uow.SaveChangesAsync();

        // add proposed match (no FK ids set)
        ProposedMatch pm = new()
        {
            AudioFileId = audio.Id,
            ComposerName = "Beethoven",
            WorkTitle = "Symphony No.5",
            CatalogNumber = "Op.67",
            ConductorName = "Conductor",
            MovementNumber = 1,
            MovementTitle = "Allegro",
            PerformanceOrder = 1,
            Source = "Manual",
            ConfidenceScore = 0.99f,
            Confirmed = false
        };

        await uow.ProposedMatch.AddAsync(pm);
        await uow.SaveChangesAsync();

        IIngestionOrchestrationService orchestration = new LibraryIngestionOrchestrationService(
            Mock.Of<IIngestionService>(),
            uow,
            Options.Create(new AppSettings { LibraryPath = "/music" })
        );

        // Act
        await orchestration.AcceptProposedMatchAsync(pm.Id, CancellationToken.None);

        // Assert - composers, works, movements, recordings and performed movement inserted
        IEnumerable<Composer> composers = await uow.Composers.GetAllAsync();
        composers.Should().ContainSingle(c => c.Name == "Beethoven");

        IEnumerable<Work> works = await uow.Works.GetAllAsync();
        works.Should().ContainSingle(w => w.Name == "Symphony No.5");

        IEnumerable<Movement> movements = await uow.Movements.GetAllAsync();
        movements.Should().ContainSingle(m => m.Name == "Allegro");

        IEnumerable<Recording> recordings = await uow.Recordings.GetAllAsync();
        recordings.Should().ContainSingle(r => r.Conductor == "Conductor");

        IEnumerable<PerformedMovement> pms = await uow.PerformedMovements.GetAllAsync();
        pms.Should().ContainSingle(pm2 => pm2.AudioFileId == audio.Id && pm2.Order == 1);

        ProposedMatch? updated = await uow.ProposedMatch.GetByIdAsync(pm.Id);
        updated.Should().NotBeNull();
        updated!.Confirmed.Should().BeTrue();
    }

    [Fact]
    public async Task AcceptProposedMatchAsync_UsesExistingIdsWhenProvided()
    {
        // Arrange - use real DB via sqlite in memory
        IServiceProvider services = SqliteInMemory.BuildTestServices();
        using IServiceScope scope = services.CreateScope();
        IUnitOfWork uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        // add audio file
        AudioFile audio = new() { Path = "/music/b.flac", Hash = 2, Status = IngestionStatus.Seen };
        await uow.AudioFiles.AddAsync(audio);
        await uow.SaveChangesAsync();

        // add existing composer/work/movement/recording
        Composer existingComposer = new() { Name = "Mozart" };
        await uow.Composers.AddAsync(existingComposer);
        await uow.SaveChangesAsync();

        Work existingWork = new() { Name = "Requiem", ComposerId = existingComposer.Id };
        await uow.Works.AddAsync(existingWork);
        await uow.SaveChangesAsync();

        Movement existingMovement = new() { Name = "Lacrimosa", Order = 1, WorkId = existingWork.Id };
        await uow.Movements.AddAsync(existingMovement);
        await uow.SaveChangesAsync();

        Recording existingRecording = new() { Conductor = "Someone", WorkId = existingWork.Id };
        await uow.Recordings.AddAsync(existingRecording);
        await uow.SaveChangesAsync();

        // add proposed match pointing to existing ids
        ProposedMatch pm = new()
        {
            AudioFileId = audio.Id,
            ComposerId = existingComposer.Id,
            WorkId = existingWork.Id,
            MovementId = existingMovement.Id,
            RecordingId = existingRecording.Id,
            PerformanceOrder = 2,
            Source = "Manual",
            ConfidenceScore = 0.5f
        };

        await uow.ProposedMatch.AddAsync(pm);
        await uow.SaveChangesAsync();

        IIngestionOrchestrationService orchestration = new LibraryIngestionOrchestrationService(
            Mock.Of<IIngestionService>(),
            uow,
            Options.Create(new AppSettings { LibraryPath = "/music" })
        );

        // Act
        await orchestration.AcceptProposedMatchAsync(pm.Id, CancellationToken.None);

        // Assert that a performed movement was created referencing the existing movement and recording
        IEnumerable<PerformedMovement> pms = await uow.PerformedMovements.GetAllAsync();
        pms.Should().ContainSingle(p => p.AudioFileId == audio.Id && p.MovementId == existingMovement.Id && p.RecordingId == existingRecording.Id && p.Order == 2);

        ProposedMatch? updated = await uow.ProposedMatch.GetByIdAsync(pm.Id);
        updated.Should().NotBeNull();
        updated!.Confirmed.Should().BeTrue();
    }
}
