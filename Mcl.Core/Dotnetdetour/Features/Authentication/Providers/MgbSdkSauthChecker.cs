using System;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

using Mcl.Core.Dotnetdetour.Utilities.Diagnostics;
namespace Mcl.Core.Dotnetdetour.Features.Authentication.Providers;

/// <summary>
/// 登录前把 sauth_json 直接交给 mgbsdk 的 uni_sauth 预检一次。
/// 核心服务器 /login-otp 校验失败时只回笼统的“服务器繁忙”，风控详情
/// （如“请使用任意手机号发送 JFxxxx 至 1069…”）只在这里的响应里才有。
/// </summary>
internal static class MgbSdkSauthChecker
{
    private const string UniSauthUrl = "https://mgbsdk.matrix.netease.com/x19/sdk/uni_sauth";
    private const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/129.0.0.0 Safari/537.36";

    // 响应里带的 <ntsdk action="outlink" href="sms:..." ...>立即发送</ntsdk> 富文本标签，弹窗里无法渲染
    private static readonly Regex NtsdkTagRegex = new(@"<ntsdk\b[^>]*>.*?</ntsdk>|<ntsdk\b[^>]*/?>", RegexOptions.Singleline | RegexOptions.Compiled);

    /// <summary>
    /// 返回 true 表示 mgbsdk 接受（code=200）或响应无法解析（不阻断登录）；
    /// 返回 false 时 <paramref name="error"/> 为可直接展示给用户的失败原因。
    /// </summary>
    public static bool 
        Check(string sauthJson, out string error)
    {
        var (ok, err) = Task.Run(() => CheckAsync(sauthJson)).Result;
        error = err;
        return ok;
    }

    public static async Task<(bool ok, string error)> CheckAsync(string sauthJson)
    {
        if (string.IsNullOrWhiteSpace(sauthJson)) return (true, null);

        string body;
        try
        {
            body = await PostAsync(sauthJson);
        }
        catch (Exception ex)
        {
            // 网络问题不在这里定性，交给启动器原有流程报错
            PluginLog.Error("Auth", $"uni_sauth 预检请求失败: {ex.GetBaseException().Message}");
            return (true, null);
        }

        return Evaluate(body, out var error) ? (true, null) : (false, error);
    }

    public static bool Evaluate(string responseBody, out string error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(responseBody)) return true;

        JObject json;
        try { json = JObject.Parse(responseBody); }
        catch { return true; }

        int code = json.Value<int?>("code") ?? 200;
        if (code == 200) return true;

        var msg = CleanMessage(json.Value<string>("msg"));
        var status = json.Value<string>("status");
        var subcode = json.Value<int?>("subcode");
        var upstream = DecodeDebugMessage(json.Value<string>("debug_message"));

        if (!string.IsNullOrEmpty(msg))
        {
            error = msg;
        }
        else if (upstream != null)
        {
            // 401/auth fail 时 msg 为空，真正原因在 base64 的 debug_message 里（渠道方原始回包，如 4399 的 10204 验证失败）
            error = $"渠道校验失败: {upstream.Value.message} (渠道 code={upstream.Value.code})\n"
                    + "凭证已失效或已被使用，请重新登录获取。";
        }
        else
        {
            error = $"登录校验失败 (code={code}{(subcode.HasValue ? $", subcode={subcode}" : "")}{(string.IsNullOrEmpty(status) ? "" : $", {status}")})";
        }

        PluginLog.Error("Auth", $"uni_sauth 拒绝登录: code={code} subcode={subcode} status={status} msg={msg} upstream={upstream?.code}/{upstream?.message}");
        return false;
    }

    /// <summary>debug_message 是 base64 编码的渠道方 JSON：{"code":10204,"result":[],"message":"验证失败"}。</summary>
    private static (int code, string message)? DecodeDebugMessage(string debugMessage)
    {
        if (string.IsNullOrWhiteSpace(debugMessage)) return null;
        try
        {
            var decoded = JObject.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(debugMessage)));
            var message = decoded.Value<string>("message");
            if (string.IsNullOrEmpty(message)) return null;
            return (decoded.Value<int?>("code") ?? 0, message);
        }
        catch
        {
            return null;
        }
    }

    public static string CleanMessage(string msg)
    {
        if (string.IsNullOrEmpty(msg)) return msg;
        return NtsdkTagRegex.Replace(msg, "").Trim();
    }

    private static async Task<string> PostAsync(string sauthJson)
    {
        using var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("User-Agent", UserAgent);
        client.Timeout = TimeSpan.FromSeconds(15);

        var content = new StringContent(sauthJson, Encoding.UTF8, "application/json");
        var response = await client.PostAsync(UniSauthUrl, content);
        return await response.Content.ReadAsStringAsync();
    }
}
