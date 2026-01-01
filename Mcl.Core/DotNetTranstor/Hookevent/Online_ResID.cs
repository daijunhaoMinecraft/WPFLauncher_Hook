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
using WPFLauncher.Network.Protocol.LobbyGame;
using WPFLauncher.ViewModel.Share;
using MessageBox = System.Windows.MessageBox;

using MicrosoftTranslator.DotNetTranstor.Tools;

namespace DotNetTranstor.Hookevent
{
	internal class Online_ResID : IMethodHook
	{
		[OriginalMethod]
		public static void Get_Room(string jnh, int jni, int jnj, Action<EntityListResponse<LobbyGameRoomEntity>> jnk)
		{
		}

		[CompilerGenerated]
		[HookMethod("WPFLauncher.Network.Protocol.LobbyGame.age", "d", "Get_Room")]
		// Token: 0x060045FE RID: 17918 RVA: 0x000ED080 File Offset: 0x000EB280
		public static void d(string jnh, int jni, int jnj, Action<EntityListResponse<LobbyGameRoomEntity>> jnk)
		{
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine($"[Online]成功将房间最大显示个数修改成{Path_Bool.MaxRoomCount.ToString()}!");
			Console.WriteLine($"[Online]获取房间列表ResID:{jnh}");
			Console.ForegroundColor = ConsoleColor.White;
			Get_Room(jnh, jni, Path_Bool.MaxRoomCount, jnk);
		}
	}
}
