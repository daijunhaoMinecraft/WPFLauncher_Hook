using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Mcl.Core.Dotnetdetour.Features.GeneralHooks;
using Mcl.Core.Dotnetdetour.UI.Controls;
using Mcl.Core.Dotnetdetour.Utilities.Diagnostics;
using WPFControls.Helpers;
using WPFLauncher.View.SysSetting;

namespace Mcl.Core.Dotnetdetour.UI.Injector;

public static class SettingsInjector
{
    private const string TabHeader = "MCL 扩展设置";
    private const string TabMarker = "MclSettingsTab";

    private static readonly FieldInfo TabControlField = typeof(SysSettingMainPage).GetField("SetTabControl",
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

    private static int _started;

    public static void Start()
    {
        if (Interlocked.Exchange(ref _started, 1) != 0) return;
        _ = WaitForApplicationAsync();
    }

    private static async Task WaitForApplicationAsync()
    {
        while (Application.Current == null) await Task.Delay(1000).ConfigureAwait(false);
        var dispatcher = Application.Current.Dispatcher;
        if (dispatcher.HasShutdownStarted) return;
        await dispatcher.InvokeAsync(new Action(() =>
        {
            var timer = new DispatcherTimer(DispatcherPriority.Background, dispatcher)
                { Interval = TimeSpan.FromSeconds(1) };
            timer.Tick += (sender, args) =>
            {
                try
                {
                    foreach (var window in Application.Current.Windows.OfType<SysSettingMainPage>().ToArray())
                        InjectCustomControls(window);
                }
                catch (Exception exception)
                {
                    PluginLog.Error("UI", exception, "无法创建 MCL 设置页。");
                }
            };
            dispatcher.ShutdownStarted += (sender, args) => timer.Stop();
            timer.Start();
        }));
    }

    private static void InjectCustomControls(SysSettingMainPage window)
    {
        if (!(TabControlField?.GetValue(window) is TabControl tabs) ||
            tabs.Items.OfType<TabItem>().Any(tab => Equals(tab.Tag, TabMarker))) return;

        // Only the tab header uses host styling. Our content has its own scoped resource dictionary.
        var tab = new TabItem
        {
            Tag = TabMarker,
            Header = TabHeader,
            Style = window.TryFindResource("LeftTabItemStyle") as Style,
            Content = new SettingsPanel(InitHook.ApplyRuntimeSettings)
        };
        if (window.TryFindResource("icon29") is ImageSource icon)
            AttachPropertyHelper.SetTabItemImage(tab, icon);
        tabs.Items.Add(tab);

        // The host puts three bottom controls over the first tab-strip column. Grow the
        // normal window by the actual new header height so the appended item stays visible.
        tabs.ApplyTemplate();
        tabs.UpdateLayout();
        var requiredHeight = tab.ActualHeight > 0 ? tab.ActualHeight : 32;
        if (window.WindowState == WindowState.Normal)
        {
            var currentHeight = double.IsNaN(window.Height) || window.Height <= 0
                ? window.ActualHeight
                : window.Height;
            if (currentHeight > 0)
            {
                var targetHeight = currentHeight + requiredHeight + 12;
                window.MinHeight = Math.Max(window.MinHeight, targetHeight);
                window.Height = targetHeight;
            }
        }
    }
}