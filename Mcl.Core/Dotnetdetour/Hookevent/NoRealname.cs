using System;
using System.Runtime.CompilerServices;

namespace Mcl.Core.Dotnetdetour.Hookevent
{
	//去除网易实名认证
	internal class NoRealname : IMethodHook
	{
		[OriginalMethod]
		protected void No_RealName(string json)
		{
		}

		[CompilerGenerated]
		[HookMethod("WPFLauncher.Unisdk.nz", "onExtendFuncFinish", "No_RealName")]
		protected void onExtendFuncFinish(string json)
		{
			if (Path_Bool.IsDebug)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("[RealName]json: " + json.ToString());
				Console.ForegroundColor = ConsoleColor.White;
			}
			No_RealName("{\"methodId\":\"getRealnameStatus\",\"status\":3}");
		}
	}
}
