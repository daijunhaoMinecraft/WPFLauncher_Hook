using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Mcl.Core.Dotnetdetour.Models.Config;
using Mcl.Core.Dotnetdetour.Utilities.Diagnostics;
using Mcl.Core.NeteaseProtocol;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mcl.Core.Dotnetdetour.Features.NetworkAndRoom;

/// <summary>
///     房间黑名单: UserID 名单与正则表达式名单 (匹配玩家名称) 的增删、落盘与踢人。
///     加入名单后立即踢出对应玩家, 与插件 Web 端 /Room/AddBlacklist 的行为保持一致。
/// </summary>
internal static class RoomBlacklist
{
    public static bool IsEnabled => WpfConfig.EnableRoomBlacklist;

    public static List<string> UserIds => WpfConfig.RoomBlacklist ?? (WpfConfig.RoomBlacklist = new List<string>());

    public static List<string> Patterns =>
        WpfConfig.RegexBlacklist ?? (WpfConfig.RegexBlacklist = new List<string>());

    public static bool AddUser(string userId, out string error)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            error = "UserID 不能为空。";
            return false;
        }

        if (UserIds.Contains(userId))
        {
            error = "该 UserID 已在黑名单中。";
            return false;
        }

        UserIds.Add(userId);
        WpfConfig.WriteRoomBlacklist();
        error = null;
        return true;
    }

    public static bool RemoveUser(string userId, out string error)
    {
        if (!UserIds.Remove(userId))
        {
            error = "该 UserID 不在黑名单中。";
            return false;
        }

        WpfConfig.WriteRoomBlacklist();
        error = null;
        return true;
    }

    public static bool AddPattern(string pattern, out string error)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            error = "正则表达式不能为空。";
            return false;
        }

        try
        {
            _ = new Regex(pattern);
        }
        catch (ArgumentException exception)
        {
            error = $"正则表达式无效: {exception.Message}";
            return false;
        }

        if (Patterns.Contains(pattern))
        {
            error = "该正则表达式已存在。";
            return false;
        }

        Patterns.Add(pattern);
        WpfConfig.WriteRegexBlacklist();
        error = null;
        return true;
    }

    public static bool RemovePattern(string pattern, out string error)
    {
        if (!Patterns.Remove(pattern))
        {
            error = "该正则表达式不在名单中。";
            return false;
        }

        WpfConfig.WriteRegexBlacklist();
        error = null;
        return true;
    }

    /// <summary>踢出玩家: 先用宿主房间命令, 玩家已不在宿主列表时回退到踢人接口。</summary>
    public static bool TryKickPlayer(string userId, out string error)
    {
        if (RoomOperations.TryKickPlayer(userId, out _))
        {
            error = null;
            return true;
        }

        var roomId = WpfConfig.RoomInfo?.entity?.entity_id;
        if (string.IsNullOrEmpty(roomId))
        {
            error = "当前未加入联机大厅房间。";
            return false;
        }

        try
        {
            var json = X19Http.Post("/online-lobby-member-kick",
                JsonConvert.SerializeObject(new { room_id = roomId, user_id = userId }));
            var response = JObject.Parse(json);
            var code = response["code"]?.ToObject<int>() ?? -1;
            if (code == 0)
            {
                error = null;
                return true;
            }

            error = response["message"]?.ToString() ?? $"踢出玩家失败（code={code}）。";
            return false;
        }
        catch (Exception exception)
        {
            PluginLog.Error("RoomManage", exception, "Kicking the blacklisted player failed.");
            error = exception.Message;
            return false;
        }
    }

    /// <summary>加入正则名单后, 把当前房间中名称匹配的玩家拉黑并踢出, 返回踢出人数。</summary>
    public static int KickMatchingPlayers(string pattern, out string error)
    {
        if (!AddPattern(pattern, out error)) return 0;

        var kicked = 0;
        try
        {
            var entity = WpfConfig.RoomInfo?.entity;
            if (entity?.fids == null || entity.fids.Count == 0)
            {
                error = null;
                return 0;
            }

            var players = X19Http.GetPlayersInfo(entity.fids.ToList());
            foreach (var player in players["entities"] ?? new JArray())
            {
                var name = player?["name"]?.ToString() ?? string.Empty;
                if (!Regex.IsMatch(name, pattern)) continue;

                var userId = player?["entity_id"]?.ToString();
                if (string.IsNullOrEmpty(userId)) continue;

                if (!UserIds.Contains(userId)) UserIds.Add(userId);
                if (TryKickPlayer(userId, out _)) kicked++;
                PluginLog.Info("RoomManage", $"Blacklisted regex matched player {userId}; kicked={kicked > 0}.");
            }

            WpfConfig.WriteRoomBlacklist();
            error = null;
        }
        catch (Exception exception)
        {
            PluginLog.Error("RoomManage", exception, "Kicking regex-matched players failed.");
            error = exception.Message;
        }

        return kicked;
    }
}
