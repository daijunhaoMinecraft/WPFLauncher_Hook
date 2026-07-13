using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using Mcl.Core.Dotnetdetour.Tools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mcl.Core.Dotnetdetour.Hookevent
{
    /// <summary>
    /// 手机号登录 — 对接 NetEase Mpay API
    /// 端口自 loginByPhone.py (X19LoginBot)
    /// </summary>
    public static class MpayPhoneLogin
    {
        private const string MPAY_HOST = "https://service.mkey.163.com";
        private const string PROJECT_ID = "x19";
        private const string GAME_VERSION = "c1.25.0";
        private const string CACHE_FILE = "device_cache.json";
        private const string APP_CHANNEL = "netease";
        private const string CV = "c4.2.0";
        private const string MCOUNT_APP_KEY = "EEkEEXLymcNjM42yLY3Bn6AO15aGy4yq";

        private static readonly Random _random = new Random();
        private static readonly HttpClient _client;
        private static string _cachedDeviceId;
        private static string _cachedUniqueId;

        static MpayPhoneLogin()
        {
            var handler = new HttpClientHandler
            {
                // 忽略 SSL 证书错误（与 Python urllib3.disable_warnings 对应）
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            };
            _client = new HttpClient(handler);
            _client.DefaultRequestHeaders.Clear();
            _client.DefaultRequestHeaders.Add("User-Agent", "WPFLauncher/0.0.0.0");
            // Content-Type 是内容头，由 FormUrlEncodedContent 自动设置，不能放在 DefaultRequestHeaders 中
            LoadDeviceCache();
        }

        #region Device Management

        /// <summary>
        /// 加载缓存的设备 ID
        /// </summary>
        private static void LoadDeviceCache()
        {
            try
            {
                string path = Path.Combine(Environment.CurrentDirectory, CACHE_FILE);
                if (File.Exists(path))
                {
                    var cache = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(path));
                    _cachedDeviceId = cache?["device_id"]?.ToString();
                    _cachedUniqueId = cache?["unique_id"]?.ToString();
                    if (!string.IsNullOrEmpty(_cachedDeviceId))
                    {
                        Tool.PrintYellow($"[MpayPhone] 从缓存加载设备 ID: {_cachedDeviceId}");
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// 保存设备 ID 到缓存文件
        /// </summary>
        private static void SaveDeviceCache(string uniqueId, string deviceId)
        {
            try
            {
                string path = Path.Combine(Environment.CurrentDirectory, CACHE_FILE);
                var cache = new JObject
                {
                    ["unique_id"] = uniqueId,
                    ["device_id"] = deviceId
                };
                File.WriteAllText(path, cache.ToString(Formatting.None));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MpayPhone] 保存设备缓存失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取或注册设备 ID。如果已有缓存则使用缓存，否则注册新设备。
        /// 也可以通过 account 的 DeviceId 指定已有设备。
        /// </summary>
        public static string GetOrRegisterDevice(string existingDeviceId = null)
        {
            // 优先使用传入的已有设备 ID
            if (!string.IsNullOrEmpty(existingDeviceId))
            {
                _cachedDeviceId = existingDeviceId;
                return _cachedDeviceId;
            }

            // 其次使用缓存的设备 ID
            if (!string.IsNullOrEmpty(_cachedDeviceId))
                return _cachedDeviceId;

            // 注册新设备
            RegisterNewDevice();
            return _cachedDeviceId;
        }

        private static void RegisterNewDevice()
        {
            string uniqueId = Guid.NewGuid().ToString("N");
            string url = $"{MPAY_HOST}/mpay/games/{PROJECT_ID}/devices";

            var formData = new Dictionary<string, string>(GetBaseParams())
            {
                ["unique_id"] = uniqueId,
                ["brand"] = "Microsoft",
                ["device_model"] = "pc_mode",
                ["device_name"] = $"PC-{GenerateRandomString(12)}",
                ["device_type"] = "Computer",
                ["init_urs_device"] = "0",
                ["mac"] = GenerateMac(),
                ["resolution"] = "1920x1080",
                ["system_name"] = "windows",
                ["system_version"] = "10.0.22621"
            };

            try
            {
                Tool.PrintYellow("[MpayPhone] 正在注册新设备...");
                var content = new FormUrlEncodedContent(formData);
                var resp = _client.PostAsync(url, content).Result;
                resp.EnsureSuccessStatusCode();
                var json = JObject.Parse(resp.Content.ReadAsStringAsync().Result);
                _cachedDeviceId = json["device"]["id"].ToString();
                _cachedUniqueId = uniqueId;

                SaveDeviceCache(uniqueId, _cachedDeviceId);
                Tool.PrintYellow($"[MpayPhone] 新设备注册成功: {_cachedDeviceId}");
            }
            catch (Exception ex)
            {
                Tool.PrintRed($"[MpayPhone] 设备注册失败: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region SMS Operations

        /// <summary>
        /// 发送短信验证码
        /// </summary>
        /// <returns>SmsResult 包含状态和数据</returns>
        public static SmsResult SendSms(string phoneNumber, string deviceId = null)
        {
            string devId = deviceId ?? GetOrRegisterDevice();
            string url = $"{MPAY_HOST}/mpay/api/users/login/mobile/get_sms";

            var formData = new Dictionary<string, string>(GetBaseParams())
            {
                ["device_id"] = devId,
                ["mobile"] = phoneNumber
            };

            try
            {
                var content = new FormUrlEncodedContent(formData);
                var resp = _client.PostAsync(url, content).Result;

                if (resp.StatusCode == HttpStatusCode.OK)
                {
                    return new SmsResult { Status = SmsStatus.Success };
                }

                // 尝试解析错误
                string body = resp.Content.ReadAsStringAsync().Result;
                JObject json = null;
                try { json = JObject.Parse(body); } catch { }

                if (json != null && json.Value<int>("code") == 1373)
                {
                    // 上行短信验证
                    var replySms = json["reply_sms"];
                    return new SmsResult
                    {
                        Status = SmsStatus.UpstreamRequired,
                        UpstreamContent = replySms?["content"]?.ToString(),
                        UpstreamNumber = replySms?["number"]?.ToString()
                    };
                }

                string reason = json?["reason"]?.ToString() ?? $"HTTP {(int)resp.StatusCode}";
                return new SmsResult { Status = SmsStatus.Failed, ErrorMessage = reason };
            }
            catch (Exception ex)
            {
                return new SmsResult { Status = SmsStatus.Failed, ErrorMessage = ex.Message };
            }
        }

        /// <summary>
        /// 验证短信验证码
        /// </summary>
        /// <param name="phoneNumber">手机号</param>
        /// <param name="code">下行验证码（普通模式）</param>
        /// <param name="upContent">上行短信内容（上行模式）</param>
        /// <param name="deviceId">设备 ID</param>
        /// <returns>VerifyResult 包含成功标志和 ticket</returns>
        public static VerifyResult VerifySms(string phoneNumber, string code = "", string upContent = "", string deviceId = null)
        {
            string devId = deviceId ?? GetOrRegisterDevice();
            string url = $"{MPAY_HOST}/mpay/api/users/login/mobile/verify_sms";

            var formData = new Dictionary<string, string>(GetBaseParams())
            {
                ["device_id"] = devId,
                ["mobile"] = phoneNumber,
                ["smscode"] = code ?? "",
                ["up_content"] = upContent ?? ""
            };

            try
            {
                var content = new FormUrlEncodedContent(formData);
                var resp = _client.PostAsync(url, content).Result;

                if (resp.StatusCode == HttpStatusCode.OK)
                {
                    var json = JObject.Parse(resp.Content.ReadAsStringAsync().Result);
                    return new VerifyResult { Success = true, Ticket = json["ticket"]?.ToString() };
                }

                string body = resp.Content.ReadAsStringAsync().Result;
                string msg;
                try
                {
                    msg = JObject.Parse(body)["reason"]?.ToString() ?? "验证失败";
                }
                catch
                {
                    msg = $"HTTP {(int)resp.StatusCode}";
                }
                return new VerifyResult { Success = false, ErrorMessage = msg };
            }
            catch (Exception ex)
            {
                return new VerifyResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        #endregion

        #region Login Completion

        /// <summary>
        /// 完成手机号登录，返回 sauth_json 字符串
        /// </summary>
        /// <param name="phoneNumber">手机号</param>
        /// <param name="ticket">验证后获取的 ticket</param>
        /// <param name="deviceId">设备 ID</param>
        /// <returns>包含 {"sauth_json": "..."} 的 JSON 字符串，失败返回 null</returns>
        public static string CompleteLogin(string phoneNumber, string ticket, string deviceId = null)
        {
            string devId = deviceId ?? GetOrRegisterDevice();
            string encodedPhone = Base64Encode(phoneNumber);
            string url = $"{MPAY_HOST}/mpay/api/users/login/mobile/finish?un={encodedPhone}";

            var formData = new Dictionary<string, string>(GetBaseParams())
            {
                ["device_id"] = devId,
                ["ticket"] = ticket,
                ["opt_fields"] = "nickname,avatar,realname_status,mobile_bind_status,mask_related_mobile,related_login_status"
            };

            try
            {
                var content = new FormUrlEncodedContent(formData);
                var resp = _client.PostAsync(url, content).Result;

                if (resp.StatusCode == HttpStatusCode.OK)
                {
                    var json = JObject.Parse(resp.Content.ReadAsStringAsync().Result);
                    var user = json["user"];
                    if (user == null) return null;

                    var sauthDict = new Dictionary<string, string>
                    {
                        ["gameid"] = PROJECT_ID,
                        ["login_channel"] = APP_CHANNEL,
                        ["app_channel"] = APP_CHANNEL,
                        ["platform"] = "pc",
                        ["sdkuid"] = user["id"].ToString(),
                        ["sessionid"] = user["token"].ToString(),
                        ["sdk_version"] = "4.2.0",
                        ["udid"] = Guid.NewGuid().ToString("N").ToUpper(),
                        ["deviceid"] = devId,
                        ["aim_info"] = "{\"aim\":\"127.0.0.1\",\"country\":\"CN\",\"tz\":\"+0800\",\"tzid\":\"\"}"
                    };

                    // 返回与 Cookie 登录一致的格式: {"sauth_json": "..."}
                    string sauthJson = JsonConvert.SerializeObject(sauthDict, Formatting.None);
                    var result = new JObject { ["sauth_json"] = sauthJson };
                    return result.ToString(Formatting.None);
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MpayPhone] 完成登录失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 完整手机号登录流程（在 UI 线程上同步执行）
        /// 此方法会弹出 SMS 验证对话框
        /// </summary>
        /// <param name="phoneNumber">手机号</param>
        /// <param name="deviceId">设备 ID（可为 null，自动获取或注册）</param>
        /// <returns>sauth_json 包装字符串，失败返回 null</returns>
        public static string FullLoginFlow(string phoneNumber, string deviceId = null)
        {
            try
            {
                // 1. 确保设备已注册
                string devId = GetOrRegisterDevice(deviceId);

                // 2. 发送短信
                Tool.PrintYellow($"[MpayPhone] 正在向 {phoneNumber} 发送验证码...");
                var smsResult = SendSms(phoneNumber, devId);

                if (smsResult.Status == SmsStatus.Failed)
                {
                    Tool.PrintRed($"[MpayPhone] 发送验证码失败: {smsResult.ErrorMessage}");
                    return null;
                }

                // 3. 显示验证对话框
                var verifyForm = new PhoneVerifyForm(phoneNumber, smsResult);
                var dialogResult = verifyForm.ShowDialog();

                if (dialogResult != System.Windows.Forms.DialogResult.OK || string.IsNullOrEmpty(verifyForm.Ticket))
                {
                    Tool.PrintYellow("[MpayPhone] 用户取消验证或验证失败");
                    return null;
                }

                // 4. 完成登录
                Tool.PrintYellow("[MpayPhone] 正在完成登录...");
                string sauthJson = CompleteLogin(phoneNumber, verifyForm.Ticket, devId);

                if (string.IsNullOrEmpty(sauthJson))
                {
                    Tool.PrintRed("[MpayPhone] 最终登录失败，接口返回异常");
                    return null;
                }

                Tool.PrintYellow($"[MpayPhone] 手机号 {phoneNumber} 登录成功!");
                return sauthJson;
            }
            catch (Exception ex)
            {
                Tool.PrintRed($"[MpayPhone] 登录流程异常: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region Helper Methods

        private static Dictionary<string, string> GetBaseParams()
        {
            return new Dictionary<string, string>
            {
                ["app_channel"] = APP_CHANNEL,
                ["app_mode"] = "2",
                ["app_type"] = "games",
                ["arch"] = "win_x64",
                ["cv"] = CV,
                ["mcount_app_key"] = MCOUNT_APP_KEY,
                ["mcount_transaction_id"] = "0",
                ["process_id"] = "1000",
                ["sv"] = "10.0.22621",
                ["updater_cv"] = "c1.0.0",
                ["game_id"] = PROJECT_ID,
                ["gv"] = GAME_VERSION
            };
        }

        private static string GenerateMac()
        {
            byte[] bytes = new byte[6];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            return string.Join(":", Array.ConvertAll(bytes, b => b.ToString("x2")));
        }

        private static string GenerateRandomString(int length = 12)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            char[] result = new char[length];
            for (int i = 0; i < length; i++)
                result[i] = chars[_random.Next(chars.Length)];
            return new string(result);
        }

        private static string Base64Encode(string s)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(s));
        }

        #endregion
    }

    #region Result Types

    public enum SmsStatus
    {
        /// <summary>普通短信验证码已发送</summary>
        Success,
        /// <summary>需要用户主动发送上行短信</summary>
        UpstreamRequired,
        /// <summary>发送失败</summary>
        Failed
    }

    public class SmsResult
    {
        public SmsStatus Status { get; set; }
        public string UpstreamContent { get; set; }
        public string UpstreamNumber { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class VerifyResult
    {
        public bool Success { get; set; }
        public string Ticket { get; set; }
        public string ErrorMessage { get; set; }
    }

    #endregion
}
