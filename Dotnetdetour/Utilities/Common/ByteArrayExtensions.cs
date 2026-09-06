using System;
using System.Text;

namespace Mcl.Core.Dotnetdetour.Tools;

public static class ByteArrayExtensions
{
    public static string ToBinary(this byte[] buffer)
    {
        var stringBuilder = new StringBuilder(buffer.Length * 8);
        for (var i = 0; i < buffer.Length; i++)
        {
            var text = Convert.ToString(buffer[i], 2);
            for (var j = 0; j < 8 - text.Length; j++) stringBuilder.Append('0');
            stringBuilder.Append(text);
        }

        return stringBuilder.ToString();
    }
}