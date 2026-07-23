using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Mcl.Core.Dotnetdetour.Models.Config;
using Mcl.Core.Dotnetdetour.Models.Globals;
using Mcl.Core.Dotnetdetour.UI.WebAssets;
using Mcl.Core.NeteaseProtocol;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WPFControls.Helpers;
using WPFLauncher.Common;
using WPFLauncher.Manager;
using WPFLauncher.Manager.Configuration;
using WPFLauncher.Manager.Game;
using WPFLauncher.Model;
using WPFLauncher.Model.Component;
using WPFLauncher.Network.Service;
using WPFLauncher.Network.TransService;
using WPFLauncher.Util;
using WPFLauncher.View.Launcher.LobbyGame;
using WPFLauncher.ViewModel.LobbyGame;
using Exception = System.Exception;

namespace Mcl.Core.Dotnetdetour.Utilities.Network;

public class SimpleHttpServer
{
    private HttpListener _httpListener;

    #region Response Handler

    /// <summary>
    ///     处理返回响应
    /// </summary>
    /// <param name="context">客户端请求数据</param>
    /// <param name="message">返回内容</param>
    /// <param name="code">返回代码(json中)</param>
    /// <param name="data">包含的数据</param>
    private void handleResponse(HttpListenerContext context, string message, int code, dynamic data)
    {
        dynamic serverResponse = new ExpandoObject();
        serverResponse.code = code;
        serverResponse.message = message;
        var applyResponse = popObject(serverResponse, data);
        sendJsonResponse(context.Response, applyResponse);
    }

    #endregion

    // 启动 HTTP 服务器
    public void Start(string urlPrefix)
    {
        if (!HttpListener.IsSupported)
        {
            WpfConfig.DefaultLogger.Error("[Http]当前系统不支持 HttpListener.");
            return;
        }

        // 使用配置中的HTTP端口
        var httpAddress = $"http://127.0.0.1:{WpfConfig.HttpPort}/";

        _httpListener = new HttpListener();
        _httpListener.Prefixes.Add(httpAddress); // 使用配置的地址
        _httpListener.Start();
        WpfConfig.DefaultLogger.Info($"[Http]HTTP 服务器已启动,监听 {httpAddress}");

        // 开始处理请求
        while (true)
            try
            {
                // 等待并获取客户端请求
                var context = _httpListener.GetContext();

                // 设置通用 CORS 头（允许所有来源、方法、头部）
                context.Response.Headers["Access-Control-Allow-Origin"] = "*";
                context.Response.Headers["Access-Control-Allow-Methods"] = "GET, POST, PUT, DELETE, OPTIONS";
                context.Response.Headers["Access-Control-Allow-Headers"] =
                    "*"; // 或指定具体头，如 "Content-Type, Authorization"

                try
                {
                    if (context.Request.HttpMethod == "OPTIONS")
                    {
                        HandleOptionsRequest(context);
                    }
                    else if (apiRequestList.Contains(context.Request.Url.AbsolutePath))
                    {
                        handleWebApiRequest(context);
                    }
                    else if (context.Request.Url.AbsolutePath.StartsWith("/api/") &&
                             !apiRequestList.Contains(context.Request.Url.AbsolutePath))
                    {
                        handleApiRequest(context);
                    }
                    else if (context.Request.Url.AbsolutePath.StartsWith("/api_DynamicToken/"))
                    {
                        handleDynamicRequest(context);
                    }
                    else if (context.Request.Url.AbsolutePath.StartsWith("/websocket"))
                    {
                        HandleWebSocketRequest(context);
                    }
                    else
                    {
                        // 判断请求方法是否是 POST
                        if (context.Request.HttpMethod == "POST")
                            HandlePostRequest(context);
                        else if (context.Request.HttpMethod == "GET") handleGetRequest(context);
                        // 非 POST 请求返回 405 Method Not Allowed
                        /*context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                        var errorResponse = new
                        {
                            message = "Only POST requests are allowed"
                        };
                        SendJsonResponse(context.Response, errorResponse);*/
                    }
                }
                catch (Exception e)
                {
                    WpfConfig.DefaultLogger.Error($"处理请求时发生错误: {e.Message}");
                    WpfConfig.DefaultLogger.Error("[STACK TRACE]:" + e.StackTrace);
                }
            }
            catch (Exception ex)
            {
                WpfConfig.DefaultLogger.Error($"处理请求时发生错误: {ex.Message}");
            }
    }

    // 停止服务器
    public void Stop()
    {
        _httpListener.Stop();
        WpfConfig.DefaultLogger.Info("HTTP 服务器已停止");
    }

    [DllImport("kernel32.dll")]
    private static extern bool AllocConsole();

    [DllImport("kernel32.dll")]
    private static extern bool FreeConsole();

    [DllImport("kernel32.dll")]
    private static extern bool SetConsoleOutputCP(uint wCodePageID);

    #region Tools

    /// <summary>
    ///     字符串查询解析工具
    /// </summary>
    /// <param name="query">URL网址</param>
    /// <returns>解析后的数据</returns>
    private NameValueCollection ParseQueryString(string query)
    {
        // 替代 HttpUtility.ParseQueryString 的方法
        var nvc = new NameValueCollection();
        if (string.IsNullOrEmpty(query)) return nvc;

        foreach (var pair in query.TrimStart('?').Split('&'))
        {
            var keyValue = pair.Split('=');
            if (keyValue.Length != 2) continue;

            var key = Uri.UnescapeDataString(keyValue[0]);
            var value = Uri.UnescapeDataString(keyValue[1]);
            nvc.Add(key, value);
        }

        return nvc;
    }

    /// <summary>
    ///     数据合并工具
    /// </summary>
    /// <param name="Object">第一个数据对象</param>
    /// <param name="SecondObject">第二个数据对象</param>
    /// <returns>合并后的数据对象</returns>
    private dynamic popObject(dynamic Object, dynamic SecondObject)
    {
        if (SecondObject != null && Object != null)
        {
            var responseDict = (IDictionary<string, object>)Object;
            var dataDict = (IDictionary<string, object>)SecondObject;
            foreach (var kvp in dataDict) responseDict[kvp.Key] = kvp.Value;
            return responseDict;
        }

        return null;
    }

    #endregion

    #region Send Response

    /// <summary>
    ///     发送Json响应到客户端
    /// </summary>
    /// <param name="response">响应</param>
    /// <param name="responseObject">响应的数据类型</param>
    private void sendJsonResponse(HttpListenerResponse response, object responseObject)
    {
        var jsonResponse = JsonConvert.SerializeObject(responseObject, Formatting.Indented);
        var responseBytes = Encoding.UTF8.GetBytes(jsonResponse);
        customContentTypeResponse(response, "application/json", responseBytes);
    }

    /// <summary>
    ///     发送Html数据到客户端
    /// </summary>
    /// <param name="response">响应</param>
    /// <param name="htmlContent">html文本</param>
    private void sendHtmlResponse(HttpListenerResponse response, string htmlContent)
    {
        var responseBytes = Encoding.UTF8.GetBytes(htmlContent);
        customContentTypeResponse(response, "text/html", responseBytes);
    }

    /// <summary>
    ///     发送自定义数据到客户端
    /// </summary>
    /// <param name="response">响应</param>
    /// <param name="contentType">数据类型</param>
    /// <param name="responseBytes">数据(需转换成字节)</param>
    private void customContentTypeResponse(HttpListenerResponse response, string contentType, byte[] responseBytes)
    {
        response.ContentType = contentType;
        response.ContentLength64 = responseBytes.Length;
        response.StatusCode = (int)HttpStatusCode.OK;
        response.OutputStream.Write(responseBytes, 0, responseBytes.Length);
    }

    #endregion

    #region Handle Request

    public static List<string> apiRequestList = new() { "/api/blacklists/regex", "/api/blacklists/users" };

    private void handleWebApiRequest(HttpListenerContext context)
    {
        if (context.Request.Url.AbsolutePath == apiRequestList[0])
        {
            // Generation Result
            dynamic serverResponse = new ExpandoObject();
            serverResponse.error = 0;
            serverResponse.isEnabled = true;
            serverResponse.patterns = (WpfConfig.RegexBlacklist ?? new List<string>()).Select(pattern => new
            {
                pattern,
                isValid = true // 假设所有模式都是有效的
            }).ToList<object>();
            serverResponse.count = serverResponse.patterns.Count;
            handleResponse(context, "获取正则表达式列表成功", 0, serverResponse);
        }
        else if (context.Request.Url.AbsolutePath == apiRequestList[1])
        {
            // 新的API端点，返回规范化的用户黑名单列表，并包含用户详情
            if (WpfConfig.EnableRoomBlacklist)
            {
                var blacklistUsers = new List<object>();
                if (WpfConfig.RoomBlacklist != null && WpfConfig.RoomBlacklist.Count > 0)
                    try
                    {
                        var userDetails = X19Http.GetPlayersInfo(WpfConfig.RoomBlacklist);
                        if (userDetails != null && userDetails["entities"] != null)
                            foreach (var user in userDetails["entities"])
                                blacklistUsers.Add(new
                                {
                                    userId = user["entity_id"]?.ToString(),
                                    userName = user["name"]?.ToString() ?? "未知用户",
                                    avatarUrl = user["avatar_image_url"]?.ToString() ??
                                                "https://x19.fp.ps.netease.com/file/5a34e0777f9d2a8a4ea3d36eza31LhW3",
                                    signature = user["signature"]?.ToString() ?? ""
                                });

                        // 处理可能缺失的用户
                        var foundUserIds = new HashSet<string>();
                        foreach (var user in blacklistUsers) foundUserIds.Add(((dynamic)user).userId.ToString());

                        foreach (var userId in WpfConfig.RoomBlacklist)
                            if (!foundUserIds.Contains(userId))
                                blacklistUsers.Add(new
                                {
                                    userId,
                                    userName = $"用户{userId}",
                                    avatarUrl = "https://x19.fp.ps.netease.com/file/5a34e0777f9d2a8a4ea3d36eza31LhW3",
                                    signature = ""
                                });
                    }
                    catch (Exception ex)
                    {
                        // 如果获取用户详情失败，至少返回基本ID信息
                        foreach (var userId in WpfConfig.RoomBlacklist)
                            blacklistUsers.Add(new
                            {
                                userId,
                                userName = $"用户{userId}",
                                avatarUrl = "https://x19.fp.ps.netease.com/file/5a34e0777f9d2a8a4ea3d36eza31LhW3",
                                signature = ""
                            });
                        WpfConfig.DefaultLogger.Error($"[HTTP] 获取黑名单用户详情失败: {ex.Message}");
                    }

                dynamic SendResponse = new ExpandoObject();
                SendResponse.error = 0;
                SendResponse.isEnabled = true;
                SendResponse.users = blacklistUsers.ToList();
                SendResponse.count = blacklistUsers.Count;
                // SendResponse = new {
                //     error = 0,
                //     isEnabled = true,
                //     users = blacklistUsers.ToList<object>(),
                //     count = blacklistUsers.Count
                // };
                handleResponse(context, "获取用户黑名单成功", 0, SendResponse);
            }
            else
            {
                dynamic SendResponse = new ExpandoObject();
                SendResponse.error = 0;
                SendResponse.isEnabled = false;
                SendResponse.users = new List<object>();
                SendResponse.count = 0;
                // SendResponse = new {
                //     error = 0,
                //     isEnabled = false,
                //     users = new List<int>() {},
                //     count = 0
                // };
                handleResponse(context, "用户黑名单功能未开启", 1, SendResponse);
            }
        }
    }

    private void handleApiRequest(HttpListenerContext context)
    {
        // 读取 POST 请求的内容
        using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
        {
            var requestBody = reader.ReadToEnd();
            dynamic SendResponse = new ExpandoObject();
            dynamic requestObject = null;
            requestObject = JsonConvert.DeserializeObject<dynamic>(requestBody);
            // 获取"/api/"后面的内容
            var contentAfterApiPost = "/" + context.Request.Url.AbsolutePath.Substring(5);
            WpfConfig.DefaultLogger.Info($"[POST]args:{contentAfterApiPost}");
            if (context.Request.HttpMethod == "POST")
            {
                // 使用 GBK 编码（适用于简体中文环境）
                var gbkEncoding = Encoding.GetEncoding("gbk");
                var gbkBytes = gbkEncoding.GetBytes(requestBody);
                var decodedFromGBK = Encoding.UTF8.GetString(gbkBytes);
                var http = new HttpClient();
                var userToken = ss.e(contentAfterApiPost, decodedFromGBK);
                http.DefaultRequestHeaders.Clear();
                http.DefaultRequestHeaders.Add("user-id", azf<arg>.Instance.User.Id);
                http.DefaultRequestHeaders.Add("user-token", userToken);
                var content = new StringContent(decodedFromGBK, Encoding.UTF8, "application/json");
                var responseData = http.PostAsync(azf<axi>.Instance.Url.ApiGatewayUrl + contentAfterApiPost, content)
                    .Result;
                var get_result = responseData.Content.ReadAsStringAsync().Result;
                try
                {
                    SendResponse = JsonConvert.DeserializeObject<JObject>(get_result);
                }
                catch (JsonReaderException)
                {
                    SendResponse = get_result;
                }

                if (WpfConfig.IsDebug) WpfConfig.DefaultLogger.Info("[HTTP][POST]请求返回内容:" + get_result);
            }
            else if (context.Request.HttpMethod == "GET")
            {
                var IsPostFlag = false;
                // 解析查询字符串
                var queryParams = context.Request.QueryString;
                if (queryParams.Count > 0 && queryParams["Method"] == "POST")
                {
                    // 将查询字符串参数添加到请求体中

                    /*var queryParamsDict = new Dictionary<string, string>();
                    foreach (string key in queryParams.AllKeys)
                    {
                        // 检查当前键是否为 "Method"，如果是则跳过
                        if (key != "method")
                        {
                            queryParamsDict[key] = queryParams[key];
                        }
                    }*/

                    // 将查询参数序列化为 JSON 字符串并合并到请求体
                    try
                    {
                        requestBody = JsonConvert.SerializeObject(JsonConvert.DeserializeObject(queryParams["data"]));
                    }
                    catch (Exception e)
                    {
                        requestBody = queryParams["data"];
                    }
                    //requestBody = $"{requestBody.TrimEnd('}')}, {queryParamsJson.TrimStart('{')}";


                    var httpClient = new HttpClient();
                    var userTokenValue = ss.e(contentAfterApiPost, requestBody);
                    httpClient.DefaultRequestHeaders.Clear();
                    httpClient.DefaultRequestHeaders.Add("user-id", azf<arg>.Instance.User.Id);
                    httpClient.DefaultRequestHeaders.Add("user-token", userTokenValue);
                    var stringContent = new StringContent(requestBody, Encoding.UTF8, "application/json");
                    var httpResponseData = httpClient
                        .PostAsync(azf<axi>.Instance.Url.ApiGatewayUrl + contentAfterApiPost, stringContent).Result;
                    var resultContent = httpResponseData.Content.ReadAsStringAsync().Result;
                    try
                    {
                        SendResponse = JsonConvert.DeserializeObject<JObject>(resultContent);
                    }
                    catch (JsonReaderException)
                    {
                        SendResponse = resultContent;
                    }

                    if (WpfConfig.IsDebug) WpfConfig.DefaultLogger.Info("[HTTP][POST]请求返回内容:" + resultContent);
                    IsPostFlag = true;
                }

                if (!IsPostFlag)
                {
                    var http = new HttpClient();
                    var userToken = ss.e(contentAfterApiPost, "");
                    http.DefaultRequestHeaders.Clear();
                    http.DefaultRequestHeaders.Add("user-id", azf<arg>.Instance.User.Id);
                    http.DefaultRequestHeaders.Add("user-token", userToken);
                    var responseData = http.GetAsync(azf<axi>.Instance.Url.ApiGatewayUrl + contentAfterApiPost).Result;
                    var get_result = responseData.Content.ReadAsStringAsync().Result;
                    try
                    {
                        SendResponse = JsonConvert.DeserializeObject<JObject>(get_result);
                    }
                    catch (JsonReaderException)
                    {
                        SendResponse = get_result;
                    }

                    if (WpfConfig.IsDebug) WpfConfig.DefaultLogger.Info("[HTTP][GET]请求返回内容:" + get_result);
                }
            }

            sendJsonResponse(context.Response, SendResponse);
        }
    }

    private void handleDynamicRequest(HttpListenerContext context)
    {
        // 读取 POST 请求的内容
        using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
        {
            var requestBody = reader.ReadToEnd();
            dynamic SendResponse = new ExpandoObject();
            dynamic requestObject = null;
            requestObject = JsonConvert.DeserializeObject<dynamic>(requestBody);
            // 获取"/api/"后面的内容
            var contentAfterApiPost = "/" + context.Request.Url.AbsolutePath.Substring(5);
            WpfConfig.DefaultLogger.Info($"[POST]args:{contentAfterApiPost}");
            if (context.Request.HttpMethod == "POST")
            {
                var http = new HttpClient();
                var userToken = ss.e(contentAfterApiPost, requestBody);
                SendResponse.UserToken = userToken;
            }
            else if (context.Request.HttpMethod == "GET")
            {
                var IsPostFlag = false;
                // 解析查询字符串
                var queryParams = context.Request.QueryString;
                if (queryParams.Count > 0 && queryParams["Method"] == "POST")
                {
                    try
                    {
                        requestBody = JsonConvert.SerializeObject(JsonConvert.DeserializeObject(queryParams["data"]));
                    }
                    catch (Exception e)
                    {
                        requestBody = queryParams["data"];
                    }

                    var httpClient = new HttpClient();
                    var userTokenValue = ss.e(contentAfterApiPost, requestBody);
                    SendResponse.UserToken = userTokenValue;
                    IsPostFlag = true;
                }

                if (!IsPostFlag)
                {
                    var http = new HttpClient();
                    var userToken = ss.e(contentAfterApiPost, "");
                    SendResponse.UserToken = userToken;
                }
            }

            sendJsonResponse(context.Response, SendResponse);
        }
    }

    private void handleGetRequest(HttpListenerContext context)
    {
        var IsSendResponseFlag = true;
        dynamic SendResponse = new ExpandoObject();
        switch (context.Request.Url.AbsolutePath)
        {
            case "/test":
                var adeType = typeof(ade);
                var ffield = adeType.GetField("f", BindingFlags.NonPublic | BindingFlags.Static);
                var cfield = adeType.GetField("c", BindingFlags.Public | BindingFlags.Instance);

                if (ffield == null)
                    SendResponse.error = 1;
                else
                    SendResponse.f = ffield.GetValue(azf<arg>.Instance);
                if (cfield == null)
                    SendResponse.error = 2;
                else
                    SendResponse.c = cfield.GetValue(azf<arg>.Instance);
                break;
            case "/help":
                // 重定向到URL : https://wpflauncherhook.apifox.cn/
                context.Response.Redirect("https://wpflauncherhook.apifox.cn/");
                IsSendResponseFlag = false;
                context.Response.Close(); // 确保响应被正确关闭
                break;
            case "/LanGame/PlayerList":
                SendResponse.code = 0;
                SendResponse.message = "正常返回";
                SendResponse.data = new
                {
                    playerlist = WebRtcVar.PlayerList,
                    InLanGame = WebRtcVar.LanGameManager != null
                };
                break;
            case "/get_login_info":
                // SendResponse.a = (string)typeof(arf).GetField("a").GetValue(WPFLauncher.Common.azf<arg>.Instance);
                // ;
                // SendResponse.b = (string)typeof(arf).GetField("b").GetValue(WPFLauncher.Common.azf<arg>.Instance);
                // ;
                SendResponse.UserInfo =
                    JsonConvert.DeserializeObject<JObject>(
                        JsonConvert.SerializeObject(azf<arg>.Instance.User));
                SendResponse.NotifyMessageList = azf<arg>.Instance.NotifyMessageList;
                SendResponse.RepairList = azf<arg>.Instance.RepairList;
                SendResponse.ClusterList = azf<arg>.Instance.ClusterList;
                SendResponse.JavaFixM = azf<arg>.Instance.JavaFixM;
                SendResponse.UserM =
                    JsonConvert.DeserializeObject<JObject>(
                        JsonConvert.SerializeObject(azf<UserM>.Instance));
                break;
            case "/get_x19info":
                SendResponse =
                    JsonConvert.DeserializeObject<JObject>(
                        JsonConvert.SerializeObject(azf<axi>.Instance));
                break;
            case "/get_roominfo":
                if (WpfConfig.RoomInfo != null)
                {
                    // 创建更完整的房间信息响应
                    dynamic response = new ExpandoObject();

                    // 设置基本房间信息
                    response.roomInfo = WpfConfig.RoomInfo;

                    // 提取和设置常用属性
                    var entity = WpfConfig.RoomInfo.entity;
                    response.roomId = entity.entity_id;
                    response.roomName = entity.room_name;
                    response.ownerId = entity.owner_id;
                    response.maxPlayers = entity.max_count;
                    response.currentPlayers = entity.cur_num;
                    response.gameStatus = entity.game_status;
                    response.version = entity.version;
                    response.hasPassword = entity.password != null && entity.password.ToString() != "0";
                    response.password = WpfConfig.Password;
                    response.visibility = entity.visibility;
                    response.allowSave = entity.allow_save;
                    response.resId = entity.res_id;
                    SendResponse.UserJoinTime = WpfConfig.JoinOrCreateTime;
                    response.UserInputPassword = WpfConfig.Password;

                    // 设置当前用户ID
                    response.currentUserId = azf<arg>.Instance.User.Id;

                    // 判断是否是房主
                    response.isOwner = entity.owner_id == azf<arg>.Instance.User.Id;

                    // 房间黑名单配置信息
                    response.isRoomBlacklistEnabled = WpfConfig.EnableRoomBlacklist;
                    response.isRegexBlacklistEnabled = WpfConfig.EnableRoomBlacklist;

                    // 获取并设置玩家信息
                    if (entity.fids != null && entity.fids.Count > 0)
                    {
                        // 获取玩家详细信息
                        var playersInfoResponse = X19Http.Post("/user/query/search-by-ids",
                            JsonConvert.SerializeObject(new { entity_ids = entity.fids }));
                        response.playersInfo = JsonConvert.DeserializeObject(playersInfoResponse);

                        // 处理玩家列表数据，适配前端展示
                        var playersList = new List<dynamic>();
                        var playersMap = new Dictionary<string, dynamic>();
                        var playersInfo = JObject.Parse(playersInfoResponse);

                        if (playersInfo["entities"] != null)
                            foreach (var playerInfo in playersInfo["entities"])
                            {
                                var userId = playerInfo["entity_id"].ToString();
                                var playerName = playerInfo["name"].ToString();
                                var avatarUrl = playerInfo["avatar_image_url"]?.ToString() ??
                                                "https://x19.fp.ps.netease.com/file/5a34e0777f9d2a8a4ea3d36eza31LhW3";
                                var frameId = playerInfo["frame_id"]?.ToString() ?? "";
                                var gender = playerInfo["gender"]?.ToString() ?? "m";
                                var signature = playerInfo["signature"]?.ToString() ?? "";

                                var playerData = new ExpandoObject() as dynamic;
                                playerData.userId = userId;
                                playerData.playerName = playerName;
                                playerData.avatarUrl = avatarUrl;
                                playerData.role = userId == entity.owner_id ? "房主" : "成员";
                                playerData.ident = userId == entity.owner_id ? 1 : 0;
                                playerData.inBlacklist = WpfConfig.RoomBlacklist != null &&
                                                         WpfConfig.RoomBlacklist.Contains(userId);
                                playerData.frameId = frameId;
                                playerData.gender = gender;
                                playerData.signature = signature;

                                playersList.Add(playerData);
                                playersMap[userId] = playerData;
                            }

                        response.playersList = playersList;
                        response.playersMap = playersMap;
                        response.playerCount = playersList.Count;
                    }

                    // 获取并设置房主信息
                    var ownerInfoResponse = X19Http.Post("/user/query/search-by-uid",
                        JsonConvert.SerializeObject(new { user_id = entity.owner_id }));
                    response.ownerInfo = JObject.Parse(ownerInfoResponse);

                    // 获取房间黑名单
                    if (WpfConfig.EnableRoomBlacklist)
                        response.roomBlacklist = WpfConfig.RoomBlacklist;
                    else
                        response.roomBlacklist = new List<string>();

                    // 获取正则黑名单
                    if (WpfConfig.EnableRoomBlacklist)
                        response.regexBlacklist = WpfConfig.RegexBlacklist;
                    else
                        response.regexBlacklist = new List<string>();

                    // 获取地图资源信息
                    if (!string.IsNullOrEmpty(entity.res_id))
                    {
                        var resourceInfoResponse = X19Http.Post("/item-details/get_v2",
                            JsonConvert.SerializeObject(new { item_id = entity.res_id }));
                        response.resourceInfo = JsonConvert.DeserializeObject(resourceInfoResponse);

                        // 获取资源图片信息
                        var titleImageResponse = X19Http.Post("/item-channel/query/search-by-item-channel",
                            JsonConvert.SerializeObject(new { item_id = entity.res_id, channel_id = "11" }));
                        response.roomTitleImage = JsonConvert.DeserializeObject(titleImageResponse);
                    }

                    SendResponse = response;
                }
                else
                {
                    SendResponse = new { error = 1, message = "当前未在房间中" };
                }

                break;
            case "/get_roominfo_fast":
                // 设置基本房间信息
                SendResponse.roomInfo = WpfConfig.RoomInfo;

                // 提取和设置常用属性 WpfConfig.RoomInfo.entity;
                SendResponse.roomId = WpfConfig.RoomInfo.entity.entity_id;
                SendResponse.roomName = WpfConfig.RoomInfo.entity.room_name;
                SendResponse.ownerId = WpfConfig.RoomInfo.entity.owner_id;
                SendResponse.maxPlayers = WpfConfig.RoomInfo.entity.max_count;
                SendResponse.currentPlayers = WpfConfig.RoomInfo.entity.cur_num;
                SendResponse.gameStatus = WpfConfig.RoomInfo.entity.game_status;
                SendResponse.version = WpfConfig.RoomInfo.entity.version;
                SendResponse.hasPassword = WpfConfig.RoomInfo.entity.password != null &&
                                           WpfConfig.RoomInfo.entity.password.ToString() != "0";
                SendResponse.password = WpfConfig.Password;
                SendResponse.visibility = WpfConfig.RoomInfo.entity.visibility;
                SendResponse.allowSave = WpfConfig.RoomInfo.entity.allow_save;
                SendResponse.resId = WpfConfig.RoomInfo.entity.res_id;
                SendResponse.UserInputPassword = WpfConfig.Password;
                SendResponse.UserJoinTime = WpfConfig.JoinOrCreateTime;
                SendResponse.PlayerList = WpfConfig.RoomPlayerList;
                // 设置当前用户ID
                SendResponse.currentUserId = azf<arg>.Instance.User.Id;

                // 判断是否是房主
                SendResponse.isOwner = WpfConfig.RoomInfo.entity.owner_id == azf<arg>.Instance.User.Id;

                // 房间黑名单配置信息
                SendResponse.isRoomBlacklistEnabled = WpfConfig.EnableRoomBlacklist;
                SendResponse.isRegexBlacklistEnabled = WpfConfig.EnableRoomBlacklist;
                break;
            case "/get_pathinfo":
                var staticMembers =
                    typeof(tb).GetFields(BindingFlags.Static | BindingFlags.Public);

                foreach (var member in staticMembers)
                    ((IDictionary<string, object>)SendResponse)[member.Name] =
                        member.GetValue(null);

                SendResponse =
                    JsonConvert.DeserializeObject<JObject>(
                        JsonConvert.SerializeObject(SendResponse));
                break;
            case "/get_userCppToken":
                var text = ss.f();
                var array = tk.c(text);
                SendResponse.UserCppToken = text;
                SendResponse.Base64Token = Convert.ToBase64String(array);
                break;
            case "/get_RecvInfo":
                SendResponse = new { ade.SendKey, ade.RecvKey, DataList = WpfConfig.RecvList };
                break;
            case "/get_RoomBlacklist":
                if (WpfConfig.EnableRoomBlacklist) // 判断房间黑名单功能是否开启
                {
                    if (WpfConfig.RoomBlacklist != null && WpfConfig.RoomBlacklist.Count > 0)
                        // 获取黑名单用户详细信息
                        try
                        {
                            var blacklistUsersInfo = X19Http.GetPlayersInfo(WpfConfig.RoomBlacklist);
                            SendResponse = new
                            {
                                error = 0,
                                message = "获取黑名单成功",
                                blacklist = WpfConfig.RoomBlacklist.ToList(),
                                blacklistInfo = blacklistUsersInfo,
                                count = WpfConfig.RoomBlacklist.Count
                            };
                        }
                        catch (Exception ex)
                        {
                            // 如果获取详细信息失败，至少返回ID列表
                            SendResponse = new
                            {
                                error = 0,
                                message = "获取黑名单成功，但获取用户详情失败",
                                blacklist = WpfConfig.RoomBlacklist.ToList(),
                                errorDetail = ex.Message,
                                count = WpfConfig.RoomBlacklist.Count
                            };
                        }
                    else
                        SendResponse = new { error = 0, message = "黑名单为空", blacklist = new List<string>(), count = 0 };
                }
                else
                {
                    SendResponse = new { error = 1, message = "房间黑名单功能未开启", isEnabled = false };
                }

                break;
            case "/get_RegexBlacklist":
                if (WpfConfig.EnableRoomBlacklist) // 判断正则表达式黑名单功能是否开启
                {
                    if (WpfConfig.RegexBlacklist != null && WpfConfig.RegexBlacklist.Count > 0)
                    {
                        // 返回更详细的黑名单信息
                        var regexDetails = new List<object>();
                        foreach (var pattern in WpfConfig.RegexBlacklist)
                            regexDetails.Add(new
                            {
                                pattern,
                                isValid = true // 假设所有模式都是有效的
                            });

                        SendResponse = new
                        {
                            error = 0,
                            message = "获取正则黑名单成功",
                            regexBlacklist = WpfConfig.RegexBlacklist.ToList(),
                            regexDetails,
                            count = WpfConfig.RegexBlacklist.Count
                        };
                    }
                    else
                    {
                        SendResponse = new
                            { error = 0, message = "正则黑名单为空", regexBlacklist = new List<string>(), count = 0 };
                    }
                }
                else
                {
                    SendResponse = new { error = 1, message = "正则表达式黑名单功能未开启", isEnabled = false };
                }

                break;
            case "/blacklist/status":
                // 返回黑名单功能状态信息
                SendResponse = new
                {
                    error = 0,
                    roomBlacklist = new
                    {
                        isEnabled = WpfConfig.EnableRoomBlacklist,
                        count = WpfConfig.RoomBlacklist?.Count ?? 0
                    },
                    regexBlacklist = new
                    {
                        isEnabled = WpfConfig.EnableRoomBlacklist,
                        count = WpfConfig.RegexBlacklist?.Count ?? 0
                    }
                };
                break;
            case "/Send_ChatMessage":
                try
                {
                    // 使用示例
                    var rawQuery = context.Request.Url.Query;
                    var queryParams = ParseQueryString(rawQuery);

                    var message = queryParams["message"];
                    var ChatUserID = uint.Parse(queryParams["userID"]);

                    if (string.IsNullOrEmpty(message))
                    {
                        SendResponse = new { error = 1, message = "消息内容不能为空,请使用?message=xxx格式" };
                    }
                    else
                    {
                        // 检查编码设置
                        var contentType = context.Request.Headers["Content-Type"] ?? "";
                        if (contentType.ToLower().Contains("charset=utf-8"))
                            message = Encoding.UTF8.GetString(Encoding.Default.GetBytes(message));

                        new acq().e(ChatUserID, message);
                        var startTime = DateTime.Now;
                        while (true)
                        {
                            if (!string.IsNullOrEmpty(WpfConfig.Get_Recv_String_ChatResult))
                            {
                                var Get_Recv_String_ChatResult_ToJson =
                                    JObject.Parse(WpfConfig.Get_Recv_String_ChatResult);
                                if (!((IDictionary<string, JToken>)
                                        Get_Recv_String_ChatResult_ToJson).ContainsKey("Get_Recv_String_ChatResult") &&
                                    Get_Recv_String_ChatResult_ToJson["err"].ToObject<int>() != 0)
                                {
                                    SendResponse = new
                                        { error = 1, message = "发送失败", errorInfo = Get_Recv_String_ChatResult_ToJson };
                                    WpfConfig.Get_Recv_String_ChatResult = string.Empty;
                                }
                                else
                                {
                                    SendResponse = new
                                    {
                                        error = 0, message = "发送成功", SendResult = Get_Recv_String_ChatResult_ToJson,
                                        SendMessage = message, ToUserID = ChatUserID
                                    };
                                    WpfConfig.Get_Recv_String_ChatResult = string.Empty;
                                }

                                break;
                            }

                            if ((DateTime.Now - startTime).TotalSeconds > 3)
                            {
                                SendResponse = new { error = 1, message = "发送超时" };
                                WpfConfig.Get_Recv_String_ChatResult = string.Empty;
                                break;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    SendResponse = new { error = 1, message = "发送失败", errorInfo = e };
                }

                break;
            case "/Send_GroupMessage":
                try
                {
                    // 使用示例
                    var rawQuery = context.Request.Url.Query;
                    var queryParams = ParseQueryString(rawQuery);

                    var message = queryParams["message"];
                    var GroupID = queryParams["groupid"];

                    if (string.IsNullOrEmpty(message))
                    {
                        SendResponse = new { error = 1, message = "消息内容不能为空,请使用?message=xxx格式" };
                    }
                    else
                    {
                        // 检查编码设置
                        var contentType = context.Request.Headers["Content-Type"] ?? "";
                        if (contentType.ToLower().Contains("charset=utf-8"))
                            message = Encoding.UTF8.GetString(Encoding.Default.GetBytes(message));

                        new acq().a(GroupID, message);
                        var startTime = DateTime.Now;
                        while (true)
                        {
                            if (!string.IsNullOrEmpty(WpfConfig.Get_Recv_String_ChatResult))
                            {
                                var Get_Recv_String_ChatResult_ToJson =
                                    JObject.Parse(WpfConfig.Get_Recv_String_ChatResult);
                                if (!((IDictionary<string, JToken>)
                                        Get_Recv_String_ChatResult_ToJson).ContainsKey("Get_Recv_String_ChatResult") &&
                                    Get_Recv_String_ChatResult_ToJson["err"].ToObject<int>() != 0)
                                {
                                    SendResponse = new
                                        { error = 1, message = "发送失败", errorInfo = Get_Recv_String_ChatResult_ToJson };
                                    WpfConfig.Get_Recv_String_ChatResult = string.Empty;
                                }
                                else
                                {
                                    SendResponse = new
                                    {
                                        error = 0, message = "发送成功", SendResult = Get_Recv_String_ChatResult_ToJson,
                                        SendMessage = message, ToGroupID = GroupID
                                    };
                                    WpfConfig.Get_Recv_String_ChatResult = string.Empty;
                                }

                                break;
                            }

                            if ((DateTime.Now - startTime).TotalSeconds > 3)
                            {
                                SendResponse = new { error = 1, message = "发送超时" };
                                WpfConfig.Get_Recv_String_ChatResult = string.Empty;
                                break;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    SendResponse = new { error = 1, message = "发送失败", errorInfo = e };
                }

                break;
            // case "/help_websocket":

            //     SendHtmlResponse(context.Response, websocketHtml);
            //     break;
            default:
                try
                {
                    if (context.Request.Url.AbsolutePath.StartsWith("/User/"))
                    {
                        var contentAfterApiPost = context.Request.Url.AbsolutePath.Substring(6);
                        // 使用逗号分割多个用户ID
                        var userIds = contentAfterApiPost.Split(',').ToList();
                        var response = X19Http.GetPlayersInfo(userIds);
                        SendResponse = JToken.FromObject(response);
                    }
                    else if (context.Request.Url.AbsolutePath == "/roommanage")
                    {
                        try
                        {
                            // 使用HtmlResource获取房间管理页面内容
                            var htmlContent = HtmlResource.GetRoomManageHtml();

                            // 向客户端发送HTML内容
                            context.Response.ContentType = "text/html";
                            var buffer = Encoding.UTF8.GetBytes(htmlContent);
                            context.Response.ContentLength64 = buffer.Length;
                            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                            context.Response.OutputStream.Close();
                            IsSendResponseFlag = false;
                        }
                        catch (Exception ex)
                        {
                            // 处理读取或发送HTML文件时可能出现的错误
                            SendResponse = JToken.FromObject(new { error = 1, message = $"加载房间管理页面失败: {ex.Message}" });
                            WpfConfig.DefaultLogger.Info($"[Http]Error loading RoomManage.html: {ex.Message}");
                        }
                    }
                    else if (context.Request.Url.AbsolutePath.StartsWith("/console/show"))
                    {
                        try
                        {
                            AllocConsole(); // 使用Kernel32.dll分配一个新的控制台
                            // 重新初始化控制台输出（GBK编码，与AppMutexHelper.CheckAppMutex一致）
                            const uint CP_GBK = 936;
                            SetConsoleOutputCP(CP_GBK);
                            Console.OutputEncoding = Encoding.GetEncoding(936);
                            var writer = new StreamWriter(
                                Console.OpenStandardOutput(),
                                Console.OutputEncoding
                            );
                            writer.AutoFlush = true;
                            Console.SetOut(writer);
                            Console.CursorVisible = false;
                            SendResponse = new { error = 0, message = "控制台已显示" };
                        }
                        catch (Exception ex)
                        {
                            SendResponse = new { error = 1, message = "显示控制台失败", errorInfo = ex.Message };
                        }
                    }
                    else if (context.Request.Url.AbsolutePath == "/console/hide")
                    {
                        try
                        {
                            // 先重定向输出流到Null，防止后续WpfConfig.DefaultLogger.Info报IOException
                            Console.SetOut(TextWriter.Null);
                            Console.SetError(TextWriter.Null);
                            FreeConsole(); // 使用Kernel32.dll释放当前进程的控制台
                            SendResponse = new { error = 0, message = "控制台已隐藏" };
                        }
                        catch (Exception ex)
                        {
                            SendResponse = new { error = 1, message = "隐藏控制台失败", errorInfo = ex.Message };
                        }
                    }
                    else if (context.Request.Url.AbsolutePath == "/console/clear")
                    {
                        try
                        {
                            Console.Clear();
                            SendResponse = new { error = 0, message = "控制台已清空" };
                        }
                        catch (Exception ex)
                        {
                            SendResponse = new { error = 1, message = "清空控制台失败", errorInfo = ex.Message };
                        }
                    }
                    else if (context.Request.Url.AbsolutePath == "/console/log")
                    {
                        // 添加日志到控制台
                        try
                        {
                            var rawQuery = context.Request.Url.Query;
                            var queryParams = ParseQueryString(rawQuery);

                            var message = queryParams["message"] ?? "无消息内容";
                            var color = queryParams["color"] ?? "white";

                            var consoleColor = ConsoleColor.White;
                            switch (color.ToLower())
                            {
                                case "red": consoleColor = ConsoleColor.Red; break;
                                case "green": consoleColor = ConsoleColor.Green; break;
                                case "blue": consoleColor = ConsoleColor.Blue; break;
                                case "yellow": consoleColor = ConsoleColor.Yellow; break;
                                case "cyan": consoleColor = ConsoleColor.Cyan; break;
                                case "magenta": consoleColor = ConsoleColor.Magenta; break;
                                default: consoleColor = ConsoleColor.White; break;
                            }

                            WpfConfig.DefaultLogger.Info($"[WebAPI] {message}");
                            Console.ResetColor();

                            SendResponse = new { error = 0, message = "日志已添加", content = message, color };
                        }
                        catch (Exception ex)
                        {
                            SendResponse = new { error = 1, message = "添加日志失败", errorInfo = ex.Message };
                        }
                    }
                    else if (context.Request.Url.AbsolutePath.StartsWith("/Room/AddBlacklist/"))
                    {
                        var userId = context.Request.Url.AbsolutePath.Substring("/Room/AddBlacklist/".Length);
                        if (string.IsNullOrEmpty(userId))
                        {
                            SendResponse = JToken.FromObject(new { error = 1, message = "用户ID不能为空" });
                        }
                        else if (WpfConfig.RoomInfo == null)
                        {
                            SendResponse = JToken.FromObject(new { error = 1, message = "当前未在房间中" });
                        }
                        else if (!WpfConfig.EnableRoomBlacklist)
                        {
                            SendResponse = JToken.FromObject(new { error = 1, message = "房间黑名单功能未开启" });
                        }
                        else
                        {
                            // 将用户添加到黑名单
                            if (WpfConfig.RoomBlacklist == null) WpfConfig.RoomBlacklist = new List<string>();

                            if (!WpfConfig.RoomBlacklist.Contains(userId)) WpfConfig.RoomBlacklist.Add(userId);

                            // 保存黑名单到文件
                            var blacklistFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "RoomConfig");
                            var blacklistFilePath = Path.Combine(blacklistFolderPath, "BlackList.json");

                            Directory.CreateDirectory(blacklistFolderPath);
                            File.WriteAllText(blacklistFilePath, JsonConvert.SerializeObject(WpfConfig.RoomBlacklist));

                            // 如果当前用户是房主，尝试踢出黑名单用户
                            var currentUserId = azf<arg>.Instance.User.Id;
                            if (WpfConfig.RoomInfo.entity.owner_id == currentUserId &&
                                WpfConfig.RoomInfo.entity.fids.Contains(userId))
                                try
                                {
                                    var requestData = JsonConvert.SerializeObject(new
                                    {
                                        room_id = WpfConfig.RoomInfo.entity.entity_id,
                                        user_id = userId
                                    });

                                    var kickResult =
                                        JObject.Parse(X19Http.Post("/online-lobby-member-kick", requestData));
                                    if (kickResult["code"].ToObject<int>() == 0)
                                        WpfConfig.DefaultLogger.Info($"[RoomManage] 已将玩家 {userId} 踢出房间并加入黑名单");
                                }
                                catch (Exception ex)
                                {
                                    WpfConfig.DefaultLogger.Info($"[RoomManage] 踢出玩家失败: {ex.Message}");
                                }

                            SendResponse = JToken.FromObject(new { error = 0, message = "已将用户添加到黑名单" });
                        }
                    }
                    else if (context.Request.Url.AbsolutePath.StartsWith("/Room/RemoveBlacklist/"))
                    {
                        var userId = context.Request.Url.AbsolutePath.Substring("/Room/RemoveBlacklist/".Length);
                        if (string.IsNullOrEmpty(userId))
                        {
                            SendResponse = JToken.FromObject(new { error = 1, message = "用户ID不能为空" });
                        }
                        else if (!WpfConfig.EnableRoomBlacklist)
                        {
                            SendResponse = JToken.FromObject(new { error = 1, message = "房间黑名单功能未开启" });
                        }
                        else if (WpfConfig.RoomBlacklist == null || !WpfConfig.RoomBlacklist.Contains(userId))
                        {
                            SendResponse = JToken.FromObject(new { error = 1, message = "该用户不在黑名单中" });
                        }
                        else
                        {
                            // 从黑名单中移除用户
                            WpfConfig.RoomBlacklist.Remove(userId);

                            // 保存黑名单到文件
                            var blacklistFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "RoomConfig");
                            var blacklistFilePath = Path.Combine(blacklistFolderPath, "BlackList.json");

                            Directory.CreateDirectory(blacklistFolderPath);
                            File.WriteAllText(blacklistFilePath, JsonConvert.SerializeObject(WpfConfig.RoomBlacklist));

                            SendResponse = JToken.FromObject(new { error = 0, message = "已将用户从黑名单中移除" });
                        }
                    }
                    else if (context.Request.Url.AbsolutePath.StartsWith("/Room/Kick/"))
                    {
                        var userId = context.Request.Url.AbsolutePath.Substring("/Room/Kick/".Length);
                        if (string.IsNullOrEmpty(userId))
                        {
                            SendResponse = JToken.FromObject(new { error = 1, message = "用户ID不能为空" });
                        }
                        else if (WpfConfig.RoomInfo == null)
                        {
                            SendResponse = JToken.FromObject(new { error = 1, message = "当前未在房间中" });
                        }
                        else
                        {
                            // 检查当前用户是否是房主
                            var currentUserId = azf<arg>.Instance.User.Id;
                            if (WpfConfig.RoomInfo.entity.owner_id != currentUserId)
                                SendResponse = JToken.FromObject(new { error = 1, message = "只有房主才能踢出玩家" });
                            else if (!WpfConfig.RoomInfo.entity.fids.Contains(userId))
                                SendResponse = JToken.FromObject(new { error = 1, message = "该玩家不在房间中" });
                            else
                                try
                                {
                                    var requestData = JsonConvert.SerializeObject(new
                                    {
                                        room_id = WpfConfig.RoomInfo.entity.entity_id,
                                        user_id = userId
                                    });

                                    var kickResult =
                                        JObject.Parse(X19Http.Post("/online-lobby-member-kick", requestData));
                                    if (kickResult["code"].ToObject<int>() == 0)
                                    {
                                        SendResponse = JToken.FromObject(new { error = 0, message = "已将玩家踢出房间" });
                                        WpfConfig.DefaultLogger.Info($"[RoomManage] 已将玩家 {userId} 踢出房间");
                                    }
                                    else
                                    {
                                        SendResponse = JToken.FromObject(new
                                            { error = 1, message = $"踢出玩家失败: {kickResult["message"]}" });
                                    }
                                }
                                catch (Exception ex)
                                {
                                    SendResponse = JToken.FromObject(new
                                        { error = 1, message = $"踢出玩家失败: {ex.Message}" });
                                }
                        }
                    }
                    else if (context.Request.Url.AbsolutePath.StartsWith("/Room/AddRegexBlacklist/"))
                    {
                        var pattern = WebUtility.UrlDecode(context.Request.Url.AbsolutePath.Substring(24));
                        var addedPatterns = new List<string>();
                        var skippedPatterns = new List<string>();

                        if (WpfConfig.EnableRoomBlacklist)
                        {
                            if (string.IsNullOrEmpty(pattern) || WpfConfig.RegexBlacklist.Contains(pattern))
                            {
                                skippedPatterns.Add(pattern);
                            }
                            else
                            {
                                WpfConfig.RegexBlacklist.Add(pattern);
                                addedPatterns.Add(pattern);
                            }

                            // 刷新正则黑名单文件
                            WpfConfig.WriteRegexBlacklist();
                            if (WpfConfig.RoomInfo != null)
                            {
                                var RoomKickInfo = new JArray();
                                var RoomPlayerIsMatchInfo = new JArray();
                                var Get_RoomPlayerInfo =
                                    X19Http.GetPlayersInfo(WpfConfig.RoomInfo.entity.fids);
                                foreach (var player in Get_RoomPlayerInfo["entities"])
                                {
                                    var playerName = player?["name"]?.ToObject<string>();
                                    var userId = player?["entity_id"]?.ToObject<string>();
                                    var isMatch = Regex.IsMatch(playerName, pattern);
                                    if (isMatch)
                                    {
                                        RoomPlayerIsMatchInfo.Add(JToken.FromObject(new
                                            { UserID = userId, RoomplayerName = playerName, IsMatchRegex = true }));
                                        // 判断userId是否存在黑名单里
                                        if (!WpfConfig.RoomBlacklist.Contains(userId))
                                            // 将玩家UID加入黑名单
                                            WpfConfig.RoomBlacklist.Add(userId);
                                        // 踢出玩家
                                        JObject RemovePlayerReturn;
                                        do
                                        {
                                            RemovePlayerReturn =
                                                JObject.Parse(X19Http.Post("/online-lobby-member-kick",
                                                    JsonConvert.SerializeObject(new
                                                    {
                                                        room_id = WpfConfig.RoomInfo.entity.entity_id, user_id = userId
                                                    })));
                                            if (RemovePlayerReturn["code"].ToObject<int>() == 0)
                                            {
                                                WpfConfig.DefaultLogger.Info("[RoomInfo]玩家 " + playerName +
                                                                             " 在正则黑名单内,已自动踢出房间");
                                                RoomKickInfo.Add(JToken.FromObject(new
                                                {
                                                    RoomplayerName = playerName, PlayerUserID = userId,
                                                    RemovePlayer = RemovePlayerReturn
                                                }));
                                                break; // 成功踢出后退出循环
                                            }

                                            WpfConfig.DefaultLogger.Info(@"[RoomInfo]玩家 " + playerName +
                                                                         " 在正则黑名单内,踢出失败,正在重试...");
                                        } while (true); // 一直重试直到成功
                                    }
                                    else
                                    {
                                        RoomPlayerIsMatchInfo.Add(JToken.FromObject(new
                                            { UserID = userId, RoomPlayerName = playerName, IsMatchRegex = false }));
                                    }
                                }

                                WpfConfig.WriteRoomBlacklist();
                                SendResponse = JToken.FromObject(new
                                {
                                    error = 0, message = "添加成功", addedPatterns, skippedPatterns,
                                    RoomInfo = new
                                        { PlayerIsMatchInfo = RoomPlayerIsMatchInfo, PlayerKickInfo = RoomKickInfo }
                                });
                            }
                            else
                            {
                                SendResponse = JToken.FromObject(new
                                    { error = 0, message = "添加成功", addedPatterns, skippedPatterns });
                            }
                        }
                        else
                        {
                            SendResponse = JToken.FromObject(new { error = 1, message = "正则黑名单功能未开启" });
                        }
                    }
                    else if (context.Request.Url.AbsolutePath.StartsWith("/Room/RemoveRegexBlacklist/"))
                    {
                        var regexPattern = context.Request.Url.AbsolutePath.Substring(27);
                        if (WpfConfig.EnableRoomBlacklist) // 判断正则黑名单功能是否开启
                        {
                            if (WpfConfig.RegexBlacklist.Contains(regexPattern)) // 判断正则是否在黑名单内
                            {
                                WpfConfig.RegexBlacklist.Remove(regexPattern);
                                // 刷新正则黑名单文件
                                var regexBlacklistFolderPath =
                                    Path.Combine(Directory.GetCurrentDirectory(), "RoomConfig");
                                var regexBlacklistFilePath =
                                    Path.Combine(regexBlacklistFolderPath, "RegexBlackList.json");
                                File.WriteAllText(regexBlacklistFilePath,
                                    JsonConvert.SerializeObject(WpfConfig.RegexBlacklist));
                                SendResponse = new { error = 0, message = "删除成功" };
                            }
                            else
                            {
                                SendResponse = new { error = 1, message = "正则不在黑名单内" };
                            }
                        }
                        else
                        {
                            SendResponse = new { error = 1, message = "正则黑名单功能未开启" };
                        }
                    }
                    else if (context.Request.Url.AbsolutePath.StartsWith("/GetItemIDFileUrl/"))
                    {
                        var itemID = context.Request.Url.AbsolutePath.Substring("/GetItemIDFileUrl/".Length);
                        SendResponse = JObject.Parse(
                            X19Http.Post("/user-item-download-v2",
                                JsonConvert.SerializeObject(new
                                {
                                    item_id = itemID, length = 0, offset = 0
                                })));
                    }
                    else if (context.Request.Url.AbsolutePath.StartsWith("/CreateRoom"))
                    {
                        try
                        {
                            // GetParams
                            var rawQuery = context.Request.Url.Query;
                            var queryParams = ParseQueryString(rawQuery);
                            var CreateRoomInfo = new jo();
                            CreateRoomInfo.Password = queryParams["Password"] ?? string.Empty;
                            CreateRoomInfo.MaxPlayer = uint.TryParse(queryParams["MaxPlayer"], out var maxPlayer)
                                ? maxPlayer
                                : 10;
                            CreateRoomInfo.WorldName = queryParams["WorldName"] ?? azf<arg>.Instance.User.Id;
                            var VisibilityStatus = queryParams["Visibility"];
                            if (string.IsNullOrEmpty(VisibilityStatus)) VisibilityStatus = "OPEN";
                            if (VisibilityStatus == "OPEN")
                                CreateRoomInfo.VisibleScope = RoomVisibleStatus.OPEN;
                            else if (VisibilityStatus == "FRIEND")
                                CreateRoomInfo.VisibleScope = RoomVisibleStatus.FRIEND;
                            else if (VisibilityStatus == "HIDDEN")
                                CreateRoomInfo.VisibleScope = RoomVisibleStatus.HIDDEN;
                            else
                                CreateRoomInfo.VisibleScope = RoomVisibleStatus.OPEN;
                            //CreateRoomInfo.VisibleScope = queryParams["VisibleScope"];
                            var MapInfo = new OnlineMapM();
                            MapInfo.ID = queryParams["ResId"] ?? "";
                            if (MapInfo.ID == string.Empty)
                            {
                                SendResponse = new { error = 1, message = "查询字符串必须的参数: ResId" };
                                break;
                            }

                            CreateRoomInfo.SelectedOnlineMap = MapInfo;

                            // 使用反射访问内部类 aqf 和 kc
                            MethodInfo methodA;
                            try
                            {
                                // 获取程序集中的类型
                                var assembly = Assembly.GetAssembly(typeof(jo)) ?? Assembly.GetExecutingAssembly();

                                // 查找 aqf 类型
                                var aqfType = assembly.GetType("WPFLauncher.Manager.aqf") ??
                                              assembly.GetTypes().FirstOrDefault(t => t.Name == "aqf");

                                if (aqfType != null)
                                {
                                    // 查找 kc 类型
                                    var kcType = assembly.GetType("WPFLauncher.ViewModel.Launcher.kc") ??
                                                 assembly.GetTypes().FirstOrDefault(t => t.Name == "kc");

                                    if (kcType != null)
                                    {
                                        // 获取 Singleton<aqf>.Instance 属性
                                        var singletonType = typeof(Singleton<>).MakeGenericType(aqfType);
                                        var instanceProperty = singletonType.GetProperty("Instance");

                                        if (instanceProperty != null)
                                        {
                                            // 获取 Instance 对象
                                            var instance = instanceProperty.GetValue(null);

                                            if (instance != null)
                                            {
                                                // 获取 f 字段
                                                var fField = aqfType.GetField("f",
                                                    BindingFlags.Public | BindingFlags.NonPublic |
                                                    BindingFlags.Instance);

                                                if (fField != null)
                                                {
                                                    // 获取 f 的值
                                                    var fValue = fField.GetValue(instance);

                                                    // 如果 f 为 null，创建一个新的 kc 实例并设置
                                                    if (fValue == null)
                                                    {
                                                        fValue = Activator.CreateInstance(kcType);
                                                        fField.SetValue(instance, fValue);
                                                    }

                                                    // 查找 LaunchGamePage 枚举类型
                                                    var launchGamePageType =
                                                        assembly.GetType(
                                                            "WPFLauncher.ViewModel.Launcher.LaunchGamePage") ??
                                                        assembly.GetTypes()
                                                            .FirstOrDefault(t => t.Name == "LaunchGamePage");

                                                    if (launchGamePageType != null && launchGamePageType.IsEnum)
                                                    {
                                                        // 获取 kc 类的 a 方法
                                                        methodA = kcType.GetMethod("a",
                                                            BindingFlags.Public | BindingFlags.NonPublic |
                                                            BindingFlags.Instance,
                                                            null,
                                                            new[] { launchGamePageType, typeof(object), typeof(int) },
                                                            null);

                                                        if (methodA != null)
                                                        {
                                                            // 获取枚举值 15 (根据IL代码)
                                                            var pageEnum = Enum.ToObject(launchGamePageType, 15);

                                                            // 调用 a 方法，传入参数 (15, null, -1)
                                                            methodA.Invoke(fValue, new[] { pageEnum, null, -1 });
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                WpfConfig.DefaultLogger.Error($"反射访问内部类时出错: {ex.Message}");
                                WpfConfig.DefaultLogger.Error($"堆栈跟踪: {ex.StackTrace}");
                            }

                            // 获取 jo 类的类型信息
                            var joType = typeof(jo);

                            // 明确指定要调用的方法：a(object obj)
                            methodA = joType.GetMethod(
                                "a",
                                BindingFlags.NonPublic | BindingFlags.Instance,
                                null,
                                new[] { typeof(object) }, // 指定参数类型为 object
                                null);

                            if (methodA != null)
                                // 调用私有方法 a
                                methodA.Invoke(CreateRoomInfo, new object[] { null });
                            else
                                WpfConfig.DefaultLogger.Error("方法 a 未找到，请确认方法签名是否正确。");
                            SendResponse = new { error = 0, message = "创建房间成功" };
                        }
                        catch (Exception ex)
                        {
                            SendResponse = new { error = 1, message = $"创建房间出错: {ex.Message}" };
                        }
                    }
                    else if (context.Request.Url.AbsolutePath.StartsWith("/JoinRoom"))
                    {
                        try
                        {
                            var queryParams = ParseQueryString(context.Request.Url.Query);
                            var roomID = queryParams["roomId"];
                            var startGame = queryParams["startGame"] ?? "false";
                            var bStartGame = false;
                            if (startGame.ToLower() == "true") bStartGame = true;
                            if (string.IsNullOrEmpty(roomID))
                            {
                                SendResponse = new { error = 1, message = "查询字符串必须的参数: roomId" };
                            }
                            else
                            {
                                // 👇 关键：通过 Dispatcher 调用到 UI 线程
                                var success = false;
                                Exception dispatchException = null;

                                // 确保 Application.Current 存在（即 WPF 应用已启动）
                                if (Application.Current == null)
                                {
                                    SendResponse = new { error = 1, message = "WPF 应用未初始化，无法加入房间" };
                                    return;
                                }

                                // 同步调用到 UI 线程
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    try
                                    {
                                        // 获取 aul 单例（在 UI 线程中安全）
                                        var aulInstance = Singleton<aum>.Instance;
                                        if (aulInstance == null)
                                        {
                                            dispatchException =
                                                new InvalidOperationException("WPFLauncher.Manager.Game.aum 单例未初始化");
                                            return;
                                        }

                                        // 调用 g 方法（现在在 UI 线程，可以安全创建 cm 窗口）
                                        var gMethod = typeof(aum).GetMethod("g",
                                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                                            null, new[] { typeof(string), typeof(bool) }, null);

                                        if (gMethod == null)
                                        {
                                            dispatchException =
                                                new MissingMethodException("未找到 WPFLauncher.Manager.Game.aum.g 方法");
                                            return;
                                        }

                                        var result = gMethod.Invoke(aulInstance, new object[] { roomID, bStartGame });
                                        success = result is not bool b || b; // 如果返回 bool 且为 true，或非 bool 视为成功
                                    }
                                    catch (Exception ex)
                                    {
                                        dispatchException = ex;
                                    }
                                });

                                if (dispatchException != null)
                                {
                                    WpfConfig.DefaultLogger.Error($"UI 线程调用失败: {dispatchException}");
                                    SendResponse = new { error = 1, message = $"加入失败: {dispatchException.Message}" };
                                }
                                else
                                {
                                    SendResponse = new { error = 0, message = "加入房间请求已发送" };
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            WpfConfig.DefaultLogger.Info(e);
                            SendResponse = new { error = 1, message = $"加入房间异常: {e.Message}" };
                        }
                    }
                    else if (context.Request.Url.AbsolutePath.StartsWith("/LeftRoom"))
                    {
                        var enableAlwaysSaveWorld = false;
                        if (WpfConfig.AlwaysSaveWorld)
                        {
                            WpfConfig.AlwaysSaveWorld = false;
                            enableAlwaysSaveWorld = true;
                        }

                        var lobbyGameRoomManagerView = azf<apn>.Instance.k<LobbyGameRoomManagerView>();
                        if (lobbyGameRoomManagerView != null)
                        {
                            // 使用Dispatcher确保在UI线程上访问DataContext
                            object roomManageMainWindow = null;
                            Type dataContextType = null;
                            var invokeSuccess = false;

                            // 在UI线程上获取DataContext
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                if (lobbyGameRoomManagerView.DataContext != null)
                                {
                                    roomManageMainWindow = lobbyGameRoomManagerView.DataContext;
                                    dataContextType = roomManageMainWindow.GetType();
                                    invokeSuccess = true;
                                }
                            });

                            // 如果成功获取到DataContext，则继续执行
                            if (invokeSuccess && roomManageMainWindow != null && dataContextType != null)
                            {
                                // 获取基类类型
                                var baseType = dataContextType.BaseType;

                                // 使用Dispatcher在UI线程上调用方法
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    // 查找 View 属性
                                    var viewProperty = baseType.GetProperty("View",
                                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                                    if (viewProperty != null)
                                    {
                                        // 获取 View 对象
                                        var viewObject = viewProperty.GetValue(roomManageMainWindow);
                                        if (viewObject != null)
                                        {
                                            // 查找 c 方法
                                            var cMethod = viewObject.GetType().GetMethod("c",
                                                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                                            if (cMethod != null)
                                                // 调用 c() 方法
                                                cMethod.Invoke(viewObject, new object[] { });
                                        }
                                    }
                                });
                            }
                        }

                        if (enableAlwaysSaveWorld) WpfConfig.AlwaysSaveWorld = true;
                        SendResponse = new { code = 0, message = "成功退出房间", details = "" };
                    }
                    else if (context.Request.Url.AbsolutePath.StartsWith("/settings"))
                    {
                        var htmlContent = HtmlResource.GetHotUpdateHtml();
                        context.Response.ContentType = "text/html; charset=utf-8";
                        var buffer = Encoding.UTF8.GetBytes(htmlContent);
                        context.Response.ContentLength64 = buffer.Length;
                        context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                        context.Response.OutputStream.Close();
                        IsSendResponseFlag = false;
                    }
                    else if (context.Request.Url.AbsolutePath.StartsWith("/config/get"))
                    {
                        SendResponse = new
                        {
                            error = 0,
                            message = "获取成功",
                            data = ConfigManager.GetCurrentConfigValues() // 自动获取所有 WpfConfig 里的值
                        };
                    }
                    else if (context.Request.Url.AbsolutePath.StartsWith("/config/settingslist"))
                    {
                        SendResponse = new
                        {
                            error = 0,
                            message = "获取元数据成功",
                            data = ConfigManager.GetMetadata() // 自动获取 key, 中文名和类型
                        };
                    }
                    else if (context.Request.Url.AbsolutePath.StartsWith("/config/save")) // 新增保存接口
                    {
                        using (var reader =
                               new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
                        {
                            var json = reader.ReadToEnd();
                            ConfigManager.UpdateFromJson(json); // 自动更新 WpfConfig 并保存文件
                        }

                        SendResponse = new { error = 0, message = "配置已更新" };
                    }
                    else if (context.Request.Url.AbsolutePath == "/")
                    {
                        // 返回API索引信息
                        dynamic apiIndex = new ExpandoObject();
                        apiIndex.message = "正常返回";
                        apiIndex.status = 200;
                        apiIndex.details = "hello world";
                        apiIndex.endpoints = new[]
                        {
                            "/get_roominfo",
                            "/roommanage",
                            "/help",
                            "/Room/AddBlacklist/{userId}",
                            "/Room/RemoveBlacklist/{userId}",
                            "/Room/AddRegexBlacklist/{pattern}",
                            "/Room/RemoveRegexBlacklist/{pattern}",
                            "/Room/Kick/{userId}"
                        };

                        SendResponse = apiIndex;
                    }
                    else
                    {
                        SendResponse = new { error = 0, message = "请求路径不存在" };
                    }
                }
                catch (Exception e)
                {
                    SendResponse = new { Error_Content = e, STACKTRACE = e.StackTrace };
                }

                break;
        }

        if (IsSendResponseFlag) sendJsonResponse(context.Response, SendResponse);
    }

    private void HandlePostRequest(HttpListenerContext context)
    {
        try
        {
            // 读取 POST 请求的内容
            using (var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8))
            {
                var requestBody = reader.ReadToEnd();
                // 解析查询字符串
                var queryParams = context.Request.QueryString;
                if (queryParams.Count > 0)
                {
                    // 将查询字符串参数添加到请求体中
                    var queryParamsDict = new Dictionary<string, string>();
                    foreach (var key in queryParams.AllKeys) queryParamsDict[key] = queryParams[key];
                    // 将查询参数序列化为 JSON 字符串并合并到请求体
                    var queryParamsJson = JsonConvert.SerializeObject(queryParamsDict);
                    requestBody = $"{requestBody.TrimEnd('}')}, {queryParamsJson.TrimStart('{')}";
                }

                //WpfConfig.DefaultLogger.Info($"收到 POST 请求,内容:{requestBody}");
                // 尝试将接收到的 JSON 数据解析成对象
                dynamic requestObject = null;
                try
                {
                    dynamic SendResponse = new ExpandoObject();
                    try
                    {
                        requestObject = JsonConvert.DeserializeObject<dynamic>(requestBody);
                    }
                    catch (Exception e)
                    {
                    }

                    switch (context.Request.Url.AbsolutePath)
                    {
                        // case "/post":
                        //     HttpClient http = new HttpClient();
                        //     string userToken = WPFLauncher.Util.ss.e((string)requestObject.url, (string)requestObject.body);
                        //     http.DefaultRequestHeaders.Clear();
                        //     http.DefaultRequestHeaders.Add("user-id", WPFLauncher.Common.azf<arg>.Instance.User.Id);
                        //     http.DefaultRequestHeaders.Add("user-token", userToken);
                        //     var content = new StringContent((string)requestObject.body, Encoding.UTF8, "application/json");
                        //     HttpResponseMessage responseData = http.PostAsync(WPFLauncher.Common.azf<WPFLauncher.Manager.Configuration.axi>.Instance.Url.ApiGatewayUrl + (string)requestObject.url, content).Result;
                        //     string get_result = responseData.Content.ReadAsStringAsync().Result;
                        //     SendResponse.user_id = WPFLauncher.Common.azf<arg>.Instance.User.Id;
                        //     SendResponse.user_token = userToken;
                        //     SendResponse.response = get_result;
                        //     WpfConfig.DefaultLogger.Info("[HTTP][POST]请求返回内容:" + get_result);
                        //     break;
                        // case "/get":
                        //     HttpClient http_Get = new HttpClient();
                        //     string userToken_Get = WPFLauncher.Util.ss.e((string)requestObject.url, (string)requestObject.body);
                        //     http_Get.DefaultRequestHeaders.Clear();
                        //     http_Get.DefaultRequestHeaders.Add("user-id", WPFLauncher.Common.azf<arg>.Instance.User.Id);
                        //     http_Get.DefaultRequestHeaders.Add("user-token", userToken_Get);
                        //     var content_Get = new StringContent(requestObject.body, Encoding.UTF8, "application/json");
                        //     HttpResponseMessage responseData_Get = http_Get.PostAsync(WPFLauncher.Common.azf<WPFLauncher.Manager.Configuration.axi>.Instance.Url.ApiGatewayUrl + (string)requestObject.url, content_Get).Result;
                        //     string get_result_Get = responseData_Get.Content.ReadAsStringAsync().Result;
                        //     SendResponse.user_id = WPFLauncher.Common.azf<arg>.Instance.User.Id;
                        //     SendResponse.user_token = userToken_Get;
                        //     SendResponse.response = get_result_Get;
                        //     WpfConfig.DefaultLogger.Info("[HTTP][POST]请求返回内容:" + get_result_Get);
                        //     break;
                        case "/Send_ChatMessage":
                            try
                            {
                                string message = requestObject["message"].ToString();
                                uint ChatUserID = uint.Parse(requestObject["userID"].ToString());
                                if (string.IsNullOrEmpty(message))
                                {
                                    SendResponse = new { error = 1, message = "消息内容不能为空" };
                                }
                                else
                                {
                                    new acq().e(ChatUserID, message);
                                    var startTime = DateTime.Now;
                                    while (true)
                                    {
                                        if (!string.IsNullOrEmpty(WpfConfig.Get_Recv_String_ChatResult))
                                        {
                                            var Get_Recv_String_ChatResult_ToJson =
                                                JObject.Parse(WpfConfig.Get_Recv_String_ChatResult);
                                            if (!((IDictionary<string, JToken>)Get_Recv_String_ChatResult_ToJson)
                                                    .ContainsKey("Get_Recv_String_ChatResult") &&
                                                Get_Recv_String_ChatResult_ToJson["err"].ToObject<int>() != 0)
                                            {
                                                SendResponse = new
                                                {
                                                    error = 1, message = "发送失败",
                                                    errorInfo = Get_Recv_String_ChatResult_ToJson
                                                };
                                                WpfConfig.Get_Recv_String_ChatResult = string.Empty;
                                            }
                                            else
                                            {
                                                SendResponse = new
                                                {
                                                    error = 0, message = "发送成功",
                                                    SendResult = Get_Recv_String_ChatResult_ToJson,
                                                    SendMessage = message, ToUserID = ChatUserID
                                                };
                                                WpfConfig.Get_Recv_String_ChatResult = string.Empty;
                                            }

                                            break;
                                        }

                                        if ((DateTime.Now - startTime).TotalSeconds > 3)
                                        {
                                            SendResponse = new { error = 1, message = "发送超时" };
                                            WpfConfig.Get_Recv_String_ChatResult = string.Empty;
                                            break;
                                        }
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                SendResponse = new { error = 1, message = "发送失败", errorInfo = e };
                            }

                            break;
                        case "/JSONEscape":
                            SendResponse.ToJsonEscape = requestBody.Replace("\"", "\\\"");
                            break;
                        case "/userToInfo":
                            SendResponse = X19Http.GetPlayerInfo(requestObject["userid"].ToString());
                            break;
                        case "/usersToInfo":
                            SendResponse =
                                X19Http.GetPlayersInfo(requestObject["entity_ids"].ToObject<List<string>>());
                            break;
                        case "/DecryptX19sign":
                            SendResponse.ToDecryptX19sign = requestBody.b();
                            break;
                        case "/EncryptX19sign":
                            SendResponse.ToEncryptX19sign = requestBody.a();
                            break;
                        case "/Room/AddRegexBlacklist":
                            if (WpfConfig.EnableRoomBlacklist)
                            {
                                string pattern = requestObject["pattern"].ToString();
                                if (string.IsNullOrEmpty(pattern))
                                {
                                    SendResponse = new { error = 1, message = "正则表达式不能为空" };
                                }
                                else if (WpfConfig.RegexBlacklist.Contains(pattern))
                                {
                                    SendResponse = new { error = 1, message = "该正则表达式已存在于黑名单中" };
                                }
                                else
                                {
                                    WpfConfig.RegexBlacklist.Add(pattern);
                                    WpfConfig.WriteRegexBlacklist();

                                    // 如果在房间中，检查玩家是否符合黑名单规则
                                    if (WpfConfig.RoomInfo != null)
                                    {
                                        var RoomKickInfo = new JArray();
                                        var RoomPlayerIsMatchInfo = new JArray();
                                        var Get_RoomPlayerInfo =
                                            X19Http.GetPlayersInfo(WpfConfig.RoomInfo.entity.fids);
                                        foreach (var player in Get_RoomPlayerInfo["entities"])
                                        {
                                            var playerName = player?["name"]?.ToObject<string>();
                                            var userId = player?["entity_id"]?.ToObject<string>();
                                            var isMatch = Regex.IsMatch(playerName, pattern);
                                            if (isMatch)
                                            {
                                                RoomPlayerIsMatchInfo.Add(JToken.FromObject(new
                                                {
                                                    UserID = userId, RoomplayerName = playerName, IsMatchRegex = true
                                                }));
                                                // 判断userId是否存在黑名单里
                                                if (!WpfConfig.RoomBlacklist.Contains(userId))
                                                    // 将玩家UID加入黑名单
                                                    WpfConfig.RoomBlacklist.Add(userId);
                                                // 踢出玩家
                                                JObject RemovePlayerReturn;
                                                do
                                                {
                                                    RemovePlayerReturn =
                                                        JObject.Parse(X19Http.Post("/online-lobby-member-kick",
                                                            JsonConvert.SerializeObject(new
                                                            {
                                                                room_id = WpfConfig.RoomInfo.entity.entity_id,
                                                                user_id = userId
                                                            })));
                                                    if (RemovePlayerReturn["code"].ToObject<int>() == 0)
                                                    {
                                                        WpfConfig.DefaultLogger.Info("[RoomInfo]玩家 " + playerName +
                                                            " 在正则黑名单内,已自动踢出房间");
                                                        RoomKickInfo.Add(JToken.FromObject(new
                                                        {
                                                            RoomplayerName = playerName, PlayerUserID = userId,
                                                            RemovePlayer = RemovePlayerReturn
                                                        }));
                                                        break; // 成功踢出后退出循环
                                                    }

                                                    WpfConfig.DefaultLogger.Info(@"[RoomInfo]玩家 " + playerName +
                                                        " 在正则黑名单内,踢出失败,正在重试...");
                                                } while (true); // 一直重试直到成功
                                            }
                                            else
                                            {
                                                RoomPlayerIsMatchInfo.Add(JToken.FromObject(new
                                                {
                                                    UserID = userId, RoomPlayerName = playerName, IsMatchRegex = false
                                                }));
                                            }
                                        }

                                        WpfConfig.WriteRoomBlacklist();
                                        SendResponse = new
                                        {
                                            error = 0, message = "添加成功",
                                            RoomInfo = new
                                            {
                                                PlayerIsMatchInfo = RoomPlayerIsMatchInfo, PlayerKickInfo = RoomKickInfo
                                            }
                                        };
                                    }
                                    else
                                    {
                                        SendResponse = new { error = 0, message = "添加成功" };
                                    }
                                }
                            }
                            else
                            {
                                SendResponse = new { error = 1, message = "正则黑名单功能未开启" };
                            }

                            break;
                        case "/Room/RemoveRegexBlacklist":
                            if (WpfConfig.EnableRoomBlacklist)
                            {
                                string pattern = requestObject["pattern"].ToString();
                                if (string.IsNullOrEmpty(pattern))
                                {
                                    SendResponse = new { error = 1, message = "正则表达式不能为空" };
                                }
                                else if (!WpfConfig.RegexBlacklist.Contains(pattern))
                                {
                                    SendResponse = new { error = 1, message = "该正则表达式不在黑名单中" };
                                }
                                else
                                {
                                    WpfConfig.RegexBlacklist.Remove(pattern);
                                    WpfConfig.WriteRegexBlacklist();
                                    SendResponse = new { error = 0, message = "已从正则黑名单移除" };
                                }
                            }
                            else
                            {
                                SendResponse = new { error = 1, message = "正则黑名单功能未开启" };
                            }

                            break;
                    }

                    sendJsonResponse(context.Response, SendResponse);
                }
                catch (JsonException)
                {
                    // 如果解析失败，返回错误消息
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    var errorResponse = new
                    {
                        message = "Invalid JSON data"
                    };
                    sendJsonResponse(context.Response, errorResponse);
                }
            }
        }
        catch (Exception ex)
        {
            WpfConfig.DefaultLogger.Info($"处理 POST 请求时发生错误: {ex.Message}");
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            var errorResponse = new
            {
                message = "服务器内部错误"
            };
            sendJsonResponse(context.Response, errorResponse);
        }
    }

    private void HandleOptionsRequest(HttpListenerContext context)
    {
        // 处理CORS
        // var request = context.Request;
        var response = context.Response;

        // // 设置通用 CORS 头（允许所有来源、方法、头部）
        // response.Headers["Access-Control-Allow-Origin"] = "*";
        // response.Headers["Access-Control-Allow-Methods"] = "GET, POST, PUT, DELETE, OPTIONS";
        // response.Headers["Access-Control-Allow-Headers"] = "*"; // 或指定具体头，如 "Content-Type, Authorization"

        // // 处理预检请求（OPTIONS）
        response.StatusCode = (int)HttpStatusCode.OK;
        response.Close();
    }

    #endregion

    #region ProcessWebSocketRequest

    public static List<WebSocket> _webSockets = new();

    // 处理WebSocket连接
    private async Task HandleWebSocketRequest(HttpListenerContext context)
    {
        try
        {
            // 接受WebSocket连接
            var webSocketContext = await context.AcceptWebSocketAsync(null);
            var webSocket = webSocketContext.WebSocket;

            // 添加到WebSocket连接列表
            lock (_webSockets)
            {
                _webSockets.Add(webSocket);
            }

            WpfConfig.DefaultLogger.Info($"[WebSocket] 新连接已建立，当前连接数: {_webSockets.Count}");

            // 处理WebSocket消息
            await ProcessWebSocketMessages(webSocket, webSocketContext);
        }
        catch (Exception ex)
        {
            WpfConfig.DefaultLogger.Error($"[WebSocket] 处理WebSocket连接时出错: {ex.Message}");
        }
    }

    // 处理WebSocket消息
    private async Task ProcessWebSocketMessages(WebSocket webSocket, HttpListenerWebSocketContext context)
    {
        var buffer = new byte[1024 * 4]; // 4KB缓冲区

        try
        {
            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection closed",
                        CancellationToken.None);
                    break;
                }

                // 处理接收到的消息
                var receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
                WpfConfig.DefaultLogger.Info($"[WebSocket] 收到消息: {receivedMessage}");

                // 处理消息并发送响应
                // string responseMessage = ProcessWebSocketMessage(receivedMessage, context);
                //
                // byte[] responseBuffer = Encoding.UTF8.GetBytes(responseMessage);
                // await webSocket.SendAsync(new ArraySegment<byte>(responseBuffer), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            WpfConfig.DefaultLogger.Error($"[WebSocket] 处理消息时出错: {ex.Message}");
        }
        finally
        {
            // 从连接列表中移除
            lock (_webSockets)
            {
                _webSockets.Remove(webSocket);
            }

            if (webSocket.State != WebSocketState.Closed && webSocket.State != WebSocketState.Aborted)
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection closed",
                    CancellationToken.None);

            webSocket.Dispose();
            WpfConfig.DefaultLogger.Info($"[WebSocket] 连接已关闭，当前连接数: {_webSockets.Count}");
        }
    }

    // 处理WebSocket消息的业务逻辑
    // private string ProcessWebSocketMessage(string message, HttpListenerWebSocketContext context)
    // {
    //     
    // }

    // 向所有WebSocket客户端广播消息
    public static async Task BroadcastWebSocketMessage(string message)
    {
        List<WebSocket> socketsToBroadcast;
        lock (_webSockets)
        {
            socketsToBroadcast = new List<WebSocket>(_webSockets);
        }

        var buffer = Encoding.UTF8.GetBytes(message);

        foreach (var webSocket in socketsToBroadcast)
            if (webSocket.State == WebSocketState.Open)
                try
                {
                    await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true,
                        CancellationToken.None);
                }
                catch (Exception ex)
                {
                    WpfConfig.DefaultLogger.Error($"[WebSocket] 广播消息时出错: {ex.Message}");
                }
    }

    #endregion
}