using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Mcl.Core.Extensions;
using Mcl.Core.Network.Interface;

namespace Mcl.Core.Network;

public static class NetworkCaptureStore
{
    private const int MaxEntries = 200;
    private const int MaxBodyCharacters = 256 * 1024;
    private static readonly object SyncRoot = new object();
    private static readonly LinkedList<NetworkCaptureEntry> Entries = new LinkedList<NetworkCaptureEntry>();
    private static long _nextId;

    public static NetworkCaptureEntry Begin(INetRequest request, Uri baseUrl, Encoding encoding, string userAgent)
    {
        var entry = new NetworkCaptureEntry
        {
            Id = Interlocked.Increment(ref _nextId),
            StartedAt = DateTimeOffset.Now,
            Method = request.Method.ToString(),
            Url = BuildUrl(baseUrl, request),
            State = "Pending"
        };
        UpdateRequest(entry, request, encoding, userAgent);

        lock (SyncRoot)
        {
            Entries.AddLast(entry);
            while (Entries.Count > MaxEntries) Entries.RemoveFirst();
        }

        return entry;
    }

    public static void Complete(NetworkCaptureEntry entry, INetRequest request, INetResponse response,
        Encoding encoding, string userAgent, string state = "Completed")
    {
        if (entry == null) return;

        lock (SyncRoot)
        {
            if (entry.IsCompleted) return;
            UpdateRequest(entry, request, encoding, userAgent);
            entry.State = state;
            entry.DurationMs = Math.Max(0, (long)(DateTimeOffset.Now - entry.StartedAt).TotalMilliseconds);
            entry.Response = response == null ? null : CreateResponse(response, encoding);
            entry.IsCompleted = true;
        }
    }

    public static void Fail(NetworkCaptureEntry entry, INetRequest request, Exception exception,
        Encoding encoding, string userAgent)
    {
        if (entry == null) return;
        lock (SyncRoot)
        {
            if (entry.IsCompleted) return;
            UpdateRequest(entry, request, encoding, userAgent);
            entry.State = "Error";
            entry.DurationMs = Math.Max(0, (long)(DateTimeOffset.Now - entry.StartedAt).TotalMilliseconds);
            entry.Error = exception?.Message;
            entry.IsCompleted = true;
        }
    }

    public static IList<NetworkCaptureEntry> GetSnapshot()
    {
        lock (SyncRoot) return Entries.Reverse().Select(Clone).ToList();
    }

    public static void Clear()
    {
        lock (SyncRoot) Entries.Clear();
    }

    private static void UpdateRequest(NetworkCaptureEntry entry, INetRequest request, Encoding encoding,
        string userAgent)
    {
        if (request == null) return;
        entry.Method = request.Method.ToString();
        entry.RequestHeaders = request.Parameters
            .Where(parameter => parameter.Type == ParameterType.HttpHeader)
            .Select(parameter => new NetworkCaptureHeader(parameter.Name, Convert.ToString(parameter.Value)))
            .ToList();

        if (!entry.RequestHeaders.Any(header => string.Equals(header.Name, "User-Agent", StringComparison.OrdinalIgnoreCase)))
            entry.RequestHeaders.Add(new NetworkCaptureHeader("User-Agent", userAgent));

        var cookies = request.Parameters.Where(parameter => parameter.Type == ParameterType.Cookie).ToList();
        if (cookies.Count > 0)
            entry.RequestHeaders.Add(new NetworkCaptureHeader("Cookie",
                string.Join("; ", cookies.Select(cookie => cookie.Name + "=" + Convert.ToString(cookie.Value)))));

        var bodyParameter = request.Parameters.FirstOrDefault(parameter => parameter.Type == ParameterType.RequestBody);
        if (bodyParameter != null)
        {
            if (!entry.RequestHeaders.Any(header => string.Equals(header.Name, "Content-Type", StringComparison.OrdinalIgnoreCase)))
                entry.RequestHeaders.Add(new NetworkCaptureHeader("Content-Type", bodyParameter.Name));
            entry.RequestBody = FormatBody(bodyParameter.Value, encoding);
        }
        else if (request.Method == Method.POST || request.Method == Method.PUT || request.Method == Method.PATCH)
        {
            var parameters = request.Parameters.Where(parameter => parameter.Type == ParameterType.GetOrPost).ToList();
            entry.RequestBody = parameters.Count == 0
                ? null
                : Limit(string.Join("&", parameters.Select(parameter => parameter.Name.UrlEncode() + "=" +
                    Convert.ToString(parameter.Value).UrlEncode())));
        }
    }

    private static NetworkCaptureResponse CreateResponse(INetResponse response, Encoding encoding)
    {
        var headers = response.Headers
            .Select(header => new NetworkCaptureHeader(header.Name, Convert.ToString(header.Value)))
            .ToList();
        if (!string.IsNullOrEmpty(response.ContentType) &&
            !headers.Any(header => string.Equals(header.Name, "Content-Type", StringComparison.OrdinalIgnoreCase)))
            headers.Add(new NetworkCaptureHeader("Content-Type", response.ContentType));

        string body;
        try
        {
            body = !string.IsNullOrEmpty(response.Content)
                ? Limit(response.Content)
                : FormatBody(response.RawBytes, encoding);
        }
        catch
        {
            body = FormatBody(response.RawBytes, encoding);
        }

        return new NetworkCaptureResponse
        {
            StatusCode = (int)response.StatusCode,
            StatusDescription = response.StatusDescription,
            ResponseStatus = response.ResponseStatus.ToString(),
            Url = response.ResponseUri?.ToString(),
            Headers = headers,
            Body = body,
            Error = response.ErrorMessage ?? response.ErrorException?.Message
        };
    }

    private static string BuildUrl(Uri baseUrl, INetRequest request)
    {
        try
        {
            var url = new Uri(baseUrl, request.Resource ?? string.Empty).ToString();
            foreach (var parameter in request.Parameters.Where(parameter => parameter.Type == ParameterType.UrlSegment))
                url = url.Replace("{" + parameter.Name + "}", Convert.ToString(parameter.Value).UrlEncode());

            var includeFormParameters = request.Method != Method.POST && request.Method != Method.PUT &&
                                        request.Method != Method.PATCH;
            var query = request.Parameters.Where(parameter => parameter.Type == ParameterType.QueryString ||
                                                               includeFormParameters && parameter.Type == ParameterType.GetOrPost)
                .Select(parameter => parameter.Name.UrlEncode() + "=" + Convert.ToString(parameter.Value).UrlEncode())
                .ToArray();
            if (query.Length > 0) url += (url.Contains("?") ? "&" : "?") + string.Join("&", query);
            return url;
        }
        catch
        {
            return (baseUrl?.ToString() ?? string.Empty) + (request?.Resource ?? string.Empty);
        }
    }

    private static string FormatBody(object value, Encoding encoding)
    {
        if (value == null) return null;
        if (!(value is byte[] bytes)) return Limit(Convert.ToString(value));
        if (bytes.Length == 0) return string.Empty;

        var text = (encoding ?? Encoding.UTF8).GetString(bytes);
        var binary = text.IndexOf('\ufffd') >= 0 || text.Any(character => char.IsControl(character) &&
            character != '\r' && character != '\n' && character != '\t');
        return binary ? Limit("[base64]\n" + Convert.ToBase64String(bytes)) : Limit(text);
    }

    private static string Limit(string value)
    {
        if (value == null || value.Length <= MaxBodyCharacters) return value;
        return value.Substring(0, MaxBodyCharacters) + "\n[truncated]";
    }

    private static NetworkCaptureEntry Clone(NetworkCaptureEntry source)
    {
        return new NetworkCaptureEntry
        {
            Id = source.Id,
            StartedAt = source.StartedAt,
            DurationMs = source.DurationMs,
            Method = source.Method,
            Url = source.Url,
            State = source.State,
            RequestHeaders = source.RequestHeaders?.Select(header => new NetworkCaptureHeader(header.Name, header.Value)).ToList(),
            RequestBody = source.RequestBody,
            Response = source.Response == null ? null : new NetworkCaptureResponse
            {
                StatusCode = source.Response.StatusCode,
                StatusDescription = source.Response.StatusDescription,
                ResponseStatus = source.Response.ResponseStatus,
                Url = source.Response.Url,
                Headers = source.Response.Headers?.Select(header => new NetworkCaptureHeader(header.Name, header.Value)).ToList(),
                Body = source.Response.Body,
                Error = source.Response.Error
            },
            Error = source.Error,
            IsCompleted = source.IsCompleted
        };
    }
}

public class NetworkCaptureEntry
{
    public long Id { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public long DurationMs { get; set; }
    public string Method { get; set; }
    public string Url { get; set; }
    public string State { get; set; }
    public IList<NetworkCaptureHeader> RequestHeaders { get; set; }
    public string RequestBody { get; set; }
    public NetworkCaptureResponse Response { get; set; }
    public string Error { get; set; }
    public bool IsCompleted { get; set; }
}

public class NetworkCaptureResponse
{
    public int StatusCode { get; set; }
    public string StatusDescription { get; set; }
    public string ResponseStatus { get; set; }
    public string Url { get; set; }
    public IList<NetworkCaptureHeader> Headers { get; set; }
    public string Body { get; set; }
    public string Error { get; set; }
}

public class NetworkCaptureHeader
{
    public NetworkCaptureHeader()
    {
    }

    public NetworkCaptureHeader(string name, string value)
    {
        Name = name;
        Value = value;
    }

    public string Name { get; set; }
    public string Value { get; set; }
}
