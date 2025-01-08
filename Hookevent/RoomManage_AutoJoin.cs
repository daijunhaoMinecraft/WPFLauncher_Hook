using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using WPFLauncher.Code;
using WPFLauncher.Network.Launcher;
using WPFLauncher.Util;
using System.Windows;
using InlineIL;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WPFLauncher;
using WPFLauncher.Common;
using WPFLauncher.Manager;
using WPFLauncher.Manager.Game.Pipeline;
using WPFLauncher.Manager.PCChannel;
using WPFLauncher.Model;
using WPFLauncher.Network;
using WPFLauncher.Network.Message;
using WPFLauncher.Network.Protocol.LobbyGame;
using WPFLauncher.View.Launcher.LobbyGame;
using WPFLauncher.ViewModel.Share;
using MessageBox = System.Windows.MessageBox;
using static InlineIL.IL.Emit;
using WPFLauncher.ViewModel.LobbyGame;
namespace DotNetTranstor.Hookevent
{
	//防止房主踢出房间(自动加入房间)
	internal class RoomManage_AutoJoin : IMethodHook
	{
		// Token: 0x0600003E RID: 62 RVA: 0x000032F0 File Offset: 0x000014F0
		[OriginalMethod]
		private new void Get_Room(abp jtn)
		{
		}

		// Token: 0x0600003F RID: 63 RVA: 0x000032F4 File Offset: 0x000014F4
		[CompilerGenerated]
		[HookMethod("WPFLauncher.Network.ChatService.ahe", "c", "Get_Room")]
		private new void c(abp jtn)
		{
			LobbyGameRoomManagerView lobbyGameRoomManagerView = ayx<apg>.Instance.k<LobbyGameRoomManagerView>();
			if (((lobbyGameRoomManagerView != null) ? lobbyGameRoomManagerView.DataContext : null) != null)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("[RoomInfo]你已被房主踢出房间,正在重新加入房间");
				while (true)
				{
					JObject Get_RoomEnter_Info = JObject.Parse(X19Http.RequestX19Api("/online-lobby-room-enter",
						JsonConvert.SerializeObject(new
						{
							room_id = Path_Bool.RoomInfo.entity.entity_id, password = Path_Bool.Password,
							check_visibilily = true
						})));
					if (Get_RoomEnter_Info["code"].ToObject<int>() == 0)
					{
						Console.ForegroundColor = ConsoleColor.Green;
						Console.WriteLine("[RoomInfo]成功加入房间!");
						break;
					}
					else if (Get_RoomEnter_Info["code"].ToObject<int>() == 12022)
					{
						Console.ForegroundColor = ConsoleColor.Red;
						Console.WriteLine($"[RoomERROR]加入房间失败:{Get_RoomEnter_Info["message"]},等待0.8秒后再次加入房间");
						Thread.Sleep(200);
					}
					else
					{
						Console.ForegroundColor = ConsoleColor.Red;
						Console.WriteLine($"[RoomERROR]加入房间失败:{Get_RoomEnter_Info["message"]}");
						Get_Room(jtn);
						break;
					}
				}
				Console.ForegroundColor = ConsoleColor.White;
			}
		}
	}
}