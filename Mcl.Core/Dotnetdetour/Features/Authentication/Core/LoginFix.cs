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

using Mcl.Core.Dotnetdetour.Utilities.Diagnostics;
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

        if (WpfConfig.EnableAlternativeAccountLogin && !WpfConfig.UseAccountManagerLogin && !WpfConfig.CookieLoginWithoutMpay)
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
        if (WpfConfig.EnableWebServer)
        {
            var wsPayload = JsonConvert.SerializeObject(new { type = "Login", cookie = new { sauth_json = sauthJson } });
            WebSocketHelper.SendToClient(wsPayload);
        }

        // WpfConfig.DefaultLogger.Debug($"最终登录 SauthJson: {sauthJson}");
        if (WpfConfig.LogSensitiveAccountDetails)
        {
            PluginLog.Debug("Auth", "SauthJson: " + JsonConvert.SerializeObject(new { sauth_json = sauthJson }));
        }
        WpfConfig.IsLoggedIn = true;
        
        LoginOtp(sauthJson, callAction);
    }
}