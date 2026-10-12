using System;
using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;
using Mcl.Core.Dotnetdetour.Models.Config;
using Mcl.Core.Dotnetdetour.UI.Windows;
using Mcl.Core.Dotnetdetour.Utilities.Diagnostics;
using WPFLauncher.Common;
using WPFLauncher.Manager;
using WPFLauncher.View.Launcher.LobbyGame;

namespace Mcl.Core.Dotnetdetour.Features.NetworkAndRoom;

/// <summary>
///     用 MCL 房间管理窗口替换启动器自带的联机大厅房间窗口。
///     宿主房间窗口仍然会被创建 (它的 ViewModel 是房间状态与全部房间命令的载体), 只是不再显示;
///     <see cref="RoomManagementWindow" /> 通过 <see cref="RoomOperations" /> 调用同一批宿主命令,
///     因此功能与宿主房间窗口保持一致。
/// </summary>
internal sealed class RoomWindowReplacement : IMethodHook
{
    [OriginalMethod]
    public static void ShowWindowOriginal(BaseWindow window)
    {
    }

    // BaseWindow.d() 是宿主所有窗口的 Show; 这里只接管联机大厅房间窗口。
    [HookMethod("WPFLauncher.Common.BaseWindow", "d", "ShowWindowOriginal")]
    public static void ShowWindow(BaseWindow window)
    {
        if (!WpfConfig.UseMclRoomManagementWindow || !(window is LobbyGameRoomManagerView roomView))
        {
            ShowWindowOriginal(window);
            return;
        }

        try
        {
            RoomManagementWindow.ShowForRoom(roomView);
            PluginLog.Debug("RoomManage", "Host lobby room window replaced by the MCL room management window.");
        }
        catch (Exception exception)
        {
            // 替换失败时回退到宿主房间窗口, 不能因为插件异常让用户进不了房间。
            PluginLog.Error("RoomManage", exception, "Replacing the host room window failed; showing the host window.");
            ShowWindowOriginal(window);
        }
    }

    /// <summary>宿主联机房间窗口 (可能处于未显示状态), 房间命令都通过它的 ViewModel 执行。</summary>
    public static LobbyGameRoomManagerView GetHostRoomView()
    {
        try
        {
            return azf<apn>.Instance.k<LobbyGameRoomManagerView>();
        }
        catch (Exception exception)
        {
            PluginLog.Debug("RoomManage", $"Resolving the host room window failed: {exception.Message}");
            return null;
        }
    }
}
