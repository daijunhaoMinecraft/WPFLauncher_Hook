using System;
using System.Runtime.CompilerServices;

namespace Mcl.Core.Dotnetdetour.HookList
{
	//去除网易实名认证
	internal class Mpay_Log : IMethodHook
	{
		[OriginalMethod]
		protected void Mpay_Log_Show(string log)
		{
		}

		[CompilerGenerated]
		[HookMethod("WPFLauncher.Unisdk.nz", "onLog", "Mpay_Log_Show")]
		protected void onLog(string log)
		{
			if (WpfConfig.IsDebug)
			{
				WpfConfig.DefaultLogger.Info(log);
			}
		}
	}
}
