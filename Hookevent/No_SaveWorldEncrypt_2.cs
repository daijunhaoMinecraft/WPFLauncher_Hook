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
	//去除网易存档加密功能
	internal class No_SaveWorldEncrypt_2 : IMethodHook
	{
		// Token: 0x0600003E RID: 62 RVA: 0x000032F0 File Offset: 0x000014F0
		[OriginalMethod]
		private unsafe static string No_WorldEncrypt(string aao)
		{
			return "";
		}

		// Token: 0x0600003F RID: 63 RVA: 0x000032F4 File Offset: 0x000014F4
		[CompilerGenerated]
		[HookMethod("MCStudio.Utils.cb", "d", "No_WorldEncrypt")]
		// Token: 0x06000433 RID: 1075 RVA: 0x00042D28 File Offset: 0x00040F28
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
