using System;
using System.Runtime.CompilerServices;
using Mcl.Core.Dotnetdetour.Tools;

namespace Mcl.Core.Dotnetdetour.HookList
{
	//去除网易实名认证
	internal class NoRealname_2 : IMethodHook
	{
		[OriginalMethod]
		protected void No_RealName(int code)
		{
		}

		[CompilerGenerated]
		[HookMethod("WPFLauncher.Unisdk.nz", "onCompactViewClosed", "No_RealName")]
		protected void onCompactViewClosed(int code)
		{
			if (WpfConfig.IsDebug)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				DebugPrint.LogDebug_NoColorSelect("[MpayLogin]code_onCompactViewClosed: " + code.ToString());
				Console.ForegroundColor = ConsoleColor.White;
			}
			No_RealName(3);
		}
	}
}
