using System;
using System.Runtime.CompilerServices;

namespace Mcl.Core.Dotnetdetour.Hookevent
{
	//去除网易存档加密功能
	internal class No_SaveWorldEncrypt : IMethodHook
	{
		[CompilerGenerated]
		[HookMethod("MCStudio.Utils.cd", "b", null)]
		public static string b(string aam)
		{
			if (Path_Bool.IsDebug)
			{
				Console.WriteLine($"[INFO]发现网易正在加密存档已被制止,存档路径:{aam}");
			}
			return "";
		}
		
		[CompilerGenerated]
		[HookMethod("MCStudio.Utils.cd", "d", null)]
		private unsafe static string d(string aao)
		{
			if (Path_Bool.IsDebug)
			{
				Console.WriteLine($"[INFO]发现网易正在加密存档已被制止,加密文件路径:{aao}");
			}
			return "";
		}

	}
}
