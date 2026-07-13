using System;
using System.Runtime.CompilerServices;
using WPFLauncher.Code;
using WPFLauncher.Network.Protocol.LobbyGame;

namespace Mcl.Core.Dotnetdetour.HookList
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
			Console.WriteLine($"[Online]成功将房间最大显示个数修改成{WpfConfig.MaxRoomCount.ToString()}!");
			Console.WriteLine($"[Online]获取房间列表ResID:{jnh}");
			Console.ForegroundColor = ConsoleColor.White;
			Get_Room(jnh, jni, WpfConfig.MaxRoomCount, jnk);
		}
	}
}
