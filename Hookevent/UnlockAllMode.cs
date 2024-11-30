using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using Mcl.Core.Network;
using Mcl.Core.Network.Interface;
using Mcl.Core.Utils;
using Newtonsoft.Json;
using WPFLauncher.Manager.GrayUpdate;
using WPFLauncher.Model;
using WPFLauncher.Model.Game;
using WPFLauncher.Model.Game.GameClient;
using WPFLauncher.Util;
using WPFLauncher.Update;

namespace DotNetTranstor.Hookevent
{
    internal class UnlockAllMode : IMethodHook
    {
        //解锁网易被暗藏的功能(也就是只能通过概率的方式获得)例如:x64_mc(64位mc)等
        [OriginalMethod]
        public void UnlockAll(GrayUpdateType nzp)
        {
        }

        [CompilerGenerated]
        [HookMethod("WPFLauncher.Manager.GrayUpdate.atw", "m", "UnlockAll")]
        public bool ak(GrayUpdateType nzp)
        {
            Console.WriteLine("[GrayUpdate]成功调用需要概率的功能");
            return true;
        }
    }
}