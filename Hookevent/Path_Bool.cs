using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WPFLauncher.Code;
using WPFLauncher.Network.Message;
using WPFLauncher.Network.Protocol.LobbyGame;
using WPFLauncher.Util;

namespace DotNetTranstor.Hookevent
{
    public class Path_Bool
    {
        public static bool IsBypassGameUpdate_Bedrock = false;
        public static bool IsStartWebSocket = false;
        public static bool IsDebug = false;
        public static EntityResponse<LobbyGameRoomEntity> RoomInfo = null;
        public static string Password = string.Empty;
        public static string Mac_Addr = string.Empty;
        public static string Random_Mac_Addr = string.Empty;
        public static bool IsPrintPostInfo = false;
        public static bool EnableModsInject = false;
        public static bool IsEnableX64mc = false;
        public static JArray RecvList = new JArray();
        public static bool EnableRoomBlacklist = false;
        public static List<string> RoomBlacklist = new List<string>();
        public static bool EnableRegexBlacklist = false;
        public static List<string> RegexBlacklist = new List<string>();
        public static void WriteRoomBlacklist()
        {
            string blacklistFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "RoomConfig");
            string blacklistFilePath = Path.Combine(blacklistFolderPath, "BlackList.json");
            File.WriteAllText(blacklistFilePath, JsonConvert.SerializeObject(Path_Bool.RoomBlacklist));
        }
        public static void WriteRegexBlacklist()
        {
            string regexBlacklistFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "RoomConfig");
            string regexBlacklistFilePath = Path.Combine(regexBlacklistFolderPath, "RegexBlackList.json");
            File.WriteAllText(regexBlacklistFilePath, JsonConvert.SerializeObject(Path_Bool.RegexBlacklist));
        }
    }
}