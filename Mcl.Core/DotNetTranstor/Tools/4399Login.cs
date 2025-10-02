using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MicrosoftTranslator.DotNetTranstor.Tools;

namespace Mcl.Core.Azure.RIP
{
	// Token: 0x02000042 RID: 66
	internal class _4399
	{
		// Token: 0x0600030D RID: 781 RVA: 0x0000AF10 File Offset: 0x00009110
		public static async Task<string> Loginby4399Async(string username, string password)
		{
			string randomStr = _4399.GetRandomStr(4);
			string text = "captchaReqb3d25c6d6a467540780";
			string sessionId = Guid.NewGuid().ToString("D");
			string text2 = "http://ptlogin.4399.com/ptlogin/login.do?v=1";
			string text3 = "postLoginHandler=default&externalLogin=qq&bizId=2100001792&appId=kid_wdsj&gameId=wd&sec=1&password=" + password + "&username=" + username;
			if (!string.IsNullOrWhiteSpace(randomStr))
			{
				text3 = string.Concat(new string[] { text3, "&redirectUrl=&sessionId=", text, "&inputCaptcha=", randomStr });
			}
			string Uauth = "";
			string Pauth = "";
			string puser = "";
			string Xauth = "";
			string Pnick = "";
			string Qnick = "";
			using (HttpClient client = new HttpClient())
			{
				client.DefaultRequestHeaders.Add("Cookie", "ptusertype=kid_wdsj.4399_login; USESSIONID=" + sessionId);
				StringContent stringContent = new StringContent(text3, Encoding.UTF8, "application/x-www-form-urlencoded");
				HttpResponseMessage httpResponseMessage = await client.PostAsync(text2, stringContent);
				HttpResponseMessage httpResponseMessage2 = httpResponseMessage;
				if (httpResponseMessage2.StatusCode != HttpStatusCode.OK)
				{
					string text4 = httpResponseMessage2.ReasonPhrase;
					if (string.IsNullOrWhiteSpace(text4))
					{
						text4 = string.Format("登录失败，HTTP错误代码：{0}", httpResponseMessage2.StatusCode);
					}

					Console.ForegroundColor = ConsoleColor.Red;
					DebugPrint.LogDebug_NoColorSelect(text4);
					Console.ForegroundColor = ConsoleColor.White;
					return "";
				}
				IEnumerable<string> enumerable;
				if (httpResponseMessage2.Headers.TryGetValues("Set-Cookie", out enumerable))
				{
					foreach (string text5 in enumerable)
					{
						if (text5.StartsWith("Uauth="))
						{
							Uauth = text5.Split(new char[] { ';' })[0].Substring(6);
						}
						else if (text5.StartsWith("Puser="))
						{
							puser = text5.Split(new char[] { ';' })[0].Substring(6);
						}
						else if (text5.StartsWith("Xauth="))
						{
							Xauth = text5.Split(new char[] { ';' })[0].Substring(6);
						}
						else if (text5.StartsWith("Pnick="))
						{
							Pnick = text5.Split(new char[] { ';' })[0].Substring(6);
						}
						else if (text5.StartsWith("Qnick="))
						{
							Qnick = text5.Split(new char[] { ';' })[0].Substring(6);
						}
						else if (text5.StartsWith("Pauth="))
						{
							Pauth = text5.Split(new char[] { ';' })[0].Substring(6);
						}
					}
				}
			}
			string text17;
			if (Uauth != null && puser != null)
			{
				string[] array = Uauth.Split(new char[] { '|' });
				if (array.Length >= 4)
				{
					text2 = "http://ptlogin.4399.com/ptlogin/checkKidLoginUserCookie.do?appId=kid_wdsj&gameUrl=http://cdn.h5wan.4399sj.com/microterminal-h5-frame?game_id=500352&rand_time=" + array[4] + "&nick=null&onLineStart=false&show=1&isCrossDomain=1&retUrl=http%253A%252F%252Fptlogin.4399.com%252Fresource%252Fucenter.html";
					try
					{
						using (HttpClientHandler handler = new HttpClientHandler())
						{
							handler.CookieContainer = new CookieContainer();
							handler.CookieContainer.Add(new Uri("http://ptlogin.4399.com"), new Cookie("phlogact", "l123456"));
							handler.CookieContainer.Add(new Uri("http://ptlogin.4399.com"), new Cookie("USESSIONID", sessionId));
							handler.CookieContainer.Add(new Uri("http://ptlogin.4399.com"), new Cookie("ptusertype", "kid_wdsj.4399_login"));
							handler.CookieContainer.Add(new Uri("http://ptlogin.4399.com"), new Cookie("Uauth", Uauth));
							handler.CookieContainer.Add(new Uri("http://ptlogin.4399.com"), new Cookie("Pauth", Pauth));
							handler.CookieContainer.Add(new Uri("http://ptlogin.4399.com"), new Cookie("ck_accname", puser));
							handler.CookieContainer.Add(new Uri("http://ptlogin.4399.com"), new Cookie("Puser", puser));
							handler.CookieContainer.Add(new Uri("http://ptlogin.4399.com"), new Cookie("Xauth", Xauth));
							handler.CookieContainer.Add(new Uri("http://ptlogin.4399.com"), new Cookie("Pnick", Pnick));
							handler.CookieContainer.Add(new Uri("http://ptlogin.4399.com"), new Cookie("Qnick", Qnick));
							using (HttpClient httpClient = new HttpClient(handler))
							{
								HttpResponseMessage httpResponseMessage3 = await httpClient.PostAsync(text2, null);
								string text6;
								if (httpResponseMessage3.StatusCode != HttpStatusCode.OK)
								{
									text6 = httpResponseMessage3.ReasonPhrase;
									if (string.IsNullOrWhiteSpace(text6))
									{
										text6 = string.Format("校验实名失败，HTTP错误代码：{0}", httpResponseMessage3.StatusCode);
									}
								}
								else
								{
									string text7 = "https://microgame.5054399.net/v2/service/sdk/info?" + "queryStr=" + Uri.EscapeDataString(httpResponseMessage3.RequestMessage.RequestUri.Query.Trim(new char[] { '?' }));
									using (HttpClient httpClient2 = new HttpClient())
									{
										HttpResponseMessage result = await httpClient2.GetAsync(text7);
										//HttpResponseMessage result = taskAwaiter.GetResult();
										if (result.StatusCode != HttpStatusCode.OK)
										{
											string text8 = result.ReasonPhrase;
											if (string.IsNullOrWhiteSpace(text8))
											{
												text8 = string.Format("用户信息获取失败，HTTP错误代码：{0}", result.StatusCode);
											}
											Console.ForegroundColor = ConsoleColor.Red;
											DebugPrint.LogDebug_NoColorSelect(text8);
											Console.ForegroundColor = ConsoleColor.White;
											return "";
										}
										TaskAwaiter<string> taskAwaiter3 = result.Content.ReadAsStringAsync().GetAwaiter();
										// if (!taskAwaiter3.IsCompleted)
										// {
										// 	await taskAwaiter3;
										// 	TaskAwaiter<string> taskAwaiter4;
										// 	taskAwaiter3 = taskAwaiter4;
										// 	taskAwaiter4 = default(TaskAwaiter<string>);
										// }
										string result2 = taskAwaiter3.GetResult();
										_4399._4399_UserInfoResponse _4399_UserInfoResponse = JsonConvert.DeserializeObject<_4399._4399_UserInfoResponse>(result2);
										if (_4399_UserInfoResponse.Data == null)
										{
											string text9 = _4399_UserInfoResponse.Message;
											if (string.IsNullOrWhiteSpace(text9))
											{
												text9 = string.Format("用户信息获取失败，错误代码：{0}", _4399_UserInfoResponse.Code);
											}
											Console.ForegroundColor = ConsoleColor.Red;
											DebugPrint.LogDebug_NoColorSelect(text9);
											Console.ForegroundColor = ConsoleColor.White;
											return "";
										}
										string text10 = JObject.Parse(result2)["data"]["sdk_login_data"].ToString();
										string text11 = "";
										if (!string.IsNullOrEmpty(text10))
										{
											Dictionary<string, string> dictionary = _4399.ParseQueryString(text10);
											string text12 = dictionary["username"];
											string text13 = dictionary["uid"];
											string text14 = dictionary["token"];
											string text15 = dictionary["time"];
											string text16 = _4399.GenerateRandomString(0x20);
											JObject jobject = new JObject();
											jobject["gameid"] = "x19";
											jobject["login_channel"] = "4399pc";
											jobject["app_channel"] = "4399pc";
											jobject["platform"] = "pc";
											jobject["sdkuid"] = text13;
											jobject["sessionid"] = text14;
											jobject["sdk_version"] = "1.0.0";
											jobject["udid"] = text16;
											jobject["deviceid"] = text16;
											jobject["aim_info"] = "{\"aim\":\"127.0.0.1\",\"country\":\"CN\",\"tz\":\"+0800\",\"tzid\":\"\"}";
											jobject["client_login_sn"] = Guid.NewGuid().ToString("N").ToUpperInvariant();
											jobject["gas_token"] = "";
											jobject["source_platform"] = "pc";
											jobject["ip"] = "127.0.0.1";
											jobject["userid"] = username.ToLower();
											jobject["realname"] = "{\"realname_type\":\"0\"}";
											jobject["timestamp"] = text15;
											text11 = JsonConvert.SerializeObject(jobject);
										}
										return text11;
									}
								}
								Console.ForegroundColor = ConsoleColor.Red;
								DebugPrint.LogDebug_NoColorSelect(text6);
								Console.ForegroundColor = ConsoleColor.White;
								return "";
							}
						}
					}
					catch (Exception ex)
					{
						DebugPrint.LogDebug_NoColorSelect(ex.Message);
						return "";
					}
				}
				Console.ForegroundColor = ConsoleColor.Red;
				DebugPrint.LogDebug_NoColorSelect("请检查账号密码是否正确或IP是否被拉黑");
				Console.ForegroundColor = ConsoleColor.White;
				text17 = "";
			}
			else
			{
				Console.ForegroundColor = ConsoleColor.Red;
				DebugPrint.LogDebug_NoColorSelect("请检查账号密码是否正确或IP是否被拉黑");
				Console.ForegroundColor = ConsoleColor.White;
				text17 = "";
			}
			return text17;
		}

		// Token: 0x0600030E RID: 782 RVA: 0x0000AF5C File Offset: 0x0000915C
		private static Dictionary<string, string> ParseQueryString(string queryString)
		{
			return (from part in queryString.Split(new char[] { '&' })
				select part.Split(new char[] { '=' })).ToDictionary((string[] split) => split[0], (string[] split) => split[1]);
		}

		// Token: 0x0600030F RID: 783 RVA: 0x0000AFE4 File Offset: 0x000091E4
		private static string GenerateRandomString(int length)
		{
			Random random = new Random();
			return new string((from s in Enumerable.Repeat<string>("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789", length)
				select s[random.Next(s.Length)]).ToArray<char>());
		}

		// Token: 0x06000310 RID: 784 RVA: 0x0000B02C File Offset: 0x0000922C
		public static string Register4399()
		{
			string text4;
			try
			{
				string text = "Azure_" + _4399.GetRandomStr(6);
				string randomStr = _4399.GetRandomStr(8);
				string text2 = string.Concat(new string[]
				{
					"postLoginHandler=default&displayMode=popup&appId=www_home&gameId=&cid=&externalLogin=qq&aid=&ref=&css=&redirectUrl=&regMode=reg_normal&sessionId=captchaReqb4b554fe42a60206057&regIdcard=true&noEmail=false&crossDomainIFrame=&crossDomainUrl=&mainDivId=popup_reg_div&showRegInfo=true&includeFcmInfo=false&expandFcmInput=false&fcmFakeValidate=true&username=",
					text,
					"&password=",
					randomStr,
					"&passwordveri=",
					randomStr,
					"&email=wqshack@qq.com&inputCaptcha=",
					_4399.GetRandomStr(4),
					"&reg_eula_agree=on"
				});
				try
				{
					HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create("http://ptlogin.4399.com/ptlogin/register.do");
					httpWebRequest.Method = "POST";
					httpWebRequest.ContentType = "application/x-www-form-urlencoded";
					byte[] bytes = Encoding.UTF8.GetBytes(text2);
					httpWebRequest.ContentLength = (long)bytes.Length;
					Stream requestStream = httpWebRequest.GetRequestStream();
					requestStream.Write(bytes, 0, bytes.Length);
					requestStream.Close();
					HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
					string text3 = new StreamReader(httpWebResponse.GetResponseStream(), Encoding.GetEncoding("utf-8")).ReadToEnd();
					if (text3.Contains("注册成功"))
					{
						text4 = text + "----" + randomStr;
					}
					else
					{
						text4 = "";
					}
				}
				catch
				{
					text4 = "";
				}
			}
			catch (Exception)
			{
				text4 = "";
			}
			return text4;
		}

		// Token: 0x06000311 RID: 785 RVA: 0x0000B1A0 File Offset: 0x000093A0
		private static string GetRandomStr(int lenLong)
		{
			Random rand = new Random();
			return new string((from s in Enumerable.Repeat<string>("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789", lenLong)
				select s[rand.Next(s.Length)]).ToArray<char>());
		}

		// Token: 0x02000043 RID: 67
		private class _4399_UserInfoResponse
		{
			// Token: 0x170000E1 RID: 225
			// (get) Token: 0x06000314 RID: 788 RVA: 0x00003885 File Offset: 0x00001A85
			// (set) Token: 0x06000315 RID: 789 RVA: 0x0000388D File Offset: 0x00001A8D
			public int Code { get; set; }

			// Token: 0x170000E2 RID: 226
			// (get) Token: 0x06000316 RID: 790 RVA: 0x00003896 File Offset: 0x00001A96
			// (set) Token: 0x06000317 RID: 791 RVA: 0x0000389E File Offset: 0x00001A9E
			public string Message { get; set; }

			// Token: 0x170000E3 RID: 227
			// (get) Token: 0x06000318 RID: 792 RVA: 0x000038A7 File Offset: 0x00001AA7
			// (set) Token: 0x06000319 RID: 793 RVA: 0x000038AF File Offset: 0x00001AAF
			public _4399._4399_UserInfo Data { get; set; }
		}

		// Token: 0x02000044 RID: 68
		private class _4399_UserInfo
		{
			// Token: 0x170000E4 RID: 228
			// (get) Token: 0x0600031C RID: 796 RVA: 0x000038B8 File Offset: 0x00001AB8
			// (set) Token: 0x0600031D RID: 797 RVA: 0x000038C0 File Offset: 0x00001AC0
			public string SdkLoginData { get; set; }
		}
	}
}
