// using System;
// using System.Collections.Generic;
// using System.IO;
// using System.Net;
// using System.Net.Http;
// using System.Runtime.CompilerServices;
// using System.Security.Cryptography;
// using System.Text;
// using System.Windows.Forms;
// using Microsoft.VisualBasic;
// using Microsoft.Win32;
// using WPFLauncher.Code;
// using WPFLauncher.Network.Launcher;
// using WPFLauncher.Util;
// using System.Windows;
//
// using Newtonsoft.Json;
// using WPFLauncher;
// using WPFLauncher.Common;
// using WPFLauncher.Manager;
// using WPFLauncher.Manager.Game.Pipeline;
// using WPFLauncher.Manager.PCChannel;
// using WPFLauncher.Model;
// using WPFLauncher.Network.Message;
// using WPFLauncher.ViewModel.Share;
// using MessageBox = System.Windows.MessageBox;
//
// using MicrosoftTranslator.DotNetTranstor.Tools;
//
// namespace DotNetTranstor.Hookevent
// {
// 	//去除网易存档加密功能
// 	internal class No_GameUqdate_1 : IMethodHook
// 	{
// 		[OriginalMethod]
// 		public bool No_Update(bool skipValidation = true)
// 		{
// 			return false;
// 		}
//
// 		[CompilerGenerated]
// 		[HookMethod("WPFLauncher.Model.Game.ale", "fn", "No_Update")]
// 		public bool CheckUpdate(bool skipValidation = true)
// 		{
// 			if (Path_Bool.IsStartWebSocket)
// 			{
// 				WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new { type = "IsBypassGameUpdate_Bedrock",BypassGameUpdate_Bedrock = Path_Bool.IsBypassGameUpdate_Bedrock, skipValidation = skipValidation}));
// 			}
//
// 			if (Path_Bool.IsDebug)
// 			{
// 				Console.WriteLine($"[INFO_Bedrock]IsBypassGameUpdate_Bedrock:{Path_Bool.IsBypassGameUpdate_Bedrock},skipValidation:{skipValidation}");
// 			}
// 			if (Path_Bool.IsBypassGameUpdate_Bedrock)
// 			{
// 				return true;
// 			}
// 			else
// 			{
// 				return No_Update(skipValidation);
// 			}
// 			return true;
// 		}
// 	}
// }

using System;
using System.IO;
using System.Runtime.CompilerServices;
using Mcl.Core.Dotnetdetour.Tools;
using Newtonsoft.Json;

namespace Mcl.Core.Dotnetdetour.Hookevent
{
	//去除网易存档加密功能
	internal class No_GameUqdate_1 : IMethodHook
	{
		[OriginalMethod]
		public bool No_Update(bool skipValidation = true)
		{
			return false;
		}

        /// <summary>
        /// 检查本地是否存在有效的基岩版游戏文件
        /// </summary>
        private bool IsLocalBedrockGameExists()
        {
            try
            {
                if (Directory.Exists(Path_Bool.BedrockPath))
                {
                    foreach (var dir in Directory.GetDirectories(Path_Bool.BedrockPath))
                    {
                        if (File.Exists(Path.Combine(dir, "Minecraft.Windows.exe")))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR_Bedrock] 检查本地游戏文件时发生异常: {ex.Message}");
                return false;
            }
        }

		[CompilerGenerated]
		[HookMethod("WPFLauncher.Model.Game.ale", "fn", "No_Update")]
		public bool CheckUpdate(bool skipValidation = true)
		{
			if (Path_Bool.IsStartWebSocket)
			{
				WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new { type = "IsBypassGameUpdate_Bedrock",BypassGameUpdate_Bedrock = Path_Bool.IsBypassGameUpdate_Bedrock, skipValidation = skipValidation}));
			}

			if (Path_Bool.IsDebug)
			{
				Console.WriteLine($"[INFO_Bedrock]IsBypassGameUpdate_Bedrock:{Path_Bool.IsBypassGameUpdate_Bedrock},skipValidation:{skipValidation}");
			}

			if (Path_Bool.IsBypassGameUpdate_Bedrock)
			{
                // --- 新增逻辑开始 ---
                if (!IsLocalBedrockGameExists())
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("[WARNING_Bedrock] 已启用跳过更新，但未在本地硬盘检测到有效的基岩版游戏文件 (Minecraft.Windows.exe)。");
                    Console.WriteLine("[WARNING_Bedrock] 将回退到原始更新检查逻辑。");
                    Console.ForegroundColor = ConsoleColor.White;
                    
                    // 返回原始方法的结果 (通常为 false，意味着需要更新或验证失败)
                    return No_Update(skipValidation);
                }
                // --- 新增逻辑结束 ---

                // 如果检测通过，则正常绕过
				return true;
			}
			else
			{
				return No_Update(skipValidation);
			}
            
            //  unreachable code removed
		}
	}
}