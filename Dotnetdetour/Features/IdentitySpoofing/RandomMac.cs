using System;
using System.Runtime.CompilerServices;

namespace Mcl.Core.Dotnetdetour.HookList
{
	//随机Mac地址,可用于解决连锁Ban问题
	internal class RandomMac : IMethodHook
	{
		[HookMethod("WPFLauncher.Manager.Log.Util.asi", "b", null)]
		public static string GetMacAddress()
		{
			WpfConfig.DefaultLogger.Info($"当前Mac地址:{WpfConfig.Mac_Addr}, 成功替换伪造的mac地址:{WpfConfig.Random_Mac_Addr}");
			return WpfConfig.Random_Mac_Addr;
		}
		
	}
}
