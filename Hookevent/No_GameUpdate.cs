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
using InlineIL;
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
using static InlineIL.IL.Emit;

namespace DotNetTranstor.Hookevent
{
	//去除网易存档加密功能
	internal class No_GameUqdate : IMethodHook
	{
		// Token: 0x0600003E RID: 62 RVA: 0x000032F0 File Offset: 0x000014F0
		[OriginalMethod]
		private void No_Update(GameM ogr, BaseWindow ogs)
		{
		}

		// Token: 0x0600003F RID: 63 RVA: 0x000032F4 File Offset: 0x000014F4
		[CompilerGenerated]
		[HookMethod("WPFLauncher.Manager.Game.Pipeline.avp", "a", "No_Update")]
		// Token: 0x06000433 RID: 1075 RVA: 0x00042D28 File Offset: 0x00040F28
		private void a(GameM ogr, BaseWindow ogs)
		{
			MessageBoxResult messageBoxResult = us.q("是否绕过当前游戏的更新?", "", "确定", "不绕过", "");
			if (messageBoxResult == MessageBoxResult.OK)
			{
				Ldarg_0();
				Ldc_I4_0();
				Call(new(new(typeof(avi)),"hr"));
				Ret();
            
				throw IL.Unreachable();
			}
			else
			{
				No_Update(ogr,ogs);
			}
		}
	}
}
