using System;
using DotNetTranstor;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WPFLauncher.Code;
using WPFLauncher.Network.Launcher;

namespace Mcl.Core.DotNetTranstor.Hookevent;

public class SolveHyt : IMethodHook
{
    // [OriginalMethod]
    // internal void b_Original(EntityResponse<ItemAddressEntity> kcn)
    // {
    //     
    // }
    //
    // [HookMethod("WPFLauncher.Model.aih.a.b", "b", "b_Original")]
    // internal void b(EntityResponse<ItemAddressEntity> kcn)
    // {
    //     if (kcn.entity.game_status != 0)
    //     {
    //         kcn.entity.game_status = 0;
    //     }
    //
    //     if (kcn.entity.port == 0)
    //     {
    //         kcn.entity.port = 25565;
    //     }
    //     Console.WriteLine("Solve Address");
    //     b_Original(kcn);
    // }
    
    [OriginalMethod]
    public static void l_Original(string hwu, Action<EntityResponse<ItemAddressEntity>> hwv)
    {
        
    }
    
    [HookMethod("WPFLauncher.Network.Launcher.aco", "l", "l_Original")]
    public static void l(string hwu, Action<EntityResponse<ItemAddressEntity>> action)
    {
        // if (kcn.entity.game_status != 0)
        // {
        //     kcn.entity.game_status = 0;
        // }
        //
        // if (kcn.entity.port == 0)
        // {
        //     kcn.entity.port = 25565;
        // }
        Console.WriteLine("Solve Address");
        // b_Original(kcn);
        // Post
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