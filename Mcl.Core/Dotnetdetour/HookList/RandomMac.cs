using System;
using System.Runtime.CompilerServices;

namespace Mcl.Core.Dotnetdetour.HookList
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
			Console.WriteLine($"[MacAddrInfo]当前Mac地址:{WpfConfig.Mac_Addr}\n[MacAddrInfo]成功替换伪造的mac地址:{WpfConfig.Random_Mac_Addr}");
			return WpfConfig.Random_Mac_Addr;
		}
		
	}
}
