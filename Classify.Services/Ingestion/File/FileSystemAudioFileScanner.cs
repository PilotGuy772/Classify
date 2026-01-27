using Classify.Core.Domain;
using Classify.Core.Enums;
using Classify.Core.Interfaces.Service;
using K4os.Hash.xxHash;

namespace Classify.Services.Ingestion.File;

public class FileSystemAudioFileScanner : IAudioFileScanner
{
    public async Task<IEnumerable<AudioFile>> ScanAudioFilesAsync(string path)
    {
        string[] files = Directory.GetFiles(path);
        List<AudioFile> audioFiles = [];
        
        foreach (string f in files)
        {
            ulong hash = await ComputeHash(f);
            audioFiles.Add(new AudioFile
            {
                Path = f,
                Hash = hash,
                Status = IngestionStatus.Seen
            });
        }

        return audioFiles;
    }
    
    private static async Task<ulong> ComputeHash(string filePath)
    {
        byte[] stream = await System.IO.File.ReadAllBytesAsync(filePath);
        XXH64 xx = new();
        xx.Update(stream);
        return xx.Digest();
    }
}