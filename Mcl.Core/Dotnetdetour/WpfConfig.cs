using System;
using System.Collections.Generic;
using System.IO;
using Mcl.Core.Dotnetdetour.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using WPFLauncher.Code;
using WPFLauncher.Network.Protocol.LobbyGame;
using WPFLauncher.Util;

namespace Mcl.Core.Dotnetdetour
{
    public class WpfConfig
    {
        public static string Version = "4.0.8-Public-Beta9";
        public static string Default_WebSocketAddress = "ws://127.0.0.1:4600/websocket";
        public static string Default_HttpAddress = "http://127.0.0.1:4600/";
        public static int HttpPort = 4600;
        public static bool CookieLoginWithoutMpay = false;
        public static string Get_Recv_String_ChatResult = string.Empty;
        public static bool IsBypassGameUpdate_Bedrock = false;
        public static bool IsDebug = false;
        public static EntityResponse<LobbyGameRoomEntity> RoomInfo = null;
        public static string Password = string.Empty;
        public static string Mac_Addr = string.Empty;
        public static string Random_Mac_Addr = string.Empty;
        public static bool IsEnableX64mc = true;
        public static JArray RecvList = new JArray();
        public static bool EnableRoomBlacklist = false;
        public static List<string> RoomBlacklist = new List<string>();
        public static List<string> RegexBlacklist = new List<string>();
        public static int MaxRoomCount = 16;
        public static bool IsLogin = false;
        public static bool AlwaysSaveWorld = true;

        public static string ServerListUrl = "https://x19.update.netease.com/serverlist/release.json";
        public static Uri ServerListUri = new Uri(ServerListUrl);
        public static JObject ServerList = new();

        public static JArray RoomPlayerList = new JArray();
        public static long JoinOrCreateTime = 0;
        public static string wpflauncherRoot = Directory.GetCurrentDirectory();
        public static bool IsStartWebSocket = false;
        public static bool IsCustomIP = false;
        public static bool IsSelectedIP = true;
        public static bool NoTwoExitMessage = true;
        public static int JoinFailRetry = 0;
        public static string JavaGamePath = string.Empty;
        public static bool UseNetworkMode = false;
        // threading Download Config
        public static int MaxThread = 8;
        public static bool IsDownloadMultiConfig = false;
        public static int LimitDownload = 30;
        public static string BedrockPath = tb.s;
        public static bool IsWindowTopMost = true;
        public static bool EnableModsInject = false;
        public static bool IsLogOutputFolder = true;
        
        // custom Settings
        public static bool EnableCustomBedrockSelect = false;
        public static bool EnableCustomAccountLogin = false;
        public static bool MpayUnless = false;
        

        public static bool KeepOffDeleteLastResourcepacks = false;
        public static bool KeepOffDeleteLastConfig = false;
        public static bool KeepOffDeleteLastShaderPacks = false;

        public static Logger DefaultLogger = LogManager.GetCurrentClassLogger();

        public static List<FriendStatus> ListFriendStatus = new List<FriendStatus>();

        public static void WriteRoomBlacklist()
        {
            string blacklistFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "RoomConfig");
            string blacklistFilePath = Path.Combine(blacklistFolderPath, "BlackList.json");
            File.WriteAllText(blacklistFilePath, JsonConvert.SerializeObject(WpfConfig.RoomBlacklist));
        }
        public static void WriteRegexBlacklist()
        {
            string regexBlacklistFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "RoomConfig");
            string regexBlacklistFilePath = Path.Combine(regexBlacklistFolderPath, "RegexBlackList.json");
            File.WriteAllText(regexBlacklistFilePath, JsonConvert.SerializeObject(WpfConfig.RegexBlacklist));
        }
        // Read
        public static void ReadRoomBlacklist()
        {
            string blacklistFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "RoomConfig");
            string blacklistFilePath = Path.Combine(blacklistFolderPath, "BlackList.json");
            if (!Directory.Exists($"{WpfConfig.wpflauncherRoot}/RoomConfig"))
            {
                // Create Directory RoomConfig
                Directory.CreateDirectory($"{WpfConfig.wpflauncherRoot}/RoomConfig");
                WpfConfig.DefaultLogger.Warn("未创建RoomConfig文件夹,已自动创建");
            }
            if (!File.Exists($"{WpfConfig.wpflauncherRoot}/RoomConfig/BlackList.json"))
            {
                // Init BlackList
                File.WriteAllText($"{WpfConfig.wpflauncherRoot}/RoomConfig/BlackList.json", "[]");
                WpfConfig.DefaultLogger.Warn("[Warn] 未创建RoomConfig/BlackList.json文件,已自动创建");
            }
            WpfConfig.RoomBlacklist = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(blacklistFilePath));
        }
    }
}