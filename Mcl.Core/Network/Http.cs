using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Mcl.Core.Extensions;
using Mcl.Core.Network.Interface;

namespace Mcl.Core.Network;

public class Http : IHttp, IHttpFactory
{
    private const string LINE_BREAK = "\r\n";

    private const string FORM_BOUNDARY = "-----------------------------28947758029299";

    private readonly IDictionary<string, Action<HttpWebRequest, string>> restrictedHeaderActions;

    private TimeOutState timeoutState;

    public Http()
    {
        Headers = new List<HttpHeader>();
        Files = new List<HttpFile>();
        Parameters = new List<HttpParameter>();
        Cookies = new List<HttpCookie>();
        restrictedHeaderActions =
            new Dictionary<string, Action<HttpWebRequest, string>>(StringComparer.OrdinalIgnoreCase);
        AddSharedHeaderActions();
        AddSyncHeaderActions();
    }

    protected bool HasParameters => Parameters.Any();

    protected bool HasCookies => Cookies.Any();

    protected bool HasBody => RequestBodyBytes != null || !string.IsNullOrEmpty(RequestBody);

    protected bool HasFiles => Files.Any();

    public HttpWebRequest DeleteAsync(Action<HttpResponse> action)
    {
        return GetStyleMethodInternalAsync("DELETE", action);
    }

    public HttpWebRequest GetAsync(Action<HttpResponse> action)
    {
        return GetStyleMethodInternalAsync("GET", action);
    }

    public HttpWebRequest HeadAsync(Action<HttpResponse> action)
    {
        return GetStyleMethodInternalAsync("HEAD", action);
    }

    public HttpWebRequest PostAsync(Action<HttpResponse> action)
    {
        return PutPostInternalAsync("POST", action);
    }

    public HttpWebRequest PutAsync(Action<HttpResponse> action)
    {
        return PutPostInternalAsync("PUT", action);
    }

    public HttpWebRequest PatchAsync(Action<HttpResponse> action)
    {
        return PutPostInternalAsync("PATCH", action);
    }

    public HttpWebRequest AsPostAsync(Action<HttpResponse> action, string httpMethod)
    {
        return PutPostInternalAsync(httpMethod.ToUpperInvariant(), action);
    }

    public HttpWebRequest AsGetAsync(Action<HttpResponse> action, string httpMethod)
    {
        return GetStyleMethodInternalAsync(httpMethod.ToUpperInvariant(), action);
    }

    public bool AlwaysMultipartFormData { get; set; }

    public string UserAgent { get; set; }

    public int Timeout { get; set; }

    public int ReadWriteTimeout { get; set; }

    public ICredentials Credentials { get; set; }

    public CookieContainer CookieContainer { get; set; }

    public Action<Stream> ResponseWriter { get; set; }

    public IList<HttpFile> Files { get; }

    public bool FollowRedirects { get; set; }

    public X509CertificateCollection ClientCertificates { get; set; }

    public int? MaxRedirects { get; set; }

    public bool UseDefaultCredentials { get; set; }

    public Encoding Encoding { get; set; } = Encoding.UTF8;

    public IList<HttpHeader> Headers { get; }

    public IList<HttpParameter> Parameters { get; }

    public IList<HttpCookie> Cookies { get; }

    public string RequestBody { get; set; }

    public string RequestContentType { get; set; }

    public byte[] RequestBodyBytes { get; set; }

    public Uri Url { get; set; }

    public bool PreAuthenticate { get; set; }

    public RequestCachePolicy CachePolicy { get; set; }

    public HttpResponse Delete()
    {
        return GetStyleMethodInternal("DELETE");
    }

    public HttpResponse Get()
    {
        return GetStyleMethodInternal("GET");
    }

    public HttpResponse Head()
    {
        return GetStyleMethodInternal("HEAD");
    }

    public HttpResponse Post()
    {
        return PostPutInternal("POST");
    }

    public HttpResponse Put()
    {
        return PostPutInternal("PUT");
    }

    public HttpResponse Patch()
    {
        return PostPutInternal("PATCH");
    }

    public HttpResponse AsGet(string httpMethod)
    {
        return GetStyleMethodInternal(httpMethod.ToUpperInvariant());
    }

    public HttpResponse AsPost(string httpMethod)
    {
        return PostPutInternal(httpMethod.ToUpperInvariant());
    }

    public IHttp Create()
    {
        return new Http();
    }

    protected virtual HttpWebRequest ConfigureAsyncWebRequest(string method, Uri url)
    {
        return ConfigureWebRequest(method, url);
    }

    private void SetTimeout(IAsyncResult asyncResult, TimeOutState timeOutState)
    {
        var flag = Timeout != 0;
        if (flag)
            ThreadPool.RegisterWaitForSingleObject(asyncResult.AsyncWaitHandle, TimeoutCallback, timeOutState, Timeout,
                true);
    }

    private static void TimeoutCallback(object state, bool timedOut)
    {
        var flag = !timedOut;
        if (!flag)
        {
            var timeOutState = state as TimeOutState;
            var flag2 = timeOutState == null;
            if (!flag2)
            {
                var timeOutState2 = timeOutState;
                lock (timeOutState2)
                {
                    timeOutState.TimedOut = true;
                }

                var request = timeOutState.Request;
                if (request != null) request.Abort();
            }
        }
    }

    private static void GetRawResponseAsync(IAsyncResult result, Action<HttpWebResponse> callback)
    {
        HttpWebResponse httpWebResponse = null;
        try
        {
            var httpWebRequest = (HttpWebRequest)result.AsyncState;
            httpWebResponse = httpWebRequest.EndGetResponse(result) as HttpWebResponse;
        }
        catch (WebException ex)
        {
            var flag = ex.Status == WebExceptionStatus.RequestCanceled;
            if (flag) throw;
            var flag2 = ex.Response is HttpWebResponse;
            if (!flag2) throw;
            httpWebResponse = ex.Response as HttpWebResponse;
        }

        callback(httpWebResponse);
        if (httpWebResponse != null) httpWebResponse.Close();
    }

    private HttpResponse CreateErrorResponse(Exception ex)
    {
        var httpResponse = new HttpResponse();
        var ex2 = ex as WebException;
        var flag = ex2 != null && ex2.Status == WebExceptionStatus.RequestCanceled;
        HttpResponse httpResponse2;
        if (flag)
        {
            httpResponse.ResponseStatus = timeoutState.TimedOut ? ResponseStatus.TimedOut : ResponseStatus.Aborted;
            httpResponse2 = httpResponse;
        }
        else
        {
            httpResponse.ErrorMessage = ex.Message;
            httpResponse.ErrorException = ex;
            httpResponse.ResponseStatus = ResponseStatus.Error;
            httpResponse2 = httpResponse;
        }

        return httpResponse2;
    }

    private void ResponseCallback(IAsyncResult result, Action<HttpResponse> callback)
    {
        var response = new HttpResponse
        {
            ResponseStatus = ResponseStatus.None
        };
        try
        {
            var timedOut = timeoutState.TimedOut;
            if (timedOut)
            {
                response.ResponseStatus = ResponseStatus.TimedOut;
                ExecuteCallback(response, callback);
            }
            else
            {
                GetRawResponseAsync(result, delegate(HttpWebResponse webResponse)
                {
                    try
                    {
                        ExtractResponseData(response, webResponse);
                        ExecuteCallback(response, callback);
                    }
                    catch
                    {
                    }
                });
            }
        }
        catch (Exception ex)
        {
            ExecuteCallback(CreateErrorResponse(ex), callback);
        }
    }

    private static void ExecuteCallback(HttpResponse response, Action<HttpResponse> callback)
    {
        PopulateErrorForIncompleteResponse(response);
        callback(response);
    }

    private static void PopulateErrorForIncompleteResponse(HttpResponse response)
    {
        var flag = response.ResponseStatus != ResponseStatus.Completed && response.ErrorException == null;
        if (flag)
        {
            response.ErrorException = response.ResponseStatus.ToWebException();
            response.ErrorMessage = response.ErrorException.Message;
        }
    }

    private long CalculateContentLength()
    {
        var flag = RequestBodyBytes != null;
        long num;
        if (flag)
        {
            num = RequestBodyBytes.Length;
        }
        else
        {
            var flag2 = !HasFiles && !AlwaysMultipartFormData;
            if (flag2)
            {
                num = Encoding.GetByteCount(RequestBody);
            }
            else
            {
                var num2 = 0L;
                foreach (var httpFile in Files)
                {
                    num2 += Encoding.GetByteCount(GetMultipartFileHeader(httpFile));
                    num2 += httpFile.ContentLength;
                    num2 += Encoding.GetByteCount("\r\n");
                }

                num2 = Parameters.Aggregate(num2,
                    (current, param) => current + Encoding.GetByteCount(GetMultipartFormData(param)));
                num2 += Encoding.GetByteCount(GetMultipartFooter());
                num = num2;
            }
        }

        return num;
    }

    private void RequestStreamCallback(IAsyncResult result, Action<HttpResponse> callback,
        HttpWebRequest httpWebRequest)
    {
        var httpWebRequest2 = (HttpWebRequest)result.AsyncState;
        var timedOut = timeoutState.TimedOut;
        if (timedOut)
        {
            var httpResponse = new HttpResponse
            {
                ResponseStatus = ResponseStatus.TimedOut
            };
            ExecuteCallback(httpResponse, callback);
        }
        else
        {
            try
            {
                using (var stream = httpWebRequest2.EndGetRequestStream(result))
                {
                    var flag = HasFiles || AlwaysMultipartFormData;
                    if (flag)
                    {
                        WriteMultipartFormData(stream);
                    }
                    else
                    {
                        var flag2 = RequestBodyBytes != null;
                        if (flag2)
                        {
                            stream.Write(RequestBodyBytes, 0, RequestBodyBytes.Length);
                        }
                        else
                        {
                            var flag3 = RequestBody != null;
                            if (flag3) WriteStringTo(stream, RequestBody);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var httpResponse2 = CreateErrorResponse(ex);
                httpResponse2.ProtocolVersion = httpWebRequest.ProtocolVersion;
                ExecuteCallback(httpResponse2, callback);
                return;
            }

            var asyncResult =
                httpWebRequest2.BeginGetResponse(delegate(IAsyncResult r) { ResponseCallback(r, callback); },
                    httpWebRequest2);
            SetTimeout(asyncResult, timeoutState);
        }
    }

    private void WriteRequestBodyAsync(HttpWebRequest webRequest, Action<HttpResponse> callback)
    {
        timeoutState = new TimeOutState
        {
            Request = webRequest
        };
        var flag = HasBody || HasFiles || AlwaysMultipartFormData;
        IAsyncResult asyncResult;
        if (flag)
        {
            webRequest.ContentLength = CalculateContentLength();
            asyncResult = webRequest.BeginGetRequestStream(
                delegate(IAsyncResult result) { RequestStreamCallback(result, callback, webRequest); }, webRequest);
        }
        else
        {
            asyncResult = webRequest.BeginGetResponse(delegate(IAsyncResult r) { ResponseCallback(r, callback); },
                webRequest);
        }

        SetTimeout(asyncResult, timeoutState);
    }

    private HttpWebRequest PutPostInternalAsync(string method, Action<HttpResponse> callback)
    {
        HttpWebRequest httpWebRequest = null;
        try
        {
            httpWebRequest = ConfigureAsyncWebRequest(method, Url);
            PreparePostBody(httpWebRequest);
            WriteRequestBodyAsync(httpWebRequest, callback);
        }
        catch (Exception ex)
        {
            ExecuteCallback(CreateErrorResponse(ex), callback);
        }

        return httpWebRequest;
    }

    private HttpWebRequest GetStyleMethodInternalAsync(string method, Action<HttpResponse> callback)
    {
        HttpWebRequest httpWebRequest = null;
        try
        {
            var url = Url;
            httpWebRequest = ConfigureAsyncWebRequest(method, url);
            var flag = HasBody && method == "DELETE";
            if (flag)
            {
                httpWebRequest.ContentType = RequestContentType;
                WriteRequestBodyAsync(httpWebRequest, callback);
            }
            else
            {
                timeoutState = new TimeOutState
                {
                    Request = httpWebRequest
                };
                var asyncResult =
                    httpWebRequest.BeginGetResponse(
                        delegate(IAsyncResult result) { ResponseCallback(result, callback); }, httpWebRequest);
                SetTimeout(asyncResult, timeoutState);
            }
        }
        catch (Exception ex)
        {
            ExecuteCallback(CreateErrorResponse(ex), callback);
        }

        return httpWebRequest;
    }

    protected virtual HttpWebRequest CreateWebRequest(Uri url)
    {
        return (HttpWebRequest)WebRequest.Create(url);
    }

    private void AddSyncHeaderActions()
    {
        restrictedHeaderActions.Add("Connection",
            delegate(HttpWebRequest r, string v) { r.KeepAlive = v.ToLower().Contains("keep-alive"); });
        restrictedHeaderActions.Add("Content-Length",
            delegate(HttpWebRequest r, string v) { r.ContentLength = Convert.ToInt64(v); });
        restrictedHeaderActions.Add("Expect", delegate(HttpWebRequest r, string v) { r.Expect = v; });
        restrictedHeaderActions.Add("If-Modified-Since",
            delegate(HttpWebRequest r, string v)
            {
                r.IfModifiedSince = Convert.ToDateTime(v, CultureInfo.InvariantCulture);
            });
        restrictedHeaderActions.Add("Referer", delegate(HttpWebRequest r, string v) { r.Referer = v; });
        restrictedHeaderActions.Add("Transfer-Encoding", delegate(HttpWebRequest r, string v)
        {
            r.TransferEncoding = v;
            r.SendChunked = true;
        });
        restrictedHeaderActions.Add("User-Agent", delegate(HttpWebRequest r, string v) { r.UserAgent = v; });
    }

    private void AddSharedHeaderActions()
    {
        restrictedHeaderActions.Add("Accept", delegate(HttpWebRequest r, string v) { r.Accept = v; });
        restrictedHeaderActions.Add("Content-Type", delegate(HttpWebRequest r, string v) { r.ContentType = v; });
        restrictedHeaderActions.Add("Date", delegate(HttpWebRequest r, string v)
        {
            DateTime dateTime;
            var flag = DateTime.TryParse(v, out dateTime);
            if (flag) r.Date = dateTime;
        });
        restrictedHeaderActions.Add("Host", delegate(HttpWebRequest r, string v) { r.Host = v; });
        restrictedHeaderActions.Add("Range", AddRange);
    }

    private static string GetMultipartFormContentType()
    {
        return string.Format("multipart/form-data; boundary={0}", "-----------------------------28947758029299");
    }

    private static string GetMultipartFileHeader(HttpFile file)
    {
        return string.Format(
            "--{0}{4}Content-Disposition: form-data; name=\"{1}\"; filename=\"{2}\"{4}Content-Type: {3}{4}{4}",
            "-----------------------------28947758029299", file.Name, file.FileName,
            file.ContentType ?? "application/octet-stream", "\r\n");
    }

    private string GetMultipartFormData(HttpParameter param)
    {
        var text = param.Name == RequestContentType
            ? "--{0}{3}Content-Type: {4}{3}Content-Disposition: form-data; name=\"{1}\"{3}{3}{2}{3}"
            : "--{0}{3}Content-Disposition: form-data; name=\"{1}\"{3}{3}{2}{3}";
        return string.Format(text, "-----------------------------28947758029299", param.Name, param.Value, "\r\n",
            param.ContentType);
    }

    private static string GetMultipartFooter()
    {
        return string.Format("--{0}--{1}", "-----------------------------28947758029299", "\r\n");
    }

    private void AppendHeaders(HttpWebRequest webRequest)
    {
        foreach (var httpHeader in Headers)
        {
            var flag = restrictedHeaderActions.ContainsKey(httpHeader.Name);
            if (flag)
                restrictedHeaderActions[httpHeader.Name](webRequest, httpHeader.Value);
            else
                webRequest.Headers.Add(httpHeader.Name, httpHeader.Value);
        }
    }

    private void AppendCookies(HttpWebRequest webRequest)
    {
        webRequest.CookieContainer = CookieContainer ?? new CookieContainer();
        foreach (var httpCookie in Cookies)
        {
            var cookie = new Cookie
            {
                Name = httpCookie.Name,
                Value = httpCookie.Value,
                Domain = webRequest.RequestUri.Host
            };
            webRequest.CookieContainer.Add(cookie);
        }
    }

    private string EncodeParameters()
    {
        var stringBuilder = new StringBuilder();
        foreach (var httpParameter in Parameters)
        {
            var flag = stringBuilder.Length > 1;
            if (flag) stringBuilder.Append("&");
            stringBuilder.AppendFormat("{0}={1}", httpParameter.Name.UrlEncode(), httpParameter.Value.UrlEncode());
        }

        return stringBuilder.ToString();
    }

    private void PreparePostBody(HttpWebRequest webRequest)
    {
        // 不要为GET请求准备POST请求体
        if (webRequest.Method == "GET")
            return;

        var flag = string.IsNullOrEmpty(webRequest.ContentType);
        var flag2 = HasFiles || AlwaysMultipartFormData;
        if (flag2)
        {
            var flag3 = flag;
            if (flag3) webRequest.ContentType = GetMultipartFormContentType();
        }
        else
        {
            var hasParameters = HasParameters;
            if (hasParameters)
            {
                var flag4 = flag;
                if (flag4) webRequest.ContentType = "application/x-www-form-urlencoded";
                RequestBody = EncodeParameters();
            }
            else
            {
                var hasBody = HasBody;
                if (hasBody)
                {
                    var flag5 = flag;
                    if (flag5) webRequest.ContentType = RequestContentType;
                }
            }
        }
    }

    private void WriteStringTo(Stream stream, string toWrite)
    {
        var bytes = Encoding.GetBytes(toWrite);
        stream.Write(bytes, 0, bytes.Length);
    }

    private void WriteMultipartFormData(Stream requestStream)
    {
        foreach (var httpParameter in Parameters) WriteStringTo(requestStream, GetMultipartFormData(httpParameter));
        foreach (var httpFile in Files)
        {
            WriteStringTo(requestStream, GetMultipartFileHeader(httpFile));
            httpFile.Writer(requestStream);
            WriteStringTo(requestStream, "\r\n");
        }

        WriteStringTo(requestStream, GetMultipartFooter());
    }

    private void ExtractResponseData(HttpResponse response, HttpWebResponse webResponse)
    {
        try
        {
            response.ContentEncoding = webResponse.ContentEncoding;
            response.Server = webResponse.Server;
            response.ContentType = webResponse.ContentType;
            response.ContentLength = webResponse.ContentLength;
            var responseStream = webResponse.GetResponseStream();
            ProcessResponseStream(responseStream, response);
            response.StatusCode = webResponse.StatusCode;
            response.StatusDescription = webResponse.StatusDescription;
            response.ResponseUri = webResponse.ResponseUri;
            response.ResponseStatus = ResponseStatus.Completed;
            var flag = webResponse.Cookies != null;
            if (flag)
                foreach (var obj in webResponse.Cookies)
                {
                    var cookie = (Cookie)obj;
                    response.Cookies.Add(new HttpCookie
                    {
                        Comment = cookie.Comment,
                        CommentUri = cookie.CommentUri,
                        Discard = cookie.Discard,
                        Domain = cookie.Domain,
                        Expired = cookie.Expired,
                        Expires = cookie.Expires,
                        HttpOnly = cookie.HttpOnly,
                        Name = cookie.Name,
                        Path = cookie.Path,
                        Port = cookie.Port,
                        Secure = cookie.Secure,
                        TimeStamp = cookie.TimeStamp,
                        Value = cookie.Value,
                        Version = cookie.Version
                    });
                }

            foreach (var text in webResponse.Headers.AllKeys)
            {
                var text2 = webResponse.Headers[text];
                response.Headers.Add(new HttpHeader
                {
                    Name = text,
                    Value = text2
                });
            }

            webResponse.Close();
        }
        finally
        {
            if (webResponse != null) ((IDisposable)webResponse).Dispose();
        }
    }

    private void ProcessResponseStream(Stream webResponseStream, HttpResponse response)
    {
        var flag = ResponseWriter == null;
        if (flag)
            response.RawBytes = webResponseStream.ReadAsBytes();
        else
            ResponseWriter(webResponseStream);
    }

    private static void AddRange(HttpWebRequest r, string range)
    {
        var match = Regex.Match(range, "(\\w+)=(\\d+)-(\\d+)$");
        var flag = !match.Success;
        if (!flag)
        {
            var value = match.Groups[1].Value;
            var num = Convert.ToInt64(match.Groups[2].Value);
            var num2 = Convert.ToInt64(match.Groups[3].Value);
            r.AddRange(value, num, num2);
        }
    }

    private HttpResponse GetStyleMethodInternal(string method)
    {
        var httpWebRequest = ConfigureWebRequest(method, Url);
        var flag = HasBody && method == "DELETE";
        if (flag)
        {
            httpWebRequest.ContentType = RequestContentType;
            WriteRequestBody(httpWebRequest);
        }

        return GetResponse(httpWebRequest);
    }

    private HttpResponse PostPutInternal(string method)
    {
        // 只有在POST, PUT, PATCH方法时才执行PreparePostBody和WriteRequestBody
        if (method != "POST" && method != "PUT" && method != "PATCH") return GetStyleMethodInternal(method);

        var httpWebRequest = ConfigureWebRequest(method, Url);
        PreparePostBody(httpWebRequest);
        WriteRequestBody(httpWebRequest);
        return GetResponse(httpWebRequest);
    }

    private static void ExtractErrorResponse(IHttpResponse httpResponse, Exception ex)
    {
        var ex2 = ex as WebException;
        var flag = ex2 != null && ex2.Status == WebExceptionStatus.Timeout;
        if (flag)
        {
            httpResponse.ResponseStatus = ResponseStatus.TimedOut;
            httpResponse.ErrorMessage = ex.Message;
            httpResponse.ErrorException = ex2;
        }
        else
        {
            httpResponse.ErrorMessage = ex.Message;
            httpResponse.ErrorException = ex;
            httpResponse.ResponseStatus = ResponseStatus.Error;
        }
    }

    private HttpResponse GetResponse(HttpWebRequest request)
    {
        var httpResponse = new HttpResponse
        {
            ResponseStatus = ResponseStatus.None
        };
        try
        {
            var rawResponse = GetRawResponse(request);
            ExtractResponseData(httpResponse, rawResponse);
        }
        catch (Exception ex)
        {
            ExtractErrorResponse(httpResponse, ex);
        }

        return httpResponse;
    }

    private static HttpWebResponse GetRawResponse(HttpWebRequest request)
    {
        HttpWebResponse httpWebResponse;
        try
        {
            httpWebResponse = (HttpWebResponse)request.GetResponse();
        }
        catch (WebException ex)
        {
            var httpWebResponse2 = ex.Response as HttpWebResponse;
            var flag = httpWebResponse2 != null;
            if (!flag) throw;
            httpWebResponse = httpWebResponse2;
        }

        return httpWebResponse;
    }

    private void WriteRequestBody(HttpWebRequest webRequest)
    {
        // GET请求不应该有请求体
        if (webRequest.Method == "GET")
            return;

        var flag = HasBody || HasFiles || AlwaysMultipartFormData;
        if (flag) webRequest.ContentLength = CalculateContentLength();
        using (var requestStream = webRequest.GetRequestStream())
        {
            var flag2 = HasFiles || AlwaysMultipartFormData;
            if (flag2)
            {
                WriteMultipartFormData(requestStream);
            }
            else
            {
                var flag3 = RequestBodyBytes != null;
                if (flag3)
                {
                    requestStream.Write(RequestBodyBytes, 0, RequestBodyBytes.Length);
                }
                else
                {
                    var flag4 = RequestBody != null;
                    if (flag4) WriteStringTo(requestStream, RequestBody);
                }
            }
        }
    }

    protected virtual HttpWebRequest ConfigureWebRequest(string method, Uri url)
    {
        var httpWebRequest = CreateWebRequest(url);
        httpWebRequest.Proxy = null;
        httpWebRequest.UseDefaultCredentials = UseDefaultCredentials;
        httpWebRequest.PreAuthenticate = PreAuthenticate;
        AppendHeaders(httpWebRequest);
        AppendCookies(httpWebRequest);
        httpWebRequest.Method = method;
        var flag = !HasFiles && !AlwaysMultipartFormData && method != "GET";
        if (flag) httpWebRequest.ContentLength = 0L;
        var flag2 = Credentials != null;
        if (flag2) httpWebRequest.Credentials = Credentials;
        var flag3 = UserAgent.HasValue();
        if (flag3) httpWebRequest.UserAgent = UserAgent;
        var flag4 = ClientCertificates != null;
        if (flag4) httpWebRequest.ClientCertificates.AddRange(ClientCertificates);
        httpWebRequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
        var flag5 = Timeout != 0;
        if (flag5) httpWebRequest.Timeout = Timeout;
        var flag6 = ReadWriteTimeout != 0;
        if (flag6) httpWebRequest.ReadWriteTimeout = ReadWriteTimeout;
        var flag7 = CachePolicy != null;
        if (flag7) httpWebRequest.CachePolicy = CachePolicy;
        httpWebRequest.AllowAutoRedirect = FollowRedirects;
        var flag8 = FollowRedirects && MaxRedirects != null;
        if (flag8) httpWebRequest.MaximumAutomaticRedirections = MaxRedirects.Value;
        return httpWebRequest;
    }
}