using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Mcl.Core.NeteaseProtocol;

namespace Mcl.Core.Dotnetdetour.Tools;

public class X19Tools
{
    public static string unix_timestamp_to(long get_unix)
    {
        var dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(get_unix);
        return dateTimeOffset.ToOffset(TimeSpan.FromHours(8)).ToString();
    }

    public static class TimestampHelper
    {
        public static long GetCurrentTimestampSeconds()
        {
            return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
        }

        public static long GetCurrentTimestampMilliseconds()
        {
            return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
        }
    }
}