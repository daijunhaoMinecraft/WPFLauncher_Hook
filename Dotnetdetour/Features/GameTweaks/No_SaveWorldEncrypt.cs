using System;
using System.Runtime.CompilerServices;

namespace Mcl.Core.Dotnetdetour.HookList
{
	//去除网易存档加密功能
	internal class No_SaveWorldEncrypt : IMethodHook
	{
		[CompilerGenerated]
		[HookMethod("MCStudio.Utils.cd", "b", null)]
		public static string EncryptFolder(string path)
		{
			WpfConfig.DefaultLogger.Info($"发现网易正在加密存档已被制止,存档路径:{path}");
			return "";
		}
		
		[CompilerGenerated]
		[HookMethod("MCStudio.Utils.cd", "d", null)]
		private unsafe static string EncryptSingleFile(string path)
		{
			WpfConfig.DefaultLogger.Info($"[INFO]发现网易正在加密存档已被制止,加密文件路径:{path}");
			return "";
		}

	}
}
