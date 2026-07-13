using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Mcl.Core.Dotnetdetour;
using Mcl.Core.Tools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mcl.Core.NeteaseProtocol;

public class X19Http
{
    public enum RequestType
    {
        Common,
        Encrypt
    }

    public static string GenerateString(int length)
    {
        char[] array = new char[]
        {
            'a', 'b', 'd', 'c', 'e', 'f', 'g', 'h', 'i', 'j',
            'k', 'l', 'm', 'n', 'p', 'r', 'q', 's', 't', 'u',
            'v', 'w', 'z', 'y', 'x', '0', '1', '2', '3', '4',
            '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E',
            'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'Q',
            'P', 'R', 'T', 'S', 'V', 'U', 'W', 'X', 'Y', 'Z'
        };
        StringBuilder stringBuilder = new StringBuilder();
        Random random = new Random(DateTime.Now.Millisecond);
        for (int i = 0; i < length; i++)
        {
            stringBuilder.Append(array[random.Next(0, array.Length)].ToString());
        }
        return stringBuilder.ToString();
    }
    
    public static string Post(string path,  string body, string baseUrl = "", RequestType requestType = RequestType.Common)
    {
        Uri requestAddress = new Uri("http://baidu.com");
        if (!path.StartsWith("/"))
        {
            path.Insert(0, "/");
        }
        if (string.IsNullOrEmpty(baseUrl))
        {
            requestAddress = new Uri(WpfConfig.ServerList["ApiGatewayUrl"].ToString() + path);
        }
        else
        {
            if (baseUrl.EndsWith("/"))
            {
                baseUrl = baseUrl.Substring(0, baseUrl.Length - 1);
            }
            requestAddress = new Uri(baseUrl + path);
        }

        HttpClient http = new HttpClient();
        http.DefaultRequestHeaders.Clear();
        http.DefaultRequestHeaders.Add("user-id", X19Crypt.UserId);
        http.DefaultRequestHeaders.Add("user-token", X19Crypt.ComputeDynamicToken(path, body));
        http.DefaultRequestHeaders.Add("X_TRACE_ID", GenerateString(32));
        if (requestType == RequestType.Common)
        {
            var content = new StringContent(body);
            var responseData = http.PostAsync(requestAddress, content).Result;
            string result = responseData.Content.ReadAsStringAsync().Result;
            return result;
        }
        else if (requestType == RequestType.Encrypt)
        {
            var content = new ByteArrayContent(X19Crypt.HttpEncrypt(Encoding.UTF7.GetBytes(body)));
            var responseData = http.PostAsync(requestAddress, content).Result;
            byte[] encryptResult = responseData.Content.ReadAsByteArrayAsync().Result;
            string result = X19Crypt.DecryptX19Body(encryptResult);
            return result;
        }
        return "";
    }
    
    public static string Get(string path, string baseUrl = "", RequestType requestType = RequestType.Common)
    {
        Uri requestAddress = new Uri("http://baidu.com");
        if (!path.StartsWith("/"))
        {
            path.Insert(0, "/");
        }
        if (string.IsNullOrEmpty(baseUrl))
        {
            requestAddress = new Uri(WpfConfig.ServerList["ApiGatewayUrl"].ToString());
        }
        else
        {
            if (baseUrl.EndsWith("/"))
            {
                baseUrl = baseUrl.Substring(0, baseUrl.Length - 1);
            }
            requestAddress = new Uri(baseUrl + path);
        }

        HttpClient http = new HttpClient();
        http.DefaultRequestHeaders.Clear();
        http.DefaultRequestHeaders.Add("user-id", X19Crypt.UserId);
        http.DefaultRequestHeaders.Add("user-token", X19Crypt.ComputeDynamicToken(path, ""));
        http.DefaultRequestHeaders.Add("X_TRACE_ID", GenerateString(32));
        if (requestType == RequestType.Common)
        {
            var responseData = http.GetAsync(requestAddress).Result;
            string result = responseData.Content.ReadAsStringAsync().Result;
            return result;
        }
        else if (requestType == RequestType.Encrypt)
        {
            var responseData = http.GetAsync(requestAddress).Result;
            byte[] encryptResult = responseData.Content.ReadAsByteArrayAsync().Result;
            string result = X19Crypt.DecryptX19Body(encryptResult);
            return result;
        }
        return "";
    }
    
    public static JObject GetPlayerInfo(string uid)
    {
        return JObject.Parse(X19Http.Post("/user/query/search-by-uid",
            JsonConvert.SerializeObject(new { user_id = uid })));
    }

    public static JObject GetPlayersInfo(List<string> uids)
    {
        return JObject.Parse(X19Http.Post("/user/query/search-by-ids",
            JsonConvert.SerializeObject(new { entity_ids = uids })));
    }
}