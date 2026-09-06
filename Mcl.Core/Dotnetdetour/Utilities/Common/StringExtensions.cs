using System;
using System.Globalization;
using System.Linq;

namespace Mcl.Core.Dotnetdetour.Utilities.Common;

public static class StringExtensions
{
    public static string RandStringRunes(int length) => Mcl.Core.Tools.StringExtensions.RandStringRunes(length);

    public static string RandomLetter(int length) =>
        Mcl.Core.Tools.StringExtensions.RandomCharacters(length, "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz");

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