using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Mcl.Core.Network.Interface;

namespace Mcl.Core.Network;

public class NetRequest : INetRequest
{
    public static int DETAULT_TIMEOUT = int.MaxValue;

    private readonly IList<DecompressionMethods> alloweDecompressionMethods;

    public NetRequest()
    {
        RequestFormat = DataFormat.Json;
        Method = Method.GET;
        Parameters = new List<Parameter>();
        Files = new List<FileParameter>();
        alloweDecompressionMethods = new List<DecompressionMethods>();
        Timeout = DETAULT_TIMEOUT;
    }

    public NetRequest(Method method)
        : this()
    {
        Method = method;
    }

    public NetRequest(string resource)
        : this(resource, Method.GET)
    {
    }

    public NetRequest(string resource, Method method)
        : this()
    {
        Resource = resource;
        Method = method;
    }

    public NetRequest(Uri resource)
        : this(resource, Method.GET)
    {
    }

    public NetRequest(Uri resource, Method method)
        : this(resource.IsAbsoluteUri ? resource.AbsolutePath + resource.Query : resource.OriginalString, method)
    {
    }

    public object UserState { get; set; }

    public DataFormat RequestFormat { get; set; }

    public IList<DecompressionMethods> AllowedDecompressionMethods
    {
        get
        {
            IList<DecompressionMethods> list2;
            if (!alloweDecompressionMethods.Any())
            {
                IList<DecompressionMethods> list = new[]
                {
                    DecompressionMethods.None,
                    DecompressionMethods.Deflate,
                    DecompressionMethods.GZip
                };
                list2 = list;
            }
            else
            {
                list2 = alloweDecompressionMethods;
            }

            return list2;
        }
    }

    public bool AlwaysMultipartFormData { get; set; }

    public Action<Stream> ResponseWriter { get; set; }

    public bool UseDefaultCredentials { get; set; }

    public INetRequest AddFile(string name, string path, string contentType = null)
    {
        var fileInfo = new FileInfo(path);
        var length = fileInfo.Length;
        return AddFile(new FileParameter
        {
            Name = name,
            FileName = Path.GetFileName(path),
            ContentLength = length,
            Writer = delegate(Stream s)
            {
                using (var streamReader = new StreamReader(new FileStream(path, FileMode.Open)))
                {
                    streamReader.BaseStream.CopyTo(s);
                }
            },
            ContentType = contentType
        });
    }

    public INetRequest AddFile(string name, byte[] bytes, string fileName, string contentType = null)
    {
        return AddFile(FileParameter.Create(name, bytes, fileName, contentType));
    }

    public INetRequest AddFile(string name, Action<Stream> writer, string fileName, long contentLength,
        string contentType = null)
    {
        return AddFile(new FileParameter
        {
            Name = name,
            Writer = writer,
            FileName = fileName,
            ContentLength = contentLength,
            ContentType = contentType
        });
    }

    public INetRequest AddFileBytes(string name, byte[] bytes, string filename,
        string contentType = "application/x-gzip")
    {
        var num = (long)bytes.Length;
        return AddFile(new FileParameter
        {
            Name = name,
            FileName = filename,
            ContentLength = num,
            ContentType = contentType,
            Writer = delegate(Stream s)
            {
                using (var streamReader = new StreamReader(new MemoryStream(bytes)))
                {
                    streamReader.BaseStream.CopyTo(s);
                }
            }
        });
    }

    public INetRequest AddBody(object obj, string contentType)
    {
        return AddParameter(contentType, obj, ParameterType.RequestBody);
    }

    public INetRequest AddBody(object obj)
    {
        return AddBody(obj, "");
    }

    public INetRequest AddJsonBody(object obj)
    {
        RequestFormat = DataFormat.Json;
        return AddBody(obj, "");
    }

    public INetRequest AddXmlBody(object obj)
    {
        RequestFormat = DataFormat.Xml;
        return AddBody(obj, "");
    }

    public INetRequest AddXmlBody(object obj, string xmlNamespace)
    {
        RequestFormat = DataFormat.Xml;
        return AddBody(obj, xmlNamespace);
    }

    public INetRequest AddObject(object obj, params string[] includedProperties)
    {
        var type = obj.GetType();
        var properties = type.GetProperties();
        foreach (var propertyInfo in properties)
        {
            var flag = includedProperties.Length == 0 ||
                       (includedProperties.Length != 0 && includedProperties.Contains(propertyInfo.Name));
            var flag2 = !flag;
            if (!flag2)
            {
                var propertyType = propertyInfo.PropertyType;
                var obj2 = propertyInfo.GetValue(obj, null);
                var flag3 = obj2 == null;
                if (!flag3)
                {
                    var isArray = propertyType.IsArray;
                    if (isArray)
                    {
                        var elementType = propertyType.GetElementType();
                        var flag4 = ((Array)obj2).Length > 0 && elementType != null && (elementType.IsPrimitive ||
                            elementType.IsValueType || elementType == typeof(string));
                        if (flag4)
                        {
                            var array2 = (from object item in (Array)obj2
                                select item.ToString()).ToArray();
                            obj2 = string.Join(",", array2);
                        }
                        else
                        {
                            obj2 = string.Join(",", (string[])obj2);
                        }
                    }

                    AddParameter(propertyInfo.Name, obj2);
                }
            }
        }

        return this;
    }

    public INetRequest AddObject(object obj)
    {
        AddObject(obj, new string[0]);
        return this;
    }

    public INetRequest AddParameter(Parameter p)
    {
        Parameters.Add(p);
        return this;
    }

    public INetRequest AddParameter(string name, object value)
    {
        return AddParameter(new Parameter
        {
            Name = name,
            Value = value,
            Type = ParameterType.GetOrPost
        });
    }

    public INetRequest AddParameter(string name, object value, ParameterType type)
    {
        return AddParameter(new Parameter
        {
            Name = name,
            Value = value,
            Type = type
        });
    }

    public INetRequest AddParameter(string name, object value, string contentType, ParameterType type)
    {
        return AddParameter(new Parameter
        {
            Name = name,
            Value = value,
            ContentType = contentType,
            Type = type
        });
    }

    public INetRequest AddOrUpdateParameter(Parameter p)
    {
        var flag = Parameters.Any(param => param.Name == p.Name);
        INetRequest netRequest;
        if (flag)
        {
            var parameter = Parameters.First(param => param.Name == p.Name);
            parameter.Value = p.Value;
            netRequest = this;
        }
        else
        {
            Parameters.Add(p);
            netRequest = this;
        }

        return netRequest;
    }

    public INetRequest AddOrUpdateParameter(string name, object value)
    {
        return AddOrUpdateParameter(new Parameter
        {
            Name = name,
            Value = value,
            Type = ParameterType.GetOrPost
        });
    }

    public INetRequest AddOrUpdateParameter(string name, object value, ParameterType type)
    {
        return AddOrUpdateParameter(new Parameter
        {
            Name = name,
            Value = value,
            Type = type
        });
    }

    public INetRequest AddOrUpdateParameter(string name, object value, string contentType, ParameterType type)
    {
        return AddOrUpdateParameter(new Parameter
        {
            Name = name,
            Value = value,
            ContentType = contentType,
            Type = type
        });
    }

    public INetRequest AddHeader(string name, string value)
    {
        var func = (string host) => Uri.CheckHostName(Regex.Split(host, ":\\d+")[0]) == UriHostNameType.Unknown;
        var flag = name == "Host" && func(value);
        if (flag) throw new ArgumentException("The specified value is not a valid Host header string.", "value");
        return AddParameter(name, value, ParameterType.HttpHeader);
    }

    public INetRequest AddCookie(string name, string value)
    {
        return AddParameter(name, value, ParameterType.Cookie);
    }

    public INetRequest AddUrlSegment(string name, string value)
    {
        return AddParameter(name, value, ParameterType.UrlSegment);
    }

    public INetRequest AddQueryParameter(string name, string value)
    {
        return AddParameter(name, value, ParameterType.QueryString);
    }

    public INetRequest AddDecompressionMethod(DecompressionMethods decompressionMethod)
    {
        var flag = !alloweDecompressionMethods.Contains(decompressionMethod);
        if (flag) alloweDecompressionMethods.Add(decompressionMethod);
        return this;
    }

    public List<Parameter> Parameters { get; }

    public List<FileParameter> Files { get; }

    public Method Method { get; set; }

    public string Resource { get; set; }

    public ICredentials Credentials { get; set; }

    public int Timeout { get; set; }

    public int ReadWriteTimeout { get; set; }

    public void IncreaseNumAttempts()
    {
        var attempts = Attempts;
        Attempts = attempts + 1;
    }

    public int Attempts { get; private set; }

    private INetRequest AddFile(FileParameter file)
    {
        Files.Add(file);
        return this;
    }

    public INetRequest AddUrlSegment(string name, object value)
    {
        return AddParameter(name, value, ParameterType.UrlSegment);
    }
}