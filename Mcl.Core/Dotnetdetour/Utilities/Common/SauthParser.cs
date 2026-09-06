using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Mcl.Core.Dotnetdetour.Features.Authentication.Core;
using Mcl.Core.Dotnetdetour.Features.Authentication.Providers;
using Mcl.Core.Dotnetdetour.Models.Config;
using Newtonsoft.Json.Linq;

using Mcl.Core.Dotnetdetour.Utilities.Diagnostics;
namespace Mcl.Core.Dotnetdetour.Utilities.Common
{
    public static class SauthParser
    {
        private static readonly Regex SauthJsonRegex = new Regex(
            "\\\"sauth_json\\\":\\\"(.*?)\\\"}\\\"}",
            RegexOptions.Compiled);

        public static string ExtractFromCookie(string rawCookieData)
        {
            if (string.IsNullOrEmpty(rawCookieData))
                return null;

            if (!rawCookieData.StartsWith("{"))
                return rawCookieData;

            try
            {
                if (rawCookieData.Contains("\"sauth_json\":"))
                    return JObject.Parse(rawCookieData)["sauth_json"].ToString();
                return rawCookieData;
            }
            catch
            {
                if (rawCookieData.Contains("sauth_json"))
                    return SauthJsonRegex.Match(rawCookieData).Groups[1].Value + "\"}";
                return rawCookieData;
            }
        }

        public static string ExtractFrom4399Login(string raw4399Input)
        {
            if (string.IsNullOrEmpty(raw4399Input) || raw4399Input == "off")
                return null;

            string[] parts = raw4399Input.Split(new[] { "----" }, StringSplitOptions.None);
            if (parts.Length != 2)
                return null;

            string username = parts[0];
            string password = parts[1];

            try
            {
                var loginResult = Task.Run(() =>
                    _4399.LoginAsync(username, password)).Result;

                if (!loginResult.Success)
                    return null;

                PluginLog.Debug("Core", "[4399] 正在使用所选账号登录。");
                return JObject.Parse(loginResult.SauthJson)["sauth_json"].ToString();
            }
            catch (Exception ex)
            {
                PluginLog.Error("Core", $"4399账号转换失败: \n{ex}");
                return null;
            }
        }

        public static string ExtractFromPhoneAccount(AccountInfo account, bool logInfo = true)
        {
            string sauthContent = null;

            if (!string.IsNullOrEmpty(account.CookieData) && account.CookieData.Contains("\"sauth_json\""))
            {
                try
                {
                    sauthContent = JObject.Parse(account.CookieData)["sauth_json"].ToString();
                    if (logInfo)
                        PluginLog.Debug("Core", "[Phone] 正在使用缓存凭证登录。");
                }
                catch
                {
                    sauthContent = null;
                }
            }

            if (!string.IsNullOrEmpty(sauthContent))
                return sauthContent;

            if (logInfo)
                PluginLog.Debug("Core", "[Phone] 开始手机号登录。");

            string result = MpayLogin.FullLoginFlow(account.PhoneNumber, account.DeviceId);
            if (string.IsNullOrEmpty(result))
                return null;

            account.CookieData = result;
            account.DeviceId = MpayLogin.GetOrRegisterDevice(account.DeviceId);
            AccountManager.Update(account.Name, account);

            try { return JObject.Parse(result)["sauth_json"].ToString(); }
            catch { return result; }
        }
    }
}
