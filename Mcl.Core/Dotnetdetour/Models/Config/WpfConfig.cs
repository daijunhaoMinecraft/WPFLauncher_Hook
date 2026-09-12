using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Permissions;
using Mcl.Core.Dotnetdetour.Models.Entities;
using Mcl.Core.Dotnetdetour.Models.Entity;
using Net.Nekocurit.Cipher;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using WPFLauncher.Code;
using WPFLauncher.Network.Protocol.LobbyGame;
using WPFLauncher.Util;
using WPFLauncher.View.UI;

namespace Mcl.Core.Dotnetdetour.Models.Config;

public partial class WpfConfig
{
    public static string Version = "7.0.0-DLL-Public";
    public static string DefaultWebSocketAddress = "ws://127.0.0.1:4600/websocket";
    public static string DefaultHttpAddress = "http://127.0.0.1:4600/";
    public static int HttpPort = 4600;
    public static bool CookieLoginWithoutMpay = false;
    public static string LastChatResponse = string.Empty;
    public static bool SkipBedrockUpdates = false;
    public static bool EnableVerboseLogging = false;
    public static EntityResponse<LobbyGameRoomEntity> RoomInfo = null;
    public static string Password = string.Empty;
    public static string MacAddress = string.Empty;
    public static string RandomMacAddress = string.Empty;
    public static bool Use64BitBedrock = true;
    public static JArray ReceivedMessages = new();
    public static bool EnableRoomBlacklist = false;
    public static List<string> RoomBlacklist = new();
    public static List<string> RegexBlacklist = new();
    public static int MaxRoomCount = 16;
    public static bool IsLoggedIn = false;

    public static string ServerListUrl = "https://x19.update.netease.com/serverlist/release.json";
    public static Uri ServerListUri = new(ServerListUrl);
    public static JObject ServerList = new();

    public static JArray RoomPlayerList = new();
    public static long JoinOrCreateTime = 0;
    public static string LauncherRootDirectory = Directory.GetCurrentDirectory();
    public static bool EnableWebServer = false;
    public static bool UseCustomServerAddress = false;
    public static bool HasSelectedServerAddress = true;
    public static bool SkipExitConfirmation = true;
    public static int JoinRetryCount = 0;
    public static string JavaGamePath = string.Empty;
    public static bool OptimizeMemoryBeforeLaunch = false;
    public static bool LogSensitiveAccountDetails = false;
    public static bool ShowStartupLogo = true;

    // 美化
    public static bool EnableChatNotifications = false;

    // threading Download Config
    public static int DownloadWorkerCount = 8;
    public static bool EnableParallelDownloads = false;
    public static int ParallelDownloadThresholdMb = 30;
    public static string BedrockDirectory = tb.s;
    public static bool KeepWindowsOnTop = false;

    // Advanced
    public static bool EnableModInjection = false;

    public static bool WriteLauncherLogsToFile = true;
    
    // 跳过 Java 游戏完整性检查(谨慎开启)
    public static bool SkipStartJavaFileFullCheck = true;

    // custom Settings
    public static bool EnableBedrockClientSelection = false;
    public static bool EnableAlternativeAccountLogin = false;
    public static bool UseAccountManagerLogin = false;
    public static bool EnableAdvancedSaveManager = true;
    public static bool ShowRoomDetailsWindow = true;

    public static bool PreserveResourcePacks = false;
    public static bool PreserveGameConfig = false;
    public static bool PreserveShaderPacks = false;

    // LanGame Settings
    public static bool EnablePortForwarding = false;
    public static bool EnableVirtualNetwork = false;

    // Java Settings
    public static string CustomJvmArguments = string.Empty;
    public static bool UseJavaExe = false;

    public static Logger DefaultLogger = LogManager.GetCurrentClassLogger();
    public static List<FriendStatus> FriendStatuses = new();

    // Custom Recent Server
    public static List<Tuple<string, NetGameResponse>> CustomRecentServers = new();
    public static bool IsJoiningCustomServer = false;
    public static bool ShowCustomServers = false;

    // Filter
    public static string LanNicknameFilterKeywords = string.Empty;
    public static List<string> LanNicknameFilters = new List<string>();

    public static bool ShowGameLogsInConsole = false;
    public static bool ShowGameLogsWindow = false;

    public static Skip32Cipher SharedUidCipher = new();

    // 测试模式, 在此模式下将会保持使用第一个账号管理器去登录账号到启动器主界面
    public static bool TestMode = false;


    public static CustomLoadingWindow LoginLoadingWindow;

    private static string RoomConfigPath(string fileName) =>
        Path.Combine(LauncherRootDirectory, "RoomConfig", fileName);

    public static void WriteRoomBlacklist() => WriteList("BlackList.json", RoomBlacklist);
    public static void WriteRegexBlacklist() => WriteList("RegexBlackList.json", RegexBlacklist);
    public static void ReadRoomBlacklist() => RoomBlacklist = ReadList("BlackList.json");
    public static void ReadRegexBlacklist() => RegexBlacklist = ReadList("RegexBlackList.json");

    private static void WriteList(string fileName, List<string> values) =>
        AtomicFile.WriteAllText(RoomConfigPath(fileName), JsonConvert.SerializeObject(values ?? new List<string>()));

    private static List<string> ReadList(string fileName)
    {
        var path = RoomConfigPath(fileName);
        if (!File.Exists(path))
        {
            WriteList(fileName, new List<string>());
            return new List<string>();
        }

        try
        {
            return JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(path)) ?? new List<string>();
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException ||
                                          exception is JsonException)
        {
            DefaultLogger.Warn("无法读取房间列表，保留原文件: " + fileName);
            return new List<string>();
        }
    }
}