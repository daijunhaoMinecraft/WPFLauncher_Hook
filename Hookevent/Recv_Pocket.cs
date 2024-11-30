using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using WPFLauncher.Code;
using WPFLauncher.Network.Launcher;
using WPFLauncher.Util;
using System.Windows;
using Mcl.Core.Network;
using Mcl.Core.Network.Interface;
using Newtonsoft.Json;
using WPFLauncher;
using WPFLauncher.Common;
using WPFLauncher.Manager;
using WPFLauncher.Manager.Login;
using WPFLauncher.Manager.PCChannel;
using WPFLauncher.Network;
using WPFLauncher.Network.Message;
using WPFLauncher.Network.Protocol;
using WPFLauncher.Util.AES;
using aax = WPFLauncher.Network.aax;
using MessageBox = System.Windows.MessageBox;

namespace DotNetTranstor.Hookevent
{
    // Token: 0x02000017 RID: 23
    internal class Recv_Pocket : IMethodHook
    {
        // Token: 0x17001049 RID: 4169
        // (get) Token: 0x06004157 RID: 16727 RVA: 0x0001E1A6 File Offset: 0x0001C3A6
        // Token: 0x0600003E RID: 62 RVA: 0x000032F0 File Offset: 0x000014F0
        [OriginalMethod]
        public static void Pocket_Info(abp hqx)
        {
        }

        // Token: 0x0600003F RID: 63 RVA: 0x000032F4 File Offset: 0x000014F4
        [CompilerGenerated]
        [HookMethod("WPFLauncher.Network.abm", "b", "Pocket_Info")]
        // Token: 0x06003885 RID: 14469 RVA: 0x000DE7B0 File Offset: 0x000DC9B0
        public static void b(abp hqx)
        {
            Console.WriteLine($"[Recv_Pocket]接收到来自网易聊天服务器请求包:{hqx.a}");
            Pocket_Info(hqx);
        }
    }
}