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

namespace DotNetTranstor.Hookevent
{
	//解决密码只能是纯数字问题(特别是联机大厅密码设置)
	internal class Password_String : IMethodHook
	{
		[OriginalMethod]
		public static bool No_Password_Number(string dpu)
		{
			return true;
		}

		[CompilerGenerated]
		[HookMethod("WPFLauncher.ViewModel.LobbyGame.jo", "c", "No_Password_Number")]
		public static bool c(string dpu)
		{
			//Console.WriteLine($"[INFO]发现WPFLauncher正在调用密码是否为整数,检测字符串:{dpu}");
			return true;
		}
	}
}
