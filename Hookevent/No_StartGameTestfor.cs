using System;
using System.IO;
using Newtonsoft.Json;
using WPFLauncher.Code;
using WPFLauncher.Common;
using WPFLauncher.Manager;
using WPFLauncher.Model;
using WPFLauncher.Model.AntiIndulgence;
using WPFLauncher.Util;

namespace DotNetTranstor.Hookevent
{
    //解决因为https://x19obtcore.nie.netease.com:8443/interconn/web/game-play-v2/login-start接口问题导致游客账户/防沉迷账户无法登录问题
    public class No_StartGameTestfor : IMethodHook
    {
        public static EntityDetailResponse<GameStartEntityV2> PostTest()
        {
            return new EntityDetailResponse<GameStartEntityV2>();
        }

        [HookMethod("WPFLauncher.Network.Protocol.aeh", "k", "PostTest")]
        public static EntityDetailResponse<GameStartEntityV2> k()
        {
            string json = "{\n   \"code\" : 0,\n   \"details\" : \"\",\n   \"entity\" : {\n      \"anti_addiction_info\" : {\n         \"current_online_time_sum\" : 0,\n         \"msg\" : \"\",\n         \"online_time_left\" : 0,\n         \"online_time_limit\" : 0,\n         \"online_time_sum\" : 0,\n         \"status\" : 0\n      },\n      \"is_anti_addiction\" : false,\n      \"record\" : null\n   },\n   \"message\" : \"正常返回\"\n}";
            EntityDetailResponse<GameStartEntityV2> gameStartEntityV2 = JsonConvert.DeserializeObject<EntityDetailResponse<GameStartEntityV2>>(json);
            return gameStartEntityV2;
        }
    }
}