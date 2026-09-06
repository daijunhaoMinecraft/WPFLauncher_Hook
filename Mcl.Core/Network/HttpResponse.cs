using System;
using System.Collections.Generic;
using System.Net;
using Mcl.Core.Extensions;
using Mcl.Core.Network.Interface;

namespace Mcl.Core.Network;

public class HttpResponse : IHttpResponse
{
    private string content;

    public HttpResponse()
    {
        ResponseStatus = ResponseStatus.None;
        Headers = new List<HttpHeader>();
        Cookies = new List<HttpCookie>();
    }

    public Version ProtocolVersion { get; set; }

    public string ContentType { get; set; }

    public long ContentLength { get; set; }

    public string ContentEncoding { get; set; }

    public string Content
    {
        get
        {
            string text;
            if ((text = content) == null) text = content = RawBytes.AsString();
            return text;
        }
    }

    public HttpStatusCode StatusCode { get; set; }

    public string StatusDescription { get; set; }

    public byte[] RawBytes { get; set; }

    public Uri ResponseUri { get; set; }

    public string Server { get; set; }

    public IList<HttpHeader> Headers { get; }

    public IList<HttpCookie> Cookies { get; }

    public ResponseStatus ResponseStatus { get; set; }

    public string ErrorMessage { get; set; }

    public Exception ErrorException { get; set; }
}