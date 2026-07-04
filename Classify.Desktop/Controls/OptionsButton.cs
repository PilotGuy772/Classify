using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Classify.Desktop.ViewModels;
using TablerIcons;

namespace Classify.Desktop.Controls;

/// <summary>
/// A right-aligned option button with three horizontal dots colored with the AccentBrush.
/// </summary>
public class OptionsButton : Button
{
    /// <summary>
    /// Defines the <see cref="MenuOptions"/> property.
    /// </summary>
    public static readonly StyledProperty<IEnumerable<MenuOptionViewModel>?> MenuOptionsProperty =
        AvaloniaProperty.Register<OptionsButton, IEnumerable<MenuOptionViewModel>?>(nameof(MenuOptions));

    /// <summary>
    /// Gets or sets the collection of menu options for this button.
    /// </summary>
    public IEnumerable<MenuOptionViewModel>? MenuOptions
    {
        get => GetValue(MenuOptionsProperty);
        set => SetValue(MenuOptionsProperty, value);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OptionsButton"/> class.
    /// </summary>
    public OptionsButton()
    {
        // Configure button appearance and layout
        Background = Avalonia.Media.Brushes.Transparent;
        BorderThickness = new Thickness(0);
        Padding = new Thickness(0);
        Width = 16;
        Height = 16;
        VerticalAlignment = VerticalAlignment.Center;
        HorizontalAlignment = HorizontalAlignment.Center;

        // Create the TablerIcon content
        TablerIcon icon = new()
        {
            Icon = Icons.IconDots,
            Width = 16,
            Height = 16
        };

        // Bind the Brush property of the TablerIcon to the AccentBrush dynamic resource
        icon.Bind(TablerIcon.BrushProperty, new DynamicResourceExtension("AccentBrush"));

        Content = icon;
    }

    /// <summary>
    /// Responds to the button being clicked by opening a native system context menu with the configured options.
    /// </summary>
    protected override void OnClick()
    {
        base.OnClick();

        IEnumerable<MenuOptionViewModel>? options = MenuOptions;
        if (options == null || !options.Any())
        {
            List<MenuOptionViewModel> defaultOptions = [];
            for (int i = 1; i <= 5; i++)
            {
                defaultOptions.Add(new MenuOptionViewModel
                {
                    Header = $"Placeholder Option {i}"
                });
            }
            options = defaultOptions;
        }

        ContextMenu contextMenu = new();

        foreach (MenuOptionViewModel option in options)
        {
            MenuItem item = new()
            {
                Header = option.Header,
                Command = option.Command,
                CommandParameter = option.CommandParameter
            };

            if (option.Icon.HasValue)
            {
                item.Icon = new TablerIcon
                {
                    Icon = option.Icon.Value,
                    Width = 16,
                    Height = 16
                };
            }

            contextMenu.Items.Add(item);
        }

        ContextMenu = contextMenu;
        contextMenu.Open(this);
    }
}
