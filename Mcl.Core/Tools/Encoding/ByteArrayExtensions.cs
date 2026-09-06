using System;

namespace Mcl.Core.Tools;

public static class ByteArrayExtensions
{
    public static string ToHex(this byte[] bytes, bool toUpper = false)
    {
        if (bytes == null) throw new ArgumentNullException(nameof(bytes));
        var alphabet = toUpper ? "0123456789ABCDEF" : "0123456789abcdef";
        var characters = new char[checked(bytes.Length * 2)];
        for (var index = 0; index < bytes.Length; index++)
        {
            characters[index * 2] = alphabet[bytes[index] >> 4];
            characters[index * 2 + 1] = alphabet[bytes[index] & 15];
        }

        return new string(characters);
    }

    public static string ToBinary(this byte[] buffer)
    {
        if (buffer == null) throw new ArgumentNullException(nameof(buffer));
        var characters = new char[checked(buffer.Length * 8)];
        for (var index = 0; index < buffer.Length; index++)
        for (var bit = 0; bit < 8; bit++)
            characters[index * 8 + bit] = (buffer[index] & (1 << (7 - bit))) == 0 ? '0' : '1';
        return new string(characters);
    }
}