using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
using WPFLauncher.Manager.Game.Pipeline;
using WPFLauncher.Manager.PCChannel;
using WPFLauncher.Model;
using WPFLauncher.Network.Message;
using WPFLauncher.ViewModel.Share;
using MessageBox = System.Windows.MessageBox;

using MicrosoftTranslator.DotNetTranstor.Tools;

namespace DotNetTranstor.Hookevent
{
	//随机Mac地址,可用于解决连锁Ban问题
	internal class RandomMac : IMethodHook
	{
		[OriginalMethod]
		public static string RandomMacAddr()
		{
			return "";
		}

		[CompilerGenerated]
		[HookMethod("WPFLauncher.Manager.Log.Util.asi", "b", "RandomMacAddr")]
		public static string b()
		{
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine($"[MacAddrInfo]当前Mac地址:{Path_Bool.Mac_Addr}\n[MacAddrInfo]成功替换伪造的mac地址:{Path_Bool.Random_Mac_Addr}");
			return Path_Bool.Random_Mac_Addr;
		}
		
	}
}
