using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using WPFLauncher.Code;
using WPFLauncher.Network.Launcher;
using WPFLauncher.Util;
using System.Windows;
using Mcl.Core.Network;
using Mcl.Core.Network.Interface;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WPFLauncher;
using WPFLauncher.Common;
using WPFLauncher.Manager;
using WPFLauncher.Manager.Login;
using WPFLauncher.Manager.PCChannel;
using WPFLauncher.Network.Message;
using WPFLauncher.Network.Protocol;
using WPFLauncher.Util.AES;
using MessageBox = System.Windows.MessageBox;
using MicrosoftTranslator.DotNetTranstor.Tools;

namespace DotNetTranstor.Hookevent
{
    // Token: 0x02000017 RID: 23
    internal class Post_INFO_1 : IMethodHook
    {
        // [OriginalMethod]
        // public static void Dynamic_Token_Info(string foy, string foz)
        // {
        // }
        //
        // [CompilerGenerated]
        // [HookMethod("WPFLauncher.Util.sl", "e", "Dynamic_Token_Info")]
        // public static void e(string foy, string foz)
        // {
        //     if (Path_Bool.IsStartWebSocket)
        //     {
        //         WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new { type = "DynamicToken", url = foy,data = foz }));
        //     }
        //     if (Path_Bool.IsDebug)
        //     {
        //         Console.WriteLine($"[Dynamic_Token]Url路径:{foy},Post请求内容:{foz}");
        //     }
        //     Dynamic_Token_Info(foy,foz);
        // }
        // Token: 0x06004231 RID: 16945 RVA: 0x000E8EA0 File Offset: 0x000E70A0

        // public static INetResponse<cm> UserTokenPost<cm>(string jcu, aen jcv) where cm : ResponseBase, new()
        // {
        //     return null;
        // }
        //
        // [HookMethod("WPFLauncher.Network.Protocol.aem", "y", "UserTokenPost")]
        // public static INetResponse<cm> y<cm>(string jcu, aen jcv) where cm : ResponseBase, new()
        // {
        //     cm response = new cm();  // 创建 cm 类型的对象
        //
        //     // 通过反射获取 cm 的属性值
        //     var properties = response.GetType().GetProperties();
        //     var stringBuilder = new StringBuilder();
        //
        //     foreach (var property in properties)
        //     {
        //         var value = property.GetValue(response);
        //         stringBuilder.AppendLine($"{property.Name}: {value}");
        //     }
        //     string CmValue = stringBuilder.ToString();
        //     Console.WriteLine("[cm]:"+CmValue);
        //     INetResponse<cm> Get_Post_Return = UserTokenPost<cm>(jcu, jcv);
        //     Console.WriteLine("[POST_Dynamic]:"+Get_Post_Return.Content);
        //     return Get_Post_Return;
        // }
        [OriginalMethod]
        public INetResponse UserTokenPost(aeo ipd)
        {
            return null;
        }
        
        [HookMethod("WPFLauncher.Network.Protocol.aen", "d", "UserTokenPost")]
        public INetResponse d(aeo ipd)
        {
            INetResponse Get_Return = UserTokenPost(ipd);
            string needEncrypt_String = "";
            if (ipd.NeedEncrypt == aep.a)
            {
                needEncrypt_String = "Normal";
            }
            else if (ipd.NeedEncrypt == aep.b)
            {
                needEncrypt_String = "CommonEncrypt";
            }
            else if (ipd.NeedEncrypt == aep.c)
            {
                needEncrypt_String = "Authentication";
            }

            if (Path_Bool.IsStartWebSocket)
            {
                WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new { type = "Post", data = new {  url = ipd.Resource, data = ipd.Body, send = needEncrypt_String, return_data = Get_Return.Content} }));
            }
            if (Path_Bool.IsDebug)
            {
                Console.Write($"[Post]请求url地址:{ipd.Resource},内容:{ipd.Body},发送参数:{needEncrypt_String}");
                // if (IsValidJson_JArray(Get_Return.Content))
                // {
                //     Console.WriteLine();
                //     PrintJsonTree(JArray.Parse(Get_Return.Content));
                // }
                // else if (IsValidJson_JObject(Get_Return.Content))
                // {
                //     Console.WriteLine();
                //     PrintJsonTree(JObject.Parse(Get_Return.Content));
                // }
                // else
                if (true) {
                    Console.Write(",返回内容:\n"+Regex.Unescape(Get_Return.Content));
                    Console.WriteLine();
                }
            }
            return Get_Return;
        }
        // 判断字符串是否是有效的 JSON
        static bool IsValidJson_JArray(string str)
        {
            try
            {
                // 尝试解析字符串
                JArray.Parse(str);
                return true; // 解析成功，说明是有效 JSON
            }
            catch (JsonException)
            {
                return false; // 解析失败，说明不是有效 JSON
            }
        }
        static bool IsValidJson_JObject(string str)
        {
            try
            {
                // 尝试解析字符串
                JObject.Parse(str);
                return true; // 解析成功，说明是有效 JSON
            }
            catch (JsonException)
            {
                return false; // 解析失败，说明不是有效 JSON
            }
        }
        
        // 递归函数，输出 JSON 树形结构（带横竖线）
        static void PrintJsonTree(JToken token, string indent = "", bool isLast = true)
        {
            if (token is JObject obj)
            {
                var properties = obj.Properties();
                int propertyCount = properties.Count();
                int currentIndex = 0;

                foreach (var property in properties)
                {
                    bool isLastProperty = ++currentIndex == propertyCount;
                    Console.WriteLine($"{indent}{(isLast ? "└─" : "├─")} {property.Name}:");
                    PrintJsonTree(property.Value, indent + (isLast ? "    " : "│   "), isLastProperty);
                }
            }
            else if (token is JArray array)
            {
                int index = 0;
                int count = array.Count;

                foreach (var item in array)
                {
                    bool isLastItem = ++index == count;
                    Console.WriteLine($"{indent}{(isLastItem ? "└─" : "├─")} [{index}]");
                    PrintJsonTree(item, indent + (isLastItem ? "    " : "│   "), isLastItem);
                }
            }
            else
            {
                Console.WriteLine($"{indent}{(isLast ? "└─" : "├─")} {token}");
            }
        }
    }
}