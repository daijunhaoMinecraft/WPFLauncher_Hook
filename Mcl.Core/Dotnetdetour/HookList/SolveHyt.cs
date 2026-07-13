using System;
using Mcl.Core.Dotnetdetour.Tools;
using Newtonsoft.Json;
using WPFLauncher.Code;
using WPFLauncher.Network.Launcher;

namespace Mcl.Core.Dotnetdetour.HookList;

public class SolveHyt : IMethodHook
{
    [HookMethod("WPFLauncher.Network.Launcher.acp", "l", null)]
    public static void l(string hwu, Action<EntityResponse<ItemAddressEntity>> action)
    {
        EntityResponse<ItemAddressEntity> result = JsonConvert.DeserializeObject<EntityResponse<ItemAddressEntity>>(X19Http.RequestX19Api("/item-address/get", "{\"item_id\":\"" + hwu + "\"}"));
        if (result.entity.game_status != 0)
        {
            result.entity.game_status = 0;
        }
        
        if (result.entity.port == 0)
        {
            result.entity.port = 25565;
        }
        action(result);
    }
}