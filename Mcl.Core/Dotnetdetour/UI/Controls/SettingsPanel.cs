using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.IO;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using Mcl.Core.Dotnetdetour.Models.Config;
using Mcl.Core.Dotnetdetour.Services.Bedrock;
using Mcl.Core.Dotnetdetour.Services.Mods;
using Mcl.Core.Dotnetdetour.UI.Themes;
using Mcl.Core.Updater;

namespace Mcl.Core.Dotnetdetour.UI.Controls;

/// <summary>The startup dialog and injected host tab share this editor and its validation path.</summary>
public sealed class SettingsPanel : UserControl
{
    private readonly Dictionary<ConfigEntry, FrameworkElement> _editors =
        new Dictionary<ConfigEntry, FrameworkElement>();

    private readonly Dictionary<ConfigEntry, FrameworkElement> _rows = new Dictionary<ConfigEntry, FrameworkElement>();
    private readonly Dictionary<string, FrameworkElement> _sections = new Dictionary<string, FrameworkElement>();
    private readonly TextBox _search;
    private readonly ListBox _navigation;
    private readonly TextBlock _status;
    private readonly CheckBox _disableLauncherUpdates;
    private readonly CheckBox _disableHookUpdates;
    private readonly ComboBox _hookUpdateChannel;
    private readonly Action _onSaved;
    private readonly Func<ConfigEntry, string, string> _selectFolder;
    private readonly Action<ProcessStartInfo> _startFolderProcess;
    private readonly Func<string, bool> _confirmClearPackcache;

    public SettingsPanel(Action onSaved = null, Func<ConfigEntry, string, string> selectFolder = null)
        : this(onSaved, selectFolder, StartFolderProcess, null)
    {
    }

    internal SettingsPanel(Action onSaved, Func<ConfigEntry, string, string> selectFolder,
        Action<ProcessStartInfo> startFolderProcess, Func<string, bool> confirmClearPackcache = null)
    {
        _onSaved = onSaved;
        _selectFolder = selectFolder ?? SelectFolder;
        _startFolderProcess = startFolderProcess ?? throw new ArgumentNullException(nameof(startFolderProcess));
        _confirmClearPackcache = confirmClearPackcache ?? ConfirmClearPackcache;
        FluentTheme.Apply(this);

        var layout = new Grid { Margin = new Thickness(24) };
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        layout.Children.Add(new TextBlock { Text = "MCL 设置", Style = (Style)FindResource("PageTitle") });

        _search = new TextBox { ToolTip = "搜索设置名称、说明或字段名" };
        AutomationProperties.SetName(_search, "搜索设置");
        var searchBox = new Grid { Margin = new Thickness(0, 8, 0, 20) };
        searchBox.Children.Add(_search);
        var searchHint = new TextBlock
        {
            Text = "搜索设置名称、说明或字段名", Margin = new Thickness(12, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center, IsHitTestVisible = false,
            Style = (Style)FindResource("SecondaryText")
        };
        searchBox.Children.Add(searchHint);
        Grid.SetRow(searchBox, 1);
        layout.Children.Add(searchBox);

        var body = new Grid();
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(154) });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Grid.SetRow(body, 2);
        layout.Children.Add(body);

        _navigation = new ListBox
        {
            BorderThickness = new Thickness(0), Background = System.Windows.Media.Brushes.Transparent,
            Margin = new Thickness(0, 0, 16, 0), ItemContainerStyle = (Style)FindResource("NavigationItem")
        };
        AutomationProperties.SetName(_navigation, "设置分类");
        _navigation.Items.Add("全部设置");
        foreach (var category in ConfigManager.Registry.Select(entry => entry.Category).Distinct())
            _navigation.Items.Add(category);
        _navigation.Items.Add("更新");
        body.Children.Add(_navigation);

        var stack = new StackPanel();
        var scroll = new ScrollViewer
        {
            Content = stack, VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Padding = new Thickness(0, 0, 8, 0)
        };
        Grid.SetColumn(scroll, 1);
        body.Children.Add(scroll);
        foreach (var group in ConfigManager.Registry.GroupBy(entry => entry.Category))
        {
            var section = CreateSection(group.Key, out var content);
            _sections[group.Key] = section;
            stack.Children.Add(section);
            foreach (var entry in group)
            {
                var card = CreateSettingCard(entry);
                content.Children.Add(card);
                _rows[entry] = card;
            }
        }

        var updates = CreateSection("更新", out var updateContent);
        _sections["更新"] = updates;
        stack.Children.Add(updates);
        _disableLauncherUpdates = new CheckBox
        {
            Content = "禁用启动器更新检查（官方 patchlist）",
            IsChecked = UpdateManager.CurrentConfig.DisableLauncherUpdate,
            Margin = new Thickness(0, 8, 0, 8)
        };
        _disableHookUpdates = new CheckBox
        {
            Content = "禁用 Mcl.Core.dll 自更新检查（GitHub）",
            IsChecked = UpdateManager.CurrentConfig.DisableUpdate,
            Margin = new Thickness(0, 0, 0, 4)
        };
        _hookUpdateChannel = new ComboBox
        {
            Items = { "Build 版（开发构建）", "Release 版（稳定发行）" },
            SelectedIndex = UpdateManager.CurrentConfig.IsBuildChannel ? 0 : 1,
            Width = 240, Margin = new Thickness(28, 0, 0, 8)
        };
        updateContent.Children.Add(_disableLauncherUpdates);
        updateContent.Children.Add(_disableHookUpdates);
        updateContent.Children.Add(_hookUpdateChannel);
        updateContent.Children.Add(new TextBlock
        {
            Text = "两条更新相互独立：启动器渠道由当前启动器自动识别（网易/4399/A50），插件更新走 GitHub。" +
                   "禁用后宿主自带更新同样会被阻止，以保护钩子文件。",
            TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 8)
        });

        var footer = new Grid { Margin = new Thickness(0, 20, 0, 0) };
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        _status = new TextBlock
        {
            Text = "更改将在保存后应用；游戏启动选项在下次启动时生效。",
            Style = (Style)FindResource("SecondaryText"), VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 16, 0)
        };
        AutomationProperties.SetLiveSetting(_status, AutomationLiveSetting.Polite);
        footer.Children.Add(_status);
        var save = new Button { Content = "保存并应用", MinWidth = 120, Style = (Style)FindResource("PrimaryButton") };
        save.Click += SaveClicked;
        Grid.SetColumn(save, 1);
        footer.Children.Add(save);
        Grid.SetRow(footer, 3);
        layout.Children.Add(footer);
        Content = layout;

        _search.TextChanged += (sender, args) =>
        {
            searchHint.Visibility = _search.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
            FilterSettings();
        };
        _navigation.SelectionChanged += (sender, args) =>
        {
            FilterSettings();
            scroll.ScrollToTop();
        };
        _navigation.SelectedIndex = 1;
    }

    private StackPanel CreateSection(string title, out StackPanel content)
    {
        var section = new StackPanel { Margin = new Thickness(0, 0, 0, 20) };
        section.Children.Add(new TextBlock
        {
            Text = title, FontSize = 18, FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 12)
        });
        content = new StackPanel();
        section.Children.Add(content);
        return section;
    }

    private Border CreateSettingCard(ConfigEntry entry)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var label = new StackPanel
            { Margin = new Thickness(0, 0, 12, 0), VerticalAlignment = VerticalAlignment.Center };
        label.Children.Add(new TextBlock
            { Text = entry.Description, TextWrapping = TextWrapping.Wrap, FontWeight = FontWeights.SemiBold });
        if (!string.IsNullOrWhiteSpace(entry.HelpText))
            label.Children.Add(new TextBlock
            {
                Text = entry.HelpText, Margin = new Thickness(0, 5, 0, 0), Style = (Style)FindResource("SecondaryText")
            });
        grid.Children.Add(label);

        FrameworkElement editor;
        if (entry.FieldType == typeof(bool))
        {
            editor = new CheckBox
            {
                IsChecked = (bool)entry.GetValue(), Style = (Style)FindResource("ToggleSwitch"),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(editor, 1);
        }
        else
        {
            editor = new TextBox
            {
                Text = Convert.ToString(entry.GetValue(), CultureInfo.InvariantCulture),
                Margin = new Thickness(0, 12, 0, 0), MinWidth = 80
            };
            if (entry.Editor == "multiline" || entry.Editor == "mcp-sources")
            {
                var text = (TextBox)editor;
                text.AcceptsReturn = true;
                text.TextWrapping = TextWrapping.Wrap;
                text.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
                text.MinHeight = 100;
                text.MaxHeight = 180;
            }

            Grid.SetRow(editor, 1);
            Grid.SetColumnSpan(editor, 2);
        }

        AutomationProperties.SetName(editor, entry.Description);
        AutomationProperties.SetHelpText(editor, entry.HelpText ?? entry.Key);
        editor.ToolTip = entry.HelpText ?? entry.Description;
        _editors[entry] = editor;
        grid.Children.Add(editor);
        if (entry.FieldName == nameof(WpfConfig.EnableModInjection))
        {
            var open = new Button
            {
                Content = "打开注入目录", Margin = new Thickness(0, 12, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                ToolTip = "打开 ModsInject 文件夹；不会启用模组注入或保存设置。"
            };
            AutomationProperties.SetName(open, "打开模组注入目录");
            open.Click += (sender, args) => OpenModInjectionDirectory();
            Grid.SetRow(open, 1);
            Grid.SetColumnSpan(open, 2);
            grid.Children.Add(open);
        }

        if (entry.Editor == "folder" || entry.Editor == "mcp-sources")
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var actions = new WrapPanel { Margin = new Thickness(0, 8, 0, 0) };
            Grid.SetRow(actions, 2);
            Grid.SetColumnSpan(actions, 2);
            grid.Children.Add(actions);
            var browse = new Button { Content = entry.Editor == "folder" ? "选择文件夹…" : "添加文件夹…" };
            AutomationProperties.SetName(browse, entry.Description + "：选择文件夹");
            browse.Click += (sender, args) => BrowseFolder(entry, (TextBox)editor);
            actions.Children.Add(browse);
            if (entry.FieldName == nameof(WpfConfig.BedrockPackcacheDirectory))
            {
                var clear = new Button
                {
                    Content = "清空目录（修复资源包）", Margin = new Thickness(8, 0, 0, 0),
                    Style = (Style)FindResource("DangerButton"),
                    ToolTip = "删除该 packcache 目录内的全部内容（保留 packcache 文件夹本身），用于修复损坏的资源包；不可撤销，请先退出游戏。"
                };
                AutomationProperties.SetName(clear, "清空 packcache 目录");
                clear.Click += (sender, args) => ClearPackcache((TextBox)editor);
                actions.Children.Add(clear);
            }

            if (entry.Editor == "mcp-sources")
            {
                var files = new Button { Content = "添加 .mcp 文件…", Margin = new Thickness(8, 0, 0, 0) };
                files.Click += (sender, args) => BrowseMcpFiles((TextBox)editor);
                actions.Children.Add(files);
                var clear = new Button { Content = "清空列表", Margin = new Thickness(8, 0, 0, 0) };
                clear.Click += (sender, args) => ((TextBox)editor).Clear();
                actions.Children.Add(clear);
            }
        }

        return new Border { Child = grid, Style = (Style)FindResource("Card"), Margin = new Thickness(0, 0, 0, 6) };
    }

    private void OpenModInjectionDirectory()
    {
        try
        {
            var path = ModInjectionDirectory.EnsureExists();
            // Let Windows open the directory as a single path, without constructing a shell command.
            _startFolderProcess(new ProcessStartInfo { FileName = path, UseShellExecute = true });
            _status.SetResourceReference(TextBlock.ForegroundProperty, "SecondaryTextBrush");
            _status.Text = "已请求打开注入目录：" + path;
        }
        catch (Exception exception)
        {
            _status.SetResourceReference(TextBlock.ForegroundProperty, "DangerBrush");
            _status.Text = "无法打开注入目录：" + exception.Message;
        }
    }

    private static void StartFolderProcess(ProcessStartInfo startInfo)
    {
        // Dispose the managed handle, not the Explorer window; shell activation can return null.
        using (Process.Start(startInfo))
        {
        }
    }

    private void BrowseFolder(ConfigEntry entry, TextBox editor)
    {
        try
        {
            var selected = _selectFolder(entry, editor.Text);
            if (selected == null) return;
            if (entry.FieldName == nameof(WpfConfig.BedrockDirectory)) BedrockPathValidator.Validate(selected);
            if (entry.Editor == "mcp-sources") AppendPaths(editor, new[] { selected });
            else
            {
                BedrockCacheOptions.ValidateEntry(entry.FieldName, selected);
                editor.Text = selected;
            }

            _status.SetResourceReference(TextBlock.ForegroundProperty, "SecondaryTextBrush");
            _status.Text = "已选择路径，点击「保存并应用」后生效。";
        }
        catch (Exception exception)
        {
            _status.SetResourceReference(TextBlock.ForegroundProperty, "DangerBrush");
            _status.Text = exception.Message;
        }
    }

    private void ClearPackcache(TextBox editor)
    {
        try
        {
            var directory = BedrockCacheOptions.FullPath(editor.Text);
            BedrockCacheOptions.ValidateEntry(nameof(WpfConfig.BedrockPackcacheDirectory), directory);
            if (!Directory.Exists(directory))
            {
                _status.SetResourceReference(TextBlock.ForegroundProperty, "DangerBrush");
                _status.Text = "目录不存在：" + directory;
                return;
            }

            if (!_confirmClearPackcache(directory)) return;
            var failures = new List<Exception>();
            var removed = BedrockCacheService.ClearPackcache(directory, failures);
            if (failures.Count > 0)
            {
                _status.SetResourceReference(TextBlock.ForegroundProperty, "DangerBrush");
                _status.Text = $"已删除 {removed} 项，{failures.Count} 项失败（可能被游戏或其它程序占用）。";
            }
            else
            {
                _status.SetResourceReference(TextBlock.ForegroundProperty, "SuccessBrush");
                _status.Text = removed == 0 ? "packcache 目录已为空。" : $"已清空 packcache 目录，共删除 {removed} 项。";
            }
        }
        catch (Exception exception)
        {
            _status.SetResourceReference(TextBlock.ForegroundProperty, "DangerBrush");
            _status.Text = "无法清空 packcache 目录：" + exception.Message;
        }
    }

    private bool ConfirmClearPackcache(string directory)
    {
        var message = "将删除该 packcache 目录内的全部内容（保留 packcache 文件夹本身），用于修复损坏的资源包：\n\n"
                      + directory + "\n\n此操作不可撤销，请先完全退出游戏和启动器。是否继续？";
        var owner = Window.GetWindow(this);
        return (owner == null
                   ? MessageBox.Show(message, "清空 packcache", MessageBoxButton.YesNo, MessageBoxImage.Warning)
                   : MessageBox.Show(owner, message, "清空 packcache", MessageBoxButton.YesNo, MessageBoxImage.Warning))
               == MessageBoxResult.Yes;
    }

    private string SelectFolder(ConfigEntry entry, string current)
    {
        using (var dialog = new System.Windows.Forms.FolderBrowserDialog
               {
                   Description = entry.HelpText ?? entry.Description,
                   SelectedPath = Directory.Exists(current) ? current : "",
                   ShowNewFolderButton = false
               })
        {
            return dialog.ShowDialog(new DialogOwner(Window.GetWindow(this))) == System.Windows.Forms.DialogResult.OK
                ? dialog.SelectedPath
                : null;
        }
    }

    private void BrowseMcpFiles(TextBox editor)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "选择 .mcp 资源文件", Filter = "MCP 文件 (*.mcp)|*.mcp", Multiselect = true, CheckFileExists = true
        };
        var owner = Window.GetWindow(this);
        if ((owner == null ? dialog.ShowDialog() : dialog.ShowDialog(owner)) == true)
            AppendPaths(editor, dialog.FileNames);
    }

    private static void AppendPaths(TextBox editor, IEnumerable<string> paths) => editor.Text =
        string.Join(Environment.NewLine,
            BedrockCacheOptions.Lines(editor.Text).Concat(paths).Distinct(StringComparer.OrdinalIgnoreCase));

    private sealed class DialogOwner : System.Windows.Forms.IWin32Window
    {
        public DialogOwner(Window window) => Handle =
            window == null ? IntPtr.Zero : new System.Windows.Interop.WindowInteropHelper(window).Handle;

        public IntPtr Handle { get; }
    }

    private void FilterSettings()
    {
        var query = _search.Text.Trim();
        var category = _navigation.SelectedItem as string;
        var searching = query.Length > 0;
        foreach (var entry in ConfigManager.Registry)
        {
            var matches = !searching ||
                          (entry.Description + " " + entry.Key + " " + entry.FieldName + " " + entry.HelpText)
                          .IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
            var visible = matches && (searching || category == "全部设置" || category == entry.Category);
            _rows[entry].Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }

        foreach (var section in _sections)
        {
            var visible = section.Key == "更新"
                ? (!searching && (category == "全部设置" || category == "更新")) || (searching && "更新".Contains(query))
                : _rows.Any(pair => pair.Key.Category == section.Key && pair.Value.Visibility == Visibility.Visible);
            section.Value.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void SaveClicked(object sender, RoutedEventArgs args)
    {
        var configurationSaved = false;
        try
        {
            var values = _editors.ToDictionary(pair => pair.Key.Key,
                pair => pair.Value is CheckBox toggle
                    ? (object)(toggle.IsChecked == true)
                    : ((TextBox)pair.Value).Text);
            ConfigManager.Update(values);
            configurationSaved = true;
            UpdateManager.CurrentConfig.DisableLauncherUpdate = _disableLauncherUpdates.IsChecked == true;
            UpdateManager.CurrentConfig.DisableUpdate = _disableHookUpdates.IsChecked == true;
            UpdateManager.CurrentConfig.IsBuildChannel = _hookUpdateChannel.SelectedIndex == 0;
            UpdateManager.SaveConfig();
            _status.SetResourceReference(TextBlock.ForegroundProperty, "SuccessBrush");
            _status.Text = "配置已保存。";
            _onSaved?.Invoke();
        }
        catch (Exception exception)
        {
            _status.SetResourceReference(TextBlock.ForegroundProperty, "DangerBrush");
            _status.Text = (configurationSaved ? "扩展配置已保存，但更新偏好或运行时应用失败：" : "更改未保存：") + exception.Message;
        }
    }
}
