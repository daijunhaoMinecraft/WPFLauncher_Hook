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
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
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
using WPFLauncher.Manager.Game.Pipeline;
using WPFLauncher.Manager.PCChannel;
using WPFLauncher.Model;
using WPFLauncher.Network.Message;
using WPFLauncher.ViewModel.Share;
using MessageBox = System.Windows.MessageBox;

using MicrosoftTranslator.DotNetTranstor.Tools;

namespace DotNetTranstor.Hookevent
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
                string configPath = Path.Combine(Directory.GetCurrentDirectory(), "BedrockPath.txt");
                string targetDir = null;

                // 1. 尝试从 BedrockPath.txt 读取路径
                if (File.Exists(configPath))
                {
                    string savedPath = File.ReadAllText(configPath).Trim();
                    if (!string.IsNullOrEmpty(savedPath) && Directory.Exists(savedPath))
                    {
                        targetDir = savedPath;
                    }
                }

                // 2. 如果配置文件中没有，或者无效，可以尝试检查 tb.s (根据你提供的另一段代码推测的全局路径变量)
                // 注意：如果 tb.s 在你的命名空间中不可见，请注释掉下面这段，或者替换为你实际使用的默认路径获取逻辑
                /*
                if (string.IsNullOrEmpty(targetDir))
                {
                     // 假设 tb 是某个静态类，s 是静态属性
                     // if (Directory.Exists(tb.s)) 
                     // {
                     //     targetDir = tb.s;
                     // }
                }
                */

                // 3. 如果仍然没有路径，返回 false
                if (string.IsNullOrEmpty(targetDir))
                {
                    return false;
                }

                // 4. 检查该目录下是否存在 Minecraft.Windows.exe
                // 有些版本可能在子目录，这里先检查根目录。如果需要递归扫描子目录，逻辑会更复杂，
                // 但通常 BedrockPath.txt 指向的是包含 exe 的具体版本文件夹或主文件夹。
                // 根据 BedrockPathWindow_Select 的逻辑，它扫描子目录。
                // 这里为了简化，我们先检查 targetDir 本身，如果 targetDir 是父目录，可能需要扫描。
                
                // 策略 A: 假设 BedrockPath.txt 指向的是包含 Minecraft.Windows.exe 的具体文件夹
                string exePath = Path.Combine(targetDir, "Minecraft.Windows.exe");
                if (File.Exists(exePath))
                {
                    return true;
                }

                // 策略 B: 如果 BedrockPath.txt 指向的是父目录（包含多个版本文件夹），则扫描子目录
                // 这与 BedrockPathWindow_Select.ScanVersions 逻辑一致
                if (Directory.Exists(targetDir))
                {
                    foreach (var dir in Directory.GetDirectories(targetDir))
                    {
                        if (File.Exists(Path.Combine(dir, "Minecraft.Windows.exe")))
                        {
                            return true; // 只要找到一个版本就算存在
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