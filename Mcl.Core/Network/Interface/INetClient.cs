using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Cache;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mcl.Core.Network.Interface;

public interface INetClient
{
    CookieContainer CookieContainer { get; set; }

    int? MaxRedirects { get; set; }

    string UserAgent { get; set; }

    int Timeout { get; set; }

    int ReadWriteTimeout { get; set; }

    bool UseSynchronizationContext { get; set; }

    Uri BaseUrl { get; set; }

    Encoding Encoding { get; set; }

    bool PreAuthenticate { get; set; }

    IList<Parameter> DefaultParameters { get; }

    X509CertificateCollection ClientCertificates { get; set; }

    RequestCachePolicy CachePolicy { get; set; }

    bool FollowRedirects { get; set; }

    NetRequestAsyncHandle ExecuteAsync(INetRequest request, Action<INetResponse, NetRequestAsyncHandle> callback);

    INetResponse Execute(INetRequest request);

    byte[] DownloadData(INetRequest request);

    Uri BuildUri(INetRequest request);

    NetRequestAsyncHandle ExecuteAsyncGet(INetRequest request, Action<INetResponse, NetRequestAsyncHandle> callback,
        string httpMethod);

    NetRequestAsyncHandle ExecuteAsyncPost(INetRequest request, Action<INetResponse, NetRequestAsyncHandle> callback,
        string httpMethod);

    INetResponse ExecuteAsGet(INetRequest request, string httpMethod);

    INetResponse ExecuteAsPost(INetRequest request, string httpMethod);

    Task<INetResponse> ExecuteTaskAsync(INetRequest request, CancellationToken token);

    Task<INetResponse> ExecuteTaskAsync(INetRequest request);

    Task<INetResponse> ExecuteGetTaskAsync(INetRequest request);

    Task<INetResponse> ExecuteGetTaskAsync(INetRequest request, CancellationToken token);

    Task<INetResponse> ExecutePostTaskAsync(INetRequest request);

    Task<INetResponse> ExecutePostTaskAsync(INetRequest request, CancellationToken token);
}