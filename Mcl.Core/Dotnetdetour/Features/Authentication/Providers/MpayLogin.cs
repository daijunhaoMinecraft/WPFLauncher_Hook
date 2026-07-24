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
            if (File.Exists(path))
            {
                var cache = JObject.Parse(File.ReadAllText(path));
                _cachedDeviceId = cache?["device_id"]?.ToString();
                _cachedUniqueId = cache?["unique_id"]?.ToString();
                _cachedDeviceKey = cache?["device_key"]?.ToString();
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
        if (!string.IsNullOrEmpty(_cachedDeviceId)) return _cachedDeviceId;

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
            _cachedDeviceId = JObject.Parse(resp.Content.ReadAsStringAsync().Result)["device"]["id"].ToString();
            File.WriteAllText(Path.Combine(Environment.CurrentDirectory, CACHE_FILE), new JObject { ["unique_id"] = uniqueId, ["device_id"] = _cachedDeviceId }.ToString(Formatting.None));
            return _cachedDeviceId;
        }
        catch (Exception ex)
        {
            WpfConfig.DefaultLogger.Error($"设备注册失败: {ex.Message}");
            throw;
        }
    }

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

        // 使用重构后的现代 UI 窗体
        using var verifyForm = new PhoneVerifyForm(phoneNumber, smsResult);
        if (verifyForm.ShowDialog() != DialogResult.OK || string.IsNullOrEmpty(verifyForm.Ticket)) return null;

        return CompleteLogin(phoneNumber, verifyForm.Ticket, devId);
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
}

public enum SmsStatus { Success, UpstreamRequired, Failed }
public class SmsResult { public SmsStatus Status { get; set; } public string UpstreamContent { get; set; } public string UpstreamNumber { get; set; } public string ErrorMessage { get; set; } }
public class VerifyResult { public bool Success { get; set; } public string Ticket { get; set; } public string ErrorMessage { get; set; } }