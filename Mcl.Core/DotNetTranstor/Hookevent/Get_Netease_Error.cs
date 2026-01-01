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
using WPFLauncher.Manager.Game.Pipeline;
using WPFLauncher.Manager.PCChannel;
using WPFLauncher.Model;
using WPFLauncher.Network.Message;
using WPFLauncher.ViewModel.Share;
using MessageBox = System.Windows.MessageBox;

using MicrosoftTranslator.DotNetTranstor.Tools;

namespace DotNetTranstor.Hookevent
{
	//获取网易日志
	internal class Get_Netease_Error : IMethodHook
	{
		[OriginalMethod]
		public static void No_Update(string asz, string ata)
		{
		}

		[CompilerGenerated]
		[HookMethod("WPFLauncher.co", "l", "No_Update")]
		public static void l(string asz, string ata)
		{
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine(asz+":\n"+ata);
			Console.ForegroundColor = ConsoleColor.White;
		}
	}
}
