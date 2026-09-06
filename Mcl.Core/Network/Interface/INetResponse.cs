using System;
using System.Collections.Generic;
using System.Net;

namespace Mcl.Core.Network.Interface;

public interface INetResponse
{
    Version ProtocolVersion { get; set; }

    INetRequest Request { get; set; }

    string ContentType { get; set; }

    long ContentLength { get; set; }

    string ContentEncoding { get; set; }

    string Content { get; set; }

    HttpStatusCode StatusCode { get; set; }

    bool IsSuccessful { get; }

    string StatusDescription { get; set; }

    byte[] RawBytes { get; set; }

    Uri ResponseUri { get; set; }

    string Server { get; set; }

    IList<NetResponseCookie> Cookies { get; }

    IList<Parameter> Headers { get; }

    ResponseStatus ResponseStatus { get; set; }

    string ErrorMessage { get; set; }

    Exception ErrorException { get; set; }
}