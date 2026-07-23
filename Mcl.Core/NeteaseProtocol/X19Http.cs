using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Mcl.Core.Dotnetdetour;
using Mcl.Core.Dotnetdetour.Models.Config;
using Mcl.Core.Tools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mcl.Core.NeteaseProtocol;

public class X19Http
{
    private static readonly HttpClient Client = new HttpClient();
    private static readonly Random Rng = new Random();

    private static readonly char[] TraceChars =
    {
        'a', 'b', 'd', 'c', 'e', 'f', 'g', 'h', 'i', 'j',
        'k', 'l', 'm', 'n', 'p', 'r', 'q', 's', 't', 'u',
        'v', 'w', 'z', 'y', 'x', '0', '1', '2', '3', '4',
        '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E',
        'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'Q',
        'P', 'R', 'T', 'S', 'V', 'U', 'W', 'X', 'Y', 'Z'
    };

    public enum RequestType
    {
        Common,
        Encrypt
    }

    public static string GenerateTraceId(int length = 32)
    {
        var sb = new StringBuilder(length);
        for (int i = 0; i < length; i++)
            sb.Append(TraceChars[Rng.Next(TraceChars.Length)]);
        return sb.ToString();
    }

    public static string Post(string path, string body, string baseUrl = "",
        RequestType requestType = RequestType.Common)
    {
        var uri = BuildUri(path, body, baseUrl);
        return ExecuteRequest(uri, path, body, requestType, isGet: false);
    }

    public static string Get(string path, string baseUrl = "", RequestType requestType = RequestType.Common)
    {
        var uri = BuildUri(path, "", baseUrl);
        return ExecuteRequest(uri, path, "", requestType, isGet: true);
    }

    private static Uri BuildUri(string path, string body, string baseUrl)
    {
        if (!path.StartsWith("/"))
            path = "/" + path;

        if (string.IsNullOrEmpty(baseUrl))
            return new Uri(WpfConfig.ServerList["ApiGatewayUrl"] + path);

        if (baseUrl.EndsWith("/"))
            baseUrl = baseUrl.Substring(0, baseUrl.Length - 1);

        return new Uri(baseUrl + path);
    }

    private static string ExecuteRequest(Uri uri, string path, string body,
        RequestType requestType, bool isGet)
    {
        var request = new HttpRequestMessage(isGet ? HttpMethod.Get : HttpMethod.Post, uri);
        request.Headers.Clear();
        request.Headers.Add("user-id", X19Crypt.UserId);
        request.Headers.Add("user-token", X19Crypt.ComputeDynamicToken(path, body));
        request.Headers.Add("X_TRACE_ID", GenerateTraceId());

        if (!isGet && requestType == RequestType.Common)
        {
            request.Content = new StringContent(body);
        }
        else if (!isGet && requestType == RequestType.Encrypt)
        {
            var encrypted = X19Crypt.HttpEncrypt(Encoding.UTF7.GetBytes(body));
            request.Content = new ByteArrayContent(encrypted);
        }

        var response = Client.SendAsync(request).Result;

        if (requestType == RequestType.Encrypt)
        {
            var encryptedBytes = response.Content.ReadAsByteArrayAsync().Result;
            return X19Crypt.DecryptX19Body(encryptedBytes);
        }

        return response.Content.ReadAsStringAsync().Result;
    }

    public static JObject GetPlayerInfo(string uid)
    {
        return JObject.Parse(Post("/user/query/search-by-uid",
            JsonConvert.SerializeObject(new { user_id = uid })));
    }

    public static JObject GetPlayersInfo(List<string> uids)
    {
        return JObject.Parse(Post("/user/query/search-by-ids",
            JsonConvert.SerializeObject(new { entity_ids = uids })));
    }
}
