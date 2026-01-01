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
	internal class Password_String_1 : IMethodHook
	{
		[OriginalMethod]
		public static bool No_Password_Number(string dpu)
		{
			return true;
		}

		[CompilerGenerated]
		[HookMethod("WPFLauncher.Manager.AntiIndulgence.aro", "h", "No_Password_Number")]
		public static string h(string nfu, string nfv = "密码")
		{
			return "";
		}
	}
}
