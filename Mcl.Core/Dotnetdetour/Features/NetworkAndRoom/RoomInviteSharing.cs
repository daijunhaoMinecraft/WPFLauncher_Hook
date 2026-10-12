using System;
using System.ComponentModel;
using System.Linq;
using Mcl.Core.Dotnetdetour.Models.Config;
using Mcl.Core.Dotnetdetour.Models.Entities;
using Mcl.Core.Dotnetdetour.Utilities.Diagnostics;
using Mcl.Core.NeteaseProtocol;
using WPFLauncher.Network.TransService;

namespace Mcl.Core.Dotnetdetour.Features.NetworkAndRoom;

/// <summary>房间分享文本: 把当前房间信息整理成可直接粘贴给他人的中文说明 (含密码)。</summary>
internal static class RoomInviteSharing
{
    public static string BuildRoomPageLink(string roomId)
    {
        return RoomInvite.BuildRoomPageLink(roomId);
    }

    /// <summary>生成中文房间信息文本。当前不在房间时返回 null。</summary>
    public static string BuildRoomText()
    {
        var entity = WpfConfig.RoomInfo?.entity;
        if (entity == null || string.IsNullOrEmpty(entity.entity_id)) return null;

        return new RoomInvite
        {
            RoomId = entity.entity_id,
            RoomName = entity.room_name,
            Password = WpfConfig.Password ?? string.Empty,
            OwnerId = entity.owner_id,
            OwnerName = TryGetPlayerName(entity.owner_id),
            Players = (int)entity.cur_num,
            MaxPlayers = (int)entity.max_count,
            AllowSave = entity.allow_save,
            Visibility = (int)entity.visibility,
            VisibilityText = DescribeVisibility(entity.visibility),
            GameStatus = entity.game_status
        }.ToHumanReadableText();
    }

    private static string TryGetPlayerName(string userId)
    {
        if (string.IsNullOrEmpty(userId)) return null;
        try
        {
            return X19Http.GetPlayerInfo(userId)?["entity"]?["name"]?.ToString();
        }
        catch (Exception exception)
        {
            PluginLog.Debug("RoomManage", $"Resolving the owner name failed: {exception.Message}");
            return null;
        }
    }

    private static string DescribeVisibility(RoomVisibleStatus visibility)
    {
        try
        {
            var description = (DescriptionAttribute)visibility.GetType()
                .GetField(visibility.ToString())
                ?.GetCustomAttributes(typeof(DescriptionAttribute), false)
                .FirstOrDefault();
            return string.IsNullOrEmpty(description?.Description)
                ? visibility.ToString()
                : description.Description;
        }
        catch (Exception)
        {
            return visibility.ToString();
        }
    }
}
