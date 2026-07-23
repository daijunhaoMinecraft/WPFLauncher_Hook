using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Mcl.Core.Dotnetdetour.Features.Authentication.Core;
using Mcl.Core.Dotnetdetour.Features.Authentication.Providers;
using Mcl.Core.Dotnetdetour.Models.Config;
using Newtonsoft.Json.Linq;

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

                WpfConfig.DefaultLogger.Info("4399:" + username);
                return JObject.Parse(loginResult.SauthJson)["sauth_json"].ToString();
            }
            catch (Exception ex)
            {
                WpfConfig.DefaultLogger.Error($"4399账号转换失败: \n{ex}");
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
                        WpfConfig.DefaultLogger.Info($"[Phone] 使用缓存凭证 {account.PhoneNumber}");
                }
                catch
                {
                    sauthContent = null;
                }
            }

            if (!string.IsNullOrEmpty(sauthContent))
                return sauthContent;

            if (logInfo)
                WpfConfig.DefaultLogger.Info($"[Phone] 开始手机号登录: {account.PhoneNumber}");

            string result = MpayPhoneLogin.FullLoginFlow(account.PhoneNumber, account.DeviceId);
            if (string.IsNullOrEmpty(result))
                return null;

            account.CookieData = result;
            account.DeviceId = MpayPhoneLogin.GetOrRegisterDevice(account.DeviceId);
            AccountManager.Update(account.Name, account);

            try { return JObject.Parse(result)["sauth_json"].ToString(); }
            catch { return result; }
        }
    }
}
