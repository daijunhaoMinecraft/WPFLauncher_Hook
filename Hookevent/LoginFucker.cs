using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using WPFLauncher.Code;
using WPFLauncher.Network.Launcher;
using WPFLauncher.Util;
using System.Windows;
using WPFLauncher.Manager.PCChannel;
using WPFLauncher.Network.Message;
using MessageBox = System.Windows.MessageBox;

namespace DotNetTranstor.Hookevent
{
	// Token: 0x02000017 RID: 23
	internal class LoginFucker : IMethodHook
	{
		// Token: 0x0600003E RID: 62 RVA: 0x000032F0 File Offset: 0x000014F0
		[OriginalMethod]
		public static void f(string hud, Action<EntityResponse<acd.Resposne>, Exception> hue)
		{
		}

		// Token: 0x0600003F RID: 63 RVA: 0x000032F4 File Offset: 0x000014F4
		[CompilerGenerated]
		[HookMethod("WPFLauncher.Network.Launcher.aci", "g", "g")]
		public static void g(string hud, Action<EntityResponse<acd.Resposne>, Exception> hue)
		{
			MessageBoxResult messageBoxResult = us.q("是否使用Cookie登录?", "", "确定", "使用原号登录", "");
			if (messageBoxResult == MessageBoxResult.OK)
			{
				HttpClient client = new HttpClient();
				WebClient client_beiyong = new WebClient();
				string text = Interaction.InputBox("请输入Cookies卡登录或按否使用本地账号登陆!", "Cookie_Login", "", -1, -1);
				bool flag4 = text.Contains("4399");
				if (flag4)
				{
					string text2 = text.Replace("\\", "");
					LoginFucker.f(string.Concat(new string[]
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
					}), hue);
				}
				else
				{
					bool flag5 = text != "";
					if (flag5)
					{
						string right2 = ",\\\"aim_info\\";
						string str2 = ",\"aim_info\":\"{\\\"aim\\\":\\\"100.100.100.100\\\",\\\"country\\\":\\\"CN\\\",\\\"tz\\\":\\\" 0800\\\",\\\"tzid\\\":\\\"\\\"}\"}";
						LoginFucker.f(LoginFucker.TextGainCenter("{\"sauth_json\":\"", right2, text) + str2, hue);
						right2 = null;
						str2 = null;
					}
					else
					{
						LoginFucker.f(hud, hue);
					}
				}
			}
			else if (messageBoxResult == MessageBoxResult.None)
			{
				LoginFucker.f(hud, hue);
			}
			else
			{
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
