using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WPFLauncher.Common;
using WPFLauncher.Manager;
using WPFLauncher.Manager.Game;

namespace Mcl.Core.DotNetTranstor.Tools;

public class ExitRoom // 退出此前进入过的房间 By Daijunhao
{
    public static bool autoExitRoom()
    {
        // 获取所有房间
        var instance = azd<aul>.Instance;
        var versionField = instance.GetType().GetField("a", BindingFlags.Public | BindingFlags.Instance);
        var version = versionField?.GetValue(instance) as string;
        string sGetAllRooms = X19Http.RequestX19Api("/online-lobby-room/query/list-room-by-res-id",
            JsonConvert.SerializeObject(new { res_id = "", offset = 0, length = 99999, version = version }));
        JObject jGetAllRooms = JObject.Parse(sGetAllRooms);
        Console.WriteLine($"[AutoExit] 成功获取到房间数:{jGetAllRooms["total"].ToString()}");
        foreach (var jGetRooms in jGetAllRooms["entities"])
        {
            if (jGetRooms["member_uids"].ToObject<List<string>>().Contains(azd<arf>.Instance.User.Id))
            {
                Console.WriteLine($"[AutoExit] 成功获取到玩家加入的房间,房间号: {jGetRooms["room_name"]}, entity_id: {jGetRooms["entity_id"]}");
                Console.WriteLine($"[AutoExit] 正在退出房间...");
                string sExitRoomResult = X19Http.RequestX19Api("/online-lobby-room-enter/leave-room",
                    JsonConvert.SerializeObject(new { room_id = jGetRooms["entity_id"] }));
                Console.WriteLine($"[AutoExit] 退出房间返回:{Regex.Escape(sExitRoomResult)}");
                if (JObject.Parse(sExitRoomResult)["code"].ToObject<int>() == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"[AutoExit] 退出房间成功!");
                    Console.ForegroundColor = ConsoleColor.White;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[AutoExit] 退出房间失败,返回信息:{JObject.Parse(sExitRoomResult)["message"].ToString()}!");
                    Console.ForegroundColor = ConsoleColor.White;
                    return false;
                }
                return true;
            }
        }
        Console.WriteLine("[AutoExit] Fail: 找不到玩家所在的房间");
        return false;
    }
}