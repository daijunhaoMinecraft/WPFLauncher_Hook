using WPFLauncher.Common;
using WPFLauncher.Model;

namespace Mcl.Core.Dotnetdetour.Hookevent;
// (Windows 7) 解决我的世界基岩版房间版本过低问题
public class FixErr : IMethodHook
{
    [OriginalMethod]
    public void SolveRoomErrCheckOriginal(GameM qgu, BaseWindow qgv)
    {
    }
    [HookMethod("WPFLauncher.Manager.Game.Pipeline.Task.awj", "hp", "SolveRoomErrCheckOriginal")]
    public void SolveRoomErrCheck(GameM qgu, BaseWindow qgv)
    {
        SolveRoomErrCheckOriginal(null, qgv);
    }
}