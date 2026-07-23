using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;
using Mcl.Core.Dotnetdetour.Models.Config;
using WPFLauncher.Model;
using WPFLauncher.Model.Game.CppGame.CppLanGame;
using WPFLauncher.Network.CppTransfer;

namespace Mcl.Core.Dotnetdetour.Features.AntiCheatBypass;

public class DisableCppLow : IMethodHook
{
    [OriginalMethod]
    public static ama ProcessVersionOriginal(GetRoomListPost.RoomEntity roomEntity)
    {
        return null;
    }

    // 处理CPP版本问题
    [HookMethod("WPFLauncher.Model.Game.Factory.all", "c", "ProcessVersionOriginal")]
    public static ama ProcessVersion(GetRoomListPost.RoomEntity roomEntity)
    {
        var result = ProcessVersionOriginal(roomEntity);
        WpfConfig.DefaultLogger.Info(
            $"BedrockRoomStatusCheck, result: {result.MaintainStatus}, Fix To: GAME_STATUS_OK");
        result.MaintainStatus = MaintainStatus.GAME_STATUS_OK;
        return result;
    }
}