using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using WPFLauncher.Code;
using WPFLauncher.Network.Launcher;
using WPFLauncher.Util;
using System.Windows;
using Newtonsoft.Json;
using WPFLauncher;
using WPFLauncher.Common;
using WPFLauncher.Manager;
using WPFLauncher.Manager.PCChannel;
using WPFLauncher.Model;
using WPFLauncher.Network.Message;
using MessageBox = System.Windows.MessageBox;
using MicrosoftTranslator.DotNetTranstor.Tools;

namespace DotNetTranstor.Hookevent
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
		// Token: 0x06004CA7 RID: 19623 RVA: 0x000FE53C File Offset: 0x000FC73C
		public new bool a()
		{
			// // 获取ayx<UserM>的实例
			// var instance = ayx<UserM>.Instance;
			//
			// // 获取ayx<UserM>的类型
			// Type type = instance.GetType();
			//
			// // 获取私有字段mChatBan
			// FieldInfo fieldInfo = type.GetField("mChatBan", BindingFlags.NonPublic | BindingFlags.Instance);
			//
			// // 检查字段是否存在
			// if (fieldInfo != null)
			// {
			// 	// 获取mChatBan字段的值
			// 	var value = fieldInfo.GetValue(instance);
			// 	// 将值转换为bool
			// 	if (bool.TryParse(value.ToString(), out bool isMuted))
			// 	{
			// 		
			// 	}
			// }
			// else
			// {
			// 	if (Path_Bool.IsDebug)
			// 	{
			// 		Console.WriteLine("[ERROR] mChatBan字段不存在");
			// 	}
			// }
			bool isMuted = No_MuteBan_1();
			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.WriteLine($"[INFO] mChatBan: {isMuted}");
			if (isMuted)
			{
				long banChatExpiredAt = aze<arg>.Instance.User.BanChatExpiredAt;
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("[IsMuteX] 您被系统暂时禁言, 时间至 " + X19Http.unix_timestamp_to(banChatExpiredAt));
				Console.WriteLine("[IsMuteX] You have been temporarily muted by the system until " + X19Http.unix_timestamp_to(banChatExpiredAt));
				Console.ForegroundColor = ConsoleColor.White; // Reset color
			}
			
			if (Path_Bool.IsDebug)
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
