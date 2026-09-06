using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Mcl.Core.Dotnetdetour.UI.Themes;

public static class FluentTheme
{
    private static readonly Uri ResourceUri = new Uri(
        "/Mcl.Core;component/Dotnetdetour/UI/Themes/Fluent.xaml", UriKind.Relative);

    public static void Apply(FrameworkElement view)
    {
        if (!view.Resources.MergedDictionaries.Any(dictionary => dictionary.Source == ResourceUri))
            view.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = ResourceUri });
        view.UseLayoutRounding = true;
        view.SnapsToDevicePixels = true;
        if (view is Control control)
        {
            control.SetResourceReference(Control.FontFamilyProperty, "UiFont");
            control.SetResourceReference(Control.ForegroundProperty, "TextBrush");
            control.SetResourceReference(Control.BackgroundProperty, "WindowBackgroundBrush");
            control.FontSize = 14;
        }
    }

    public static Brush Brush(FrameworkElement view, string key) => (Brush)view.FindResource(key);
}