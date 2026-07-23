using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using Mcl.Core.Dotnetdetour.Tools;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WPFLauncher.Code;
using WPFLauncher.Network.Launcher;
using WPFLauncher.Util;
using MessageBox = System.Windows.MessageBox;

namespace Mcl.Core.Dotnetdetour.HookList
{
	// Token: 0x02000017 RID: 23
	public class LoginFix : IMethodHook
	{
		[OriginalMethod]
		public static void f(string hud, Action<EntityResponse<acl.Resposne>, Exception> hue = null)
		{
		}

		[CompilerGenerated]
		[HookMethod("WPFLauncher.Network.Launcher.acp", "g", "g")]
		public static async Task g(string hud, Action<EntityResponse<acl.Resposne>, Exception> hue)
		{
			if (WpfConfig.EnableCustomAccountLogin && !WpfConfig.MpayUnless)
			{
				MessageBoxResult messageBoxResult;
				if (WpfConfig.CookieLoginWithoutMpay)
				{
					messageBoxResult = MessageBoxResult.Cancel;
				}
				else
				{
					messageBoxResult = uz.q("是否使用Cookie登录?", "", "多账号管理", "使用原号登录", "");
				}
				if (messageBoxResult == MessageBoxResult.OK)
				{
					// 多账号管理器
					var accountForm = new AccountSelectForm();
					accountForm.ShowDialog();

					if (accountForm.Action == AccountSelectForm.LoginAction.UseSelected && accountForm.SelectedAccount != null)
					{
						// 使用选中的已保存账号
						LoginWithSavedAccount(accountForm.SelectedAccount, hud, hue);
					}
					else if (accountForm.Action == AccountSelectForm.LoginAction.ManualInput)
					{
						// 手动输入（兼容旧流程）
						DoManualLogin(hud, hue);
					}
					else
					{
						// 使用原号登录
						LoginWithOriginal(hud, hue);
					}
				}
				else if (messageBoxResult == MessageBoxResult.None)
				{
					LoginWithOriginal(hud, hue);
				}
				else
				{
					LoginWithOriginal(hud, hue);
				}
			}
			else
			{
				LoginWithOriginal(hud, hue);
			}
		}

		/// <summary>
		/// 使用已保存的账号登录
		/// </summary>
		private static void LoginWithSavedAccount(AccountInfo acc, string hud, Action<EntityResponse<acl.Resposne>, Exception> hue)
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
						LoginWithOriginal(hud, hue);
						return;
					}
				}
				catch (Exception ex)
				{
					WpfConfig.DefaultLogger.Error($"4399账号转换失败，切换为原号登录: \n{ex}");
					LoginWithOriginal(hud, hue);
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
						LoginWithOriginal(hud, hue);
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
				LoginWithOriginal(hud, hue);
				return;
			}

			if (WpfConfig.IsStartWebSocket)
			{
				WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new { type = "Login", cookie = new { sauth_json = text9 } }));
			}
			WpfConfig.DefaultLogger.Info($"SauthJson: {JsonConvert.SerializeObject(new { sauth_json = text9 })}");
			WpfConfig.IsLogin = true;
			LoginFix.f(text9, hue);
		}

		/// <summary>
		/// 使用原号登录（不替换Cookie）
		/// </summary>
		private static void LoginWithOriginal(string hud, Action<EntityResponse<acl.Resposne>, Exception> hue)
		{
			if (WpfConfig.IsStartWebSocket)
			{
				WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new { type = "Login", cookie = new { sauth_json = hud } }));
			}
			WpfConfig.DefaultLogger.Info($"SauthJson:{JsonConvert.SerializeObject(new { sauth_json = hud })}");
			WpfConfig.IsLogin = true;
			LoginFix.f(hud, hue);
		}

		/// <summary>
		/// 手动输入登录（兼容旧版InputBox流程）
		/// </summary>
		private static void DoManualLogin(string hud, Action<EntityResponse<acl.Resposne>, Exception> hue)
		{
			string text = "";
			bool loginby4399 = false;
			string text2 = "";
			if (uz.r("请选择cookie登录或4399登录", string.Empty, "cookie", "4399", "") == MessageBoxResult.Yes)
			{
				text = Interaction.InputBox("请输入Cookies \n如果原号登陆请输入off或者空", "Cookies", text, -1, -1);
			}
			else
			{
				try
				{
					text2 = Interaction.InputBox("请输入4399账号 \n如果原号登陆请输入off或者空\n4399账号格式为 xxxx----xxxx(4个减号) \n例如UserName----UserPassword", "4399LOGIN", text2, -1, -1);
					loginby4399 = true;
					if (!(text2 == "off") && !(text2 == ""))
					{
						string[] array = text2.Split(new string[] { "----" }, StringSplitOptions.None);
						string text6 = array[0];
						string text7 = array[1];
						var loginResult2 = Task.Run(() =>
							_4399.LoginAsync(text6, text7)).Result;
						if (loginResult2.Success)
							text = JObject.Parse(loginResult2.SauthJson)["sauth_json"].ToString();
							// text = loginResult2.SauthJson;
						else
							uz.n("4399登录失败: " + (loginResult2.ErrorMessage ?? "未知错误"), "");
					}
				}
				catch (Exception ex)
				{
					WpfConfig.DefaultLogger.Error(ex);
					LoginWithOriginal(hud, hue);
					return;
				}
			}
			string text9 = "";
			if (text.StartsWith("{"))
			{
				try
				{
					if (text.Contains("\"sauth_json\":"))
					{
						text9 = JObject.Parse(text)["sauth_json"].ToString();
					}
					else
					{
						text9 = text;
					}
				}
				catch (Exception)
				{
					if (text.Contains("sauth_json"))
					{
						text9 = new Regex("\\\"sauth_json\\\":\\\"(.*?)\\\"}\\\"}").Match(text).Groups[1].Value + "\"}";
					}
				}
			}
			if (string.IsNullOrEmpty(text9) && !text.StartsWith("{"))
			{
				WpfConfig.DefaultLogger.Error("cookies有误");
				if (loginby4399)
				{
					text2 = "";
				}
				else
				{
					text = "";
				}
			}
			if (loginby4399)
			{
				if (!(text2 == "off") && !(text2 == ""))
				{
					WpfConfig.DefaultLogger.Info("4399:" + text2);
					LoginWithCookieData(text9, hue);
				}
				else
				{
					WpfConfig.DefaultLogger.Info("选择原号登录(原因：玩家填入off或空)");
					LoginWithOriginal(hud, hue);
				}
			}
			else if (!(text == "off") && !(text == ""))
			{
				WpfConfig.DefaultLogger.Info("cookies:" + text);
				LoginWithCookieData(text9, hue);
			}
			else
			{
				WpfConfig.DefaultLogger.Info("选择原号登录(原因：玩家填入off或空)");
				LoginWithOriginal(hud, hue);
			}
		}

		/// <summary>
		/// 用Cookie数据登录（WebSocket通知 + 调用原始登录方法）
		/// </summary>
		private static void LoginWithCookieData(string text9, Action<EntityResponse<acl.Resposne>, Exception> hue)
		{
			if (WpfConfig.IsStartWebSocket)
			{
				WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new { type = "Login", cookie = new { sauth_json = text9 } }));
			}
			WpfConfig.DefaultLogger.Info($"SauthJson: {JsonConvert.SerializeObject(new { sauth_json = text9 })}");
			WpfConfig.IsLogin = true;
			LoginFix.f(text9, hue);
		}
	}
}
