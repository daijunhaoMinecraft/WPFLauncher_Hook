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
using WPFLauncher.Manager.PCChannel;
using WPFLauncher.Network.Message;
using MessageBox = System.Windows.MessageBox;
using MicrosoftTranslator.DotNetTranstor.Tools;
using WPFLauncher.SQLite;

namespace DotNetTranstor.Hookevent
{
	//去除网易存档加密功能
	internal class No_SaveWorldEncrypt : IMethodHook
	{
		[CompilerGenerated]
		[HookMethod("MCStudio.Utils.cd", "b", null)]
		public static string b(string aam)
		{
			if (Path_Bool.IsDebug)
			{
				Console.WriteLine($"[INFO]发现网易正在加密存档已被制止,存档路径:{aam}");
			}
			return "";
		}
		
		[CompilerGenerated]
		[HookMethod("MCStudio.Utils.cd", "d", null)]
		private unsafe static string d(string aao)
		{
			if (Path_Bool.IsDebug)
			{
				Console.WriteLine($"[INFO]发现网易正在加密存档已被制止,加密文件路径:{aao}");
			}
			return "";
		}

	}
}
