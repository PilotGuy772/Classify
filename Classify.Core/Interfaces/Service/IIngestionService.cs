namespace Classify.Core.Interfaces.Service;

public interface IIngestionService
{
    public Task ScanLibraryAsync(string path);
}