using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.WebSockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json.Linq;
using WPFLauncher.Common;
using WPFLauncher.Manager.Configuration;
using WPFLauncher.Util;

namespace DotNetTranstor.Hookevent
{
    internal class X19Fucker : IMethodHook
    {
        // 导入 AllocConsole 函数
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AllocConsole();
        
        //X19_发烧平台绕过Hook
        [OriginalMethod]
        public static void X19_Fever_bypass()
        {
        }

        [CompilerGenerated]
        [HookMethod("WPFLauncher.Manager.apf", "aj", "X19_Fever_bypass")]
        public bool Fever_False()
        {
            // 分配一个新的控制台
            AllocConsole();
            // 重定向输出流到控制台
            var writer = new StreamWriter(Console.OpenStandardOutput());
            writer.AutoFlush = true; // 设置为自动刷新，确保每次写入都立即输出
            Console.SetOut(writer);
            // 设置控制台输出编码为 UTF-8
            Console.OutputEncoding = Encoding.UTF8;
            Console.CursorVisible = false;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\u2588\u2588\u2557    \u2588\u2588\u2557\u2588\u2588\u2588\u2588\u2588\u2588\u2557 \u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2557\u2588\u2588\u2557  \u2588\u2588\u2557 \u2588\u2588\u2588\u2588\u2588\u2588\u2557  \u2588\u2588\u2588\u2588\u2588\u2588\u2557 \u2588\u2588\u2557  \u2588\u2588\u2557\n\u2588\u2588\u2551    \u2588\u2588\u2551\u2588\u2588\u2554\u2550\u2550\u2588\u2588\u2557\u2588\u2588\u2554\u2550\u2550\u2550\u2550\u255d\u2588\u2588\u2551  \u2588\u2588\u2551\u2588\u2588\u2554\u2550\u2550\u2550\u2588\u2588\u2557\u2588\u2588\u2554\u2550\u2550\u2550\u2588\u2588\u2557\u2588\u2588\u2551 \u2588\u2588\u2554\u255d\n\u2588\u2588\u2551 \u2588\u2557 \u2588\u2588\u2551\u2588\u2588\u2588\u2588\u2588\u2588\u2554\u255d\u2588\u2588\u2588\u2588\u2588\u2557  \u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2551\u2588\u2588\u2551   \u2588\u2588\u2551\u2588\u2588\u2551   \u2588\u2588\u2551\u2588\u2588\u2588\u2588\u2588\u2554\u255d \n\u2588\u2588\u2551\u2588\u2588\u2588\u2557\u2588\u2588\u2551\u2588\u2588\u2554\u2550\u2550\u2550\u255d \u2588\u2588\u2554\u2550\u2550\u255d  \u2588\u2588\u2554\u2550\u2550\u2588\u2588\u2551\u2588\u2588\u2551   \u2588\u2588\u2551\u2588\u2588\u2551   \u2588\u2588\u2551\u2588\u2588\u2554\u2550\u2588\u2588\u2557 \n\u255a\u2588\u2588\u2588\u2554\u2588\u2588\u2588\u2554\u255d\u2588\u2588\u2551     \u2588\u2588\u2551     \u2588\u2588\u2551  \u2588\u2588\u2551\u255a\u2588\u2588\u2588\u2588\u2588\u2588\u2554\u255d\u255a\u2588\u2588\u2588\u2588\u2588\u2588\u2554\u255d\u2588\u2588\u2551  \u2588\u2588\u2557\n \u255a\u2550\u2550\u255d\u255a\u2550\u2550\u255d \u255a\u2550\u255d     \u255a\u2550\u255d     \u255a\u2550\u255d  \u255a\u2550\u255d \u255a\u2550\u2550\u2550\u2550\u2550\u255d  \u255a\u2550\u2550\u2550\u2550\u2550\u255d \u255a\u2550\u255d  \u255a\u2550\u255d\n                                                            ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[WPFLauncherHook]成功Hook网易我的世界启动器,感谢使用\ngithub链接:https://github.com/daijunhaoMinecraft/WPFLauncher_Hook\nBy:daijunhao");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("[INFO]控制台输出成功启动!");
            //Console.WriteLine("[INFO]成功绕过发烧平台!");
            MessageBoxResult messageBoxResult = us.q("是否绕过基岩版更新?", "", "确定", "不绕过", "");
            if (messageBoxResult == MessageBoxResult.OK)
            {
                Path_Bool.IsBypassGameUpdate_Bedrock = true;
            }
            MessageBoxResult messageBoxResult_IsX64MC = us.q("请选择基岩版版本类型?", "", "X64_mc", "windowsmc", "");
            if (messageBoxResult_IsX64MC == MessageBoxResult.OK)
            {
                Path_Bool.IsEnableX64mc = true;
            }
            MessageBoxResult messageBoxResult_Ws = us.q("是否开启Web服务器?\n这其中包括WebSocket服务器和Http服务器\n服务器连接地址:ws://localhost:4600/\nHTTP服务器地址:http://127.0.0.1:4601", "", "开启", "关闭", "");
            if (messageBoxResult_Ws == MessageBoxResult.OK)
            {
                Path_Bool.IsStartWebSocket = true;
                WebSocketHelper.StartWebSocketServer();
                var server = new SimpleHttpServer();
                Task server1Task = Task.Run(() => server.Start("http://127.0.0.1:4601/"));
                Console.WriteLine("[INFO]Web服务器已启动!");
            }
            MessageBoxResult messageBoxResult_IsDebug = us.q("是否开启详细日志输出?", "", "开启", "关闭", "");
            if (messageBoxResult_IsDebug == MessageBoxResult.OK)
            {
                Path_Bool.IsDebug = true;
            }
            string Get_Mac_Addr = Get_MacAddr();
            string Get_Random_Mac_Addr = GenerateRandomMacAddress();
            // 格式化MAC地址为带冒号的格式
            // 格式化MAC地址为带冒号的格式
            string Get_Mac_Addr_Original = Get_Mac_Addr;
            string Get_Random_Mac_Addr_Original = ConvertToOriginalFormat(Get_Random_Mac_Addr);
            if (Get_Mac_Addr.Length == 12)
            {
                // 按照每2个字符插入一个冒号
                Get_Mac_Addr = string.Join(":", Enumerable.Range(0, 6).Select(i => Get_Mac_Addr.Substring(i * 2, 2)));
            }

            MessageBoxResult messageBoxResult_Inject = us.q("是否开启模组注入功能(仅限Java版)?", "", "开启", "关闭", "");
            if (messageBoxResult_Inject == MessageBoxResult.OK)
            {
                Path_Bool.EnableModsInject = true;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("[INFO]成功启用模组注入功能!");

                // 创建ModsInject文件夹
                string modsInjectPath = Path.Combine(Directory.GetCurrentDirectory(), "ModsInject");
                Directory.CreateDirectory(modsInjectPath); // 忽略创建失败

                // 打开ModsInject文件夹
                System.Diagnostics.Process.Start("explorer.exe", modsInjectPath);

                Console.WriteLine("[INFO]已打开模组注入文件夹,请放入适合的模组!");
                Console.ForegroundColor = ConsoleColor.White;
            }

            MessageBoxResult messageBoxResult_RoomBlacklist = us.q("是否开启房间黑名单功能?\n注:若需要添加玩家黑名单则需要开启Web功能(Http请求)\n因为添加/删除玩家黑名单需要Http请求", "", "开启", "关闭", "");
            if (messageBoxResult_RoomBlacklist == MessageBoxResult.OK)
            {
                Path_Bool.EnableRoomBlacklist = true;
                Console.ForegroundColor = ConsoleColor.Green;
                string blacklistFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "RoomConfig");
                string blacklistFilePath = Path.Combine(blacklistFolderPath, "BlackList.json");

                // 检查文件夹是否存在，如果不存在则创建
                if (!Directory.Exists(blacklistFolderPath))
                {
                    Directory.CreateDirectory(blacklistFolderPath);
                    Console.WriteLine("房间黑名单文件夹已创建: " + blacklistFolderPath);
                }

                // 检查BlackList.json文件是否存在，如果不存在则创建
                if (!File.Exists(blacklistFilePath))
                {
                    File.WriteAllText(blacklistFilePath, "");
                    Console.WriteLine("房间黑名单文件已创建: " + blacklistFilePath + "，内容为空列表。");
                }
                // 读取BlackList.json文件内容
                try
                {
                    string jsonContent = File.ReadAllText(blacklistFilePath);
                    // 解析可能出现的格式
                    if (jsonContent.StartsWith("[") && jsonContent.EndsWith("]"))
                    {
                        Path_Bool.RoomBlacklist = JArray.Parse(jsonContent).Select(x => x.ToString().Trim('"')).ToList();
                    }
                    else
                    {
                        Path_Bool.RoomBlacklist = jsonContent.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim('"')).ToList();
                    }
                    Console.WriteLine("房间黑名单内容: " + string.Join(", ", Path_Bool.RoomBlacklist));
                }
                catch (Exception ex)
                {
                    Console.WriteLine("读取房间黑名单时发生错误: " + ex.Message);
                    Path_Bool.RoomBlacklist = new List<string>(); // 如果读取失败，返回空的List<string>
                }

                Path_Bool.EnableRegexBlacklist = true; // 启用正则表达式黑名单功能
                string regexBlacklistFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "RoomConfig"); // 配置文件夹保持不变
                string regexBlacklistFilePath = Path.Combine(regexBlacklistFolderPath, "RegexBlackList.json");

                // 检查文件夹是否存在，如果不存在则创建
                if (!Directory.Exists(regexBlacklistFolderPath))
                {
                    Directory.CreateDirectory(regexBlacklistFolderPath);
                    Console.WriteLine("正则表达式黑名单文件夹已创建: " + regexBlacklistFolderPath);
                }

                // 检查RegexBlackList.json文件是否存在，如果不存在则创建
                if (!File.Exists(regexBlacklistFilePath))
                {
                    File.WriteAllText(regexBlacklistFilePath, "[]");
                    Console.WriteLine("正则表达式黑名单文件已创建: " + regexBlacklistFilePath + "，内容为空列表。");
                }

                // 读取RegexBlackList.json文件内容
                try
                {
                    string jsonContent = File.ReadAllText(regexBlacklistFilePath);
                    Path_Bool.RegexBlacklist = JArray.Parse(jsonContent).Select(x => x.ToString()).ToList();
                    Console.WriteLine("正则表达式黑名单内容: " + string.Join(", ", Path_Bool.RegexBlacklist));
                }
                catch (Exception ex)
                {
                    Console.WriteLine("读取正则表达式黑名单时发生错误: " + ex.Message);
                    Path_Bool.RegexBlacklist = new List<string>(); // 如果读取失败，返回空的List<string>
                }

                Console.ForegroundColor = ConsoleColor.White;
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[RandomMac]当前Mac地址:{Get_Mac_Addr},原始输出为:{Get_Mac_Addr_Original}\n[RandomMac]成功替换原来的Mac地址为随机Mac地址:{Get_Random_Mac_Addr},替换输出为:{Get_Random_Mac_Addr_Original}");
            Console.ForegroundColor = ConsoleColor.White;
            Path_Bool.Mac_Addr = Get_Mac_Addr;
            Path_Bool.Random_Mac_Addr = Get_Random_Mac_Addr_Original;
            ayx<axa>.Instance.App.EnableCppGameDebug = true;
            ayx<axa>.Instance.App.NetGameFilter = true;
            ayx<axa>.Instance.App.EnableNetGameFilterSetting = true;
            ayx<axa>.Instance.App.CppGameDebugPath = su.s + "\\CppGameDebug\\";
            return false;
        }
        
        public static string GenerateRandomMacAddress()
        {
            Random random = new Random();
            byte[] macAddr = new byte[6];
            random.NextBytes(macAddr);

            // 保证MAC地址的第一个字节的最低位为0（即设备地址类型为“单播”）
            macAddr[0] = (byte)(macAddr[0] & (byte)254);

            // 将字节数组格式化为MAC地址
            return string.Join(":", macAddr.Select(b => b.ToString("X2")));
        }
        public static string ConvertToOriginalFormat(string macAddress)
        {
            // 去掉所有冒号
            return macAddress.Replace(":", string.Empty);
        }
        // Token: 0x06006251 RID: 25169 RVA: 0x0014118C File Offset: 0x0013F38C
        public static string Get_MacAddr()
        {
            string text = string.Empty;
            try
            {
                NetworkInterface[] allNetworkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
                foreach (NetworkInterface networkInterface in allNetworkInterfaces)
                {
                    if (text == string.Empty)
                    {
                        text = networkInterface.GetPhysicalAddress().ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                sx.Default.l(ex, "GetMACAddress");
            }
            return text;
        }
    }
}