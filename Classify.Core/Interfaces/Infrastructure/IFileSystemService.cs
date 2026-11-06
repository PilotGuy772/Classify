namespace Classify.Core.Interfaces.Infrastructure;

public interface IFileSystemService
{
    bool TryRevealInFileManager(string path);
    string? PickDirectory(string? currentPath);
    string? PickFile(string? currentPath);
}
