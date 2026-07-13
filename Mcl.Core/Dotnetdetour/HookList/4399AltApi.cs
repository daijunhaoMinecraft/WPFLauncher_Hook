using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NLog;

namespace Mcl.Core.Dotnetdetour.HookList
{
    internal static class _4399AltApi
    {
        public const string ProfileUrl = "https://4399.nekoinsi.de/profile";

        private const string ApiUrl = "https://4399.nekoinsi.de/api/alt";
        private const string ApiKeyFile = "4399_alt_apikey.txt";

        public static string LoadApiKey()
        {
            try
            {
                string filePath = Path.Combine(Environment.CurrentDirectory, ApiKeyFile);
                return File.Exists(filePath) ? File.ReadAllText(filePath).Trim() : string.Empty;
            }
            catch (Exception ex)
            {
                WpfConfig.DefaultLogger.Error($"[4399AltApi] 读取ApiKey失败: {ex.Message}");
                return string.Empty;
            }
        }

        public static void SaveApiKey(string apiKey)
        {
            try
            {
                string filePath = Path.Combine(Environment.CurrentDirectory, ApiKeyFile);
                File.WriteAllText(filePath, (apiKey ?? string.Empty).Trim());
            }
            catch (Exception ex)
            {
                WpfConfig.DefaultLogger.Error($"[4399AltApi] 保存ApiKey失败: {ex.Message}");
                throw;
            }
        }

        public static bool OpenProfilePage(out string errorMessage)
        {
            try
            {
                Process.Start(new ProcessStartInfo(ProfileUrl) { UseShellExecute = true });
                errorMessage = null;
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                WpfConfig.DefaultLogger.Error($"[4399AltApi] 打开ApiKey页面失败: {ex.Message}");
                return false;
            }
        }

        public static async Task<AltAccountResult> FetchAltAsync()
        {
            return await FetchAltAsync(LoadApiKey()).ConfigureAwait(false);
        }

        public static async Task<AltAccountResult> FetchAltAsync(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return AltAccountResult.Fail("请先填写4399小号ApiKey");
            }

            try
            {
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

                using (var client = new HttpClient())
                using (var request = new HttpRequestMessage(HttpMethod.Get, ApiUrl))
                {
                    request.Headers.TryAddWithoutValidation("Authorization", "Ciallo " + apiKey.Trim());

                    var response = await client.SendAsync(request).ConfigureAwait(false);
                    string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    JObject json;
                    try
                    {
                        json = JObject.Parse(body);
                    }
                    catch
                    {
                        return AltAccountResult.Fail($"API返回内容解析失败: HTTP {(int)response.StatusCode}");
                    }

                    int code = json["code"]?.Value<int>() ?? -1;
                    string message = json["message"]?.ToString() ?? "未知错误";
                    if (code != 0)
                    {
                        return AltAccountResult.Fail(message);
                    }

                    var data = json["data"] as JArray;
                    string rawAccount = data != null && data.Count > 0 ? data[0]?.ToString() : null;
                    if (string.IsNullOrWhiteSpace(rawAccount))
                    {
                        return AltAccountResult.Fail("API未返回可用小号");
                    }

                    int separatorIndex = rawAccount.IndexOf("----", StringComparison.Ordinal);
                    if (separatorIndex <= 0 || separatorIndex + 4 >= rawAccount.Length)
                    {
                        return AltAccountResult.Fail("API返回的小号格式无效");
                    }

                    string username = rawAccount.Substring(0, separatorIndex);
                    string password = rawAccount.Substring(separatorIndex + 4);
                    return AltAccountResult.Ok(username, password, rawAccount);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[4399AltApi] 获取小号失败: {ex}");
                return AltAccountResult.Fail(ex.Message);
            }
        }

        public class AltAccountResult
        {
            public bool Success { get; private set; }
            public string ErrorMessage { get; private set; }
            public string Username { get; private set; }
            public string Password { get; private set; }
            public string RawAccount { get; private set; }

            public static AltAccountResult Ok(string username, string password, string rawAccount)
            {
                return new AltAccountResult
                {
                    Success = true,
                    Username = username,
                    Password = password,
                    RawAccount = rawAccount
                };
            }

            public static AltAccountResult Fail(string errorMessage)
            {
                return new AltAccountResult
                {
                    Success = false,
                    ErrorMessage = errorMessage
                };
            }
        }
    }
}