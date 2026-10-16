using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using Mcl.Core.Dotnetdetour.Models.Config;
using Mcl.Core.Dotnetdetour.Services.Bedrock;
using Mcl.Core.Dotnetdetour.UI.Controls;
using Newtonsoft.Json.Linq;

internal static partial class Program
{
    private static void BedrockTests()
    {
        var original = ConfigManager.GetCurrentConfigValues();
        var area = Path.Combine(_output, "bedrock-" + Guid.NewGuid().ToString("N"));
        var cache = Path.Combine(area, "packcache");
        var source = Path.Combine(area, "source");
        var target = Path.Combine(cache, "target");
        Directory.CreateDirectory(source);
        Directory.CreateDirectory(target);
        try
        {
            Check(ConfigManager.Registry.Count == 45, "45 registered settings");
            Check(!WpfConfig.EnableBedrockCacheCleanup && !WpfConfig.EnableBedrockMcpCopy, "cache features default off");
            // 与 WpfConfig 字段和 ConfigManager 帮助文本保持一致（HEAD 中测试曾期望 MinecraftPC_Netease，属笔误）。
            Check(WpfConfig.BedrockPackcacheDirectory == Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "MinecraftPE_Netease", "packcache"), "release packcache default, not PB");
            Check(typeof(WpfConfig).GetField("Version") == null, "no mutable global version");
            // 版本号固定校验：修改 Mcl.Core/Properties/AssemblyInfo.cs 后需同步更新这里。
            Check(WpfConfig.GetVersion() == typeof(WpfConfig).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion &&
                WpfConfig.GetVersion() == "7.0.1-DLL-Public-Beta1", "version comes from linked assembly metadata");
            Check(typeof(WpfConfig).GetProperty("BedrockPath") == null && typeof(WpfConfig).GetProperty("IsDebug") == null, "legacy source aliases removed");
            foreach (var path in new[] { "Minecraft.Windows.exe", @"C:\Games\Minecraft.Windows.exe", @"""C:\Games\MINECRAFT.WINDOWS.EXE""" })
                Check(BedrockCacheService.IsBedrockExecutable(path), "exact Bedrock executable " + path);
            foreach (var path in new[] { null, "", "java.exe", "Minecraft.Windows.exe.bak", @"C:\Minecraft.Windows.exe\other.exe", "FakeMinecraft.Windows.exe" })
                Check(!BedrockCacheService.IsBedrockExecutable(path), "exclude unrelated executable " + path);

            ConfigManager.Update(new Dictionary<string, object> { [nameof(WpfConfig.BedrockPackcacheDirectory)] = cache });
            var malformedCache = new JObject { [nameof(WpfConfig.EnableBedrockCacheCleanup)] = true,
                [nameof(WpfConfig.BedrockPackcacheDirectory)] = area };
            File.WriteAllText(ConfigManager.ConfigFilePath, malformedCache.ToString());
            ConfigManager.Load();
            Check(!WpfConfig.EnableBedrockCacheCleanup && !WpfConfig.EnableBedrockMcpCopy && WpfConfig.BedrockPackcacheDirectory == cache,
                "invalid cache load disables destructive work rather than operating on fallback root");
            ConfigManager.Save();
            var before = File.ReadAllText(ConfigManager.ConfigFilePath);
            foreach (var bad in new[] { "../outside", "*.json", "sub\\file.json", "file:stream", "..", "file." })
                Reject(() => ConfigManager.Update(new Dictionary<string, object> {
                    [nameof(WpfConfig.EnableBedrockCacheCleanup)] = true, [nameof(WpfConfig.BedrockCacheFileNames)] = bad }), "reject unsafe cleanup name " + bad);
            Check(!WpfConfig.EnableBedrockCacheCleanup && File.ReadAllText(ConfigManager.ConfigFilePath) == before, "unsafe targets cannot partially enable deletion");
            Reject(() => ConfigManager.UpdateFromJson("{\"EnableBedrockMcpCopy\":true}"), "copy requires source");
            Reject(() => ConfigManager.Update(new Dictionary<string, object> { [nameof(WpfConfig.BedrockPackcacheDirectory)] = area }), "cache cannot be arbitrary root");
            foreach (var path in new[] { target, cache, area, @"C:relative", @"\\server\share\folder", source + ":stream" })
                Reject(() => ConfigManager.Update(new Dictionary<string, object> { [nameof(WpfConfig.BedrockMcpSources)] = path }), "unsafe or overlapping source " + path);

            File.WriteAllText(Path.Combine(target, "existing.mcp"), "marker");
            File.WriteAllText(Path.Combine(source, "custom.mcp"), "custom-v1");
            Directory.CreateDirectory(Path.Combine(source, "nested"));
            File.WriteAllText(Path.Combine(source, "nested", "nested.mcp"), "nested");
            var removed = Path.Combine(target, "play.entity.json");
            File.WriteAllText(removed, "remove");
            File.WriteAllText(Path.Combine(target, "keep.json"), "keep");
            var folder = Path.Combine(target, "attachables", "nested");
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "data"), "remove");
            var errors = new List<Exception>();
            var disabled = new BedrockCacheWorker(ConfigManager.GetBedrockCacheOptions(), errors.Add);
            disabled.RunPass(() => true, () => true, CancellationToken.None);
            Check(File.Exists(removed), "disabled worker never deletes");
            ConfigManager.Update(new Dictionary<string, object> {
                [nameof(WpfConfig.EnableBedrockCacheCleanup)] = true, [nameof(WpfConfig.EnableBedrockMcpCopy)] = true,
                [nameof(WpfConfig.BedrockMcpSources)] = source });
            ConfigManager.Load();
            Check(WpfConfig.EnableBedrockCacheCleanup && WpfConfig.EnableBedrockMcpCopy && WpfConfig.BedrockMcpSources == source, "new options roundtrip through JSON");
            var worker = new BedrockCacheWorker(ConfigManager.GetBedrockCacheOptions(), errors.Add);
            worker.RunPass(() => true, () => true, CancellationToken.None);
            Check(!File.Exists(removed) && !Directory.Exists(Path.Combine(target, "attachables")), "cleanup matches files and recursive folders");
            Check(File.Exists(Path.Combine(target, "keep.json")), "cleanup keeps unmatched files");
            Check(File.ReadAllText(Path.Combine(target, "custom.mcp")) == "custom-v1" && File.Exists(Path.Combine(target, "nested", "nested.mcp")), "copy merges source contents recursively");
            File.WriteAllText(Path.Combine(source, "custom.mcp"), "custom-v2");
            worker.RunPass(() => true, () => true, CancellationToken.None);
            Check(File.ReadAllText(Path.Combine(target, "custom.mcp")) == "custom-v1" && !Directory.Exists(Path.Combine(target, "nested", "nested")), "copy once and no recursive feedback");
            File.WriteAllText(removed, "regenerated");
            Check(worker.RunPass(() => true, () => true, CancellationToken.None) && !File.Exists(removed),
                "regenerated target removed and pass reports activity");
            var later = Path.Combine(cache, "later");
            Directory.CreateDirectory(later);
            File.WriteAllText(Path.Combine(later, "existing.mcp"), "marker");
            // A locked file exercises failure/retry without permissions or real game files.
            using (var locked = new FileStream(Path.Combine(later, "custom.mcp"), FileMode.Create, FileAccess.Write, FileShare.None))
                worker.RunPass(() => true, () => true, CancellationToken.None);
            Check(errors.Count > 0, "copy failures are reported");
            worker.RunPass(() => true, () => true, CancellationToken.None);
            Check(File.ReadAllText(Path.Combine(later, "custom.mcp")) == "custom-v2", "failed copy retries for later targets");
            File.WriteAllText(removed, "keep-on-stop");
            worker.RunPass(() => false, () => false, CancellationToken.None);
            Check(File.Exists(removed), "switch off or exited process disables mutation");
            using (var canceled = new CancellationTokenSource())
            {
                canceled.Cancel();
                try { worker.RunPass(() => true, () => true, canceled.Token); throw new Exception("Expected cancellation"); }
                catch (OperationCanceledException) { _checks++; }
            }
            Check(File.Exists(removed), "cancellation checked before deleting");
            try { BedrockCacheWorker.EnsureSafePath(cache, source); throw new Exception("Expected boundary rejection"); }
            catch (IOException) { _checks++; }
            ConfigManager.Update(new Dictionary<string, object> {
                [nameof(WpfConfig.BedrockCacheFileNames)] = "", [nameof(WpfConfig.BedrockCacheFolderNames)] = "",
                [nameof(WpfConfig.EnableBedrockMcpCopy)] = false });
            var emptyTargets = new BedrockCacheWorker(ConfigManager.GetBedrockCacheOptions(), errors.Add);
            Check(!emptyTargets.RunPass(() => true, () => true, CancellationToken.None) && File.Exists(removed) &&
                  WpfConfig.BedrockCacheFileNames == "", "empty target list stays empty, not reset to defaults, and reports idle");

            var clearRoot = Path.Combine(area, "clear-area", "packcache");
            Directory.CreateDirectory(Path.Combine(clearRoot, "nested"));
            File.WriteAllText(Path.Combine(clearRoot, "play.entity.json"), "x");
            File.WriteAllText(Path.Combine(clearRoot, "nested", "deep.json"), "y");
            var clearFailures = new List<Exception>();
            var cleared = BedrockCacheService.ClearPackcache(clearRoot, clearFailures);
            Check(cleared == 2 && clearFailures.Count == 0 && Directory.Exists(clearRoot) &&
                  !Directory.EnumerateFileSystemEntries(clearRoot).Any(), "manual clear empties packcache but keeps the root");
            Reject(() => BedrockCacheService.ClearPackcache(source, clearFailures), "manual clear refuses a non-packcache directory");
            Check(BedrockCacheService.ClearPackcache(Path.Combine(area, "missing", "packcache"), clearFailures) == 0,
                "manual clear reports nothing for a missing directory");
            LifecycleAndLinkTests(area, cache, removed, errors);
        }
        finally
        {
            foreach (var entry in ConfigManager.Registry) entry.SetValue(original[entry.Key]);
            ConfigManager.Save();
        }
    }

    private static void LifecycleAndLinkTests(string area, string cache, string removed, List<Exception> errors)
    {
        var values = ConfigManager.GetCurrentConfigValues();
        values[nameof(WpfConfig.EnableBedrockCacheCleanup)] = true;
        values[nameof(WpfConfig.EnableBedrockMcpCopy)] = false;
        values[nameof(WpfConfig.BedrockCacheFileNames)] = "play.entity.json";
        var options = BedrockCacheOptions.FromValues(values);
        BedrockCacheService.RunSessionAsync(() => null, options, () => true, () => false, CancellationToken.None, 1, 3).GetAwaiter().GetResult();
        Check(File.Exists(removed), "never-started process times out without mutation");
        BedrockCacheService.RunSessionAsync(() => false, options, () => true, () => false, CancellationToken.None, 1).GetAwaiter().GetResult();
        Check(File.Exists(removed), "already-exited process does not start file work");
        BedrockCacheService.RunSessionAsync(() => true, options, () => false, () => false, CancellationToken.None, 1).GetAwaiter().GetResult();
        Check(File.Exists(removed), "session exits when configured switches are off");
        var probes = 0;
        using (var deadline = new CancellationTokenSource(2000))
            BedrockCacheService.RunSessionAsync(() => ++probes < 3 ? (bool?)null : File.Exists(removed), options,
                () => true, () => false, deadline.Token, 1).GetAwaiter().GetResult();
        Check(probes >= 3 && !File.Exists(removed), "session waits for start and stops when owning process exits");

        var timed = ConfigManager.GetCurrentConfigValues();
        timed[nameof(WpfConfig.EnableBedrockCacheCleanup)] = true;
        timed[nameof(WpfConfig.EnableBedrockMcpCopy)] = false;
        timed[nameof(WpfConfig.BedrockCacheFileNames)] = "";
        timed[nameof(WpfConfig.BedrockCacheFolderNames)] = "";
        timed[nameof(WpfConfig.BedrockCacheDurationSeconds)] = 1;
        var timedOptions = BedrockCacheOptions.FromValues(timed);
        Check(timedOptions.DurationSeconds == 1, "duration roundtrips into options");
        var alive = true;
        var watch = Stopwatch.StartNew();
        BedrockCacheService.RunSessionAsync(() => alive, timedOptions, () => true, () => false, CancellationToken.None, 1)
            .GetAwaiter().GetResult();
        Check(alive && watch.ElapsedMilliseconds >= 900 && watch.ElapsedMilliseconds < 5000,
            "session stops after configured duration while process stays alive");

        var outside = Path.Combine(area, "outside");
        Directory.CreateDirectory(outside);
        var protectedFile = Path.Combine(outside, "play.entity.json");
        File.WriteAllText(protectedFile, "must stay");
        var link = Path.Combine(cache, "linked-outside");
        if (!CreateSymbolicLink(link, outside, 3) && !CreateSymbolicLink(link, outside, 1))
        {
            Console.WriteLine("SKIP: directory-link checks need Developer Mode or link privileges; error=" + Marshal.GetLastWin32Error());
            return;
        }
        var worker = new BedrockCacheWorker(options, errors.Add);
        var countBefore = errors.Count;
        worker.RunPass(() => true, () => false, CancellationToken.None);
        Check(errors.Count > countBefore && File.ReadAllText(protectedFile) == "must stay", "directory link is refused without touching external files");
        try { BedrockCacheWorker.EnsureSafePath(link, Path.Combine(link, "play.entity.json")); throw new Exception("Expected link root rejection"); }
        catch (IOException) { _checks++; }
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool CreateSymbolicLink(string symbolicLink, string target, int flags);

    private static void FolderPickerTests()
    {
        var clients = Path.Combine(_output, "clients-" + Guid.NewGuid().ToString("N"));
        var child = Path.Combine(clients, "client-name");
        Directory.CreateDirectory(child);
        File.WriteAllText(Path.Combine(child, "Minecraft.Windows.exe"), "test marker, never executed");
        BedrockPathValidator.Validate(clients);
        Check(true, "client container shape accepted");
        Reject(() => BedrockPathValidator.Validate(child), "direct executable directory rejected");
        var oldValue = WpfConfig.BedrockDirectory;
        var configBefore = File.ReadAllText(ConfigManager.ConfigFilePath);
        string selected = null;
        var panel = new SettingsPanel(selectFolder: (entry, current) => selected);
        Render(panel, 860, 680, "settings-picker-test.png");
        var editor = Descendants<TextBox>(panel).Single(control => AutomationProperties.GetName(control) == "基岩版目录");
        var button = Descendants<Button>(panel).Single(control => AutomationProperties.GetName(control) == "基岩版目录：选择文件夹");
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Check(editor.Text == oldValue, "folder picker cancel preserves draft");
        selected = child;
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Check(editor.Text == oldValue && Descendants<TextBlock>(panel).Any(control => control.Text.Contains("<客户端名称文件夹>")), "wrong folder shows structural hint and preserves draft");
        selected = clients;
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Check(editor.Text == clients && WpfConfig.BedrockDirectory == oldValue && File.ReadAllText(ConfigManager.ConfigFilePath) == configBefore, "folder selection only edits draft until save");
        ConfigManager.Update(new Dictionary<string, object> { ["BedrockPath"] = clients });
        Check(WpfConfig.BedrockDirectory == clients && JObject.Parse(File.ReadAllText(ConfigManager.ConfigFilePath))["BedrockPath"].Value<string>() == clients, "BedrockPath stable key works without legacy property");
        WpfConfig.BedrockDirectory = oldValue;
        ConfigManager.Save();
    }

    private static void BedrockClearUiTests()
    {
        var area = Path.Combine(_output, "clear-ui-" + Guid.NewGuid().ToString("N"));
        var cache = Path.Combine(area, "packcache");
        Directory.CreateDirectory(Path.Combine(cache, "sub"));
        File.WriteAllText(Path.Combine(cache, "play.entity.json"), "x");
        File.WriteAllText(Path.Combine(cache, "sub", "data"), "y");
        var original = WpfConfig.BedrockPackcacheDirectory;
        try
        {
            ConfigManager.Update(new Dictionary<string, object> { [nameof(WpfConfig.BedrockPackcacheDirectory)] = cache });
            var panel = new SettingsPanel(null, (entry, current) => null, _ => { }, _ => true);
            Render(panel, 860, 680, "settings-clear-test.png");
            var clear = Descendants<Button>(panel).Single(control => AutomationProperties.GetName(control) == "清空 packcache 目录");
            clear.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check(Directory.Exists(cache) && !Directory.EnumerateFileSystemEntries(cache).Any(), "settings clear button empties packcache and keeps it");
            Check(Descendants<TextBlock>(panel).Any(control => control.Text.Contains("已清空 packcache 目录")), "settings clear reports success");

            var missing = Path.Combine(area, "missing", "packcache");
            ConfigManager.Update(new Dictionary<string, object> { [nameof(WpfConfig.BedrockPackcacheDirectory)] = missing });
            var missingPanel = new SettingsPanel(null, (entry, current) => null, _ => { }, _ => true);
            Render(missingPanel, 860, 680, "settings-clear-missing-test.png");
            var clearMissing = Descendants<Button>(missingPanel).Single(control => AutomationProperties.GetName(control) == "清空 packcache 目录");
            clearMissing.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check(Descendants<TextBlock>(missingPanel).Any(control => control.Text.Contains("目录不存在")), "settings clear warns when directory is missing");

            ConfigManager.Update(new Dictionary<string, object> { [nameof(WpfConfig.BedrockPackcacheDirectory)] = cache });
            Directory.CreateDirectory(cache);
            File.WriteAllText(Path.Combine(cache, "keep.json"), "keep");
            var declined = new SettingsPanel(null, (entry, current) => null, _ => { }, _ => false);
            Render(declined, 860, 680, "settings-clear-declined-test.png");
            Descendants<Button>(declined).Single(control => AutomationProperties.GetName(control) == "清空 packcache 目录")
                .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check(File.Exists(Path.Combine(cache, "keep.json")), "declined confirmation leaves the directory untouched");
        }
        finally
        {
            ConfigManager.Update(new Dictionary<string, object> { [nameof(WpfConfig.BedrockPackcacheDirectory)] = original });
        }
    }
}
