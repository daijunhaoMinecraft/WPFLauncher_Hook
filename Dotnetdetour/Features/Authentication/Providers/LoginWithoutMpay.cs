using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using Mcl.Core.Dotnetdetour.Tools;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WPFLauncher.Common;
using WPFLauncher.Manager;
using WPFLauncher.Sdk.MPay;
using WPFLauncher.Util;

namespace Mcl.Core.Dotnetdetour.HookList;

public class LoginWithoutMpay : IMethodHook
{
	    private static readonly Random random = new Random();
    private static readonly char[] chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();
    
    /// <summary>
    /// 生成指定长度的随机数字+大写字母字符串
    /// </summary>
    /// <param name="length">字符串长度，默认32</param>
    /// <returns>随机字符串</returns>
    public static string GenerateRandomString(int length = 32)
    {
        StringBuilder result = new StringBuilder(length);
        for (int i = 0; i < length; i++)
        {
            result.Append(chars[random.Next(chars.Length)]);
        }
        return result.ToString();
    }
    
    private static void InjectMpayCookie(string sauthContent)
    {
        try
        {
            // Bypass
            WPFLauncher.Common.azf<apm>.Instance.CanChannelLogin = true;
            // 反
            object arfInstance = WPFLauncher.Common.azf<arf>.Instance;

            // 安全地通过反射修改字段 i
            Type arfType = typeof(arf);
            FieldInfo fieldI = arfType.GetField("i", BindingFlags.Public | BindingFlags.Instance);
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

            string randomUDID = GenerateRandomString();
            string randomDeviceId = GenerateRandomString();
            WPFLauncher.Common.azf<WPFLauncher.Manager.Configuration.axi>.Instance.App.UDID = randomUDID;
            WPFLauncher.Common.azf<WPFLauncher.Manager.Configuration.axi>.Instance.App.DeviceId = randomDeviceId;

            FieldInfo fieldD = arfType.GetField("d", BindingFlags.Public | BindingFlags.Instance);
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
            WPFLauncher.Common.azf<apm>.Instance.h();
        }
        catch (Exception ex)
        {
            WpfConfig.DefaultLogger.Error($"Cookie注入失败, 使用原号登录: \n{ex}");
        }
    }
	
    [OriginalMethod]
    public void InitMpay(string title, string mpayPath, Action<int> initFinishAction, string uniSdkUrl)
    {
    }
    
    [HookMethod("WPFLauncher.Unisdk.nx", "a", "InitMpay")]
    public void InitMpayHook(string title, string mpayPath, Action<int> initFinishAction, string uniSdkUrl)
    {
        if (WpfConfig.MpayUnless)
        {
            initFinishAction(0);
            while (true)
            {
	            // 打开多账号管理器
	            var accountForm = new AccountSelectForm();
	            accountForm.ShowDialog();

	            if (accountForm.Action == AccountSelectForm.LoginAction.UseSelected && accountForm.SelectedAccount != null)
	            {
		            LoginWithSaved(accountForm.SelectedAccount);
		            break;
	            }
	            if (accountForm.Action == AccountSelectForm.LoginAction.ManualInput)
	            {
		            DoManualCookieInput();
	            }
	            else if (accountForm.Action == AccountSelectForm.LoginAction.UseOriginal)
	            {
		            uz.n("你不能使用此选项, 请重新选择", "");
	            }
	            else if (accountForm.Action == AccountSelectForm.LoginAction.Exit)
	            {
		            Environment.Exit(0);
	            }
            }
            azf<apm>.Instance.CanChannelLogin = true;
        }
        else
        {
            InitMpay(title, mpayPath, initFinishAction, uniSdkUrl);
        }
    }

    public static void LoginWithSaved(AccountInfo acc)
    {
        string text9 = "";

			if (acc.Type == AccountType._4399)
			{
				try
				{
					var loginResult = Task.Run(() =>
						_4399.LoginAsync(acc.Username, acc.Password)).Result;
					if (loginResult.Success)
					{
						text9 = JObject.Parse(loginResult.SauthJson)["sauth_json"].ToString();
						// text9 = loginResult.SauthJson;
						WpfConfig.DefaultLogger.Info("4399:" + acc.Username);
					}
					else
					{
						uz.n("4399登录失败: " + (loginResult.ErrorMessage ?? "未知错误"), "");
						return;
					}
				}
				catch (Exception ex)
				{
					WpfConfig.DefaultLogger.Error($"4399账号转换失败，切换为原号登录: \n{ex}");
					return;
				}
			}
			else if (acc.Type == AccountType.Phone)
			{
				// 手机号登录 — 先检查是否有缓存的 Cookie
				if (!string.IsNullOrEmpty(acc.CookieData) && acc.CookieData.Contains("\"sauth_json\""))
				{
					// 有缓存的 sauth_json，尝试直接使用
					try
					{
						text9 = JObject.Parse(acc.CookieData)["sauth_json"].ToString();
						WpfConfig.DefaultLogger.Info($"[Phone] 使用缓存凭证 {acc.PhoneNumber}");
					}
					catch
					{
						text9 = null;
					}
				}

				if (string.IsNullOrEmpty(text9))
				{
					// 需要完整手机号登录流程
					WpfConfig.DefaultLogger.Info($"[Phone] 开始手机号登录: {acc.PhoneNumber}");
					string sauthJson = MpayPhoneLogin.FullLoginFlow(acc.PhoneNumber, acc.DeviceId);

					if (string.IsNullOrEmpty(sauthJson))
					{
						WpfConfig.DefaultLogger.Error("手机号登录失败，切换为原号登录");
						return;
					}

					// 缓存 sauth_json 到账号
					acc.CookieData = sauthJson;
					acc.DeviceId = MpayPhoneLogin.GetOrRegisterDevice(acc.DeviceId);
					AccountManager.Update(acc.Name, acc);

					try
					{
						text9 = JObject.Parse(sauthJson)["sauth_json"].ToString();
					}
					catch
					{
						text9 = sauthJson;
					}
				}
			}
			else // Cookie
			{
				string cookieData = acc.CookieData;
				if (cookieData.StartsWith("{"))
				{
					try
					{
						if (cookieData.Contains("\"sauth_json\":"))
						{
							text9 = JObject.Parse(cookieData)["sauth_json"].ToString();
						}
						else
						{
							text9 = cookieData;
						}
					}
					catch (Exception)
					{
						if (cookieData.Contains("sauth_json"))
						{
							text9 = new Regex("\\\"sauth_json\\\":\\\"(.*?)\\\"}\\\"}").Match(cookieData).Groups[1].Value + "\"}";
						}
					}
				}
				if (string.IsNullOrEmpty(text9))
				{
					text9 = cookieData;
				}
				WpfConfig.DefaultLogger.Info("cookies:" + cookieData);
			}

			if (string.IsNullOrEmpty(text9))
			{
				WpfConfig.DefaultLogger.Error("账号凭证有误，切换为原号登录");
				return;
			}

			if (WpfConfig.IsStartWebSocket)
			{
				WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new { type = "Login", cookie = new { sauth_json = text9 } }));
			}
			WpfConfig.DefaultLogger.Info($"SauthJson: {JsonConvert.SerializeObject(new { sauth_json = text9 })}");
			InjectMpayCookie(text9);
			WpfConfig.IsLogin = true;
    }
    
    [HookMethod("WPFLauncher.Manager.PCChannel.asx", "a", null)]
    public bool IsNeteaseChannel()
    {
	    return true;
    }

    [HookMethod("WPFLauncher.Manager.arf", "i", null)]
    public bool CanLogin()
    {
	    return true;
    }

    [OriginalMethod]
    public void ProcessLogout()
    {
	    
    }

    [HookMethod("WPFLauncher.Manager.arf", "j", "ProcessLogout")]
    public void ProcessLogoutHook()
    {
	    WpfConfig.DefaultLogger.Info("Logout!");
	    ProcessLogout();
	    if (WpfConfig.MpayUnless)
	    {
		    while (true)
		    {
			    // 打开多账号管理器
			    var accountForm = new AccountSelectForm();
			    accountForm.ShowDialog();

			    if (accountForm.Action == AccountSelectForm.LoginAction.UseSelected && accountForm.SelectedAccount != null)
			    {
				    LoginWithSaved(accountForm.SelectedAccount);
				    break;
			    }

			    if (accountForm.Action == AccountSelectForm.LoginAction.ManualInput)
			    {
				    DoManualCookieInput();
			    }
			    else if (accountForm.Action == AccountSelectForm.LoginAction.UseOriginal)
			    {
				    uz.n("你不能使用此选项, 请重新选择", "");
			    }
			    else if (accountForm.Action == AccountSelectForm.LoginAction.Exit)
			    {
				    Environment.Exit(0);
			    }
		    }
		    azf<apm>.Instance.CanChannelLogin = true;
	    }
    }
    
    /// <summary>
    /// 旧版手动输入 Cookie 流程（兼容保留）
    /// </summary>
    private void DoManualCookieInput()
    {
	    string text3 = "cookies.txt";
	    string filePath = Path.Combine(Environment.CurrentDirectory, text3);
	    string readCookie = "";
	    if (File.Exists(filePath))
	    {
		    readCookie = File.ReadAllText(filePath);
	    }
	    string cookie = Interaction.InputBox("请输入Cookies"/*\n自动获取cookie请输入1"*/, "Cookies", readCookie, -1, -1);
	    WpfConfig.DefaultLogger.Info("cookies:" + cookie);
	    File.WriteAllText(filePath, cookie);

	    // 尝试解析 sauth_json
	    string sauthContent = cookie;
	    if (cookie.StartsWith("{"))
	    {
		    try
		    {
			    if (cookie.Contains("\"sauth_json\":"))
			    {
				    sauthContent = JObject.Parse(cookie)["sauth_json"].ToString();
			    }
		    }
		    catch (Exception)
		    {
			    if (cookie.Contains("sauth_json"))
			    {
				    sauthContent = new Regex("\\\"sauth_json\\\":\\\"(.*?)\\\"}\\\"}").Match(cookie).Groups[1].Value + "\"}";
			    }
		    }
	    }

	    InjectMpayCookie(sauthContent);
    }
}