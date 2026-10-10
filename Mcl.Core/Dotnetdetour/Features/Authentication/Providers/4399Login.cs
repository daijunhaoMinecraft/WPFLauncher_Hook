using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Mcl.Core.Dotnetdetour.Models.Config;
using Mcl.Core.Dotnetdetour.UI.Forms;
using Mcl.Core.Dotnetdetour.Utilities.Network;

using Mcl.Core.Dotnetdetour.Utilities.Diagnostics;
namespace Mcl.Core.Dotnetdetour.Features.Authentication.Providers;

internal class _4399
{
    // Keep every 4399 path (browser requests, captcha, uni_sauth) on the same Chrome profile.
    private const string UserAgent = CefBrowserProfile.UserAgent;
    private const string OAuth2BaseUrl = "https://ptlogin.4399.com/oauth2/";
    private const string RedirectUri = "https://m.4399api.com/openapi/oauth-callback.html?gamekey=44770&game_key=115716";
    private const string SdkVersion = "3.18.0.682";

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
                if (oauthParams == null || string.IsNullOrEmpty(oauthParams.OauthUrl))
                    return LoginResult.Fail("获取 4399 OAuth 参数失败");

                var loginUrl = $"{OAuth2BaseUrl}loginAndAuthorize.do?channel=&sdk=op&sdk_version={SdkVersion}";
                var form = BuildLoginForm(username, password, captchaResult.Text, captchaResult.CaptchaId, oauthParams);

                PluginLog.Info("Auth", $"[4399] Starting browser login flow for '{username}'");

                // Prime the browser session. Cookies are stored in the shared browser request
                // context and automatically reused by the login POST below.
                var primeResponse = await CefBrowserRequest.GetAsync(
                    oauthParams.OauthUrl, CefBrowserProfile.Navigation(RedirectUri));
                if (primeResponse.Failed)
                {
                    PluginLog.Error("Auth", $"[4399] Browser session priming failed: {primeResponse.ErrorMessage}");
                    return LoginResult.Fail($"4399 会话初始化失败: {primeResponse.ErrorMessage}");
                }
                PluginLog.Debug("Auth", $"[4399] Browser session primed, http={primeResponse.StatusCode}");

                CefBrowserResponse loginResponse = null;
                const int maxRetries = 20;

                for (int attempt = 0; attempt < maxRetries; attempt++)
                {
                    loginResponse = await PostLoginViaHttpClientAsync(loginUrl, form, "https://ptlogin.4399.com/");

                    if (loginResponse.Failed)
                    {
                        PluginLog.Error("Auth", $"[4399] Browser login request failed: {loginResponse.ErrorMessage}");
                        return LoginResult.Fail($"4399 登录请求失败: {loginResponse.ErrorMessage}");
                    }

                    if (loginResponse.StatusCode == (int)HttpStatusCode.Accepted)
                    {
                        PluginLog.Warn("Auth", $"[4399] Server busy (HTTP 202), retry {attempt + 1}/{maxRetries}");
                        if (attempt < maxRetries - 1) await Task.Delay(1000);
                    }
                    else
                    {
                        break;
                    }
                }

                if (loginResponse.StatusCode == (int)HttpStatusCode.Accepted)
                    PluginLog.Error("Auth", $"[4399] Server still busy after {maxRetries} retries, giving up");

                var responseBody = loginResponse.Body;

                if (string.IsNullOrEmpty(responseBody))
                {
                    var location = loginResponse.Location;
                    if (string.IsNullOrEmpty(location)) return LoginResult.Fail("登录成功但无重定向地址");

                    var oauthData = await GetOAuthCallbackAsync(location);
                    if (oauthData == null) return LoginResult.Fail("OAuth 回调解析失败");

                    var sauthJson = BuildSauthJson(oauthData.Uid, oauthData.State);

                    // uni_sauth registration intentionally stays on the managed HTTP client.
                    using var sauthClient = CreateHttpClient();
                    var content = new StringContent(sauthJson, Encoding.UTF8, "application/json");
                    var sauthResponse = await sauthClient.PostAsync("https://mgbsdk.matrix.netease.com/x19/sdk/uni_sauth", content);

                    var sauthResponseBody = await sauthResponse.Content.ReadAsStringAsync();
                    PluginLog.Info("Auth",
                        $"[4399] uni_sauth status: {(int)sauthResponse.StatusCode} {sauthResponse.StatusCode}, body length={sauthResponseBody?.Length ?? 0}");

                    // 尝试解析 JSON 响应体，检查业务 403
                    JObject sauthObj = null;
                    try
                    {
                        sauthObj = JObject.Parse(sauthResponseBody);
                    }
                    catch
                    {
                        // 非 JSON 响应，走 HTTP 状态码判断
                    }

                    if (sauthObj != null && (int?)sauthObj["code"] == 403)
                    {
                        var subcode = (int?)sauthObj["subcode"] ?? 0;
                        var status = (string)sauthObj["status"] ?? "";
                        var rawMsg = (string)sauthObj["msg"] ?? "";
                        var popup = (int?)sauthObj["popup"] ?? 0;

                        // 清理 <ntsdk ...>...</ntsdk> 标签，仅保留纯文本
                        var cleanMsg = Regex.Replace(rawMsg, @"<ntsdk[^>]*>.*?</ntsdk>", "", RegexOptions.Singleline).Trim();
                        if (string.IsNullOrEmpty(cleanMsg)) cleanMsg = rawMsg;

                        PluginLog.Error("Auth",
                            $"[4399] uni_sauth business 403: subcode={subcode}, status={status}, popup={popup}, msg={cleanMsg}");

                        // 弹窗提示（若项目使用其他 UI 框架，请替换为对应调用）
                        if (popup == 1 || true) // popup 字段为 1 时必弹；此处统一弹窗
                        {
                            System.Windows.MessageBox.Show(
                                cleanMsg,
                                "4399 安全验证",
                                System.Windows.MessageBoxButton.OK,
                                System.Windows.MessageBoxImage.Warning);
                        }

                        return LoginResult.Fail($"uni_sauth 403: {cleanMsg}");
                    }

                    // HTTP 状态码兜底
                    if (!sauthResponse.IsSuccessStatusCode)
                    {
                        PluginLog.Error("Auth",
                            $"[4399] uni_sauth request failed: {(int)sauthResponse.StatusCode} {sauthResponseBody}");
                        return LoginResult.Fail($"uni_sauth 请求失败 ({(int)sauthResponse.StatusCode}): {sauthResponseBody}");
                    }

                    var result = new JObject { ["sauth_json"] = sauthJson };
                    return LoginResult.Ok(result.ToString(Formatting.None));
                }

                if (responseBody.Contains("验证码错误"))
                {
                    PluginLog.Info("Auth", "[4399] Captcha rejected by server, restarting login attempt");
                    continue; // 重新尝试
                }

                var errMsg = Regex.Match(responseBody, @"id=""login_err_msg""\s*>\s*([^<]+)\s*<").Groups[1].Value.Trim();
                PluginLog.Warn("Auth", $"[4399] Login rejected: {(string.IsNullOrEmpty(errMsg) ? "unknown error" : errMsg)}");
                return LoginResult.Fail(!string.IsNullOrEmpty(errMsg) ? errMsg : "未知登录错误");
            }
            catch (Exception ex)
            {
                PluginLog.Error("Auth", $"[4399] Login exception: {ex.Message}");
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
            // Keep the captcha on the same browser session as the rest of the 4399 flow so
            // session cookies set before/after it stay consistent.
            var url = $"https://ptlogin.4399.com/ptlogin/captcha.do?captchaId={captchaId}";
            var response = CefBrowserRequest
                .GetAsync(url, CefBrowserProfile.Image("https://ptlogin.4399.com/"))
                .GetAwaiter().GetResult();

            if (response.Failed || response.RawBytes == null || response.RawBytes.Length == 0)
            {
                PluginLog.Error("Auth",
                    $"[4399] Captcha image request failed: {(response.Failed ? response.ErrorMessage : "empty body")}");
                return null;
            }

            PluginLog.Debug("Auth", $"[4399] Captcha image downloaded ({response.RawBytes.Length} bytes)");
            return Convert.ToBase64String(response.RawBytes);
        }
        catch (Exception ex)
        {
            PluginLog.Error("Auth", $"[4399] Captcha image download failed: {ex.Message}");
            return null;
        }
    }

    private static async Task<OAuthParams> ParamsAsync()
    {
        var response = await CefBrowserRequest.GetAsync(RedirectUri, CefBrowserProfile.Navigation());
        if (response.Failed || string.IsNullOrEmpty(response.Body))
        {
            PluginLog.Error("Auth",
                $"[4399] OAuth params request failed: {(response.Failed ? response.ErrorMessage : "empty body")}");
            return null;
        }

        var oauthUrl = JObject.Parse(response.Body)["result"]?.ToString();
        if (string.IsNullOrEmpty(oauthUrl))
        {
            PluginLog.Error("Auth", "[4399] OAuth params response did not contain a result url");
            return null;
        }

        var query = HttpUtility.ParseQueryString(new Uri(oauthUrl).Query);
        PluginLog.Debug("Auth", "[4399] OAuth params resolved");

        return new OAuthParams
        {
            ClientId = query["client_id"] ?? "", State = query["state"] ?? "",
            RedirectUri = query["redirect_uri"] ?? "", D = query["_d"] ?? "",
            BizId = query["bizId"] ?? "", Ref = query["ref"] ?? "", OauthUrl = oauthUrl
        };
    }

    private static async Task<OAuthCallbackData> GetOAuthCallbackAsync(string locationUrl)
    {
        var response = await CefBrowserRequest.GetAsync(locationUrl,
            CefBrowserProfile.Navigation("https://ptlogin.4399.com/"));
        if (response.Failed || string.IsNullOrEmpty(response.Body))
        {
            PluginLog.Error("Auth",
                $"[4399] OAuth callback request failed: {(response.Failed ? response.ErrorMessage : "empty body")}");
            return null;
        }

        var result = JObject.Parse(response.Body)["result"];
        if (result == null)
        {
            PluginLog.Error("Auth", "[4399] OAuth callback response did not contain a result");
            return null;
        }

        PluginLog.Debug("Auth", "[4399] OAuth callback resolved");
        return new OAuthCallbackData { Uid = result["uid"]?.ToString(), State = result["state"]?.ToString() };
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

    /// <summary>
    /// Sends the login POST through a managed HttpClient while copying the browser's full header
    /// set and the cookies held by the shared browser session, so the request still looks like it
    /// came from the embedded Chrome.
    /// </summary>
    private static async Task<CefBrowserResponse> PostLoginViaHttpClientAsync(string loginUrl,
        Dictionary<string, string> form, string referer)
    {
        var result = new CefBrowserResponse { Body = string.Empty };
        try
        {
            var cookieHeader = await CefBrowserRequest.GetCookieHeaderAsync("https://ptlogin.4399.com/");

            using var handler = new HttpClientHandler
            {
                AllowAutoRedirect = false,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
            using var request = new HttpRequestMessage(HttpMethod.Post, loginUrl)
            {
                Content = new FormUrlEncodedContent(form)
            };

            foreach (var header in CefBrowserProfile.LoginHeaders("https://ptlogin.4399.com", referer))
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            if (!string.IsNullOrEmpty(cookieHeader))
                request.Headers.TryAddWithoutValidation("Cookie", cookieHeader);

            PluginLog.Debug("Auth", $"[4399] Login POST via HttpClient (cookie length={cookieHeader?.Length ?? 0})");

            using var response = await client.SendAsync(request);
            result.StatusCode = (int)response.StatusCode;
            result.Body = await response.Content.ReadAsStringAsync();
            result.Headers = new NameValueCollection();
            if (response.Headers.Location != null)
                result.Headers["Location"] = response.Headers.Location.ToString();
        }
        catch (Exception ex)
        {
            result.Failed = true;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    private class CaptchaSolveResult
    {
        public string Text { get; }
        public string CaptchaId { get; }
        public bool UsedOcr { get; }
        public CaptchaSolveResult(string text, string id, bool ocr) { Text = text; CaptchaId = id; UsedOcr = ocr; }
    }

    private class OAuthParams
    {
        public string BizId, ClientId, D, RedirectUri, Ref, State, OauthUrl;
    }

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