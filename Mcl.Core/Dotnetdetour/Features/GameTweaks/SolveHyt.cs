using System;
using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;
using Mcl.Core.NeteaseProtocol;
using Newtonsoft.Json;
using WPFLauncher.Code;
using WPFLauncher.Network.Launcher;

namespace Mcl.Core.Dotnetdetour.Features.GameTweaks;

public class SolveHyt : IMethodHook
{
    [HookMethod("WPFLauncher.Network.Launcher.acp", "l")]
    public static void GetItemAddress(string resId, Action<EntityResponse<ItemAddressEntity>> action)
    {
        var result =
            JsonConvert.DeserializeObject<EntityResponse<ItemAddressEntity>>(X19Http.Post("/item-address/get",
                "{\"item_id\":\"" + resId + "\"}"));
        if (result.entity.game_status != 0) result.entity.game_status = 0;

        if (result.entity.port == 0) result.entity.port = 25565;
        action(result);
    }
}