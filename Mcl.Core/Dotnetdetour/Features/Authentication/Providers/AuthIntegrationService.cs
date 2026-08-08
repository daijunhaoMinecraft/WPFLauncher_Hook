using System;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Mcl.Core.Dotnetdetour.Features.Authentication.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Mcl.Core.Dotnetdetour.Models.Config;
using Mcl.Core.Dotnetdetour.UI.Forms;
using Mcl.Core.Dotnetdetour.Utilities.Common;
using WPFLauncher.Common;
using WPFLauncher.Manager;
using WPFLauncher.Manager.Configuration;
using WPFLauncher.Util;

namespace Mcl.Core.Dotnetdetour.Features.Authentication.Providers;

public static class CookieValidator
{
    public static bool ValidateSauth(string cookieData, out string error)
    {
        error = "";
        if (string.IsNullOrWhiteSpace(cookieData))
        {
            error = "Cookie 不能为空";
            return false;
        }

        try
        {
            var root = JObject.Parse(cookieData);
            var sauthToken = root["sauth_json"];
            
            if (sauthToken == null)
            {
                error = "JSON 中未找到 'sauth_json' 字段。";
                return false;
            }
            if (sauthToken.Type != JTokenType.String)
            {
                error = "'sauth_json' 的值必须是包含 JSON 结构的字符串类型。";
                return false;
            }

            JObject.Parse(sauthToken.ToString()); 
            return true;
        }
        catch (JsonReaderException)
        {
            error = "格式错误：提供的不是有效的 JSON 字符串。";
            return false;
        }
        catch (Exception ex)
        {
            error = $"校验异常: {ex.Message}";
            return false;
        }
    }
}

public static class AuthIntegrationService
{
    private static readonly Random _random = new();
    
    public static string RequestUserLogin(bool allowOriginal = true)
    {
        while (true)
        {
            // 声明局部变量，用于跨线程接收 WPF 窗口的返回结果
            bool dialogSuccess = false;
            bool useOriginalLogin = false;
            AccountInfo selectedAccount = null;

            // 1. 必须开启一个全新的 STA 线程来运行 WPF 界面 (DLL 注入环境的必备安全措施)
            Thread staThread = new Thread(() =>
            {
                var accountWindow = new AccountSelectionWindow();
            
                // ShowDialog() 在 WPF 中返回 bool? (true 表示确定，false/null 表示取消或关闭)
                bool? result = accountWindow.ShowDialog();
            
                // 提取我们需要的属性，赋值给外部变量
                dialogSuccess = (result == true);
                useOriginalLogin = accountWindow.UseOriginalLogin;
                selectedAccount = accountWindow.SelectedAccount;
            });

            // 强制设置为 STA (单线程单元)，WPF 运行的硬性要求
            staThread.SetApartmentState(ApartmentState.STA);
            staThread.IsBackground = true;
            staThread.Start();
        
            // 阻塞当前线程，直到用户在 WPF 窗口中做出选择并关闭窗口
            staThread.Join(); 

            // 2. 检查返回值 (对应原代码的 accountForm.ShowDialog() != DialogResult.OK)
            if (!dialogSuccess)
            {
                Environment.Exit(0);
                return null;
            }

            // 3. 处理原版登录逻辑
            if (useOriginalLogin)
            {
                if (!allowOriginal)
                {
                    // 这里的 uz.n 应该是你原有的提示框封装，保持不变
                    uz.n("当前模式不可使用原号登录，请重新选择");
                    continue;
                }
                return null;
            }

            // 4. 处理选择的账号逻辑
            if (selectedAccount != null)
            {
                string sauthJson = ExtractSauth(selectedAccount);
                if (string.IsNullOrEmpty(sauthJson))
                {
                    WpfConfig.DefaultLogger.Error("账号凭证提取失败，请重试");
                    continue;
                }
                return sauthJson;
            }
        }
    }

    public static string ExtractSauth(AccountInfo acc)
    {
        try
        {
            return acc.Type switch
            {
                AccountType.Cookie => SauthParser.ExtractFromCookie(acc.CookieData) ?? acc.CookieData,
                AccountType.Phone => SauthParser.ExtractFromPhoneAccount(acc),
                AccountType.Email => MpayLogin.EmailLoginFlow(acc.Username, acc.Password),
                AccountType._4399 => Parse4399Account(acc),
                _ => null
            };
        }
        catch (Exception ex)
        {
            WpfConfig.DefaultLogger.Error($"凭证提取异常: {ex}");
            return null;
        }
    }

    private static string Parse4399Account(AccountInfo acc)
    {
        var rawResult = SauthParser.ExtractFrom4399Login($"{acc.Username}----{acc.Password}");
        if (string.IsNullOrEmpty(rawResult)) return null;

        try
        {
            var jsonObj = JObject.Parse(rawResult);
            if (jsonObj["sauth_json"] != null) return jsonObj["sauth_json"].ToString();
        }
        catch { /* ignore parse error, return raw */ }
        
        return rawResult;
    }

    public static void InjectMpayCookie(string sauthContent)
    {
        try
        {
            azf<apm>.Instance.CanChannelLogin = true;
            object arfInstance = azf<arf>.Instance;
            var arfType = typeof(arf);

            arfType.GetField("i", BindingFlags.Public | BindingFlags.Instance)?.SetValue(arfInstance, true);
            
            azf<axi>.Instance.App.UDID = GenerateRandomString();
            azf<axi>.Instance.App.DeviceId = GenerateRandomString();

            var fieldD = arfType.GetField("d", BindingFlags.Public | BindingFlags.Instance);
            if (fieldD != null)
            {
                fieldD.SetValue(arfInstance, sauthContent);
                WpfConfig.DefaultLogger.Info("MPay 状态与 Cookie 注入成功");
            }

            WpfConfig.CookieLoginWithoutMpay = true;
            WpfConfig.IsLogin = true;
            azf<apm>.Instance.h();
        }
        catch (Exception ex)
        {
            WpfConfig.DefaultLogger.Error($"Cookie注入失败: {ex}");
            throw;
        }
    }

    private static string GenerateRandomString(int length = 32)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var result = new StringBuilder(length);
        for (int i = 0; i < length; i++) result.Append(chars[_random.Next(chars.Length)]);
        return result.ToString();
    }
}