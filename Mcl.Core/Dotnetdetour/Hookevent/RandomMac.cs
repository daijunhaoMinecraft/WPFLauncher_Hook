using System;
using System.Runtime.CompilerServices;

namespace Mcl.Core.Dotnetdetour.Hookevent
{
	//随机Mac地址,可用于解决连锁Ban问题
	internal class RandomMac : IMethodHook
	{
		[OriginalMethod]
		public static string RandomMacAddr()
		{
			return "";
		}

		[CompilerGenerated]
		[HookMethod("WPFLauncher.Manager.Log.Util.asi", "b", "RandomMacAddr")]
		public static string b()
		{
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine($"[MacAddrInfo]当前Mac地址:{Path_Bool.Mac_Addr}\n[MacAddrInfo]成功替换伪造的mac地址:{Path_Bool.Random_Mac_Addr}");
			return Path_Bool.Random_Mac_Addr;
		}
		
	}
}
