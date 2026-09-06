using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using Mcl.Core.Dotnetdetour.Models.Config;
using Mcl.Core.Dotnetdetour.UI.Controls;
using Mcl.Core.Dotnetdetour.UI.Themes;
using Mcl.Core.Dotnetdetour.UI.WebAssets;
using Mcl.Core.Dotnetdetour.Utilities.Diagnostics;
using Mcl.Core.Tools;
using Net.Nekocurit.Cipher;
using Newtonsoft.Json.Linq;
using NLog;

internal static class Program
{
    private static int _checks;
    private static string _root;
    private static string _output;

    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length != 2) { Console.Error.WriteLine("Usage: Mcl.Core.exe <repository> <output-directory>"); return 2; }
        _root = Path.GetFullPath(args[0]);
        _output = Path.GetFullPath(args[1]);
        Directory.CreateDirectory(_output);
        WpfConfig.LauncherRootDirectory = Path.Combine(_output, "config-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(WpfConfig.LauncherRootDirectory);
        try
        {
            CryptoTests();
            ConfigurationTests();
            LoggingTests();
            BufferTests();
            ResourceTests();
            UiTests();
            Console.WriteLine($"PASS: {_checks} checks. UI renders: {_output}");
            return 0;
        }
        catch (Exception exception) { Console.Error.WriteLine(exception); return 1; }
        finally { LogManager.Shutdown(); }
    }

    private static void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException("FAILED: " + name);
        _checks++;
    }

    private static void Reject(Action action, string name)
    {
        try { action(); }
        catch (ArgumentException) { _checks++; return; }
        catch (FormatException) { _checks++; return; }
        throw new InvalidOperationException("Expected validation failure: " + name);
    }

    private static void CryptoTests()
    {
        // Captured from the working-tree implementation before any algorithm refactoring.
        var inputs = new uint[] { 0, 1, 42, 0x7fffffff, 0x80000000, 0xffffffff };
        var expected = new uint[] { 1213517259, 3820495339, 2233423682, 2626871699, 2114553146, 1574517983 };
        var cipher = new Skip32Cipher();
        for (var index = 0; index < inputs.Length; index++)
        {
            Check(cipher.Encrypt(inputs[index]) == expected[index], "Skip32 golden vector " + index);
            Check(cipher.Decrypt(expected[index]) == inputs[index], "Skip32 inverse " + index);
            Check(UidHelper.ToMobileUid(inputs[index]) == (inputs[index] | 0x80000000), "mobile UID " + index);
            Check(UidHelper.TogglePlatform(UidHelper.TogglePlatform(inputs[index])) == inputs[index], "UID toggle " + index);
        }
        var suppliedKey = Encoding.ASCII.GetBytes("SaintSteve");
        var protectedCipher = new Skip32Cipher(suppliedKey);
        suppliedKey[0] = 0;
        Check(protectedCipher.Encrypt(42) == expected[2], "Skip32 owns its key copy");
        Reject(() => new Skip32Cipher(new byte[9]), "Skip32 invalid key");

        X19Crypt.Token = "test-token";
        Check(X19Crypt.GetH5Token() == "90567ff86d28e6fad19b5dece8296c59", "H5 token golden vector");
        Check(X19Crypt.ComputeDynamicToken("/example?", "{\"a\":1}") == "PD3u6uppumx1fHs11", "dynamic token golden vector");
        Check(X19Crypt.ComputeDynamicToken("/example?", "{\"a\":1}", new byte[] { 0, 1, 128, 255 }) == "PPoqK3SuOa5t7HU91", "binary token golden vector");
        Check("hello world".Sign() == "!x19sign!9dff2dd4ed36977d37ece96c20bff41534e0969e7daa95fd5dcca3f8a0bf54b9", "signature golden vector");
        Check("MCL test 中文".Sign() == "!x19sign!4d30c95d00a94651902c3b629daf6845347c7775bf8f005297d7eb606be1e8cffd84271dc2552318", "Unicode signature golden vector");
        Check("".Sign() == "!x19sign!844b55d5d6d2df7278b0be85711c3ffacb17be19ba401beb32913218da93e338", "empty signature golden vector");
        Check(!X19SignHelper.IsSigned(null), "null signature predicate");
        Check(X19SignHelper.Decrypt("plain") == "plain", "plain signature passthrough");

        var key = Encoding.ASCII.GetBytes("0123456789abcdef");
        var iv = new byte[16];
        Check(AesHelper.AesCbcEncrypt(key, key, iv).ToHex() == "72727e881edcfd0100a718687909b565", "AES CBC golden vector");
        foreach (var length in new[] { 0, 1, 15, 16, 17, 31, 32, 1024 })
        {
            var text = new string('x', length);
            var data = Encoding.UTF8.GetBytes(text);
            Check(X19Crypt.DecryptX19Body(X19Crypt.HttpEncrypt(data)) == text, "X19 frame roundtrip " + length);
            Check(AesHelper.Decrypt(key, AesHelper.Encrypt(key, data)).SequenceEqual(data), "legacy ECB roundtrip " + length);
        }
        Reject(() => X19Crypt.DecryptX19Body(null), "null X19 frame");
        Reject(() => X19Crypt.DecryptX19Body(new byte[18]), "short X19 frame");
        Reject(() => X19Crypt.DecryptX19Body(new byte[34]), "unaligned X19 frame");
        var missingTrailer = new byte[33];
        Buffer.BlockCopy(AesHelper.AesCbcEncrypt(X19Crypt.PickKey(0), new byte[16], iv), 0, missingTrailer, 16, 16);
        Reject(() => X19Crypt.DecryptX19Body(missingTrailer), "bounded trailer validation");
        Check(new byte[] { 0, 1, 128, 255 }.ToBinary() == "00000000000000011000000011111111", "binary encoding");
        Check(new byte[] { 0, 128, 255 }.ToHex(true) == "0080FF", "uppercase hex encoding");
        Check(Mcl.Core.Dotnetdetour.Utilities.Crypto.AESHelper.AES_CBC_Encrypt(key, key, iv)
            .SequenceEqual(AesHelper.AesCbcEncrypt(key, key, iv)), "legacy AES facade");
    }

    private static void ConfigurationTests()
    {
        File.WriteAllText(ConfigManager.ConfigFilePath, "{\"IsDebug\":false,\"MaxThread\":12,\"HttpPort\":5001,\"future-option\":{\"enabled\":true}}");
        ConfigManager.Load();
        Check(WpfConfig.DownloadWorkerCount == 12 && WpfConfig.HttpPort == 5001, "load old keys");
        ConfigManager.UpdateFromJson("{\"DownloadWorkerCount\":16,\"EnableVerboseLogging\":true}");
        Check(WpfConfig.EnableVerboseLogging && WpfConfig.DownloadWorkerCount == 16, "update new aliases");
        var saved = JObject.Parse(File.ReadAllText(ConfigManager.ConfigFilePath));
        Check(saved["IsDebug"].Value<bool>() && saved["EnableVerboseLogging"] == null, "persist stable old keys");
        Check(saved["future-option"]["enabled"].Value<bool>(), "unknown options preserved");
        var before = File.ReadAllText(ConfigManager.ConfigFilePath);
        Reject(() => ConfigManager.UpdateFromJson("{\"HttpPort\":6000,\"MaxThread\":0}"), "batch range validation");
        Check(WpfConfig.HttpPort == 5001 && File.ReadAllText(ConfigManager.ConfigFilePath) == before, "invalid batch changes neither memory nor file");
        foreach (var json in new[] { "{\"HttpPort\":65536}", "{\"HttpPort\":1.5}", "{\"HttpPort\":true}",
                     "{\"IsDebug\":null}", "{\"IsDebug\":1}", "{\"MaxThread\":[]}", "{\"ServerListUrl\":\"file:///C:/secret\"}",
                     "{\"CustomJVMArguments\":\"-Dkey=\\\"open\"}", "{\"CustomJVMArguments\":\"invalid\"}" })
            Reject(() => ConfigManager.UpdateFromJson(json), "invalid configuration " + json);
        ConfigManager.UpdateFromJson("{\"IsDebug\":false,\"EnableVerboseLogging\":true,\"Version\":\"not-allowed\"}");
        Check(!WpfConfig.EnableVerboseLogging && WpfConfig.Version != "not-allowed", "stable-key precedence and registry whitelist");
        ConfigManager.UpdateFromJson("{\"HttpPort\":1,\"MaxThread\":64}");
        Check(WpfConfig.HttpPort == 1 && WpfConfig.DownloadWorkerCount == 64, "integer boundaries");

        var lockedBefore = File.ReadAllText(ConfigManager.ConfigFilePath);
        using (var file = new FileStream(ConfigManager.ConfigFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            try { ConfigManager.UpdateFromJson("{\"HttpPort\":6000}"); throw new Exception("Expected persistence failure"); }
            catch (IOException) { _checks++; }
        }
        Check(WpfConfig.HttpPort == 1 && File.ReadAllText(ConfigManager.ConfigFilePath) == lockedBefore, "failed replacement does not publish");
        File.WriteAllText(ConfigManager.ConfigFilePath, "{\"MaxThread\":0,\"HttpPort\":5002}");
        ConfigManager.Load();
        Check(WpfConfig.DownloadWorkerCount == 64 && WpfConfig.HttpPort == 5002, "bad entry does not discard valid neighbors");
        File.WriteAllText(ConfigManager.ConfigFilePath, "broken-json");
        ConfigManager.Load();
        Check(File.ReadAllText(ConfigManager.ConfigFilePath) == "broken-json", "malformed file not overwritten");
        WpfConfig.RoomBlacklist = new List<string> { "example" };
        WpfConfig.WriteRoomBlacklist();
        WpfConfig.ReadRoomBlacklist();
        Check(WpfConfig.RoomBlacklist.SequenceEqual(new[] { "example" }), "blacklist directory created on write");
        WpfConfig.ReadRegexBlacklist();
        Check(WpfConfig.RegexBlacklist.Count == 0, "regex blacklist first run");
    }

    private static void LoggingTests()
    {
        var original = Console.Out;
        var writer = new StringWriter();
        try
        {
            Console.SetOut(writer);
            var logger = LogManager.GetLogger("host-test");
            LauncherLogging.Configure(false, true, WpfConfig.LauncherRootDirectory);
            var computed = false;
            LauncherLogging.Debug(() => { computed = true; return "hidden-lazy"; });
            logger.Debug("hidden-debug");
            logger.Trace("hidden-trace");
            logger.Info("visible-info");
            logger.Warn("visible-warning");
            logger.Error("visible-error");
            LogManager.Flush();
            var consoleText = writer.ToString();
            var fileText = string.Concat(Directory.GetFiles(Path.Combine(WpfConfig.LauncherRootDirectory, "logs"), "*.log").Select(File.ReadAllText));
            Check(!computed && !logger.IsDebugEnabled, "verbose off gates lazy work and host logger");
            Check(!consoleText.Contains("hidden-") && !fileText.Contains("hidden-"), "quiet console and file");
            Check(fileText.Contains("visible-info") && fileText.Contains("visible-warning") && fileText.Contains("visible-error"), "essential file levels preserved");
            Check(consoleText.Contains("visible-info") && consoleText.Contains("visible-warning") && consoleText.Contains("visible-error"), "essential console levels preserved");
            LauncherLogging.Configure(true, true, WpfConfig.LauncherRootDirectory);
            logger.Debug("enabled-debug");
            LauncherLogging.Debug(() => "enabled-lazy");
            LogManager.Flush();
            Check(logger.IsDebugEnabled && writer.ToString().Contains("enabled-debug"), "live verbose enable");
            LauncherLogging.Configure(false, false, WpfConfig.LauncherRootDirectory);
            logger.Debug("disabled-again");
            logger.Info("console-only");
            LogManager.Flush();
            Check(!writer.ToString().Contains("disabled-again"), "live verbose disable");
            Check(LogManager.Configuration.FindTargetByName("mcl-file") == null, "file output disabled immediately");
            Check(LogManager.Configuration.LoggingRules.Count == 1, "reconfiguration does not duplicate targets");
        }
        finally { Console.SetOut(original); }
    }

    private static void BufferTests()
    {
        var buffer = new BoundedLogBuffer(1024, 256);
        Parallel.For(0, 5000, index => buffer.Append("line-" + index + Environment.NewLine));
        Check(buffer.HistoryLength <= 1024 && buffer.PendingLength <= 256, "concurrent buffer capacity");
        Check(buffer.Drain().Contains("已跳过"), "dropped UI lines reported");
        buffer.Append(new string('x', 10000));
        Check(buffer.HistoryLength <= 1024 && buffer.PendingLength <= 256, "oversized line bounded");
        Check(buffer.Snapshot().Length <= 1024, "bounded export snapshot");
        buffer.Close();
        buffer.Append("ignored");
        Check(buffer.HistoryLength == 0 && buffer.PendingLength == 0 && buffer.Snapshot() == "", "closed buffer ignores producers");
    }

    private static void ResourceTests()
    {
        WpfConfig.HttpPort = 54321;
        var room = HtmlResource.GetRoomManageHtml();
        Check(room.Contains("54321") && !room.Contains("__MCL_HTTP_PORT__"), "embedded room port substitution");
        Check(room.Contains("--primary-color: #005fb8") && !room.Contains("/* MCL_FLUENT_THEME */"), "embedded room Fluent theme");
        var settings = HtmlResource.GetHotUpdateHtml();
        Check(settings.Contains("/config/save") && !settings.Contains("cdn.tailwindcss.com"), "offline settings resource");
        File.WriteAllText(Path.Combine(_output, "settings-preview.html"), settings);
        File.WriteAllText(Path.Combine(_output, "room-preview.html"), room);
    }

    private static void UiTests()
    {
        var application = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        var initialResources = application.Resources.MergedDictionaries.Count;
        var savedCount = 0;
        var panel = new SettingsPanel(() => savedCount++);
        Render(panel, 860, 680, "settings-general.png");
        var navigation = Descendants<ListBox>(panel).Single();
        navigation.SelectedItem = "日志与诊断";
        Render(panel, 860, 680, "settings-logging.png");
        var search = Descendants<TextBox>(panel).Single(control => AutomationProperties.GetName(control) == "搜索设置");
        search.Text = "JVM";
        Render(panel, 660, 520, "settings-search.png");
        Check(search.IsEnabled && search.ActualWidth > 0 && search.Text == "JVM", "settings search remains editable");
        Check(application.Resources.MergedDictionaries.Count == initialResources, "theme does not pollute host application");

        var port = Descendants<TextBox>(panel).Single(control => AutomationProperties.GetName(control) == "Web服务器端口");
        var saveButton = Descendants<Button>(panel).Single(control => Equals(control.Content, "保存并应用"));
        var originalPort = WpfConfig.HttpPort;
        port.Text = "invalid";
        saveButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Check(savedCount == 0 && WpfConfig.HttpPort == originalPort, "invalid UI input neither saves nor closes");
        Check(Descendants<TextBlock>(panel).Any(control => control.Text.StartsWith("更改未保存：")), "inline UI validation error");
        port.Text = "54322";
        saveButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Check(savedCount == 1 && WpfConfig.HttpPort == 54322, "UI save uses shared configuration pipeline");
        Check(JObject.Parse(File.ReadAllText(ConfigManager.ConfigFilePath))["HttpPort"].Value<int>() == 54322, "UI save persisted");

        var logWindow = new ProcessLogWindow("minecraft.exe");
        logWindow.AppendLog("[Game] 游戏正在启动…");
        logWindow.AppendLog("示例诊断输出");
        logWindow.OnProcessExited(0);
        Render((FrameworkElement)logWindow.Content, 852, 482, "process-log.png");
        logWindow.Close();
        logWindow.AppendLog("ignored after close");

        var presentation = XNamespace.Get("http://schemas.microsoft.com/winfx/2006/xaml/presentation");
        var xaml = XNamespace.Get("http://schemas.microsoft.com/winfx/2006/xaml");
        var eventNames = new HashSet<string> { "Click", "Loaded", "Closed", "Closing", "KeyDown", "KeyUp", "MouseLeftButtonDown", "MouseDoubleClick",
            "SelectionChanged", "TextChanged", "Checked", "Unchecked", "ScrollChanged", "PreviewMouseLeftButtonDown", "PreviewKeyDown", "MouseDown", "SizeChanged", "PreviewTextInput" };
        var windows = Directory.GetFiles(Path.Combine(_root, "Mcl.Core", "Dotnetdetour"), "*.xaml", SearchOption.AllDirectories);
        var loaded = 0;
        foreach (var path in windows)
        {
            var document = XDocument.Load(path);
            if (document.Root.Name != presentation + "Window") continue;
            document.Root.Attribute(xaml + "Class")?.Remove();
            foreach (var attribute in document.Descendants().Attributes().Where(attribute => eventNames.Contains(attribute.Name.LocalName)).ToList()) attribute.Remove();
            // Load markup without code-behind: never run login, network, updater or host callbacks.
            Console.WriteLine("Render: " + Path.GetFileName(path));
            var window = (Window)XamlReader.Parse(document.ToString());
            window.ApplyTemplate();
            Check(window.FindResource("PrimaryButton") is Style, "theme lookup " + Path.GetFileName(path));
            var content = window.Content as FrameworkElement;
            if (content != null) Render(content, window.Width - 16, window.Height - 40, Path.GetFileNameWithoutExtension(path) + ".png");
            window.Close();
            loaded++;
        }
        Check(loaded == 21, "all 21 XAML windows parsed and rendered");
        application.Shutdown();
    }

    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is T typed) yield return typed;
            foreach (var descendant in Descendants<T>(child)) yield return descendant;
        }
    }

    private static void Render(FrameworkElement view, double width, double height, string name)
    {
        view.Measure(new Size(width, height));
        view.Arrange(new Rect(0, 0, width, height));
        view.UpdateLayout();
        var bitmap = new RenderTargetBitmap((int)width, (int)height, 96, 96, PixelFormats.Pbgra32);
        var background = new DrawingVisual();
        using (var drawing = background.RenderOpen())
            drawing.DrawRectangle(new SolidColorBrush(Color.FromRgb(243, 243, 243)), null, new Rect(0, 0, width, height));
        bitmap.Render(background);
        bitmap.Render(view);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using (var file = File.Create(Path.Combine(_output, name))) encoder.Save(file);
    }
}
