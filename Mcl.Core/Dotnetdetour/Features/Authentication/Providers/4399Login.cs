using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Mcl.Core.Dotnetdetour.Models.Config;
using Mcl.Core.Dotnetdetour.UI.Forms;

namespace Mcl.Core.Dotnetdetour.Features.Authentication.Providers;

internal class _4399
{
    private const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/129.0.0.0 Safari/537.36";
    private const string OAuth2BaseUrl = "https://ptlogin.4399.com/oauth2/";
    private const string RedirectUri = "https://m.4399api.com/openapi/oauth-callback.html?gamekey=44770&game_key=115716";
    private const string SdkVersion = "3.12.2.503";

    public static int OcrMaxAttempts { get; set; } = 5;

    [DllImport("ocr.dll")]
    private static extern string ocr4399(string imageBase64);

    public static async Task<LoginResult> LoginAsync(string username, string password)
    {
        int ocrAttemptCount = 0;

        while (true)
        {
            try
            {
                var captchaId = Guid.NewGuid().ToString("N").ToUpper();
                var captchaResult = await GetAndSolveCaptchaAsync(captchaId, ocrAttemptCount);
                
                if (captchaResult == null || string.IsNullOrEmpty(captchaResult.Text))
                    return LoginResult.Fail("验证码输入取消");

                if (captchaResult.UsedOcr) ocrAttemptCount++;

                var oauthParams = await ParamsAsync();
                var loginUrl = $"{OAuth2BaseUrl}loginAndAuthorize.do?channel=&sdk=op&sdk_version={SdkVersion}";
                var form = BuildLoginForm(username, password, captchaResult.Text, captchaResult.CaptchaId, oauthParams);

                using var client = CreateHttpClient();
                var httpResponse = await client.PostAsync(loginUrl, new FormUrlEncodedContent(form));
                var responseBody = await httpResponse.Content.ReadAsStringAsync();

                if (string.IsNullOrEmpty(responseBody))
                {
                    var location = httpResponse.Headers.Location?.ToString();
                    if (string.IsNullOrEmpty(location)) return LoginResult.Fail("登录成功但无重定向地址");

                    var oauthData = await GetOAuthCallbackAsync(location);
                    if (oauthData == null) return LoginResult.Fail("OAuth 回调解析失败");

                    var sauthJson = BuildSauthJson(oauthData.Uid, oauthData.State);
                    
                    // 发送给 uni_sauth 注册
                    using var sauthClient = CreateHttpClient();
                    var content = new StringContent(sauthJson, Encoding.UTF8, "application/json");
                    await sauthClient.PostAsync("https://mgbsdk.matrix.netease.com/x19/sdk/uni_sauth", content);

                    var result = new JObject { ["sauth_json"] = sauthJson };
                    return LoginResult.Ok(result.ToString(Formatting.None));
                }

                if (responseBody.Contains("验证码错误")) continue; // 重新尝试

                var errMsg = Regex.Match(responseBody, @"id=""login_err_msg""\s*>\s*([^<]+)\s*<").Groups[1].Value.Trim();
                return LoginResult.Fail(!string.IsNullOrEmpty(errMsg) ? errMsg : "未知登录错误");
            }
            catch (Exception ex)
            {
                WpfConfig.DefaultLogger.Error($"[4399] 登录异常: {ex.Message}");
                return LoginResult.Fail(ex.Message);
            }
        }
    }

    private static Task<CaptchaSolveResult> GetAndSolveCaptchaAsync(string initialCaptchaId, int ocrAttemptCount)
    {
        var base64 = DownloadCaptchaImageSync(initialCaptchaId);
        if (string.IsNullOrEmpty(base64)) return Task.FromResult<CaptchaSolveResult>(null);

        string currentCaptchaId = initialCaptchaId;
        if (ocrAttemptCount < OcrMaxAttempts)
        {
            try
            {
                var ocrText = ocr4399(base64)?.Trim();
                if (!string.IsNullOrEmpty(ocrText))
                    return Task.FromResult(new CaptchaSolveResult(ocrText, currentCaptchaId, true));
            }
            catch { /* Ignore OCR fail */ }
        }

        // 使用重构后的 Captcha UI
        string manualText = CaptchaHelper.GetOcrWithRefresh(base64, () =>
        {
            currentCaptchaId = Guid.NewGuid().ToString("N").ToUpper();
            return DownloadCaptchaImageSync(currentCaptchaId);
        });

        if (string.IsNullOrEmpty(manualText)) return Task.FromResult<CaptchaSolveResult>(null);
        return Task.FromResult(new CaptchaSolveResult(manualText, currentCaptchaId, false));
    }

    private static string DownloadCaptchaImageSync(string captchaId)
    {
        try
        {
            using var wc = new WebClient();
            wc.Headers.Add("User-Agent", UserAgent);
            return Convert.ToBase64String(wc.DownloadData($"https://ptlogin.4399.com/ptlogin/captcha.do?captchaId={captchaId}"));
        }
        catch { return null; }
    }

    private static async Task<OAuthParams> ParamsAsync()
    {
        using var client = CreateHttpClient();
        var json = await client.GetStringAsync(RedirectUri);
        var oauthUrl = JObject.Parse(json)["result"]?.ToString();
        var q = HttpUtility.ParseQueryString(new Uri(oauthUrl).Query);
        
        return new OAuthParams
        {
            ClientId = q["client_id"] ?? "", State = q["state"] ?? "",
            RedirectUri = q["redirect_uri"] ?? "", D = q["_d"] ?? "",
            BizId = q["bizId"] ?? "", Ref = q["ref"] ?? ""
        };
    }

    private static async Task<OAuthCallbackData> GetOAuthCallbackAsync(string locationUrl)
    {
        using var client = CreateHttpClient();
        var json = await client.GetStringAsync(locationUrl);
        var result = JObject.Parse(json)["result"];
        
        return result != null ? new OAuthCallbackData { Uid = result["uid"]?.ToString(), State = result["state"]?.ToString() } : null;
    }

    private static Dictionary<string, string> BuildLoginForm(string username, string password, string captcha, string captchaId, OAuthParams p)
    {
        return new Dictionary<string, string>
        {
            ["auth_action"] = "ORILOGIN", ["bizId"] = "2100001792",
            ["captcha"] = captcha ?? "", ["captcha_id"] = captchaId ?? "",
            ["client_id"] = p.ClientId, ["isInputRealname"] = "false",
            ["isVaildRealname"] = "false", ["password"] = password,
            ["redirect_uri"] = RedirectUri, ["ref"] = p.Ref,
            ["response_type"] = "TOKEN", ["scope"] = "basic",
            ["sec"] = "0", ["state"] = p.State, ["username"] = username
        };
    }

    private static string BuildSauthJson(string uid, string state)
    {
        var sauth = new JObject
        {
            ["aim_info"] = "{\"aim\":\"127.0.0.1\",\"country\":\"CN\",\"tz\":\"+0800\",\"tzid\":\"\"}",
            ["realname"] = "{\"realname_type\":2}", ["app_channel"] = "4399com",
            ["platform"] = "ad", ["client_login_sn"] = "4399_Gen",
            ["gameid"] = "x19", ["login_channel"] = "4399com",
            ["sdk_version"] = "3.12.2", ["sdkuid"] = uid,
            ["sessionid"] = state, ["udid"] = Guid.NewGuid().ToString("N").Substring(0, 16),
            ["deviceid"] = "4399_Gen"
        };
        return sauth.ToString(Formatting.None);
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("User-Agent", UserAgent);
        return client;
    }

    private class CaptchaSolveResult
    {
        public string Text { get; }
        public string CaptchaId { get; }
        public bool UsedOcr { get; }
        public CaptchaSolveResult(string text, string id, bool ocr) { Text = text; CaptchaId = id; UsedOcr = ocr; }
    }

    private class OAuthParams { public string BizId, ClientId, D, RedirectUri, Ref, State; }
    private class OAuthCallbackData { public string State, Uid; }

    public class LoginResult
    {
        public bool Success { get; private set; }
        public string ErrorMessage { get; private set; }
        public string SauthJson { get; private set; }
        public static LoginResult Ok(string sauthJson) => new() { Success = true, SauthJson = sauthJson };
        public static LoginResult Fail(string error) => new() { ErrorMessage = error };
    }
}