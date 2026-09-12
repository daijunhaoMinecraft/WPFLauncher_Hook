using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;
using Mcl.Core.Dotnetdetour.Models.Config;
using WPFLauncher.Common;
using WPFLauncher.Manager.Game.Pipeline;
using WPFLauncher.Model;
using WPFLauncher.Model.Game.CppGame.CppLanGame;
using WPFLauncher.Network.CppTransfer;

namespace Mcl.Core.Dotnetdetour.Features.GameTweaks;

// (Windows 7) 解决我的世界基岩版房间版本过低问题
public class ForceJoinLowVersionRoom : IMethodHook
{
    [OriginalMethod]
    public void CheckCppRoomExist(avq instance, GameM gameM, BaseWindow baseWindow)
    {
    }

    [HookMethod("WPFLauncher.Manager.Game.Pipeline.Task.awj", "hp", "CheckCppRoomExist")]
    public void CheckCppRoomExistHook(avq instance, GameM gameM, BaseWindow baseWindow)
    {
        WpfConfig.DefaultLogger.Debug("成功跳过房间检查");
        instance.hs(LTaskOpcode.NEXT);
        return;
    }

    [OriginalMethod]
    public static ama ConvertToInstance(GetRoomListPost.RoomEntity room)
    {
        return null;
    }

    [HookMethod("WPFLauncher.Model.Game.Factory.all", "c", "ConvertToInstance")]
    public static ama ConvertToInstanceHook(GetRoomListPost.RoomEntity room)
    {
        ama convertResult = ConvertToInstance(room);
        if (convertResult.MaintainStatus == MaintainStatus.GAME_STATUS_LOW_VERSION)
        {
            WpfConfig.DefaultLogger.Warn($"低版本房间: {room.name}, 已强制转换成正常显示");
            convertResult.MaintainStatus = MaintainStatus.GAME_STATUS_OK;
        }
        return convertResult;
    }
}