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
		[OriginalMethod]
		protected void No_RealName(int code)
		{
		}

		[CompilerGenerated]
		[HookMethod("WPFLauncher.Unisdk.nz", "onCompactViewClosed", "No_RealName")]
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
