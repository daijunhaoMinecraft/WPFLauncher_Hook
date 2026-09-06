using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace Mcl.Core.Network.Interface;

public interface INetRequest
{
    bool AlwaysMultipartFormData { get; set; }

    Action<Stream> ResponseWriter { get; set; }

    List<Parameter> Parameters { get; }

    List<FileParameter> Files { get; }

    Method Method { get; set; }

    string Resource { get; set; }

    ICredentials Credentials { get; set; }

    int Timeout { get; set; }

    int ReadWriteTimeout { get; set; }

    int Attempts { get; }

    bool UseDefaultCredentials { get; set; }

    IList<DecompressionMethods> AllowedDecompressionMethods { get; }

    INetRequest AddFile(string name, string path, string contentType = null);

    INetRequest AddFile(string name, byte[] bytes, string fileName, string contentType = null);

    INetRequest AddFile(string name, Action<Stream> writer, string fileName, long contentLength,
        string contentType = null);

    INetRequest AddFileBytes(string name, byte[] bytes, string filename, string contentType = "application/x-gzip");

    INetRequest AddBody(object obj, string xmlNamespace);

    INetRequest AddBody(object obj);

    INetRequest AddJsonBody(object obj);

    INetRequest AddXmlBody(object obj);

    INetRequest AddXmlBody(object obj, string xmlNamespace);

    INetRequest AddObject(object obj, params string[] includedProperties);

    INetRequest AddObject(object obj);

    INetRequest AddParameter(Parameter p);

    INetRequest AddParameter(string name, object value);

    INetRequest AddParameter(string name, object value, ParameterType type);

    INetRequest AddParameter(string name, object value, string contentType, ParameterType type);

    INetRequest AddOrUpdateParameter(Parameter p);

    INetRequest AddOrUpdateParameter(string name, object value);

    INetRequest AddOrUpdateParameter(string name, object value, ParameterType type);

    INetRequest AddOrUpdateParameter(string name, object value, string contentType, ParameterType type);

    INetRequest AddHeader(string name, string value);

    INetRequest AddCookie(string name, string value);

    INetRequest AddUrlSegment(string name, string value);

    INetRequest AddQueryParameter(string name, string value);

    INetRequest AddDecompressionMethod(DecompressionMethods decompressionMethod);

    void IncreaseNumAttempts();
}