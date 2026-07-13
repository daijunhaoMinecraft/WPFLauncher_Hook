using System;
using System.Runtime.CompilerServices;

namespace Mcl.Core.Dotnetdetour.HookList
{
	//获取网易日志
	internal class Get_Netease_Error : IMethodHook
	{
		[OriginalMethod]
		public static void No_Update(string asz, string ata)
		{
		}

		[CompilerGenerated]
		[HookMethod("WPFLauncher.co", "l", "No_Update")]
		public static void l(string asz, string ata)
		{
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine(asz+":\n"+ata);
			Console.ForegroundColor = ConsoleColor.White;
		}
	}
}
