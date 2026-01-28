using System.Diagnostics.Eventing.Reader;
using Classify.Core.Domain;
using Classify.Core.Interfaces.Service;
using Classify.Services.Ingestion.File;
using FluentAssertions;

namespace Tests.Services.Ingestion;

public class FileScanning
{
    [Fact]
    public async Task ScanFiles_FromFileSystem_CreateNewAudioFileObjects()
    {
        // Arrange
        string filePath = Path.Join(Directory.GetParent(Directory
            .GetParent(Directory.GetParent(Path.GetFullPath("."))!.FullName)!.FullName)!.FullName, "/Services/Ingestion/TestFiles/");
        string[] testFiles =
        [
            "beethoven_5th_symphony_mvmt_1.mp3",
            "Sibelius Violin_Concerto Hilary Hahn.flac",
            "VivaldiFourSeasonsWinterMvmt1ApollosFire.wav"
        ];

        IAudioFileScanner scanner = new FileSystemAudioFileScanner();
        // no database or additional services needed for this
        
        // Act
        AudioFile[] audioFiles = (await scanner.ScanAudioFilesAsync(filePath)).ToArray();
        
        // Assert
        audioFiles.Should().HaveCount(3);
        audioFiles.Select(af => Path.GetFileName(af.Path)).Should().Contain(testFiles);
    }
}