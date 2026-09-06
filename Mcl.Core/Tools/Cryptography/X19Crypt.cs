using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Mcl.Core.Tools;

public class X19Crypt
{
    private static readonly string[] _keys = new[]
    {
        "MK6mipwmOUedplb6", "OtEylfId6dyhrfdn", "VNbhn5mvUaQaeOo9", "bIEoQGQYjKd02U0J", "fuaJrPwaH2cfXXLP",
        "LEkdyiroouKQ4XN1", "jM1h27H4UROu427W", "DhReQada7gZybTDk", "ZGXfpSTYUvcdKqdY", "AZwKf7MWZrJpGR5W",
        "amuvbcHw38TcSyPU", "SI4QotspbjhyFdT0", "VP4dhjKnDGlSJtbB", "UXDZx4KhZywQ2tcn", "NIK73ZNvNqzva4kd",
        "WeiW7qU766Q1YQZI"
    };

    public static string Token { get; set; } = string.Empty;
    public static string UserId { get; set; } = string.Empty;

    public static string GetH5Token()
    {
        if (string.IsNullOrEmpty(Token))
            return string.Empty;
        return HashHelper.ComputeMD5(Encoding.UTF8.GetBytes(Token));
    }

    public static byte[] PickKey(byte query)
    {
        return Encoding.UTF8.GetBytes(_keys[(query >> 4) & 15]);
    }

    public static string DecryptX19Body(byte[] body)
    {
        if (body == null) throw new ArgumentNullException(nameof(body));
        const int ivLength = 16;
        var encryptedLength = body.Length - ivLength - 1;
        if (encryptedLength < 16 || encryptedLength % 16 != 0)
            throw new ArgumentException("The X19 body must contain an IV, complete AES blocks and a selector.",
                nameof(body));
        var iv = new byte[ivLength];
        var encrypted = new byte[encryptedLength];
        Buffer.BlockCopy(body, 0, iv, 0, iv.Length);
        Buffer.BlockCopy(body, iv.Length, encrypted, 0, encrypted.Length);
        var plaintext = AesHelper.AesCbcDecrypt(PickKey(body[body.Length - 1]), encrypted, iv);
        var last = plaintext.Length - 1;
        var trailerCharacters = 0;
        while (last >= 0 && trailerCharacters < 16)
        {
            if (plaintext[last] != 0) trailerCharacters++;
            last--;
        }

        if (trailerCharacters != 16) throw new FormatException("The X19 body has an invalid random trailer.");
        return Encoding.UTF8.GetString(plaintext, 0, last + 1);
    }

    public static byte[] HttpEncrypt(byte[] bodyIn)
    {
        byte[] array4;
        try
        {
            var array = new byte[(int)Math.Ceiling((bodyIn.Length + 16) / 16.0) * 16];
            Array.Copy(bodyIn, array, bodyIn.Length);
            var bytes = Encoding.ASCII.GetBytes(StringExtensions.RandStringRunes(16));
            for (var i = 0; i < bytes.Length; i++) array[i + bodyIn.Length] = bytes[i];
            var b = (byte)((new Random().Next(0, 15) << 4) | 2);
            var bytes2 = Encoding.ASCII.GetBytes(StringExtensions.RandStringRunes(16));
            var array2 = AesHelper.AesCbcEncrypt(PickKey(b), array, bytes2);
            var array3 = new byte[16 + array2.Length + 1];
            Array.Copy(bytes2, 0, array3, 0, 16);
            Array.Copy(array2, 0, array3, 16, array2.Length);
            array3[array3.Length - 1] = b;
            array4 = array3;
        }
        catch
        {
            array4 = new byte[0];
        }

        return array4;
    }

    public static string ComputeDynamicToken(string path, string body) =>
        ComputeDynamicTokenCore(path, body, HashHelper.ComputeMD5(Encoding.UTF8.GetBytes(Token)));

    public static string ComputeDynamicToken(string path, string body, byte[] token) =>
        ComputeDynamicTokenCore(path, body, token.ToHex());

    private static string ComputeDynamicTokenCore(string path, string body, string tokenHash)
    {
        if (path == null) throw new ArgumentNullException(nameof(path));
        var hash = HashHelper.ComputeMD5(Encoding.UTF8.GetBytes(tokenHash + body + "0eGsBkhl" + path.TrimEnd('?')));
        var bytes = Encoding.ASCII.GetBytes(hash);
        var transformed = new byte[bytes.Length];
        // Rotate the complete ASCII bit stream left by six bits, then XOR with the original.
        for (var index = 0; index < bytes.Length; index++)
            transformed[index] =
                (byte)(((bytes[index] << 6) | (bytes[(index + 1) % bytes.Length] >> 2)) ^ bytes[index]);
        return Convert.ToBase64String(transformed).Substring(0, 16).Replace("+", "m").Replace("/", "o") + "1";
    }
}