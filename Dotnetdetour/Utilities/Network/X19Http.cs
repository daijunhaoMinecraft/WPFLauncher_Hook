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
    private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static string unix_timestamp_to(long unixTimestamp)
    {
        var dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp);
        return dateTimeOffset.ToOffset(TimeSpan.FromHours(8)).ToString();
    }

    public static class TimestampHelper
    {
        public static long GetCurrentTimestampSeconds()
        {
            return (long)(DateTime.UtcNow - UnixEpoch).TotalSeconds;
        }

        public static long GetCurrentTimestampMilliseconds()
        {
            return (long)(DateTime.UtcNow - UnixEpoch).TotalMilliseconds;
        }
    }
}
