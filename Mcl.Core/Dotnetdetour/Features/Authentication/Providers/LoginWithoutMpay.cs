using System;
using System.IO;
using System.Reflection;
using System.Text;
using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;
using Mcl.Core.Dotnetdetour.Features.Authentication.Core;
using Mcl.Core.Dotnetdetour.Features.GeneralHooks;
using Mcl.Core.Dotnetdetour.Models.Config;
using Mcl.Core.Dotnetdetour.Utilities.Common;
using Mcl.Core.Dotnetdetour.Utilities.Network;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using WPFLauncher.Common;
using WPFLauncher.Manager;
using WPFLauncher.Manager.Configuration;
using WPFLauncher.Sdk.MPay;
using WPFLauncher.Util;

namespace Mcl.Core.Dotnetdetour.Features.Authentication.Providers;

public class LoginWithoutMpay : IMethodHook
{
    private static readonly Random random = new();
    private static readonly char[] chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();

    /// <summary>
    ///     生成指定长度的随机数字+大写字母字符串
    /// </summary>
    /// <param name="length">字符串长度，默认32</param>
    /// <returns>随机字符串</returns>
    public static string GenerateRandomString(int length = 32)
    {
        var result = new StringBuilder(length);
        for (var i = 0; i < length; i++) result.Append(chars[random.Next(chars.Length)]);
        return result.ToString();
    }

    private static void InjectMpayCookie(string sauthContent)
    {
        try
        {
            // Bypass
            azf<apm>.Instance.CanChannelLogin = true;
            // 反
            object arfInstance = azf<arf>.Instance;

            // 安全地通过反射修改字段 i
            var arfType = typeof(arf);
            var fieldI = arfType.GetField("i", BindingFlags.Public | BindingFlags.Instance);
            if (fieldI != null)
            {
                fieldI.SetValue(arfInstance, true);
                WpfConfig.DefaultLogger.Info("设置bool成功");
            }

            // 构建 SauthJsonEntity 并注入
            SauthJsonEntity sauthJsonEntity;
            try
            {
                // 尝试从 sauthContent 反序列化为 SauthJsonEntity
                sauthJsonEntity = JsonConvert.DeserializeObject<SauthJsonEntity>(sauthContent);
            }
            catch
            {
                // 如果反序列化失败，创建新的并设置基本字段
                sauthJsonEntity = new SauthJsonEntity();
            }

            var randomUDID = GenerateRandomString();
            var randomDeviceId = GenerateRandomString();
            azf<axi>.Instance.App.UDID = randomUDID;
            azf<axi>.Instance.App.DeviceId = randomDeviceId;

            var fieldD = arfType.GetField("d", BindingFlags.Public | BindingFlags.Instance);
            if (fieldD != null)
            {
                fieldD.SetValue(arfInstance, sauthContent);
                WpfConfig.DefaultLogger.Info("设置Cookie成功");
            }
            else
            {
                // 尝试序列化整个 SauthJsonEntity
                fieldD = arfType.GetField("d", BindingFlags.Public | BindingFlags.Instance);
                if (fieldD != null)
                {
                    fieldD.SetValue(arfInstance, JsonConvert.SerializeObject(sauthJsonEntity));
                    WpfConfig.DefaultLogger.Info("设置Cookie成功(Entity)");
                }
            }

            WpfConfig.CookieLoginWithoutMpay = true;
            WpfConfig.IsLogin = true;
            azf<apm>.Instance.h();
        }
        catch (Exception ex)
        {
            WpfConfig.DefaultLogger.Error($"Cookie注入失败, 使用原号登录: \n{ex}");
        }
    }

    [OriginalMethod]
    public void InitMpay(string title, string mpayPath, Action<int> initFinishAction, string uniSdkUrl)
    {
    }

    [HookMethod("WPFLauncher.Unisdk.nx", "a", "InitMpay")]
    public void InitMpayHook(string title, string mpayPath, Action<int> initFinishAction, string uniSdkUrl)
    {
        if (WpfConfig.MpayUnless)
        {
            initFinishAction(0);
            while (true)
            {
                // 打开多账号管理器
                var accountForm = new AccountSelectForm();
                accountForm.ShowDialog();

                if (accountForm.Action == AccountSelectForm.LoginAction.UseSelected &&
                    accountForm.SelectedAccount != null)
                {
                    LoginWithSaved(accountForm.SelectedAccount);
                    break;
                }

                if (accountForm.Action == AccountSelectForm.LoginAction.ManualInput)
                    DoManualCookieInput();
                else if (accountForm.Action == AccountSelectForm.LoginAction.UseOriginal)
                    uz.n("你不能使用此选项, 请重新选择");
                else if (accountForm.Action == AccountSelectForm.LoginAction.Exit) Environment.Exit(0);
            }

            azf<apm>.Instance.CanChannelLogin = true;
        }
        else
        {
            InitMpay(title, mpayPath, initFinishAction, uniSdkUrl);
        }
    }

    public static void LoginWithSaved(AccountInfo acc)
    {
        var sauthJson = "";

        if (acc.Type == AccountType._4399)
        {
            sauthJson = SauthParser.ExtractFrom4399Login($"{acc.Username}----{acc.Password}");
            if (string.IsNullOrEmpty(sauthJson))
            {
                uz.n("4399登录失败");
                return;
            }
        }
        else if (acc.Type == AccountType.Phone)
        {
            sauthJson = SauthParser.ExtractFromPhoneAccount(acc);
            if (string.IsNullOrEmpty(sauthJson))
            {
                WpfConfig.DefaultLogger.Error("手机号登录失败，切换为原号登录");
                return;
            }
        }
        else // Cookie
        {
            sauthJson = SauthParser.ExtractFromCookie(acc.CookieData);
            WpfConfig.DefaultLogger.Info("cookies:" + acc.CookieData);
        }

        if (string.IsNullOrEmpty(sauthJson))
        {
            WpfConfig.DefaultLogger.Error("账号凭证有误，切换为原号登录");
            return;
        }

        if (WpfConfig.IsStartWebSocket)
            WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new
                { type = "Login", cookie = new { sauth_json = sauthJson } }));
        WpfConfig.DefaultLogger.Info($"SauthJson: {JsonConvert.SerializeObject(new { sauth_json = sauthJson })}");
        InjectMpayCookie(sauthJson);
        WpfConfig.IsLogin = true;
    }

    [HookMethod("WPFLauncher.Manager.PCChannel.asx", "a")]
    public bool IsNeteaseChannel()
    {
        return true;
    }

    [HookMethod("WPFLauncher.Manager.arf", "i")]
    public bool CanLogin()
    {
        return true;
    }

    [OriginalMethod]
    public void ProcessLogout()
    {
    }

    [HookMethod("WPFLauncher.Manager.arf", "j", "ProcessLogout")]
    public void ProcessLogoutHook()
    {
        WpfConfig.DefaultLogger.Info("Logout!");
        ProcessLogout();
        if (WpfConfig.MpayUnless)
        {
            while (true)
            {
                // 打开多账号管理器
                var accountForm = new AccountSelectForm();
                accountForm.ShowDialog();

                if (accountForm.Action == AccountSelectForm.LoginAction.UseSelected &&
                    accountForm.SelectedAccount != null)
                {
                    LoginWithSaved(accountForm.SelectedAccount);
                    break;
                }

                if (accountForm.Action == AccountSelectForm.LoginAction.ManualInput)
                    DoManualCookieInput();
                else if (accountForm.Action == AccountSelectForm.LoginAction.UseOriginal)
                    uz.n("你不能使用此选项, 请重新选择");
                else if (accountForm.Action == AccountSelectForm.LoginAction.Exit) Environment.Exit(0);
            }

            azf<apm>.Instance.CanChannelLogin = true;
        }
    }

    /// <summary>
    ///     旧版手动输入 Cookie 流程（兼容保留）
    /// </summary>
    private void DoManualCookieInput()
    {
        var text3 = "cookies.txt";
        var filePath = Path.Combine(Environment.CurrentDirectory, text3);
        var readCookie = "";
        if (File.Exists(filePath)) readCookie = File.ReadAllText(filePath);
        var cookie = Interaction.InputBox("请输入Cookies" /*\n自动获取cookie请输入1"*/, "Cookies", readCookie);
        WpfConfig.DefaultLogger.Info("cookies:" + cookie);
        File.WriteAllText(filePath, cookie);

        var sauthContent = SauthParser.ExtractFromCookie(cookie);
        InjectMpayCookie(sauthContent);
    }
}
