using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Mcl.Core.Dotnetdetour.Hookevent;
using Mcl.Core.Dotnetdetour.Tools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mcl.Core.Dotnetdetour;

// 1. 手动补齐 .NET 4.8.1 缺失的 ModuleInitializerAttribute 特性
// 注意：命名空间必须严格是 System.Runtime.CompilerServices
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class ModuleInitializerAttribute : Attribute
{
}

// 2. 编写启动器类
public static class HookBootstrapper
{
    // 导入 AllocConsole 函数
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AllocConsole();
		        
    [DllImport("kernel32.dll")]
    private static extern bool SetConsoleOutputCP(uint wCodePageID);
        
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate handler, bool add);

    public delegate bool ConsoleCtrlDelegate(int ctrlType);

    // 控制台事件类型枚举
    public const int CTRL_C_EVENT = 0;
    public const int CTRL_BREAK_EVENT = 1;
    public const int CTRL_CLOSE_EVENT = 2; // 点击 "X" 按钮
    public const int CTRL_LOGOFF_EVENT = 5;
    public const int CTRL_SHUTDOWN_EVENT = 6;

    private static ConsoleCtrlDelegate _consoleHandler;

    private static void ExitProcess()
    {
        Logger.Info("程序即将退出, 正在执行清理操作...");
        Logger.Info("检查是否进入联机大厅房间");
        if (Path_Bool.RoomInfo != null)
        {
            if (!string.IsNullOrWhiteSpace(Path_Bool.RoomInfo.entity.entity_id))
            {
                Logger.Info("检测到用户尚未退出房间, 正在退出...");
                Console.WriteLine("[AutoExit] 正在退出房间...");
                var sExitRoomResult = X19Http.RequestX19Api("/online-lobby-room-enter/leave-room",
                    JsonConvert.SerializeObject(new { room_id = Path_Bool.RoomInfo.entity.entity_id }));
                Console.WriteLine($"[AutoExit] 退出房间返回:{Regex.Escape(sExitRoomResult)}");
                if (JObject.Parse(sExitRoomResult)["code"].ToObject<int>() == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("[AutoExit] 退出房间成功!");
                    Console.ForegroundColor = ConsoleColor.White;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[AutoExit] 退出房间失败,返回信息:{JObject.Parse(sExitRoomResult)["message"]}!");
                    Console.ForegroundColor = ConsoleColor.White;
                }
            }
        }
        Logger.Info("通过, 正在退出程序...");
    }
        
    // 2. 拦截事件的执行逻辑
    private static bool HandlerRoutine(int ctrlType)
    {
        if (ctrlType == CTRL_CLOSE_EVENT)
        {
            // 在这里运行你的拦截代码！
            Console.WriteLine("\n[拦截] 检测到控制台正在关闭！");
            
            // 运行清理代码、保存数据等操作
            ExitProcess();

            return true; 
        }

        return false;
    }
        
        
    private static void OnProcessExit(object sender, EventArgs e)
    {
        ExitProcess();
    }

        
        
    // 标记为 ModuleInitializer
    [ModuleInitializer]
    internal static void InitializeOnLoad()
    {
        AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnProcessExit);

        if (!File.Exists("DisableConsole"))
        {
            // 分配一个新的控制台
            AllocConsole();
            
            const uint CP_GBK = 936;
            
            // 1. 强制设置控制台输出代码页为 936 (GBK)
            SetConsoleOutputCP(CP_GBK);
            
            // 2. 设置 .NET 控制台输出编码为 GBK
            Console.OutputEncoding = Encoding.GetEncoding(936);
            
            // 3. 重定向输出流，并显式指定编码！
            var writer = new StreamWriter(
                Console.OpenStandardOutput(),
                Console.OutputEncoding  // 👈 关键：使用一致的编码
            );
            writer.AutoFlush = true;
            Console.SetOut(writer);
            Console.CursorVisible = false;
                
            // 实例化委托并保持引用
            _consoleHandler = new ConsoleCtrlDelegate(HandlerRoutine);
        
            // 注册控制台事件处理器
            SetConsoleCtrlHandler(_consoleHandler, true);
        }
        MethodHook.InstallTypes(new[] { typeof(InitHook) });
    }
}