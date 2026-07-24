using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
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
    private static string _cachedDeviceKey; // 网易分配的设备 AES 密钥

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
            if (File.Exists(path))
            {
                var cache = JObject.Parse(File.ReadAllText(path));
                _cachedDeviceId = cache["device_id"]?.ToString();
                _cachedUniqueId = cache["unique_id"]?.ToString();
                _cachedDeviceKey = cache["device_key"]?.ToString();
            }
        }
        catch { /* ignored */ }
    }

    private static void SaveDeviceCache(string uniqueId, string deviceId, string deviceKey)
    {
        try
        {
            var path = Path.Combine(Environment.CurrentDirectory, CACHE_FILE);
            var cache = new JObject
            {
                ["unique_id"] = uniqueId,
                ["device_id"] = deviceId,
                ["device_key"] = deviceKey
            };
            File.WriteAllText(path, cache.ToString(Formatting.None));
        }
        catch { /* ignored */ }
    }

    public static string GetOrRegisterDevice(string existingDeviceId = null)
    {
        if (!string.IsNullOrEmpty(existingDeviceId)) return existingDeviceId;
        if (!string.IsNullOrEmpty(_cachedDeviceId) && !string.IsNullOrEmpty(_cachedDeviceKey)) return _cachedDeviceId;

        var uniqueId = Guid.NewGuid().ToString("N");
        var formData = new Dictionary<string, string>(GetBaseParams())
        {
            ["unique_id"] = uniqueId, ["brand"] = "Microsoft", ["device_model"] = "pc_mode",
            ["device_name"] = $"PC-{Guid.NewGuid().ToString("N").Substring(0, 6)}",
            ["device_type"] = "Computer", ["init_urs_device"] = "0", ["mac"] = GenerateMac(),
            ["resolution"] = "1920x1080", ["system_name"] = "windows", ["system_version"] = "10.0.22621"
        };

        try
        {
            var resp = _client.PostAsync($"{MPAY_HOST}/mpay/games/{PROJECT_ID}/devices", new FormUrlEncodedContent(formData)).Result;

            // 处理非 200 状态码
            if (resp.StatusCode != HttpStatusCode.OK)
            {
                var responseStr = resp.Content.ReadAsStringAsync().Result;
                try
                {
                    var responseJson = JObject.Parse(responseStr);
                    int code = responseJson["code"]?.ToObject<int>() ?? 0;
                    if (code == 1351)
                    {
                        string verifyUrl = responseJson["verify_url"]?.ToString();
                        if (!string.IsNullOrEmpty(verifyUrl))
                        {
                            Clipboard.SetText(verifyUrl);
                            try { System.Diagnostics.Process.Start(verifyUrl); } catch { }
                        }
                        string reason = responseJson["reason"]?.ToString() ?? "需要验证";
                        MessageBox.Show(
                            $"{reason}\n验证链接: {verifyUrl}\n链接已复制到剪贴板并已在浏览器中打开，验证完成后请重新操作。",
                            "设备注册需要验证",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                    else
                    {
                        // 其他错误码，解码后显示
                        string decoded = System.Text.RegularExpressions.Regex.Unescape(responseStr);
                        MessageBox.Show($"设备注册失败:\n{decoded}", "设备注册错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch
                {
                    // JSON 解析失败，同样解码后显示
                    string decoded = System.Text.RegularExpressions.Regex.Unescape(responseStr);
                    MessageBox.Show($"设备注册失败:\n{decoded}", "设备注册错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return null; // 任何错误情况均返回 null
            }

            // 状态码 200，正常解析
            var json = JObject.Parse(resp.Content.ReadAsStringAsync().Result);
            _cachedDeviceId = json["device"]["id"].ToString();
            _cachedDeviceKey = json["device"]["key"].ToString();
            _cachedUniqueId = uniqueId;

            SaveDeviceCache(_cachedUniqueId, _cachedDeviceId, _cachedDeviceKey);
            return _cachedDeviceId;
        }
        catch (Exception ex)
        {
            WpfConfig.DefaultLogger.Error($"设备注册失败: {ex.Message}");
            // 发生网络异常等非业务错误时，也显示通用提示并返回 null
            MessageBox.Show($"设备注册发生异常: {ex.Message}", "设备注册错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return null;
        }
    }

    // ----------------- 网易邮箱登录 ----------------- //
    public static string EmailLoginFlow(string email, string password)
    {
        try
        {
            // 确保设备和网易分配的 AES 密钥存在
            GetOrRegisterDevice();

            string encodedEmail = Convert.ToBase64String(Encoding.UTF8.GetBytes(email));
            
            var loginParams = new JObject
            {
                ["username"] = email,
                ["password"] = Md5Hash(password),
                ["unique_id"] = _cachedUniqueId
            };
            
            string encryptedParams = AesEncryptHex(loginParams.ToString(Formatting.None), _cachedDeviceKey);

            var formData = new Dictionary<string, string>(GetBaseParams())
            {
                ["opt_fields"] = "nickname,avatar,realname_status,mobile_bind_status,mask_related_mobile,related_login_status",
                ["params"] = encryptedParams,
                ["un"] = encodedEmail
            };

            var url = $"{MPAY_HOST}/mpay/games/{PROJECT_ID}/devices/{_cachedDeviceId}/users";
            var resp = _client.PostAsync(url, new FormUrlEncodedContent(formData)).Result;
            
            var responseStr = resp.Content.ReadAsStringAsync().Result;
            if (resp.StatusCode != HttpStatusCode.OK)
            {
                try
                {
                    var responseJson = JObject.Parse(responseStr);
                    int code = responseJson["code"]?.ToObject<int>() ?? 0;
                    if (code == 1351)
                    {
                        string verifyUrl = responseJson["verify_url"]?.ToString();
                        if (!string.IsNullOrEmpty(verifyUrl))
                        {
                            Clipboard.SetText(verifyUrl);
                            try
                            {
                                System.Diagnostics.Process.Start(verifyUrl);
                            }
                            catch
                            {
                                // ignored
                            }
                        }

                        string reason = responseJson["reason"]?.ToString() ?? "需要验证";
                        MessageBox.Show(
                            $"{reason}\n验证链接: {verifyUrl}\n链接已复制到剪贴板并已在浏览器中打开，验证完成后请重新登录。",
                            "登录错误",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                    else
                    {
                        string decoded = System.Text.RegularExpressions.Regex.Unescape(responseStr);
                        MessageBox.Show($"邮箱登录失败:\n{decoded}", "登录错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception)
                {
                    string decoded = System.Text.RegularExpressions.Regex.Unescape(responseStr);
                    MessageBox.Show($"邮箱登录失败:\n{decoded}", "登录错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return null;
            }

            var user = JObject.Parse(responseStr)["user"];
            if (user == null) return null;

            var sauthDict = new Dictionary<string, string>
            {
                ["gameid"] = PROJECT_ID, ["login_channel"] = APP_CHANNEL, ["app_channel"] = APP_CHANNEL,
                ["platform"] = "pc", ["sdkuid"] = user["id"].ToString(), ["sessionid"] = user["token"].ToString(),
                ["sdk_version"] = "4.2.0", ["udid"] = Guid.NewGuid().ToString("N").ToUpper(), ["deviceid"] = _cachedDeviceId,
                ["aim_info"] = "{\"aim\":\"127.0.0.1\",\"country\":\"CN\",\"tz\":\"+0800\",\"tzid\":\"\"}"
            };

            return JsonConvert.SerializeObject(sauthDict, Formatting.None);
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
        var formData = new Dictionary<string, string>(GetBaseParams())
        {
            ["device_id"] = deviceId ?? GetOrRegisterDevice(),
            ["mobile"] = phoneNumber
        };

        try
        {
            var resp = _client.PostAsync($"{MPAY_HOST}/mpay/api/users/login/mobile/get_sms", new FormUrlEncodedContent(formData)).Result;
            if (resp.StatusCode == HttpStatusCode.OK) return new SmsResult { Status = SmsStatus.Success };

            var json = JObject.Parse(resp.Content.ReadAsStringAsync().Result);
            if (json.Value<int>("code") == 1373)
            {
                return new SmsResult { Status = SmsStatus.UpstreamRequired, UpstreamContent = json["reply_sms"]?["content"]?.ToString(), UpstreamNumber = json["reply_sms"]?["number"]?.ToString() };
            }

            return new SmsResult { Status = SmsStatus.Failed, ErrorMessage = json["reason"]?.ToString() };
        }
        catch (Exception ex) { return new SmsResult { Status = SmsStatus.Failed, ErrorMessage = ex.Message }; }
    }

    public static VerifyResult VerifySms(string phoneNumber, string code = "", string upContent = "", string deviceId = null)
    {
        var formData = new Dictionary<string, string>(GetBaseParams())
        {
            ["device_id"] = deviceId ?? GetOrRegisterDevice(), ["mobile"] = phoneNumber,
            ["smscode"] = code ?? "", ["up_content"] = upContent ?? ""
        };

        try
        {
            var resp = _client.PostAsync($"{MPAY_HOST}/mpay/api/users/login/mobile/verify_sms", new FormUrlEncodedContent(formData)).Result;
            var json = JObject.Parse(resp.Content.ReadAsStringAsync().Result);
            
            return resp.StatusCode == HttpStatusCode.OK 
                ? new VerifyResult { Success = true, Ticket = json["ticket"]?.ToString() }
                : new VerifyResult { Success = false, ErrorMessage = json["reason"]?.ToString() };
        }
        catch (Exception ex) { return new VerifyResult { Success = false, ErrorMessage = ex.Message }; }
    }

    public static string CompleteLogin(string phoneNumber, string ticket, string deviceId = null)
    {
        var devId = deviceId ?? GetOrRegisterDevice();
        var formData = new Dictionary<string, string>(GetBaseParams())
        {
            ["device_id"] = devId, ["ticket"] = ticket,
            ["opt_fields"] = "nickname,avatar,realname_status,mobile_bind_status,mask_related_mobile,related_login_status"
        };

        try
        {
            var resp = _client.PostAsync($"{MPAY_HOST}/mpay/api/users/login/mobile/finish?un={Convert.ToBase64String(Encoding.UTF8.GetBytes(phoneNumber))}", new FormUrlEncodedContent(formData)).Result;
            if (resp.StatusCode != HttpStatusCode.OK) return null;

            var user = JObject.Parse(resp.Content.ReadAsStringAsync().Result)["user"];
            if (user == null) return null;

            var sauthDict = new Dictionary<string, string>
            {
                ["gameid"] = PROJECT_ID, ["login_channel"] = APP_CHANNEL, ["app_channel"] = APP_CHANNEL,
                ["platform"] = "pc", ["sdkuid"] = user["id"].ToString(), ["sessionid"] = user["token"].ToString(),
                ["sdk_version"] = "4.2.0", ["udid"] = Guid.NewGuid().ToString("N").ToUpper(), ["deviceid"] = devId,
                ["aim_info"] = "{\"aim\":\"127.0.0.1\",\"country\":\"CN\",\"tz\":\"+0800\",\"tzid\":\"\"}"
            };

            return new JObject { ["sauth_json"] = JsonConvert.SerializeObject(sauthDict, Formatting.None) }.ToString(Formatting.None);
        }
        catch { return null; }
    }

    public static string FullLoginFlow(string phoneNumber, string deviceId = null)
    {
        var devId = GetOrRegisterDevice(deviceId);
        var smsResult = SendSms(phoneNumber, devId);

        if (smsResult.Status == SmsStatus.Failed) return null;

        using var verifyForm = new PhoneVerifyForm(phoneNumber, smsResult);
        if (verifyForm.ShowDialog() != DialogResult.OK || string.IsNullOrEmpty(verifyForm.Ticket)) return null;

        return CompleteLogin(phoneNumber, verifyForm.Ticket, devId);
    }

    // ----------------- 加密与辅助工具 ----------------- //
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
        byte[] hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
    }
}

public enum SmsStatus { Success, UpstreamRequired, Failed }
public class SmsResult { public SmsStatus Status { get; set; } public string UpstreamContent { get; set; } public string UpstreamNumber { get; set; } public string ErrorMessage { get; set; } }
public class VerifyResult { public bool Success { get; set; } public string Ticket { get; set; } public string ErrorMessage { get; set; } }