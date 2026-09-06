using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using Mcl.Core.Dotnetdetour.Models.Config;
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
    private readonly CheckBox _disableUpdates;
    private readonly CheckBox _developmentUpdates;
    private readonly Action _onSaved;

    public SettingsPanel(Action onSaved = null)
    {
        _onSaved = onSaved;
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
        _disableUpdates = new CheckBox
        {
            Content = "禁用自动更新检查", IsChecked = UpdateManager.CurrentConfig.DisableUpdate,
            Margin = new Thickness(0, 8, 0, 8)
        };
        _developmentUpdates = new CheckBox
        {
            Content = "接收开发版更新（未勾选时使用稳定版）",
            IsChecked = UpdateManager.CurrentConfig.IsBuildChannel, Margin = new Thickness(0, 8, 0, 8)
        };
        updateContent.Children.Add(_disableUpdates);
        updateContent.Children.Add(_developmentUpdates);

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
            Grid.SetRow(editor, 1);
            Grid.SetColumnSpan(editor, 2);
        }

        AutomationProperties.SetName(editor, entry.Description);
        AutomationProperties.SetHelpText(editor, entry.HelpText ?? entry.Key);
        editor.ToolTip = entry.HelpText ?? entry.Description;
        _editors[entry] = editor;
        grid.Children.Add(editor);
        return new Border { Child = grid, Style = (Style)FindResource("Card"), Margin = new Thickness(0, 0, 0, 6) };
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
            UpdateManager.CurrentConfig.DisableUpdate = _disableUpdates.IsChecked == true;
            UpdateManager.CurrentConfig.IsBuildChannel = _developmentUpdates.IsChecked == true;
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