using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace Classify.Desktop;

public class ViewLocator : IDataTemplate
{
    public Control? Build(object? data)
    {
        if (data is null)
            return null;

        string viewName = data.GetType().FullName!
            .Replace("DialogViewModel", "Dialog")
            .Replace("ViewModel", "View");

        Type? viewType = Type.GetType(viewName);

        return viewType != null
            ? (Control)Activator.CreateInstance(viewType)!
            : new TextBlock { Text = viewName };
    }

    public bool Match(object? data)
        => data is not null && data.GetType().Name.EndsWith("ViewModel");
}