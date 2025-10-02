using System;
using System.Collections.Generic;
using System.Text;
using JS4399MC;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MicrosoftTranslator.DotNetTranstor.Tools;

namespace FeverToSauth
{
	// Token: 0x0200006E RID: 110
	public class FeverAuth
	{
		// Token: 0x060005EE RID: 1518 RVA: 0x00003FEF File Offset: 0x000021EF
		public static string Base64Encode(string plainText)
		{
			return Convert.ToBase64String(Encoding.UTF8.GetBytes(plainText));
		}

		// Token: 0x060005EF RID: 1519 RVA: 0x0001D344 File Offset: 0x0001B544
		public static string Base64UnEncode(string plainText)
		{
			byte[] array = Convert.FromBase64String(plainText);
			return Encoding.UTF8.GetString(array);
		}

		// Token: 0x060005F0 RID: 1520 RVA: 0x0001D368 File Offset: 0x0001B568
		public static string SDK4399ToSauth(string token)
		{
			string text;
			try
			{
				token = FeverAuth.Base64UnEncode(token);
				FeverAuth.SDK4399Token sdk4399Token = JsonConvert.DeserializeObject<FeverAuth.SDK4399Token>(token);
				JS4399 js = new JS4399(new JS4399HttpConfig());
				Dictionary<string, object> dictionary = new Dictionary<string, object>();
				dictionary.Add("username", sdk4399Token.username);
				dictionary.Add("password", sdk4399Token.password);
				JS4399Result result = js.JS4399LoginAsync(dictionary, async (string base64) => Ocr.GetOcr(base64)).Result;
				if (result.Success)
				{
					text = result.SauthJson;
				}
				else
				{
					Console.WriteLine(result.Message);
					text = null;
				}
			}
			catch (Exception)
			{
				text = null;
			}
			return text;
		}

		// Token: 0x060005F1 RID: 1521 RVA: 0x0001D42C File Offset: 0x0001B62C
		public static string SDK4399ToSauthPE(string token)
		{
			string text;
			try
			{
				token = token.Substring(7);
				token = FeverAuth.Base64UnEncode(token);
				FeverAuth.SDK4399Token sdk4399Token = JsonConvert.DeserializeObject<FeverAuth.SDK4399Token>(token);
				JS4399 js = new JS4399(new JS4399HttpConfig());
				Dictionary<string, object> dictionary = new Dictionary<string, object>();
				dictionary.Add("username", sdk4399Token.username);
				dictionary.Add("password", sdk4399Token.password);
				JS4399Result result = js.method_1(dictionary, async (string base64) => Ocr.GetOcr(base64)).Result;
				if (result.Success)
				{
					text = result.SauthJson;
				}
				else
				{
					Console.WriteLine(result.Message);
					text = null;
				}
			}
			catch (Exception)
			{
				text = null;
			}
			return text;
		}

		// Token: 0x060005F2 RID: 1522 RVA: 0x0001D4F0 File Offset: 0x0001B6F0
		public static string SDK4399ToSauthPE_(string token)
		{
			string text;
			try
			{
				token = token.Substring(7);
				token = FeverAuth.Base64UnEncode(token);
				FeverAuth.SDK4399Token sdk4399Token = JsonConvert.DeserializeObject<FeverAuth.SDK4399Token>(token);
				JS4399Result result = new JS4399(new JS4399HttpConfig()).JS4399LoginAsyncPE_(new Dictionary<string, object>
				{
					{ "username", sdk4399Token.username },
					{ "password", sdk4399Token.password }
				}).Result;
				if (!result.Success)
				{
					Console.WriteLine(result.Message);
					text = null;
				}
				else
				{
					text = result.SauthJson;
				}
			}
			catch (Exception)
			{
				text = null;
			}
			return text;
		}
		
		// Token: 0x0200006F RID: 111
		internal class SDK4399Token
		{
			// Token: 0x170000FB RID: 251
			// (get) Token: 0x06000605 RID: 1541 RVA: 0x00004001 File Offset: 0x00002201
			// (set) Token: 0x06000606 RID: 1542 RVA: 0x0001D598 File Offset: 0x0001B798
			public string username { get; set; }

			// Token: 0x170000FC RID: 252
			// (get) Token: 0x06000607 RID: 1543 RVA: 0x00004009 File Offset: 0x00002209
			// (set) Token: 0x06000608 RID: 1544 RVA: 0x0001D5AC File Offset: 0x0001B7AC
			public string password { get; set; }
		}
	}
}
