using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Login.NetEase;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Noya.LocalServer.Common.Cryptography;

namespace JS4399MC
{
	// Token: 0x02000064 RID: 100
	public class JS4399
	{
		// Token: 0x06000546 RID: 1350 RVA: 0x0001A068 File Offset: 0x00018268
		public JS4399(JS4399HttpConfig httpConfig = null)
		{
			JS4399._httpConfig = httpConfig ?? new JS4399HttpConfig();
		}

		// Token: 0x06000547 RID: 1351 RVA: 0x0001A08C File Offset: 0x0001828C
		public async Task<JS4399Result> method_0(Dictionary<string, object> registerConfig = null, Func<string, Task<string>> captchaFunctionAsync = null)
		{
			JS4399Result result = new JS4399Result();
			try
			{
				string username = ((registerConfig != null && registerConfig.ContainsKey("username")) ? registerConfig["username"].ToString() : this.GenerateRandomString(10, null));
				string password = ((registerConfig == null || !registerConfig.ContainsKey("password")) ? this.GenerateRandomString(10, null) : registerConfig["password"].ToString());
				string captchaID = "captchaReqb3d25c6d6a4" + this.GenerateRandomString(8, new Dictionary<string, object> { { "numbers", true } });
				using (HttpClient httpClient = new HttpClient(new HttpClientHandler
				{
					Proxy = JS4399._httpConfig.Proxy,
					UseProxy = (JS4399._httpConfig.Proxy != null)
				}))
				{
					httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:134.0) Gecko/20100101 Firefox/134.0");
					string text = "https://ptlogin.4399.com/ptlogin/captcha.do?xx=1&captchaId=" + captchaID;
					HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, text);
					HttpResponseMessage httpResponseMessage = await httpClient.SendAsync(httpRequestMessage);
					if (!httpResponseMessage.IsSuccessStatusCode)
					{
						result.Message = "访问验证码接口失败";
						return result;
					}
					byte[] buffer = await httpResponseMessage.Content.ReadAsByteArrayAsync();
					string text2 = Convert.ToBase64String(buffer);
					string text3 = await captchaFunctionAsync(text2);
					string text4 = "110108" + this.GetRandomDate("19700101", "20041231") + this.GenerateRandomString(3, new Dictionary<string, object> { { "numbers", true } });
					text4 += this.GetIDCardLastCode(text4);
					string text5 = this.GenerateRandomString(1, new Dictionary<string, object> { { "custom", "李王张刘陈杨赵黄周吴徐孙胡朱高林何郭马罗梁宋郑谢韩唐冯于董萧程曹袁邓许傅沈曾彭吕苏卢蒋蔡贾丁魏薛叶阎余潘杜戴夏钟汪田任姜范方石姚谭廖邹熊金陆郝孔白崔康毛邱秦江史顾侯邵孟龙万段漕钱汤尹黎易常武乔贺赖龚文" } }) + this.GenerateRandomString(2, new Dictionary<string, object> { { "chinese", true } });
					string text6 = string.Concat(new string[]
					{
						"https://ptlogin.4399.com/ptlogin/register.do?postLoginHandler=default&displayMode=popup&appId=www_home&gameId=&cid=&externalLogin=qq&aid=&ref=&css=&redirectUrl=&regMode=reg_normal&sessionId=",
						captchaID,
						"&regIdcard=true&noEmail=false&crossDomainIFrame=&crossDomainUrl=&mainDivId=popup_reg_div&showRegInfo=true&includeFcmInfo=false&expandFcmInput=true&fcmFakeValidate=true&userNameLabel=4399%E7%94%A8%E6%88%B7%E5%90%8D&username=",
						username,
						"&password=",
						password,
						"&realname=",
						HttpUtility.UrlEncode(text5),
						"&idcard=",
						text4,
						"&email=",
						this.GenerateRandomString(9, new Dictionary<string, object> { { "numbers", true } }),
						"@qq.com&reg_eula_agree=on&inputCaptcha=",
						text3
					});
					httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, text6);
					HttpResponseMessage httpResponseMessage2 = await httpClient.SendAsync(httpRequestMessage);
					if (!httpResponseMessage2.IsSuccessStatusCode)
					{
						result.Message = "访问注册接口失败";
						return result;
					}
					string text7 = await httpResponseMessage2.Content.ReadAsStringAsync();
					if (text7.Contains("验证码错误"))
					{
						result.Message = "验证码错误";
						return result;
					}
					if (text7.Contains("用户名格式错误"))
					{
						result.Message = "用户名格式错误";
						return result;
					}
					if (text7.Contains("用户名已被注册"))
					{
						result.Message = "用户名已被注册";
						return result;
					}
					if (!text7.Contains("请一定记住您注册的用户名和密码"))
					{
						result.Message = "未知错误";
						return result;
					}
					result.Success = true;
					result.Message = "注册成功";
					result.Username = username;
					result.Password = password;
					username = null;
					password = null;
					captchaID = null;
				}
			}
			catch (Exception ex)
			{
				result.Message = ex.Message;
			}
			return result;
		}

		// Token: 0x06000548 RID: 1352 RVA: 0x0001A0E0 File Offset: 0x000182E0
		public async Task<JS4399Result> JS4399LoginAsync(Dictionary<string, object> loginConfig, Func<string, Task<string>> captchaFunctionAsync)
		{
			JS4399Result result = new JS4399Result();
			try
			{
				string username = loginConfig["username"].ToString();
				string password = loginConfig["password"].ToString();
				string text = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
				HttpClient httpClient = new HttpClient(new HttpClientHandler
				{
					Proxy = JS4399._httpConfig.Proxy,
					UseProxy = (JS4399._httpConfig.Proxy != null)
				})
				{
					DefaultRequestHeaders = { { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:134.0) Gecko/20100101 Firefox/134.0" } }
				};
				Function.ClientLog("[4399SDK]ptlogin verify", ConsoleColor.Gray);
				string text2 = string.Concat(new string[] { "https://ptlogin.4399.com/ptlogin/verify.do?username=", username, "&appId=kid_wdsj&t=", text, "&inputWidth=iptw2&v=1" });
				HttpResponseMessage httpResponseMessage = await httpClient.GetAsync(text2).ConfigureAwait(false);
				if (!httpResponseMessage.IsSuccessStatusCode)
				{
					result.Message = "访问验证接口失败";
					Function.ClientError(result.Message, ConsoleColor.Red);
					return result;
				}
				string text3 = await httpResponseMessage.Content.ReadAsStringAsync();
				string text4 = null;
				string captchaID = null;
				if (text3 != "0")
				{
					captchaID = this.GetBetweenStrings(text3, "captchaId=", "'");
					if (string.IsNullOrEmpty(captchaID))
					{
						result.Message = "获取captchaID失败";
						Function.ClientError(result.Message, ConsoleColor.Red);
						return result;
					}
					Function.ClientLog("[4399SDK]ptlogin captcha", ConsoleColor.Gray);
					HttpResponseMessage httpResponseMessage2 = await httpClient.GetAsync("https://ptlogin.4399.com/ptlogin/captcha.do?captchaId=" + captchaID);
					Function.ClientLog("[4399SDK]ptlogin captchaOK", ConsoleColor.Gray);
					if (!httpResponseMessage2.IsSuccessStatusCode)
					{
						result.Message = "获取验证码失败";
						Function.ClientError(result.Message, ConsoleColor.Red);
						return result;
					}

					// byte[] captchaBytes = await httpResponseMessage2.Content.ReadAsByteArrayAsync();
					// text4 = await captchaFunctionAsync(Convert.ToBase64String(captchaBytes));

					// 新写法：直接 await ReadAsByteArrayAsync 并转换
					byte[] captchaBytes = await httpResponseMessage2.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
					text4 = await captchaFunctionAsync(Convert.ToBase64String(captchaBytes)).ConfigureAwait(false);
				}
				string text5 = string.Concat(new string[] { "loginFrom=uframe&postLoginHandler=default&layoutSelfAdapting=true&externalLogin=qq&displayMode=popup&layout=vertical&bizId=2100001792&appId=kid_wdsj&gameId=wd&css=http%3A%2F%2Fmicrogame.5054399.net%2Fv2%2Fresource%2FcssSdk%2Fdefault%2Flogin.css&redirectUrl=&sessionId=", captchaID, "&mainDivId=popup_login_div&includeFcmInfo=false&level=8&regLevel=8&userNameLabel=4399%E7%94%A8%E6%88%B7%E5%90%8D&userNameTip=%E8%AF%B7%E8%BE%93%E5%85%A54399%E7%94%A8%E6%88%B7%E5%90%8D&welcomeTip=%E6%AC%A2%E8%BF%8E%E5%9B%9E%E5%88%B04399&sec=1&password=", password, "&username=", username, "&inputCaptcha=", text4 });
				StringContent stringContent = new StringContent(text5, Encoding.UTF8, "application/x-www-form-urlencoded");
				Function.ClientLog("[4399SDK]ptlogin login", ConsoleColor.Gray);
				Function.ClientLog("[4399SDK]ptlogin LoginData:" + text5, ConsoleColor.Gray);
				HttpResponseMessage loginResponse = await httpClient.PostAsync("https://ptlogin.4399.com/ptlogin/login.do?v=1", stringContent);
				if (!loginResponse.IsSuccessStatusCode)
				{
					result.Message = "登录请求失败";
					Function.ClientError(result.Message, ConsoleColor.Red);
					return result;
				}
				string result2 = await loginResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
				if (result2.Contains("验证码错误"))
				{
					result.Message = "验证码错误";
					Function.ClientError(result.Message, ConsoleColor.Red);
					return result;
				}
				if (result2.Contains("密码错误"))
				{
					result.Message = "密码错误";
					Function.ClientError(result.Message, ConsoleColor.Red);
					return result;
				}
				if (result2.Contains("用户不存在"))
				{
					result.Message = "用户不存在";
					Function.ClientError(result.Message, ConsoleColor.Red);
					return result;
				}
				string randtime = this.GetBetweenStrings(result2, "parent.timestamp = \"", "\"");
				if (string.IsNullOrEmpty(randtime))
				{
					result.Message = "获取时间戳失败";
					Function.ClientError(result.Message, ConsoleColor.Red);
					return result;
				}
				Function.ClientLog("[4399SDK]Set Cookie", ConsoleColor.Gray);
				List<string> list = loginResponse.Headers.GetValues("Set-Cookie").ToList<string>();
				if (list == null || list.Count == 0)
				{
					result.Message = "Cookie获取失败";
					Function.ClientError(result.Message, ConsoleColor.Red);
					return result;
				}
				string text6 = string.Join("; ", list.Select((string cookie) => cookie.Split(new char[] { ';' })[0]));
				CookieContainer cookieContainer = new CookieContainer();
				Function.ClientLog("[4399SDK]checkKidLoginUserCookie", ConsoleColor.Gray);
				Uri uri = new Uri("https://ptlogin.4399.com/ptlogin/checkKidLoginUserCookie.do");
				string[] array = text6.Split(new char[] { ';' });
				for (int i = 0; i < array.Length; i++)
				{
					string[] array2 = array[i].Split(new char[] { '=' });
					if (array2.Length == 2)
					{
						cookieContainer.Add(uri, new Cookie(array2[0].Trim(), array2[1].Trim()));
					}
				}
				Function.ClientLog("[4399SDK]Second login check", ConsoleColor.Gray);
				string text7 = "https://ptlogin.4399.com/ptlogin/checkKidLoginUserCookie.do?appId=kid_wdsj&gameUrl=http://cdn.h5wan.4399sj.com/microterminal-h5-frame?game_id=500352&rand_time=" + randtime + "&nick=null&onLineStart=false&show=1&isCrossDomain=1&retUrl=http%253A%252F%252Fptlogin.4399.com%252Fresource%252Fucenter.html%253Faction%253Dlogin%2526appId%253Dkid_wdsj%2526loginLevel%253D8%2526regLevel%253D8%2526bizId%253D2100001792%2526externalLogin%253Dqq%2526qrLogin%253Dtrue%2526layout%253Dvertical%2526level%253D101%2526css%253Dhttp%253A%252F%252Fmicrogame.5054399.net%252Fv2%252Fresource%252FcssSdk%252Fdefault%252Flogin.css%2526v%253D2018_11_26_16%2526postLoginHandler%253Dredirect%2526checkLoginUserCookie%253Dtrue%2526redirectUrl%253Dhttp%25253A%25252F%25252Fcdn.h5wan.4399sj.com%25252Fmicroterminal-h5-frame%25253Fgame_id%25253D500352%252526rand_time%25253D" + randtime;
				HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, text7);
				httpRequestMessage.Headers.Add("Cookie", text6);
				HttpResponseMessage httpResponseMessage3 = null;
				using (HttpClient client = new HttpClient(new HttpClientHandler
				{
					Proxy = JS4399._httpConfig.Proxy,
					UseProxy = (JS4399._httpConfig.Proxy != null),
					AllowAutoRedirect = false,
					CookieContainer = cookieContainer
				}))
				{
					client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:134.0) Gecko/20100101 Firefox/134.0");
					httpResponseMessage3 = await client.SendAsync(httpRequestMessage);
				}
				HttpClient tmpClient = null;
				if (httpResponseMessage3.StatusCode != HttpStatusCode.Found)
				{
					result.Message = "检查登录状态失败";
					Function.ClientError(result.Message, ConsoleColor.Red);
					return result;
				}
				Uri location = httpResponseMessage3.Headers.Location;
				string text8 = ((location == null) ? null : location.ToString());
				if (string.IsNullOrEmpty(text8))
				{
					result.Message = "获取重定向地址失败";
					Function.ClientError(result.Message, ConsoleColor.Red);
					return result;
				}
				Uri uri2 = new Uri(text8);
				if (uri2.Host != "cdn.h5wan.4399sj.com")
				{
					result.Message = "重定向域名错误";
					Function.ClientError(result.Message, ConsoleColor.Red);
					return result;
				}
				NameValueCollection nameValueCollection = HttpUtility.ParseQueryString(uri2.Query);
				string text9 = nameValueCollection["sig"];
				string uid = nameValueCollection["uid"];
				string time = nameValueCollection["time"];
				string text10 = nameValueCollection["validateState"];
				if (string.IsNullOrEmpty(text9) || string.IsNullOrEmpty(uid) || string.IsNullOrEmpty(time) || string.IsNullOrEmpty(text10))
				{
					result.Message = "解析重定向参数失败";
					Function.ClientError(result.Message, ConsoleColor.Red);
					return result;
				}
				Function.ClientLog("[4399SDK]Get SDK info", ConsoleColor.Gray);
				string text11 = string.Concat(new string[]
				{
					"https://microgame.5054399.net/v2/service/sdk/info?callback=&queryStr=game_id%3D500352%26nick%3Dnull%26sig%3D",
					text9,
					"%26uid%3D",
					uid,
					"%26fcm%3D0%26show%3D1%26isCrossDomain%3D1%26rand_time%3D",
					randtime,
					"%26ptusertype%3D4399%26time%3D",
					time,
					"%26validateState%3D",
					text10,
					"%26username%3D",
					username.ToLower(),
					"&_=",
					time
				});
				HttpResponseMessage result3 = await httpClient.GetAsync(text11);
				if (!result3.IsSuccessStatusCode)
				{
					result.Message = "获取SDK信息失败";
					Function.ClientError(result.Message, ConsoleColor.Red);
					return result;
				}
				// 推荐方式：直接 await Task<string>
				string result3Content = await result3.Content.ReadAsStringAsync();
				JToken jtoken = JObject.Parse(result3Content)["data"];
				string text12;
				if (jtoken != null)
				{
					JToken jtoken2 = jtoken["sdk_login_data"];
					text12 = ((jtoken2 == null) ? null : jtoken2.ToString());
				}
				else
				{
					text12 = null;
				}
				string text13 = text12;
				if (string.IsNullOrEmpty(text13))
				{
					result.Message = "解析SDK数据失败";
					Function.ClientError(result.Message, ConsoleColor.Red);
					return result;
				}
				string text14 = HttpUtility.ParseQueryString(text13)["token"];
				if (string.IsNullOrEmpty(text14))
				{
					result.Message = "获取token失败";
					Function.ClientError(result.Message, ConsoleColor.Red);
					return result;
				}
				string[] array3 = (from _ in Enumerable.Range(0, 2)
					select this.GenerateRandomString(32, new Dictionary<string, object> { { "custom", "0123456789ABCDEF" } })).ToArray<string>();
				object obj = new
				{
					aim_info = "{\"aim\":\"127.0.0.1\",\"country\":\"CN\",\"tz\":\"+0800\",\"tzid\":\"\"}",
					app_channel = "4399pc",
					client_login_sn = array3[0],
					deviceid = array3[1],
					gameid = "x19",
					gas_token = "",
					ip = "127.0.0.1",
					login_channel = "4399pc",
					platform = "pc",
					realname = "{\"realname_type\":\"0\"}",
					sdk_version = "1.0.0",
					sdkuid = uid,
					sessionid = text14,
					source_platform = "pc",
					timestamp = time,
					udid = array3[1],
					userid = username.ToLower()
				};
				string sauthJsonValue = JsonConvert.SerializeObject(obj);
				string sauthJson = JsonConvert.SerializeObject(new
				{
					sauth_json = sauthJsonValue
				});
				Function.ClientLog("[4399SDK]Final login requests", ConsoleColor.Gray);
				httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, "https://mgbsdk.matrix.netease.com/x19/sdk/uni_sauth");
				httpRequestMessage.Headers.Add("User-Agent", "WPFLauncher/0.0.0.0");
				stringContent = new StringContent(sauthJsonValue, Encoding.UTF8, "application/json");
				httpRequestMessage.Content = stringContent;
				HttpResponseMessage response = await httpClient.SendAsync(httpRequestMessage);
				if (!response.IsSuccessStatusCode)
				{
					result.Message = "统一认证请求失败";
					Function.ClientError(result.Message, ConsoleColor.Red);
					return result;
				}
				Function.ClientLog("[4399SDK][SAUTH_JSON]:" + sauthJson, ConsoleColor.Gray);
				result.Success = true;
				result.Message = "登录成功";
				result.SauthJson = sauthJson;
				result.SauthJsonValue = sauthJsonValue;
				username = null;
				password = null;
				httpClient = null;
				captchaID = null;
				loginResponse = null;
				randtime = null;
				uid = null;
				time = null;
				sauthJsonValue = null;
				sauthJson = null;
			}
			catch (Exception ex)
			{
				result.Message = ex.Message;
			}
			return result;
		}

		// Token: 0x06000549 RID: 1353 RVA: 0x0001A134 File Offset: 0x00018334
		public async Task<JS4399Result> method_1(Dictionary<string, object> loginConfig, Func<string, Task<string>> captchaFunctionAsync)
		{
			JS4399Result result = new JS4399Result();
			try
			{
				string username = loginConfig["username"].ToString();
				string password = loginConfig["password"].ToString();
				string text = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
				HttpClient httpClient = new HttpClient(new HttpClientHandler
				{
					Proxy = JS4399._httpConfig.Proxy,
					UseProxy = (JS4399._httpConfig.Proxy != null)
				})
				{
					DefaultRequestHeaders = { { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:134.0) Gecko/20100101 Firefox/134.0" } }
				};
				Function.ClientLog("[4399SDK]ptlogin verify", ConsoleColor.Gray);
				string text2 = string.Concat(new string[] { "https://ptlogin.4399.com/ptlogin/verify.do?username=", username, "&appId=kid_wdsj&t=", text, "&inputWidth=iptw2&v=1" });
				HttpResponseMessage httpResponseMessage = await httpClient.GetAsync(text2);
				if (!httpResponseMessage.IsSuccessStatusCode)
				{
					result.Message = "访问验证接口失败";
					Function.ClientError(result.Message, ConsoleColor.Red);
					return result;
				}
				string result2 = await httpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
				string text3 = null;
				string captchaID = null;
				if (result2 != "0")
				{
					captchaID = this.GetBetweenStrings(result2, "captchaId=", "'");
					if (string.IsNullOrEmpty(captchaID))
					{
						result.Message = "获取captchaID失败";
						Function.ClientError(result.Message, ConsoleColor.Red);
						return result;
					}
					Function.ClientLog("[4399SDK]ptlogin captcha", ConsoleColor.Gray);
					HttpResponseMessage httpResponseMessage2 = await httpClient.GetAsync("https://ptlogin.4399.com/ptlogin/captcha.do?captchaId=" + captchaID);
					if (!httpResponseMessage2.IsSuccessStatusCode)
					{
						result.Message = "获取验证码失败";
						Function.ClientError(result.Message, ConsoleColor.Red);
						return result;
					}
					byte[] captchaBytes = await httpResponseMessage2.Content.ReadAsByteArrayAsync();
					text3 = await captchaFunctionAsync(Convert.ToBase64String(captchaBytes));
				}
				string text4 = string.Concat(new string[] { "loginFrom=uframe&postLoginHandler=default&layoutSelfAdapting=true&externalLogin=qq&displayMode=popup&layout=vertical&bizId=2100001792&appId=kid_wdsj&gameId=wd&css=http%3A%2F%2Fmicrogame.5054399.net%2Fv2%2Fresource%2FcssSdk%2Fdefault%2Flogin.css&redirectUrl=&sessionId=", captchaID, "&mainDivId=popup_login_div&includeFcmInfo=false&level=8&regLevel=8&userNameLabel=4399%E7%94%A8%E6%88%B7%E5%90%8D&userNameTip=%E8%AF%B7%E8%BE%93%E5%85%A54399%E7%94%A8%E6%88%B7%E5%90%8D&welcomeTip=%E6%AC%A2%E8%BF%8E%E5%9B%9E%E5%88%B04399&sec=1&password=", password, "&username=", username, "&inputCaptcha=", text3 });
				StringContent stringContent = new StringContent(text4, Encoding.UTF8, "application/x-www-form-urlencoded");
				Function.ClientLog("[4399SDK]ptlogin login", ConsoleColor.Gray);
				Function.ClientLog("[4399SDK]ptlogin LoginData:" + text4, ConsoleColor.Gray);
				HttpResponseMessage loginResponse = await httpClient.PostAsync("https://ptlogin.4399.com/ptlogin/login.do?v=1", stringContent);
				if (!loginResponse.IsSuccessStatusCode)
				{
					result.Message = "登录请求失败";
					Function.ClientError(result.Message, ConsoleColor.Red);
					return result;
				}
				string result3 = await loginResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
				if (result3.Contains("验证码错误"))
				{
					result.Message = "验证码错误";
					Function.ClientError(result.Message, ConsoleColor.Red);
					return result;
				}
				if (result3.Contains("密码错误"))
				{
					result.Message = "密码错误";
					Function.ClientError(result.Message, ConsoleColor.Red);
					return result;
				}
				if (result3.Contains("用户不存在"))
				{
					result.Message = "用户不存在";
					Function.ClientError(result.Message, ConsoleColor.Red);
					return result;
				}
				string randtime = this.GetBetweenStrings(result3, "parent.timestamp = \"", "\"");
				if (string.IsNullOrEmpty(randtime))
				{
					result.Message = "获取时间戳失败";
					Function.ClientError(result.Message, ConsoleColor.Red);
					return result;
				}
				Function.ClientLog("[4399SDK]Set Cookie", ConsoleColor.Gray);
				List<string> list = loginResponse.Headers.GetValues("Set-Cookie").ToList<string>();
				if (list == null || list.Count == 0)
				{
					result.Message = "Cookie获取失败";
					Function.ClientError(result.Message, ConsoleColor.Red);
					return result;
				}
				string text5 = string.Join("; ", list.Select((string cookie) => cookie.Split(new char[] { ';' })[0]));
				CookieContainer cookieContainer = new CookieContainer();
				Function.ClientLog("[4399SDK]checkKidLoginUserCookie", ConsoleColor.Gray);
				Uri uri = new Uri("https://ptlogin.4399.com/ptlogin/checkKidLoginUserCookie.do");
				string[] array = text5.Split(new char[] { ';' });
				for (int i = 0; i < array.Length; i++)
				{
					string[] array2 = array[i].Split(new char[] { '=' });
					if (array2.Length == 2)
					{
						cookieContainer.Add(uri, new Cookie(array2[0].Trim(), array2[1].Trim()));
					}
				}
				Function.ClientLog("[4399SDK]Second login check", ConsoleColor.Gray);
				string text6 = "https://ptlogin.4399.com/ptlogin/checkKidLoginUserCookie.do?appId=kid_wdsj&gameUrl=http://cdn.h5wan.4399sj.com/microterminal-h5-frame?game_id=500352&rand_time=" + randtime + "&nick=null&onLineStart=false&show=1&isCrossDomain=1&retUrl=http%253A%252F%252Fptlogin.4399.com%252Fresource%252Fucenter.html%253Faction%253Dlogin%2526appId%253Dkid_wdsj%2526loginLevel%253D8%2526regLevel%253D8%2526bizId%253D2100001792%2526externalLogin%253Dqq%2526qrLogin%253Dtrue%2526layout%253Dvertical%2526level%253D101%2526css%253Dhttp%253A%252F%252Fmicrogame.5054399.net%252Fv2%252Fresource%252FcssSdk%252Fdefault%252Flogin.css%2526v%253D2018_11_26_16%2526postLoginHandler%253Dredirect%2526checkLoginUserCookie%253Dtrue%2526redirectUrl%253Dhttp%25253A%25252F%25252Fcdn.h5wan.4399sj.com%25252Fmicroterminal-h5-frame%25253Fgame_id%25253D500352%252526rand_time%25253D" + randtime;
				HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, text6);
				httpRequestMessage.Headers.Add("Cookie", text5);
				HttpResponseMessage httpResponseMessage3 = null;
				HttpClient tmpClientForCheck = null;
				using (HttpClient client = new HttpClient(new HttpClientHandler
				{
				    Proxy = JS4399._httpConfig.Proxy,
				    UseProxy = (JS4399._httpConfig.Proxy != null),
				    AllowAutoRedirect = false,
				    CookieContainer = cookieContainer
				}))
				{
				    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:134.0) Gecko/20100101 Firefox/134.0");
				    httpResponseMessage3 = await client.SendAsync(httpRequestMessage);
				}
				tmpClientForCheck = null;
				if (httpResponseMessage3.StatusCode != HttpStatusCode.Found)
				{
					result.Message = "检查登录状态失败";
					Function.ClientError(result.Message, ConsoleColor.Red);
					return result;
				}
				Uri location = httpResponseMessage3.Headers.Location;
				string text7 = ((location != null) ? location.ToString() : null);
				if (string.IsNullOrEmpty(text7))
				{
					result.Message = "获取重定向地址失败";
					Function.ClientError(result.Message, ConsoleColor.Red);
					return result;
				}
				Uri uri2 = new Uri(text7);
				if (uri2.Host != "cdn.h5wan.4399sj.com")
				{
					result.Message = "重定向域名错误";
					Function.ClientError(result.Message, ConsoleColor.Red);
					return result;
				}
				NameValueCollection nameValueCollection = HttpUtility.ParseQueryString(uri2.Query);
				string text8 = nameValueCollection["sig"];
				string uid = nameValueCollection["uid"];
				string time = nameValueCollection["time"];
				string text9 = nameValueCollection["validateState"];
				if (string.IsNullOrEmpty(text8) || string.IsNullOrEmpty(uid) || string.IsNullOrEmpty(time) || string.IsNullOrEmpty(text9))
				{
					result.Message = "解析重定向参数失败";
					Function.ClientError(result.Message, ConsoleColor.Red);
					return result;
				}
				Function.ClientLog("[4399SDK]Get SDK info", ConsoleColor.Gray);
				string text10 = string.Concat(new string[]
				{
					"https://microgame.5054399.net/v2/service/sdk/info?callback=&queryStr=game_id%3D500352%26nick%3Dnull%26sig%3D",
					text8,
					"%26uid%3D",
					uid,
					"%26fcm%3D0%26show%3D1%26isCrossDomain%3D1%26rand_time%3D",
					randtime,
					"%26ptusertype%3D4399%26time%3D",
					time,
					"%26validateState%3D",
					text9,
					"%26username%3D",
					username.ToLower(),
					"&_=",
					time
				});
				// 推荐方式：直接 await Task<HttpResponseMessage>
				HttpResponseMessage result4 = await httpClient.GetAsync(text10);
				if (!result4.IsSuccessStatusCode)
				{
					result.Message = "获取SDK信息失败";
					Function.ClientError(result.Message, ConsoleColor.Red);
					return result;
				}
				
				// 推荐方式：直接 await Task<string>
				string result4Content = await result4.Content.ReadAsStringAsync();
				JToken jtoken = JObject.Parse(result4Content)["data"];
				string text11;
				if (jtoken == null)
				{
					text11 = null;
				}
				else
				{
					JToken jtoken2 = jtoken["sdk_login_data"];
					text11 = jtoken2 != null ? jtoken2.ToString() : null;
				}
				string text12 = text11;
				if (string.IsNullOrEmpty(text12))
				{
					result.Message = "解析SDK数据失败";
					Function.ClientError(result.Message, ConsoleColor.Red);
					return result;
				}
				string text13 = HttpUtility.ParseQueryString(text12)["token"];
				if (string.IsNullOrEmpty(text13))
				{
					result.Message = "获取token失败";
					Function.ClientError(result.Message, ConsoleColor.Red);
					return result;
				}
				string[] array3 = (from _ in Enumerable.Range(0, 2)
					select this.GenerateRandomString(32, new Dictionary<string, object> { { "custom", "0123456789ABCDEF" } })).ToArray<string>();
				object obj = new
				{
					aim_info = "{\"aim\":\"127.0.0.1\",\"country\":\"CN\",\"tz\":\"+0800\",\"tzid\":\"\"}",
					app_channel = "4399pc",
					client_login_sn = array3[0],
					deviceid = array3[1],
					gameid = "x19",
					gas_token = "",
					ip = "127.0.0.1",
					login_channel = "4399pc",
					platform = "pc",
					realname = "{\"realname_type\":\"0\"}",
					sdk_version = "1.0.0",
					sdkuid = uid,
					sessionid = text13,
					source_platform = "pc",
					timestamp = time,
					udid = array3[1],
					userid = username.ToLower()
				};
				string sauthJsonValue = JsonConvert.SerializeObject(obj);
				string sauthJson = JsonConvert.SerializeObject(new
				{
					sauth_json = sauthJsonValue
				});
				Function.ClientLog("[4399SDK]Final login requests", ConsoleColor.Gray);
				httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, "https://mgbsdk.matrix.netease.com/x19/sdk/uni_sauth");
				httpRequestMessage.Headers.Add("User-Agent", "WPFLauncher/0.0.0.0");
				stringContent = new StringContent(sauthJsonValue, Encoding.UTF8, "application/json");
				httpRequestMessage.Content = stringContent;
				HttpResponseMessage response = await httpClient.SendAsync(httpRequestMessage);
				if (!response.IsSuccessStatusCode)
				    {
				    result.Message = "统一认证请求失败";
				    Function.ClientError(result.Message, ConsoleColor.Red);
				    return result;
				}
				Function.ClientLog("[4399SDK][SAUTH_JSON]:" + sauthJson, ConsoleColor.Gray);
				result.Success = true;
				result.Message = "登录成功";
				result.SauthJson = sauthJson;
				result.SauthJsonValue = sauthJsonValue;
				username = null;
				password = null;
				httpClient = null;
				captchaID = null;
				loginResponse = null;
				randtime = null;
				uid = null;
				time = null;
				sauthJsonValue = null;
				sauthJson = null;
			}
			catch (Exception ex)
			{
				result.Message = ex.Message;
			}
			return result;
		}

		// Token: 0x0600054A RID: 1354 RVA: 0x0001A188 File Offset: 0x00018388
		public async Task<JS4399Result> JS4399LoginAsyncPE_(Dictionary<string, object> loginConfig)
		{
			return null;
		}

		// Token: 0x0600054B RID: 1355 RVA: 0x0001A1C4 File Offset: 0x000183C4
		private string GetBetweenStrings(string str, string start, string end)
		{
			int num = str.IndexOf(start, StringComparison.Ordinal);
			if (num == -1)
			{
				return null;
			}
			num += start.Length;
			int num2 = str.IndexOf(end, num, StringComparison.Ordinal);
			if (num2 != -1)
			{
				return str.Substring(num, num2 - num);
			}
			return null;
		}

		// Token: 0x0600054C RID: 1356 RVA: 0x0001A210 File Offset: 0x00018410
		public string GenerateRandomString(int length = 10, Dictionary<string, object> options = null)
		{
			Dictionary<string, object> dictionary;
			if ((dictionary = options) == null)
			{
				Dictionary<string, object> dictionary2 = new Dictionary<string, object>();
				dictionary2.Add("numbers", true);
				dictionary = dictionary2;
				dictionary2.Add("lowercase", true);
			}
			options = dictionary;
			StringBuilder stringBuilder = new StringBuilder();
			bool flag = true;
			bool flag2 = false;
			if (options.ContainsKey("custom") && options["custom"].ToString() != "")
			{
				stringBuilder.Append(options["custom"].ToString());
				flag = false;
			}
			else
			{
				if (options.ContainsKey("numbers") && (bool)options["numbers"])
				{
					stringBuilder.Append("0123456789");
					flag = false;
				}
				if (options.ContainsKey("lowercase") && (bool)options["lowercase"])
				{
					stringBuilder.Append("abcdefghijklmnopqrstuvwxyz");
					flag = false;
				}
				if (options.ContainsKey("uppercase") && (bool)options["uppercase"])
				{
					stringBuilder.Append("ABCDEFGHIJKLMNOPQRSTUVWXYZ");
					flag = false;
				}
				if (options.ContainsKey("symbols") && (bool)options["symbols"])
				{
					stringBuilder.Append("!@#$%^&*()-_=+[]{}|;:,.<>?/");
					flag = false;
				}
				flag2 = options.ContainsKey("chinese") && (bool)options["chinese"];
			}
			StringBuilder stringBuilder2 = new StringBuilder();
			if (stringBuilder.Length == 0 && !flag2)
			{
				throw new ArgumentException("Character pool is empty. Please ensure options contain at least one character set.");
			}
			for (int i = 0; i < length; i++)
			{
				if ((options.ContainsKey("custom") && options["custom"].ToString() != "") || (!flag && (!flag2 || JS4399._random.NextDouble() <= 0.5)))
				{
					int num = JS4399._random.Next(stringBuilder.Length);
					stringBuilder2.Append(stringBuilder.ToString()[num]);
				}
				else
				{
					stringBuilder2.Append(JS4399.GenerateChineseCharacter());
				}
			}
			return stringBuilder2.ToString();
		}

		// Token: 0x0600054D RID: 1357 RVA: 0x00003DDE File Offset: 0x00001FDE
		private static char GenerateChineseCharacter()
		{
			return (char)JS4399._random.Next(19968, 40870);
		}

		// Token: 0x0600054E RID: 1358 RVA: 0x0001A438 File Offset: 0x00018638
		private string GetRandomDate(string startDate, string endDate)
		{
			DateTime dateTime = DateTime.ParseExact(startDate, "yyyyMMdd", CultureInfo.InvariantCulture);
			int days = (DateTime.ParseExact(endDate, "yyyyMMdd", CultureInfo.InvariantCulture) - dateTime).Days;
			return dateTime.AddDays((double)JS4399._random.Next(days)).ToString("yyyyMMdd");
		}

		// Token: 0x0600054F RID: 1359 RVA: 0x0001A4A0 File Offset: 0x000186A0
		private string GetFormEscape(string FormData)
		{
			string text = string.Empty;
			foreach (char c in FormData)
			{
				string text2 = text;
				string text3;
				if (c != '+')
				{
					if (c != '/')
					{
						if (c != '=')
						{
							text3 = c.ToString();
						}
						else
						{
							text3 = "%3D";
						}
					}
					else
					{
						text3 = "%2F";
					}
				}
				else
				{
					text3 = "%2B";
				}
				text = text2 + text3;
			}
			return text;
		}

		// Token: 0x06000550 RID: 1360 RVA: 0x0001A520 File Offset: 0x00018720
		private string GetIDCardLastCode(string idCard)
		{
			int[] factors = new int[]
			{
				7, 9, 10, 5, 8, 4, 2, 1, 6, 3,
				7, 9, 10, 5, 8, 4, 2
			};
			string[] array = new string[]
			{
				"1", "0", "X", "9", "8", "7", "6", "5", "4", "3",
				"2"
			};
			int num = idCard.Take(17).Select((char c, int i) => (int)(c - '0') * factors[i]).Sum();
			return array[num % 11];
		}

		// Token: 0x06000551 RID: 1361 RVA: 0x0001A5E0 File Offset: 0x000187E0
		public static string GetEncryptPassword(string password)
		{
			MD5 md = MD5.Create();
			int num = 16;
			int num2 = 32;
			int num3 = 16;
			byte[] array = new byte[48];
			byte[] array2 = new byte[0];
			int i = 0;
			Random random = new Random();
			byte[] array3 = new byte[8];
			random.NextBytes(array3);
			while (i < num2 + num3)
			{
				array2 = new byte[0];
				if (i > 0)
				{
					byte[] array4 = new byte[num];
					Array.Copy(array, i - num, array4, 0, num);
					array2 = array2.Concat(array4).ToArray<byte>();
				}
				array2 = array2.Concat(Encoding.ASCII.GetBytes("lzYW5qaXVqa")).ToArray<byte>();
				array2 = array2.Concat(array3).ToArray<byte>();
				Array.Copy(md.ComputeHash(array2), 0, array, i, num);
				for (int j = 1; j < 1; j++)
				{
					byte[] array5 = new byte[num];
					Array.Copy(array, i, array5, 0, num);
					array2 = array2.Concat(array5).ToArray<byte>();
					Array.Copy(md.ComputeHash(array2), 0, array, i, num);
				}
				i += num;
			}
			byte[] array6 = new byte[num2];
			byte[] array7 = new byte[num3];
			Array.Copy(array, 0, array6, 0, num2);
			Array.Copy(array, num2, array7, 0, num3);
			return Convert.ToBase64String(Encoding.ASCII.GetBytes("Salted__").Concat(array3).Concat(AESHelper.AES_CBC256_Encrypt(array6, Encoding.ASCII.GetBytes(password), array7))
				.ToArray<byte>());
		}
		
		// Token: 0x040001C1 RID: 449
		private static readonly Random _random = new Random();

		// Token: 0x040001C2 RID: 450
		private static JS4399HttpConfig _httpConfig;
	}
}

