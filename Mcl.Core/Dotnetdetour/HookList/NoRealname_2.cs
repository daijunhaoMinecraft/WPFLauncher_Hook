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
				WpfConfig.DefaultLogger.Info("[MpayLogin]code_onCompactViewClosed: " + code.ToString());
			}
			No_RealName(3);
		}
	}
}
