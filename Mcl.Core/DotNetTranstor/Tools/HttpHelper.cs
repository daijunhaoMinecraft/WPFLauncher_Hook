using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using DotNetTranstor.Hookevent;
using Microsoft.VisualBasic.ApplicationServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WPFLauncher.Code;
using WPFLauncher.Common;
using WPFLauncher.Manager;
using WPFLauncher.Manager.Configuration;
using WPFLauncher.Manager.Configuration.CppConfigure;
using WPFLauncher.Manager.Login;
using WPFLauncher.Model;
using WPFLauncher.Model.Game.CppGame;
using WPFLauncher.Network.Message;
using WPFLauncher.Network.Protocol.LobbyGame;
using WPFLauncher.Network.Service;
using WPFLauncher.Util;
using WPFLauncher.View.Chat;
using WPFLauncher.ViewModel.Chat;
using Exception = System.Exception;
using System.Web;
using Login.NetEase;
using Mcl.Core.Network.Interface;
using WPFLauncher.Network.Protocol;
using WPFLauncher.Network.Protocol.Cpp;
using System.Collections.Specialized;
using System.Runtime.InteropServices;
using Mcl.Core.Utils;
using WPFLauncher.Model.Component;
using WPFLauncher.Network.TransService;
using WPFLauncher.ViewModel.Launcher;
using WPFLauncher.ViewModel.LobbyGame;
using WPFControls.Helpers;
using WPFLauncher.Manager.Game;
using WPFLauncher.Model.Game;
using WPFLauncher.SQLite;
using MicrosoftTranslator.DotNetTranstor.Tools;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using Mcl.Core.DotNetTranstor.Tools;
using WPFLauncher.View.Launcher.LobbyGame;

namespace DotNetTranstor.Tools
{
public class SimpleHttpServer
{
    private HttpListener _httpListener;

    #region Tools
    
    /// <summary>
    /// 字符串查询解析工具
    /// </summary>
    /// <param name="query">URL网址</param>
    /// <returns>解析后的数据</returns>
    private NameValueCollection ParseQueryString(string query)
    {
        // 替代 HttpUtility.ParseQueryString 的方法
        var nvc = new NameValueCollection();
        if (string.IsNullOrEmpty(query)) return nvc;
    
        foreach (string pair in query.TrimStart('?').Split('&'))
        {
            string[] keyValue = pair.Split('=');
            if (keyValue.Length != 2) continue;
        
            string key = Uri.UnescapeDataString(keyValue[0]);
            string value = Uri.UnescapeDataString(keyValue[1]);
            nvc.Add(key, value);
        }
        return nvc;
    }

    /// <summary>
    /// 数据合并工具
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
            foreach (var kvp in dataDict)
            {
                responseDict[kvp.Key] = kvp.Value;
            }
            return responseDict;
        }
        return null;
    }

    #endregion
    
    #region Send Response

    /// <summary>
    /// 发送Json响应到客户端
    /// </summary>
    /// <param name="response">响应</param>
    /// <param name="responseObject">响应的数据类型</param>
    private void sendJsonResponse(HttpListenerResponse response, object responseObject)
    {
        string jsonResponse = JsonConvert.SerializeObject(responseObject, Formatting.Indented);
        byte[] responseBytes = Encoding.UTF8.GetBytes(jsonResponse);
        customContentTypeResponse(response, "application/json", responseBytes);
    }
    /// <summary>
    /// 发送Html数据到客户端
    /// </summary>
    /// <param name="response">响应</param>
    /// <param name="htmlContent">html文本</param>
    private void sendHtmlResponse(HttpListenerResponse response, string htmlContent)
    {
        byte[] responseBytes = Encoding.UTF8.GetBytes(htmlContent);
        customContentTypeResponse(response, "text/html", responseBytes);
    }
    /// <summary>
    /// 发送自定义数据到客户端
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
    
    #region Response Handler
    /// <summary>
    /// 处理返回响应
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
        dynamic applyResponse = popObject(serverResponse, data);
        sendJsonResponse(context.Response, applyResponse);
    }

    #endregion
    
    #region Handle Request

    public static List<string> apiRequestList = new List<string>() { "/api/blacklists/regex", "/api/blacklists/users" };
    
    private void handleWebApiRequest(HttpListenerContext context)
    {
        if (context.Request.Url.AbsolutePath == apiRequestList[0])
        {
            // Generation Result
            dynamic serverResponse = new ExpandoObject();
            serverResponse.error = 0;
            serverResponse.isEnabled = true;
            serverResponse.patterns = (Path_Bool.RegexBlacklist ?? new List<string>()).Select(pattern => new {
                pattern = pattern,
                isValid = true // 假设所有模式都是有效的
            }).ToList<object>();
            serverResponse.count = serverResponse.patterns.Count;
            handleResponse(context, "获取正则表达式列表成功", 0, serverResponse);
        }
        else if (context.Request.Url.AbsolutePath==apiRequestList[1])
        {
            // 新的API端点，返回规范化的用户黑名单列表，并包含用户详情
            if (Path_Bool.EnableRoomBlacklist)
            {
                var blacklistUsers = new List<object>();
                if (Path_Bool.RoomBlacklist != null && Path_Bool.RoomBlacklist.Count > 0)
                {
                    try
                    {
                        JObject userDetails = X19Http.Get_Players_Info(Path_Bool.RoomBlacklist);
                        if (userDetails != null && userDetails["entities"] != null)
                        {
                            foreach (var user in userDetails["entities"])
                            {
                                blacklistUsers.Add(new {
                                    userId = user["entity_id"]?.ToString(),
                                    userName = user["name"]?.ToString() ?? "未知用户",
                                    avatarUrl = user["avatar_image_url"]?.ToString() ?? "https://x19.fp.ps.netease.com/file/5a34e0777f9d2a8a4ea3d36eza31LhW3",
                                    signature = user["signature"]?.ToString() ?? ""
                                });
                            }
                        }
                        
                        // 处理可能缺失的用户
                        var foundUserIds = new HashSet<string>();
                        foreach (var user in blacklistUsers)
                        {
                            foundUserIds.Add(((dynamic)user).userId.ToString());
                        }
                        
                        foreach (var userId in Path_Bool.RoomBlacklist)
                        {
                            if (!foundUserIds.Contains(userId))
                            {
                                blacklistUsers.Add(new {
                                    userId = userId,
                                    userName = $"用户{userId}",
                                    avatarUrl = "https://x19.fp.ps.netease.com/file/5a34e0777f9d2a8a4ea3d36eza31LhW3",
                                    signature = ""
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // 如果获取用户详情失败，至少返回基本ID信息
                        foreach (var userId in Path_Bool.RoomBlacklist)
                        {
                            blacklistUsers.Add(new {
                                userId = userId,
                                userName = $"用户{userId}",
                                avatarUrl = "https://x19.fp.ps.netease.com/file/5a34e0777f9d2a8a4ea3d36eza31LhW3",
                                signature = ""
                            });
                        }
                        DebugPrint.LogDebug_NoColorSelect($"[HTTP] 获取黑名单用户详情失败: {ex.Message}");
                    }
                }

                dynamic SendResponse = new ExpandoObject();
                SendResponse.error = 0;
                SendResponse.isEnabled = true;
                SendResponse.users = blacklistUsers.ToList<object>();
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
        using (StreamReader reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
        {
            string requestBody = reader.ReadToEnd();
            dynamic SendResponse = new ExpandoObject();
            dynamic requestObject = null;
            requestObject = JsonConvert.DeserializeObject<dynamic>(requestBody);
            // 获取"/api/"后面的内容
            string contentAfterApiPost = "/"+context.Request.Url.AbsolutePath.Substring(5);
            DebugPrint.LogDebug_NoColorSelect($"[POST]args:{contentAfterApiPost}");
            if (context.Request.HttpMethod == "POST")
            {
                // 使用 GBK 编码（适用于简体中文环境）
                Encoding gbkEncoding = Encoding.GetEncoding("gbk");
                byte[] gbkBytes = gbkEncoding.GetBytes(requestBody);
                string decodedFromGBK = Encoding.UTF8.GetString(gbkBytes);
                HttpClient http = new HttpClient();
                string userToken = WPFLauncher.Util.ss.e((string)contentAfterApiPost, decodedFromGBK);
                http.DefaultRequestHeaders.Clear();
                http.DefaultRequestHeaders.Add("user-id", aze<arg>.Instance.User.Id);
                http.DefaultRequestHeaders.Add("user-token", userToken);
                var content = new StringContent((string)decodedFromGBK, Encoding.UTF8, "application/json");
                HttpResponseMessage responseData = http.PostAsync(aze<axh>.Instance.Url.ApiGatewayUrl + contentAfterApiPost, content).Result;
                string get_result = responseData.Content.ReadAsStringAsync().Result;
                try
                {
                    SendResponse = JsonConvert.DeserializeObject<JObject>(get_result);
                }
                catch (JsonReaderException)
                {
                    SendResponse = get_result;
                }

                if (Path_Bool.IsDebug)
                {
                    DebugPrint.LogDebug_NoColorSelect("[HTTP][POST]请求返回内容:" + get_result);
                }
            }
            else if (context.Request.HttpMethod == "GET")
            {
                bool IsPostFlag = false;
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

                    
                    HttpClient httpClient = new HttpClient();
                    string userTokenValue = WPFLauncher.Util.ss.e((string)contentAfterApiPost, requestBody);
                    httpClient.DefaultRequestHeaders.Clear();
                    httpClient.DefaultRequestHeaders.Add("user-id", aze<arg>.Instance.User.Id);
                    httpClient.DefaultRequestHeaders.Add("user-token", userTokenValue);
                    var stringContent = new StringContent(requestBody, Encoding.UTF8, "application/json");
                    HttpResponseMessage httpResponseData = httpClient.PostAsync(aze<axh>.Instance.Url.ApiGatewayUrl + contentAfterApiPost, stringContent).Result;
                    string resultContent = httpResponseData.Content.ReadAsStringAsync().Result;
                    try
                    {
                        SendResponse = JsonConvert.DeserializeObject<JObject>(resultContent);
                    }
                    catch (JsonReaderException)
                    {
                        SendResponse = resultContent;
                    }

                    if (Path_Bool.IsDebug)
                    {
                        DebugPrint.LogDebug_NoColorSelect("[HTTP][POST]请求返回内容:" + resultContent);
                    }
                    IsPostFlag = true;
                }

                if (!IsPostFlag)
                {
                    HttpClient http = new HttpClient();
                    string userToken = WPFLauncher.Util.ss.e((string)contentAfterApiPost, "");
                    http.DefaultRequestHeaders.Clear();
                    http.DefaultRequestHeaders.Add("user-id", aze<arg>.Instance.User.Id);
                    http.DefaultRequestHeaders.Add("user-token", userToken);
                    HttpResponseMessage responseData = http.GetAsync(aze<axh>.Instance.Url.ApiGatewayUrl + (string)contentAfterApiPost).Result;
                    string get_result = responseData.Content.ReadAsStringAsync().Result;
                    try
                    {
                        SendResponse = JsonConvert.DeserializeObject<JObject>(get_result);
                    }
                    catch (JsonReaderException)
                    {
                        SendResponse = get_result;
                    }

                    if (Path_Bool.IsDebug)
                    {
                        DebugPrint.LogDebug_NoColorSelect("[HTTP][GET]请求返回内容:" + get_result);
                    }
                }
            }
            sendJsonResponse(context.Response, SendResponse);
        }
    }

    private void handleDynamicRequest(HttpListenerContext context)
    {
        // 读取 POST 请求的内容
        using (StreamReader reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
        {
            string requestBody = reader.ReadToEnd();
            dynamic SendResponse = new ExpandoObject();
            dynamic requestObject = null;
            requestObject = JsonConvert.DeserializeObject<dynamic>(requestBody);
            // 获取"/api/"后面的内容
            string contentAfterApiPost = "/"+context.Request.Url.AbsolutePath.Substring(5);
            DebugPrint.LogDebug_NoColorSelect($"[POST]args:{contentAfterApiPost}");
            if (context.Request.HttpMethod == "POST")
            {

                HttpClient http = new HttpClient();
                string userToken = WPFLauncher.Util.ss.e((string)contentAfterApiPost, requestBody);
                SendResponse.UserToken = userToken;
            }
            else if (context.Request.HttpMethod == "GET")
            {
                bool IsPostFlag = false;
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
                    HttpClient httpClient = new HttpClient();
                    string userTokenValue = WPFLauncher.Util.ss.e((string)contentAfterApiPost, requestBody);
                    SendResponse.UserToken = userTokenValue;
                    IsPostFlag = true;
                }

                if (!IsPostFlag)
                {
                    HttpClient http = new HttpClient();
                    string userToken = WPFLauncher.Util.ss.e((string)contentAfterApiPost, "");
                    SendResponse.UserToken = userToken;
                }
            }
            sendJsonResponse(context.Response, SendResponse);
        }
    }

    private void handleGetRequest(HttpListenerContext context)
    {
        bool IsSendResponseFlag = true;
        dynamic SendResponse = new ExpandoObject();
        switch (context.Request.Url.AbsolutePath)
        {
            case "/help":
                // 重定向到URL : https://wpflauncherhook.apifox.cn/
                context.Response.Redirect("https://wpflauncherhook.apifox.cn/");
                IsSendResponseFlag = false;
                context.Response.Close(); // 确保响应被正确关闭
                break;
            case "/get_login_info":
                SendResponse.a = (string)typeof(arf).GetField("a").GetValue(aze<arg>.Instance);
                ;
                SendResponse.b = (string)typeof(arf).GetField("b").GetValue(aze<arg>.Instance);
                ;
                SendResponse.UserInfo =
                    JsonConvert.DeserializeObject<JObject>(
                        JsonConvert.SerializeObject(aze<arg>.Instance.User));
                SendResponse.NotifyMessageList = aze<arg>.Instance.NotifyMessageList;
                SendResponse.RepairList = aze<arg>.Instance.RepairList;
                SendResponse.ClusterList = aze<arg>.Instance.ClusterList;
                SendResponse.JavaFixM = aze<arg>.Instance.JavaFixM;
                SendResponse.UserM =
                    JsonConvert.DeserializeObject<JObject>(
                        JsonConvert.SerializeObject(aze<UserM>.Instance));
                break;
            case "/get_x19info":
                SendResponse =
                    JsonConvert.DeserializeObject<JObject>(
                        JsonConvert.SerializeObject(aze<axh>.Instance));
                break;
            case "/get_roominfo":
                if (Path_Bool.RoomInfo != null)
                {
                    // 创建更完整的房间信息响应
                    dynamic response = new ExpandoObject();
                    
                    // 设置基本房间信息
                    response.roomInfo = Path_Bool.RoomInfo;
                    
                    // 提取和设置常用属性
                    var entity = Path_Bool.RoomInfo.entity;
                    response.roomId = entity.entity_id;
                    response.roomName = entity.room_name;
                    response.ownerId = entity.owner_id;
                    response.maxPlayers = entity.max_count;
                    response.currentPlayers = entity.cur_num;
                    response.gameStatus = entity.game_status;
                    response.version = entity.version;
                    response.hasPassword = entity.password != null && entity.password.ToString() != "0";
                    response.password = Path_Bool.Password;
                    response.visibility = entity.visibility;
                    response.allowSave = entity.allow_save;
                    response.resId = entity.res_id;
                    SendResponse.UserJoinTime = Path_Bool.JoinOrCreateTime;
                    response.UserInputPassword = Path_Bool.Password;
                    
                    // 设置当前用户ID
                    response.currentUserId = aze<arg>.Instance.User.Id;
                    
                    // 判断是否是房主
                    response.isOwner = entity.owner_id == aze<arg>.Instance.User.Id;
                    
                    // 房间黑名单配置信息
                    response.isRoomBlacklistEnabled = Path_Bool.EnableRoomBlacklist;
                    response.isRegexBlacklistEnabled = Path_Bool.EnableRoomBlacklist;
                    
                    // 获取并设置玩家信息
                    if (entity.fids != null && entity.fids.Count > 0)
                    {
                        // 获取玩家详细信息
                        var playersInfoResponse = X19Http.RequestX19Api("/user/query/search-by-ids", 
                            JsonConvert.SerializeObject(new { entity_ids = entity.fids }));
                        response.playersInfo = JsonConvert.DeserializeObject(playersInfoResponse);
                        
                        // 处理玩家列表数据，适配前端展示
                        var playersList = new List<dynamic>();
                        var playersMap = new Dictionary<string, dynamic>();
                        JObject playersInfo = JObject.Parse(playersInfoResponse);
                        
                        if (playersInfo["entities"] != null)
                        {
                            foreach (var playerInfo in playersInfo["entities"])
                            {
                                string userId = playerInfo["entity_id"].ToString();
                                string playerName = playerInfo["name"].ToString();
                                string avatarUrl = playerInfo["avatar_image_url"]?.ToString() ?? "https://x19.fp.ps.netease.com/file/5a34e0777f9d2a8a4ea3d36eza31LhW3";
                                string frameId = playerInfo["frame_id"]?.ToString() ?? "";
                                string gender = playerInfo["gender"]?.ToString() ?? "m";
                                string signature = playerInfo["signature"]?.ToString() ?? "";
                                
                                var playerData = new ExpandoObject() as dynamic;
                                playerData.userId = userId;
                                playerData.playerName = playerName;
                                playerData.avatarUrl = avatarUrl;
                                playerData.role = userId == entity.owner_id ? "房主" : "成员";
                                playerData.ident = userId == entity.owner_id ? 1 : 0;
                                playerData.inBlacklist = Path_Bool.RoomBlacklist != null && Path_Bool.RoomBlacklist.Contains(userId);
                                playerData.frameId = frameId;
                                playerData.gender = gender;
                                playerData.signature = signature;
                                
                                playersList.Add(playerData);
                                playersMap[userId] = playerData;
                            }
                        }
                        
                        response.playersList = playersList;
                        response.playersMap = playersMap;
                        response.playerCount = playersList.Count;
                    }
                    
                    // 获取并设置房主信息
                    var ownerInfoResponse = X19Http.RequestX19Api("/user/query/search-by-uid", 
                        JsonConvert.SerializeObject(new { user_id = entity.owner_id }));
                    response.ownerInfo = JObject.Parse(ownerInfoResponse);
                    
                    // 获取房间黑名单
                    if (Path_Bool.EnableRoomBlacklist)
                    {
                        response.roomBlacklist = Path_Bool.RoomBlacklist;
                    }
                    else
                    {
                        response.roomBlacklist = new List<string>();
                    }
                    
                    // 获取正则黑名单
                    if (Path_Bool.EnableRoomBlacklist)
                    {
                        response.regexBlacklist = Path_Bool.RegexBlacklist;
                    }
                    else
                    {
                        response.regexBlacklist = new List<string>();
                    }
                    
                    // 获取地图资源信息
                    if (!string.IsNullOrEmpty(entity.res_id))
                    {
                        var resourceInfoResponse = X19Http.RequestX19Api("/item-details/get_v2",
                            JsonConvert.SerializeObject(new { item_id = entity.res_id }));
                        response.resourceInfo = JsonConvert.DeserializeObject(resourceInfoResponse);
                        
                        // 获取资源图片信息
                        var titleImageResponse = X19Http.RequestX19Api("/item-channel/query/search-by-item-channel",
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
                SendResponse.roomInfo = Path_Bool.RoomInfo;
                
                // 提取和设置常用属性 Path_Bool.RoomInfo.entity;
                SendResponse.roomId = Path_Bool.RoomInfo.entity.entity_id;
                SendResponse.roomName = Path_Bool.RoomInfo.entity.room_name;
                SendResponse.ownerId = Path_Bool.RoomInfo.entity.owner_id;
                SendResponse.maxPlayers = Path_Bool.RoomInfo.entity.max_count;
                SendResponse.currentPlayers = Path_Bool.RoomInfo.entity.cur_num;
                SendResponse.gameStatus = Path_Bool.RoomInfo.entity.game_status;
                SendResponse.version = Path_Bool.RoomInfo.entity.version;
                SendResponse.hasPassword = Path_Bool.RoomInfo.entity.password != null && Path_Bool.RoomInfo.entity.password.ToString() != "0";
                SendResponse.password = Path_Bool.Password;
                SendResponse.visibility = Path_Bool.RoomInfo.entity.visibility;
                SendResponse.allowSave = Path_Bool.RoomInfo.entity.allow_save;
                SendResponse.resId = Path_Bool.RoomInfo.entity.res_id;
                SendResponse.UserInputPassword = Path_Bool.Password;
                SendResponse.UserJoinTime = Path_Bool.JoinOrCreateTime;
                SendResponse.PlayerList = Path_Bool.RoomPlayerList;
                // 设置当前用户ID
                SendResponse.currentUserId = aze<arg>.Instance.User.Id;
                
                // 判断是否是房主
                SendResponse.isOwner = Path_Bool.RoomInfo.entity.owner_id == aze<arg>.Instance.User.Id;
                
                // 房间黑名单配置信息
                SendResponse.isRoomBlacklistEnabled = Path_Bool.EnableRoomBlacklist;
                SendResponse.isRegexBlacklistEnabled = Path_Bool.EnableRoomBlacklist;
                break;
            case "/get_pathinfo":
                var staticMembers =
                    typeof(ta).GetFields(BindingFlags.Static | BindingFlags.Public);

                foreach (var member in staticMembers)
                {
                    ((IDictionary<string, object>)SendResponse)[member.Name] =
                        member.GetValue(null);
                }

                SendResponse =
                    JsonConvert.DeserializeObject<JObject>(
                        JsonConvert.SerializeObject(SendResponse));
                break;
            case "/get_userCppToken":
                string text = WPFLauncher.Util.ss.f();
                byte[] array = WPFLauncher.Util.tk.c(text);
                SendResponse.UserCppToken = text;
                SendResponse.Base64Token = Convert.ToBase64String(array);
                break;
            case "/get_RecvInfo":
                SendResponse = new {SendKey = WPFLauncher.Network.Service.ade.SendKey, RecvKey = WPFLauncher.Network.Service.ade.RecvKey, DataList = Path_Bool.RecvList};
                break;
            case "/get_RoomBlacklist":
                if (Path_Bool.EnableRoomBlacklist) // 判断房间黑名单功能是否开启
                {
                    if (Path_Bool.RoomBlacklist != null && Path_Bool.RoomBlacklist.Count > 0)
                    {
                        // 获取黑名单用户详细信息
                        try
                        {
                            JObject blacklistUsersInfo = X19Http.Get_Players_Info(Path_Bool.RoomBlacklist);
                            SendResponse = new { 
                                error = 0, 
                                message = "获取黑名单成功", 
                                blacklist = Path_Bool.RoomBlacklist.ToList(),
                                blacklistInfo = blacklistUsersInfo,
                                count = Path_Bool.RoomBlacklist.Count
                            };
                        }
                        catch (Exception ex)
                        {
                            // 如果获取详细信息失败，至少返回ID列表
                            SendResponse = new { 
                                error = 0, 
                                message = "获取黑名单成功，但获取用户详情失败", 
                                blacklist = Path_Bool.RoomBlacklist.ToList(),
                                errorDetail = ex.Message,
                                count = Path_Bool.RoomBlacklist.Count
                            };
                        }
                    }
                    else
                    {
                        SendResponse = new { error = 0, message = "黑名单为空", blacklist = new List<string>(), count = 0 };
                    }
                }
                else
                {
                    SendResponse = new { error = 1, message = "房间黑名单功能未开启", isEnabled = false };
                }
                break;
            case "/get_RegexBlacklist":
                if (Path_Bool.EnableRoomBlacklist) // 判断正则表达式黑名单功能是否开启
                {
                    if (Path_Bool.RegexBlacklist != null && Path_Bool.RegexBlacklist.Count > 0)
                    {
                        // 返回更详细的黑名单信息
                        var regexDetails = new List<object>();
                        foreach (var pattern in Path_Bool.RegexBlacklist)
                        {
                            regexDetails.Add(new {
                                pattern = pattern,
                                isValid = true // 假设所有模式都是有效的
                            });
                        }
                        
                        SendResponse = new { 
                            error = 0, 
                            message = "获取正则黑名单成功", 
                            regexBlacklist = Path_Bool.RegexBlacklist.ToList(),
                            regexDetails = regexDetails,
                            count = Path_Bool.RegexBlacklist.Count
                        };
                    }
                    else
                    {
                        SendResponse = new { error = 0, message = "正则黑名单为空", regexBlacklist = new List<string>(), count = 0 };
                    }
                }
                else
                {
                    SendResponse = new { error = 1, message = "正则表达式黑名单功能未开启", isEnabled = false };
                }
                break;
            case "/blacklist/status":
                // 返回黑名单功能状态信息
                SendResponse = new {
                    error = 0,
                    roomBlacklist = new {
                        isEnabled = Path_Bool.EnableRoomBlacklist,
                        count = Path_Bool.RoomBlacklist?.Count ?? 0
                    },
                    regexBlacklist = new {
                        isEnabled = Path_Bool.EnableRoomBlacklist,
                        count = Path_Bool.RegexBlacklist?.Count ?? 0
                    }
                };
                break;
            case "/Send_ChatMessage":
                try
                {
                    // 使用示例
                    var rawQuery = context.Request.Url.Query;
                    var queryParams = ParseQueryString(rawQuery);

                    string message = queryParams["message"];
                    uint ChatUserID = uint.Parse(queryParams["userID"]);
                    
                    if (string.IsNullOrEmpty(message))
                    {
                        SendResponse = new { error = 1, message = "消息内容不能为空,请使用?message=xxx格式" };
                    }
                    else
                    {
                        // 检查编码设置
                        string contentType = context.Request.Headers["Content-Type"] ?? "";
                        if (contentType.ToLower().Contains("charset=utf-8"))
                        {
                            message = Encoding.UTF8.GetString(Encoding.Default.GetBytes(message));
                        }
                        
                        new WPFLauncher.Network.Service.acq().e(ChatUserID, message);
                        var startTime = DateTime.Now;
                        while (true)
                        {
                            if (!string.IsNullOrEmpty(Path_Bool.Get_Recv_String_ChatResult))
                            {
                                JObject Get_Recv_String_ChatResult_ToJson =
                                    JObject.Parse(Path_Bool.Get_Recv_String_ChatResult);
                                if (!((IDictionary<string, JToken>)
                                        Get_Recv_String_ChatResult_ToJson).ContainsKey("Get_Recv_String_ChatResult") && Get_Recv_String_ChatResult_ToJson["err"].ToObject<int>() != 0)
                                {
                                    SendResponse = new { error = 1, message = "发送失败", errorInfo = Get_Recv_String_ChatResult_ToJson };
                                    Path_Bool.Get_Recv_String_ChatResult = string.Empty;
                                }
                                else
                                {
                                    SendResponse = new { error = 0, message = "发送成功", SendResult = Get_Recv_String_ChatResult_ToJson, SendMessage = message, ToUserID = ChatUserID};
                                    Path_Bool.Get_Recv_String_ChatResult = string.Empty;
                                }
                                break;
                            }
                            if((DateTime.Now - startTime).TotalSeconds > 3)
                            {
                                SendResponse = new { error = 1, message = "发送超时" };
                                Path_Bool.Get_Recv_String_ChatResult = string.Empty;
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

                    string message = queryParams["message"];
                    string GroupID = queryParams["groupid"];
                    
                    if (string.IsNullOrEmpty(message))
                    {
                        SendResponse = new { error = 1, message = "消息内容不能为空,请使用?message=xxx格式" };
                    }
                    else
                    {
                        // 检查编码设置
                        string contentType = context.Request.Headers["Content-Type"] ?? "";
                        if (contentType.ToLower().Contains("charset=utf-8"))
                        {
                            message = Encoding.UTF8.GetString(Encoding.Default.GetBytes(message));
                        }
                        
                        new WPFLauncher.Network.Service.acq().a(GroupID, message);
                        var startTime = DateTime.Now;
                        while (true)
                        {
                            if (!string.IsNullOrEmpty(Path_Bool.Get_Recv_String_ChatResult))
                            {
                                JObject Get_Recv_String_ChatResult_ToJson =
                                    JObject.Parse(Path_Bool.Get_Recv_String_ChatResult);
                                if (!((IDictionary<string, JToken>)
                                        Get_Recv_String_ChatResult_ToJson).ContainsKey("Get_Recv_String_ChatResult") && Get_Recv_String_ChatResult_ToJson["err"].ToObject<int>() != 0)
                                {
                                    SendResponse = new { error = 1, message = "发送失败", errorInfo = Get_Recv_String_ChatResult_ToJson };
                                    Path_Bool.Get_Recv_String_ChatResult = string.Empty;
                                }
                                else
                                {
                                    SendResponse = new { error = 0, message = "发送成功", SendResult = Get_Recv_String_ChatResult_ToJson, SendMessage = message, ToGroupID = GroupID};
                                    Path_Bool.Get_Recv_String_ChatResult = string.Empty;
                                }
                                break;
                            }
                            if((DateTime.Now - startTime).TotalSeconds > 3)
                            {
                                SendResponse = new { error = 1, message = "发送超时" };
                                Path_Bool.Get_Recv_String_ChatResult = string.Empty;
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
                        string contentAfterApiPost = context.Request.Url.AbsolutePath.Substring(6);
                        // 使用逗号分割多个用户ID
                        List<string> userIds = contentAfterApiPost.Split(',').ToList();
                        JObject response = X19Http.Get_Players_Info(userIds);
                        SendResponse = JToken.FromObject(response);
                    }
                    else if (context.Request.Url.AbsolutePath == "/roommanage")
                    {
                        try
                        {
                            // 使用HtmlResource获取房间管理页面内容
                            string htmlContent = HtmlResource.GetRoomManageHtml();
                            
                            // 向客户端发送HTML内容
                            context.Response.ContentType = "text/html";
                            byte[] buffer = Encoding.UTF8.GetBytes(htmlContent);
                            context.Response.ContentLength64 = buffer.Length;
                            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                            context.Response.OutputStream.Close();
                            IsSendResponseFlag = false;
                        }
                        catch (Exception ex)
                        {
                            // 处理读取或发送HTML文件时可能出现的错误
                            SendResponse = JToken.FromObject(new { error = 1, message = $"加载房间管理页面失败: {ex.Message}" });
                            Console.WriteLine($"[Http]Error loading RoomManage.html: {ex.Message}");
                        }
                    }
                    else if (context.Request.Url.AbsolutePath.StartsWith("/console/show"))
                    {
                        try
                        {
                            AllocConsole(); // 使用Kernel32.dll分配一个新的控制台
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
                            
                            string message = queryParams["message"] ?? "无消息内容";
                            string color = queryParams["color"] ?? "white";
                            
                            ConsoleColor consoleColor = ConsoleColor.White;
                            switch(color.ToLower())
                            {
                                case "red": consoleColor = ConsoleColor.Red; break;
                                case "green": consoleColor = ConsoleColor.Green; break;
                                case "blue": consoleColor = ConsoleColor.Blue; break;
                                case "yellow": consoleColor = ConsoleColor.Yellow; break;
                                case "cyan": consoleColor = ConsoleColor.Cyan; break;
                                case "magenta": consoleColor = ConsoleColor.Magenta; break;
                                default: consoleColor = ConsoleColor.White; break;
                            }
                            
                            Console.ForegroundColor = consoleColor;
                            Console.WriteLine($"[WebAPI] {message}");
                            Console.ResetColor();
                            
                            SendResponse = new { error = 0, message = "日志已添加", content = message, color = color };
                        }
                        catch (Exception ex)
                        {
                            SendResponse = new { error = 1, message = "添加日志失败", errorInfo = ex.Message };
                        }
                    }                                        
                    else if (context.Request.Url.AbsolutePath.StartsWith("/Room/AddBlacklist/"))
                    {
                        string userId = context.Request.Url.AbsolutePath.Substring("/Room/AddBlacklist/".Length);
                        if (string.IsNullOrEmpty(userId))
                        {
                            SendResponse = JToken.FromObject(new { error = 1, message = "用户ID不能为空" });
                        }
                        else if (Path_Bool.RoomInfo == null)
                        {
                            SendResponse = JToken.FromObject(new { error = 1, message = "当前未在房间中" });
                        }
                        else if (!Path_Bool.EnableRoomBlacklist)
                        {
                            SendResponse = JToken.FromObject(new { error = 1, message = "房间黑名单功能未开启" });
                        }
                        else
                        {
                            // 将用户添加到黑名单
                            if (Path_Bool.RoomBlacklist == null)
                            {
                                Path_Bool.RoomBlacklist = new List<string>();
                            }

                            if (!Path_Bool.RoomBlacklist.Contains(userId))
                            {
                                Path_Bool.RoomBlacklist.Add(userId);
                            }

                            // 保存黑名单到文件
                            string blacklistFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "RoomConfig");
                            string blacklistFilePath = Path.Combine(blacklistFolderPath, "BlackList.json");

                            Directory.CreateDirectory(blacklistFolderPath);
                            File.WriteAllText(blacklistFilePath, JsonConvert.SerializeObject(Path_Bool.RoomBlacklist));

                            // 如果当前用户是房主，尝试踢出黑名单用户
                            string currentUserId = aze<arg>.Instance.User.Id;
                            if (Path_Bool.RoomInfo.entity.owner_id == currentUserId && Path_Bool.RoomInfo.entity.fids.Contains(userId))
                            {
                                try
                                {
                                    string requestData = JsonConvert.SerializeObject(new
                                    {
                                        room_id = Path_Bool.RoomInfo.entity.entity_id,
                                        user_id = userId
                                    });

                                    JObject kickResult = JObject.Parse(X19Http.RequestX19Api("/online-lobby-member-kick", requestData));
                                    if (kickResult["code"].ToObject<int>() == 0)
                                    {
                                        Console.WriteLine($"[RoomManage] 已将玩家 {userId} 踢出房间并加入黑名单");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"[RoomManage] 踢出玩家失败: {ex.Message}");
                                }
                            }

                            SendResponse = JToken.FromObject(new { error = 0, message = "已将用户添加到黑名单" });
                        }
                    }
                    else if (context.Request.Url.AbsolutePath.StartsWith("/Room/RemoveBlacklist/"))
                    {
                        string userId = context.Request.Url.AbsolutePath.Substring("/Room/RemoveBlacklist/".Length);
                        if (string.IsNullOrEmpty(userId))
                        {
                            SendResponse = JToken.FromObject(new { error = 1, message = "用户ID不能为空" });
                        }
                        else if (!Path_Bool.EnableRoomBlacklist)
                        {
                            SendResponse = JToken.FromObject(new { error = 1, message = "房间黑名单功能未开启" });
                        }
                        else if (Path_Bool.RoomBlacklist == null || !Path_Bool.RoomBlacklist.Contains(userId))
                        {
                            SendResponse = JToken.FromObject(new { error = 1, message = "该用户不在黑名单中" });
                        }
                        else
                        {
                            // 从黑名单中移除用户
                            Path_Bool.RoomBlacklist.Remove(userId);

                            // 保存黑名单到文件
                            string blacklistFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "RoomConfig");
                            string blacklistFilePath = Path.Combine(blacklistFolderPath, "BlackList.json");

                            Directory.CreateDirectory(blacklistFolderPath);
                            File.WriteAllText(blacklistFilePath, JsonConvert.SerializeObject(Path_Bool.RoomBlacklist));

                            SendResponse = JToken.FromObject(new { error = 0, message = "已将用户从黑名单中移除" });
                        }
                    }
                    else if (context.Request.Url.AbsolutePath.StartsWith("/Room/Kick/"))
                    {
                        string userId = context.Request.Url.AbsolutePath.Substring("/Room/Kick/".Length);
                        if (string.IsNullOrEmpty(userId))
                        {
                            SendResponse = JToken.FromObject(new { error = 1, message = "用户ID不能为空" });
                        }
                        else if (Path_Bool.RoomInfo == null)
                        {
                            SendResponse = JToken.FromObject(new { error = 1, message = "当前未在房间中" });
                        }
                        else
                        {
                            // 检查当前用户是否是房主
                            string currentUserId = aze<arg>.Instance.User.Id;
                            if (Path_Bool.RoomInfo.entity.owner_id != currentUserId)
                            {
                                SendResponse = JToken.FromObject(new { error = 1, message = "只有房主才能踢出玩家" });
                            }
                            else if (!Path_Bool.RoomInfo.entity.fids.Contains(userId))
                            {
                                SendResponse = JToken.FromObject(new { error = 1, message = "该玩家不在房间中" });
                            }
                            else
                            {
                                try
                                {
                                    string requestData = JsonConvert.SerializeObject(new
                                    {
                                        room_id = Path_Bool.RoomInfo.entity.entity_id,
                                        user_id = userId
                                    });

                                    JObject kickResult = JObject.Parse(X19Http.RequestX19Api("/online-lobby-member-kick", requestData));
                                    if (kickResult["code"].ToObject<int>() == 0)
                                    {
                                        SendResponse = JToken.FromObject(new { error = 0, message = "已将玩家踢出房间" });
                                        Console.WriteLine($"[RoomManage] 已将玩家 {userId} 踢出房间");
                                    }
                                    else
                                    {
                                        SendResponse = JToken.FromObject(new { error = 1, message = $"踢出玩家失败: {kickResult["message"]}" });
                                    }
                                }
                                catch (Exception ex)
                                {
                                    SendResponse = JToken.FromObject(new { error = 1, message = $"踢出玩家失败: {ex.Message}" });
                                }
                            }
                        }
                    }
                    else if (context.Request.Url.AbsolutePath.StartsWith("/Room/AddRegexBlacklist/"))
                    {
                        string pattern = WebUtility.UrlDecode(context.Request.Url.AbsolutePath.Substring(24));
                        var addedPatterns = new List<string>();
                        var skippedPatterns = new List<string>();

                        if (Path_Bool.EnableRoomBlacklist)
                        {
                            if (string.IsNullOrEmpty(pattern) || Path_Bool.RegexBlacklist.Contains(pattern))
                            {
                                skippedPatterns.Add(pattern);
                            }
                            else
                            {
                                Path_Bool.RegexBlacklist.Add(pattern);
                                addedPatterns.Add(pattern);
                            }

                            // 刷新正则黑名单文件
                            Path_Bool.WriteRegexBlacklist();
                            if (Path_Bool.RoomInfo != null)
                            {
                                var RoomKickInfo = new JArray();
                                var RoomPlayerIsMatchInfo = new JArray();
                                JObject Get_RoomPlayerInfo =
                                    X19Http.Get_Players_Info(Path_Bool.RoomInfo.entity.fids);
                                foreach (var player in Get_RoomPlayerInfo["entities"])
                                {
                                    string playerName = player?["name"]?.ToObject<string>();
                                    string userId = player?["entity_id"]?.ToObject<string>();
                                    bool isMatch = System.Text.RegularExpressions.Regex.IsMatch(playerName, pattern);
                                    if (isMatch)
                                    {
                                        RoomPlayerIsMatchInfo.Add(JToken.FromObject(new {UserID = userId,RoomplayerName = playerName, IsMatchRegex = true}));
                                        // 判断userId是否存在黑名单里
                                        if (!Path_Bool.RoomBlacklist.Contains(userId))
                                        {
                                            // 将玩家UID加入黑名单
                                            Path_Bool.RoomBlacklist.Add(userId);
                                        }
                                        // 踢出玩家
                                        JObject RemovePlayerReturn;
                                        do
                                        {
                                            RemovePlayerReturn = JObject.Parse(X19Http.RequestX19Api("/online-lobby-member-kick", JsonConvert.SerializeObject(new { room_id = Path_Bool.RoomInfo.entity.entity_id, user_id = userId })));
                                            if (RemovePlayerReturn["code"].ToObject<int>() == 0)
                                            {
                                                Console.ForegroundColor = ConsoleColor.Red;
                                                Console.WriteLine("[RoomInfo]玩家 " + playerName + " 在正则黑名单内,已自动踢出房间");
                                                RoomKickInfo.Add(JToken.FromObject(new { RoomplayerName = playerName, PlayerUserID = userId, RemovePlayer = RemovePlayerReturn}));
                                                break; // 成功踢出后退出循环
                                            }
                                            else
                                            {
                                                Console.ForegroundColor = ConsoleColor.Red;
                                                Console.WriteLine(@"[RoomInfo]玩家 " + playerName + " 在正则黑名单内,踢出失败,正在重试...");
                                            }
                                        } while (true); // 一直重试直到成功
                                    }
                                    else
                                    {
                                        RoomPlayerIsMatchInfo.Add(JToken.FromObject(new {UserID = userId,RoomPlayerName = playerName, IsMatchRegex = false}));
                                    }
                                }
                                Path_Bool.WriteRoomBlacklist();
                                SendResponse = JToken.FromObject(new { error = 0, message = "添加成功", addedPatterns = addedPatterns, skippedPatterns = skippedPatterns, RoomInfo = new {PlayerIsMatchInfo = RoomPlayerIsMatchInfo, PlayerKickInfo = RoomKickInfo } });
                            }
                            else
                            {
                                SendResponse = JToken.FromObject(new { error = 0, message = "添加成功", addedPatterns = addedPatterns, skippedPatterns = skippedPatterns });
                            }

                        }
                        else
                        {
                            SendResponse = JToken.FromObject(new { error = 1, message = "正则黑名单功能未开启" });
                        }
                    }
                    else if (context.Request.Url.AbsolutePath.StartsWith("/Room/RemoveRegexBlacklist/"))
                    {
                        string regexPattern = context.Request.Url.AbsolutePath.Substring(27);
                        if (Path_Bool.EnableRoomBlacklist) // 判断正则黑名单功能是否开启
                        {
                            if (Path_Bool.RegexBlacklist.Contains(regexPattern)) // 判断正则是否在黑名单内
                            {
                                Path_Bool.RegexBlacklist.Remove(regexPattern);
                                // 刷新正则黑名单文件
                                string regexBlacklistFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "RoomConfig");
                                string regexBlacklistFilePath = Path.Combine(regexBlacklistFolderPath, "RegexBlackList.json");
                                File.WriteAllText(regexBlacklistFilePath, JsonConvert.SerializeObject(Path_Bool.RegexBlacklist));
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
                        string itemID = context.Request.Url.AbsolutePath.Substring("/GetItemIDFileUrl/".Length);
                        SendResponse = JObject.Parse(
                            X19Http.RequestX19Api("/user-item-download-v2",
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
                            jo CreateRoomInfo = new jo();
                            CreateRoomInfo.Password = queryParams["Password"]?.ToString() ?? string.Empty;
                            CreateRoomInfo.MaxPlayer = uint.TryParse(queryParams["MaxPlayer"], out uint maxPlayer) ? maxPlayer : 10;
                            CreateRoomInfo.WorldName = queryParams["WorldName"]?.ToString() ?? aze<arg>.Instance.User.Id;
                            string VisibilityStatus = queryParams["Visibility"];
                            if (string.IsNullOrEmpty(VisibilityStatus))
                            {
                                VisibilityStatus = "OPEN";
                            }
                            if (VisibilityStatus == "OPEN")
                            {
                                CreateRoomInfo.VisibleScope = RoomVisibleStatus.OPEN;
                            }
                            else if (VisibilityStatus == "FRIEND")
                            {
                                CreateRoomInfo.VisibleScope = RoomVisibleStatus.FRIEND;
                            }
                            else if (VisibilityStatus == "HIDDEN")
                            {
                                CreateRoomInfo.VisibleScope = RoomVisibleStatus.HIDDEN;
                            }
                            else
                            {
                                CreateRoomInfo.VisibleScope = RoomVisibleStatus.OPEN;
                            }
                            //CreateRoomInfo.VisibleScope = queryParams["VisibleScope"];
                            OnlineMapM MapInfo = new OnlineMapM();
                            MapInfo.ID = queryParams["ResId"] ?? "";
                            if (MapInfo.ID == String.Empty)
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
                                Type aqfType = assembly.GetType("WPFLauncher.Manager.aqf") ?? 
                                              assembly.GetTypes().FirstOrDefault(t => t.Name == "aqf");
                                
                                if (aqfType != null)
                                {
                                    // 查找 kc 类型
                                    Type kcType = assembly.GetType("WPFLauncher.ViewModel.Launcher.kc") ?? 
                                                 assembly.GetTypes().FirstOrDefault(t => t.Name == "kc");
                                    
                                    if (kcType != null)
                                    {
                                        // 获取 Singleton<aqf>.Instance 属性
                                        Type singletonType = typeof(Singleton<>).MakeGenericType(aqfType);
                                        PropertyInfo instanceProperty = singletonType.GetProperty("Instance");
                                        
                                        if (instanceProperty != null)
                                        {
                                            // 获取 Instance 对象
                                            object instance = instanceProperty.GetValue(null);
                                            
                                            if (instance != null)
                                            {
                                                // 获取 f 字段
                                                FieldInfo fField = aqfType.GetField("f", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                                                
                                                if (fField != null)
                                                {
                                                    // 获取 f 的值
                                                    object fValue = fField.GetValue(instance);
                                                    
                                                    // 如果 f 为 null，创建一个新的 kc 实例并设置
                                                    if (fValue == null)
                                                    {
                                                        fValue = Activator.CreateInstance(kcType);
                                                        fField.SetValue(instance, fValue);
                                                    }
                                                    
                                                    // 查找 LaunchGamePage 枚举类型
                                                    Type launchGamePageType = assembly.GetType("WPFLauncher.ViewModel.Launcher.LaunchGamePage") ??
                                                                           assembly.GetTypes().FirstOrDefault(t => t.Name == "LaunchGamePage");
                                                    
                                                    if (launchGamePageType != null && launchGamePageType.IsEnum)
                                                    {
                                                        // 获取 kc 类的 a 方法
                                                        methodA = kcType.GetMethod("a", 
                                                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                                                            null,
                                                            new Type[] { launchGamePageType, typeof(object), typeof(int) },
                                                            null);

                                                        if (methodA != null)
                                                        {
                                                            // 获取枚举值 15 (根据IL代码)
                                                            object pageEnum = Enum.ToObject(launchGamePageType, 15);
                                                            
                                                            // 调用 a 方法，传入参数 (15, null, -1)
                                                            methodA.Invoke(fValue, new object[] { pageEnum, null, -1 });
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
                                Console.WriteLine($"反射访问内部类时出错: {ex.Message}");
                                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                            }
                            
                            // 获取 jo 类的类型信息
                            Type joType = typeof(jo);

                            // 明确指定要调用的方法：a(object obj)
                            methodA = joType.GetMethod(
                                "a",
                                BindingFlags.NonPublic | BindingFlags.Instance,
                                null,
                                new Type[] { typeof(object) }, // 指定参数类型为 object
                                null);

                            if (methodA != null)
                            {
                                // 调用私有方法 a
                                methodA.Invoke(CreateRoomInfo, new object[] { null });
                            }
                            else
                            {
                                Console.WriteLine("方法 a 未找到，请确认方法签名是否正确。");
                            }
                            SendResponse = new { error = 0, message = $"创建房间成功" };
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
                            string roomID = queryParams["roomId"];
                            string startGame = queryParams["startGame"]?.ToString() ?? "false";
                            bool bStartGame = false;
                            if (startGame.ToLower() == "true")
                            {
                                bStartGame = true;
                            }
                            if (String.IsNullOrEmpty(roomID))
                            {
                                SendResponse = new { error = 1, message = "查询字符串必须的参数: roomId" };
                            }
                            else
                            {
                                // 👇 关键：通过 Dispatcher 调用到 UI 线程
                                bool success = false;
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
                                            dispatchException = new InvalidOperationException("WPFLauncher.Manager.Game.aum 单例未初始化");
                                            return;
                                        }

                                        // 调用 g 方法（现在在 UI 线程，可以安全创建 cm 窗口）
                                        var gMethod = typeof(WPFLauncher.Manager.Game.aum).GetMethod("g", 
                                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                                            null, new Type[] { typeof(string), typeof(bool) }, null);

                                        if (gMethod == null)
                                        {
                                            dispatchException = new MissingMethodException("未找到 WPFLauncher.Manager.Game.aum.g 方法");
                                            return;
                                        }

                                        object result = gMethod.Invoke(aulInstance, new object[] { roomID, bStartGame });
                                        success = result is not bool b || b; // 如果返回 bool 且为 true，或非 bool 视为成功
                                    }
                                    catch (Exception ex)
                                    {
                                        dispatchException = ex;
                                    }
                                });

                                if (dispatchException != null)
                                {
                                    Console.WriteLine($"UI 线程调用失败: {dispatchException}");
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
                            Console.WriteLine(e);
                            SendResponse = new { error = 1, message = $"加入房间异常: {e.Message}" };
                        }
                    }
                    else if (context.Request.Url.AbsolutePath.StartsWith("/LeftRoom"))
                    {
                        bool enableAlwaysSaveWorld = false;
                        if (Path_Bool.AlwaysSaveWorld)
                        {
                            Path_Bool.AlwaysSaveWorld = false;
                            enableAlwaysSaveWorld = true;
                        }
                        LobbyGameRoomManagerView lobbyGameRoomManagerView = aze<apn>.Instance.k<LobbyGameRoomManagerView>();
                        if (lobbyGameRoomManagerView != null)
                        {
                            // 使用Dispatcher确保在UI线程上访问DataContext
                            object roomManageMainWindow = null;
                            Type dataContextType = null;
                            bool invokeSuccess = false;
                            
                            // 在UI线程上获取DataContext
                            Application.Current.Dispatcher.Invoke(() => {
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
                                Type baseType = dataContextType.BaseType;
                                
                                // 使用Dispatcher在UI线程上调用方法
                                Application.Current.Dispatcher.Invoke(() => {
                                    // 查找 View 属性
                                    PropertyInfo viewProperty = baseType.GetProperty("View", 
                                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                                    
                                    if (viewProperty != null)
                                    {
                                        // 获取 View 对象
                                        object viewObject = viewProperty.GetValue(roomManageMainWindow);
                                        if (viewObject != null)
                                        {
                                            // 查找 c 方法
                                            MethodInfo cMethod = viewObject.GetType().GetMethod("c",
                                                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                                            
                                            if (cMethod != null)
                                            {
                                                // 调用 c() 方法
                                                cMethod.Invoke(viewObject, new object[] { });
                                            }
                                        }
                                    }
                                });
                            }
                        }

                        if (enableAlwaysSaveWorld)
                        {
                            Path_Bool.AlwaysSaveWorld = true;
                        }
                        SendResponse = new { code = 0, message = "成功退出房间", details = "" };
                    }
                    else if (context.Request.Url.AbsolutePath.StartsWith("/settings"))
                    {
                        try
                        {
                            // 返回设置管理页面
                            string htmlContent = HtmlResource.GetHotUpdateHtml(); //File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DotNetTranstor", "HTML", "HotUpdateConfig.html"));
                            
                            context.Response.ContentType = "text/html";
                            byte[] buffer = Encoding.UTF8.GetBytes(htmlContent);
                            context.Response.ContentLength64 = buffer.Length;
                            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                            context.Response.OutputStream.Close();
                            IsSendResponseFlag = false;
                        }
                        catch (Exception ex)
                        {
                            // 处理读取或发送HTML文件时可能出现的错误
                            SendResponse = new
                            {
                                error = 1,
                                message = "无法加载设置页面: " + ex.Message
                            };
                        }
                    }
                    else if (context.Request.Url.AbsolutePath.StartsWith("/config/get"))
                    {
                        // 获取当前配置
                        SendResponse = new
                        {
                            error = 0,
                            message = "获取配置成功",
                            data = new
                            {
                                IsBypassGameUpdate_Bedrock = Path_Bool.IsBypassGameUpdate_Bedrock,
                                IsEnableX64mc = Path_Bool.IsEnableX64mc,
                                IsDebug = Path_Bool.IsDebug,
                                EnableRoomBlacklist = Path_Bool.EnableRoomBlacklist,
                                EnableRegexBlacklist = Path_Bool.EnableRoomBlacklist,
                                MaxRoomCount = Path_Bool.MaxRoomCount,
                                HttpPort = Path_Bool.HttpPort,
                                NeteaseUpdateDomainhttp = Path_Bool.NeteaseUpdateDomainhttp,
                                AlwaysSaveWorld = Path_Bool.AlwaysSaveWorld,
                                IsCustomIP = Path_Bool.IsCustomIP,
                                NoTwoExitMessage = Path_Bool.NoTwoExitMessage
                            }
                        };
                    }
                    else if (context.Request.Url.AbsolutePath.StartsWith("/config/settingslist"))
                    {
                        // 获取设置列表
                        SendResponse = new
                        {
                            error = 0,
                            message = "获取设置列表成功",
                            data = new Dictionary<string, string>
                            {
                                { "IsBypassGameUpdate_Bedrock", "跳过基岩版游戏更新" },
                                { "IsEnableX64mc", "启用64位MC" },
                                { "IsDebug", "调试模式" },
                                { "EnableModsInject", "启用模组注入" },
                                { "EnableRoomBlacklist", "启用房间黑名单" },
                                { "EnableRegexBlacklist", "启用正则表达式黑名单" },
                                { "MaxRoomCount", "最大房间数" },
                                { "HttpPort", "HTTP端口" },
                                { "NeteaseUpdateDomainhttp", "网易更新域" },
                                { "IsDecryptMod", "解密模组" },
                                { "AlwaysSaveWorld", "总是保存世界" },
                                { "IsCustomIP", "自定义IP" },
                                { "NoTwoExitMessage", "禁用联机大厅退出房间二次确认" }
                            }
                        };
                        sendJsonResponse(context.Response, SendResponse);
                        IsSendResponseFlag = false;
                    }
                    else if (context.Request.Url.AbsolutePath == "/")
                    {
                        // 返回API索引信息
                        dynamic apiIndex = new ExpandoObject();
                        apiIndex.message = "正常返回";
                        apiIndex.status = 200;
                        apiIndex.details = "hello world";
                        apiIndex.endpoints = new string[] 
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

        if (IsSendResponseFlag)
        {
            sendJsonResponse(context.Response, SendResponse);
        }
    }
    
    private void HandlePostRequest(HttpListenerContext context)
    {
        try
        {
            // 读取 POST 请求的内容
            using (StreamReader reader = new StreamReader(context.Request.InputStream, Encoding.UTF8))
            {
                string requestBody = reader.ReadToEnd();
                // 解析查询字符串
                var queryParams = context.Request.QueryString;
                if (queryParams.Count > 0)
                {
                    // 将查询字符串参数添加到请求体中
                    var queryParamsDict = new Dictionary<string, string>();
                    foreach (string key in queryParams.AllKeys)
                    {
                        queryParamsDict[key] = queryParams[key];
                    }
                    // 将查询参数序列化为 JSON 字符串并合并到请求体
                    var queryParamsJson = JsonConvert.SerializeObject(queryParamsDict);
                    requestBody = $"{requestBody.TrimEnd('}')}, {queryParamsJson.TrimStart('{')}";
                }
                //Console.WriteLine($"收到 POST 请求,内容:{requestBody}");
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
                        //     http.DefaultRequestHeaders.Add("user-id", aze<arg>.Instance.User.Id);
                        //     http.DefaultRequestHeaders.Add("user-token", userToken);
                        //     var content = new StringContent((string)requestObject.body, Encoding.UTF8, "application/json");
                        //     HttpResponseMessage responseData = http.PostAsync(aze<axh>.Instance.Url.ApiGatewayUrl + (string)requestObject.url, content).Result;
                        //     string get_result = responseData.Content.ReadAsStringAsync().Result;
                        //     SendResponse.user_id = aze<arg>.Instance.User.Id;
                        //     SendResponse.user_token = userToken;
                        //     SendResponse.response = get_result;
                        //     Console.WriteLine("[HTTP][POST]请求返回内容:" + get_result);
                        //     break;
                        // case "/get":
                        //     HttpClient http_Get = new HttpClient();
                        //     string userToken_Get = WPFLauncher.Util.ss.e((string)requestObject.url, (string)requestObject.body);
                        //     http_Get.DefaultRequestHeaders.Clear();
                        //     http_Get.DefaultRequestHeaders.Add("user-id", aze<arg>.Instance.User.Id);
                        //     http_Get.DefaultRequestHeaders.Add("user-token", userToken_Get);
                        //     var content_Get = new StringContent(requestObject.body, Encoding.UTF8, "application/json");
                        //     HttpResponseMessage responseData_Get = http_Get.PostAsync(aze<axh>.Instance.Url.ApiGatewayUrl + (string)requestObject.url, content_Get).Result;
                        //     string get_result_Get = responseData_Get.Content.ReadAsStringAsync().Result;
                        //     SendResponse.user_id = aze<arg>.Instance.User.Id;
                        //     SendResponse.user_token = userToken_Get;
                        //     SendResponse.response = get_result_Get;
                        //     Console.WriteLine("[HTTP][POST]请求返回内容:" + get_result_Get);
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
                                    new WPFLauncher.Network.Service.acq().e(ChatUserID, message);
                                    var startTime = DateTime.Now;
                                    while (true)
                                    {
                                        if (!string.IsNullOrEmpty(Path_Bool.Get_Recv_String_ChatResult))
                                        {
                                            JObject Get_Recv_String_ChatResult_ToJson =
                                                JObject.Parse(Path_Bool.Get_Recv_String_ChatResult);
                                            if (!((IDictionary<string, JToken>)Get_Recv_String_ChatResult_ToJson).ContainsKey("Get_Recv_String_ChatResult") && Get_Recv_String_ChatResult_ToJson["err"].ToObject<int>() != 0)
                                            {
                                                SendResponse = new { error = 1, message = "发送失败", errorInfo = Get_Recv_String_ChatResult_ToJson };
                                                Path_Bool.Get_Recv_String_ChatResult = string.Empty;
                                            }
                                            else
                                            {
                                                SendResponse = new { error = 0, message = "发送成功", SendResult = Get_Recv_String_ChatResult_ToJson, SendMessage = message, ToUserID = ChatUserID};
                                                Path_Bool.Get_Recv_String_ChatResult = string.Empty;
                                            }
                                            break;
                                        }
                                        if((DateTime.Now - startTime).TotalSeconds > 3)
                                        {
                                            SendResponse = new { error = 1, message = "发送超时" };
                                            Path_Bool.Get_Recv_String_ChatResult = string.Empty;
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
                            SendResponse = X19Http.Get_Player_Info(requestObject["userid"].ToString());
                            break;
                        case "/usersToInfo":
                            SendResponse = X19Http.Get_Players_Info(requestObject["entity_ids"].ToObject<List<string>>());
                            break;
                        case "/DecryptX19sign":
                            SendResponse.ToDecryptX19sign = WPFLauncher.Util.ue.b(requestBody);
                            break;
                        case "/EncryptX19sign":
                            SendResponse.ToEncryptX19sign = WPFLauncher.Util.ue.a(requestBody);
                            break;
                        case "/Room/AddRegexBlacklist":
                            if (Path_Bool.EnableRoomBlacklist)
                            {
                                string pattern = requestObject["pattern"].ToString();
                                if (string.IsNullOrEmpty(pattern))
                                {
                                    SendResponse = new { error = 1, message = "正则表达式不能为空" };
                                }
                                else if (Path_Bool.RegexBlacklist.Contains(pattern))
                                {
                                    SendResponse = new { error = 1, message = "该正则表达式已存在于黑名单中" };
                                }
                                else
                                {
                                    Path_Bool.RegexBlacklist.Add(pattern);
                                    Path_Bool.WriteRegexBlacklist();
                                    
                                    // 如果在房间中，检查玩家是否符合黑名单规则
                                    if (Path_Bool.RoomInfo != null)
                                    {
                                        var RoomKickInfo = new JArray();
                                        var RoomPlayerIsMatchInfo = new JArray();
                                        JObject Get_RoomPlayerInfo = X19Http.Get_Players_Info(Path_Bool.RoomInfo.entity.fids);
                                        foreach (var player in Get_RoomPlayerInfo["entities"])
                                        {
                                            string playerName = player?["name"]?.ToObject<string>();
                                            string userId = player?["entity_id"]?.ToObject<string>();
                                            bool isMatch = System.Text.RegularExpressions.Regex.IsMatch(playerName, pattern);
                                            if (isMatch)
                                            {
                                                RoomPlayerIsMatchInfo.Add(JToken.FromObject(new {UserID = userId, RoomplayerName = playerName, IsMatchRegex = true}));
                                                // 判断userId是否存在黑名单里
                                                if (!Path_Bool.RoomBlacklist.Contains(userId))
                                                {
                                                    // 将玩家UID加入黑名单
                                                    Path_Bool.RoomBlacklist.Add(userId);
                                                }
                                                // 踢出玩家
                                                JObject RemovePlayerReturn;
                                                do
                                                {
                                                    RemovePlayerReturn = JObject.Parse(X19Http.RequestX19Api("/online-lobby-member-kick", JsonConvert.SerializeObject(new { room_id = Path_Bool.RoomInfo.entity.entity_id, user_id = userId })));
                                                    if (RemovePlayerReturn["code"].ToObject<int>() == 0)
                                                    {
                                                        Console.ForegroundColor = ConsoleColor.Red;
                                                        Console.WriteLine("[RoomInfo]玩家 " + playerName + " 在正则黑名单内,已自动踢出房间");
                                                        RoomKickInfo.Add(JToken.FromObject(new { RoomplayerName = playerName, PlayerUserID = userId, RemovePlayer = RemovePlayerReturn}));
                                                        break; // 成功踢出后退出循环
                                                    }
                                                    else
                                                    {
                                                        Console.ForegroundColor = ConsoleColor.Red;
                                                        Console.WriteLine(@"[RoomInfo]玩家 " + playerName + " 在正则黑名单内,踢出失败,正在重试...");
                                                    }
                                                } while (true); // 一直重试直到成功
                                            }
                                            else
                                            {
                                                RoomPlayerIsMatchInfo.Add(JToken.FromObject(new {UserID = userId, RoomPlayerName = playerName, IsMatchRegex = false}));
                                            }
                                        }
                                        Path_Bool.WriteRoomBlacklist();
                                        SendResponse = new { error = 0, message = "添加成功", RoomInfo = new {PlayerIsMatchInfo = RoomPlayerIsMatchInfo, PlayerKickInfo = RoomKickInfo } };
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
                            if (Path_Bool.EnableRoomBlacklist)
                            {
                                string pattern = requestObject["pattern"].ToString();
                                if (string.IsNullOrEmpty(pattern))
                                {
                                    SendResponse = new { error = 1, message = "正则表达式不能为空" };
                                }
                                else if (!Path_Bool.RegexBlacklist.Contains(pattern))
                                {
                                    SendResponse = new { error = 1, message = "该正则表达式不在黑名单中" };
                                }
                                else
                                {
                                    Path_Bool.RegexBlacklist.Remove(pattern);
                                    Path_Bool.WriteRegexBlacklist();
                                    SendResponse = new { error = 0, message = "已从正则黑名单移除" };
                                }
                            }
                            else
                            {
                                SendResponse = new { error = 1, message = "正则黑名单功能未开启" };
                            }
                            break;
                        case "/config/apply":
                            // 更新配置
                            try
                            {
                                var configData = JsonConvert.DeserializeObject<JObject>(requestBody);
                                
                                // 更新Path_Bool中的配置项
                                if (configData["IsBypassGameUpdate_Bedrock"] != null)
                                    Path_Bool.IsBypassGameUpdate_Bedrock = configData["IsBypassGameUpdate_Bedrock"].Value<bool>();
                                
                                if (configData["IsEnableX64mc"] != null)
                                    Path_Bool.IsEnableX64mc = configData["IsEnableX64mc"].Value<bool>();
                                
                                if (configData["IsDebug"] != null)
                                    Path_Bool.IsDebug = configData["IsDebug"].Value<bool>();
                                
                                if (configData["EnableRoomBlacklist"] != null)
                                    Path_Bool.EnableRoomBlacklist = configData["EnableRoomBlacklist"].Value<bool>();
                                
                                if (configData["EnableRegexBlacklist"] != null)
                                    Path_Bool.EnableRoomBlacklist = configData["EnableRegexBlacklist"].Value<bool>();
                                
                                if (configData["MaxRoomCount"] != null)
                                    Path_Bool.MaxRoomCount = configData["MaxRoomCount"].Value<int>();
                                
                                if (configData["HttpPort"] != null)
                                    Path_Bool.HttpPort = configData["HttpPort"].Value<int>();
                                
                                if (configData["NeteaseUpdateDomainhttp"] != null)
                                    Path_Bool.NeteaseUpdateDomainhttp = configData["NeteaseUpdateDomainhttp"].Value<string>();
                                
                                if (configData["AlwaysSaveWorld"] != null)
                                    Path_Bool.AlwaysSaveWorld = configData["AlwaysSaveWorld"].Value<bool>();
                                
                                if (configData["IsCustomIP"] != null)
                                    Path_Bool.IsCustomIP = configData["IsCustomIP"].Value<bool>();
                                
                                if (configData["NoTwoExitMessage"] != null)
                                    Path_Bool.NoTwoExitMessage = configData["NoTwoExitMessage"].Value<bool>();
                                
                                SendResponse = new
                                {
                                    error = 0,
                                    message = "配置更新成功"
                                };
                            }
                            catch (Exception ex)
                            {
                                SendResponse = new
                                {
                                    error = 1,
                                    message = "配置更新失败: " + ex.Message
                                };
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
            Console.WriteLine($"处理 POST 请求时发生错误: {ex.Message}");
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            var errorResponse = new
            {
                message = "服务器内部错误"
            };
            sendJsonResponse(context.Response, errorResponse);
        }
    }
    #endregion

    #region ProcessWebSocketRequest
    
    public static List<WebSocket> _webSockets = new List<WebSocket>();
        // 处理WebSocket连接
    private async Task HandleWebSocketRequest(HttpListenerContext context)
    {
        try
        {
            // 接受WebSocket连接
            HttpListenerWebSocketContext webSocketContext = await context.AcceptWebSocketAsync(null);
            WebSocket webSocket = webSocketContext.WebSocket;
            
            // 添加到WebSocket连接列表
            lock (_webSockets)
            {
                _webSockets.Add(webSocket);
            }
            
            DebugPrint.LogDebug_NoColorSelect($"[WebSocket] 新连接已建立，当前连接数: {_webSockets.Count}");
            
            // 处理WebSocket消息
            await ProcessWebSocketMessages(webSocket, webSocketContext);
        }
        catch (Exception ex)
        {
            DebugPrint.LogDebug_NoColorSelect($"[WebSocket] 处理WebSocket连接时出错: {ex.Message}");
        }
    }

    // 处理WebSocket消息
    private async Task ProcessWebSocketMessages(WebSocket webSocket, HttpListenerWebSocketContext context)
    {
        byte[] buffer = new byte[1024 * 4]; // 4KB缓冲区
        
        try
        {
            while (webSocket.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection closed", CancellationToken.None);
                    break;
                }
                
                // 处理接收到的消息
                string receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
                DebugPrint.LogDebug_NoColorSelect($"[WebSocket] 收到消息: {receivedMessage}");
                
                // 处理消息并发送响应
                // string responseMessage = ProcessWebSocketMessage(receivedMessage, context);
                //
                // byte[] responseBuffer = Encoding.UTF8.GetBytes(responseMessage);
                // await webSocket.SendAsync(new ArraySegment<byte>(responseBuffer), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            DebugPrint.LogDebug_NoColorSelect($"[WebSocket] 处理消息时出错: {ex.Message}");
        }
        finally
        {
            // 从连接列表中移除
            lock (_webSockets)
            {
                _webSockets.Remove(webSocket);
            }
            
            if (webSocket.State != WebSocketState.Closed && webSocket.State != WebSocketState.Aborted)
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection closed", CancellationToken.None);
            }
            
            webSocket.Dispose();
            DebugPrint.LogDebug_NoColorSelect($"[WebSocket] 连接已关闭，当前连接数: {_webSockets.Count}");
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
        
        byte[] buffer = Encoding.UTF8.GetBytes(message);
        
        foreach (var webSocket in socketsToBroadcast)
        {
            if (webSocket.State == WebSocketState.Open)
            {
                try
                {
                    await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    DebugPrint.LogDebug_NoColorSelect($"[WebSocket] 广播消息时出错: {ex.Message}");
                }
            }
        }
    }

    #endregion
    
    // 启动 HTTP 服务器
    public void Start(string urlPrefix)
    {
        if (!HttpListener.IsSupported)
        {
            DebugPrint.LogDebug_NoColorSelect("[Http]当前系统不支持 HttpListener.");
            return;
        }

        // 使用配置中的HTTP端口
        string httpAddress = $"http://127.0.0.1:{Path_Bool.HttpPort}/";

        _httpListener = new HttpListener();
        _httpListener.Prefixes.Add(httpAddress);  // 使用配置的地址
        _httpListener.Start();
        DebugPrint.LogDebug_NoColorSelect($"[Http]HTTP 服务器已启动,监听 {httpAddress}");

        // 开始处理请求
        while (true)
        {
            try
            {
                // 等待并获取客户端请求
                var context = _httpListener.GetContext();
                try
                {
                    if (apiRequestList.Contains(context.Request.Url.AbsolutePath))
                    {
                        handleWebApiRequest(context);
                    }
                    else if (context.Request.Url.AbsolutePath.StartsWith("/api/") && !apiRequestList.Contains(context.Request.Url.AbsolutePath))
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
                        {
                            HandlePostRequest(context);
                        }
                        else if (context.Request.HttpMethod == "GET")
                        {
                            handleGetRequest(context);
                        }
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
                    Console.WriteLine($"处理请求时发生错误: {e.Message}");
                    Console.WriteLine("[STACK TRACE]:" + e.StackTrace);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理请求时发生错误: {ex.Message}");
            }
        }
    }
    
    // 停止服务器
    public void Stop()
    {
        _httpListener.Stop();
        Console.WriteLine("HTTP 服务器已停止");
    }

    [DllImport("kernel32.dll")]
    private static extern bool AllocConsole();
    
    [DllImport("kernel32.dll")]
    private static extern bool FreeConsole();
    }
}