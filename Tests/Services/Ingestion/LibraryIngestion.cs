using Classify.Core.Domain;
using Classify.Core.Enums;
using Classify.Core.Interfaces;
using Classify.Core.Interfaces.Infrastructure;
using Classify.Core.Interfaces.Service;
using Classify.Data;
using Classify.Data.Context;
using Classify.Services.Ingestion;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Tests.Services.Ingestion;

public class LibraryIngestion
{
    [Fact]
    public async Task ScanLibrary_WhenFilesAreNew_WithSeenStatus()
    { 
        // Arrange
        AudioFile[] fakeFiles =
        [
            new() { Path = "a.flac", Hash = 1, Status = IngestionStatus.Seen },
            new() { Path = "b.flac", Hash = 2, Status = IngestionStatus.Seen }
        ];

        Mock<IAudioFileScanner> scanner = new();
        scanner
            .Setup(s => s.ScanAudioFilesAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(fakeFiles);

        IServiceProvider services = SqliteInMemory.BuildTestServices();
        IUnitOfWork unitOfWork = services.GetRequiredService<IUnitOfWork>();
        IAppSettings appSettings = services.GetRequiredService<IAppSettings>();

        LibraryIngestionService service = new(scanner.Object, unitOfWork, appSettings);

        // Act
        await service.ScanLibraryAsync("/music");

        // Assert
        IEnumerable<AudioFile> allFiles = (await unitOfWork.AudioFiles.GetAllAsync()).ToArray();

        allFiles.Should().HaveCount(2);
        allFiles.Select(f => f.Path).Should().Contain("a.flac", "b.flac");
    }

    [Fact]
    public async Task ScanLibrary_WithDuplicateFiles_IgnoringExistingFiles()
    {
        // Arrange
        AudioFile[] fakeFiles =
        [
            new() { Path = "a.flac", Hash = 1, Status = IngestionStatus.Seen },
            new() { Path = "b.flac", Hash = 2, Status = IngestionStatus.Seen }
        ];
        
        Mock<IAudioFileScanner> scanner = new();
        scanner
            .Setup(s => s.ScanAudioFilesAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(fakeFiles);

        IServiceProvider services = SqliteInMemory.BuildTestServices();
        IUnitOfWork unitOfWork = services.GetRequiredService<IUnitOfWork>();
        IAppSettings appSettings = services.GetRequiredService<IAppSettings>();

        LibraryIngestionService service = new(scanner.Object, unitOfWork, appSettings);

        // add one to the DB
        await unitOfWork.AudioFiles.AddAsync(fakeFiles[0]);
        await unitOfWork.SaveChangesAsync();
        
        // Act
        await service.ScanLibraryAsync("/music");

        // Assert
        IEnumerable<AudioFile> allFiles = (await unitOfWork.AudioFiles.GetAllAsync()).ToArray();

        allFiles.Should().HaveCount(2);
        allFiles.Select(f => f.Path).Should().Contain("a.flac", "b.flac");
    }
}