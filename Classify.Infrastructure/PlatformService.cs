using System;
using Classify.Core.Interfaces.Infrastructure;

namespace Classify.Infrastructure;

public class PlatformService : IPlatformService
{
    public PlatformService()
    {
        if (OperatingSystem.IsMacOS())
        {
            Current = PlatformKind.MacOS;
        }
        else if (OperatingSystem.IsLinux())
        {
            Current = PlatformKind.Linux;
        }
        else if (OperatingSystem.IsWindows())
        {
            Current = PlatformKind.Windows;
        }
        else
        {
            Current = PlatformKind.Unknown;
        }
    }

    public PlatformKind Current { get; }

    public bool IsMacOS => Current == PlatformKind.MacOS;
    public bool IsLinux => Current == PlatformKind.Linux;
    public bool IsWindows => Current == PlatformKind.Windows;
}

