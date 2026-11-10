using Classify.Core.Domain;
using Classify.Core.Enums;
using Classify.Core.Interfaces.Service;
using K4os.Hash.xxHash;

namespace Classify.Services.Ingestion.File;

public class FileSystemAudioFileScanner : IAudioFileScanner
{
    public async Task<IEnumerable<AudioFile>> ScanAudioFilesAsync(string path, string extensions)
    {
        string[] acceptedExtensions = extensions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        IEnumerable<string> files = Directory.GetFiles(path)
            .Where(f => acceptedExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase));
        List<AudioFile> audioFiles = [];

        foreach (string file in files)
        {
            ulong hash = await ComputeHash(file);
            string relativePath = file.ToRelativeLibraryPath(path);
            audioFiles.Add(new AudioFile
            {
                Path = relativePath,
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