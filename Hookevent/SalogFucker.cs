using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using WPFLauncher.Code;
using WPFLauncher.Network.Launcher;
using WPFLauncher.Util;
using System.Windows;
using Newtonsoft.Json;
using WPFLauncher;
using WPFLauncher.Common;
using WPFLauncher.Manager;
using WPFLauncher.Manager.PCChannel;
using WPFLauncher.Network.Message;
using MessageBox = System.Windows.MessageBox;

namespace DotNetTranstor.Hookevent
{
	// Token: 0x02000017 RID: 23
	internal class SalogFucker : IMethodHook
	{
		// Token: 0x0600003E RID: 62 RVA: 0x000032F0 File Offset: 0x000014F0
		[OriginalMethod]
		public static void f(string aqq, string aqr)
		{
		}

		// Token: 0x0600003F RID: 63 RVA: 0x000032F4 File Offset: 0x000014F4
		[CompilerGenerated]
		[HookMethod("WPFLauncher.cn", "k", "f")]
		// Token: 0x06000433 RID: 1075 RVA: 0x00042D28 File Offset: 0x00040F28
		public static void k(string aqq, string aqr)
		{
			if (Path_Bool.IsStartWebSocket)
			{
				WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new { type = "salog-new", SalogType = aqq,data = aqr}));
			}
			
			Console.WriteLine("[INFO]发现/salog-new的post请求(进程检测/dll检测/注入检测等)已及时制止");
			Console.WriteLine("[INFO]检测类型:" + aqq + "发送数据:" + aqr);
		}
	}
}
