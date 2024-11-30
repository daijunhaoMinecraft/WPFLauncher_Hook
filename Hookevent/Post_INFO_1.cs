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
using WPFLauncher.Network.Message;
using WPFLauncher.Network.Protocol;
using WPFLauncher.Util.AES;
using MessageBox = System.Windows.MessageBox;

namespace DotNetTranstor.Hookevent
{
    // Token: 0x02000017 RID: 23
    internal class Post_INFO_1 : IMethodHook
    {
        // Token: 0x17001049 RID: 4169
        // (get) Token: 0x06004157 RID: 16727 RVA: 0x0001E1A6 File Offset: 0x0001C3A6
        // Token: 0x0600003E RID: 62 RVA: 0x000032F0 File Offset: 0x000014F0
        [OriginalMethod]
        public static void Dynamic_Token_Info(string foy, string foz)
        {
        }

        // Token: 0x0600003F RID: 63 RVA: 0x000032F4 File Offset: 0x000014F4
        [CompilerGenerated]
        [HookMethod("WPFLauncher.Util.sl", "e", "Dynamic_Token_Info")]
        // Token: 0x06002F2C RID: 12076 RVA: 0x000AE038 File Offset: 0x000AC238
        public static void e(string foy, string foz)
        {
            Console.WriteLine($"[Dynamic_Token]Url路径:{foy},Post请求内容:{foz}");
            Dynamic_Token_Info(foy,foz);
        }
    }
}