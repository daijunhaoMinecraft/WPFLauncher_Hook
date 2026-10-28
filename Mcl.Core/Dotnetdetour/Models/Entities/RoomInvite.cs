using System;
using System.Text;

namespace Mcl.Core.Dotnetdetour.Models.Entities;

/// <summary>房间信息文本 (含密码), 用于复制给其他玩家的中文说明。</summary>
public sealed class RoomInvite
{
    /// <summary>第三方房间信息页面 (neteasehelper)。</summary>
    public const string RoomPageFormat = "https://neteasehelper.theconsole.top/rooms/{0}?edition=pc";

    public string RoomId { get; set; }
    public string RoomName { get; set; }
    public string Password { get; set; }
    public string OwnerId { get; set; }
    public string OwnerName { get; set; }
    public int Players { get; set; }
    public int MaxPlayers { get; set; }
    public bool AllowSave { get; set; }
    public int Visibility { get; set; }

    /// <summary>可见性的中文描述 (可选, 仅用于分享文本)。</summary>
    public string VisibilityText { get; set; }

    public int GameStatus { get; set; }

    /// <summary>生成可读的中文房间介绍。</summary>
    public string ToHumanReadableText()
    {
        var builder = new StringBuilder();
        builder.AppendLine("【MCL 联机房间信息】");
        builder.AppendLine($"房间号: {RoomName}");
        builder.AppendLine($"房间ID: {RoomId}");
        builder.AppendLine($"房主: {DescribeOwner()}");
        builder.AppendLine($"当前人数: {Players}/{MaxPlayers}");
        builder.AppendLine($"密码: {DescribePassword()}");
        builder.AppendLine($"允许保存: {(AllowSave ? "是" : "否")}");
        builder.AppendLine($"可见性: {(string.IsNullOrEmpty(VisibilityText) ? Visibility.ToString() : VisibilityText)}");
        builder.AppendLine($"游戏状态: {(GameStatus == 1 ? "在线" : "离线")}");
        var link = RoomPageLink;
        if (!string.IsNullOrEmpty(link)) builder.AppendLine($"在浏览器查看: {link}");
        return builder.ToString().TrimEnd();
    }

    public string DescribeOwner()
    {
        if (string.IsNullOrEmpty(OwnerName)) return string.IsNullOrEmpty(OwnerId) ? "未知" : OwnerId;
        return string.IsNullOrEmpty(OwnerId) ? OwnerName : $"{OwnerName}({OwnerId})";
    }

    public string DescribePassword()
    {
        return string.IsNullOrEmpty(Password) ? "无" : Password;
    }

    /// <summary>该房间在浏览器中的信息页面地址。</summary>
    public string RoomPageLink => BuildRoomPageLink(RoomId);

    public static string BuildRoomPageLink(string roomId)
    {
        return string.IsNullOrEmpty(roomId) ? null : string.Format(RoomPageFormat, roomId);
    }
}
