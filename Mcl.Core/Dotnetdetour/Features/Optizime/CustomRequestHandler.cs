using System;
using CefSharp;
using CefSharp.Handler;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using CefSharp.Wpf;
using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;
using Mcl.Core.Dotnetdetour.Models.Config;
using WPFLauncher.View;

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
        WpfConfig.DefaultLogger.Info($"[Chrome Request] url: {request.Url}");
        // 检查请求的 URL 是否包含你要替换的文件名
        if (request.Url.Contains("31.c5774a7b.chunk.js"))
        {
            // 如果是，则返回我们自定义的资源请求处理器
            return new CustomResourceRequestHandler();
        }

        // 其他请求走默认处理（正常加载网络资源）
        return base.GetResourceRequestHandler(chromiumWebBrowser, browser, frame, request, isNavigation, isDownload, requestInitiator, ref disableDefaultHandling);
    }
}

// 2. 自定义资源请求处理器
public class CustomResourceRequestHandler : ResourceRequestHandler
{
    // 缓存文件的字节数组，避免每次请求都去解析程序集，提高性能
    private static byte[] _cachedJsBytes = null;

    protected override IResourceHandler GetResourceHandler(
        IWebBrowser chromiumWebBrowser, 
        IBrowser browser, 
        IFrame frame, 
        IRequest request)
    {
        byte[] jsData = GetEmbeddedJsData();

        if (jsData != null)
        {
            // 将字节数组转为 MemoryStream 交给 CefSharp
            // 注意：每次请求都需要 new 一个新的 MemoryStream
            MemoryStream stream = new MemoryStream(jsData);
            
            // 使用 FromStream 替代 FromFilePath
            return ResourceHandler.FromStream(stream, mimeType: "application/javascript");
        }

        return base.GetResourceHandler(chromiumWebBrowser, browser, frame, request);
    }

    /// <summary>
    /// 从 DLL 中读取嵌入的资源
    /// </summary>
    private byte[] GetEmbeddedJsData()
    {
        // 如果已经缓存过，直接返回
        if (_cachedJsBytes != null) return _cachedJsBytes;

        // 获取当前运行的 DLL 程序集
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

public class ChromeBrowserLoadHook : IMethodHook
{
    // 目标：WPFLauncher.View.ChromeBrowser 类的 e 方法
    [HookMethod("WPFLauncher.View.ChromeBrowser", "e", "Original")]
    public static void e(ChromiumWebBrowser instance, string bfw) // 🌟 修复关键：加上 static！
    {
        // 此时 instance 就是目标浏览器的实例，安全且不会崩溃
        if (instance.RequestHandler == null || !(instance.RequestHandler is CustomRequestHandler))
        {
            instance.RequestHandler = new CustomRequestHandler();
        }

        // 调用原程序的 e 方法
        Original(instance, bfw);
    }

    [OriginalMethod]
    public static void Original(ChromiumWebBrowser instance, string bfw) // 🌟 这里也必须是 static！
    {
        // 占位符，不会执行
        return;
    }
}
