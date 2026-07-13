using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WPFLauncher.Common;
using WPFLauncher.Manager;
using WPFLauncher.Manager.Configuration;
using WPFLauncher.Util;

namespace Mcl.Core.Dotnetdetour.Tools;

public class X19Http
{
    public static string RequestX19Api(string url, string data)
    {
        var http = new HttpClient();
        var userToken = ss.e(url, data);
        http.DefaultRequestHeaders.Clear();
        http.DefaultRequestHeaders.Add("user-id", azf<arg>.Instance.User.Id);
        http.DefaultRequestHeaders.Add("user-token", userToken);
        var content = new StringContent(data, Encoding.UTF8, "application/json");
        // https://x19apigatewayobt.nie.netease.com/online-lobby-member/query/list-by-room-id
        var responseData = http.PostAsync(azf<axi>.Instance.Url.ApiGatewayUrl + url, content).Result;
        var get_result = responseData.Content.ReadAsStringAsync().Result;
        return get_result;
    }

    public static byte[] RequestX19ApiEncrypt(string url, string data, string Salt = "Auto")
    {
        var http = new HttpClient();
        var userToken = ss.e(url, data);
        http.DefaultRequestHeaders.Clear();
        http.DefaultRequestHeaders.Add("user-id", azf<arg>.Instance.User.Id);
        http.DefaultRequestHeaders.Add("user-token", userToken);
        var Key = string.Empty;
        var byteArray = ss.b(url, data, out Key);
        var content = new ByteArrayContent(byteArray);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        // https://x19apigatewayobt.nie.netease.com/online-lobby-member/query/list-by-room-id
        var SaltUrl = azf<axi>.Instance.Url.ApiGatewayUrl;
        if (Salt != "Auto") SaltUrl = Salt;
        var responseData = http.PostAsync(SaltUrl + url, content).Result;
        var get_result = responseData.Content.ReadAsByteArrayAsync().Result;
        return get_result;
    }

    public static JObject Get_Player_Info(string uid)
    {
        return JObject.Parse(RequestX19Api("/user/query/search-by-uid",
            JsonConvert.SerializeObject(new { user_id = uid })));
    }

    public static JObject Get_Players_Info(List<string> uids)
    {
        return JObject.Parse(RequestX19Api("/user/query/search-by-ids",
            JsonConvert.SerializeObject(new { entity_ids = uids })));
    }

    public static string unix_timestamp_to(long get_unix)
    {
        var dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(get_unix);
        return dateTimeOffset.ToOffset(TimeSpan.FromHours(8)).ToString();
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