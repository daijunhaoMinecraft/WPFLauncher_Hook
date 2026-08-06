using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Mcl.Core.Dotnetdetour.Models.Config;
using Mcl.Core.Dotnetdetour.UI.Forms;

namespace Mcl.Core.Dotnetdetour.Features.Authentication.Providers;

public static class MpayLogin
{
    private const string MPAY_HOST = "https://service.mkey.163.com";
    private const string PROJECT_ID = "x19";
    private const string CACHE_FILE = "device_cache.json";
    private const string APP_CHANNEL = "netease";
    
    private static readonly HttpClient _client;
    private static string _cachedDeviceId;
    private static string _cachedUniqueId;
    private static string _cachedDeviceKey;

    static MpayLogin()
    {
        _client = new HttpClient(new HttpClientHandler { ServerCertificateCustomValidationCallback = (sender, cert, chain, errors) => true });
        _client.DefaultRequestHeaders.Add("User-Agent", "WPFLauncher/0.0.0.0");
        LoadDeviceCache();
    }

    private static void LoadDeviceCache()
    {
        try
        {
            var path = Path.Combine(Environment.CurrentDirectory, CACHE_FILE);
            if (!File.Exists(path)) return;
            
            var cache = JObject.Parse(File.ReadAllText(path));
            _cachedDeviceId = cache["device_id"]?.ToString();
            _cachedUniqueId = cache["unique_id"]?.ToString();
            _cachedDeviceKey = cache["device_key"]?.ToString();
        }
        catch { /* ignored */ }
    }

    private static void SaveDeviceCache()
    {
        try
        {
            var cache = new JObject
            {
                ["unique_id"] = _cachedUniqueId,
                ["device_id"] = _cachedDeviceId,
                ["device_key"] = _cachedDeviceKey
            };
            File.WriteAllText(Path.Combine(Environment.CurrentDirectory, CACHE_FILE), cache.ToString(Formatting.None));
        }
        catch { /* ignored */ }
    }

    public static string GetOrRegisterDevice(string existingDeviceId = null)
    {
        if (!string.IsNullOrEmpty(existingDeviceId)) return existingDeviceId;
        if (!string.IsNullOrEmpty(_cachedDeviceId) && !string.IsNullOrEmpty(_cachedDeviceKey)) return _cachedDeviceId;

        var uniqueId = Guid.NewGuid().ToString("N");
        var formData = GetBaseParams();
        formData["unique_id"] = uniqueId;
        formData["brand"] = "Microsoft";
        formData["device_model"] = "pc_mode";
        formData["device_name"] = $"PC-{Guid.NewGuid().ToString("N").Substring(0, 6)}";
        formData["device_type"] = "Computer";
        formData["init_urs_device"] = "0";
        formData["mac"] = GenerateMac();
        formData["resolution"] = "1920x1080";
        formData["system_name"] = "windows";
        formData["system_version"] = "10.0.22621";

        try
        {
            var (isSuccess, responseStr) = PostRequest($"/mpay/games/{PROJECT_ID}/devices", formData);

            // 此前失败的原因：网易注册成功可能会返回 201 Created，不能严格限定等于 200
            if (!isSuccess)
            {
                HandleApiError(responseStr, "设备注册");
                return null;
            }

            var json = JObject.Parse(responseStr);
            _cachedDeviceId = json["device"]?["id"]?.ToString();
            _cachedDeviceKey = json["device"]?["key"]?.ToString();
            _cachedUniqueId = uniqueId;

            SaveDeviceCache();
            return _cachedDeviceId;
        }
        catch (Exception ex)
        {
            WpfConfig.DefaultLogger.Error($"设备注册失败: {ex.Message}");
            MessageBox.Show($"设备注册发生异常: {ex.Message}", "设备注册错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return null;
        }
    }

    // ----------------- 网易邮箱登录 ----------------- //
    public static string EmailLoginFlow(string email, string password)
    {
        try
        {
            // 添加空检查。如果注册设备失败，中断流程
            var devId = GetOrRegisterDevice();
            if (string.IsNullOrEmpty(devId) || string.IsNullOrEmpty(_cachedDeviceKey)) return null;

            var loginParams = new JObject
            {
                ["username"] = email,
                ["password"] = Md5Hash(password),
                ["unique_id"] = _cachedUniqueId
            };

            var formData = GetBaseParams();
            formData["opt_fields"] = "nickname,avatar,realname_status,mobile_bind_status,mask_related_mobile,related_login_status";
            formData["params"] = AesEncryptHex(loginParams.ToString(Formatting.None), _cachedDeviceKey);
            formData["un"] = Base64Encode(email);

            var url = $"/mpay/games/{PROJECT_ID}/devices/{_cachedDeviceId}/users";
            var (isSuccess, responseStr) = PostRequest(url, formData);
            
            if (!isSuccess)
            {
                HandleApiError(responseStr, "登录");
                return null;
            }

            var user = JObject.Parse(responseStr)["user"];
            return user == null ? null : GenerateSauthString(user["id"].ToString(), user["token"].ToString(), _cachedDeviceId);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"邮箱登录异常: {ex.Message}", "发生异常", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return null;
        }
    }

    // ----------------- 手机验证码登录 ----------------- //
    public static SmsResult SendSms(string phoneNumber, string deviceId = null)
    {
        var devId = deviceId ?? GetOrRegisterDevice();
        if (string.IsNullOrEmpty(devId)) return new SmsResult { Status = SmsStatus.Failed, ErrorMessage = "设备获取失败，无法发送验证码" };

        var formData = GetBaseParams();
        formData["device_id"] = devId;
        formData["mobile"] = phoneNumber;

        try
        {
            var (isSuccess, responseStr) = PostRequest("/mpay/api/users/login/mobile/get_sms", formData);
            if (isSuccess) return new SmsResult { Status = SmsStatus.Success };

            var json = JObject.Parse(responseStr);
            if (json.Value<int>("code") == 1373)
            {
                return new SmsResult 
                { 
                    Status = SmsStatus.UpstreamRequired, 
                    UpstreamContent = json["reply_sms"]?["content"]?.ToString(), 
                    UpstreamNumber = json["reply_sms"]?["number"]?.ToString() 
                };
            }
            return new SmsResult { Status = SmsStatus.Failed, ErrorMessage = json["reason"]?.ToString() };
        }
        catch (Exception ex) { return new SmsResult { Status = SmsStatus.Failed, ErrorMessage = ex.Message }; }
    }

    public static VerifyResult VerifySms(string phoneNumber, string code = "", string upContent = "", string deviceId = null)
    {
        var devId = deviceId ?? GetOrRegisterDevice();
        if (string.IsNullOrEmpty(devId)) return new VerifyResult { Success = false, ErrorMessage = "设备获取失败" };

        var formData = GetBaseParams();
        formData["device_id"] = devId;
        formData["mobile"] = phoneNumber;
        formData["smscode"] = code ?? "";
        formData["up_content"] = upContent ?? "";

        try
        {
            var (isSuccess, responseStr) = PostRequest("/mpay/api/users/login/mobile/verify_sms", formData);
            var json = JObject.Parse(responseStr);
            
            return isSuccess 
                ? new VerifyResult { Success = true, Ticket = json["ticket"]?.ToString() }
                : new VerifyResult { Success = false, ErrorMessage = json["reason"]?.ToString() };
        }
        catch (Exception ex) { return new VerifyResult { Success = false, ErrorMessage = ex.Message }; }
    }

    public static string CompleteLogin(string phoneNumber, string ticket, string deviceId = null)
    {
        var devId = deviceId ?? GetOrRegisterDevice();
        if (string.IsNullOrEmpty(devId)) return null;

        var formData = GetBaseParams();
        formData["device_id"] = devId;
        formData["ticket"] = ticket;
        formData["opt_fields"] = "nickname,avatar,realname_status,mobile_bind_status,mask_related_mobile,related_login_status";

        try
        {
            var url = $"/mpay/api/users/login/mobile/finish?un={Base64Encode(phoneNumber)}";
            var (isSuccess, responseStr) = PostRequest(url, formData);
            if (!isSuccess) return null;

            var user = JObject.Parse(responseStr)["user"];
            if (user == null) return null;

            var sauthStr = GenerateSauthString(user["id"].ToString(), user["token"].ToString(), devId);
            return new JObject { ["sauth_json"] = sauthStr }.ToString(Formatting.None);
        }
        catch { return null; }
    }

    public static string FullLoginFlow(string phoneNumber, string deviceId = null)
    {
        var devId = GetOrRegisterDevice(deviceId);
        if (string.IsNullOrEmpty(devId)) return null;

        var smsResult = SendSms(phoneNumber, devId);
        if (smsResult.Status == SmsStatus.Failed) return null;

        using var verifyForm = new PhoneVerifyForm(phoneNumber, smsResult);
        if (verifyForm.ShowDialog() != DialogResult.OK || string.IsNullOrEmpty(verifyForm.Ticket)) return null;

        return CompleteLogin(phoneNumber, verifyForm.Ticket, devId);
    }

    // ----------------- 加密与辅助工具 ----------------- //
    private static (bool IsSuccess, string ResponseStr) PostRequest(string endpoint, Dictionary<string, string> data)
    {
        var resp = _client.PostAsync(MPAY_HOST + endpoint, new FormUrlEncodedContent(data)).GetAwaiter().GetResult();
        var responseStr = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        return (resp.IsSuccessStatusCode, responseStr); // 使用 IsSuccessStatusCode 更安全，包含所有的 2xx 请求。
    }

    private static void HandleApiError(string responseStr, string contextName)
    {
        try
        {
            var json = JObject.Parse(responseStr);
            if (json["code"]?.ToObject<int>() == 1351)
            {
                var verifyUrl = json["verify_url"]?.ToString();
                if (!string.IsNullOrEmpty(verifyUrl))
                {
                    Clipboard.SetText(verifyUrl);
                    try { Process.Start(verifyUrl); } catch { }
                }
                var reason = json["reason"]?.ToString() ?? "需要验证";
                MessageBox.Show($"{reason}\n验证链接: {verifyUrl}\n链接已复制到剪贴板并已在浏览器中打开，验证完成后请重新操作。", 
                    $"{contextName}需要验证", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }
        catch { /* 解析失败则回退到通用提示 */ }

        string decoded = Regex.Unescape(responseStr);
        MessageBox.Show($"{contextName}失败:\n{decoded}", $"{contextName}错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private static string GenerateSauthString(string userId, string token, string deviceId)
    {
        var sauthDict = new Dictionary<string, string>
        {
            ["gameid"] = PROJECT_ID, ["login_channel"] = APP_CHANNEL, ["app_channel"] = APP_CHANNEL,
            ["platform"] = "pc", ["sdkuid"] = userId, ["sessionid"] = token,
            ["sdk_version"] = "4.2.0", ["udid"] = Guid.NewGuid().ToString("N").ToUpper(), ["deviceid"] = deviceId,
            ["aim_info"] = "{\"aim\":\"127.0.0.1\",\"country\":\"CN\",\"tz\":\"+0800\",\"tzid\":\"\"}"
        };
        return JsonConvert.SerializeObject(sauthDict, Formatting.None);
    }

    private static Dictionary<string, string> GetBaseParams() => new()
    {
        ["app_channel"] = APP_CHANNEL, ["app_mode"] = "2", ["app_type"] = "games",
        ["arch"] = "win_x64", ["cv"] = "c4.2.0", ["mcount_app_key"] = "EEkEEXLymcNjM42yLY3Bn6AO15aGy4yq",
        ["mcount_transaction_id"] = "0", ["process_id"] = "1000", ["sv"] = "10.0.22621",
        ["updater_cv"] = "c1.0.0", ["game_id"] = PROJECT_ID, ["gv"] = "c1.25.0"
    };

    private static string GenerateMac()
    {
        var bytes = new byte[6];
        RandomNumberGenerator.Create().GetBytes(bytes);
        return string.Join(":", Array.ConvertAll(bytes, b => b.ToString("x2")));
    }

    private static string AesEncryptHex(string plainText, string hexKey)
    {
        byte[] keyBytes = new byte[hexKey.Length / 2];
        for (int i = 0; i < keyBytes.Length; i++)
            keyBytes[i] = Convert.ToByte(hexKey.Substring(i * 2, 2), 16);
        
        using var aes = Aes.Create();
        aes.Key = keyBytes;
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.PKCS7;
        
        using var encryptor = aes.CreateEncryptor();
        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
        byte[] cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        
        return BitConverter.ToString(cipherBytes).Replace("-", "").ToLower();
    }

    private static string Md5Hash(string input)
    {
        using var md5 = MD5.Create();
        return BitConverter.ToString(md5.ComputeHash(Encoding.UTF8.GetBytes(input))).Replace("-", "").ToLower();
    }

    private static string Base64Encode(string input) => Convert.ToBase64String(Encoding.UTF8.GetBytes(input));
}

public enum SmsStatus { Success, UpstreamRequired, Failed }
public class SmsResult { public SmsStatus Status { get; set; } public string UpstreamContent { get; set; } public string UpstreamNumber { get; set; } public string ErrorMessage { get; set; } }
public class VerifyResult { public bool Success { get; set; } public string Ticket { get; set; } public string ErrorMessage { get; set; } }