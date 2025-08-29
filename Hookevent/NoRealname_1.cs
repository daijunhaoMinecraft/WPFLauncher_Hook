using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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
using WPFLauncher;
using WPFLauncher.Common;
using WPFLauncher.Manager;
using WPFLauncher.Manager.Game.Pipeline;
using WPFLauncher.Manager.PCChannel;
using WPFLauncher.Model;
using WPFLauncher.Network.Message;
using WPFLauncher.ViewModel.Share;
using MessageBox = System.Windows.MessageBox;

using MicrosoftTranslator.DotNetTranstor.Tools;

namespace DotNetTranstor.Hookevent
{
	//去除网易实名认证
	internal class NoRealname_1 : IMethodHook
	{
		// Token: 0x0600003E RID: 62 RVA: 0x000032F0 File Offset: 0x000014F0
		[OriginalMethod]
		protected void No_RealName(int code)
		{
		}

		// Token: 0x0600003F RID: 63 RVA: 0x000032F4 File Offset: 0x000014F4
		[CompilerGenerated]
		[HookMethod("WPFLauncher.Unisdk.ny", "onLoginFinish", "No_RealName")]
		// Token: 0x06000433 RID: 1075 RVA: 0x00042D28 File Offset: 0x00040F28
		protected async void onLoginFinish(int code)
		{
			if (Path_Bool.IsDebug)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("[MpayLogin]返回代码: " + code.ToString());
				Console.ForegroundColor = ConsoleColor.White;
			}

			if (code == 1)
			{
				//玩家取消登录
				// MessageBoxResult messageBoxResult = uu.q("看起来你取消登录了\n是否选择其他登录方式(Cookie账户登录/4399账户登录)?", "", "确定", "退出游戏", "");
				// if (messageBoxResult == MessageBoxResult.OK)
				// {
				// 	string text3 = "cookies.txt";
				// 	string filePath = Path.Combine(Environment.CurrentDirectory, text3);
				// 	string text = "";
				// 	bool loginby4399 = false;
				// 	if (File.Exists(filePath))
				// 	{
				// 		text = File.ReadAllText(filePath);
				// 	}
				//
				// 	int get = 0;
				// 	string text4 = "4399.txt";
				// 	string filePath2 = Path.Combine(Environment.CurrentDirectory, text4);
				// 	string text2 = "";
				// 	if (File.Exists(filePath2))
				// 	{
				// 		text2 = File.ReadAllText(filePath2);
				// 	}
				//
				// 	if (uu.r("请选择cookie登录或4399登录", string.Empty, "cookie", "4399", "") == MessageBoxResult.Yes)
				// 	{
				// 		text = Interaction.InputBox("请输入Cookies \n如果原号登陆请输入off或者空" /*\n自动获取cookie请输入1"*/, "Cookies", text, -1, -1);
				// 		get = 1;
				// 	}
				// 	else
				// 	{
				// 		try
				// 		{
				// 			text2 = Interaction.InputBox(
				// 				"请输入4399账号 \n如果原号登陆请输入off或者空\n4399账号格式为 xxxx----xxxx(4个减号) \n例如UserName----UserPassword",
				// 				"4399LOGIN", text2, -1, -1);
				// 			loginby4399 = true;
				// 			if (!(text2 == "off") && !(text2 == ""))
				// 			{
				// 				string[] array = text2.Split(new string[] { "----" }, StringSplitOptions.None);
				// 				string text6 = array[0];
				// 				string text7 = array[1];
				// 				string text8 = await _4399.Loginby4399Async(text6, text7);
				// 				if (text8 != "")
				// 				{
				//
				// 				}
				//
				// 				text = text8;
				// 				get = 3;
				// 			}
				// 		}
				// 		catch (Exception ex)
				// 		{
				// 			Console.WriteLine(ex.ToString()); 
				// 			No_RealName(1);
				// 		}
				// 	}
				//
				// 	string text9 = "";
				// 	if (text.StartsWith("{"))
				// 	{
				// 		try
				// 		{
				// 			if (text.Contains("\"sauth_json\":"))
				// 			{
				// 				text9 = JObject.Parse(text)["sauth_json"].ToString();
				// 			}
				// 			else
				// 			{
				// 				text9 = text;
				// 			}
				//
				// 			goto IL_0412;
				// 		}
				// 		catch (Exception)
				// 		{
				// 			if (text.Contains("sauth_json"))
				// 			{
				// 				text9 =
				// 					new Regex("\\\"sauth_json\\\":\\\"(.*?)\\\"}\\\"}").Match(text).Groups[1].Value +
				// 					"\"}";
				// 			}
				//
				// 			goto IL_0412;
				// 		}
				// 	}
				//
				// 	Tool.PrintRed("cookies有误");
				// 	if (loginby4399)
				// 	{
				// 		text2 = "";
				// 	}
				// 	else
				// 	{
				// 		text = "";
				// 	}
				//
				// 	IL_0412:
				// 	if (loginby4399)
				// 	{
				// 		if (!(text2 == "off") && !(text2 == ""))
				// 		{
				// 			Tool.PrintYellow("4399:" + text2);
				// 			File.WriteAllText(filePath2, text2);
				// 			if (Path_Bool.IsStartWebSocket)
				// 			{
				// 				WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new
				// 					{ type = "Login", cookie = new { sauth_json = text9 } }));
				// 			}
				// 			Console.WriteLine(
				// 				$"[INFO]当前登录账号Cookie内容:{JsonConvert.SerializeObject(new { sauth_json = text9 })}");
				// 			var instance = ayz<ara>.Instance;
				// 			var type = typeof(ayz<ara>);
				// 			var fieldInfo = type.GetField("b", BindingFlags.Public | BindingFlags.Instance);
				// 			if (fieldInfo != null)
				// 			{
				// 				fieldInfo.SetValue(instance, text9);
				// 			}
				// 		}
				// 		else
				// 		{
				// 			Tool.PrintYellow("退出游戏(原因：玩家填入off或空或填入的cookie有误)");
				// 			No_RealName(1);
				// 		}
				// 	}
				// 	else if (!(text == "off") && !(text == ""))
				// 	{
				// 		if (get == 1)
				// 		{
				// 			Tool.PrintYellow("cookies:" + text);
				// 			File.WriteAllText(filePath, text);
				// 			if (Path_Bool.IsStartWebSocket)
				// 			{
				// 				WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new
				// 					{ type = "Login", cookie = new { sauth_json = text9 } }));
				// 			}
				//
				// 			Console.WriteLine(
				// 				$"[INFO]当前登录账号Cookie内容:{JsonConvert.SerializeObject(new { sauth_json = text9 })}");
				// 			var instance = ayz<ara>.Instance;
				// 			var type = typeof(ayz<ara>);
				// 			var fieldInfo = type.GetField("b", BindingFlags.Public | BindingFlags.Instance);
				// 			if (fieldInfo != null)
				// 			{
				// 				fieldInfo.SetValue(instance, text9);
				// 			}
				// 		}
				// 		else if (get == 2)
				// 		{
				// 			Tool.PrintYellow("cookies:" + text);
				// 			File.WriteAllText(filePath, text);
				// 			if (Path_Bool.IsStartWebSocket)
				// 			{
				// 				WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new
				// 					{ type = "Login", cookie = new { sauth_json = text9 } }));
				// 			}
				//
				// 			Console.WriteLine(
				// 				$"[INFO]当前登录账号Cookie内容:{JsonConvert.SerializeObject(new { sauth_json = text9 })}");
				// 			var instance = ayz<ara>.Instance;
				// 			var type = typeof(ayz<ara>);
				// 			var fieldInfo = type.GetField("b", BindingFlags.Public | BindingFlags.Instance);
				// 			if (fieldInfo != null)
				// 			{
				// 				fieldInfo.SetValue(instance, text9);
				// 			}
				// 		}
				// 	}
				// 	else
				// 	{
				// 		Tool.PrintYellow("退出游戏(原因：玩家填入off或空或填入的cookie有误)");
				// 		No_RealName(1);
				// 	}
				// 	var instance1 = ayz<ara>.Instance;
				// 	var type1 = typeof(ayz<ara>);
				// 	var fieldInfo1 = type1.GetField("f", BindingFlags.Public | BindingFlags.Instance);
				// 	if (fieldInfo1 != null)
				// 	{
				// 		fieldInfo1.SetValue(instance1, true);
				// 	}
				// }
				// else
				// {
				// 	No_RealName(1);
				// }
				//
				// No_RealName(0);
				if (Path_Bool.IsLogin)
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
	}
}
