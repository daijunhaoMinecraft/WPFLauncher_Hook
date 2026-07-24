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

public static class CookieValidator
{
    public static bool ValidateSauth(string cookieData, out string error)
    {
        error = "";
        if (string.IsNullOrWhiteSpace(cookieData))
        {
            error = "Cookie 不能为空";
            return false;
        }

        try
        {
            var root = JObject.Parse(cookieData);
            var sauthToken = root["sauth_json"];
            
            if (sauthToken == null)
            {
                error = "JSON 中未找到 'sauth_json' 字段。";
                return false;
            }
            if (sauthToken.Type != JTokenType.String)
            {
                error = "'sauth_json' 的值必须是包含 JSON 结构的字符串类型。";
                return false;
            }

            JObject.Parse(sauthToken.ToString()); 
            return true;
        }
        catch (JsonReaderException)
        {
            error = "格式错误：提供的不是有效的 JSON 字符串。";
            return false;
        }
        catch (Exception ex)
        {
            error = $"校验异常: {ex.Message}";
            return false;
        }
    }
}

public static class AuthIntegrationService
{
    private static readonly Random _random = new();

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

    public static string ExtractSauth(AccountInfo acc)
    {
        try
        {
            return acc.Type switch
            {
                AccountType.Cookie => SauthParser.ExtractFromCookie(acc.CookieData) ?? acc.CookieData,
                AccountType.Phone => SauthParser.ExtractFromPhoneAccount(acc),
                AccountType.Email => MpayLogin.EmailLoginFlow(acc.Username, acc.Password),
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
            var jsonObj = JObject.Parse(rawResult);
            if (jsonObj["sauth_json"] != null) return jsonObj["sauth_json"].ToString();
        }
        catch { /* ignore parse error, return raw */ }
        
        return rawResult;
    }

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