using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;
using Mcl.Core.Dotnetdetour.Features.Authentication.Providers;
using Mcl.Core.Dotnetdetour.Models.Config;
using Mcl.Core.Dotnetdetour.Utilities.Network;
using WPFLauncher.Code;
using WPFLauncher.Network.Launcher;
using WPFLauncher.View.UI;

namespace Mcl.Core.Dotnetdetour.Features.Authentication.Core;

public class LoginFix : IMethodHook
{
    [OriginalMethod]
    public static void LoginOtp(string sauthJson, Action<EntityResponse<acl.Resposne>, Exception> callAction = null) { }

    [CompilerGenerated]
    [HookMethod("WPFLauncher.Network.Launcher.acp", "g", "LoginOtp")]
    public static async Task LoginOtpHook(string sauthJson, Action<EntityResponse<acl.Resposne>, Exception> callAction)
    {
        string sauthJsonToUse = sauthJson;

        if (WpfConfig.EnableCustomAccountLogin && !WpfConfig.MpayUnless && !WpfConfig.CookieLoginWithoutMpay)
        {
            string newSauth = AuthIntegrationService.RequestUserLogin(allowOriginal: true);
            if (!string.IsNullOrEmpty(newSauth))
            {
                sauthJsonToUse = newSauth;
            }
        }

        ExecuteFinalLogin(sauthJsonToUse, callAction);
    }

    private static void ExecuteFinalLogin(string sauthJson, Action<EntityResponse<acl.Resposne>, Exception> callAction)
    {
        if (WpfConfig.IsStartWebSocket)
        {
            var wsPayload = JsonConvert.SerializeObject(new { type = "Login", cookie = new { sauth_json = sauthJson } });
            WebSocketHelper.SendToClient(wsPayload);
        }

        // WpfConfig.DefaultLogger.Info($"最终登录 SauthJson: {sauthJson}");
        WpfConfig.DefaultLogger.Debug("SauthJson: " + JsonConvert.SerializeObject(new { sauth_json = sauthJson }));
        WpfConfig.IsLogin = true;
        
        LoginOtp(sauthJson, callAction);
    }
}