using System;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using Mcl.Core.Dotnetdetour.Features.Authentication.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Mcl.Core.Dotnetdetour.Models.Config;
using Mcl.Core.Dotnetdetour.UI.Forms;
using Mcl.Core.Dotnetdetour.Utilities.Common;
using WPFLauncher.Common;
using WPFLauncher.Manager;
using WPFLauncher.Manager.Configuration;
using WPFLauncher.Util;

namespace Mcl.Core.Dotnetdetour.Features.Authentication.Providers;

public static class AuthIntegrationService
{
    private static readonly Random _random = new();

    /// <summary>
    /// 统一处理登录界面的呼出和后续逻辑
    /// 返回值: sauth_json (如果用户选择原版登录，则返回 null)
    /// </summary>
    public static string RequestUserLogin(bool allowOriginal = true)
    {
        while (true)
        {
            using var accountForm = new AccountSelectionForm();
            if (accountForm.ShowDialog() != DialogResult.OK)
            {
                Environment.Exit(0);
                return null;
            }

            if (accountForm.UseOriginalLogin)
            {
                if (!allowOriginal)
                {
                    uz.n("当前模式不可使用原号登录，请重新选择");
                    continue;
                }
                return null;
            }

            if (accountForm.SelectedAccount != null)
            {
                string sauthJson = ExtractSauth(accountForm.SelectedAccount);
                if (string.IsNullOrEmpty(sauthJson))
                {
                    WpfConfig.DefaultLogger.Error("账号凭证提取失败，请重试");
                    continue;
                }
                return sauthJson;
            }
        }
    }

    /// <summary>
    /// 根据不同账号类型解析出终极 SauthJson
    /// </summary>
    public static string ExtractSauth(AccountInfo acc)
    {
        try
        {
            return acc.Type switch
            {
                AccountType.Cookie => SauthParser.ExtractFromCookie(acc.CookieData) ?? acc.CookieData,
                AccountType.Phone => SauthParser.ExtractFromPhoneAccount(acc),
                AccountType._4399 => Parse4399Account(acc),
                _ => null
            };
        }
        catch (Exception ex)
        {
            WpfConfig.DefaultLogger.Error($"凭证提取异常: {ex}");
            return null;
        }
    }

    private static string Parse4399Account(AccountInfo acc)
    {
        var rawResult = SauthParser.ExtractFrom4399Login($"{acc.Username}----{acc.Password}");
        if (string.IsNullOrEmpty(rawResult)) return null;

        try
        {
            // 尝试提取嵌套的 sauth_json
            var jsonObj = JObject.Parse(rawResult);
            if (jsonObj["sauth_json"] != null)
                return jsonObj["sauth_json"].ToString();
        }
        catch { /* ignore parse error, return raw */ }
        
        return rawResult;
    }

    /// <summary>
    /// 统一执行反射注入 MPay Cookie 逻辑
    /// </summary>
    public static void InjectMpayCookie(string sauthContent)
    {
        try
        {
            azf<apm>.Instance.CanChannelLogin = true;
            object arfInstance = azf<arf>.Instance;
            var arfType = typeof(arf);

            arfType.GetField("i", BindingFlags.Public | BindingFlags.Instance)?.SetValue(arfInstance, true);
            
            azf<axi>.Instance.App.UDID = GenerateRandomString();
            azf<axi>.Instance.App.DeviceId = GenerateRandomString();

            var fieldD = arfType.GetField("d", BindingFlags.Public | BindingFlags.Instance);
            if (fieldD != null)
            {
                fieldD.SetValue(arfInstance, sauthContent);
                WpfConfig.DefaultLogger.Info("MPay 状态与 Cookie 注入成功");
            }

            WpfConfig.CookieLoginWithoutMpay = true;
            WpfConfig.IsLogin = true;
            azf<apm>.Instance.h();
        }
        catch (Exception ex)
        {
            WpfConfig.DefaultLogger.Error($"Cookie注入失败: {ex}");
            throw;
        }
    }

    private static string GenerateRandomString(int length = 32)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var result = new StringBuilder(length);
        for (int i = 0; i < length; i++) result.Append(chars[_random.Next(chars.Length)]);
        return result.ToString();
    }
}