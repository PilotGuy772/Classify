using Classify.Core.Interfaces.Infrastructure;

namespace Classify.Core.Domain;

/// <summary>
/// Provides helpers for converting audio file paths between library-relative storage and fully qualified paths.
/// </summary>
public static class AudioFilePathExtensions
{
    public static string ToRelativeLibraryPath(this string path, string libraryRoot)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        string normalizedPath = path.Replace('\\', '/');
        if (!Path.IsPathRooted(path) || string.IsNullOrWhiteSpace(libraryRoot))
        {
            return normalizedPath;
        }

        try
        {
            return Path.GetRelativePath(libraryRoot, path).Replace('\\', '/');
        }
        catch
        {
            return normalizedPath;
        }
    }

    public static string ToLibraryPath(this string path, string libraryRoot)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        string normalizedPath = path.Replace('\\', '/');
        if (Path.IsPathRooted(path) || string.IsNullOrWhiteSpace(libraryRoot))
        {
            return normalizedPath;
        }

        return Path.Combine(libraryRoot, normalizedPath.Replace('/', Path.DirectorySeparatorChar));
    }

    public static string GetFullPath(this AudioFile audioFile, IAppSettings appSettings)
    {
        ArgumentNullException.ThrowIfNull(audioFile);
        ArgumentNullException.ThrowIfNull(appSettings);

        return audioFile.Path.ToLibraryPath(appSettings.CurrentLibraryPath);
    }
}
