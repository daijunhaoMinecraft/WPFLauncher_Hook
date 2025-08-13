﻿using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WPFLauncher.Code;
using WPFLauncher.Network.Launcher;
using WPFLauncher.Network.Message;
using WPFLauncher.Network.Protocol.LobbyGame;
using WPFLauncher.Util;

namespace DotNetTranstor.Hookevent
{
    public class Path_Bool
    {
        public static string Default_HttpAddress = "http://127.0.0.1:4601/";
        public static string Default_WebSocketAddress = "ws://127.0.0.1:4600/";
        public static int HttpPort = 4601;
        public static int WebSocketPort = 4600;
        public static string Get_Recv_String_ChatResult = string.Empty;
        public static bool IsBypassGameUpdate_Bedrock = false;
        public static bool IsStartWebSocket = false;
        public static bool IsDebug = false;
        public static EntityResponse<LobbyGameRoomEntity> RoomInfo = null;
        public static string Password = string.Empty;
        public static string Mac_Addr = string.Empty;
        public static string Random_Mac_Addr = string.Empty;
        public static bool IsEnableX64mc = false;
        public static JArray RecvList = new JArray();
        public static bool EnableRoomBlacklist = false;
        public static List<string> RoomBlacklist = new List<string>();
        public static bool EnableRegexBlacklist = false;
        public static List<string> RegexBlacklist = new List<string>();
        public static int MaxRoomCount = 16;
        public static bool IsLogin = false;
        public static bool AlwaysSaveWorld = true;
        public static string NeteaseUpdateDomainhttp = "https://x19.update.netease.com";
        public static List<DecryptItemInfo> DecryptItems = new List<DecryptItemInfo>();
        public static JArray RoomPlayerList = new JArray();
        public static long JoinOrCreateTime = 0;
        public class DecryptItemInfo
        {
            /// <summary>
            /// 组件ID
            /// </summary>
            public string item_id { get; set; }
            /// <summary>
            /// 解密密钥
            /// </summary>
            public string decryptkey { get; set; }
            /// <summary>
            /// 组件UUID
            /// </summary>
            public string uuid { get; set; }
        }
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