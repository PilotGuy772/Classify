namespace Classify.Core.Interfaces.Infrastructure;

public enum PlatformKind
{
    MacOS,
    Linux,
    Windows,
    Unknown
}

public interface IPlatformService
{
    PlatformKind Current { get; }
    bool IsMacOS { get; }
    bool IsLinux { get; }
    bool IsWindows { get; }
}

