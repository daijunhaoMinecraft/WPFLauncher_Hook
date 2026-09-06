using System;
using System.Collections.Generic;
using System.Net;
using Mcl.Core.Extensions;
using Mcl.Core.Network.Interface;

namespace Mcl.Core.Network;

public abstract class NetResponseBase
{
    private string content;

    protected NetResponseBase()
    {
        ResponseStatus = ResponseStatus.None;
        Headers = new List<Parameter>();
        Cookies = new List<NetResponseCookie>();
    }

    public INetRequest Request { get; set; }

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
        set => content = value;
    }

    public HttpStatusCode StatusCode { get; set; }

    public bool IsSuccessful => StatusCode >= HttpStatusCode.OK && StatusCode <= (HttpStatusCode)299 &&
                                ResponseStatus == ResponseStatus.Completed;

    public string StatusDescription { get; set; }

    public byte[] RawBytes { get; set; }

    public Uri ResponseUri { get; set; }

    public string Server { get; set; }

    public IList<NetResponseCookie> Cookies { get; protected internal set; }

    public IList<Parameter> Headers { get; protected internal set; }

    public ResponseStatus ResponseStatus { get; set; }

    public string ErrorMessage { get; set; }

    public Exception ErrorException { get; set; }

    public Version ProtocolVersion { get; set; }

    protected string DebuggerDisplay()
    {
        return string.Format("StatusCode: {0}, Content-Type: {1}, Content-Length: {2})", StatusCode, ContentType,
            ContentLength);
    }
}