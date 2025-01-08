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
	internal class No_GameUqdate_1 : IMethodHook
	{
		// Token: 0x0600003E RID: 62 RVA: 0x000032F0 File Offset: 0x000014F0
		[OriginalMethod]
		public bool No_Update(bool pwk = true)
		{
			return false;
		}

		// Token: 0x0600003F RID: 63 RVA: 0x000032F4 File Offset: 0x000014F4
		[CompilerGenerated]
		[HookMethod("WPFLauncher.Model.Game.akx", "fm", "No_Update")]
		// Token: 0x06000433 RID: 1075 RVA: 0x00042D28 File Offset: 0x00040F28
		public bool fm(bool pwk = true)
		{
			if (Path_Bool.IsStartWebSocket)
			{
				WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new { type = "IsBypassGameUpdate_Bedrock",BypassGameUpdate_Bedrock = Path_Bool.IsBypassGameUpdate_Bedrock, pwk = pwk}));
			}

			if (Path_Bool.IsDebug)
			{
				Console.WriteLine($"[INFO_Bedrock]IsBypassGameUpdate_Bedrock:{Path_Bool.IsBypassGameUpdate_Bedrock},pwk:{pwk}");
			}
			if (Path_Bool.IsBypassGameUpdate_Bedrock)
			{
				return true;
			}
			else
			{
				return No_Update(pwk);
			}
			return true;
		}
	}
}
