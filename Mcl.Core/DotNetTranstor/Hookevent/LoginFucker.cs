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
using Azure;
using Mcl.Core.Azure.RIP;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WPFLauncher.Manager.PCChannel;
using WPFLauncher.Network.Message;
using JS4399MC;
using MessageBox = System.Windows.MessageBox;
using MicrosoftTranslator.DotNetTranstor.Tools;

namespace DotNetTranstor.Hookevent
{
	// Token: 0x02000017 RID: 23
	internal class LoginFucker : IMethodHook
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
				messageBoxResult = uz.q("是否使用Cookie登录?", "", "确定", "使用原号登录", "");
			}
			if (messageBoxResult == MessageBoxResult.OK)
			{
				string text3 = "cookies.txt";
				string filePath = Path.Combine(Environment.CurrentDirectory, text3);
				string text = "";
				bool loginby4399 = false;
				if (File.Exists(filePath))
				{
					text = File.ReadAllText(filePath);
				}
				int get = 0;
				string text4 = "4399.txt";
				string filePath2 = Path.Combine(Environment.CurrentDirectory, text4);
				string text2 = "";
				if (File.Exists(filePath2))
				{
					text2 = File.ReadAllText(filePath2);
				}
				if (uz.r("请选择cookie登录或4399登录", string.Empty, "cookie", "4399", "") == MessageBoxResult.Yes)
				{
					text = Interaction.InputBox("请输入Cookies \n如果原号登陆请输入off或者空"/*\n自动获取cookie请输入1"*/, "Cookies", text, -1, -1);
					/*if (text == "1")
					{
						text = "off";
						string text5 = Tool.CalculateMD5Hash("VIP_Azure_a09s8ug" + Tool.GetCurrentTimeStamp());
						text = Tool.HttpGet("http://111.180.204.28:4514/extract?md5=" + text5 + "&timestamp=" + Tool.GetCurrentTimeStamp());
						if (text == "Key not found.")
						{
							us.n("当前时间段或当天获取总数搭上限，请等待一小时后再试", "");
							text = "off";
						}
						if (text.ToLower().Contains("too late") || text.ToLower().Contains("too early"))
						{
							us.n("时间戳验证错误，请检测你的网络或修改你的系统时间后重试", "");
							text = "off";
						}
						if (!(text == "error") && !(text == "访问速度过快，请等待10秒钟再访问。频繁访问可能会导致封禁。"))
						{
							get = 1;
						}
						else
						{
							us.n("cookie自动获取失败", "");
							text = "off";
						}
					}
					else
					{
						get = 2;
					}*/
					get = 2;
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
							string text8 = FeverToSauth.FeverAuth.SDK4399ToSauth(FeverToSauth.FeverAuth.Base64Encode(JsonConvert.SerializeObject(new FeverToSauth.FeverAuth.SDK4399Token() {username = text6,password = text7}))); // from null application
							text = text8;
							get = 3;
						}
					}
					catch (Exception ex)
					{
						Console.WriteLine(ex.ToString());
						if (Path_Bool.IsStartWebSocket)
						{
							WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new { type = "Login", cookie = new { sauth_json = hud } }));
						}
						Console.WriteLine($"[INFO]当前登录账号Cookie内容:{JsonConvert.SerializeObject(new { sauth_json = hud })}");
						Path_Bool.IsLogin = true;
						LoginFucker.f(hud, hue);
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
						goto IL_0412;
					}
					catch (Exception)
					{
						if (text.Contains("sauth_json"))
						{
							text9 = new Regex("\\\"sauth_json\\\":\\\"(.*?)\\\"}\\\"}").Match(text).Groups[1].Value + "\"}";
						}
						goto IL_0412;
					}
				}
				Tool.PrintRed("cookies有误");
				if (loginby4399)
				{
					text2 = "";
				}
				else
				{
					text = "";
				}
				IL_0412:
				if (loginby4399)
				{
					if (!(text2 == "off") && !(text2 == ""))
					{
						Tool.PrintYellow("4399:" + text2);
						File.WriteAllText(filePath2, text2);
						if (Path_Bool.IsStartWebSocket)
						{
							WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new { type = "Login", cookie = new { sauth_json = text9 } }));
						}
						Console.WriteLine($"[INFO]当前登录账号Cookie内容:{JsonConvert.SerializeObject(new { sauth_json = text9 })}");
						Path_Bool.IsLogin = true;
						LoginFucker.f(text9, hue);
					}
					else
					{
						Tool.PrintYellow("选择原号登录(原因：玩家填入off或空或填入的cookie有误)");
						if (Path_Bool.IsStartWebSocket)
						{
							WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new { type = "Login", cookie = new { sauth_json = hud } }));
						}
						Console.WriteLine($"[INFO]当前登录账号Cookie内容:{JsonConvert.SerializeObject(new { sauth_json = hud })}");
						Path_Bool.IsLogin = true;
						LoginFucker.f(hud, hue);
					}
				}
				else if (!(text == "off") && !(text == ""))
				{
					if (get == 1)
					{
						Tool.PrintYellow("cookies:" + text);
						File.WriteAllText(filePath, text);
						if (Path_Bool.IsStartWebSocket)
						{
							WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new { type = "Login", cookie = new { sauth_json = text9 } }));
						}
						Console.WriteLine($"[INFO]当前登录账号Cookie内容:{JsonConvert.SerializeObject(new { sauth_json = text9 })}");
						Path_Bool.IsLogin = true;
						LoginFucker.f(text9, hue);
					}
					else if (get == 2)
					{
						Tool.PrintYellow("cookies:" + text);
						File.WriteAllText(filePath, text);
						if (Path_Bool.IsStartWebSocket)
						{
							WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new { type = "Login", cookie = new { sauth_json = text9 } }));
						}
						Console.WriteLine($"[INFO]当前登录账号Cookie内容:{JsonConvert.SerializeObject(new { sauth_json = text9 })}");
						Path_Bool.IsLogin = true;
						LoginFucker.f(text9, hue);
					}
				}
				else
				{
					Tool.PrintYellow("选择原号登录(原因：玩家填入off或空或填入的cookie有误)");
					Path_Bool.IsLogin = true;
					LoginFucker.f(hud, hue);
				}
				
				/*HttpClient client = new HttpClient();
				WebClient client_beiyong = new WebClient();
				string text = Interaction.InputBox("请输入Cookies卡登录或按否使用本地账号登陆!", "Cookie_Login", "", -1, -1);
				bool flag4 = text.Contains("4399");
				if (flag4)
				{
					string text2 = text.Replace("\\", "");
					string GetSauth_Content = string.Concat(new string[]
					{
						"{\"gameid\":\"x19\",\"login_channel\":\"4399pc\",\"app_channel\":\"4399pc\",\"platform\":\"pc\",\"sdkuid\":\"",
						LoginFucker.ExtractTextBetween(text2, "sdkuid\":\"", "\""),
						"\",\"sessionid\":\"",
						LoginFucker.ExtractTextBetween(text2, "sessionid\":\"", "\""),
						"\",\"sdk_version\":\"1.0.0\",\"udid\":\"",
						LoginFucker.ExtractTextBetween(text2, "udid\":\"", "\""),
						"\",\"deviceid\":\"",
						LoginFucker.ExtractTextBetween(text2, "deviceid\":\"", "\""),
						"\",\"aim_info\":\"{\\\"aim\\\":\\\"127.0.0.1\\\",\\\"country\\\":\\\"CN\\\",\\\"tz\\\":\\\"+0800\\\",\\\"tzid\\\":\\\"\\\"}\",\"client_login_sn\":\"5F3B00E48A434706BE5F0DFC7041D899\",\"gas_token\":\"\",\"source_platform\":\"pc\",\"ip\":\"127.0.0.1\",\"userid\":\"",
						LoginFucker.ExtractTextBetween(text2, "\"userid\":\"", "\""),
						"\",\"realname\":\"{\\\"realname_type\\\":\\\"0\\\"}\",\"timestamp\":\"",
						LoginFucker.ExtractTextBetween(text2, "timestamp\":\"", "\""),
						"\"}"
					});
					if (Path_Bool.IsStartWebSocket)
					{
						WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new { type = "Login", cookie = new { sauth_json = GetSauth_Content } }));
					}
					Console.WriteLine($"[INFO]当前登录账号Cookie内容:{JsonConvert.SerializeObject(new { sauth_json = GetSauth_Content })}");
					LoginFucker.f(GetSauth_Content, hue);
				}
				else
				{
					bool flag5 = text != "";
					if (flag5)
					{
						string right2 = ",\\\"aim_info\\";
						string str2 = ",\"aim_info\":\"{\\\"aim\\\":\\\"100.100.100.100\\\",\\\"country\\\":\\\"CN\\\",\\\"tz\\\":\\\" 0800\\\",\\\"tzid\\\":\\\"\\\"}\"}";
						if (Path_Bool.IsStartWebSocket)
						{
							WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new { type = "Login", cookie = new { sauth_json = hud } }));
						}
						Console.WriteLine($"[INFO]当前登录账号Cookie内容:{JsonConvert.SerializeObject(new { sauth_json = hud })}");
						LoginFucker.f(LoginFucker.TextGainCenter("{\"sauth_json\":\"", right2, text) + str2, hue);
						right2 = null;
						str2 = null;
					}
					else
					{
						if (Path_Bool.IsStartWebSocket)
						{
							WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new { type = "Login", cookie = new { sauth_json = hud } }));
						}
						Console.WriteLine($"[INFO]当前登录账号Cookie内容:{JsonConvert.SerializeObject(new { sauth_json = hud })}");
						LoginFucker.f(hud, hue);
					}
				}*/
			}
			else if (messageBoxResult == MessageBoxResult.None)
			{
				if (Path_Bool.IsStartWebSocket)
				{
					WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new { type = "Login", cookie = new { sauth_json = hud } }));
				}
				Console.WriteLine($"[INFO]当前登录账号Cookie内容:{JsonConvert.SerializeObject(new { sauth_json = hud })}");
				Path_Bool.IsLogin = true;
				LoginFucker.f(hud, hue);
			}
			else
			{
				if (Path_Bool.IsStartWebSocket)
				{
					WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new { type = "Login", cookie = new { sauth_json = hud } }));
				}
				Console.WriteLine($"[INFO]当前登录账号Cookie内容:{JsonConvert.SerializeObject(new { sauth_json = hud })}");
				Path_Bool.IsLogin = true;
				LoginFucker.f(hud, hue);
			}
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
