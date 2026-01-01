using System;
using System.Collections.Generic;
using System.IO;
using Mcl.Core.Network;
using Mcl.Core.Network.Interface;
using Newtonsoft.Json;
using WPFLauncher.Code;
using WPFLauncher.Model.AntiIndulgence;
using WPFLauncher.Util;
using MicrosoftTranslator.DotNetTranstor.Tools;

namespace DotNetTranstor.Hookevent
{
    //解决因为https://x19obtcore.nie.netease.com:8443/interconn/web/game-play-v2/start接口问题导致游客账户/防沉迷账户无法启动游戏问题
    public class No_StartGameTestfor_GameStart : IMethodHook
    {
        internal void GameTest(string ilr, int ils, List<string> ilt, Action<EntityDetailResponse<GameStartEntityV2>> ilu)
        {
        }

        [HookMethod("WPFLauncher.Network.Protocol.aei", "f", "GameTest")]
        public static void f(string ilr, int ils, List<string> ilt, Action<EntityDetailResponse<GameStartEntityV2>> ilu)
        {
            Action<EntityDetailResponse<GameStartEntityV2>> Get_Action = ilu;
            string json = "{ \"code\": 0, \"details\": \"\", \"entity\": { \"anti_addiction_info\": { \"current_online_time_sum\": 0, \"msg\": \"\", \"online_time_left\": 0, \"online_time_limit\": 0, \"online_time_sum\": 0, \"status\": 0 }, \"is_anti_addiction\": false, \"record\": null }, \"message\": \"正常返回\" }";
            Get_Action(JsonConvert.DeserializeObject<EntityDetailResponse<GameStartEntityV2>>(json));
            DebugPrint.LogDebug_NoColorSelect("[GameStart]游戏已启动");
            //EntityDetailResponse<GameStartEntityV2> gameStartEntityV2 = JsonConvert.DeserializeObject<EntityDetailResponse<GameStartEntityV2>>(json); 
        }
    }
}