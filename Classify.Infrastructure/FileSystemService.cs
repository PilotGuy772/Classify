using System;
using System.Diagnostics;
using System.IO;
using Classify.Core.Interfaces.Infrastructure;

namespace Classify.Infrastructure;

public class FileSystemService : IFileSystemService
{
    private readonly IPlatformService _platformService;

    public FileSystemService(IPlatformService platformService)
    {
        _platformService = platformService ?? throw new ArgumentNullException(nameof(platformService));
    }

    public bool TryRevealInFileManager(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !_platformService.IsMacOS)
        {
            return false;
        }

        if (!File.Exists(path) && !Directory.Exists(path))
        {
            return false;
        }

        try
        {
            using Process process = new();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "open",
                UseShellExecute = false,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
            };
            process.StartInfo.ArgumentList.Add("-R");
            process.StartInfo.ArgumentList.Add(path);
            return process.Start();
        }
        catch
        {
            return false;
        }
    }

    public string? PickDirectory(string? currentPath)
    {
        if (!_platformService.IsMacOS)
        {
            return null;
        }

        string defaultLocation = GetDefaultLocation(currentPath);
        string script = $"set selected to choose folder default location POSIX file \"{EscapeAppleScriptString(defaultLocation)}\" with prompt \"Select a folder\"\nreturn POSIX path of selected";
        return RunAppleScript(script);
    }

    public string? PickFile(string? currentPath)
    {
        if (!_platformService.IsMacOS)
        {
            return null;
        }

        string defaultLocation = GetDefaultLocation(currentPath);
        string script = $"set selected to choose file default location POSIX file \"{EscapeAppleScriptString(defaultLocation)}\" with prompt \"Select a file\"\nreturn POSIX path of selected";
        return RunAppleScript(script);
    }

    private static string GetDefaultLocation(string? currentPath)
    {
        if (!string.IsNullOrWhiteSpace(currentPath))
        {
            string directory = Path.GetDirectoryName(currentPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                return directory;
            }
        }

        return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    }

    private static string RunAppleScript(string script)
    {
        try
        {
            using Process process = new();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "/usr/bin/osascript",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };

            process.StartInfo.ArgumentList.Add("-e");
            process.StartInfo.ArgumentList.Add(script);
            process.Start();

            string output = process.StandardOutput.ReadToEnd().Trim();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(output))
            {
                return null;
            }

            return output;
        }
        catch
        {
            return null;
        }
    }

    private static string EscapeAppleScriptString(string value)
        => value.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
