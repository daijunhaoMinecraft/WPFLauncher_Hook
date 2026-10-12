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
        new ConfigEntry("BedrockPath", nameof(WpfConfig.BedrockDirectory), "基岩版目录", "基岩版",
            helpText: BedrockPathValidator.HelpText, editor: "folder"),
        new ConfigEntry("EnableBedrockCacheCleanup", nameof(WpfConfig.EnableBedrockCacheCleanup), "热清理 PackCache 缓存", "基岩版缓存",
            helpText: "默认关闭。仅在 Minecraft.Windows.exe 运行期间，每 800ms 删除下列名称的缓存文件/文件夹（不可撤销，请先备份）。开启在下次启动游戏时生效，关闭保存后停止。"),
        new ConfigEntry("EnableBedrockMcpCopy", nameof(WpfConfig.EnableBedrockMcpCopy), "热复制 MCP 资源", "基岩版缓存",
            helpText: "默认关闭。仅向 packcache 中已有 .mcp 文件的目录复制资源，可覆盖同名文件；不是 DLL/内存注入。开启在下次启动游戏时生效，关闭保存后停止。"),
        new ConfigEntry("BedrockPackcacheDirectory", nameof(WpfConfig.BedrockPackcacheDirectory), "packcache 目录",
            "基岩版缓存",
            helpText: "默认 %AppData%\\MinecraftPE_Netease\\packcache。请选择名称为 packcache 的文件夹，不得与资源来源目录重叠。",
            editor: "folder"),
        new ConfigEntry("BedrockCacheFileNames", nameof(WpfConfig.BedrockCacheFileNames), "清理文件名", "基岩版缓存",
            helpText: "每行一个精确文件名；不支持路径或通配符。留空表示不删除文件。", editor: "multiline"),
        new ConfigEntry("BedrockCacheFolderNames", nameof(WpfConfig.BedrockCacheFolderNames), "清理文件夹名", "基岩版缓存",
            helpText: "每行一个精确文件夹名；匹配的文件夹及其内容会被删除。留空表示不删除文件夹。", editor: "multiline"),
        new ConfigEntry("BedrockCacheDurationSeconds", nameof(WpfConfig.BedrockCacheDurationSeconds),
            "缓存自动化持续时间(秒)", "基岩版缓存", 0, 86400,
            helpText: "单位秒，最大 86400；0 表示持续到游戏进程退出。空闲时扫描间隔会自动延长以降低开销，超过该时长后停止本会话的清理与复制扫描。"),
        new ConfigEntry("BedrockMcpSources", nameof(WpfConfig.BedrockMcpSources), "MCP 资源来源", "基岩版缓存",
            helpText: "每行一个 .mcp 文件或文件夹的绝对路径。文件夹内容递归复制；按列表顺序覆盖。同一游戏会话中每个目标目录成功复制一次，失败会重试。", editor: "mcp-sources"),
        new ConfigEntry("IsEnableX64mc", nameof(WpfConfig.Use64BitBedrock), "使用X64版本(基岩)", "基岩版"),
        new ConfigEntry("EnableCustomBedrockSelect", nameof(WpfConfig.EnableBedrockClientSelection), "自由选择基岩版客户端",
            "基岩版"),
        new ConfigEntry("KeepOffDeleteLastResourcepacks", nameof(WpfConfig.PreserveResourcePacks),
            "阻止网易删除resourcepacks文件夹", "Java 版"),
        new ConfigEntry("KeepOffDeleteLastConfig", nameof(WpfConfig.PreserveGameConfig), "阻止网易删除config文件夹", "Java 版"),
        new ConfigEntry("KeepOffDeleteLastShaderPacks", nameof(WpfConfig.PreserveShaderPacks), "阻止网易删除shaderpacks文件夹",
            "Java 版"),
        new ConfigEntry("UseJavaExe", nameof(WpfConfig.UseJavaExe), "使用 java.exe 启动游戏而不是 javaw.exe", "Java 版"),
        new ConfigEntry("CustomJVMArguments", nameof(WpfConfig.CustomJvmArguments), "自定义 JVM 参数", "Java 版"),
        new ConfigEntry("EnableModsInject", nameof(WpfConfig.EnableModInjection), "启用模组注入", "Java 版",
            helpText: "将 .jar 文件放入当前工作目录下的 ModsInject 文件夹，下次启动 Java 游戏时使用。加载/保存配置不会自动打开文件夹；可点击桌面设置页的「打开注入目录」。"),
        new ConfigEntry("SkipStartJavaFileFullCheck", nameof(WpfConfig.SkipStartJavaFileFullCheck), "跳过文件完整性检查(谨慎开启)",
            "Java 版"),
        new ConfigEntry("EnableCustomAccountLogin", nameof(WpfConfig.EnableAlternativeAccountLogin),
            "选择使用Sauth/4399账号登录", "账号与存档"),
        new ConfigEntry("MpayUnless", nameof(WpfConfig.UseAccountManagerLogin), "不使用Mpay登录(将会调用账号管理器窗口登录)", "账号与存档"),
        new ConfigEntry("AdvancedSavesManager", nameof(WpfConfig.EnableAdvancedSaveManager), "更高级的存档管理界面(支持多槽位)",
            "账号与存档"),
        new ConfigEntry("ShowCustomServer", nameof(WpfConfig.ShowCustomServers), "显示自定义服务器(如基岩版布吉岛等)", "联机与房间"),
        new ConfigEntry("MaxRoomCount", nameof(WpfConfig.MaxRoomCount), "最大房间数量", "联机与房间", 1, 1000),
        new ConfigEntry("IsCustomIP", nameof(WpfConfig.UseCustomServerAddress), "自定义IP进入服务器", "联机与房间"),
        new ConfigEntry("NoTwoExitMessage", nameof(WpfConfig.SkipExitConfirmation), "禁用退出二次确认", "联机与房间"),
        new ConfigEntry("EnableRoomBlacklist", nameof(WpfConfig.EnableRoomBlacklist), "启用房间黑名单", "联机与房间"),
        new ConfigEntry("AllowFrp", nameof(WpfConfig.EnablePortForwarding), "允许内网穿透", "联机与房间"),
        new ConfigEntry("UseNetworkMode", nameof(WpfConfig.EnableVirtualNetwork), "使用组网模式(需开启允许内网穿透)", "联机与房间",
            helpText: "同时开启「允许内网穿透」后生效。"),
        new ConfigEntry("UseMclRoomManagementWindow", nameof(WpfConfig.UseMclRoomManagementWindow),
            "用 MCL 房间管理窗口替换启动器自带房间管理", "联机与房间",
            helpText: "默认开启。启用后启动器房间窗口不再显示，改由 MCL 房间管理窗口提供开始/进入游戏、邀请、备份与重置存档、举报、退出房间、踢出玩家等功能；密码修改与房间黑名单也在此窗口内。关闭后恢复启动器自带房间窗口，可从房间信息窗口打开房间管理页面。"),
        new ConfigEntry("LanGameNicknameFilterString", nameof(WpfConfig.LanNicknameFilterKeywords),
            "过滤本地联机玩家名称关键字(使用分号隔开, 例如: 生存;一服)", "联机与房间"),
        new ConfigEntry("IsStartWebSocket", nameof(WpfConfig.EnableWebServer), "启用Web服务器", "下载与服务"),
        new ConfigEntry("HttpPort", nameof(WpfConfig.HttpPort), "Web服务器端口", "下载与服务", 1, 65535),
        new ConfigEntry("ServerListUrl", nameof(WpfConfig.ServerListUrl), "网易更新域名", "下载与服务"),
        new ConfigEntry("IsDownloadMultiConfig", nameof(WpfConfig.EnableParallelDownloads), "启用多线程下载", "下载与服务"),
        new ConfigEntry("MaxThread", nameof(WpfConfig.DownloadWorkerCount), "下载多线程数", "下载与服务", 1, 64,
            helpText: "有效范围 1–64；仅在开启多线程下载时使用。"),
        new ConfigEntry("LimitDownload", nameof(WpfConfig.ParallelDownloadThresholdMb),
            "大小限制(小于此大小即为小文件, 只使用单线程下载, 单位MB)", "下载与服务", 1, 4096, helpText: "单位 MiB，小于此阈值的文件使用单线程。"),
        new ConfigEntry("IsDebug", nameof(WpfConfig.EnableVerboseLogging), "启用详细日志", "日志与诊断",
            helpText: "关闭时隐藏 Debug/Trace 与请求、数据包等诊断输出；保留关键状态、警告和错误。保存后立即生效。"),
        new ConfigEntry("ShowLogInConsole", nameof(WpfConfig.ShowGameLogsInConsole), "显示游戏输出日志到控制台上", "日志与诊断",
            helpText: "仅控制游戏 stdout/stderr，不受详细日志开关影响。"),
        new ConfigEntry("ShowLogInWpf", nameof(WpfConfig.ShowGameLogsWindow), "显示游戏输出日志到Wpf日志窗口上", "日志与诊断",
            helpText: "下次启动游戏时创建日志窗口；关闭后停止接收游戏输出。"),
        new ConfigEntry("IsLogOutputFolder", nameof(WpfConfig.WriteLauncherLogsToFile), "启动器日志输出到文件夹", "日志与诊断",
            helpText: "将启动器日志写入 logs 目录，与控制台使用相同的详细级别。"),
        new ConfigEntry("ShowAccountInfo", nameof(WpfConfig.LogSensitiveAccountDetails), "记录账号敏感诊断", "日志与诊断",
            helpText: "需要同时开启详细日志，可能将身份凭据写入控制台和日志文件。仅在本机排障时临时开启，分享日志前务必检查。"),
        new ConfigEntry("ShowStartupLogo", nameof(WpfConfig.ShowStartupLogo), "显示启动 Logo", "日志与诊断",
            helpText: "默认开启；关闭后仅隐藏启动 Logo，不影响版本、状态和错误日志。"),
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
            fieldName = entry.FieldName, help = entry.HelpText, minimum = entry.Minimum, maximum = entry.Maximum,
            editor = entry.Editor
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
                var invalidCacheSetting = false;
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
                        invalidCacheSetting |= BedrockCacheOptions.IsCacheField(entry.FieldName);
                        PluginLog.Warn("Config", "忽略无效配置项（保留当前值）: " + entry.Key);
                    }
                }

                try
                {
                    if (invalidCacheSetting) throw new ArgumentException("Invalid cache setting.");
                    BedrockCacheOptions.ValidateValues(GetCurrentConfigValues());
                }
                catch (ArgumentException)
                {
                    // Never perform destructive work on a fallback path after ignoring invalid cache input.
                    WpfConfig.EnableBedrockCacheCleanup = false;
                    WpfConfig.EnableBedrockMcpCopy = false;
                    PluginLog.Warn("Config",
                        "Invalid cache configuration; both file automation switches were disabled.");
                }
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException ||
                                              exception is JsonException)
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
            BedrockCacheOptions.ValidateValues(values);
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
        BedrockCacheOptions.ValidateEntry(entry.FieldName, value);
        if (entry.FieldName == nameof(WpfConfig.ServerListUrl))
        {
            if (!Uri.TryCreate((string)value, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
                throw new ArgumentException(entry.Description + "：请输入完整的 HTTP(S) 地址。");
        }
        else if (entry.FieldName == nameof(WpfConfig.BedrockDirectory))
        {
            try
            {
                Path.GetFullPath((string)value);
            }
            catch (Exception exception) when (exception is ArgumentException || exception is NotSupportedException ||
                                              exception is PathTooLongException)
            {
                throw new ArgumentException(entry.Description + "：请输入有效路径。", exception);
            }

            if (!string.Equals((string)value, WpfConfig.BedrockDirectory, StringComparison.OrdinalIgnoreCase))
                BedrockPathValidator.Validate((string)value);
        }
        else if (entry.FieldName == nameof(WpfConfig.CustomJvmArguments))
        {
            var arguments = ((string)value).Trim();
            if (arguments.Length > 0 && (!arguments.StartsWith("-", StringComparison.Ordinal) ||
                                         arguments.Count(c => c == '"') % 2 != 0))
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

    internal static BedrockCacheOptions GetBedrockCacheOptions()
    {
        lock (Sync) return BedrockCacheOptions.FromValues(GetCurrentConfigValues());
    }
}
