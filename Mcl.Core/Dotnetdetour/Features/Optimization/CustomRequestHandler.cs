using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using CefSharp;
using CefSharp.Handler;
using CefSharp.Wpf;
using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;
using Mcl.Core.Dotnetdetour.Utilities.Diagnostics;
using Mcl.Core.Network;
using Mcl.Core.Network.Interface;
// 确保包含 INetRequest 等接口

namespace Mcl.Core.Dotnetdetour.Features.Optimization
{
    // 1. 自定义请求处理器
    public class CustomRequestHandler : RequestHandler
    {
        protected override IResourceRequestHandler GetResourceRequestHandler(
            IWebBrowser chromiumWebBrowser, 
            IBrowser browser, 
            IFrame frame, 
            IRequest request, 
            bool isNavigation, 
            bool isDownload, 
            string requestInitiator, 
            ref bool disableDefaultHandling)
        {
            PluginLog.Info("Web", $"[Chrome Request] url: {request.Url}");
            
            // 返回我们的资源处理器，用于捕获【所有】网络请求
            // 如果要替换 JS，逻辑会包含在 CustomResourceRequestHandler 内部
            return new CustomResourceRequestHandler();
        }
    }

    // 2. 自定义资源请求处理器
    public class CustomResourceRequestHandler : ResourceRequestHandler
    {
        private static byte[] _cachedJsBytes = null;
        
        // 字典用于在请求开始 (OnBeforeResourceLoad) 和结束 (OnResourceLoadComplete) 之间传递 Entry
        private static readonly ConcurrentDictionary<ulong, NetworkCaptureEntry> CaptureEntries = 
            new ConcurrentDictionary<ulong, NetworkCaptureEntry>();

        // 处理自定义本地资源（保留你原本替换 JS 的逻辑）
        protected override IResourceHandler GetResourceHandler(
            IWebBrowser chromiumWebBrowser, 
            IBrowser browser, 
            IFrame frame, 
            IRequest request)
        {
            if (request.Url.Contains("31.c5774a7b.chunk.js"))
            {
                byte[] jsData = GetEmbeddedJsData();
                if (jsData != null)
                {
                    MemoryStream stream = new MemoryStream(jsData);
                    return ResourceHandler.FromStream(stream, mimeType: "application/javascript");
                }
            }
            // 2. 过滤 sa-multi-log 请求
            if (request.Url.Contains("sa-multi-log"))
            {
                // ==========================================
                // 处理 OPTIONS (CORS 预检请求)
                // ==========================================
                if (request.Method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
                {
                    var optionsHandler = new ResourceHandler
                    {
                        MimeType = "text/plain",
                        StatusCode = 204, // 204 No Content 是 OPTIONS 最标准的返回码
                        StatusText = "No Content",
                        Stream = new MemoryStream(), // OPTIONS 不需要返回 Body
                        AutoDisposeStream = true
                    };

                    // 必须告诉浏览器：允许跨域，允许 POST，并且允许前端携带自定义头
                    optionsHandler.Headers.Add("Access-Control-Allow-Origin", "https://x19.gsf.netease.com");
                    optionsHandler.Headers.Add("Access-Control-Allow-Methods", "POST, GET, OPTIONS");
                    optionsHandler.Headers.Add("Access-Control-Allow-Headers", "*"); 
                    optionsHandler.Headers.Add("Access-Control-Max-Age", "86400"); // 告诉浏览器缓存这个预检结果一天

                    return optionsHandler;
                }
                
                // ==========================================
                // 处理真正的 POST / GET 请求
                // ==========================================
                string jsonResponse = "{\"code\":0,\"message\":\"正常返回\",\"details\":\"\",\"entity\":null}";
                byte[] responseBytes = Encoding.UTF8.GetBytes(jsonResponse);

                var handler = new ResourceHandler
                {
                    MimeType = "application/json",
                    StatusCode = 200,
                    StatusText = "OK",
                    Stream = new MemoryStream(responseBytes),
                    AutoDisposeStream = true
                };

                // 你的自定义响应头
                handler.Headers.Add("server", "nginx");
                handler.Headers.Add("Access-Control-Allow-Origin", "https://x19.gsf.netease.com");

                return handler;
            }
            return base.GetResourceHandler(chromiumWebBrowser, browser, frame, request);
        }

        // --- 网络捕获生命周期：开始 ---
        protected override CefReturnValue OnBeforeResourceLoad(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IRequestCallback callback)
        {
            try
            {
                var netRequest = new CefNetRequestAdapter(request);
                var uri = new Uri(request.Url);
                
                // 获取 User-Agent
                string userAgent = request.Headers["User-Agent"] ?? "CefSharp Browser";

                var entry = NetworkCaptureStore.Begin(netRequest, uri, Encoding.UTF8, userAgent);
                CaptureEntries.TryAdd(request.Identifier, entry);
            }
            catch (Exception ex)
            {
                PluginLog.Error("Web", $"NetworkCapture Begin Error: {ex.Message}");
            }

            return base.OnBeforeResourceLoad(chromiumWebBrowser, browser, frame, request, callback);
        }

        // --- 网络捕获生命周期：结束 ---
        protected override void OnResourceLoadComplete(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IResponse response, UrlRequestStatus status, long receivedContentLength)
        {
            try
            {
                if (CaptureEntries.TryRemove(request.Identifier, out var entry))
                {
                    var netRequest = new CefNetRequestAdapter(request);
                    string userAgent = request.Headers["User-Agent"] ?? "CefSharp Browser";

                    if (status == UrlRequestStatus.Success)
                    {
                        var netResponse = new CefNetResponseAdapter(response, request.Url);
                        NetworkCaptureStore.Complete(entry, netRequest, netResponse, Encoding.UTF8, userAgent);
                    }
                    else
                    {
                        var exception = new Exception($"CefSharp Request Failed with status: {status}");
                        NetworkCaptureStore.Fail(entry, netRequest, exception, Encoding.UTF8, userAgent);
                    }
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error("Web", $"NetworkCapture Complete Error: {ex.Message}");
            }

            base.OnResourceLoadComplete(chromiumWebBrowser, browser, frame, request, response, status, receivedContentLength);
        }

        private byte[] GetEmbeddedJsData()
        {
            if (_cachedJsBytes != null) return _cachedJsBytes;

            var assembly = Assembly.GetExecutingAssembly();
            string resourceName = "Mcl.Core.Resources.31.c5774a7b.chunk.js"; 
            
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream != null)
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        stream.CopyTo(ms);
                        _cachedJsBytes = ms.ToArray();
                    }
                }
            }
            return _cachedJsBytes;
        }
    }

    // ========================================================================
    // 以下为适配器类 (Adapters)，用于将 CefSharp 数据映射到你定义的 INetRequest 接口
    // ========================================================================
    
    public class CefNetRequestAdapter : INetRequest
    {
        public CefNetRequestAdapter(IRequest cefRequest)
        {
            Parameters = new List<Parameter>();
            Files = new List<FileParameter>();
            AllowedDecompressionMethods = new List<DecompressionMethods>();

            // 1. 映射 Headers
            if (cefRequest.Headers != null)
            {
                foreach (string key in cefRequest.Headers)
                {
                    Parameters.Add(new Parameter 
                    { 
                        Name = key, 
                        Value = cefRequest.Headers[key], 
                        Type = ParameterType.HttpHeader 
                    });
                }
            }

            // 2. 映射 POST Data (RequestBody)
            if (cefRequest.PostData != null && cefRequest.PostData.Elements.Count > 0)
            {
                var element = cefRequest.PostData.Elements[0];
                if (element.Bytes != null)
                {
                    string bodyContent = Encoding.UTF8.GetString(element.Bytes);
                    Parameters.Add(new Parameter 
                    { 
                        Name = "application/json", 
                        Value = bodyContent, 
                        Type = ParameterType.RequestBody 
                    });
                }
            }

            // 3. 映射基础属性
            if (Enum.TryParse<Method>(cefRequest.Method, true, out var parsedMethod))
            {
                Method = parsedMethod;
            }
            else
            {
                Method = Method.GET;
            }
            
            Resource = string.Empty;
        }

        // --- NetworkCaptureStore 实际会用到的核心属性 ---
        public Method Method { get; set; }
        public string Resource { get; set; }
        public List<Parameter> Parameters { get; set; }

        // --- 接口要求的其他属性 (CaptureStore 不用，但必须实现以通过编译) ---
        public IList<DecompressionMethods> AllowedDecompressionMethods { get; set; }
        public bool AlwaysMultipartFormData { get; set; }
        public int Attempts { get; set; }
        public ICredentials Credentials { get; set; }
        public List<FileParameter> Files { get; set; }
        public int ReadWriteTimeout { get; set; }
        public Action<Stream> ResponseWriter { get; set; }
        public int Timeout { get; set; }
        public bool UseDefaultCredentials { get; set; }

        // --- 接口要求的扩展方法 (全部返回 this 即可) ---
        public void IncreaseNumAttempts() { Attempts++; }
        public INetRequest AddBody(object obj, string contentType) => this;
        public INetRequest AddBody(object obj) => this;
        public INetRequest AddCookie(string name, string value) => this;
        public INetRequest AddDecompressionMethod(DecompressionMethods method) => this;
        public INetRequest AddFile(string name, string path, string contentType) => this;
        public INetRequest AddFile(string name, byte[] bytes, string fileName, string contentType) => this;
        public INetRequest AddFile(string name, Action<Stream> writer, string fileName, long contentLength, string contentType) => this;
        public INetRequest AddFileBytes(string name, byte[] bytes, string filename, string contentType) => this;
        public INetRequest AddHeader(string name, string value) => this;
        public INetRequest AddJsonBody(object obj) => this;
        public INetRequest AddObject(object obj, params string[] includedProperties) => this;
        public INetRequest AddObject(object obj) => this;
        public INetRequest AddOrUpdateParameter(Parameter parameter) => this;
        public INetRequest AddOrUpdateParameter(string name, object value) => this;
        public INetRequest AddOrUpdateParameter(string name, object value, ParameterType type) => this;
        public INetRequest AddOrUpdateParameter(string name, object value, string contentType, ParameterType type) => this;
        public INetRequest AddParameter(Parameter parameter) => this;
        public INetRequest AddParameter(string name, object value) => this;
        public INetRequest AddParameter(string name, object value, ParameterType type) => this;
        public INetRequest AddParameter(string name, object value, string contentType, ParameterType type) => this;
        public INetRequest AddQueryParameter(string name, string value) => this;
        public INetRequest AddUrlSegment(string name, string value) => this;
        public INetRequest AddXmlBody(object obj) => this;
        public INetRequest AddXmlBody(object obj, string xmlNamespace) => this;
    }

    // ========================================================================
    // 修复后的响应适配器 (CefNetResponseAdapter)
    // ========================================================================
    public class CefNetResponseAdapter : INetResponse
    {
        public CefNetResponseAdapter(IResponse cefResponse, string requestUrl)
        {
            Headers = new List<Parameter>();
            Cookies = new List<NetResponseCookie>();
            
            // 映射响应头
            if (cefResponse.Headers != null)
            {
                foreach (string key in cefResponse.Headers)
                {
                    Headers.Add(new Parameter 
                    { 
                        Name = key, 
                        Value = cefResponse.Headers[key],
                        Type = ParameterType.HttpHeader
                    });
                }
            }

            ContentType = cefResponse.MimeType;
            StatusCode = (HttpStatusCode)cefResponse.StatusCode; // 类型强转为 HttpStatusCode
            StatusDescription = cefResponse.StatusText;
            ResponseUri = new Uri(requestUrl);
            ResponseStatus = ResponseStatus.Completed;
            IsSuccessful = cefResponse.StatusCode >= 200 && cefResponse.StatusCode <= 299;
            ErrorMessage = cefResponse.ErrorCode == CefErrorCode.None ? null : cefResponse.ErrorCode.ToString();
        }

        // --- NetworkCaptureStore 实际会用到的核心属性 ---
        public IList<Parameter> Headers { get; set; }
        public string ContentType { get; set; }
        public string Content { get; set; } // 网页内容默认无法直接从 CefSharp 拿，保持为 null
        public byte[] RawBytes { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public string StatusDescription { get; set; }
        public ResponseStatus ResponseStatus { get; set; }
        public Uri ResponseUri { get; set; }
        public string ErrorMessage { get; set; }
        public Exception ErrorException { get; set; }

        // --- 接口要求的其他属性 ---
        public string ContentEncoding { get; set; }
        public long ContentLength { get; set; }
        public IList<NetResponseCookie> Cookies { get; set; }
        public bool IsSuccessful { get; set; }
        public Version ProtocolVersion { get; set; }
        public INetRequest Request { get; set; }
        public string Server { get; set; }
    }

    /// <summary>
    /// 模拟你的 Parameter 对象
    /// </summary>
    public class CefNetParameter
    {
        public CefNetParameter(string name, object value, ParameterType type)
        {
            Name = name;
            Value = value;
            Type = type;
        }

        public string Name { get; set; }
        public object Value { get; set; }
        public ParameterType Type { get; set; }
    }

    /// <summary>
    /// 模拟 Header 对象
    /// </summary>
    public class CefNetHeader
    {
        public CefNetHeader(string name, object value)
        {
            Name = name;
            Value = value;
        }
        public string Name { get; set; }
        public object Value { get; set; }
    }

    // ========================================================================
    // Hook 保持不变
    // ========================================================================
    public class ChromeBrowserLoadHook : IMethodHook
    {
        [HookMethod("WPFLauncher.View.ChromeBrowser", "e", "Original")]
        public static void e(ChromiumWebBrowser instance, string bfw) 
        {
            if (instance.RequestHandler == null || !(instance.RequestHandler is CustomRequestHandler))
            {
                instance.RequestHandler = new CustomRequestHandler();
            }
            Original(instance, bfw);
        }

        [OriginalMethod]
        public static void Original(ChromiumWebBrowser instance, string bfw) 
        {
            return;
        }
    }
}