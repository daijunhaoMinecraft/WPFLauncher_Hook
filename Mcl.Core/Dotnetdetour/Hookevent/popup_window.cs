using System;
using System.Runtime.CompilerServices;

namespace Mcl.Core.Dotnetdetour.Hookevent
{
	// Token: 0x02000017 RID: 23
	internal class popup_window : IMethodHook
	{
		[OriginalMethod]
		public static void f(bool nso)
		{
		}

		[CompilerGenerated]
		[HookMethod("WPFLauncher.Manager.NewsPop.atd", "e", "f")]
		private void e(bool nso)
		{
			if (Path_Bool.IsDebug)
			{
				Console.ForegroundColor = ConsoleColor.Green;
				Console.WriteLine("[INFO]检测到网易正在初始化活动页面广告已被制止并拦截");
				Console.ForegroundColor = ConsoleColor.White;
			}
			return;
		}
	}
}