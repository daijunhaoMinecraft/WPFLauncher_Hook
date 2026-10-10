using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using Mcl.Core.Dotnetdetour.Models.Config;
using Mcl.Core.Dotnetdetour.Services.Mods;
using Mcl.Core.Dotnetdetour.UI.Controls;
using Newtonsoft.Json.Linq;

internal static partial class Program
{
    private static void ModInjectionDirectoryTests()
    {
        var previousDirectory = Directory.GetCurrentDirectory();
        var area = Path.Combine(_output, "mod-directory-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(area);
        try
        {
            Directory.SetCurrentDirectory(area);
            var expected = Path.Combine(area, "ModsInject");
            Check(ModInjectionDirectory.GetPath() == expected && !Directory.Exists(expected), "mod directory lookup is side-effect-free and preserves CWD contract");
            Check(ModInjectionDirectory.EnsureExists() == expected && Directory.Exists(expected), "mod directory preparation creates missing folder");
            Check(ModInjectionDirectory.EnsureExists() == expected, "mod directory preparation can be repeated");
            Directory.Delete(expected); // Empty directory created by this test, never a real mod directory.
            Check(ModInjectionDirectory.GetJarFiles().Length == 0 && Directory.Exists(expected), "game scan recreates removed source directory and returns an empty list");
            File.WriteAllText(Path.Combine(expected, "sample.jar"), "test marker, never executed");
            File.WriteAllText(Path.Combine(expected, "UPPER.JAR"), "test marker");
            File.WriteAllText(Path.Combine(expected, "notes.txt"), "not a mod");
            Directory.CreateDirectory(Path.Combine(expected, "nested"));
            File.WriteAllText(Path.Combine(expected, "nested", "nested.jar"), "not a top-level mod");
            var jars = ModInjectionDirectory.GetJarFiles().Select(Path.GetFileName).OrderBy(name => name).ToArray();
            Check(jars.SequenceEqual(new[] { "sample.jar", "UPPER.JAR" }.OrderBy(name => name)), "mod discovery keeps existing top-level jar filter");
            Check(File.ReadAllText(Path.Combine(expected, "sample.jar")) == "test marker, never executed", "directory preparation does not modify mod content");
        }
        finally { Directory.SetCurrentDirectory(previousDirectory); }

        // Full InitHook cannot be invoked here: it applies network and host side effects.
        // Guard its actual wiring in addition to testing the linked preparation implementation above.
        var init = File.ReadAllText(Path.Combine(_root, "Mcl.Core", "Dotnetdetour", "Features", "GeneralHooks", "InitHook.cs"));
        Check(init.Contains("ModInjectionDirectory.EnsureExists()") && !Regex.IsMatch(init, @"\bProcess\s*\.\s*Start\s*\("), "runtime settings use preparation only, never launch Explorer");
        var config = File.ReadAllText(Path.Combine(_root, "Mcl.Core", "Dotnetdetour", "Models", "Config", "ConfigManager.cs"));
        Check(!Regex.IsMatch(config, @"\bProcess\s*\.\s*Start\s*\("), "configuration persistence has no process-launch action");
        var game = File.ReadAllText(Path.Combine(_root, "Mcl.Core", "Dotnetdetour", "Features", "NetworkAndRoom", "WebRTCEx.cs"));
        Check(game.Contains("ModInjectionDirectory.GetJarFiles()"), "game hook uses shared missing-directory-safe discovery");
    }

    private static void ModInjectionUiTests()
    {
        var original = ConfigManager.GetCurrentConfigValues();
        var previousDirectory = Directory.GetCurrentDirectory();
        var area = Path.Combine(_output, "mod-ui 空格 中文-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(area);
        try
        {
            Directory.SetCurrentDirectory(area);
            ConfigManager.UpdateFromJson("{\"EnableModsInject\":false}");
            var opened = new List<ProcessStartInfo>();
            var saved = 0;
            var panel = new SettingsPanel(() =>
            {
                saved++;
                // Use the same preparation as ApplyRuntimeSettings, without its unrelated host/network work.
                if (WpfConfig.EnableModInjection) ModInjectionDirectory.EnsureExists();
            }, null, opened.Add);
            Render(panel, 860, 680, "settings-mods-before.png");
            var search = Descendants<TextBox>(panel).Single(control => AutomationProperties.GetName(control) == "搜索设置");
            search.Text = "模组注入";
            Render(panel, 860, 680, "settings-mods.png");
            var open = Descendants<Button>(panel).Single(control => AutomationProperties.GetName(control) == "打开模组注入目录");
            var save = Descendants<Button>(panel).Single(control => Equals(control.Content, "保存并应用"));
            var toggle = Descendants<CheckBox>(panel).Single(control => AutomationProperties.GetName(control) == "启用模组注入");
            var port = Descendants<TextBox>(panel).Single(control => AutomationProperties.GetName(control) == "Web服务器端口");
            Check(open.IsEnabled && opened.Count == 0 && !Directory.Exists(ModInjectionDirectory.GetPath()), "constructing settings panel neither opens nor creates mod directory");
            var before = File.ReadAllText(ConfigManager.ConfigFilePath);
            open.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check(opened.Count == 1 && Directory.Exists(ModInjectionDirectory.GetPath()), "explicit open creates directory and invokes launcher once");
            Check(!WpfConfig.EnableModInjection && toggle.IsChecked == false && saved == 0 && before == File.ReadAllText(ConfigManager.ConfigFilePath), "open button does not enable injection or save settings");
            Check(opened[0].UseShellExecute && opened[0].FileName == ModInjectionDirectory.GetPath() && opened[0].Arguments == "", "folder launch preserves Unicode and spaces as a single path without shell arguments");

            toggle.IsChecked = true;
            Check(opened.Count == 1 && !WpfConfig.EnableModInjection, "editing injection toggle alone has no process-launch side effect");
            save.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check(saved == 1 && WpfConfig.EnableModInjection && opened.Count == 1, "enabling and applying injection never opens Explorer");
            Check(JObject.Parse(File.ReadAllText(ConfigManager.ConfigFilePath))["EnableModsInject"].Value<bool>(), "legacy injection JSON key is preserved");
            save.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            port.Text = "54323";
            save.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check(saved == 3 && WpfConfig.HttpPort == 54323 && opened.Count == 1, "repeated save and unrelated save do not reopen folder");
            for (var i = 0; i < 3; i++)
            {
                ConfigManager.Load();
                if (WpfConfig.EnableModInjection) ModInjectionDirectory.EnsureExists();
                ConfigManager.Save();
            }
            Check(opened.Count == 1, "repeated load and runtime preparation do not reopen folder");

            toggle.IsChecked = false;
            save.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            toggle.IsChecked = true;
            save.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check(saved == 5 && WpfConfig.EnableModInjection && opened.Count == 1, "disable then reenable remains quiet");
            before = File.ReadAllText(ConfigManager.ConfigFilePath);
            port.Text = "invalid";
            save.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check(saved == 5 && opened.Count == 1 && File.ReadAllText(ConfigManager.ConfigFilePath) == before, "failed save neither opens folder nor invokes runtime callback");
            port.Text = WpfConfig.HttpPort.ToString();
            open.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check(opened.Count == 2 && File.ReadAllText(ConfigManager.ConfigFilePath) == before, "subsequent explicit click opens once and leaves saved configuration intact");
            Render(panel, 660, 520, "settings-mods-small.png");
            Check(open.ActualWidth > 0 && open.ActualHeight > 0, "manual folder action is visible in small settings panel");

            var failedOpens = 0;
            var failingPanel = new SettingsPanel(() => saved++, null, startInfo =>
            {
                failedOpens++;
                throw new Win32Exception(5, "simulated folder open failure");
            });
            Render(failingPanel, 860, 680, "settings-mods-failure-before.png");
            var failingButton = Descendants<Button>(failingPanel).Single(control => AutomationProperties.GetName(control) == "打开模组注入目录");
            failingButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check(failedOpens == 1 && Descendants<TextBlock>(failingPanel).Any(control => control.Text.StartsWith("无法打开注入目录：")), "shell activation failure is shown inline");
            Check(saved == 5 && WpfConfig.EnableModInjection && File.ReadAllText(ConfigManager.ConfigFilePath) == before, "shell failure does not change already-saved configuration");

            var blocked = Path.Combine(area, "blocked");
            Directory.CreateDirectory(blocked);
            Directory.SetCurrentDirectory(blocked);
            File.WriteAllText(ModInjectionDirectory.GetPath(), "file conflicts with directory name");
            open.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check(opened.Count == 2 && Descendants<TextBlock>(panel).Any(control => control.Text.StartsWith("无法打开注入目录：")), "directory creation failure prevents process launch and shows error");
            Check(File.ReadAllText(ConfigManager.ConfigFilePath) == before, "directory failure does not alter config file");
        }
        finally
        {
            Directory.SetCurrentDirectory(previousDirectory);
            foreach (var entry in ConfigManager.Registry) entry.SetValue(original[entry.Key]);
            ConfigManager.Save();
        }
    }
}
