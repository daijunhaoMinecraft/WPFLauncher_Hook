using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using MicrosoftTranslator.DotNetTranstor.Tools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// ──────────────────────────────────────────────────────────────
// 4399 PE (手游) 登录/注册模块
// 对齐 Python 参考实现 —— 纯 PE 流程
// 流程: captcha → getParams(GET) → loginAndAuthorize → OAuth callback → sauth
// 关键区别: 密码明文、验证码必须、无 uni_sauth
// ──────────────────────────────────────────────────────────────

namespace Mcl.Core.DotNetTranstor.Tools
{
    internal class _4399
    {
        // ══════════════════════════════════════════════════════
        // 公开 API
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// 4399 PE 登录 —— 对齐 Python 参考实现。
        /// 验证码必须，密码明文提交，验证码错误自动重试。
        /// 返回的 SauthJson 是 {"sauth_json":"..."} 格式的 JSON 字符串。
        /// </summary>
        public static async Task<LoginResult> LoginAsync(
            string username, string password)
        {
            Console.WriteLine("[4399PE] ========== 登录开始 ==========");
            Console.WriteLine($"[4399PE] 用户名: {username}");

            while (true)
            {
                try
                {
                    // ── 1. 生成 captchaId + 识别验证码（必须）──
                    var captchaId = Guid.NewGuid().ToString()
                        .Replace("-", "").ToUpper();
                    Console.WriteLine($"[4399PE] Step 1: captchaId={captchaId}");

                    var captchaResult = await GetAndSolveCaptchaAsync(captchaId)
                        .ConfigureAwait(false);
                    if (string.IsNullOrEmpty(captchaResult))
                    {
                        Console.WriteLine("[4399PE] 用户取消验证码输入");
                        return LoginResult.Fail("验证码输入取消");
                    }
                    Console.WriteLine($"[4399PE] 验证码结果: \"{captchaResult}\"");

                    // ── 2. 获取 OAuth 参数 ─────────────────────
                    Console.WriteLine("[4399PE] Step 2: 获取 OAuth 参数...");
                    var oauthParams = await ParamsAsync()
                        .ConfigureAwait(false);
                    Console.WriteLine($"[4399PE] client_id={oauthParams.ClientId}, state={oauthParams.State}");

                    // ── 3. POST loginAndAuthorize ──────────────
                    var form = BuildLoginForm(username, password,
                        captchaResult, captchaId, oauthParams);
                    var loginUrl = OAuth2BaseUrl + "loginAndAuthorize.do" +
                                   "?channel=&sdk=op&sdk_version=" + SdkVersion;
                    Console.WriteLine("[4399PE] Step 3: POST loginAndAuthorize...");

                    using (var client = CreateHttpClient())
                    {
                        var httpResponse = await client.PostAsync(
                                loginUrl, new FormUrlEncodedContent(form))
                            .ConfigureAwait(false);
                        Console.WriteLine($"[4399PE] HTTP {(int)httpResponse.StatusCode}");

                        var responseBody = await httpResponse.Content
                            .ReadAsStringAsync().ConfigureAwait(false);
                        Console.WriteLine($"[4399PE] body 长度: {responseBody?.Length ?? 0}");

                        // ── 4. 空 body → 登录成功 ────────────
                        if (string.IsNullOrEmpty(responseBody))
                        {
                            var location = httpResponse.Headers.Location
                                ?.ToString();
                            if (string.IsNullOrEmpty(location))
                                return LoginResult.Fail(
                                    "登录成功但无 Location header");

                            Console.WriteLine($"[4399PE] Step 4: GET OAuth 回调: {location}");
                            var oauthData = await GetOAuthCallbackAsync(location)
                                .ConfigureAwait(false);
                            if (oauthData == null)
                                return LoginResult.Fail(
                                    "OAuth 回调解析失败");

                            Console.WriteLine($"[4399PE] uid={oauthData.Uid}, state={oauthData.State}");

                            var sauthJson = BuildSauthJson(oauthData.Uid,
                                oauthData.State);
                            Console.WriteLine("[4399PE] Step 5: 获取 uni_sauth 返回");

                            var sauthUrl = "https://mgbsdk.matrix.netease.com/x19/sdk/uni_sauth";
                            using (var sauthClient = CreateHttpClient())
                            {
                                var sauthContent = new StringContent(
                                    sauthJson, Encoding.UTF8, "application/json");
                                var sauthResponse = await sauthClient.PostAsync(
                                        sauthUrl, sauthContent)
                                    .ConfigureAwait(false);
                                var sauthResult = await sauthResponse.Content
                                    .ReadAsStringAsync()
                                    .ConfigureAwait(false);
                                Console.WriteLine(
                                    $"[4399PE] uni_sauth HTTP {(int)sauthResponse.StatusCode}");
                                Console.WriteLine(
                                    $"[4399PE] uni_sauth 返回: {sauthResult}");
                            }

                            var result = new JObject
                            {
                                ["sauth_json"] = sauthJson,
                            };
                            
                            Console.WriteLine("[4399PE] ========== 登录成功! ==========");
                            return LoginResult.Ok(
                                JsonConvert.SerializeObject(result));
                        }

                        // ── 5. 有 body → 错误处理 ────────────
                        if (responseBody.Contains("验证码错误"))
                        {
                            Console.WriteLine(
                                "[4399PE] 验证码错误，重新获取...");
                            continue; // 重新获取验证码 → 回到 while 顶部
                        }

                        // 提取 #login_err_msg
                        var m = Regex.Match(responseBody,
                            @"id=""login_err_msg""\s*>\s*([^<]+)\s*<");
                        if (m.Success)
                        {
                            var errMsg = m.Groups[1].Value.Trim();
                            Console.WriteLine(
                                $"[4399PE] 登录错误: {errMsg}");
                            return LoginResult.Fail(errMsg);
                        }

                        // 如果 HTML 较短，打印出来帮助排查
                        if (responseBody.Length < 500)
                            Console.WriteLine(
                                $"[4399PE] HTML: {responseBody}");

                        return LoginResult.Fail("未知登录错误");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"[4399PE] 异常: {ex.GetType().Name}: {ex.Message}");
                    Console.WriteLine($"[4399PE] 堆栈: {ex.StackTrace}");
                    return LoginResult.Fail(ex.Message);
                }
            }
        }

        /// <summary>
        /// 4399 PE 注册（自动生成用户名密码）。
        /// 保留原有的 oauth.html POST 流程 + 加密密码。
        /// </summary>
        public static RegisterResult Register(
            string captcha = null,
            string captchaId = null)
        {
            Console.WriteLine("[4399PE] ========== 注册开始 ==========");
            try
            {
                var username = GenerateRandomString(3) + "_" +
                               GenerateRandomString(6);
                var password = GenerateRandomString(8);
                Console.WriteLine(
                    $"[4399PE][Reg] 自动生成: user={username}");

                var encryptedPwd = EncryptPassword(password);
                var device = CreateDevice();

                Console.WriteLine("[4399PE][Reg] Step 1: GET OAuth params...");
                var oauthParams = Task.Run(
                    () => ParamsAsync()).Result;

                Console.WriteLine("[4399PE][Reg] Step 2: POST authorize.do...");
                var reqInfo = Task.Run(() =>
                    GetReqInfoAsync("REGISTER", oauthParams)).Result;
                if (reqInfo.CaptchaRequired && string.IsNullOrEmpty(captcha))
                    return RegisterResult.CaptchaNeeded(
                        reqInfo.CaptchaId, reqInfo.CaptchaUrl);

                var form = BuildRegisterForm(username, encryptedPwd,
                    captcha,
                    captchaId ?? reqInfo.CaptchaId,
                    oauthParams, reqInfo.RegReqId);

                var regUrl = OAuth2BaseUrl + "registerAndAuthorize.do";
                Console.WriteLine(
                    "[4399PE][Reg] Step 3: POST registerAndAuthorize...");

                var request = (HttpWebRequest)WebRequest.Create(regUrl);
                request.Method = "POST";
                request.ContentType =
                    "application/x-www-form-urlencoded";
                request.UserAgent = UserAgent;
                request.AllowAutoRedirect = false;

                var bodyBytes = Encoding.UTF8.GetBytes(
                    string.Join("&", form.Select(kv =>
                        Uri.EscapeDataString(kv.Key) + "=" +
                        Uri.EscapeDataString(kv.Value))));
                request.ContentLength = bodyBytes.Length;

                using (var reqStream = request.GetRequestStream())
                    reqStream.Write(bodyBytes, 0, bodyBytes.Length);

                using (var response =
                       (HttpWebResponse)request.GetResponse())
                {
                    var location = response.Headers["Location"];
                    if (location != null)
                    {
                        var regResult = Task.Run(() =>
                            HandleRegCallbackAsync(
                                location, device, username, password)
                        ).Result;
                        if (regResult.Success)
                            return RegisterResult.Ok(username, password);
                        return RegisterResult.Fail(
                            regResult.ErrorMessage ?? "注册回调失败");
                    }

                    using (var reader = new StreamReader(
                               response.GetResponseStream(),
                               Encoding.GetEncoding("utf-8")))
                    {
                        var html = reader.ReadToEnd();
                        var lr = ParseLoginError(html, captcha);
                        if (lr.CaptchaRequired)
                            return RegisterResult.CaptchaNeeded(
                                lr.CaptchaId, lr.CaptchaUrl);
                        return RegisterResult.Fail(
                            lr.ErrorMessage ?? "注册失败");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[4399PE][Reg] 异常: {ex.Message}");
                return RegisterResult.Fail(ex.Message);
            }
        }

        // ══════════════════════════════════════════════════════
        // 验证码: 下载 + OCR（支持刷新）
        // ══════════════════════════════════════════════════════

        private static async Task<string> GetAndSolveCaptchaAsync(
            string initialCaptchaId)
        {
            // 下载初始验证码图片
            var base64 = DownloadCaptchaImageSync(initialCaptchaId);
            if (string.IsNullOrEmpty(base64)) return null;

            Console.WriteLine(
                $"[4399PE][Captcha] 图片大小: {Convert.FromBase64String(base64).Length} bytes");
            Console.WriteLine(
                "[4399PE][Captcha] 调用 OCR 弹窗（支持刷新）...");

            // 刷新回调：生成新 captchaId → 下载新图片 → 返回 base64
            // 此回调在 OCR 对话框的 STA 线程上同步执行
            var currentCaptchaId = initialCaptchaId;
            Func<string> onRefresh = () =>
            {
                currentCaptchaId = Guid.NewGuid().ToString()
                    .Replace("-", "").ToUpper();
                Console.WriteLine(
                    $"[4399PE][Captcha] 用户刷新: new captchaId={currentCaptchaId}");
                return DownloadCaptchaImageSync(currentCaptchaId);
            };

            return Ocr.GetOcrWithRefresh(base64, onRefresh);
        }

        /// <summary>
        /// 同步下载验证码图片（用于刷新回调）。
        /// </summary>
        private static string DownloadCaptchaImageSync(string captchaId)
        {
            var url =
                "https://ptlogin.4399.com/ptlogin/captcha.do" +
                "?captchaId=" + captchaId;
            Console.WriteLine(
                $"[4399PE][Captcha] 下载: {url}");
            try
            {
                using (var wc = new WebClient())
                {
                    wc.Headers.Add("User-Agent", UserAgent);
                    var bytes = wc.DownloadData(url);
                    return Convert.ToBase64String(bytes);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[4399PE][Captcha] 下载失败: {ex.Message}");
                return null;
            }
        }

        // ══════════════════════════════════════════════════════
        // OAuth 参数获取: GET oauth-callback.html
        // ══════════════════════════════════════════════════════

        private static async Task<OAuthParams> ParamsAsync()
        {
            var callbackUrl =
                "https://m.4399api.com/openapi/oauth-callback.html" +
                "?gamekey=44770&game_key=115716";
            Console.WriteLine(
                $"[4399PE][Params] GET {callbackUrl}");

            using (var client = CreateHttpClient())
            {
                var response = await client.GetAsync(callbackUrl)
                    .ConfigureAwait(false);
                var json = await response.Content
                    .ReadAsStringAsync().ConfigureAwait(false);
                var root = JObject.Parse(json);
                var oauthUrl = root["result"]?.ToString();
                if (string.IsNullOrEmpty(oauthUrl))
                    throw new Exception("获取 oauth URL 失败");

                Console.WriteLine(
                    $"[4399PE][Params] oauth URL: {oauthUrl}");

                var uri = new Uri(oauthUrl);
                var q = HttpUtility.ParseQueryString(uri.Query);
                return new OAuthParams
                {
                    ClientId    = q["client_id"]    ?? "",
                    State       = q["state"]        ?? "",
                    RedirectUri = q["redirect_uri"] ?? "",
                    D           = q["_d"]           ?? "",
                    BizId       = q["bizId"]        ?? "",
                    Ref         = q["ref"]          ?? "",
                };
            }
        }

        // ══════════════════════════════════════════════════════
        // OAuth 回调: GET Location → JSON
        // ══════════════════════════════════════════════════════

        private static async Task<OAuthCallbackData>
            GetOAuthCallbackAsync(string locationUrl)
        {
            using (var client = CreateHttpClient())
            {
                var response = await client.GetAsync(locationUrl)
                    .ConfigureAwait(false);
                var json = await response.Content
                    .ReadAsStringAsync().ConfigureAwait(false);
                Console.WriteLine(
                    $"[4399PE][OAuth] 响应: {json}");

                var root = JObject.Parse(json);
                var result = root["result"];
                if (result == null) return null;

                return new OAuthCallbackData
                {
                    Uid   = result["uid"]?.ToString(),
                    State = result["state"]?.ToString(),
                };
            }
        }

        // ══════════════════════════════════════════════════════
        // 注册 OAuth 回调（保留加密密码版本）
        // ══════════════════════════════════════════════════════

        private static async Task<LoginResult> HandleRegCallbackAsync(
            string locationUrl, string deviceJson,
            string username, string password)
        {
            using (var client = CreateHttpClient())
            {
                var response = await client.GetAsync(locationUrl)
                    .ConfigureAwait(false);
                var json = await response.Content
                    .ReadAsStringAsync().ConfigureAwait(false);
                var root = JObject.Parse(json);

                var result = root["result"];
                if (result == null)
                    return LoginResult.Fail(
                        root["message"]?.ToString() ??
                        "OAuth回调解析失败");

                var uid   = result["uid"]?.ToString();
                var state = result["state"]?.ToString();
                if (string.IsNullOrEmpty(uid) ||
                    string.IsNullOrEmpty(state))
                    return LoginResult.Fail(
                        "OAuth回调缺少 uid/state");

                var sauthJson = BuildSauthJsonPE(uid, state,
                    deviceJson, username);
                var wrapped = new JObject
                {
                    ["sauth_json"] = sauthJson,
                };
                return LoginResult.Ok(
                    JsonConvert.SerializeObject(wrapped));
            }
        }

        // ══════════════════════════════════════════════════════
        // 注册预检: authorize.do
        // ══════════════════════════════════════════════════════

        private static async Task<ReqInfoResult> GetReqInfoAsync(
            string authAction, OAuthParams oauthParams)
        {
            var form = new Dictionary<string, string>
            {
                ["_d"]                     = oauthParams.D,
                ["access_token"]           = "",
                ["aid"]                    = "",
                ["auth_action"]            = authAction,
                ["auto_scroll"]            = "",
                ["autoCreateAccount"]      = "",
                ["bizId"]                  = oauthParams.BizId,
                ["cid"]                    = "",
                ["client_id"]              = oauthParams.ClientId,
                ["css"]                    = "",
                ["expand_ext_login_list"]  = "",
                ["isInputRealname"]        = "false",
                ["isVaildRealname"]        = "false",
                ["password"]               = "",
                ["phone"]                  = "",
                ["phone_captcha"]          = "",
                ["redirect_uri"]           = oauthParams.RedirectUri,
                ["ref"]                    = oauthParams.Ref,
                ["reg_mode"]               = "reg_normal",
                ["response_type"]          = "TOKEN",
                ["scope"]                  = "basic",
                ["sec"]                    = "1",
                ["show_4399"]              = "",
                ["show_back_button"]       = "",
                ["show_close_button"]      = "",
                ["show_ext_login"]         = "",
                ["show_forget_password"]   = "",
                ["show_topbar"]            = "false",
                ["state"]                  = oauthParams.State,
                ["uid"]                    = "",
                ["username"]               = "",
                ["username_history"]       = "",
            };

            var url = OAuth2BaseUrl + "authorize.do" +
                      "?channel=&sdk=op&sdk_version=" + SdkVersion;

            using (var client = CreateHttpClient())
            {
                var response = await client.PostAsync(
                        url, new FormUrlEncodedContent(form))
                    .ConfigureAwait(false);
                var html = await response.Content
                    .ReadAsStringAsync().ConfigureAwait(false);

                var cId = ExtractAttributePE(html,
                    "name=\"captcha_id\"", "value");
                return new ReqInfoResult
                {
                    CaptchaId       = cId,
                    CaptchaUrl      = ExtractAttributePE(html,
                        "id=\"captcha_img\"", "src"),
                    RegReqId        = ExtractAttributePE(html,
                        "id=\"reg_req_id\"", "value"),
                    Username        = ExtractAttributePE(html,
                        "name=\"username\"", "value"),
                    CaptchaRequired = !string.IsNullOrEmpty(cId),
                };
            }
        }

        // ══════════════════════════════════════════════════════
        // HTML 错误解析
        // ══════════════════════════════════════════════════════

        private static LoginResult ParseLoginError(
            string html, string submittedCaptcha)
        {
            var errorMsg = ExtractBetween(html,
                "id=\"login_err_msg\"", "</");
            if (!string.IsNullOrEmpty(errorMsg))
            {
                errorMsg = ExtractBetween(errorMsg, ">", null);
                errorMsg = errorMsg?.Trim();
            }

            if (errorMsg == "验证码错误")
                return LoginResult.CaptchaNeeded(
                    ExtractAttributePE(html,
                        "name=\"captcha_id\"", "value"),
                    ExtractAttributePE(html,
                        "id=\"captcha_img\"", "src"));

            var captchaId = ExtractAttributePE(html,
                "name=\"captcha_id\"", "value");
            if (!string.IsNullOrEmpty(captchaId) &&
                string.IsNullOrEmpty(submittedCaptcha))
                return LoginResult.CaptchaNeeded(
                    captchaId,
                    ExtractAttributePE(html,
                        "id=\"captcha_img\"", "src"));

            if (!string.IsNullOrEmpty(errorMsg))
                return LoginResult.Fail(errorMsg);

            return LoginResult.Fail("未知登录错误");
        }

        // ══════════════════════════════════════════════════════
        // 密码加密（仅注册使用）
        // ══════════════════════════════════════════════════════

        private static string EncryptPassword(string password)
        {
            var keyMaterial = Encoding.UTF8.GetBytes("lzYW5qaXVqa");
            var salt = new byte[8];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(salt);

            byte[] h1, h2, h3;
            using (var md5 = MD5.Create())
            {
                h1 = md5.ComputeHash(Combine(keyMaterial, salt));
                h2 = md5.ComputeHash(Combine(h1, keyMaterial, salt));
                h3 = md5.ComputeHash(Combine(h2, keyMaterial, salt));
            }

            var key = new byte[32];
            Buffer.BlockCopy(h1, 0, key, 0, 16);
            Buffer.BlockCopy(h2, 0, key, 16, 16);

            var iv = new byte[16];
            Buffer.BlockCopy(h3, 0, iv, 0, 16);

            byte[] cipher;
            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                using (var encryptor = aes.CreateEncryptor())
                using (var ms = new MemoryStream())
                using (var cs = new CryptoStream(
                    ms, encryptor, CryptoStreamMode.Write))
                {
                    var plain = Encoding.UTF8.GetBytes(password);
                    cs.Write(plain, 0, plain.Length);
                    cs.FlushFinalBlock();
                    cipher = ms.ToArray();
                }
            }

            return Convert.ToBase64String(
                Combine(
                    Encoding.UTF8.GetBytes("Salted__"), salt, cipher));
        }

        // ══════════════════════════════════════════════════════
        // 设备信息
        // ══════════════════════════════════════════════════════

        private static string CreateDevice()
        {
            var now = DateTime.Now;
            var suffix = now.Year.ToString() +
                         now.Month.ToString("D2") +
                         now.Day.ToString("D2");

            var device = new JObject
            {
                ["BID"]                  = "com.netease.mc.m4399",
                ["CANAL_IDENTIFIER"]     = "",
                ["DEBUG"]                = "false",
                ["DEVICE_IDENTIFIER"]    = suffix + GenerateRandomHex(54),
                ["DEVICE_IDENTIFIER_SM"] = suffix + GenerateRandomHex(54),
                ["DEVICE_MODEL"]         = "22081212C",
                ["DEVICE_MODEL_VERSION"] = "13",
                ["GAME_BOX_VERSION"]     = "",
                ["GAME_KEY"]             = "115716",
                ["GAME_VERSION"]         = "3.7.15.287957",
                ["NETWORK_TYPE"]         = "WIFI",
                ["PLATFORM_TYPE"]        = "Android",
                ["RUNTIME"]              = "Origin",
                ["SCREEN_RESOLUTION"]    = "2624*1220",
                ["SDK_VERSION"]          = SdkVersion,
                ["SERVER_SERIAL"]        = "0",
                ["SYSTEM_VERSION"]       = "13",
                ["TEAM"]                 = 2,
                ["UDID"]                 = "",
                ["UID"]                  = "",
                ["VIP_INFO"]             = "",
            };

            return JsonConvert.SerializeObject(device);
        }

        // ══════════════════════════════════════════════════════
        // 表单构建
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// 登录表单 —— 对齐 Python 参考实现。
        /// 密码明文提交，字段精简。
        /// </summary>
        private static Dictionary<string, string> BuildLoginForm(
            string username, string password,
            string captcha, string captchaId, OAuthParams p)
        {
            return new Dictionary<string, string>
            {
                ["auth_action"]       = "ORILOGIN",
                ["bizId"]             = "2100001792",
                ["captcha"]           = captcha ?? "",
                ["captcha_id"]        = captchaId ?? "",
                ["client_id"]         = p.ClientId,
                ["isInputRealname"]   = "false",
                ["isVaildRealname"]   = "false",
                ["password"]          = password,
                ["redirect_uri"]      = RedirectUri,
                ["ref"]               = p.Ref,
                ["response_type"]     = "TOKEN",
                ["scope"]             = "basic",
                ["sec"]               = "0",
                ["state"]             = p.State,
                ["username"]          = username,
            };
        }

        private static Dictionary<string, string> BuildRegisterForm(
            string username, string encryptedPassword,
            string captcha, string captchaId,
            OAuthParams p, string regReqId)
        {
            return new Dictionary<string, string>
            {
                ["_d"]                   = p.D,
                ["access_token"]         = "",
                ["aid"]                  = "",
                ["auth_action"]          = "REGISTER",
                ["auto_scroll"]          = "",
                ["autoCreateAccount"]    = "",
                ["captcha"]              = captcha ?? "",
                ["captcha_id"]           = captchaId ?? "",
                ["cid"]                  = "",
                ["client_id"]            = p.ClientId,
                ["css"]                  = "",
                ["expand_ext_login_list"]= "",
                ["isInputRealname"]      = "false",
                ["password"]             = encryptedPassword,
                ["phone_captcha"]        = "",
                ["redirect_uri"]         = p.RedirectUri,
                ["ref"]                  = p.Ref,
                ["reg_mode"]             = "reg_normal",
                ["reg_req_id"]           = regReqId ?? "",
                ["response_type"]        = "TOKEN",
                ["scope"]                = "basic",
                ["sec"]                  = "1",
                ["show_4399"]            = "",
                ["show_back_button"]     = "",
                ["show_close_button"]    = "",
                ["show_ext_login"]       = "",
                ["show_forget_password"] = "",
                ["show_topbar"]          = "false",
                ["state"]                = p.State,
                ["uid"]                  = "",
                ["username"]             = username,
                ["username_history"]     = "",
            };
        }

        // ══════════════════════════════════════════════════════
        // sauth JSON 构建
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// 登录用 sauth JSON —— 对齐 Python 参考实现。
        /// </summary>
        private static string BuildSauthJson(string uid, string state)
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ" +
                        "abcdefghijklmnopqrstuvwxyz0123456789";
            var rnd = new Random();
            var udid = new string(Enumerable.Range(0, 16)
                .Select(_ => chars[rnd.Next(chars.Length)])
                .ToArray());

            var sauth = new JObject
            {
                ["aim_info"]        = "{\"aim\":\"127.0.0.1\"," +
                    "\"country\":\"CN\",\"tz\":\"+0800\"," +
                    "\"tzid\":\"\"}",
                ["realname"]        = "{\"realname_type\":2}",
                ["app_channel"]     = "4399com",
                ["platform"]        = "ad",
                ["client_login_sn"] = "4399FuckYou",
                ["gameid"]          = "x19",
                ["login_channel"]   = "4399com",
                ["sdk_version"]     = "3.12.2",
                ["sdkuid"]          = uid,
                ["sessionid"]       = state,
                ["udid"]            = udid,
                ["deviceid"]        = "4399FuckYou",
            };

            return JsonConvert.SerializeObject(sauth);
        }

        /// <summary>
        /// 注册用 sauth JSON —— 更完整的 PE 字段。
        /// </summary>
        private static string BuildSauthJsonPE(
            string uid, string state,
            string deviceJson, string username)
        {
            var sauth = new JObject
            {
                ["access_token"]       = "",
                ["aim_info"]           = "{\"aim\":\"127.0.0.1\"," +
                    "\"country\":\"CN\",\"tz\":\"+0800\"," +
                    "\"tzid\":\"Asia\\/Shanghai\"," +
                    "\"celluar_ip\":\"\",\"operator\":\"46015\"," +
                    "\"is_vpn_enabled\":false}",
                ["app_channel"]        = "4399com",
                ["client_login_sn"]    = GenerateRandomHex(32),
                ["gameid"]             = "x19",
                ["get_access_token"]   = "1",
                ["ip"]                 = "127.0.0.1",
                ["is_unisdk_guest"]    = 0,
                ["login_channel"]      = "4399com",
                ["platform"]           = "ad",
                ["realname"]           = "{\"realname_type\":2}",
                ["sdk_version"]        = "3.12.2",
                ["sdkuid"]             = uid,
                ["sessionid"]          = state,
                ["source_app_channel"] = "4399com",
                ["source_platform"]    = "ad",
                ["step"]               = RandomStep().ToString(),
                ["step2"]              = RandomStep().ToString(),
                ["udid"]               = GenerateRandomHex(16),
            };

            return JsonConvert.SerializeObject(sauth);
        }

        // ══════════════════════════════════════════════════════
        // HTML 解析工具
        // ══════════════════════════════════════════════════════

        private static string ExtractBetween(
            string source, string start, string end)
        {
            if (string.IsNullOrEmpty(source)) return null;
            var si = source.IndexOf(start, StringComparison.Ordinal);
            if (si < 0) return null;
            si += start.Length;
            if (string.IsNullOrEmpty(end))
                return source.Substring(si);
            var ei = source.IndexOf(end, si, StringComparison.Ordinal);
            return ei < 0
                ? null
                : source.Substring(si, ei - si);
        }

        private static string ExtractAttributePE(
            string html, string elementMarker, string attrName)
        {
            if (string.IsNullOrEmpty(html)) return null;
            var mi = html.IndexOf(elementMarker,
                StringComparison.Ordinal);
            if (mi < 0) return null;
            var tagStart = html.LastIndexOf('<', mi);
            if (tagStart < 0) tagStart = 0;
            var tagEnd = html.IndexOf('>', mi);
            if (tagEnd < 0) tagEnd = html.Length;
            var tag = html.Substring(tagStart,
                tagEnd - tagStart + 1);

            var m = Regex.Match(tag,
                attrName + @"\s*=\s*""([^""]*)""",
                RegexOptions.IgnoreCase);
            if (m.Success) return m.Groups[1].Value;

            m = Regex.Match(tag,
                attrName + @"\s*=\s*'([^']*)'",
                RegexOptions.IgnoreCase);
            return m.Success ? m.Groups[1].Value : null;
        }

        // ══════════════════════════════════════════════════════
        // 通用工具
        // ══════════════════════════════════════════════════════

        private static HttpClient CreateHttpClient()
        {
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = false,
            };
            var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Add(
                "User-Agent", UserAgent);
            return client;
        }

        private static byte[] Combine(params byte[][] arrays)
        {
            var total = arrays.Sum(a => a.Length);
            var result = new byte[total];
            var offset = 0;
            foreach (var a in arrays)
            {
                Buffer.BlockCopy(a, 0, result, offset, a.Length);
                offset += a.Length;
            }
            return result;
        }

        private static string GenerateRandomString(int length)
        {
            const string chars =
                "ABCDEFGHIJKLMNOPQRSTUVWXYZ" +
                "abcdefghijklmnopqrstuvwxyz0123456789";
            var rnd = new Random();
            return new string(Enumerable.Range(0, length)
                .Select(_ => chars[rnd.Next(chars.Length)])
                .ToArray());
        }

        private static string GenerateRandomHex(int length)
        {
            const string hex = "0123456789ABCDEF";
            var rnd = new Random();
            return new string(Enumerable.Range(0, length)
                .Select(_ => hex[rnd.Next(hex.Length)])
                .ToArray());
        }

        private static int RandomStep()
        {
            var buffer = new byte[4];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(buffer);
            return BitConverter.ToInt32(buffer, 0);
        }

        // ══════════════════════════════════════════════════════
        // 常量
        // ══════════════════════════════════════════════════════

        private const string UserAgent =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
            "AppleWebKit/537.36 (KHTML, like Gecko) " +
            "Chrome/129.0.0.0 Safari/537.36";

        private const string OAuth2BaseUrl =
            "https://ptlogin.4399.com/oauth2/";

        private const string RedirectUri =
            "https://m.4399api.com/openapi/oauth-callback.html" +
            "?gamekey=44770&game_key=115716";

        private const string SdkVersion = "3.12.2.503";

        // ══════════════════════════════════════════════════════
        // 内部数据类
        // ══════════════════════════════════════════════════════

        private class OAuthParams
        {
            public string ClientId;
            public string State;
            public string RedirectUri;
            public string D;
            public string BizId;
            public string Ref;
        }

        private class OAuthCallbackData
        {
            public string Uid;
            public string State;
        }

        private class ReqInfoResult
        {
            public bool   CaptchaRequired;
            public string CaptchaId;
            public string CaptchaUrl;
            public string RegReqId;
            public string Username;
        }

        // ══════════════════════════════════════════════════════
        // 结果类型
        // ══════════════════════════════════════════════════════

        public class LoginResult
        {
            public bool   Success          { get; private set; }
            public bool   CaptchaRequired  { get; private set; }
            public string ErrorMessage     { get; private set; }
            public string SauthJson        { get; private set; }
            public string CaptchaId        { get; private set; }
            public string CaptchaUrl       { get; private set; }

            public static LoginResult Ok(string sauthJson) =>
                new() { Success = true, SauthJson = sauthJson };

            public static LoginResult Fail(string error) =>
                new() { ErrorMessage = error };

            public static LoginResult CaptchaNeeded(
                string captchaId, string captchaUrl) =>
                new()
                {
                    CaptchaRequired = true,
                    CaptchaId       = captchaId,
                    CaptchaUrl      = captchaUrl,
                    ErrorMessage    = "需要验证码",
                };
        }

        public class RegisterResult
        {
            public bool   Success         { get; private set; }
            public bool   CaptchaRequired { get; private set; }
            public string ErrorMessage    { get; private set; }
            public string Username        { get; private set; }
            public string Password        { get; private set; }
            public string CaptchaId       { get; private set; }
            public string CaptchaUrl      { get; private set; }

            public static RegisterResult Ok(
                string username, string password) =>
                new()
                {
                    Success = true,
                    Username = username,
                    Password = password,
                };

            public static RegisterResult Fail(string error) =>
                new() { ErrorMessage = error };

            public static RegisterResult CaptchaNeeded(
                string captchaId, string captchaUrl) =>
                new()
                {
                    CaptchaRequired = true,
                    CaptchaId       = captchaId,
                    CaptchaUrl      = captchaUrl,
                    ErrorMessage    = "注册需要验证码",
                };
        }
    }
}
