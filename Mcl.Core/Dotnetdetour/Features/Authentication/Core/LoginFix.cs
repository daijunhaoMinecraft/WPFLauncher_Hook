using System;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;
using Mcl.Core.Dotnetdetour.Features.Authentication.Providers;
using Mcl.Core.Dotnetdetour.Features.GeneralHooks;
using Mcl.Core.Dotnetdetour.Models.Config;
using Mcl.Core.Dotnetdetour.UI.Controls;
using Mcl.Core.Dotnetdetour.Utilities.Common;
using Mcl.Core.Dotnetdetour.Utilities.Network;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WPFLauncher.Code;
using WPFLauncher.Network.Launcher;
using WPFLauncher.Util;

namespace Mcl.Core.Dotnetdetour.Features.Authentication.Core;

// Token: 0x02000017 RID: 23
public class LoginFix : IMethodHook
{
    [OriginalMethod]
    public static void f(string hud, Action<EntityResponse<acl.Resposne>, Exception> hue = null)
    {
    }

    [CompilerGenerated]
    [HookMethod("WPFLauncher.Network.Launcher.acp", "g", "g")]
    public static async Task g(string hud, Action<EntityResponse<acl.Resposne>, Exception> hue)
    {
        if (WpfConfig.EnableCustomAccountLogin && !WpfConfig.MpayUnless)
        {
            MessageBoxResult messageBoxResult;
            if (WpfConfig.CookieLoginWithoutMpay)
                messageBoxResult = MessageBoxResult.Cancel;
            else
                messageBoxResult = uz.q("是否使用Cookie登录?", "", "多账号管理", "使用原号登录");
            if (messageBoxResult == MessageBoxResult.OK)
            {
                var accountForm = new AccountSelectForm();
                accountForm.ShowDialog();

                if (accountForm.Action == AccountSelectForm.LoginAction.UseSelected &&
                    accountForm.SelectedAccount != null)
                    LoginWithSavedAccount(accountForm.SelectedAccount, hud, hue);
                else if (accountForm.Action == AccountSelectForm.LoginAction.ManualInput)
                    DoManualLogin(hud, hue);
                else
                    LoginWithOriginal(hud, hue);
            }
            else if (messageBoxResult == MessageBoxResult.None)
            {
                LoginWithOriginal(hud, hue);
            }
            else
            {
                LoginWithOriginal(hud, hue);
            }
        }
        else
        {
            LoginWithOriginal(hud, hue);
        }
    }

    private static void LoginWithSavedAccount(AccountInfo acc, string hud,
        Action<EntityResponse<acl.Resposne>, Exception> hue)
    {
        var sauthJson = "";

        if (acc.Type == AccountType._4399)
        {
            try
            {
                var raw4399Input = $"{acc.Username}----{acc.Password}";
                sauthJson = SauthParser.ExtractFrom4399Login(raw4399Input);
                if (string.IsNullOrEmpty(sauthJson))
                {
                    LoginWithOriginal(hud, hue);
                    return;
                }
            }
            catch (Exception ex)
            {
                WpfConfig.DefaultLogger.Error($"4399账号转换失败，切换为原号登录: \n{ex}");
                LoginWithOriginal(hud, hue);
                return;
            }
        }
        else if (acc.Type == AccountType.Phone)
        {
            sauthJson = SauthParser.ExtractFromPhoneAccount(acc);
            if (string.IsNullOrEmpty(sauthJson))
            {
                WpfConfig.DefaultLogger.Error("手机号登录失败，切换为原号登录");
                LoginWithOriginal(hud, hue);
                return;
            }
        }
        else // Cookie
        {
            sauthJson = SauthParser.ExtractFromCookie(acc.CookieData);
            if (string.IsNullOrEmpty(sauthJson))
                sauthJson = acc.CookieData;
            WpfConfig.DefaultLogger.Info("cookies:" + acc.CookieData);
        }

        if (string.IsNullOrEmpty(sauthJson))
        {
            WpfConfig.DefaultLogger.Error("账号凭证有误，切换为原号登录");
            LoginWithOriginal(hud, hue);
            return;
        }

        if (WpfConfig.IsStartWebSocket)
            WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new
                { type = "Login", cookie = new { sauth_json = sauthJson } }));
        WpfConfig.DefaultLogger.Info($"SauthJson: {JsonConvert.SerializeObject(new { sauth_json = sauthJson })}");
        WpfConfig.IsLogin = true;
        f(sauthJson, hue);
    }

    private static void LoginWithOriginal(string hud, Action<EntityResponse<acl.Resposne>, Exception> hue)
    {
        if (WpfConfig.IsStartWebSocket)
            WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new
                { type = "Login", cookie = new { sauth_json = hud } }));
        WpfConfig.DefaultLogger.Info($"SauthJson:{JsonConvert.SerializeObject(new { sauth_json = hud })}");
        WpfConfig.IsLogin = true;
        f(hud, hue);
    }

    private static void DoManualLogin(string hud, Action<EntityResponse<acl.Resposne>, Exception> hue)
    {
        var cookieRawInput = "";
        var loginby4399 = false;
        var raw4399Input = "";
        if (uz.r("请选择cookie登录或4399登录", string.Empty, "cookie", "4399") == MessageBoxResult.Yes)
            cookieRawInput = Interaction.InputBox("请输入Cookies \n如果原号登陆请输入off或者空", "Cookies", cookieRawInput);
        else
            try
            {
                raw4399Input = Interaction.InputBox(
                    "请输入4399账号 \n如果原号登陆请输入off或者空\n4399账号格式为 xxxx----xxxx(4个减号) \n例如UserName----UserPassword",
                    "4399LOGIN", raw4399Input);
                loginby4399 = true;
                if (!(raw4399Input == "off") && !(raw4399Input == ""))
                    cookieRawInput = SauthParser.ExtractFrom4399Login(raw4399Input);
            }
            catch (Exception ex)
            {
                WpfConfig.DefaultLogger.Error(ex);
                LoginWithOriginal(hud, hue);
                return;
            }

        var sauthJson = SauthParser.ExtractFromCookie(cookieRawInput);

        if (string.IsNullOrEmpty(sauthJson) && !string.IsNullOrEmpty(cookieRawInput))
        {
            WpfConfig.DefaultLogger.Error("cookies有误");
            if (loginby4399)
                raw4399Input = "";
            else
                cookieRawInput = "";
        }

        if (loginby4399)
        {
            if (!(raw4399Input == "off") && !(raw4399Input == ""))
            {
                WpfConfig.DefaultLogger.Info("4399:" + raw4399Input);
                LoginWithOriginal(sauthJson, hue);
            }
            else
            {
                WpfConfig.DefaultLogger.Info("选择原号登录(原因：玩家填入off或空)");
                LoginWithOriginal(hud, hue);
            }
        }
        else if (!(cookieRawInput == "off") && !(cookieRawInput == ""))
        {
            WpfConfig.DefaultLogger.Info("cookies:" + cookieRawInput);
            LoginWithOriginal(sauthJson, hue);
        }
        else
        {
            WpfConfig.DefaultLogger.Info("选择原号登录(原因：玩家填入off或空)");
            LoginWithOriginal(hud, hue);
        }
    }
}
