using System;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using DotNetTranstor;
using DotNetTranstor.Hookevent;
using Mcl.Core.DotNetTranstor.Tools;
using Mcl.Core.DotNetTranstor.Var;
using Mcl.Core.DotNetTranstor.Window;
using WPFLauncher.Common;
using WPFLauncher.Manager;
using WPFLauncher.Manager.Configuration;
using WPFLauncher.Manager.Game.Pipeline;
using WPFLauncher.Manager.LanGame;
using WPFLauncher.Model;
using WPFLauncher.Model.Game.GameClient;
using WPFLauncher.Network.TransService;
using WPFLauncher.SQLite;
using WPFLauncher.Util;

namespace Mcl.Core.DotNetTranstor.Hookevent;

// WebRTC扩展: 国际服之间联机
public class WebRtcEx : IMethodHook
{
    [OriginalMethod]
    private int RunGameOriginal()
    {
        return 0;
    }
    
    // 拦截Java启动
    [HookMethod("WPFLauncher.Manager.Game.Launcher.auv", "m", "RunGameOriginal")]
    private int RunGame()
    {
        try
        {
            if (WebRtcVar.Mode == ForwardMode.Client)
            {
                MessageBoxResult res = uz.q("是否将数据转发到一个端口上(WebRtc->端口->玩家)", "", "是", "否", "");
                if (res == MessageBoxResult.OK)
                {
                    using (var f = new ClientSelectPort()) f.ShowDialog();
                    WebRtcVar.InitForwarder();
                    // 显式指定 System.Windows.Forms 避免和 WPF 冲突
                    Task.Run(() => { System.Windows.Forms.Application.Run(new ForwarderControlPanel()); });
                    return 0;
                }
            }
            else if (WebRtcVar.Mode == ForwardMode.Server)
            {
                MessageBoxResult res = uz.q("是否启用端口转发功能(端口->WebRtc->玩家)", "", "是", "否", "");
                if (res == MessageBoxResult.OK)
                {
                    WebRtcVar.Mode = ForwardMode.Server;
                    using (var f = new ServerSelectPort()) f.ShowDialog();
                    if (WebRtcVar.AitFunction == null)
                    {
                        Console.WriteLine("ait 为 null");
                    }
                    else if (WebRtcVar.AitFunction.axy == null)
                    {
                        Console.WriteLine("发包函数为Null");
                    }
                    CallAtpDMethodUsingReflection(WebRtcVar.AitFunction as GameM, RoomVisibleStatus.OPEN);
                    // WebRtcVar.gameM.axy.@as(new object[]
                    // {
                    //     528,
                    //     (byte)RoomVisibleStatus.OPEN
                    // });
                    CallShowRoomManageReflection();
            
                    WebRtcVar.InitForwarder();
                    return 0;
                }
            }
            return RunGameOriginal();
        }
        catch (AccessViolationException ave)
        {
            Console.WriteLine($"内存违规: {ave.Message}");
            Console.WriteLine($"StackTrace: {ave.StackTrace}");
            return 0;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        return 0;
    }

    /// <summary>
    /// 使用反射调用 aze<atp>.Instance.d() 方法
    /// </summary>
    /// <param name="gameM">GameM 参数</param>
    /// <param name="roomVisibleStatus">RoomVisibleStatus 参数</param>
    private static bool CallAtpDMethodUsingReflection(GameM gameM, RoomVisibleStatus roomVisibleStatus)
    {
        try
        {
            // 获取 WPFLauncher 程序集
            Assembly wpfLauncherAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.FullName.Contains("WPFLauncher"));

            if (wpfLauncherAssembly == null)
            {
                Console.WriteLine("[WebRtcEx] 未找到 WPFLauncher 程序集");
                return false;
            }

            // 获取 atp 类型
            Type atpType = wpfLauncherAssembly.GetType("WPFLauncher.Manager.LanGame.atp");
            if (atpType == null)
            {
                Console.WriteLine("[WebRtcEx] 未找到 LanGame.atp 类型");
                return false;
            }

            // 构造 aze<> 泛型类型
            Type azeGenericType = typeof(aze<>);
            Type constructedAzeType = azeGenericType.MakeGenericType(atpType);

            // 获取 Instance 属性
            PropertyInfo instanceProperty = constructedAzeType.GetProperty("Instance",
                BindingFlags.Public | BindingFlags.Static);

            if (instanceProperty == null)
            {
                Console.WriteLine("[WebRtcEx] 未找到 Instance 属性");
                return false;
            }

            // 获取 atp 实例
            object atpInstance = instanceProperty.GetValue(null);
            if (atpInstance == null)
            {
                Console.WriteLine("[WebRtcEx] atp 实例为 null");
                return false;
            }

            // 获取 d 方法并调用
            MethodInfo dMethod = atpInstance.GetType().GetMethod("d",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (dMethod != null)
            {
                dMethod.Invoke(atpInstance, new object[] { gameM, roomVisibleStatus });
                Console.WriteLine("[WebRtcEx] 成功调用 atp.d() 方法");
                return true;
            }
            else
            {
                Console.WriteLine("[WebRtcEx] 未找到 d 方法");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[WebRtcEx] 反射调用 atp.d() 方法时出错: {ex.Message}\nStackTrace: {ex.StackTrace}");
            Console.ForegroundColor = ConsoleColor.White;
            return false;
        }
    }

    private static void CallShowRoomManageReflection()
    {
        // 获取目标对象
        object target = WebRtcVar.AitFunction;

        if (target == null)
        {
            Console.WriteLine("WebRtcVar.gameM 为 null");
            return;
        }

        // 通过反射获取私有方法 "ap"
        MethodInfo method = target.GetType().GetMethod(
            "ap",
            BindingFlags.NonPublic | BindingFlags.Instance // 私有 + 实例方法
        );

        if (method == null)
        {
            Console.WriteLine("未找到私有方法 'ap'");
            return;
        }

        // 调用方法（无参数）
        try
        {
            method.Invoke(target, null); // 或 method.Invoke(target, new object[0])
            Console.WriteLine("成功调用 ap() 方法");
        }
        catch (TargetInvocationException ex)
        {
            Console.WriteLine($"调用 ap() 时发生异常: {ex.InnerException?.Message ?? ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"反射调用失败: {ex.Message}");
        }
    }

    // [OriginalMethod]
    // public static GameM GetGameMOriginal(axc content)
    // {
    //     return null;
    // }
    //
    // [HookMethod("WPFLauncher.Model.Game.Factory.all", "a", "GetGameMOriginal")]
    // public static GameM GetGameM(axc content)
    // {
    //     GameM gameM = GetGameMOriginal(content);
    //     if (content.GameType == GType.LAN_GAME && !sa.a(content.GameVersion))
    //     {
    //         ait originalAit = WebRtcVar.AitFunction;
    //         WebRtcVar.AitFunction = (ait)gameM;
    //         Console.WriteLine("[WebRtc] 获取游戏实例成功");
    //     }
    //
    //     return gameM;
    // }

    [OriginalMethod]
    public void SetGameMOriginal(ait gameM) {}
    
    [HookMethod("WPFLauncher.Manager.LanGame.atm", "e", "SetGameMOriginal")]
    public void SetGameM(ait gameM)
    {
        WebRtcVar.AitFunction = gameM;
        Console.WriteLine("获取gameM实例成功!");
        SetGameMOriginal(gameM);
    }
    
    [HookMethod("WPFLauncher.Manager.Game.Crash.ava", "b", null)]
    public static string b(int ogm)
    {
        // 输出当前函数调用的堆栈信息
        StackTrace stackTrace = new StackTrace(true);
        Console.WriteLine("[WebRtcEx.b] 调用堆栈:");
        for (int i = 0; i < stackTrace.FrameCount; i++)
        {
            StackFrame frame = stackTrace.GetFrame(i);
            MethodBase method = frame.GetMethod();
            Console.WriteLine($"  [{i}] {method.DeclaringType?.FullName}.{method.Name} (行: {frame.GetFileLineNumber()})");
        }
        return "恭喜: 你的Crash被我截到了";
    }
    
    // [OriginalMethod]
    // private void ProcessErrOriginal(avo min)
    // {
    // }
    //
    // [HookMethod("WPFLauncher.Manager.aqb", "c", "ProcessErrOriginal")]
    // private void ProcessErr(avo min)
    // {
    //     // if (min.a > LTaskOpcode.NEXT)
    //     // {
    //     //     min.a = LTaskOpcode.CANCEL;
    //     //     Console.WriteLine("[WebRtc] 触发取消任务");
    //     // }
    //     // else
    //     // {
    //     //     Console.WriteLine($"[WebRtc] 当前min.a的值为: {min.a}");
    //     //     min.a = LTaskOpcode.NEXT;
    //     // }
    //     min.a = LTaskOpcode.NEXT;
    //     ProcessErrOriginal(min);
    // }


    #region 判断玩家当前状态(进入房间/创建房间)

    [OriginalMethod]
    private void JoinRoomResultOriginal(byte[] data)
    {
    }

    [HookMethod("WPFLauncher.Manager.LanGame.atm", "af", "JoinRoomResultOriginal")]
    private void JoinRoomResult(byte[] data)
    {
        WebRtcVar.Mode = ForwardMode.Client;
        Console.WriteLine("[WebRtc] 切换模式至客户端");
        JoinRoomResultOriginal(data);
    }

    [OriginalMethod]
    private void SendCreateRoomOriginal(ait config)
    {
    }

    [HookMethod("WPFLauncher.Manager.LanGame.atm", "aa", "SendCreateRoomOriginal")]
    private void SendCreateRoom(ait config)
    {
        if (config != null)
        {
            WebRtcVar.AitFunction = config;
        }
        else
        {
            Console.WriteLine("创建房间ait为null");
        }
        WebRtcVar.Mode = ForwardMode.Server;
        Console.WriteLine("[WebRtc] 切换模式至服务端");
        SendCreateRoomOriginal(config);
    }
    
    // 退出房间
    [OriginalMethod]
    public void ExitRoomOriginal()
    {
    }

    [HookMethod("WPFLauncher.Manager.LanGame.atm", "t", "ExitRoomOriginal")]
    public void ExitRoom()
    {
        Console.WriteLine("[WebRtc] 退出房间");
        WebRtcVar.StopForwarder();
        // WebRtcVar.ControlPanel.Close();
        WebRtcVar.Mode = ForwardMode.None;
        ExitRoomOriginal();
    }
    
    #endregion

    [HookMethod("WPFLauncher.Manager.aqr", "b", null)]
    public void ClearProcess()
    {
        try
        {
            // 1. 获取 aqr 的单例实例
            // 假设 aze<T> 是一个泛型单例类，Instance 是静态属性
            var instanceProperty = typeof(aze<aqr>).GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            
            if (instanceProperty == null)
            {
                Console.WriteLine("错误: 未找到 aze<aqr>.Instance 属性");
                return;
            }

            object aqrInstance = instanceProperty.GetValue(null);

            if (aqrInstance == null)
            {
                Console.WriteLine("错误: aqr 实例为 null");
                return;
            }

            // 2. 获取 aqr 的类型
            Type aqrType = aqrInstance.GetType();

            // 3. 获取私有字段 "a" 
            // BindingFlags.NonPublic: 包含私有成员
            // BindingFlags.Instance: 包含实例成员
            FieldInfo fieldInfo = aqrType.GetField("a", BindingFlags.NonPublic | BindingFlags.Instance);

            if (fieldInfo == null)
            {
                Console.WriteLine("错误: 未找到字段 'a' (可能名称已被混淆改变，当前反编译显示为 'a')");
                return;
            }

            // 4. 获取字段的值 (即 List<aqq> 对象)
            object listObj = fieldInfo.GetValue(aqrInstance);

            if (listObj == null)
            {
                Console.WriteLine("警告: 字段 'a' 当前为 null，无需清空");
                return;
            }

            // 5. 调用 Clear 方法
            // 确保它是一个 IList 或者具体的 List<>
            if (listObj is IList list)
            {
                // 原代码中有 lock(this.a)，但在外部反射调用 Clear() 通常是线程安全的原子操作
                // 如果极度担心竞态条件，理论上需要 lock(listObj)，但这需要获取 monitor 锁，比较复杂
                // 对于大多数清理场景，直接 Clear 即可
                list.Clear();
                Console.WriteLine("成功: 已清空 private List<aqq> a");
            }
            else
            {
                Console.WriteLine("错误: 字段 'a' 不是 IList 类型");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"发生异常: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }
}