using System;
using System.Globalization;
using System.Linq;

namespace Mcl.Core.Dotnetdetour.Tools;

public static class StringExtensions
{
    public static string RandStringRunes(int length)
    {
        var text = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string((from s in Enumerable.Repeat(text, length)
            select s[random.Next(s.Length)]).ToArray());
    }

    public static string RandomLetter(int length)
    {
        var random = new Random();
        return new string((from s in Enumerable.Repeat("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz", length)
            select s[random.Next(s.Length)]).ToArray());
    }

    public static uint SafeParseToUInt32(this string numStr)
    {
        uint num;
        uint.TryParse(numStr, out num);
        return num;
    }

    public static byte[] HexToBytes(this string hex)
    {
        if (string.IsNullOrEmpty(hex)) return null;
        var array = new byte[hex.Length / 2];
        for (var i = 0; i < array.Length; i++)
            try
            {
                array[i] = byte.Parse(hex.Substring(i * 2, 2), NumberStyles.HexNumber);
            }
            catch (Exception ex)
            {
                throw new FormatException("hex is not a valid hex number!", ex);
            }

        return array;
    }
}