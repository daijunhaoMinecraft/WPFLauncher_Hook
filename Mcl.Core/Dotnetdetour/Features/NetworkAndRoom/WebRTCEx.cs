using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;
using Mcl.Core.Dotnetdetour.Features.GameTweaks;
using Mcl.Core.Dotnetdetour.Features.GeneralHooks;
using Mcl.Core.Dotnetdetour.Models.Config;
using Mcl.Core.Dotnetdetour.Models.Globals;
using Mcl.Core.Dotnetdetour.UI.Controls;
using Mcl.Core.Dotnetdetour.UI.Core;  // [新增] 必须引用包含 ThreadHelperSTATask 的命名空间
using WPFLauncher.Common;
using WPFLauncher.Manager.Game.Pipeline;
using WPFLauncher.Model;
using WPFLauncher.Network.TransService;
using WPFLauncher.Util;

namespace Mcl.Core.Dotnetdetour.Features.NetworkAndRoom;

public class WebRtcEx : IMethodHook
{
    public static string ByteArrayToHexString(byte[] byteArray)
    {
        StringBuilder hex = new StringBuilder(byteArray.Length * 2);
        foreach (byte b in byteArray)
        {
            hex.AppendFormat("{0:x2}", b);
        }
        return hex.ToString();
    }
    
    [OriginalMethod]
    private int RunGameOriginal()
    {
        return 0;
    }

    [HookMethod(TargetConst.JavaProcess, TargetConst.JavaStartTarget, "RunGameOriginal")]
    private int RunGame()
    {
        bool originalStart = true;
        if (WpfConfig.AllowFrp)
        {
            if (WebRtcVar.LanGameManager != null)
            {
                WpfConfig.DefaultLogger.Info(WebRtcVar.LanGameManager.ae());
                try
                {
                    if (WpfConfig.UseNetworkMode)
                    {
                        // [修复] 使用 WPF STA 线程呼出 SelectIpWindow
                        string GetUserVirtualIp()
                        {
                            string ip = null;
                            ThreadHelperSTATask.Run(() =>
                            {
                                var ipForm = new SelectIpWindow();
                                if (ipForm.ShowDialog() == true) 
                                    ip = ipForm.SelectedIp;
                            });
                            return ip;
                        }

                        // [修复] WPF 中异步非阻塞呼出独立窗口的标准做法
                        void ShowMonitorAsync()
                        {
                            var monitorThread = new Thread(() =>
                            {
                                try
                                {
                                    var window = new NetworkMonitorWindow();
                                    WebRtcVar.NetworkMonitor = window;
                                    
                                    // 开启当前线程的消息循环机制，使 WPF 窗口能在非主线程持续渲染
                                    window.Show();
                                    System.Windows.Threading.Dispatcher.Run();
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
                            var res = uz.q("是否使用组网功能(需管理员权限)", "", "是", "否");
                            if (res == MessageBoxResult.OK)
                            {
                                originalStart = false;
                                WebRtcVar.Enable = true;
                                WebRtcVar.PlayerList.Clear();
                                ProcessMessage.SendData(WebRtcVar.TargetPeerId, GetPlayerListProto.MagicHandshake.ToArray());
                                
                                while (WebRtcVar.PlayerList.Count == 0)
                                {
                                    Thread.Sleep(1000);
                                    WpfConfig.DefaultLogger.Info("等待玩家列表获取成功...");
                                }

                                WpfConfig.DefaultLogger.Info($"成功获取到 {WebRtcVar.PlayerList.Count} 个玩家。");
                                var clientIp = GetUserVirtualIp();

                                if (string.IsNullOrEmpty(clientIp))
                                {
                                    WpfConfig.DefaultLogger.Warn("用户未配置 IP，启动中止。");
                                    return -1;
                                }

                                WpfConfig.DefaultLogger.Info($"[客户端] 正在启动虚拟网卡 ({clientIp})...");

                                Task.Run(() =>
                                {
                                    try
                                    {
                                        WintunRouterService.Instance.Start(clientIp);
                                    }
                                    catch (Exception ex)
                                    {
                                        WpfConfig.DefaultLogger.Error($"启动失败: {ex.Message}");
                                    }
                                });

                                WebRtcVar.Enable = true;
                                WpfConfig.DefaultLogger.Info($"客户端已启动。IP: {clientIp}");

                                ShowMonitorAsync();
                                return 0;
                            }
                        }
                        else if (WebRtcVar.Mode == ForwardMode.Server)
                        {
                            var res = uz.q("是否使用组网功能(需管理员权限)", "", "是", "否");
                            if (res == MessageBoxResult.OK)
                            {
                                originalStart = false;
                                WebRtcVar.Mode = ForwardMode.Server;

                                var serverIp = GetUserVirtualIp();
                                if (string.IsNullOrEmpty(serverIp))
                                {
                                    WpfConfig.DefaultLogger.Warn("用户未配置 IP，启动中止。");
                                    return -1;
                                }

                                if (WebRtcVar.LanGameManager == null)
                                    WpfConfig.DefaultLogger.Warn("房间管理实例 为 Null");
                                else if (WebRtcVar.LanGameManager.aya == null) 
                                    WpfConfig.DefaultLogger.Warn("发包函数为Null");

                                CallAtpDMethodUsingReflection(WebRtcVar.LanGameManager, RoomVisibleStatus.OPEN);
                                CallShowRoomManageReflection();

                                WpfConfig.DefaultLogger.Info($"[服务端] 正在启动虚拟网卡 ({serverIp})...");
                                WintunRouterService.Instance.Start(serverIp);

                                WebRtcVar.Enable = true;
                                WpfConfig.DefaultLogger.Info($"服务端已启动。IP: {serverIp}");

                                ShowMonitorAsync();
                                return 0;
                            }
                        }
                    }
                    else
                    {
                        if (WebRtcVar.Mode == ForwardMode.Client)
                        {
                            var res = uz.q("是否将数据转发到一个端口上(WebRtc->端口->玩家)", "", "是", "否");
                            if (res == MessageBoxResult.OK)
                            {
                                originalStart = false;
                                
                                // [修复] 使用 STA 安全包裹 ClientSelectPortWindow
                                ThreadHelperSTATask.Run(() =>
                                {
                                    var f = new ClientSelectPortWindow();
                                    f.ShowDialog();
                                });

                                WebRtcVar.InitForwarder();

                                // [修复] 启动非阻塞的 ForwarderControlPanel
                                var panelThread = new Thread(() =>
                                {
                                    try
                                    {
                                        var panel = new ForwarderControlPanel();
                                        panel.Show();
                                        System.Windows.Threading.Dispatcher.Run();
                                    }
                                    catch (Exception ex)
                                    {
                                        WpfConfig.DefaultLogger.Error($"[控制台窗体错误] {ex}");
                                    }
                                });
                                panelThread.SetApartmentState(ApartmentState.STA);
                                panelThread.IsBackground = true;
                                panelThread.Start();
                                
                                return 0;
                            }
                        }
                        else if (WebRtcVar.Mode == ForwardMode.Server)
                        {
                            var res = uz.q("是否启用端口转发功能(端口->WebRtc->玩家)", "", "是", "否");
                            if (res == MessageBoxResult.OK)
                            {
                                originalStart = false;
                                WebRtcVar.Mode = ForwardMode.Server;
                                
                                // [修复] 使用 STA 安全包裹 ServerSelectPortWindow
                                ThreadHelperSTATask.Run(() =>
                                {
                                    var f = new ServerSelectPortWindow();
                                    f.ShowDialog();
                                });

                                if (WebRtcVar.LanGameManager == null)
                                    WpfConfig.DefaultLogger.Warn("房间管理实例 为 Null");
                                else if (WebRtcVar.LanGameManager.aya == null) 
                                    WpfConfig.DefaultLogger.Warn("发包函数为Null");
                                
                                CallAtpDMethodUsingReflection(WebRtcVar.LanGameManager, RoomVisibleStatus.OPEN);
                                CallShowRoomManageReflection();

                                WebRtcVar.InitForwarder();
                                return 0;
                            }
                        }
                    }
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
            }
        }
        
        if (WpfConfig.EnableModsInject)
        {
            var modsInjectPath = Path.Combine(Directory.GetCurrentDirectory(), "ModsInject");
            var minecraftModsPath = Path.Combine(MinecraftPath.GetMinecraftPath(), "mods");

            if (!Directory.Exists(minecraftModsPath))
                Directory.CreateDirectory(minecraftModsPath);

            var jarFiles = Directory.GetFiles(modsInjectPath, "*.jar");

            using (var md5 = MD5.Create())
            {
                foreach (var jarFile in jarFiles)
                {
                    string originalFileName = Path.GetFileName(jarFile);
                    byte[] hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(originalFileName));

                    long number = Math.Abs(BitConverter.ToInt64(hashBytes, 0));
                    string numericId = number.ToString().PadLeft(18, '0');
                    if (numericId.Length > 18)
                        numericId = numericId.Substring(0, 18);

                    int ver1 = (Math.Abs(hashBytes[0]) % 9) + 1;
                    int ver2 = (Math.Abs(hashBytes[1]) % 9) + 1;

                    string newFileName = $"{numericId}@{ver1}@{ver2}.jar";
                    string destinationPath = Path.Combine(minecraftModsPath, newFileName);

                    File.Copy(jarFile, destinationPath, true);
                    WpfConfig.DefaultLogger.Info($"成功复制模组: {originalFileName} (伪装名称: {newFileName}) 到 {minecraftModsPath}");
                }
            }
        }
        return RunGameOriginal();
    }

    private static bool CallAtpDMethodUsingReflection(GameM gameM, RoomVisibleStatus roomVisibleStatus)
    {
        try
        {
            var wpfLauncherAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.FullName.Contains("WPFLauncher"));

            if (wpfLauncherAssembly == null) return false;

            var atpType = wpfLauncherAssembly.GetType("WPFLauncher.Manager.LanGame.atp");
            if (atpType == null) return false;

            var azeGenericType = typeof(azf<>);
            var constructedAzeType = azeGenericType.MakeGenericType(atpType);

            var instanceProperty = constructedAzeType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            if (instanceProperty == null) return false;

            var atpInstance = instanceProperty.GetValue(null);
            if (atpInstance == null) return false;

            var dMethod = atpInstance.GetType().GetMethod("d", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (dMethod != null)
            {
                dMethod.Invoke(atpInstance, new object[] { gameM, roomVisibleStatus });
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            WpfConfig.DefaultLogger.Error($"[WebRtcEx] 反射调用 atp.d() 方法时出错: {ex.Message}");
            return false;
        }
    }

    private static void CallShowRoomManageReflection()
    {
        object target = WebRtcVar.LanGameManager;
        if (target == null) return;

        var method = target.GetType().GetMethod("ap", BindingFlags.NonPublic | BindingFlags.Instance);
        if (method == null) return;

        try
        {
            method.Invoke(target, null); 
        }
        catch (Exception ex)
        {
            WpfConfig.DefaultLogger.Error($"反射调用失败: {ex}");
        }
    }

    [OriginalMethod]
    public void SetGameMOriginal(ait gameM) { }

    [HookMethod(TargetConst.LanGameManager, "e", "SetGameMOriginal")]
    public void SetGameM(ait gameM)
    {
        WebRtcVar.LanGameManager = gameM;
        WpfConfig.DefaultLogger.Info("获取gameM实例成功!");
        SetGameMOriginal(gameM);
    }

    [HookMethod("WPFLauncher.Manager.Game.Crash.ava", "b")]
    public static string b(int ogm)
    {
        var stackTrace = new StackTrace(true);
        WpfConfig.DefaultLogger.Info("[WebRtcEx.b] 调用堆栈:");
        for (var i = 0; i < stackTrace.FrameCount; i++)
        {
            var frame = stackTrace.GetFrame(i);
            var method = frame.GetMethod();
            WpfConfig.DefaultLogger.Info($"  [{i}] {method.DeclaringType?.FullName}.{method.Name} (行: {frame.GetFileLineNumber()})");
        }
        return "恭喜: 你的Crash被我截到了";
    }

    [OriginalMethod]
    private void ClearProcessOriginal(avo min) { }

    [HookMethod("WPFLauncher.Manager.aqr", "b", "ClearProcessOriginal")]
    public void ClearProcess(avo min)
    {
        try { ClearProcessOriginal(min); }
        catch (Exception ex) { Console.WriteLine($"发生异常: {ex.Message}"); }
    }

    #region 判断玩家当前状态(进入房间/创建房间)

    [OriginalMethod]
    private void JoinRoomResultOriginal(byte[] data) { }

    [HookMethod(TargetConst.LanGameManager, "af", "JoinRoomResultOriginal")]
    private void JoinRoomResult(byte[] data)
    {
        WebRtcVar.Mode = ForwardMode.Client;
        Console.WriteLine("[WebRtc] 切换模式至客户端");
        JoinRoomResultOriginal(data);
    }

    [OriginalMethod]
    private void SendCreateRoomOriginal(ait config) { }

    [HookMethod(TargetConst.LanGameManager, "aa", "SendCreateRoomOriginal")]
    private void SendCreateRoom(ait config)
    {
        if (config != null)
            WebRtcVar.LanGameManager = config;
        
        WebRtcVar.Mode = ForwardMode.Server;
        Console.WriteLine("[WebRtc] 切换模式至服务端");
        SendCreateRoomOriginal(config);
    }

    [OriginalMethod]
    public void ExitRoomOriginal() { }

    [HookMethod(TargetConst.LanGameManager, "t", "ExitRoomOriginal")]
    public void ExitRoom()
    {
        Console.WriteLine("[WebRtc] 退出房间");
        WebRtcVar.StopForwarder();
        WebRtcVar.Mode = ForwardMode.None;
        ExitRoomOriginal();
    }

    #endregion
}