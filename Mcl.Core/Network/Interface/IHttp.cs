using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Mcl.Core.Network.Interface;

public interface IHttp
{
    Action<Stream> ResponseWriter { get; set; }

    CookieContainer CookieContainer { get; set; }

    ICredentials Credentials { get; set; }

    bool AlwaysMultipartFormData { get; set; }

    string UserAgent { get; set; }

    int Timeout { get; set; }

    int ReadWriteTimeout { get; set; }

    bool FollowRedirects { get; set; }

    X509CertificateCollection ClientCertificates { get; set; }

    int? MaxRedirects { get; set; }

    bool UseDefaultCredentials { get; set; }

    Encoding Encoding { get; set; }

    IList<HttpHeader> Headers { get; }

    IList<HttpParameter> Parameters { get; }

    IList<HttpFile> Files { get; }

    IList<HttpCookie> Cookies { get; }

    string RequestBody { get; set; }

    string RequestContentType { get; set; }

    bool PreAuthenticate { get; set; }

    RequestCachePolicy CachePolicy { get; set; }

    byte[] RequestBodyBytes { get; set; }

    Uri Url { get; set; }

    HttpWebRequest DeleteAsync(Action<HttpResponse> action);

    HttpWebRequest GetAsync(Action<HttpResponse> action);

    HttpWebRequest HeadAsync(Action<HttpResponse> action);

    HttpWebRequest PostAsync(Action<HttpResponse> action);

    HttpWebRequest PutAsync(Action<HttpResponse> action);

    HttpWebRequest PatchAsync(Action<HttpResponse> action);

    HttpWebRequest AsPostAsync(Action<HttpResponse> action, string httpMethod);

    HttpWebRequest AsGetAsync(Action<HttpResponse> action, string httpMethod);

    HttpResponse Delete();

    HttpResponse Get();

    HttpResponse Head();

    HttpResponse Post();

    HttpResponse Put();

    HttpResponse Patch();

    HttpResponse AsPost(string httpMethod);

    HttpResponse AsGet(string httpMethod);
}