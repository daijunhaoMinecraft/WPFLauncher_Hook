using System;
using System.Text;

namespace Mcl.Core.Extensions;

public static class StringExtensions
{
    public static string UrlEncode(this string value)
    {
        return Uri.EscapeDataString(value);
    }

    public static string UrlDecode(this string value)
    {
        return Uri.UnescapeDataString(value);
    }

    public static string AsString(this byte[] buffer)
    {
        var flag = buffer == null;
        string text;
        if (flag)
        {
            text = "";
        }
        else
        {
            var utf = Encoding.UTF8;
            text = utf.GetString(buffer, 0, buffer.Length);
        }

        return text;
    }

    public static bool HasValue(this string input)
    {
        return !string.IsNullOrEmpty(input);
    }
}