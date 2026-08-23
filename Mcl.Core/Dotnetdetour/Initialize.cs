using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks; // 引入 Task
using Mcl.Core.Dotnetdetour.CoreEngine.Base;
using Mcl.Core.Dotnetdetour.Features.GeneralHooks;
using Mcl.Core.Dotnetdetour.Models.Config;
using Mcl.Core.Dotnetdetour.UI.Injector;
using Mcl.Core.NeteaseProtocol;
using Mcl.Core.Tools;
using Mcl.Core.Updater;
using Microsoft.Win32;
using Net.Nekocurit.Cipher;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class ModuleInitializerAttribute : Attribute
    {
    }

    public class HookBootstrapper
    {
        public delegate bool ConsoleCtrlDelegate(int ctrlType);

        public const int CTRL_C_EVENT = 0;
        public const int CTRL_BREAK_EVENT = 1;
        public const int CTRL_CLOSE_EVENT = 2;
        public const int CTRL_LOGOFF_EVENT = 5;
        public const int CTRL_SHUTDOWN_EVENT = 6;

        private static ConsoleCtrlDelegate _consoleHandler;
        
        // 1. 添加一个防重入标记，确保退出逻辑绝对只执行一次
        private static int _isExiting = 0;

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll")]
        private static extern bool SetConsoleOutputCP(uint wCodePageID);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate handler, bool add);

        private static void ExitProcess()
        {
            // 2. 利用原子操作检查并设置标记，如果已经是 1 说明正在退出，直接 return 防止重复执行
            if (Interlocked.Exchange(ref _isExiting, 1) == 1)
            {
                return;
            }

            try
            {
                WpfConfig.DefaultLogger.Info("程序即将退出, 正在执行清理操作...");
                WpfConfig.DefaultLogger.Info("检查是否进入联机大厅房间");

                if (WpfConfig.RoomInfo != null && !string.IsNullOrWhiteSpace(WpfConfig.RoomInfo.entity?.entity_id))
                {
                    WpfConfig.DefaultLogger.Info("检测到用户尚未退出房间, 正在退出...");
                    
                    // 3. 将同步的 HTTP 请求包裹在 Task 中，添加超时控制
                    var exitRoomTask = Task.Run(() =>
                    {
                        return X19Http.Post("/online-lobby-room-enter/leave-room",
                            JsonConvert.SerializeObject(new { room_id = WpfConfig.RoomInfo.entity.entity_id }));
                    });

                    // 设置最大等待时间（例如 1.5 秒）。如果网络卡死，1.5秒后直接强制退出，不一直等
                    if (exitRoomTask.Wait(TimeSpan.FromMilliseconds(1500)))
                    {
                        var sExitRoomResult = exitRoomTask.Result;
                        WpfConfig.DefaultLogger.Info($"退出房间返回: {Regex.Escape(sExitRoomResult)}");
                        
                        try 
                        {
                            if (JObject.Parse(sExitRoomResult)["code"].ToObject<int>() == 0)
                                WpfConfig.DefaultLogger.Info("退出房间成功!");
                            else
                                WpfConfig.DefaultLogger.Error($"退出房间失败,返回信息:{JObject.Parse(sExitRoomResult)["message"]}!");
                        }
                        catch (Exception ex)
                        {
                            WpfConfig.DefaultLogger.Error($"解析退房响应失败: {ex.Message}");
                        }
                    }
                    else
                    {
                        WpfConfig.DefaultLogger.Error("退出房间请求超时，强制结束！");
                    }
                }
            }
            catch (Exception ex)
            {
                WpfConfig.DefaultLogger.Error($"退出清理时发生异常: {ex.Message}");
            }
            finally
            {
                WpfConfig.DefaultLogger.Info("清理完成, 正在退出程序...");
                Environment.Exit(0);
            }
        }

        private static bool HandlerRoutine(int ctrlType)
        {
            // 处理 X 关闭和 Ctrl+C
            if (ctrlType == CTRL_CLOSE_EVENT || ctrlType == CTRL_C_EVENT)
            {
                WpfConfig.DefaultLogger.Info("\n[拦截] 检测到控制台正在关闭！");

                // 4. 将退出操作放到后台线程执行，让 HandlerRoutine 立即返回 true。
                // 这样 Windows 操作系统就不会因为回调函数阻塞而触发 5 秒强制查杀等待。
                new Thread(() => ExitProcess()) { IsBackground = false }.Start();

                return true; // 告诉系统我们已处理该消息，请等待我们自行 Exit
            }

            return false;
        }

        private static void OnProcessExit(object sender, EventArgs e)
        {
            // 由于上面加了 _isExiting 锁，这里调用不会造成死循环和重复发包
            ExitProcess();
        }

        [ModuleInitializer]
        internal static void InitializeOnLoad()
        {
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

            if (!File.Exists("DisableConsole"))
            {
                AllocConsole();

                const uint CP_GBK = 936;
                SetConsoleOutputCP(CP_GBK);
                Console.OutputEncoding = Encoding.GetEncoding(936);

                var writer = new StreamWriter(Console.OpenStandardOutput(), Console.OutputEncoding);
                writer.AutoFlush = true;
                Console.SetOut(writer);
                Console.CursorVisible = false;

                _consoleHandler = HandlerRoutine;
                SetConsoleCtrlHandler(_consoleHandler, true);
            }
            
            var dummyColor = System.Drawing.SystemColors.Window; 
            var dummyFont = System.Drawing.SystemFonts.DefaultFont;
            
            Microsoft.Win32.SystemEvents.UserPreferenceChanged += DummyHandler;
            Microsoft.Win32.SystemEvents.UserPreferenceChanged -= DummyHandler;
            Mcl.Core.Updater.UpdateManager.Initialize();
            SettingsInjector.Start();

            MethodHook.InstallTypes(new[] { typeof(InitHook) });
        }
        
        private static void DummyHandler(object sender, UserPreferenceChangedEventArgs e) { }
    }
}