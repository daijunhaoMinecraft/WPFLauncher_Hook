using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
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
using WPFLauncher.Manager.Game.Pipeline;
using WPFLauncher.Manager.PCChannel;
using WPFLauncher.Model;
using WPFLauncher.Network.Message;
using WPFLauncher.ViewModel.Share;
using MessageBox = System.Windows.MessageBox;
using MicrosoftTranslator.DotNetTranstor.Tools;

namespace DotNetTranstor.Hookevent
{
	//去除网易实名认证
	internal class NoRealname_2 : IMethodHook
	{
		// Token: 0x0600003E RID: 62 RVA: 0x000032F0 File Offset: 0x000014F0
		[OriginalMethod]
		protected void No_RealName(int code)
		{
		}

		// Token: 0x0600003F RID: 63 RVA: 0x000032F4 File Offset: 0x000014F4
		[CompilerGenerated]
		[HookMethod("WPFLauncher.Unisdk.ny", "onCompactViewClosed", "No_RealName")]
		// Token: 0x06000433 RID: 1075 RVA: 0x00042D28 File Offset: 0x00040F28
		protected void onCompactViewClosed(int code)
		{
			if (Path_Bool.IsDebug)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				DebugPrint.LogDebug_NoColorSelect("[MpayLogin]code_onCompactViewClosed: " + code.ToString());
				Console.ForegroundColor = ConsoleColor.White;
			}
			No_RealName(3);
		}
	}
}
