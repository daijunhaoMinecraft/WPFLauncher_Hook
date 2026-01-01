using System;
using System.Collections.Generic;
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
using WPFLauncher.Manager.Login;
using WPFLauncher.Manager.PCChannel;
using WPFLauncher.Network.Message;
using MessageBox = System.Windows.MessageBox;
using MicrosoftTranslator.DotNetTranstor.Tools;

namespace DotNetTranstor.Hookevent
{
	// Token: 0x02000017 RID: 23
	internal class popup_window : IMethodHook
	{
		[OriginalMethod]
		public static void f(bool nso)
		{
		}

		[CompilerGenerated]
		[HookMethod("WPFLauncher.Manager.NewsPop.atd", "e", "f")]
		private void e(bool nso)
		{
			if (Path_Bool.IsDebug)
			{
				Console.ForegroundColor = ConsoleColor.Green;
				Console.WriteLine("[INFO]检测到网易正在初始化活动页面广告已被制止并拦截");
				Console.ForegroundColor = ConsoleColor.White;
			}
			return;
		}
	}
}