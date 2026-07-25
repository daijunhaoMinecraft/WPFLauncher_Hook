using System;
using System.IO;
using System.Net;
using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;
using Mcl.Core.Dotnetdetour.Models.Config;
using WPFLauncher.Common;
using WPFLauncher.Manager;
using WPFLauncher.Manager.PCChannel;

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
    [HookMethod("WPFLauncher.Manager.arf", "b", "InitChannel")]
    public string InitChannelHook()
    {
        if (File.Exists("4399pc.data"))
        {
            Console.WriteLine("选择渠道服: 4399");
            return "4399pc";
        }

        if (File.Exists("native_a50_cn.data"))
        {
            Console.WriteLine("选择渠道服: a50sdk");
            return "a50_sdk_cn";
        }
        Console.WriteLine("选择渠道服: netease");
        return "netease";
    }

    [HookMethod("WPFLauncher.Manager.arf", "i")]
    public bool CanLogin() => true;

    [HookMethod("WPFLauncher.Update.xw", "b", null)]
    public bool ComparePath(string path1, string path2)
    {
        return true;
    }
}