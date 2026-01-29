using Classify.Core.Domain;
using Classify.Core.Enums;
using Classify.Core.Interfaces;
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
            new() { Path = "/music/a.flac", Hash = 1, Status = IngestionStatus.Seen },
            new() { Path = "/music/b.flac", Hash = 2, Status = IngestionStatus.Seen }
        ];

        Mock<IAudioFileScanner> scanner = new();
        scanner
            .Setup(s => s.ScanAudioFilesAsync(It.IsAny<string>()))
            .ReturnsAsync(fakeFiles);

        IServiceProvider services = SqliteInMemory.BuildTestServices();
        IUnitOfWork unitOfWork = services.GetRequiredService<IUnitOfWork>();

        LibraryIngestionService service = new(scanner.Object, unitOfWork);

        // Act`
        await service.ScanLibraryAsync("/music");

        // Assert
        IEnumerable<AudioFile> allFiles = (await unitOfWork.AudioFiles.GetAllAsync()).ToArray();
        
        allFiles.Should().HaveCount(2);
        allFiles.Select(f => f.Path).Should().Contain("/music/a.flac", "/music/b.flac");
    }

    [Fact]
    public async Task ScanLibrary_WithDuplicateFiles_IgnoringExistingFiles()
    {
        // Arrange
        AudioFile[] fakeFiles =
        [
            new() { Path = "/music/a.flac", Hash = 1, Status = IngestionStatus.Seen },
            new() { Path = "/music/b.flac", Hash = 2, Status = IngestionStatus.Seen }
        ];
        
        Mock<IAudioFileScanner> scanner = new();
        scanner
            .Setup(s => s.ScanAudioFilesAsync(It.IsAny<string>()))
            .ReturnsAsync(fakeFiles);

        IServiceProvider services = SqliteInMemory.BuildTestServices();
        IUnitOfWork unitOfWork = services.GetRequiredService<IUnitOfWork>();

        LibraryIngestionService service = new(scanner.Object, unitOfWork);

        // add one to the DB
        await unitOfWork.AudioFiles.AddAsync(fakeFiles[0]);
        await unitOfWork.SaveChangesAsync();
        
        // Act
        await service.ScanLibraryAsync("/music");

        // Assert
        IEnumerable<AudioFile> allFiles = (await unitOfWork.AudioFiles.GetAllAsync()).ToArray();
        
        allFiles.Should().HaveCount(2);
        allFiles.Select(f => f.Path).Should().Contain("/music/a.flac", "/music/b.flac");
    }
}