using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Mcl.Core.Dotnetdetour;
using Mcl.Core.Dotnetdetour.Features.Authentication.Core;
using Mcl.Core.Dotnetdetour.Models.Config;
using Mcl.Core.Dotnetdetour.Models.Entity;
using Mcl.Core.Dotnetdetour.Utilities.Network;
using Mcl.Core.Extensions;
using Mcl.Core.NeteaseProtocol;
using Mcl.Core.Network.Interface;
using Mcl.Core.Tools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WPFLauncher.Util;

using Mcl.Core.Dotnetdetour.Utilities.Diagnostics;
namespace Mcl.Core.Network;

public class NetClient : INetClient
{
    private static readonly Version version = new AssemblyName(Assembly.GetExecutingAssembly().FullName).Version;

    private readonly Regex structuredSyntaxSuffixRegex = new("\\+\\w+$", RegexOptions.Compiled);

    private readonly Regex structuredSyntaxSuffixWildcardRegex = new("^\\*\\+\\w+$", RegexOptions.Compiled);

    public IHttpFactory HttpFactory = new SimpleFactory<Http>();

    public NetClient()
    {
        Encoding = Encoding.UTF8;
        AcceptTypes = new List<string>();
        DefaultParameters = new List<Parameter>();
        FollowRedirects = true;
    }

    public NetClient(Uri baseUrl)
        : this()
    {
        BaseUrl = baseUrl;
    }

    public NetClient(string baseUrl)
        : this()
    {
        var flag = string.IsNullOrEmpty(baseUrl);
        if (flag) throw new ArgumentNullException("baseUrl");
        BaseUrl = new Uri(baseUrl);
    }

    private IList<string> AcceptTypes { get; }

    public virtual NetRequestAsyncHandle ExecuteAsync(INetRequest request,
        Action<INetResponse, NetRequestAsyncHandle> callback)
    {
        var capture = NetworkCaptureStore.Begin(request, BaseUrl, Encoding, GetEffectiveUserAgent());
        NetRequestAsyncHandle handle = null;
        try
        {
            handle = ExecuteAsyncCore(request, delegate(INetResponse response, NetRequestAsyncHandle asyncHandle)
            {
                NetworkCaptureStore.Complete(capture, request, response, Encoding, GetEffectiveUserAgent());
                callback(response, asyncHandle);
            });
            if (handle?.WebRequest == null && !capture.IsCompleted)
                NetworkCaptureStore.Complete(capture, request, null, Encoding, GetEffectiveUserAgent(), "Intercepted");
            return handle;
        }
        catch (Exception ex)
        {
            NetworkCaptureStore.Fail(capture, request, ex, Encoding, GetEffectiveUserAgent());
            throw;
        }
    }

    private NetRequestAsyncHandle ExecuteAsyncCore(INetRequest request,
        Action<INetResponse, NetRequestAsyncHandle> callback)
    {
        var uri = new Uri(new Uri(BaseUrl.ToString()), request.Resource);
        PluginLog.Info("Network", $"[AsyncRequest] url: {uri}");

        // 获取请求内容
        var body = new byte[] { };
        var stringBody = "";
        var isString = false;
        foreach (var param in request.Parameters)
            if (param.Type == ParameterType.RequestBody)
            {
                if (param.Value is byte[] byteValue)
                {
                    body = byteValue;
                }
                else if (param.Value is string stringValue)
                {
                    isString = true;
                    stringBody = stringValue;
                    body = Encoding.UTF8.GetBytes(stringValue);
                }
            }

        var name = Enum.GetName(typeof(Method), request.Method);
        var method = request.Method;
        var netRequestAsyncHandle = new NetRequestAsyncHandle();
        var netResponse = new NetResponse();

        try
        {
            if (uri.ToString().EndsWith("/salog-new"))
            {
                var decryptString = X19Crypt.DecryptX19Body(body);
                var parseJson = JObject.Parse(decryptString);
                PluginLog.Info("Network", "Blocked security telemetry request: endpoint=/salog-new type={0}", parseJson["type"]?.ToString());
                PluginLog.Debug("Network", "Security telemetry payload omitted: size={0}", decryptString.Length);
                return netRequestAsyncHandle;
            }

            if (uri.ToString().EndsWith("/salog"))
            {
                var JsonRequest = JObject.Parse(stringBody);
                PluginLog.Debug("Network", "Security telemetry payload omitted: endpoint=/salog size={0} type={1}",
                    stringBody.Length, JsonRequest["type"]?.ToString());
                return netRequestAsyncHandle;
            }

            if (uri.ToString().EndsWith("/client-log"))
            {
                var JsonRequest = JObject.Parse(stringBody);
                PluginLog.Debug("Network", "Client log request received: file={0} size={1}",
                    JsonRequest["file_name"]?.ToString(), stringBody.Length);
                return netRequestAsyncHandle;
            }

            if (uri.ToString().EndsWith("/diagnostic-log"))
            {
                PluginLog.Debug("Network", "Diagnostic log payload omitted: size={0}", stringBody.Length);
                return netRequestAsyncHandle;
            }

            if (uri.ToString().EndsWith("/diagnostic-value"))
            {
                PluginLog.Debug("Network", "Diagnostic value payload omitted: size={0}", stringBody.Length);
                return netRequestAsyncHandle;
            }

            if (uri.ToString().EndsWith("game-play-v2/start"))
            {
                PluginLog.Info("Game", "Game start request intercepted.");
                var result =
                    "{\n   \"code\" : 0,\n   \"details\" : \"\",\n   \"entity\" : {\n      \"anti_addiction_info\" : {\n         \"current_online_time_sum\" : 0,\n         \"msg\" : \"\",\n         \"online_time_left\" : 0,\n         \"online_time_limit\" : 0,\n         \"online_time_sum\" : 0,\n         \"status\" : 0\n      },\n      \"is_anti_addiction\" : false,\n      \"record\" : null\n   },\n   \"message\" : \"正常返回\"\n}";
                netResponse.Content = result;
                callback(netResponse, netRequestAsyncHandle);
                return netRequestAsyncHandle;
            }

            if (uri.ToString().EndsWith("game-play-v2/stop"))
            {
                PluginLog.Info("Game", "Game stop request intercepted.");
                var result =
                    "{\"code\":0,\"message\":\"\\u6b63\\u5e38\\u8fd4\\u56de\",\"details\":\"\",\"entity\":null}";
                WpfConfig.IsJoiningCustomServer = false;
                netResponse.Content = result;
                callback(netResponse, netRequestAsyncHandle);
                return netRequestAsyncHandle;
            }

            if (uri.ToString().EndsWith("/authentication/update"))
            {
                var decryptRequest = X19Crypt.DecryptX19Body(body);
                var authenticationUpdateResult = X19Http.Post("/authentication/update", decryptRequest, WpfConfig.ServerList["CoreServerUrl"].ToString(), X19Http.RequestType.Encrypt);

                var authResult = JObject.Parse(authenticationUpdateResult);
                if (WpfConfig.LogSensitiveAccountDetails)
                {
                    PluginLog.Debug("Auth", "Authentication response received: size={0}", authenticationUpdateResult.Length);
                }
                if (authResult["code"].ToObject<int>() == 0)
                {
                    X19Crypt.Token = authResult["entity"]["token"].ToString();
                    X19Crypt.UserId = authResult["entity"]["entity_id"].ToString();
                    PluginLog.Info("Auth", "Authentication updated: userId={0}", X19Crypt.UserId);
                }

                netResponse.RawBytes = X19Crypt.HttpEncrypt(Encoding.UTF8.GetBytes(authenticationUpdateResult));
                netResponse.Content = authenticationUpdateResult;
                callback(netResponse, netRequestAsyncHandle);
                return netRequestAsyncHandle;
            }
            
            if (uri.ToString().EndsWith("/item-address/get"))
            {
                JObject queryJson = JObject.Parse(stringBody);
                Tuple<string, NetGameResponse> customResponse = WpfConfig.CustomRecentServers.FirstOrDefault(x => queryJson["item_id"].ToString() == x.Item1);
                if (customResponse != null)
                {
                    netResponse.Content = $"{{\"code\":0,\"details\":\"\",\"entity\":{{\"announcement\":\"\",\"entity_id\":\"{queryJson["item_id"]}\",\"game_status\":0,\"in_whitelist\":false,\"ip\":\"{customResponse.Item2.NetGameEntity.Ip}\",\"isp_enable\":false,\"port\":{customResponse.Item2.NetGameEntity.Port.ToString()}}},\"message\":\"正常返回\"}}";
                    callback(netResponse, netRequestAsyncHandle);
                    WpfConfig.IsJoiningCustomServer = true;
                    return netRequestAsyncHandle;
                }
                WpfConfig.IsJoiningCustomServer = false;
            }
        }
        catch (Exception e)
        {
            PluginLog.Error("Network", e);
            return netRequestAsyncHandle;
        }

        if (method - Method.POST > 1 && method != Method.PATCH)
            netRequestAsyncHandle = ExecuteAsync(request, callback, name, DoAsGetAsync);
        else
            netRequestAsyncHandle = ExecuteAsync(request, callback, name, DoAsPostAsync);
        return netRequestAsyncHandle;
    }

    public virtual NetRequestAsyncHandle ExecuteAsyncGet(INetRequest request,
        Action<INetResponse, NetRequestAsyncHandle> callback, string httpMethod)
    {
        return ExecuteAsync(request, callback, httpMethod, DoAsGetAsync);
    }

    public virtual NetRequestAsyncHandle ExecuteAsyncPost(INetRequest request,
        Action<INetResponse, NetRequestAsyncHandle> callback, string httpMethod)
    {
        request.Method = Method.POST;
        return ExecuteAsync(request, callback, httpMethod, DoAsPostAsync);
    }

    public virtual Task<INetResponse> ExecuteTaskAsync(INetRequest request)
    {
        return ExecuteTaskAsync(request, CancellationToken.None);
    }

    public virtual Task<INetResponse> ExecuteGetTaskAsync(INetRequest request)
    {
        return ExecuteGetTaskAsync(request, CancellationToken.None);
    }

    public virtual Task<INetResponse> ExecuteGetTaskAsync(INetRequest request, CancellationToken token)
    {
        var flag = request == null;
        if (flag) throw new ArgumentNullException("request");
        request.Method = Method.GET;
        return ExecuteTaskAsync(request, token);
    }

    public virtual Task<INetResponse> ExecutePostTaskAsync(INetRequest request)
    {
        return ExecutePostTaskAsync(request, CancellationToken.None);
    }

    public virtual Task<INetResponse> ExecutePostTaskAsync(INetRequest request, CancellationToken token)
    {
        var flag = request == null;
        if (flag) throw new ArgumentNullException("request");
        request.Method = Method.POST;
        return ExecuteTaskAsync(request, token);
    }

    public virtual Task<INetResponse> ExecuteTaskAsync(INetRequest request, CancellationToken token)
    {
        var flag = request == null;
        if (flag) throw new ArgumentNullException("request");
        var taskCompletionSource = new TaskCompletionSource<INetResponse>();
        try
        {
            var async = ExecuteAsync(request, delegate(INetResponse response, NetRequestAsyncHandle _)
            {
                var isCancellationRequested = token.IsCancellationRequested;
                if (isCancellationRequested)
                    taskCompletionSource.TrySetCanceled();
                else
                    taskCompletionSource.TrySetResult(response);
            });
            var registration = token.Register(delegate
            {
                async.Abort();
                taskCompletionSource.TrySetCanceled();
            });
            taskCompletionSource.Task.ContinueWith(delegate { registration.Dispose(); }, token);
        }
        catch (Exception ex)
        {
            taskCompletionSource.TrySetException(ex);
        }

        return taskCompletionSource.Task;
    }

    public int? MaxRedirects { get; set; }

    public X509CertificateCollection ClientCertificates { get; set; }

    public RequestCachePolicy CachePolicy { get; set; }

    public bool FollowRedirects { get; set; }

    public CookieContainer CookieContainer { get; set; }

    public string UserAgent { get; set; }

    public int Timeout { get; set; }

    public int ReadWriteTimeout { get; set; }

    public bool UseSynchronizationContext { get; set; }

    public virtual Uri BaseUrl { get; set; }

    public Encoding Encoding { get; set; }

    public bool PreAuthenticate { get; set; }

    public IList<Parameter> DefaultParameters { get; }

    public Uri BuildUri(INetRequest request)
    {
        var flag = BaseUrl == null;
        if (flag) throw new NullReferenceException("NetClient must contain a value for BaseUrl");
        var text = request.Resource;
        var enumerable = request.Parameters.Where(p => p.Type == ParameterType.UrlSegment);
        var uriBuilder = new UriBuilder(BaseUrl);
        foreach (var parameter in enumerable)
        {
            var flag2 = parameter.Value == null;
            if (flag2)
                throw new ArgumentException(
                    string.Format("Cannot build uri when url segment parameter '{0}' value is null.", parameter.Name),
                    "request");
            var flag3 = !string.IsNullOrEmpty(text);
            if (flag3) text = text.Replace("{" + parameter.Name + "}", parameter.Value.ToString().UrlEncode());
            uriBuilder.Path = uriBuilder.Path.UrlDecode()
                .Replace("{" + parameter.Name + "}", parameter.Value.ToString().UrlEncode());
        }

        BaseUrl = new Uri(uriBuilder.ToString());
        var flag4 = !string.IsNullOrEmpty(text) && text.StartsWith("/");
        if (flag4) text = text.Substring(1);
        var flag5 = BaseUrl != null && !string.IsNullOrEmpty(BaseUrl.AbsoluteUri);
        if (flag5)
        {
            var flag6 = !BaseUrl.AbsoluteUri.EndsWith("/") && !string.IsNullOrEmpty(text);
            if (flag6) text = "/" + text;
            text = string.IsNullOrEmpty(text) ? BaseUrl.AbsoluteUri : string.Format("{0}{1}", BaseUrl, text);
        }

        var flag7 = request.Method != Method.POST && request.Method != Method.PUT && request.Method != Method.PATCH;
        IEnumerable<Parameter> enumerable2;
        if (flag7)
            enumerable2 = request.Parameters
                .Where(p => p.Type == ParameterType.GetOrPost || p.Type == ParameterType.QueryString).ToList();
        else
            enumerable2 = request.Parameters.Where(p => p.Type == ParameterType.QueryString).ToList();
        var flag8 = !enumerable2.Any();
        Uri uri;
        if (flag8)
        {
            uri = new Uri(text);
        }
        else
        {
            var text2 = EncodeParameters(enumerable2);
            var text3 = text != null && text.Contains("?") ? "&" : "?";
            text = text + text3 + text2;
            uri = new Uri(text);
        }

        return uri;
    }

    public byte[] DownloadData(INetRequest request)
    {
        return DownloadData(request, false);
    }

    public virtual INetResponse Execute(INetRequest request)
    {
        var capture = NetworkCaptureStore.Begin(request, BaseUrl, Encoding, GetEffectiveUserAgent());
        try
        {
            var response = ExecuteCore(request);
            NetworkCaptureStore.Complete(capture, request, response, Encoding, GetEffectiveUserAgent());
            return response;
        }
        catch (Exception ex)
        {
            NetworkCaptureStore.Fail(capture, request, ex, Encoding, GetEffectiveUserAgent());
            throw;
        }
    }

    private INetResponse ExecuteCore(INetRequest request)
    {
        var isServerListRequest = false;
        var uri = new Uri(new Uri(BaseUrl.ToString()), request.Resource);
        PluginLog.Info("Network", $"[Request] url: {uri}");
        if (request.Resource.EndsWith("/serverlist/release.json") ||
            uri.ToString().EndsWith("/serverlist/release.json"))
        {
            PluginLog.Info("Network", "更改服务器");
            request.Resource = "";
            BaseUrl = WpfConfig.ServerListUri;
            isServerListRequest = true;
        }

        // 获取请求内容
        var body = new byte[] { };
        var stringBody = "";
        var isString = false;
        foreach (var param in request.Parameters)
            if (param.Type == ParameterType.RequestBody)
            {
                if (param.Value is byte[] byteValue)
                {
                    body = byteValue;
                }
                else if (param.Value is string stringValue)
                {
                    isString = true;
                    stringBody = stringValue;
                    body = Encoding.UTF8.GetBytes(stringValue);
                }
            }

        var name = Enum.GetName(typeof(Method), request.Method);
        var method = request.Method;
        INetResponse netResponse = new NetResponse();

        if (uri.ToString().EndsWith("/diagnostic-log"))
        {
            PluginLog.Debug("Network", $"diagnosticLog, Json: {stringBody}");
            return netResponse;
        }

        if (uri.ToString().EndsWith("popup-window/query"))
        {
            PluginLog.Info("Web", "Launcher popup request blocked.");
            netResponse.Content =
                "{\"code\":0,\"details\":\"\",\"entity\":{\"popup_window\":[],\"server_time\":0},\"message\":\"正常返回\"}";
            return netResponse;
        }

        if (uri.ToString().EndsWith("pe-item/query/search-lobby-by-id-list"))
        {
            if (WpfConfig.RoomInfo != null)
            {
                PluginLog.Info("Web", "Online lobby download request blocked.");
                netResponse.Content = "{\"code\":0,\"details\":\"\",\"entities\":[],\"message\":\"正常返回\",\"total\":0}";
                return netResponse;
            }
        }

        if (uri.ToString().EndsWith("/salog-new"))
        {
            var decryptString = X19Crypt.DecryptX19Body(body);
            var parseJson = JObject.Parse(decryptString);
            PluginLog.Info("Network", "Blocked security telemetry request: endpoint=/salog-new type={0}", parseJson["type"]?.ToString());
            PluginLog.Debug("Network", "Security telemetry payload omitted: size={0}", decryptString.Length);
            return netResponse;
        }

        if (uri.ToString().EndsWith("/authentication-otp"))
        {
            var sauthJson = SauthJsonRandomGenerator.Generate();
            if (WpfConfig.LogSensitiveAccountDetails)
            {
                PluginLog.Credential("Auth", "Sauth", sauthJson);
            }
            var decryptBody = JObject.Parse(X19Crypt.DecryptX19Body(body));
            decryptBody["sauth_json"] = sauthJson;
            SetRequestBody(request, X19Crypt.HttpEncrypt(Encoding.UTF8.GetBytes(decryptBody.ToString())));
        }

        if (uri.ToString().EndsWith("game-play-v2/start"))
        {
            PluginLog.Info("Network", "用户启动了游戏");
            var result =
                "{\n   \"code\" : 0,\n   \"details\" : \"\",\n   \"entity\" : {\n      \"anti_addiction_info\" : {\n         \"current_online_time_sum\" : 0,\n         \"msg\" : \"\",\n         \"online_time_left\" : 0,\n         \"online_time_limit\" : 0,\n         \"online_time_sum\" : 0,\n         \"status\" : 0\n      },\n      \"is_anti_addiction\" : false,\n      \"record\" : null\n   },\n   \"message\" : \"正常返回\"\n}";
            netResponse.Content = result;
            return netResponse;
        }

        if (uri.ToString().EndsWith("/item/query/search-by-iid"))
        {
            JObject queryJson = JObject.Parse(stringBody);
            Tuple<string, NetGameResponse> customResponse = WpfConfig.CustomRecentServers.FirstOrDefault(x => queryJson["item_id"].ToString() == x.Item1);
            if (customResponse != null)
            {
                netResponse.Content = JsonConvert.SerializeObject(customResponse.Item2);
                return netResponse;
            }
        }
        
        if (uri.ToString().EndsWith("/item-channel/query/search-by-item-channel"))
        {
            JObject queryJson = JObject.Parse(stringBody);
            Tuple<string, NetGameResponse> customResponse = WpfConfig.CustomRecentServers.FirstOrDefault(x => queryJson["item_id"].ToString() == x.Item1);
            if (customResponse != null)
            {
                netResponse.Content = $"{{\"code\":0,\"details\":\"\",\"entities\":[{{\"channel_id\":\"{queryJson["channel_id"].ToString()}\",\"entity_id\":\"2650669\",\"item_id\":\"{queryJson["item_id"].ToString()}\",\"title_image_url\":\"{customResponse.Item2.NetGameEntity.TitleImageUrl}\"}}],\"message\":\"正常返回\",\"total\":1}}";
                return netResponse;
            }
        }
        
        if (uri.ToString().EndsWith("/item/user-is-purchase-item"))
        {
            JObject queryJson = JObject.Parse(stringBody);
            Tuple<string, NetGameResponse> customResponse = WpfConfig.CustomRecentServers.FirstOrDefault(x => queryJson["item_id"].ToString() == x.Item1);
            if (customResponse != null)
            {
                netResponse.Content = $"{{\"code\":0,\"message\":\"正常返回\",\"details\":\"\",\"entity\":{{\"entity_id\":\"{queryJson["item_id"].ToString()}\"}}}}";
                return netResponse;
            }
        }
        
        if (uri.ToString().EndsWith("/game-server-info/get"))
        {
            JObject queryJson = JObject.Parse(stringBody);
            Tuple<string, NetGameResponse> customResponse = WpfConfig.CustomRecentServers.FirstOrDefault(x => queryJson["server_id"].ToString() == x.Item1);
            if (customResponse != null)
            {
                netResponse.Content = $"{{\"code\":0,\"details\":\"\",\"entity\":{{\"entity_id\":\"{queryJson["server_id"]}\",\"online_count\":1}},\"message\":\"正常返回\"}}";
                return netResponse;
            }
        }
        
        if (uri.ToString().EndsWith("/pe-game/load-pe-mcgame-res-infos"))
        {
            JObject queryJson = JObject.Parse(stringBody);
            Tuple<string, NetGameResponse> customResponse = WpfConfig.CustomRecentServers.FirstOrDefault(x => queryJson["item_id"].ToString() == x.Item1);
            if (customResponse != null)
            {
                netResponse.Content = "{\"code\":0,\"details\":\"\",\"entities\":[],\"message\":\"正常返回\"}";
                return netResponse;
            }
        }

        if (method - Method.POST > 1 && method != Method.PATCH)
            netResponse = Execute(request, name, DoExecuteAsGet);
        else
            netResponse = Execute(request, name, DoExecuteAsPost);

        if (uri.ToString().EndsWith("/authentication-otp") || uri.ToString().EndsWith("/authentication/update"))
        {
            var decryptString = X19Crypt.DecryptX19Body(netResponse.RawBytes);
            var authResult = JObject.Parse(decryptString);
            if (WpfConfig.LogSensitiveAccountDetails)
            {
                PluginLog.Debug("Auth", "Authentication response received: size={0}", decryptString.Length);
            }
            if (authResult["code"].ToObject<int>() == 0)
            {
                X19Crypt.Token = authResult["entity"]["token"].ToString();
                X19Crypt.UserId = authResult["entity"]["entity_id"].ToString();
                var UserDetailResult = X19Http.Post("/user-detail", "");
                if (WpfConfig.LogSensitiveAccountDetails)
                {
                    PluginLog.Info("Auth", "Login succeeded: userId={0}", X19Crypt.UserId);
                }
            }
            else if (authResult["code"].ToObject<int>() == 29)
            {
                var detailsObj = JObject.Parse(authResult["details"].ToString());
                PluginLog.Error("Network", 
                    $"因 {detailsObj["ban_msg"]} 您的账号被禁止登录游戏至 {X19Tools.unix_timestamp_to(detailsObj["ban_to_ts"].ToObject<long>())}，{authResult["message"]}!");
            }
            else
            {
                PluginLog.Error("Network", $"Auth Failed: {decryptString}");
            }
        }

        if (uri.ToString().EndsWith("/game-auth-item-list/query/search-by-game"))
        {
            JObject jsonRequest = JObject.Parse(stringBody);
            JObject jsonResponse = JObject.Parse(netResponse.Content);
            if (jsonResponse["code"].ToObject<int>() == 13)
            {
                netResponse.Content = JsonConvert.SerializeObject(new
                {
                    code = 0,
                    details = "",
                    entity = new
                    {
                        game_type = jsonRequest["game_type"].Value<int>(),
                        iid_list = Array.Empty<long>(),
                        mc_version_id = jsonRequest["mc_version_id"].Value<int>()
                    },
                    message = "正常返回"
                });
            }
        }

        if (uri.ToString().EndsWith("/room-related"))
        {
            JObject jsonResponse = JObject.Parse(netResponse.Content);
            if (jsonResponse["code"].ToObject<int>() == 0)
            {
                // Filter Bad Room
                var list = (JArray)jsonResponse["list"];
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    var roomInfo = list[i];
                    try
                    {
                        JObject tipsJson = JObject.Parse(roomInfo["tips"].ToString());
                        if (WpfConfig.LanNicknameFilters.FirstOrDefault(x => tipsJson["NickName"].ToString().Contains(x)) != null)
                        {
                            list.RemoveAt(i);
                        }
                    }
                    catch { }
                }
            }
            netResponse.Content = jsonResponse.ToString();
        }

        if (isServerListRequest) WpfConfig.ServerList = JObject.Parse(netResponse.Content);

        return netResponse;
    }

    public INetResponse ExecuteAsGet(INetRequest request, string httpMethod)
    {
        return Execute(request, httpMethod, DoExecuteAsGet);
    }

    public INetResponse ExecuteAsPost(INetRequest request, string httpMethod)
    {
        request.Method = Method.POST;
        return Execute(request, httpMethod, DoExecuteAsPost);
    }

    private NetRequestAsyncHandle ExecuteAsync(INetRequest request,
        Action<INetResponse, NetRequestAsyncHandle> callback, string httpMethod,
        Func<IHttp, Action<HttpResponse>, string, HttpWebRequest> getWebRequest)
    {
        var http = HttpFactory.Create();
        ConfigureHttp(request, http);
        var asyncHandle = new NetRequestAsyncHandle();
        var action = delegate(HttpResponse r) { ProcessResponse(request, r, asyncHandle, callback); };
        var flag = UseSynchronizationContext && SynchronizationContext.Current != null;
        if (flag)
        {
            var ctx = SynchronizationContext.Current;
            var cb = action;
            action = delegate(HttpResponse resp) { ctx.Post(delegate { cb(resp); }, null); };
        }

        asyncHandle.WebRequest = getWebRequest(http, action, httpMethod);
        return asyncHandle;
    }

    private static HttpWebRequest DoAsGetAsync(IHttp http, Action<HttpResponse> responseCb, string method)
    {
        return http.AsGetAsync(responseCb, method);
    }

    private static HttpWebRequest DoAsPostAsync(IHttp http, Action<HttpResponse> responseCb, string method)
    {
        return http.AsPostAsync(responseCb, method);
    }

    private static void ProcessResponse(INetRequest request, HttpResponse httpResponse,
        NetRequestAsyncHandle asyncHandle, Action<INetResponse, NetRequestAsyncHandle> callback)
    {
        var netResponse = ConvertToNetResponse(request, httpResponse);
        callback(netResponse, asyncHandle);
    }

    private static string EncodeParameters(IEnumerable<Parameter> parameters)
    {
        return string.Join("&", parameters.Select(EncodeParameter).ToArray());
    }

    private static string EncodeParameter(Parameter parameter)
    {
        return parameter.Value == null
            ? parameter.Name.UrlEncode() + "="
            : parameter.Name.UrlEncode() + "=" + parameter.Value.ToString().UrlEncode();
    }

    private void ConfigureHttp(INetRequest request, IHttp http)
    {
        http.Encoding = Encoding;
        http.AlwaysMultipartFormData = request.AlwaysMultipartFormData;
        http.UseDefaultCredentials = request.UseDefaultCredentials;
        http.ResponseWriter = request.ResponseWriter;
        http.CookieContainer = CookieContainer;
        using (var enumerator = DefaultParameters.GetEnumerator())
        {
            while (enumerator.MoveNext())
            {
                var p2 = enumerator.Current;
                var flag = request.Parameters.Any(p => p.Name == p2.Name && p.Type == p2.Type);
                if (!flag) request.AddParameter(p2);
            }
        }

        var flag2 = request.Parameters.All(p => p.Name.ToLowerInvariant() != "accept");
        if (flag2)
        {
            var text = string.Join(", ", AcceptTypes.ToArray());
            request.AddParameter("Accept", text, ParameterType.HttpHeader);
        }

        http.Url = BuildUri(request);
        http.PreAuthenticate = PreAuthenticate;
        var text2 = UserAgent ?? http.UserAgent;
        http.UserAgent = !string.IsNullOrEmpty(text2) ? text2 : "WPFLauncher/" + version;
        var num = request.Timeout > 0 ? request.Timeout : Timeout;
        var flag3 = num > 0;
        if (flag3) http.Timeout = num;
        var num2 = request.ReadWriteTimeout > 0 ? request.ReadWriteTimeout : ReadWriteTimeout;
        var flag4 = num2 > 0;
        if (flag4) http.ReadWriteTimeout = num2;
        http.FollowRedirects = FollowRedirects;
        var flag5 = ClientCertificates != null;
        if (flag5) http.ClientCertificates = ClientCertificates;
        http.MaxRedirects = MaxRedirects;
        http.CachePolicy = CachePolicy;
        var flag6 = request.Credentials != null;
        if (flag6) http.Credentials = request.Credentials;
        var enumerable = from p in request.Parameters
            where p.Type == ParameterType.HttpHeader
            select new HttpHeader
            {
                Name = p.Name,
                Value = Convert.ToString(p.Value)
            };
        foreach (var httpHeader in enumerable) http.Headers.Add(httpHeader);
        var enumerable2 = from p in request.Parameters
            where p.Type == ParameterType.Cookie
            select new HttpCookie
            {
                Name = p.Name,
                Value = Convert.ToString(p.Value)
            };
        foreach (var httpCookie in enumerable2) http.Cookies.Add(httpCookie);
        var enumerable3 = from p in request.Parameters
            where p.Type == ParameterType.GetOrPost && p.Value != null
            select new HttpParameter
            {
                Name = p.Name,
                Value = Convert.ToString(p.Value)
            };
        foreach (var httpParameter in enumerable3) http.Parameters.Add(httpParameter);
        foreach (var fileParameter in request.Files)
            http.Files.Add(new HttpFile
            {
                Name = fileParameter.Name,
                ContentType = fileParameter.ContentType,
                Writer = fileParameter.Writer,
                FileName = fileParameter.FileName,
                ContentLength = fileParameter.ContentLength
            });
        var parameter = request.Parameters.FirstOrDefault(p => p.Type == ParameterType.RequestBody);
        var flag7 = parameter != null;
        if (flag7)
        {
            http.RequestContentType = parameter.Name;
            var flag8 = !http.Files.Any();
            if (flag8)
            {
                var value = parameter.Value;
                var flag9 = value is byte[];
                if (flag9)
                    http.RequestBodyBytes = (byte[])value;
                else
                    http.RequestBody = Convert.ToString(parameter.Value);
            }
            else
            {
                http.Parameters.Add(new HttpParameter
                {
                    Name = parameter.Name,
                    Value = Convert.ToString(parameter.Value),
                    ContentType = parameter.ContentType
                });
            }
        }
    }

    private static NetResponse ConvertToNetResponse(INetRequest request, HttpResponse httpResponse)
    {
        var netResponse = new NetResponse
        {
            Content = httpResponse.Content,
            ContentEncoding = httpResponse.ContentEncoding,
            ContentLength = httpResponse.ContentLength,
            ContentType = httpResponse.ContentType,
            ErrorException = httpResponse.ErrorException,
            ErrorMessage = httpResponse.ErrorMessage,
            RawBytes = httpResponse.RawBytes,
            ResponseStatus = httpResponse.ResponseStatus,
            ResponseUri = httpResponse.ResponseUri,
            Server = httpResponse.Server,
            StatusCode = httpResponse.StatusCode,
            StatusDescription = httpResponse.StatusDescription,
            Request = request,
            ProtocolVersion = httpResponse.ProtocolVersion
        };
        foreach (var httpHeader in httpResponse.Headers)
            netResponse.Headers.Add(new Parameter
            {
                Name = httpHeader.Name,
                Value = httpHeader.Value,
                Type = ParameterType.HttpHeader
            });
        foreach (var httpCookie in httpResponse.Cookies)
            netResponse.Cookies.Add(new NetResponseCookie
            {
                Comment = httpCookie.Comment,
                CommentUri = httpCookie.CommentUri,
                Discard = httpCookie.Discard,
                Domain = httpCookie.Domain,
                Expired = httpCookie.Expired,
                Expires = httpCookie.Expires,
                HttpOnly = httpCookie.HttpOnly,
                Name = httpCookie.Name,
                Path = httpCookie.Path,
                Port = httpCookie.Port,
                Secure = httpCookie.Secure,
                TimeStamp = httpCookie.TimeStamp,
                Value = httpCookie.Value,
                Version = httpCookie.Version
            });
        return netResponse;
    }

    public byte[] DownloadData(INetRequest request, bool throwOnError)
    {
        var netResponse = Execute(request);
        var flag = netResponse.ResponseStatus == ResponseStatus.Error && throwOnError;
        if (flag) throw netResponse.ErrorException;
        return netResponse.RawBytes;
    }

    // 用字符串替换请求体
    private void SetRequestBody(INetRequest request, string newBody)
    {
        foreach (var param in request.Parameters)
            if (param.Type == ParameterType.RequestBody)
            {
                param.Value = newBody;
                break;
            }
    }

    // 用字节数组替换请求体
    private void SetRequestBody(INetRequest request, byte[] newBody)
    {
        foreach (var param in request.Parameters)
            if (param.Type == ParameterType.RequestBody)
            {
                param.Value = newBody;
                break;
            }
    }

    private INetResponse Execute(INetRequest request, string httpMethod, Func<IHttp, string, HttpResponse> getResponse)
    {
        INetResponse netResponse = new NetResponse();
        try
        {
            var http = HttpFactory.Create();
            ConfigureHttp(request, http);
            netResponse = ConvertToNetResponse(request, getResponse(http, httpMethod));
            netResponse.Request = request;
            netResponse.Request.IncreaseNumAttempts();
        }
        catch (Exception ex)
        {
            netResponse.ResponseStatus = ResponseStatus.Error;
            netResponse.ErrorMessage = ex.Message;
            netResponse.ErrorException = ex;
        }

        return netResponse;
    }

    private static HttpResponse DoExecuteAsGet(IHttp http, string method)
    {
        return http.AsGet(method);
    }

    private static HttpResponse DoExecuteAsPost(IHttp http, string method)
    {
        return http.AsPost(method);
    }

    private string GetEffectiveUserAgent()
    {
        return string.IsNullOrEmpty(UserAgent) ? "WPFLauncher/" + version : UserAgent;
    }
}
