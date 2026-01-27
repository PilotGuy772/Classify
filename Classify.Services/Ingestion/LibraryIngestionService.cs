using Classify.Core.Domain;
using Classify.Core.Interfaces;
using Classify.Core.Interfaces.Service;

namespace Classify.Services.Ingestion;

public class LibraryIngestionService(IAudioFileScanner audioFileScanner, IUnitOfWork unitOfWork) : IIngestionService
{
    private readonly IAudioFileScanner _audioFileScanner = audioFileScanner;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task ScanLibraryAsync(string path)
    {
        IEnumerable<AudioFile> audioFiles = await _audioFileScanner.ScanAudioFilesAsync(path);
        foreach (AudioFile af in audioFiles)
        {
            if (await _unitOfWork.AudioFiles.GetAudioFileByPath(af.Path) is null)
                await _unitOfWork.AudioFiles.AddAsync(af);
        }

        await _unitOfWork.SaveChangesAsync();
        // transaction complete
    }
}