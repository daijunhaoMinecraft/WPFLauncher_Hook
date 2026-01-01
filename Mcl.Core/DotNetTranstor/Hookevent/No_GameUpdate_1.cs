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
	//去除网易存档加密功能
	internal class No_GameUqdate_1 : IMethodHook
	{
		[OriginalMethod]
		public bool No_Update(bool skipValidation = true)
		{
			return false;
		}

		[CompilerGenerated]
		[HookMethod("WPFLauncher.Model.Game.ale", "fn", "No_Update")]
		public bool CheckUpdate(bool skipValidation = true)
		{
			if (Path_Bool.IsStartWebSocket)
			{
				WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new { type = "IsBypassGameUpdate_Bedrock",BypassGameUpdate_Bedrock = Path_Bool.IsBypassGameUpdate_Bedrock, skipValidation = skipValidation}));
			}

			if (Path_Bool.IsDebug)
			{
				Console.WriteLine($"[INFO_Bedrock]IsBypassGameUpdate_Bedrock:{Path_Bool.IsBypassGameUpdate_Bedrock},skipValidation:{skipValidation}");
			}
			if (Path_Bool.IsBypassGameUpdate_Bedrock)
			{
				return true;
			}
			else
			{
				return No_Update(skipValidation);
			}
			return true;
		}
	}
}
