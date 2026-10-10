using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using CefSharp;
using Mcl.Core.Dotnetdetour.Utilities.Diagnostics;

namespace Mcl.Core.Dotnetdetour.Utilities.Network;

/// <summary>Result of a request executed through the CefSharp browser network stack.</summary>
internal sealed class CefBrowserResponse
{
    public bool Failed { get; set; }
    public string ErrorMessage { get; set; }
    public int StatusCode { get; set; }
    public string StatusText { get; set; }
    public string MimeType { get; set; }
    public string Body { get; set; }

    /// <summary>Raw response bytes; use these for binary content such as images.</summary>
    public byte[] RawBytes { get; set; }

    public NameValueCollection Headers { get; set; }

    /// <summary>True when the transport succeeded and an HTTP response was received.</summary>
    public bool IsSuccessStatusCode => StatusCode >= 200 && StatusCode <= 299;

    public string Location => Headers?.Get("Location");
}

/// <summary>
/// Issues GET/POST requests through CefSharp's URL request API so they use the shared browser
/// request context (cookies, cache and Chromium network stack) instead of a managed HTTP client.
/// All calls share one session context — the host browser's <see cref="IRequestContext"/> when it
/// is known, otherwise the global one — so cookies set by one request are sent by the next and
/// the flow stays inside the same Chrome session.
/// </summary>
internal static class CefBrowserRequest
{
    private const int DefaultTimeoutMs = 30000;
    private static IRequestContext _sessionContext;

    /// <summary>
    /// Binds the shared session to the host browser's request context so these requests reuse the
    /// same cookies/cache as the embedded Chrome. Passing null keeps the global request context.
    /// </summary>
    public static void UseSession(IRequestContext context)
    {
        if (context == null) return;
        _sessionContext = context;
        PluginLog.Debug("Auth", $"Browser session bound to host request context (shared={context.IsGlobal})");
    }

    /// <summary>
    /// Reads the cookies the shared session currently holds for <paramref name="url"/> and returns
    /// them as a single "name=value; ..." header value. Used when a request goes through a managed
    /// HttpClient but must still carry the browser session's cookies.
    /// </summary>
    public static async Task<string> GetCookieHeaderAsync(string url, int timeoutMs = 5000)
    {
        var completion = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            var manager = GetCookieManager();
            if (manager == null)
            {
                completion.TrySetResult(string.Empty);
            }
            else if (!manager.VisitUrlCookies(url, true, new CookieHeaderCollector(completion)))
            {
                completion.TrySetResult(string.Empty);
            }
        }
        catch (Exception ex)
        {
            PluginLog.Warn("Auth", $"Browser cookie read failed: {ex.Message}");
            completion.TrySetResult(string.Empty);
        }

        var finished = await Task.WhenAny(completion.Task, Task.Delay(timeoutMs)).ConfigureAwait(false);
        return finished == completion.Task ? await completion.Task.ConfigureAwait(false) : string.Empty;
    }

    private static ICookieManager GetCookieManager()
    {
        var context = _sessionContext;
        if (context != null)
        {
            try { return context.GetCookieManager(null); }
            catch { /* fall back to the global cookie manager below */ }
        }
        return Cef.GetGlobalCookieManager(null);
    }

    public static Task<CefBrowserResponse> GetAsync(string url, IDictionary<string, string> headers = null,
        bool stopOnRedirect = true, int timeoutMs = DefaultTimeoutMs)
        => SendAsync(url, "GET", null, null, headers, stopOnRedirect, timeoutMs);

    public static Task<CefBrowserResponse> PostFormAsync(string url, IDictionary<string, string> form,
        IDictionary<string, string> headers = null, bool stopOnRedirect = true, int timeoutMs = DefaultTimeoutMs)
        => SendAsync(url, "POST", EncodeForm(form), "application/x-www-form-urlencoded", headers, stopOnRedirect, timeoutMs);

    private static string EncodeForm(IDictionary<string, string> form)
    {
        if (form == null) return string.Empty;
        var builder = new StringBuilder();
        foreach (var pair in form)
        {
            if (builder.Length > 0) builder.Append('&');
            builder.Append(HttpUtility.UrlEncode(pair.Key)).Append('=')
                .Append(HttpUtility.UrlEncode(pair.Value ?? string.Empty));
        }
        return builder.ToString();
    }

    private static async Task<CefBrowserResponse> SendAsync(string url, string method, string body, string contentType,
        IDictionary<string, string> headers, bool stopOnRedirect, int timeoutMs)
    {
        if (string.IsNullOrEmpty(url)) return Fail("request url is empty");

        if (!await WaitForCefAsync().ConfigureAwait(false))
            return Fail("CefSharp is not initialized");

        var completion = new TaskCompletionSource<CefBrowserResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        var client = new CefUrlRequestClient(completion);

        try
        {
            _ = Cef.UIThreadTaskFactory.StartNew(() =>
            {
                try
                {
                    var request = CefSharp.Request.Create();
                    request.Url = url;
                    request.Method = method;
                    request.Flags = UrlRequestFlags.AllowStoredCredentials;
                    if (stopOnRedirect) request.Flags |= UrlRequestFlags.StopOnRedirect;
                    CefBrowserProfile.ApplyBase(request);
                    if (headers != null)
                        foreach (var header in headers)
                            request.SetHeaderByName(header.Key, header.Value, true);

                    if (body != null)
                    {
                        request.InitializePostData();
                        var element = request.PostData.CreatePostDataElement();
                        element.Bytes = Encoding.UTF8.GetBytes(body);
                        request.PostData.AddElement(element);
                        if (!string.IsNullOrEmpty(contentType))
                            request.SetHeaderByName("Content-Type", contentType, true);
                    }

                    client.AttachRequest(CreateUrlRequest(request, client));
                    PluginLog.Debug("Auth", $"Browser {method} request started: {SafeUrl(url)}");
                }
                catch (Exception ex)
                {
                    completion.TrySetResult(Fail($"browser request creation failed: {ex.Message}"));
                }
            });
        }
        catch (Exception ex)
        {
            return Fail($"browser request dispatch failed: {ex.Message}");
        }

        var finished = await Task.WhenAny(completion.Task, Task.Delay(timeoutMs)).ConfigureAwait(false);
        if (finished != completion.Task)
        {
            client.Cancel();
            PluginLog.Warn("Auth", $"Browser {method} request timed out after {timeoutMs}ms: {SafeUrl(url)}");
            return Fail("request timed out");
        }

        var response = await completion.Task.ConfigureAwait(false);
        PluginLog.Debug("Auth",
            $"Browser {method} request finished: {SafeUrl(url)} status={response.StatusCode} failed={response.Failed} length={response.Body?.Length ?? 0}");
        return response;
    }

    /// <summary>
    /// Creates the URL request on the shared session context so every call reuses the same
    /// cookie jar/cache as the host browser, falling back to the global context if the captured
    /// context is gone (e.g. the browser was closed).
    /// </summary>
    private static IUrlRequest CreateUrlRequest(IRequest request, CefUrlRequestClient client)
    {
        var context = _sessionContext;
        if (context != null)
        {
            try { return new CefSharp.UrlRequest(request, client, context); }
            catch (Exception ex)
            {
                PluginLog.Warn("Auth", $"Browser session context unavailable, falling back to global: {ex.Message}");
            }
        }
        return new CefSharp.UrlRequest(request, client);
    }

    private static async Task<bool> WaitForCefAsync()
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            if (Cef.IsInitialized) return true;
            await Task.Delay(100).ConfigureAwait(false);
        }
        return Cef.IsInitialized;
    }

    private static string SafeUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return string.Empty;
        try { return new Uri(url).GetLeftPart(UriPartial.Path); }
        catch { return "<invalid-url>"; }
    }

    private static CefBrowserResponse Fail(string message)
        => new CefBrowserResponse { Failed = true, ErrorMessage = message, Body = string.Empty };

    /// <summary>Collects visited cookies into a single Cookie header; Dispose signals completion.</summary>
    private sealed class CookieHeaderCollector : ICookieVisitor
    {
        private readonly TaskCompletionSource<string> _completion;
        private readonly List<string> _cookies = new List<string>();

        public CookieHeaderCollector(TaskCompletionSource<string> completion) => _completion = completion;

        public bool Visit(Cookie cookie, int count, int total, ref bool deleteCookie)
        {
            if (cookie != null && !string.IsNullOrEmpty(cookie.Name))
                _cookies.Add(cookie.Name + "=" + cookie.Value);
            return true;
        }

        public void Dispose() => _completion.TrySetResult(string.Join("; ", _cookies));
    }

    private sealed class CefUrlRequestClient : IUrlRequestClient
    {
        private readonly TaskCompletionSource<CefBrowserResponse> _completion;
        private readonly MemoryStream _buffer = new MemoryStream();
        private IUrlRequest _request;

        public CefUrlRequestClient(TaskCompletionSource<CefBrowserResponse> completion)
        {
            _completion = completion;
        }

        public void AttachRequest(IUrlRequest request) => _request = request;

        public void Cancel()
        {
            var request = _request;
            if (request != null)
            {
                try { Cef.UIThreadTaskFactory.StartNew(() => { try { request.Dispose(); } catch { } }); }
                catch { }
            }
            _completion.TrySetResult(Fail("request canceled"));
        }

        public void OnDownloadData(IUrlRequest request, Stream data)
        {
            if (data == null) return;
            try { data.CopyTo(_buffer); }
            catch (Exception ex) { PluginLog.Debug("Auth", $"Browser download data copy failed: {ex.Message}"); }
        }

        public void OnDownloadProgress(IUrlRequest request, long current, long total) { }

        public void OnUploadProgress(IUrlRequest request, long current, long total) { }

        public bool GetAuthCredentials(bool isProxy, string host, int port, string realm, string scheme,
            IAuthCallback callback) => false;

        public void OnRequestComplete(IUrlRequest request)
        {
            var response = new CefBrowserResponse { Body = string.Empty };
            try
            {
                var cefResponse = request.Response;
                if (cefResponse != null)
                {
                    response.StatusCode = cefResponse.StatusCode;
                    response.StatusText = cefResponse.StatusText;
                    response.MimeType = cefResponse.MimeType;
                    response.Headers = cefResponse.Headers;

                    if (cefResponse.ErrorCode != CefErrorCode.None)
                        response.ErrorMessage = cefResponse.ErrorCode.ToString();
                }

                // A received HTTP response (including 3xx when StopOnRedirect is set) is a valid
                // result even if the transport status is not Success; only a missing response is a failure.
                response.Failed = cefResponse == null || response.StatusCode == 0;
                if (response.Failed && string.IsNullOrEmpty(response.ErrorMessage))
                    response.ErrorMessage = $"UrlRequestStatus={request.RequestStatus}";

                if (_buffer.Length > 0)
                {
                    response.RawBytes = _buffer.ToArray();
                    response.Body = Encoding.UTF8.GetString(response.RawBytes);
                }
            }
            catch (Exception ex)
            {
                response.Failed = true;
                response.ErrorMessage = ex.Message;
            }
            finally
            {
                try { request.Dispose(); } catch { }
                _request = null;
                _completion.TrySetResult(response);
            }
        }
    }
}

/// <summary>
/// A consistent Windows/Chrome request profile. Requests already run on Chromium's network
/// stack (real TLS/JA3, HTTP/2 and header ordering); this fills in the HTTP headers a real
/// Chrome sends so the request does not look like a bare programmatic client.
/// The user agent and the client hints intentionally describe the same Chrome major version.
/// </summary>
internal static class CefBrowserProfile
{
    private const string ChromeMajor = "129";
    private const string ChromeFullVersion = "129.0.0.0";

    public const string UserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/"
        + ChromeFullVersion + " Safari/537.36";

    private const string ClientHints = "\"Google Chrome\";v=\"" + ChromeMajor + "\", \"Chromium\";v=\""
        + ChromeMajor + "\", \"Not=A?Brand\";v=\"24\"";

    private const string AcceptLanguage = "zh-CN,zh;q=0.9,en;q=0.8";

    /// <summary>Headers every request shares (sent on both navigation and XHR).</summary>
    public static IDictionary<string, string> BaseHeaders() => new Dictionary<string, string>
    {
        ["User-Agent"] = UserAgent,
        ["Accept-Language"] = AcceptLanguage,
        ["sec-ch-ua"] = ClientHints,
        ["sec-ch-ua-mobile"] = "?0",
        ["sec-ch-ua-platform"] = "\"Windows\""
    };

    /// <summary>Applies the shared headers to a browser URL request.</summary>
    public static void ApplyBase(IRequest request)
    {
        foreach (var header in BaseHeaders())
            request.SetHeaderByName(header.Key, header.Value, true);
    }

    /// <summary>Headers for a top-level document navigation.</summary>
    public static IDictionary<string, string> Navigation(string referer = null)
    {
        var headers = new Dictionary<string, string>
        {
            ["Accept"] = "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7",
            ["Upgrade-Insecure-Requests"] = "1",
            ["Sec-Fetch-Dest"] = "document",
            ["Sec-Fetch-Mode"] = "navigate",
            ["Sec-Fetch-Site"] = string.IsNullOrEmpty(referer) ? "none" : "cross-site",
            ["Sec-Fetch-User"] = "?1"
        };
        if (!string.IsNullOrEmpty(referer)) headers["Referer"] = referer;
        return headers;
    }

    /// <summary>Headers for an image subresource (e.g. a captcha image loaded by &lt;img&gt;).</summary>
    public static IDictionary<string, string> Image(string referer = null, string origin = null)
    {
        var headers = new Dictionary<string, string>
        {
            ["Accept"] = "image/avif,image/webp,image/apng,image/svg+xml,image/*,*/*;q=0.8",
            ["Sec-Fetch-Dest"] = "image",
            ["Sec-Fetch-Mode"] = "no-cors",
            ["Sec-Fetch-Site"] = string.IsNullOrEmpty(origin) ? "same-origin" : "cross-site"
        };
        if (!string.IsNullOrEmpty(origin)) headers["Origin"] = origin;
        if (!string.IsNullOrEmpty(referer)) headers["Referer"] = referer;
        return headers;
    }

    /// <summary>Headers for a same-origin script (fetch/XHR) request such as a form submit.</summary>
    public static IDictionary<string, string> Xhr(string origin, string referer = null)
    {
        var headers = new Dictionary<string, string>
        {
            ["Accept"] = "application/json, text/plain, */*",
            ["Sec-Fetch-Dest"] = "empty",
            ["Sec-Fetch-Mode"] = "cors",
            ["Sec-Fetch-Site"] = "same-origin"
        };
        if (!string.IsNullOrEmpty(origin)) headers["Origin"] = origin;
        if (!string.IsNullOrEmpty(referer)) headers["Referer"] = referer;
        return headers;
    }

    /// <summary>
    /// The complete set of browser headers for a same-origin XHR form submit (the login POST).
    /// Includes the shared base headers so a managed HttpClient can mirror the browser exactly.
    /// Content-Type/Content-Length/Accept-Encoding are intentionally left to HttpClient.
    /// </summary>
    public static IDictionary<string, string> LoginHeaders(string origin, string referer)
    {
        var headers = new Dictionary<string, string>(BaseHeaders(), StringComparer.OrdinalIgnoreCase);
        foreach (var header in Xhr(origin, referer)) headers[header.Key] = header.Value;
        return headers;
    }
}
