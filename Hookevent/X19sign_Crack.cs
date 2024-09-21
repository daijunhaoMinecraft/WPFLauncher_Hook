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

namespace DotNetTranstor.Hookevent
{
	// Token: 0x02000017 RID: 23
	internal class X19sign_Crack : IMethodHook
	{
		// Token: 0x0600003E RID: 62 RVA: 0x000032F0 File Offset: 0x000014F0
		[OriginalMethod]
		public static void f(string aqo, string aqp)
		{
		}

		// Token: 0x0600003F RID: 63 RVA: 0x000032F4 File Offset: 0x000014F4
		[CompilerGenerated]
		[HookMethod("WPFLauncher.Util.tp", "a", "f")]
		// Token: 0x06000433 RID: 1075 RVA: 0x00042D28 File Offset: 0x00040F28
		public static string a(string ggq)
		{
			File.AppendAllText(Path.Combine(Directory.GetCurrentDirectory(),"X19sign_Decrypt.txt"),$"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}]X19sign_Decrypt:{ggq}\n");
			return ggq;
		}
	}
}
