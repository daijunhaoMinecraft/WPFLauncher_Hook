using System;
using System.Runtime.CompilerServices;
using Mcl.Core.Dotnetdetour.Tools;
using WPFLauncher.Manager;

namespace Mcl.Core.Dotnetdetour.HookList
{
	//去除禁言
	internal class No_MuteBan : IMethodHook
	{
		[OriginalMethod]
		public new bool No_MuteBan_1()
		{
			return false;
		}

		[CompilerGenerated]
		[HookMethod("WPFLauncher.Model.UserM", "a", "No_MuteBan_1")]
		public new bool a()
		{
			bool isMuted = No_MuteBan_1();
			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.WriteLine($"[INFO] mChatBan: {isMuted}");
			if (isMuted)
			{
				long banChatExpiredAt = WPFLauncher.Common.azf<arg>.Instance.User.BanChatExpiredAt;
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("[IsMuteX] 您被系统暂时禁言, 时间至 " + X19Http.unix_timestamp_to(banChatExpiredAt));
				Console.WriteLine("[IsMuteX] You have been temporarily muted by the system until " + X19Http.unix_timestamp_to(banChatExpiredAt));
				Console.ForegroundColor = ConsoleColor.White; // Reset color
			}
			
			if (WpfConfig.IsDebug)
			{
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.WriteLine($"[INFO] 发现网易尝试检测该账号是否在禁言状态, 已被制止");
				Console.WriteLine($"[INFO] Detected that NetEase is trying to check if this account is muted, action has been blocked.");
			}
			Console.ForegroundColor = ConsoleColor.White; // Reset color
			return false;
		}
	}
}
