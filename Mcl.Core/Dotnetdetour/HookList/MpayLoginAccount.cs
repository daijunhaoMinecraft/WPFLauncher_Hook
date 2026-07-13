using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using Mcl.Core.Dotnetdetour.Tools;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WPFLauncher.Manager;
using WPFLauncher.Sdk.MPay;
using WPFLauncher.Util;

namespace Mcl.Core.Dotnetdetour.HookList
{
	//去除网易实名认证 + 集成多账号管理
	internal class MpayLoginAccount : IMethodHook
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

		[OriginalMethod]
		protected void No_RealName(int code)
		{
		}

		[CompilerGenerated]
		[HookMethod("WPFLauncher.Unisdk.nx", "onLoginFinish", "No_RealName")]
		protected async void onLoginFinish(int code)
		{
			if (WpfConfig.IsDebug)
			{
				WpfConfig.DefaultLogger.Info("[MpayLogin]返回代码: " + code.ToString());
			}

			if (code == 1)
			{
				//玩家取消登录 — 显示多账号管理界面
				MessageBoxResult messageBoxResult = uz.q("是否使用多账号登录?", "", "多账号管理", "取消", "");
				if (messageBoxResult == MessageBoxResult.OK)
				{
					// 打开多账号管理器
					var accountForm = new AccountSelectForm();
					accountForm.ShowDialog();

					if (accountForm.Action == AccountSelectForm.LoginAction.UseSelected && accountForm.SelectedAccount != null)
					{
						// 使用选中的已保存账号
						LoginWithSavedAccountInMpay(accountForm.SelectedAccount);
					}
					else if (accountForm.Action == AccountSelectForm.LoginAction.ManualInput)
					{
						// 手动输入（兼容旧流程）
						DoManualCookieInput();
					}
					else
					{
						// 使用原号登录或取消
						No_RealName(1);
					}
				}
				else
				{
					No_RealName(1);
				}

				if (WpfConfig.IsLogin)
				{
					No_RealName(0);
				}
				else
				{
					No_RealName(1);
				}
			}
			No_RealName(0);
		}

		/// <summary>
		/// 使用已保存的账号在 Mpay 流程中登录
		/// 从账号中提取 sauth_json 并注入 Mpay 内部状态
		/// </summary>
		private void LoginWithSavedAccountInMpay(AccountInfo acc)
		{
			string sauthContent = ExtractSauthFromAccount(acc);

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

		/// <summary>
		/// 从账号中提取 sauth_json 内容
		/// 支持 Cookie、4399、Phone 三种类型
		/// </summary>
		private string ExtractSauthFromAccount(AccountInfo acc)
		{
			string sauthContent = "";

			if (acc.Type == AccountType.Cookie)
			{
				string cookieData = acc.CookieData;
				if (cookieData.StartsWith("{"))
				{
					try
					{
						if (cookieData.Contains("\"sauth_json\":"))
						{
							sauthContent = JObject.Parse(cookieData)["sauth_json"].ToString();
						}
						else
						{
							sauthContent = cookieData;
						}
					}
					catch (Exception)
					{
						if (cookieData.Contains("sauth_json"))
						{
							sauthContent = new Regex("\\\"sauth_json\\\":\\\"(.*?)\\\"}\\\"}").Match(cookieData).Groups[1].Value + "\"}";
						}
					}
				}
				if (string.IsNullOrEmpty(sauthContent))
				{
					sauthContent = cookieData;
				}
				WpfConfig.DefaultLogger.Info("cookies:" + cookieData);
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
						uz.n("4399登录失败: " + (loginResult.ErrorMessage ?? "未知错误"), "");
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
				// 手机号登录 — 检查缓存或执行完整登录流程
				if (!string.IsNullOrEmpty(acc.CookieData) && acc.CookieData.Contains("\"sauth_json\""))
				{
					try
					{
						sauthContent = JObject.Parse(acc.CookieData)["sauth_json"].ToString();
						WpfConfig.DefaultLogger.Info($"[Phone] 使用缓存凭证 {acc.PhoneNumber}");
					}
					catch
					{
						sauthContent = null;
					}
				}

				if (string.IsNullOrEmpty(sauthContent))
				{
					WpfConfig.DefaultLogger.Info($"[Phone] 开始手机号登录: {acc.PhoneNumber}");
					string result = MpayPhoneLogin.FullLoginFlow(acc.PhoneNumber, acc.DeviceId);

					if (string.IsNullOrEmpty(result))
					{
						WpfConfig.DefaultLogger.Error("手机号登录失败");
						return null;
					}

					// 缓存凭证
					acc.CookieData = result;
					acc.DeviceId = MpayPhoneLogin.GetOrRegisterDevice(acc.DeviceId);
					AccountManager.Update(acc.Name, acc);

					try
					{
						sauthContent = JObject.Parse(result)["sauth_json"].ToString();
					}
					catch
					{
						sauthContent = result;
					}
				}
			}

			return sauthContent;
		}

		/// <summary>
		/// 将 sauth_json 注入 Mpay 内部状态，并重启登录流程
		/// </summary>
		private void InjectMpayCookie(string sauthContent)
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
				No_RealName(1);
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
}
