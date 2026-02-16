using System;
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
using WPFLauncher.Manager.Configuration;
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

    [OriginalMethod]
    public static GameM GetGameMOriginal(axc content)
    {
        return null;
    }

    [HookMethod("WPFLauncher.Model.Game.Factory.all", "a", "GetGameMOriginal")]
    public static GameM GetGameM(axc content)
    {
        GameM gameM = GetGameMOriginal(content);
        if (content.GameType == GType.LAN_GAME && !sa.a(content.GameVersion))
        {
            WebRtcVar.AitFunction = (ait)gameM;
            // Console.WriteLine("[WebRtc] 获取游戏实例成功");
        }

        return gameM;
    }

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
        WebRtcVar.ControlPanel.Close();
        WebRtcVar.Mode = ForwardMode.None;

        Console.WriteLine("[WebRtc] 清理完毕");
        ExitRoomOriginal();
    }
    
    #endregion
}