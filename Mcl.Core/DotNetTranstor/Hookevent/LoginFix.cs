using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using WPFLauncher.Code;
using WPFLauncher.Network.Launcher;
using WPFLauncher.Util;
using System.Windows;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WPFLauncher.Manager.PCChannel;
using WPFLauncher.Network.Message;
using Mcl.Core.DotNetTranstor.Tools;
using MessageBox = System.Windows.MessageBox;
using MicrosoftTranslator.DotNetTranstor.Tools;

namespace DotNetTranstor.Hookevent
{
	// Token: 0x02000017 RID: 23
	internal class LoginFix : IMethodHook
	{
		[OriginalMethod]
		public static void f(string hud, Action<EntityResponse<acl.Resposne>, Exception> hue = null)
		{
		}

		[CompilerGenerated]
		[HookMethod("WPFLauncher.Network.Launcher.acp", "g", "g")]
		public static async Task g(string hud, Action<EntityResponse<acl.Resposne>, Exception> hue)
		{
			MessageBoxResult messageBoxResult;
			if (Path_Bool.CookieLoginWithoutMpay)
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
						Tool.PrintYellow("4399:" + acc.Username);
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
					Console.WriteLine(ex.ToString());
					Tool.PrintRed("4399账号转换失败，切换为原号登录");
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
						Tool.PrintYellow($"[Phone] 使用缓存凭证 {acc.PhoneNumber}");
					}
					catch
					{
						text9 = null;
					}
				}

				if (string.IsNullOrEmpty(text9))
				{
					// 需要完整手机号登录流程
					Tool.PrintYellow($"[Phone] 开始手机号登录: {acc.PhoneNumber}");
					string sauthJson = MpayPhoneLogin.FullLoginFlow(acc.PhoneNumber, acc.DeviceId);

					if (string.IsNullOrEmpty(sauthJson))
					{
						Tool.PrintRed("手机号登录失败，切换为原号登录");
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
				Tool.PrintYellow("cookies:" + cookieData);
			}

			if (string.IsNullOrEmpty(text9))
			{
				Tool.PrintRed("账号凭证有误，切换为原号登录");
				LoginWithOriginal(hud, hue);
				return;
			}

			if (Path_Bool.IsStartWebSocket)
			{
				WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new { type = "Login", cookie = new { sauth_json = text9 } }));
			}
			Console.WriteLine($"[INFO]当前登录账号Cookie内容:{JsonConvert.SerializeObject(new { sauth_json = text9 })}");
			Path_Bool.IsLogin = true;
			LoginFix.f(text9, hue);
		}

		/// <summary>
		/// 使用原号登录（不替换Cookie）
		/// </summary>
		private static void LoginWithOriginal(string hud, Action<EntityResponse<acl.Resposne>, Exception> hue)
		{
			if (Path_Bool.IsStartWebSocket)
			{
				WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new { type = "Login", cookie = new { sauth_json = hud } }));
			}
			Console.WriteLine($"[INFO]当前登录账号Cookie内容:{JsonConvert.SerializeObject(new { sauth_json = hud })}");
			Path_Bool.IsLogin = true;
			LoginFix.f(hud, hue);
		}

		/// <summary>
		/// 手动输入登录（兼容旧版InputBox流程）
		/// </summary>
		private static void DoManualLogin(string hud, Action<EntityResponse<acl.Resposne>, Exception> hue)
		{
			string text3 = "cookies.txt";
			string filePath = Path.Combine(Environment.CurrentDirectory, text3);
			string text = "";
			bool loginby4399 = false;
			if (File.Exists(filePath))
			{
				text = File.ReadAllText(filePath);
			}
			string text4 = "4399.txt";
			string filePath2 = Path.Combine(Environment.CurrentDirectory, text4);
			string text2 = "";
			if (File.Exists(filePath2))
			{
				text2 = File.ReadAllText(filePath2);
			}
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
					Console.WriteLine(ex.ToString());
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
				Tool.PrintRed("cookies有误");
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
					Tool.PrintYellow("4399:" + text2);
					File.WriteAllText(filePath2, text2);
					LoginWithCookieData(text9, hue);
				}
				else
				{
					Tool.PrintYellow("选择原号登录(原因：玩家填入off或空)");
					LoginWithOriginal(hud, hue);
				}
			}
			else if (!(text == "off") && !(text == ""))
			{
				Tool.PrintYellow("cookies:" + text);
				File.WriteAllText(filePath, text);
				LoginWithCookieData(text9, hue);
			}
			else
			{
				Tool.PrintYellow("选择原号登录(原因：玩家填入off或空)");
				LoginWithOriginal(hud, hue);
			}
		}

		/// <summary>
		/// 用Cookie数据登录（WebSocket通知 + 调用原始登录方法）
		/// </summary>
		private static void LoginWithCookieData(string text9, Action<EntityResponse<acl.Resposne>, Exception> hue)
		{
			if (Path_Bool.IsStartWebSocket)
			{
				WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new { type = "Login", cookie = new { sauth_json = text9 } }));
			}
			Console.WriteLine($"[INFO]当前登录账号Cookie内容:{JsonConvert.SerializeObject(new { sauth_json = text9 })}");
			Path_Bool.IsLogin = true;
			LoginFix.f(text9, hue);
		}

		// Token: 0x06000040 RID: 64 RVA: 0x00003334 File Offset: 0x00001534
		public static string TextGainCenter(string left, string right, string text)
		{
			bool flag = string.IsNullOrEmpty(text);
			string text2;
			if (flag)
			{
				int num = (int)MessageBox.Show("出现严重错误，返回文本为空！\nError:Text is Empty!");
				text2 = "";
			}
			else
			{
				int num2 = text.IndexOf(left);
				bool flag2 = num2 == -1;
				if (flag2)
				{
					int num3 = (int)MessageBox.Show("出现严重错误，返回文本为空！\nError:Can't Find String For Left!");
					text2 = "";
				}
				else
				{
					int num4 = num2 + left.Length;
					int num5 = text.IndexOf(right, num4);
					bool flag3 = num5 == -1;
					if (flag3)
					{
						int num6 = (int)MessageBox.Show("出现严重错误，返回文本为空！nError:Can't Find String For Right!");
						text2 = "";
					}
					else
					{
						string text3 = text.Substring(num4, num5 - num4);
						string text4 = "\\";
						string text5 = "";
						string text6 = text4;
						string text7 = text5;
						text2 = text3.Replace(text6, text7);
					}
				}
			}
			return text2;
		}

		// Token: 0x06000041 RID: 65 RVA: 0x000033F4 File Offset: 0x000015F4
		public static string GetRegistryValue(string keyName)
		{
			string text = "";
			try
			{
				RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Netease\\MCLauncher");
				text = (string)registryKey.GetValue(keyName);
			}
			catch (Exception ex)
			{
			}
			return text;
		}

		// Token: 0x06000042 RID: 66 RVA: 0x00003444 File Offset: 0x00001644
		public static string ExtractTextBetween(string text, string left, string right)
		{
			bool flag = string.IsNullOrEmpty(text);
			string text2;
			if (flag)
			{
				Console.WriteLine("文本为空！");
				text2 = "";
			}
			else
			{
				int num = text.IndexOf(left);
				bool flag2 = num == -1;
				if (flag2)
				{
					Console.WriteLine("找不到左边界字符串！");
					text2 = "";
				}
				else
				{
					int num2 = num + left.Length;
					int num3 = text.IndexOf(right, num2);
					bool flag3 = num3 != -1;
					if (flag3)
					{
						text2 = text.Substring(num2, num3 - num2);
					}
					else
					{
						Console.WriteLine("找不到右边界字符串！");
						text2 = "";
					}
				}
			}
			return text2;
		}

		// Token: 0x06000043 RID: 67 RVA: 0x000034DC File Offset: 0x000016DC
		public static string CalculateMD5(string input)
		{
			string text;
			using (MD5 md = MD5.Create())
			{
				byte[] bytes = Encoding.UTF8.GetBytes(input);
				byte[] array = md.ComputeHash(bytes);
				StringBuilder stringBuilder = new StringBuilder();
				for (int i = 0; i < array.Length; i++)
				{
					stringBuilder.Append(array[i].ToString("x2"));
				}
				text = stringBuilder.ToString();
			}
			return text;
		}
	}
}
