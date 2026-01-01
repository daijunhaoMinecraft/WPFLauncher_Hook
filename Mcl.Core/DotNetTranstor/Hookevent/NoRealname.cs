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

using MicrosoftTranslator.DotNetTranstor.Tools;

namespace DotNetTranstor.Hookevent
{
	//去除网易实名认证
	internal class NoRealname : IMethodHook
	{
		[OriginalMethod]
		protected void No_RealName(string json)
		{
		}

		[CompilerGenerated]
		[HookMethod("WPFLauncher.Unisdk.nz", "onExtendFuncFinish", "No_RealName")]
		protected void onExtendFuncFinish(string json)
		{
			if (Path_Bool.IsDebug)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("[RealName]json: " + json.ToString());
				Console.ForegroundColor = ConsoleColor.White;
			}
			No_RealName("{\"methodId\":\"getRealnameStatus\",\"status\":3}");
		}
	}
}
