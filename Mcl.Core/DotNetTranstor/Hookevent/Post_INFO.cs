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
using MicrosoftTranslator.DotNetTranstor.Tools;

namespace DotNetTranstor.Hookevent
{
    // Token: 0x02000017 RID: 23
    internal class Post_INFO : IMethodHook
    {
        // Token: 0x17001049 RID: 4169
        // (get) Token: 0x06004157 RID: 16727 RVA: 0x0001E1A6 File Offset: 0x0001C3A6
        // Token: 0x0600003E RID: 62 RVA: 0x000032F0 File Offset: 0x000014F0
        [OriginalMethod]
        public static void Get_X19_Post_Info(string resource, string parameter, aeq needEncrypt = aeq.a, string prefix = null, bool isHome = false)
        {
        }

        // Token: 0x0600003F RID: 63 RVA: 0x000032F4 File Offset: 0x000014F4
        [CompilerGenerated]
        [HookMethod("WPFLauncher.Network.Protocol.aet", "Post", "Get_X19_Post_Info")]
        // Token: 0x06004235 RID: 16949 RVA: 0x000E8F7C File Offset: 0x000E717C
        public static INetResponse Post(string resource, string parameter, aeq needEncrypt = aeq.a, string prefix = null, bool isHome = false)
        {
            aeu aen = new aeu
            {
                Body = parameter,
                Method = Method.POST,
                Option = needEncrypt,
                Resource = resource
            };
            if (isHome)
            {
                aen.Header["server"] = "home";
            }
            INetResponse Get_Content = aet.x(prefix, aen);
            string needEncrypt_String = "";
            if (needEncrypt == aeq.a)
            {
                needEncrypt_String = "Normal";
            }
            else if (needEncrypt == aeq.b)
            {
                needEncrypt_String = "CommonEncrypt";
            }
            else if (needEncrypt == aeq.c)
            {
                needEncrypt_String = "Authentication";
            }
            if (Path_Bool.IsStartWebSocket)
            {
                WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new { type = "Post", url = resource, parameter = parameter, needEncrypt = needEncrypt_String, prefix = prefix, isHome = isHome, result = Regex.Unescape(Get_Content.Content) }));
            }
            if (Path_Bool.IsDebug)
            {
                DebugPrint.LogDebug_NoColorSelect($"[Post_Core]请求url地址:{resource},内容:{parameter},返回结果:{Regex.Unescape(Get_Content.Content)},发送参数:{needEncrypt_String}");
            }
            return Get_Content;
        }
    }
}