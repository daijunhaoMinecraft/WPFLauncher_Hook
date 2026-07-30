using System;
using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;
using Mcl.Core.Dotnetdetour.Models.Config;
using Mcl.Core.Dotnetdetour.Models.Entity;
using WPFLauncher.Common;
using WPFLauncher.Manager.Configuration;

namespace Mcl.Core.Dotnetdetour.Features.GameTweaks;

public class AddCustomNetGameServer : IMethodHook
{
    // PE 网络游戏自定义添加
    [OriginalMethod]
    private void RefreshRecentNetGameList(string unkString, Action afterAction = null, Action unk = null)
    {
    }
    
    [HookMethod("WPFLauncher.ViewModel.Launcher.kh", "b", "RefreshRecentNetGameList")]
    private void RefreshRecentNetGameListHook(string unkString, Action afterAction = null, Action unk = null)
    {
        int insertIndex = 0;
        foreach (Tuple<string, NetGameResponse> recentList in WpfConfig.CustomRecentList)
        {
            string item = recentList.Item1;
            if (!azf<axi>.Instance.NetGameConfig.OrderList.Contains(item))
            {
                azf<axi>.Instance.NetGameConfig.OrderList.Insert(0, item);
            }
        }
        WpfConfig.DefaultLogger.Debug("调用最近网络游戏刷新成功!");
        RefreshRecentNetGameList(unkString, afterAction, unk);
    }
}