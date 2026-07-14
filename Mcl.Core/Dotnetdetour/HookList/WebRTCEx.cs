using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using Mcl.Core.Dotnetdetour.Tools.Network;
using Mcl.Core.Dotnetdetour.Var;
using Mcl.Core.Dotnetdetour.Window;
using WPFLauncher.Manager.Game.Pipeline;
using WPFLauncher.Model;
using WPFLauncher.Network.TransService;
using WPFLauncher.Util;

namespace Mcl.Core.Dotnetdetour.HookList;

// WebRTC扩展: 国际服之间联机
public class WebRtcEx : IMethodHook
{
    [OriginalMethod]
    private int RunGameOriginal()
    {
        return 0;
    }
    
    // 拦截Java启动
    [HookMethod(TargetConst.JavaProcess, TargetConst.JavaStartTarget, "RunGameOriginal")]
    private int RunGame()
    {
        if (WebRtcVar.LanGameManager != null)
        {
            WpfConfig.DefaultLogger.Info(WebRtcVar.LanGameManager.ae());
        }
        try
        {
            if (WpfConfig.UseNetworkMode)
            {
                // 辅助函数：获取用户 IP
                string GetUserVirtualIp()
                {
                    using (var ipForm = new SelectIp())
                    {
                        if (ipForm.ShowDialog() == DialogResult.OK)
                        {
                            return ipForm.SelectedIp;
                        }
                        return null;
                    }
                }
                
                void ShowMonitorAsync()
                {
                    Thread monitorThread = new Thread(() =>
                    {
                        try
                        {
                            // 1. 先创建局部变量，确保实例创建成功
                            var tempForm = new NetworkMonitorForm(); 
            
                            if (tempForm == null) {
                                WpfConfig.DefaultLogger.Error("窗体实例化失败！");
                                return;
                            }

                            // 2. 赋值给全局静态变量
                            WebRtcVar.NetworkMonitor = tempForm;

                            // 3. 运行这个局部实例
                            System.Windows.Forms.Application.Run(tempForm); 
                        }
                        catch (Exception ex)
                        {
                            WpfConfig.DefaultLogger.Error($"[监控窗体错误] {ex}");
                        }
                    });

                    monitorThread.SetApartmentState(ApartmentState.STA);
                    monitorThread.IsBackground = true;
                    monitorThread.Start();
                }

                if (WebRtcVar.Mode == ForwardMode.Client)
                {
                    MessageBoxResult res = uz.q("是否使用组网功能(需管理员权限)", "", "是", "否", "");
                    if (res == MessageBoxResult.OK)
                    {
                        WebRtcVar.Enable = true;
                        WebRtcVar.PlayerList.Clear();
                        WebSocket_WebRtc.SendData(WebRtcVar.TargetPeerId, GetPlayerListProto.MagicHandshake.ToArray());
                        while (WebRtcVar.PlayerList.Count == 0)
                        {
                            Thread.Sleep(1000);
                            WpfConfig.DefaultLogger.Info($"等待玩家列表获取成功...");
                        }
                        WpfConfig.DefaultLogger.Info($"成功获取到 {WebRtcVar.PlayerList.Count} 个玩家。");
                        string clientIp = GetUserVirtualIp();
                        
                        if (string.IsNullOrEmpty(clientIp))
                        {
                            WpfConfig.DefaultLogger.Warn("用户未配置 IP，启动中止。");
                            return -1;
                        }

                        WpfConfig.DefaultLogger.Info($"[客户端] 正在启动虚拟网卡 ({clientIp})...");
                        
                        // 启动 Wintun (后台运行)
                        Task.Run(() => { 
                            try 
                            {
                                WintunRouterService.Instance.Start(clientIp); 
                            }
                            catch (Exception ex) 
                            {
                                WpfConfig.DefaultLogger.Error($"启动失败: {ex.Message}");
                                // 这里可能需要通知 UI 层报错
                            }
                        });

                        WebRtcVar.Enable = true;
                        WpfConfig.DefaultLogger.Info($"客户端已启动。IP: {clientIp}");
                        
                        // [关键修改] 启动后立即打开监控窗口
                        // Task.Run((() => ShowMonitor()));
                        ShowMonitorAsync();
                        
                        return 0;
                    }
                }
                else if (WebRtcVar.Mode == ForwardMode.Server)
                {
                    MessageBoxResult res = uz.q("是否使用组网功能(需管理员权限)", "", "是", "否", "");
                    if (res == MessageBoxResult.OK)
                    {
                        WebRtcVar.Mode = ForwardMode.Server;

                        string serverIp = GetUserVirtualIp();
                        if (string.IsNullOrEmpty(serverIp))
                        {
                            WpfConfig.DefaultLogger.Warn("用户未配置 IP，启动中止。");
                            return -1;
                        }

                        if (WebRtcVar.LanGameManager == null)
                        {
                            WpfConfig.DefaultLogger.Warn("房间管理实例 为 Null");
                        }
                        else if (WebRtcVar.LanGameManager.aya == null)
                        {
                            WpfConfig.DefaultLogger.Warn("发包函数为Null");
                        }

                        // 执行游戏逻辑
                        CallAtpDMethodUsingReflection(WebRtcVar.LanGameManager as GameM, RoomVisibleStatus.OPEN);
                        CallShowRoomManageReflection();

                        WpfConfig.DefaultLogger.Info($"[服务端] 正在启动虚拟网卡 ({serverIp})...");
                        
                        // 启动 Wintun
                        WintunRouterService.Instance.Start(serverIp);
                        
                        WebRtcVar.Enable = true;
                        WpfConfig.DefaultLogger.Info($"服务端已启动。IP: {serverIp}");

                        // [关键修改] 启动后立即打开监控窗口
                        ShowMonitorAsync();

                        return 0;
                    }
                }
            }
            else
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
                        if (WebRtcVar.LanGameManager == null)
                        {
                            WpfConfig.DefaultLogger.Warn("房间管理实例 为 Null");
                        }
                        else if (WebRtcVar.LanGameManager.aya == null)
                        {
                            WpfConfig.DefaultLogger.Warn("发包函数为Null");
                        }
                        CallAtpDMethodUsingReflection(WebRtcVar.LanGameManager as GameM, RoomVisibleStatus.OPEN);
                        CallShowRoomManageReflection();
            
                        WebRtcVar.InitForwarder();
                        return 0;
                    }
                }
            }
            return RunGameOriginal();
        }
        catch (AccessViolationException ave)
        {
            WpfConfig.DefaultLogger.Error($"内存违规: {ave.Message}");
            WpfConfig.DefaultLogger.Error($"StackTrace: {ave.StackTrace}");
            return 0;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        return 0;
    }

    /// <summary>
    /// 使用反射调用 WPFLauncher.Common.azf<atp>.Instance.d() 方法
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
                WpfConfig.DefaultLogger.Error("[WebRtcEx] 未找到 WPFLauncher 程序集");
                return false;
            }

            // 获取 atp 类型
            Type atpType = wpfLauncherAssembly.GetType("WPFLauncher.Manager.LanGame.atp");
            if (atpType == null)
            {
                WpfConfig.DefaultLogger.Error("[WebRtcEx] 未找到 LanGame.atp 类型");
                return false;
            }

            // 构造 WPFLauncher.Common.azf<> 泛型类型
            Type azeGenericType = typeof(WPFLauncher.Common.azf<>);
            Type constructedAzeType = azeGenericType.MakeGenericType(atpType);

            // 获取 Instance 属性
            PropertyInfo instanceProperty = constructedAzeType.GetProperty("Instance",
                BindingFlags.Public | BindingFlags.Static);

            if (instanceProperty == null)
            {
                WpfConfig.DefaultLogger.Error("[WebRtcEx] 未找到 Instance 属性");
                return false;
            }

            // 获取 atp 实例
            object atpInstance = instanceProperty.GetValue(null);
            if (atpInstance == null)
            {
                WpfConfig.DefaultLogger.Error("[WebRtcEx] atp 实例为 null");
                return false;
            }

            // 获取 d 方法并调用
            MethodInfo dMethod = atpInstance.GetType().GetMethod("d",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (dMethod != null)
            {
                dMethod.Invoke(atpInstance, new object[] { gameM, roomVisibleStatus });
                WpfConfig.DefaultLogger.Info("[WebRtcEx] 成功调用 atp.d() 方法");
                return true;
            }
            else
            {
                WpfConfig.DefaultLogger.Error("[WebRtcEx] 未找到 d 方法");
                return false;
            }
        }
        catch (Exception ex)
        {
            WpfConfig.DefaultLogger.Error($"[WebRtcEx] 反射调用 atp.d() 方法时出错: {ex.Message}\nStackTrace: {ex.StackTrace}");
            return false;
        }
    }

    private static void CallShowRoomManageReflection()
    {
        // 获取目标对象
        object target = WebRtcVar.LanGameManager;

        if (target == null)
        {
            WpfConfig.DefaultLogger.Error("WebRtcVar.gameM 为 null");
            return;
        }

        // 通过反射获取私有方法 "ap"
        MethodInfo method = target.GetType().GetMethod(
            "ap",
            BindingFlags.NonPublic | BindingFlags.Instance // 私有 + 实例方法
        );

        if (method == null)
        {
            WpfConfig.DefaultLogger.Error("未找到私有方法 'ap'");
            return;
        }

        // 调用方法（无参数）
        try
        {
            method.Invoke(target, null); // 或 method.Invoke(target, new object[0])
            WpfConfig.DefaultLogger.Info("成功调用 ap() 方法");
        }
        catch (TargetInvocationException ex)
        {
            WpfConfig.DefaultLogger.Error($"调用 ap() 时发生异常: {ex}");
        }
        catch (Exception ex)
        {
            WpfConfig.DefaultLogger.Error($"反射调用失败: {ex}");
        }
    }

    [OriginalMethod]
    public void SetGameMOriginal(ait gameM) {}

    [HookMethod(TargetConst.LanGameManager, "e", "SetGameMOriginal")]
    public void SetGameM(ait gameM)
    {
        WebRtcVar.LanGameManager = gameM;
        WpfConfig.DefaultLogger.Info("获取gameM实例成功!");
        SetGameMOriginal(gameM);
    }
    
    [HookMethod("WPFLauncher.Manager.Game.Crash.ava", "b", null)]
    public static string b(int ogm)
    {
        // 输出当前函数调用的堆栈信息
        StackTrace stackTrace = new StackTrace(true);
        WpfConfig.DefaultLogger.Info("[WebRtcEx.b] 调用堆栈:");
        for (int i = 0; i < stackTrace.FrameCount; i++)
        {
            StackFrame frame = stackTrace.GetFrame(i);
            MethodBase method = frame.GetMethod();
            WpfConfig.DefaultLogger.Info($"  [{i}] {method.DeclaringType?.FullName}.{method.Name} (行: {frame.GetFileLineNumber()})");
        }
        return "恭喜: 你的Crash被我截到了";
    }
    
    [OriginalMethod]
    private void ClearProcessOriginal(avo min)
    {
    }


    #region 判断玩家当前状态(进入房间/创建房间)

    [OriginalMethod]
    private void JoinRoomResultOriginal(byte[] data)
    {
    }

    [HookMethod(TargetConst.LanGameManager, "af", "JoinRoomResultOriginal")]
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

    [HookMethod(TargetConst.LanGameManager, "aa", "SendCreateRoomOriginal")]
    private void SendCreateRoom(ait config)
    {
        if (config != null)
        {
            WebRtcVar.LanGameManager = config;
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

    [HookMethod(TargetConst.LanGameManager, "t", "ExitRoomOriginal")]
    public void ExitRoom()
    {
        Console.WriteLine("[WebRtc] 退出房间");
        WebRtcVar.StopForwarder();
        // WebRtcVar.ControlPanel.Close();
        WebRtcVar.Mode = ForwardMode.None;
        ExitRoomOriginal();
    }
    
    #endregion

    [HookMethod("WPFLauncher.Manager.aqr", "b", "ClearProcessOriginal")]
    public void ClearProcess(avo min)
    {
        try
        {
            ClearProcessOriginal(min);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"发生异常: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }
    
    
}