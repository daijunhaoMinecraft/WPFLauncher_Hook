using System;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;
using Mcl.Core.Dotnetdetour.Models.Config;
using Mcl.Core.Dotnetdetour.UI.Controls;
using Mcl.Core.Dotnetdetour.Utilities.Diagnostics;
using WPFLauncher.View.Launcher.GameInfo;

namespace Mcl.Core.Dotnetdetour.Features.NetworkAndRoom;

/// <summary>
/// Adds the desktop cloud-save controls to the host's local-multiplayer
/// information panel. The panel reads the selected room at click time, so it
/// remains correct when the list selection changes.
/// </summary>
internal sealed class LanGameCloudSaveIntegration : IMethodHook
{
    private static readonly ConditionalWeakTable<CreateLanGameInfoUserControl, LanCloudSavePanel> Panels =
        new ConditionalWeakTable<CreateLanGameInfoUserControl, LanCloudSavePanel>();

    [OriginalMethod]
    private static void CreateLanGameInfoOriginal(CreateLanGameInfoUserControl instance)
    {
    }

    [HookMethod("WPFLauncher.View.Launcher.GameInfo.CreateLanGameInfoUserControl", ".ctor",
        "CreateLanGameInfoOriginal")]
    private static void CreateLanGameInfoHook(CreateLanGameInfoUserControl instance)
    {
        CreateLanGameInfoOriginal(instance);
        if (instance == null || !WpfConfig.EnableLanCloudSave) return;

        instance.Loaded += OnLoaded;
    }

    private static void OnLoaded(object sender, RoutedEventArgs args)
    {
        var control = sender as CreateLanGameInfoUserControl;
        if (control == null) return;
        control.Loaded -= OnLoaded;

        try
        {
            AttachPanel(control);
        }
        catch (Exception exception)
        {
            PluginLog.Error("CloudSave", exception,
                "Attaching the LAN cloud-save controls failed.");
        }
    }

    private static void AttachPanel(CreateLanGameInfoUserControl control)
    {
        LanCloudSavePanel existing;
        if (Panels.TryGetValue(control, out existing)) return;

        var root = control.Content as DependencyObject;
        var bottom = FindBottomStack(root);
        if (bottom == null)
        {
            PluginLog.Debug("CloudSave",
                "The LAN information panel layout did not contain the expected bottom stack.");
            return;
        }

        var panel = new LanCloudSavePanel { Margin = new Thickness(0, 6, 0, 0) };
        bottom.Children.Insert(0, panel);
        Panels.Add(control, panel);
        control.DataContextChanged += (sender, args) =>
            panel.SetGame(LanCloudSaveService.GetSelectedGame(control.DataContext));
        panel.SetGame(LanCloudSaveService.GetSelectedGame(control.DataContext));
    }

    private static StackPanel FindBottomStack(DependencyObject root)
    {
        if (root == null) return null;

        var stack = root as StackPanel;
        if (stack != null && Grid.GetRow(stack) == 5) return stack;

        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var index = 0; index < count; index++)
        {
            var result = FindBottomStack(VisualTreeHelper.GetChild(root, index));
            if (result != null) return result;
        }

        return null;
    }
}
