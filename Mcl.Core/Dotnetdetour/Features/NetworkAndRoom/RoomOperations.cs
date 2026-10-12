using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Mcl.Core.Dotnetdetour.Models.Config;
using Mcl.Core.Dotnetdetour.Utilities.Diagnostics;
using Mcl.Core.NeteaseProtocol;
using Mcl.Core.Tools;
using Newtonsoft.Json;
using WPFLauncher.Code;
using WPFLauncher.Common;
using WPFLauncher.Manager;
using WPFLauncher.Model.Game;
using WPFLauncher.Network.Protocol.LobbyGame;

namespace Mcl.Core.Dotnetdetour.Features.NetworkAndRoom;

/// <summary>
///     房间操作桥接: MCL 房间管理窗口执行宿主房间窗口的公开命令, 覆盖
///     开始/进入游戏、邀请、备份存档、重置存档、举报房间、退出房间、踢出玩家与添加好友,
///     因此确认框、失败提示和插件已有 Hook 的行为与启动器自带房间窗口一致。
///     宿主房间 ViewModel 是 internal 类型, 这里按项目既有做法通过反射访问。
/// </summary>
internal static class RoomOperations
{
    private const BindingFlags InstanceFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    private const string NoHostRoom = "未检测到启动器房间窗口，可在房间创建/加入后重试。";

    public static bool HasHostRoom => GetHostRoomViewModel() != null;

    /// <summary>
    ///     从服务器读取当前房间信息 (只读刷新, 不会走进房/退房流程)。
    ///     该接口返回的 fids 可能为空, 此时保留本地成员列表。
    /// </summary>
    public static bool TryRefreshRoomInfo(out string error)
    {
        var current = WpfConfig.RoomInfo?.entity;
        if (current == null || string.IsNullOrEmpty(current.entity_id))
        {
            error = "当前未加入联机大厅房间。";
            return false;
        }

        try
        {
            var json = X19Http.Post("/online-lobby-room/get",
                JsonConvert.SerializeObject(new { room_id = current.entity_id }));
            var response = JsonConvert.DeserializeObject<EntityResponse<LobbyGameRoomEntity>>(json);
            if (response == null || response.code != 0 || response.entity == null)
            {
                error = string.IsNullOrEmpty(response?.message) ? "服务器未返回房间信息。" : response.message;
                return false;
            }

            var entity = response.entity;
            if (entity.fids == null || entity.fids.Count == 0)
                entity.fids = current.fids == null ? new List<string>() : new List<string>(current.fids);

            var selfId = CurrentUserId();
            if (!string.IsNullOrEmpty(selfId) && !entity.fids.Contains(selfId)) entity.fids.Add(selfId);
            entity.cur_num = (uint)entity.fids.Count;

            WpfConfig.RoomInfo = response;
            error = null;
            return true;
        }
        catch (Exception exception)
        {
            PluginLog.Error("RoomManage", exception, "Refreshing the room info failed.");
            error = exception.Message;
            return false;
        }
    }

    private static string CurrentUserId()
    {
        try
        {
            var userId = azf<arg>.Instance?.User?.Id;
            if (!string.IsNullOrEmpty(userId)) return userId;
        }
        catch (Exception)
        {
            // 宿主尚未登录时退回插件记录的账号 ID。
        }

        return X19Crypt.UserId ?? string.Empty;
    }

    /// <summary>宿主的房间游戏状态。</summary>
    public static LobbyGameState GetGameState()
    {
        return GetSelectedGame()?.State ?? LobbyGameState.None;
    }

    public static bool TryOperateGame(out string error)
    {
        return TryExecuteRoomCommand("GameOperateCommand", out error);
    }

    public static bool TryInvitePlayers(out string error)
    {
        return TryExecuteRoomCommand("InviteCommand", out error);
    }

    public static bool TryBackupSaves(out string error)
    {
        return TryExecuteRoomCommand("BackupSaveCommand", out error);
    }

    public static bool TryResetSave(out string error)
    {
        return TryExecuteRoomCommand("ResetSaveCommand", out error);
    }

    public static bool TryReportRoom(out string error)
    {
        return TryExecuteRoomCommand("ReportCommand", out error);
    }

    public static bool TryLeaveRoom(out string error)
    {
        return TryExecuteRoomCommand("CloseCommand", out error);
    }

    public static bool TryKickPlayer(string userId, out string error)
    {
        return TryExecutePlayerCommand("KickOutPlayerCommand", userId, out error);
    }

    public static bool TryAddFriend(string userId, out string error)
    {
        return TryExecutePlayerCommand("AddFriendCommand", userId, out error);
    }

    public static bool TryTransferHost(string userId, out string error)
    {
        return TryExecutePlayerCommand("ResetHostCommand", userId, out error);
    }

    private static bool TryExecuteRoomCommand(string commandProperty, out string error)
    {
        var viewModel = GetHostRoomViewModel();
        if (viewModel == null)
        {
            error = NoHostRoom;
            return false;
        }

        return TryExecuteCommand(viewModel, commandProperty, null, out error);
    }

    private static bool TryExecutePlayerCommand(string commandProperty, string userId, out string error)
    {
        var viewModel = GetHostRoomViewModel();
        if (viewModel == null)
        {
            error = NoHostRoom;
            return false;
        }

        var player = FindPlayer(viewModel, userId);
        if (player == null)
        {
            error = "该玩家已不在启动器房间列表中，请刷新后重试。";
            return false;
        }

        return TryExecuteCommand(viewModel, commandProperty, player, out error);
    }

    private static bool TryExecuteCommand(object viewModel, string commandProperty, object parameter,
        out string error)
    {
        try
        {
            var command = GetProperty(viewModel, commandProperty) as RelayCommand;
            if (command == null)
            {
                error = $"启动器未提供房间命令 {commandProperty}，可能版本不匹配。";
                return false;
            }

            command.Execute(parameter);
            error = null;
            return true;
        }
        catch (Exception exception)
        {
            PluginLog.Error("RoomManage", exception, $"Executing the host room command {commandProperty} failed.");
            error = $"调用启动器房间命令失败: {exception.Message}";
            return false;
        }
    }

    private static object FindPlayer(object viewModel, string userId)
    {
        if (string.IsNullOrEmpty(userId) || !(GetProperty(viewModel, "Players") is IEnumerable players)) return null;

        foreach (var player in players)
        {
            if (player == null) continue;
            var value = GetProperty(player, "UserId");
            if (value == null) continue;
            if (string.Equals(Convert.ToString(value, CultureInfo.InvariantCulture), userId,
                    StringComparison.Ordinal))
                return player;
        }

        return null;
    }

    /// <summary>宿主当前选中的房间对象 (akv), 状态变化会通过 PropertyChanged 通知。</summary>
    public static akv GetSelectedGame()
    {
        return GetProperty(GetHostRoomViewModel(), "SelectedGame") as akv;
    }

    private static object GetHostRoomViewModel()
    {
        var view = RoomWindowReplacement.GetHostRoomView();
        if (view == null) return null;

        try
        {
            return view.Dispatcher.CheckAccess()
                ? view.DataContext
                : view.Dispatcher.Invoke(() => view.DataContext);
        }
        catch (Exception exception)
        {
            PluginLog.Debug("RoomManage", $"Reading the host room view model failed: {exception.Message}");
            return null;
        }
    }

    private static object GetProperty(object instance, string name)
    {
        if (instance == null) return null;
        return instance.GetType().GetProperty(name, InstanceFlags)?.GetValue(instance);
    }
}
