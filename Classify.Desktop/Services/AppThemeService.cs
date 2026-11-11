using Avalonia;
using Avalonia.Styling;
using Classify.Core.Interfaces.Infrastructure;

namespace Classify.Desktop.Services;

public static class AppThemeService
{
    public static void Apply(IAppSettings appSettings)
    {
        if (Application.Current is null)
        {
            return;
        }

        Application.Current.RequestedThemeVariant = appSettings.Appearance switch
        {
            AppAppearance.System => ThemeVariant.Default,
            AppAppearance.Light => ThemeVariant.Light,
            AppAppearance.Dark => ThemeVariant.Dark,
            _ => ThemeVariant.Default,
        };
    }
}
