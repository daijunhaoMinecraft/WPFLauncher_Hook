using System;
using System.Threading;
using System.Threading.Tasks;
using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;
using Mcl.Core.Dotnetdetour.Models.Config;
using Mcl.Core.Dotnetdetour.Utilities.Diagnostics;
using Mcl.Core.NeteaseProtocol;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WPFLauncher.Code;
using WPFLauncher.Network.Protocol;
using WPFLauncher.Network.Protocol.LobbyGame;
using WPFLauncher.Network.TransService;

namespace Mcl.Core.Dotnetdetour.Features.NetworkAndRoom;

/// <summary>
///     联机大厅房间设置更新请求 (/online-lobby-room/update)。
///     宿主原始请求只包含 room_id/visibility/allow_save/owner_id, 服务端因此无法修改密码。
///     这里用 detour 接管宿主请求, 在保留宿主网络栈 (签名、动态令牌、回调线程) 的前提下补充
///     password 字段, 支持房主设置、修改和取消房间密码; 宿主自身的房间设置更新也会带上当前密码。
/// </summary>
internal sealed class RoomSettingsUpdater : IMethodHook
{
    public const string UpdateRoomResource = "/online-lobby-room/update";

    /// <summary>同一线程内的一次性待处理密码修改, 只允许紧随其后的那次宿主请求消费。</summary>
    [ThreadStatic] private static PasswordChangeTicket pendingPasswordChange;

    /// <summary>一次房间设置更新的结果, 供界面显示。</summary>
    public sealed class RoomUpdateResult
    {
        public bool Success { get; internal set; }
        public int Code { get; internal set; }
        public string Message { get; internal set; } = string.Empty;
        public bool PasswordChangeRequested { get; internal set; }
        public bool PasswordChangeApplied { get; internal set; }
    }

    internal sealed class PasswordChangeTicket
    {
        private int handled;

        public PasswordChangeTicket(string roomId, string password)
        {
            RoomId = roomId ?? string.Empty;
            Password = password ?? string.Empty;
        }

        public string RoomId { get; }

        public string Password { get; }

        /// <summary>detour 是否已经把密码写进宿主请求。</summary>
        public bool IsHandled => Volatile.Read(ref handled) != 0;

        public void MarkHandled() => Volatile.Write(ref handled, 1);
    }

    /// <summary>
    ///     由插件界面发起房间设置更新。
    ///     <paramref name="newPassword" /> 为 null 表示只更新可见性/存档设置并保留当前密码;
    ///     空字符串表示取消密码保护; 其他值表示设置或修改密码。
    /// </summary>
    public static void ApplyRoomSettings(string roomId, RoomVisibleStatus visibility, bool allowSave, string ownerId,
        string newPassword, Action<RoomUpdateResult> completed)
    {
        var passwordChangeRequested = newPassword != null;
        if (string.IsNullOrEmpty(roomId))
        {
            completed?.Invoke(Failure("房间ID为空，无法更新房间设置。", passwordChangeRequested));
            return;
        }

        if (!uint.TryParse(ownerId, out var ownerUserId))
        {
            completed?.Invoke(Failure("房主ID无效，无法更新房间设置。", passwordChangeRequested));
            return;
        }

        var ticket = passwordChangeRequested ? new PasswordChangeTicket(roomId, newPassword) : null;
        pendingPasswordChange = ticket;

        Action<EntityResponse<ResetRoomSettingRequest>> hostCallback = response =>
        {
            // detour 未接管时宿主发出的是不带密码的旧请求, 其结果由下面的直连回退流程报告。
            if (ticket != null && !ticket.IsHandled) return;
            Report(ticket, response?.code ?? -1, response?.message, completed);
        };

        var hostInvoked = TryInvokeHostUpdate(roomId, (int)visibility, allowSave, ownerUserId, hostCallback);
        pendingPasswordChange = null;
        if (hostInvoked && (ticket == null || ticket.IsHandled)) return;

        PluginLog.Warn("Network", hostInvoked
            ? "Room update detour did not handle the password field; sending through the plugin client."
            : "Host room update entry point is unavailable; sending through the plugin client.");

        var fallbackTicket = ticket;
        Task.Run(() => SendUpdateDirectly(roomId, (int)visibility, allowSave, ownerUserId, fallbackTicket?.Password,
            fallbackTicket, completed));
    }

    [OriginalMethod]
    public static void UpdateRoomSettingOriginal(string roomId, int visibility, bool allowSave, uint ownerId,
        Action<EntityResponse<ResetRoomSettingRequest>> callback)
    {
    }

    [HookMethod("WPFLauncher.Network.Protocol.LobbyGame.age", "b", "UpdateRoomSettingOriginal")]
    public static void UpdateRoomSetting(string roomId, int visibility, bool allowSave, uint ownerId,
        Action<EntityResponse<ResetRoomSettingRequest>> callback)
    {
        var ticket = TakePendingTicket(roomId);
        var password = ticket?.Password ?? WpfConfig.Password;
        if (ticket == null && string.IsNullOrEmpty(password))
        {
            // 房间没有密码, 保持宿主原始请求 (不产生多余的 password 字段)。
            UpdateRoomSettingOriginal(roomId, visibility, allowSave, ownerId, callback);
            return;
        }

        try
        {
            aet.g<EntityResponse<ResetRoomSettingRequest>>(UpdateRoomResource,
                BuildRequestBody(roomId, visibility, allowSave, ownerId, password),
                (response, handle) => callback?.Invoke(response?.Data), aeq.a, null, false);
            ticket?.MarkHandled();
            PluginLog.Debug("Network", ticket == null
                ? "Room update request sent with the current room password."
                : "Room update request sent with a new room password.");
        }
        catch (Exception exception)
        {
            PluginLog.Error("Network", exception, "Failed to send the room update request through the host stack.");
            // 有密码修改时留给插件直连回退, 避免用一个不带密码的请求掩盖失败。
            if (ticket == null) UpdateRoomSettingOriginal(roomId, visibility, allowSave, ownerId, callback);
        }
    }

    private static PasswordChangeTicket TakePendingTicket(string roomId)
    {
        var ticket = pendingPasswordChange;
        pendingPasswordChange = null;
        if (ticket == null) return null;
        if (string.Equals(ticket.RoomId, roomId, StringComparison.Ordinal)) return ticket;

        // 房间号不匹配时保留待处理状态, 交给真正针对该房间的请求。
        pendingPasswordChange = ticket;
        return null;
    }

    private static bool TryInvokeHostUpdate(string roomId, int visibility, bool allowSave, uint ownerId,
        Action<EntityResponse<ResetRoomSettingRequest>> callback)
    {
        try
        {
            age.b(roomId, visibility, allowSave, ownerId, callback);
            return true;
        }
        catch (Exception exception)
        {
            PluginLog.Error("Network", exception, "Calling the host room update entry point failed.");
            return false;
        }
    }

    private static void SendUpdateDirectly(string roomId, int visibility, bool allowSave, uint ownerId,
        string password, PasswordChangeTicket ticket, Action<RoomUpdateResult> completed)
    {
        try
        {
            var json = X19Http.Post(UpdateRoomResource,
                BuildRequestBody(roomId, visibility, allowSave, ownerId, password));

            EntityResponse<ResetRoomSettingRequest> response = null;
            try
            {
                response = JsonConvert.DeserializeObject<EntityResponse<ResetRoomSettingRequest>>(json);
            }
            catch (Exception parseException)
            {
                PluginLog.Error("Network", parseException, "Parsing the room update response failed.");
            }

            Report(ticket, response?.code ?? -1, response?.message, completed);
        }
        catch (Exception exception)
        {
            PluginLog.Error("Network", exception, "Direct room update request failed.");
            Report(ticket, -1, "网络请求失败，请查看日志。", completed);
        }
    }

    private static string BuildRequestBody(string roomId, int visibility, bool allowSave, uint ownerId, string password)
    {
        var body = new JObject
        {
            ["visibility"] = visibility,
            ["allow_save"] = allowSave,
            ["owner_id"] = ownerId,
            ["room_id"] = roomId
        };
        if (password != null) body["password"] = password;
        return body.ToString(Formatting.None);
    }

    private static void Report(PasswordChangeTicket ticket, int code, string message,
        Action<RoomUpdateResult> completed)
    {
        var success = code == 0;
        if (success && ticket != null)
        {
            WpfConfig.Password = ticket.Password;
            if (WpfConfig.RoomInfo?.entity != null) WpfConfig.RoomInfo.entity.password = ticket.Password.Length > 0;
            PluginLog.Info("Network", ticket.Password.Length == 0
                ? "Room password protection disabled."
                : "Room password updated.");
        }

        completed?.Invoke(new RoomUpdateResult
        {
            Success = success,
            Code = code,
            Message = message ?? string.Empty,
            PasswordChangeRequested = ticket != null,
            PasswordChangeApplied = success && ticket != null
        });
    }

    private static RoomUpdateResult Failure(string message, bool passwordChangeRequested)
    {
        return new RoomUpdateResult
        {
            Success = false,
            Code = -1,
            Message = message,
            PasswordChangeRequested = passwordChangeRequested
        };
    }
}
