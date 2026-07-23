using System;
using System.Runtime.CompilerServices;
using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;
using Mcl.Core.Dotnetdetour.Models.Config;
using WPFLauncher.Code;
using WPFLauncher.Network.Protocol.LobbyGame;

namespace Mcl.Core.Dotnetdetour.Features.NetworkAndRoom;

internal class LobbyGameList : IMethodHook
{
    [OriginalMethod]
    public static void GetLobbyGameList(string resId, int jni, int jnj,
        Action<EntityListResponse<LobbyGameRoomEntity>> response)
    {
    }

    [CompilerGenerated]
    [HookMethod("WPFLauncher.Network.Protocol.LobbyGame.age", "d", "GetLobbyGameList")]
    // Token: 0x060045FE RID: 17918 RVA: 0x000ED080 File Offset: 0x000EB280
    public static void GetLobbyGameListHook(string resId, int jni, int jnj,
        Action<EntityListResponse<LobbyGameRoomEntity>> response)
    {
        Console.WriteLine($"[Online]成功将房间最大显示个数修改成{WpfConfig.MaxRoomCount.ToString()}!");
        Console.WriteLine($"[Online]获取房间列表ResID:{resId}");
        GetLobbyGameList(resId, jni, WpfConfig.MaxRoomCount, response);
    }
}