using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using DotNetTranstor.Hookevent;
using Microsoft.VisualBasic.ApplicationServices;
using MicrosoftTranslator.DotNetTranstor.Hookevent;
using MicrosoftTranslator.DotNetTranstor.Tools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WPFLauncher.Code;
using WPFLauncher.Common;
using WPFLauncher.Manager;
using WPFLauncher.Manager.Configuration;
using WPFLauncher.Manager.Login;
using WPFLauncher.Model;
using WPFLauncher.Network.Protocol.LobbyGame;
using WPFLauncher.Util;
using Exception = System.Exception;

public class X19Http
{
    public static string RequestX19Api(string url, string data)
    {
        HttpClient http = new HttpClient();
        string userToken = WPFLauncher.Util.sr.e(url, data);
        http.DefaultRequestHeaders.Clear();
        http.DefaultRequestHeaders.Add("user-id", azd<arf>.Instance.User.Id);
        http.DefaultRequestHeaders.Add("user-token", userToken);
        var content = new StringContent(data, Encoding.UTF8, "application/json");
        // https://x19apigatewayobt.nie.netease.com/online-lobby-member/query/list-by-room-id
        HttpResponseMessage responseData = http.PostAsync(azd<axg>.Instance.Url.ApiGatewayUrl + url, content).Result;
        string get_result = responseData.Content.ReadAsStringAsync().Result;
        return get_result;
    }

    public static byte[] RequestX19ApiEncrypt(string url, string data, string Salt = "Auto")
    {
	    HttpClient http = new HttpClient();
	    string userToken = WPFLauncher.Util.sr.e(url, data);
	    http.DefaultRequestHeaders.Clear();
	    http.DefaultRequestHeaders.Add("user-id", azd<arf>.Instance.User.Id);
	    http.DefaultRequestHeaders.Add("user-token", userToken);
	    string Key = string.Empty;
	    byte[] byteArray = WPFLauncher.Util.sr.b(url, data, out Key);
	    var content = new ByteArrayContent(byteArray);
	    content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
	    // https://x19apigatewayobt.nie.netease.com/online-lobby-member/query/list-by-room-id
	    string SaltUrl = azd<axg>.Instance.Url.ApiGatewayUrl;
	    if (Salt != "Auto")
	    {
		    SaltUrl = Salt;
	    }
	    HttpResponseMessage responseData = http.PostAsync(SaltUrl + url, content).Result;
	    byte[] get_result = responseData.Content.ReadAsByteArrayAsync().Result;
	    return get_result;
    }
    public static JObject Get_Player_Info(string uid)
    {
        return JObject.Parse(X19Http.RequestX19Api("/user/query/search-by-uid", JsonConvert.SerializeObject(new { user_id = uid })));
    }
    public static JObject Get_Players_Info(List<string> uids)
    {
        return JObject.Parse(X19Http.RequestX19Api("/user/query/search-by-ids", JsonConvert.SerializeObject(new { entity_ids = uids })));
    }
    public static string unix_timestamp_to(long get_unix)
    {
        DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(get_unix);
        return dateTimeOffset.ToOffset(TimeSpan.FromHours(8)).ToString();
    }
    // Token: 0x06000290 RID: 656 RVA: 0x0000A850 File Offset: 0x00008A50
	public static string GetDecryptionKey(string device_id, string TextContentKey, string user_id)
	{
		string text2;
		try
		{
			string text = "TG8hVJD3Lt1r86Cv" + user_id + device_id;
			int length = text.Length;
			byte[] array = Encoding.UTF8.GetBytes(text);
			byte[] contentKey = Convert.FromBase64String(TextContentKey);
			for (int i = length - 1; i != -1; i--)
			{
				int num3 = i % 0x10;
				contentKey[num3] ^= array[i];
			}
			text2 = Encoding.ASCII.GetString(contentKey);
		}
		catch (Exception ex)
		{
			DebugPrint.LogDebug_NoColorSelect(ex.Message);
			text2 = null;
		}
		return text2;
	}
	public static class TimestampHelper
	{
		public static long GetCurrentTimestampSeconds()
		{
			return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
		}

		public static long GetCurrentTimestampMilliseconds()
		{
			return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
		}
	}
}