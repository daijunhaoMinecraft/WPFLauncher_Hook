using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;
using Mcl.Core.Dotnetdetour.Features.Authentication.Providers;
using Mcl.Core.Dotnetdetour.Models.Config;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WPFLauncher.Common;
using WPFLauncher.Manager;
using WPFLauncher.Manager.Configuration;
using WPFLauncher.Sdk.MPay;
using WPFLauncher.Util;
using Mcl.Core.Dotnetdetour.Utilities.Common;

namespace Mcl.Core.Dotnetdetour.Features.Authentication.Core;

//去除网易实名认证 + 集成多账号管理
internal class MpayLoginAccount : IMethodHook
{
    private static readonly Random random = new();
    private static readonly char[] chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();

    public static string GenerateRandomString(int length = 32)
    {
        var result = new StringBuilder(length);
        for (var i = 0; i < length; i++) result.Append(chars[random.Next(chars.Length)]);
        return result.ToString();
    }

    [OriginalMethod]
    protected void No_RealName(int code)
    {
    }

    [CompilerGenerated]
    [HookMethod("WPFLauncher.Unisdk.nx", "onLoginFinish", "No_RealName")]
    protected async void onLoginFinish(int code)
    {
        if (WpfConfig.IsDebug) WpfConfig.DefaultLogger.Info("[MpayLogin]返回代码: " + code);

        No_RealName(code);
    }

    private void LoginWithSavedAccountInMpay(AccountInfo acc)
    {
        var sauthContent = ExtractSauthFromAccount(acc);

        if (string.IsNullOrEmpty(sauthContent))
        {
            WpfConfig.DefaultLogger.Error("账号凭证提取失败，使用原号登录");
            No_RealName(1);
            return;
        }

        // 标记账号使用
        AccountManager.MarkUsed(acc);

        // 注入 Mpay 状态
        InjectMpayCookie(sauthContent);
    }

    private string ExtractSauthFromAccount(AccountInfo acc)
    {
        var sauthContent = "";

        if (acc.Type == AccountType.Cookie)
        {
            sauthContent = SauthParser.ExtractFromCookie(acc.CookieData);
            WpfConfig.DefaultLogger.Info("cookies:" + acc.CookieData);
        }
        else if (acc.Type == AccountType._4399)
        {
            try
            {
                var loginResult = Task.Run(() =>
                    _4399.LoginAsync(acc.Username, acc.Password)).Result;
                if (loginResult.Success && loginResult.SauthJson.StartsWith("{"))
                {
                    try
                    {
                        sauthContent = JObject.Parse(loginResult.SauthJson)["sauth_json"].ToString();
                    }
                    catch (Exception ex)
                    {
                        WpfConfig.DefaultLogger.Info($"解析Json失败: {ex}");
                        sauthContent = loginResult.SauthJson;
                    }

                    WpfConfig.DefaultLogger.Info("4399:" + acc.Username);
                }
                else
                {
                    uz.n("4399登录失败: " + (loginResult.ErrorMessage ?? "未知错误"));
                    return null;
                }
            }
            catch (Exception ex)
            {
                WpfConfig.DefaultLogger.Error($"4399账号转换失败, \n{ex}");
                return null;
            }
        }
        else if (acc.Type == AccountType.Phone)
        {
            sauthContent = SauthParser.ExtractFromPhoneAccount(acc);
        }

        return sauthContent;
    }

    private void InjectMpayCookie(string sauthContent)
    {
        try
        {
            // Bypass
            azf<apm>.Instance.CanChannelLogin = true;
            // 反
            object arfInstance = azf<arf>.Instance;

            // 安全地通过反射修改字段 i
            var arfType = typeof(arf);
            var fieldI = arfType.GetField("i", BindingFlags.Public | BindingFlags.Instance);
            if (fieldI != null)
            {
                fieldI.SetValue(arfInstance, true);
                WpfConfig.DefaultLogger.Info("设置bool成功");
            }

            // 构建 SauthJsonEntity 并注入
            SauthJsonEntity sauthJsonEntity;
            try
            {
                // 尝试从 sauthContent 反序列化为 SauthJsonEntity
                sauthJsonEntity = JsonConvert.DeserializeObject<SauthJsonEntity>(sauthContent);
            }
            catch
            {
                // 如果反序列化失败，创建新的并设置基本字段
                sauthJsonEntity = new SauthJsonEntity();
            }

            var randomUDID = GenerateRandomString();
            var randomDeviceId = GenerateRandomString();
            azf<axi>.Instance.App.UDID = randomUDID;
            azf<axi>.Instance.App.DeviceId = randomDeviceId;

            var fieldD = arfType.GetField("d", BindingFlags.Public | BindingFlags.Instance);
            if (fieldD != null)
            {
                fieldD.SetValue(arfInstance, sauthContent);
                WpfConfig.DefaultLogger.Info("设置Cookie成功");
            }
            else
            {
                // 尝试序列化整个 SauthJsonEntity
                fieldD = arfType.GetField("d", BindingFlags.Public | BindingFlags.Instance);
                if (fieldD != null)
                {
                    fieldD.SetValue(arfInstance, JsonConvert.SerializeObject(sauthJsonEntity));
                    WpfConfig.DefaultLogger.Info("设置Cookie成功(Entity)");
                }
            }

            WpfConfig.CookieLoginWithoutMpay = true;
            WpfConfig.IsLogin = true;
            azf<apm>.Instance.h();
        }
        catch (Exception ex)
        {
            WpfConfig.DefaultLogger.Error($"Cookie注入失败, 使用原号登录: \n{ex}");
            No_RealName(1);
        }
    }

    private void DoManualCookieInput()
    {
        var text3 = "cookies.txt";
        var filePath = Path.Combine(Environment.CurrentDirectory, text3);
        var readCookie = "";
        if (File.Exists(filePath)) readCookie = File.ReadAllText(filePath);
        var cookie = Interaction.InputBox("请输入Cookies" /*\n自动获取cookie请输入1"*/, "Cookies", readCookie);
        WpfConfig.DefaultLogger.Info("cookies:" + cookie);
        File.WriteAllText(filePath, cookie);

        var sauthContent = SauthParser.ExtractFromCookie(cookie);

        InjectMpayCookie(sauthContent);
    }
}