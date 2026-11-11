using Classify.Core.Domain;
using Classify.Core.Interfaces;
using Classify.Core.Interfaces.Infrastructure;
using Classify.Core.Interfaces.Service;

namespace Classify.Services.Ingestion;

public class LibraryIngestionService(IAudioFileScanner audioFileScanner, IUnitOfWork unitOfWork, IAppSettings appSettings) : IIngestionService
{
    public async Task ScanLibraryAsync(string path)
    {
        IEnumerable<AudioFile> audioFiles = await audioFileScanner.ScanAudioFilesAsync(path, appSettings.AcceptedFileExtensions);
        foreach (AudioFile af in audioFiles)
        {
            string relativePath = af.Path.ToRelativeLibraryPath(appSettings.CurrentLibraryPath);
            af.Path = relativePath;

            if (await unitOfWork.AudioFiles.GetAudioFileByPath(relativePath) is null)
                await unitOfWork.AudioFiles.AddAsync(af);
        }

        await unitOfWork.SaveChangesAsync();
        // transaction complete
    }
}