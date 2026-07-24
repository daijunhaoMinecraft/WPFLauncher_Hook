using System;
using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;
using Mcl.Core.Dotnetdetour.Models.Config;
using WPFLauncher.Common;
using WPFLauncher.Manager;

namespace Mcl.Core.Dotnetdetour.Features.Authentication.Providers;

public class LoginWithoutMpay : IMethodHook
{
    [OriginalMethod]
    public void InitMpay(string title, string mpayPath, Action<int> initFinishAction, string uniSdkUrl) { }

    [HookMethod("WPFLauncher.Unisdk.nx", "a", "InitMpay")]
    public void InitMpayHook(string title, string mpayPath, Action<int> initFinishAction, string uniSdkUrl)
    {
        if (WpfConfig.MpayUnless)
        {
            initFinishAction(0);
            
            // 使用不允许原号登录的模式调起 UI
            string sauthJson = AuthIntegrationService.RequestUserLogin(allowOriginal: false);
            
            if (!string.IsNullOrEmpty(sauthJson))
            {
                AuthIntegrationService.InjectMpayCookie(sauthJson);
            }
            azf<apm>.Instance.CanChannelLogin = true;
        }
        else
        {
            InitMpay(title, mpayPath, initFinishAction, uniSdkUrl);
        }
    }

    [OriginalMethod]
    public void ProcessLogout() { }

    [HookMethod("WPFLauncher.Manager.arf", "j", "ProcessLogout")]
    public void ProcessLogoutHook()
    {
        WpfConfig.DefaultLogger.Info("执行注销...");
        ProcessLogout();

        if (WpfConfig.MpayUnless)
        {
            string sauthJson = AuthIntegrationService.RequestUserLogin(allowOriginal: false);
            if (!string.IsNullOrEmpty(sauthJson))
            {
                AuthIntegrationService.InjectMpayCookie(sauthJson);
            }
            azf<apm>.Instance.CanChannelLogin = true;
        }
    }

    [HookMethod("WPFLauncher.Manager.PCChannel.asx", "a")]
    public bool IsNeteaseChannel() => true;

    [OriginalMethod]
    public string InitChannel()
    {
        return "";
    }
    
    // 解决启动Java版游戏崩溃问题
    [HookMethod("WPFLauncher.Manager.arf", "b")]
    public string InitChannelHook()
    {
        if (WpfConfig.MpayUnless)
        {
            return "netease";
        }

        return InitChannel();
    }

    [HookMethod("WPFLauncher.Manager.arf", "i")]
    public bool CanLogin() => true;
}