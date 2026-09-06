using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Mcl.Core.Dotnetdetour.Utilities.Diagnostics;

namespace Mcl.Core.Dotnetdetour.Models.Config;

public static class ConfigManager
{
    private static readonly object Sync = new object();
    private static JObject _unknownValues = new JObject();
    public static string ConfigFilePath => Path.Combine(WpfConfig.LauncherRootDirectory, "config.json");

    // Key is a public JSON/API contract. FieldName may change independently.
    public static readonly IReadOnlyList<ConfigEntry> Registry = Array.AsReadOnly(new[]
    {
        new ConfigEntry("MemoryOptimize", nameof(WpfConfig.OptimizeMemoryBeforeLaunch), "游戏启动前进行内存优化", "常规"),
        new ConfigEntry("IsWindowTopMost", nameof(WpfConfig.KeepWindowsOnTop), "窗口保持在最上层", "常规"),
        new ConfigEntry("ShowWindowsNotify", nameof(WpfConfig.EnableChatNotifications), "聊天显示系统原生通知", "常规"),
        new ConfigEntry("IsBypassGameUpdate_Bedrock", nameof(WpfConfig.SkipBedrockUpdates), "绕过基岩版更新", "基岩版"),
        new ConfigEntry("BedrockPath", nameof(WpfConfig.BedrockDirectory), "基岩版目录", "基岩版"),
        new ConfigEntry("IsEnableX64mc", nameof(WpfConfig.Use64BitBedrock), "使用X64版本(基岩)", "基岩版"),
        new ConfigEntry("EnableCustomBedrockSelect", nameof(WpfConfig.EnableBedrockClientSelection), "自由选择基岩版客户端", "基岩版"),
        new ConfigEntry("KeepOffDeleteLastResourcepacks", nameof(WpfConfig.PreserveResourcePacks), "阻止网易删除resourcepacks文件夹", "Java 版"),
        new ConfigEntry("KeepOffDeleteLastConfig", nameof(WpfConfig.PreserveGameConfig), "阻止网易删除config文件夹", "Java 版"),
        new ConfigEntry("KeepOffDeleteLastShaderPacks", nameof(WpfConfig.PreserveShaderPacks), "阻止网易删除shaderpacks文件夹", "Java 版"),
        new ConfigEntry("UseJavaExe", nameof(WpfConfig.UseJavaExe), "使用 java.exe 启动游戏而不是 javaw.exe", "Java 版"),
        new ConfigEntry("CustomJVMArguments", nameof(WpfConfig.CustomJvmArguments), "自定义 JVM 参数", "Java 版"),
        new ConfigEntry("EnableModsInject", nameof(WpfConfig.EnableModInjection), "启用模组注入", "Java 版"),
        new ConfigEntry("EnableCustomAccountLogin", nameof(WpfConfig.EnableAlternativeAccountLogin), "选择使用Sauth/4399账号登录", "账号与存档"),
        new ConfigEntry("MpayUnless", nameof(WpfConfig.UseAccountManagerLogin), "不使用Mpay登录(将会调用账号管理器窗口登录)", "账号与存档"),
        new ConfigEntry("AdvancedSavesManager", nameof(WpfConfig.EnableAdvancedSaveManager), "更高级的存档管理界面(支持多槽位)", "账号与存档"),
        new ConfigEntry("ShowCustomServer", nameof(WpfConfig.ShowCustomServers), "显示自定义服务器(如基岩版布吉岛等)", "联机与房间"),
        new ConfigEntry("MaxRoomCount", nameof(WpfConfig.MaxRoomCount), "最大房间数量", "联机与房间", 1, 1000),
        new ConfigEntry("IsCustomIP", nameof(WpfConfig.UseCustomServerAddress), "自定义IP进入服务器", "联机与房间"),
        new ConfigEntry("NoTwoExitMessage", nameof(WpfConfig.SkipExitConfirmation), "禁用退出二次确认", "联机与房间"),
        new ConfigEntry("EnableRoomBlacklist", nameof(WpfConfig.EnableRoomBlacklist), "启用房间黑名单", "联机与房间"),
        new ConfigEntry("AllowFrp", nameof(WpfConfig.EnablePortForwarding), "允许内网穿透", "联机与房间"),
        new ConfigEntry("UseNetworkMode", nameof(WpfConfig.EnableVirtualNetwork), "使用组网模式(需开启允许内网穿透)", "联机与房间", helpText: "同时开启「允许内网穿透」后生效。"),
        new ConfigEntry("ShowRoomManagerWindow", nameof(WpfConfig.ShowRoomDetailsWindow), "显示房间信息查看窗口", "联机与房间"),
        new ConfigEntry("LanGameNicknameFilterString", nameof(WpfConfig.LanNicknameFilterKeywords), "过滤本地联机玩家名称关键字(使用分号隔开, 例如: 生存;一服)", "联机与房间"),
        new ConfigEntry("IsStartWebSocket", nameof(WpfConfig.EnableWebServer), "启用Web服务器", "下载与服务"),
        new ConfigEntry("HttpPort", nameof(WpfConfig.HttpPort), "Web服务器端口", "下载与服务", 1, 65535),
        new ConfigEntry("ServerListUrl", nameof(WpfConfig.ServerListUrl), "网易更新域名", "下载与服务"),
        new ConfigEntry("IsDownloadMultiConfig", nameof(WpfConfig.EnableParallelDownloads), "启用多线程下载", "下载与服务"),
        new ConfigEntry("MaxThread", nameof(WpfConfig.DownloadWorkerCount), "下载多线程数", "下载与服务", 1, 64, helpText: "有效范围 1–64；仅在开启多线程下载时使用。"),
        new ConfigEntry("LimitDownload", nameof(WpfConfig.ParallelDownloadThresholdMb), "大小限制(小于此大小即为小文件, 只使用单线程下载, 单位MB)", "下载与服务", 1, 4096, helpText: "单位 MiB，小于此阈值的文件使用单线程。"),
        new ConfigEntry("IsDebug", nameof(WpfConfig.EnableVerboseLogging), "启用详细日志", "日志与诊断", helpText: "关闭时隐藏 Debug/Trace 与请求、数据包等诊断输出；保留关键状态、警告和错误。保存后立即生效。"),
        new ConfigEntry("ShowLogInConsole", nameof(WpfConfig.ShowGameLogsInConsole), "显示游戏输出日志到控制台上", "日志与诊断", helpText: "仅控制游戏 stdout/stderr，不受详细日志开关影响。"),
        new ConfigEntry("ShowLogInWpf", nameof(WpfConfig.ShowGameLogsWindow), "显示游戏输出日志到Wpf日志窗口上", "日志与诊断", helpText: "下次启动游戏时创建日志窗口；关闭后停止接收游戏输出。"),
        new ConfigEntry("IsLogOutputFolder", nameof(WpfConfig.WriteLauncherLogsToFile), "启动器日志输出到文件夹", "日志与诊断", helpText: "将启动器日志写入 logs 目录，与控制台使用相同的详细级别。"),
        new ConfigEntry("ShowAccountInfo", nameof(WpfConfig.LogSensitiveAccountDetails), "记录账号敏感诊断", "日志与诊断", helpText: "需要同时开启详细日志，可能将身份凭据写入控制台和日志文件。仅在本机排障时临时开启，分享日志前务必检查。"),
        new ConfigEntry("ShowStartupLogo", nameof(WpfConfig.ShowStartupLogo), "显示启动 Logo", "日志与诊断", helpText: "默认开启；关闭后仅隐藏启动 Logo，不影响版本、状态和错误日志。"),
    });

    public static Dictionary<string, object> GetCurrentConfigValues()
    {
        lock (Sync) return Registry.ToDictionary(entry => entry.Key, entry => entry.GetValue());
    }

    public static Dictionary<string, object> GetMetadata() => Registry.ToDictionary(
        entry => entry.Key,
        entry => (object)new
        {
            desc = entry.Description, type = entry.FieldType.Name, category = entry.Category,
            fieldName = entry.FieldName, help = entry.HelpText, minimum = entry.Minimum, maximum = entry.Maximum
        });

    public static void Save()
    {
        lock (Sync) Persist(GetCurrentConfigValues());
        ApplyLoggingSettings();
    }

    public static void Load()
    {
        lock (Sync)
        {
            if (!File.Exists(ConfigFilePath))
            {
                ApplyLoggingSettings();
                return;
            }
            try
            {
                var values = JObject.Parse(File.ReadAllText(ConfigFilePath));
                _unknownValues = (JObject)values.DeepClone();
                foreach (var entry in Registry)
                {
                    // Old keys take precedence when both names occur in an existing file.
                    var value = values[entry.Key] ?? values[entry.FieldName];
                    _unknownValues.Remove(entry.Key);
                    _unknownValues.Remove(entry.FieldName);
                    if (value == null) continue;
                    try
                    {
                        var converted = entry.ConvertValue(GetScalar(value));
                        Validate(entry, converted);
                        entry.SetValue(converted);
                    }
                    catch (ArgumentException)
                    {
                        PluginLog.Warn("Config", "忽略无效配置项（保留当前值）: " + entry.Key);
                    }
                }
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is JsonException)
            {
                PluginLog.Error("Config", exception, "无法读取配置，保留当前值且不覆盖原文件。");
            }
        }
        ApplyLoggingSettings();
    }

    public static void UpdateFromJson(string json)
    {
        var document = JObject.Parse(json);
        Update(document.Properties().ToDictionary(property => property.Name,
            property => (object)property.Value));
    }

    /// <summary>Validate everything, persist once, then publish. Failed input never partially applies.</summary>
    public static void Update(IDictionary<string, object> updates)
    {
        if (updates == null) throw new ArgumentNullException(nameof(updates));
        lock (Sync)
        {
            var pending = new Dictionary<ConfigEntry, object>();
            foreach (var entry in Registry)
            {
                if (!updates.TryGetValue(entry.Key, out var value) &&
                    !updates.TryGetValue(entry.FieldName, out value)) continue;
                var converted = entry.ConvertValue(value is JToken token ? GetScalar(token) : value);
                Validate(entry, converted);
                pending[entry] = converted;
            }

            var values = GetCurrentConfigValues();
            foreach (var pair in pending) values[pair.Key.Key] = pair.Value;
            Persist(values);
            foreach (var pair in pending) pair.Key.SetValue(pair.Value);
        }
        ApplyLoggingSettings();
    }

    private static object GetScalar(JToken token)
    {
        if (!(token is JValue value)) throw new ArgumentException("配置值必须是文本、布尔值或整数。");
        return value.Value;
    }

    private static void Validate(ConfigEntry entry, object value)
    {
        if (entry.FieldName == nameof(WpfConfig.ServerListUrl))
        {
            if (!Uri.TryCreate((string)value, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
                throw new ArgumentException(entry.Description + "：请输入完整的 HTTP(S) 地址。");
        }
        else if (entry.FieldName == nameof(WpfConfig.BedrockDirectory))
        {
            try { Path.GetFullPath((string)value); }
            catch (Exception exception) when (exception is ArgumentException || exception is NotSupportedException || exception is PathTooLongException)
            {
                throw new ArgumentException(entry.Description + "：请输入有效路径。", exception);
            }
        }
        else if (entry.FieldName == nameof(WpfConfig.CustomJvmArguments))
        {
            var arguments = ((string)value).Trim();
            if (arguments.Length > 0 && (!arguments.StartsWith("-", StringComparison.Ordinal) || arguments.Count(c => c == '"') % 2 != 0))
                throw new ArgumentException("JVM 参数必须以 '-' 开头，且双引号需要成对闭合。");
        }
    }

    private static void Persist(Dictionary<string, object> values)
    {
        var document = (JObject)_unknownValues.DeepClone();
        foreach (var pair in values) document[pair.Key] = JToken.FromObject(pair.Value ?? "");
        AtomicFile.WriteAllText(ConfigFilePath, document.ToString(Formatting.Indented));
    }

    private static void ApplyLoggingSettings() => LauncherLogging.Configure(
        WpfConfig.EnableVerboseLogging, WpfConfig.WriteLauncherLogsToFile, WpfConfig.LauncherRootDirectory);
}
