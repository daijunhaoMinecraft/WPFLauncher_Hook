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
using WPFLauncher.Model;
using WPFLauncher.Model.Game;
using WPFLauncher.Model.Game.GameClient;
using WPFLauncher.Util;
using WPFLauncher.Update;

namespace DotNetTranstor.Hookevent
{
    internal class BedrockFucker : IMethodHook
    {
        [OriginalMethod]
        public void X64_mc_ByPass()
        {
        }

        [CompilerGenerated]
        [HookMethod("WPFLauncher.Manager.apf", "ak", "X64_mc_ByPass")]
        public bool ak()
        {
            X64_mc_ByPass();
            return Environment.Is64BitOperatingSystem;
        }
    }
}