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
        var hash = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(Token));
        var sb = new StringBuilder();
        foreach (var b in hash) sb.Append(b.ToString("x2"));
        return sb.ToString();
    }

    public static byte[] PickKey(byte query)
    {
        return Encoding.UTF8.GetBytes(_keys[(query >> 4) & 15]);
    }

    public static string DecryptX19Body(byte[] body)
    {
        if (body.Length < 18) throw new ArgumentException("Input body too short");
        var array = AesHelper.AesCbcDecrypt(PickKey(body[body.Length - 1]),
            body.Skip(16).Take(body.Length - 17).ToArray(), body.Take(16).ToArray());
        var i = 0;
        var num = array.Length - 1;
        while (i < 16)
        {
            if (array[num] != 0) i++;
            num--;
        }

        return Encoding.UTF8.GetString(array.Take(num + 1).ToArray());
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

    public static string ComputeDynamicToken(string path, string body)
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.Append(HashHelper.ComputeMD5(Encoding.UTF8.GetBytes(Token)));
        stringBuilder.Append(body);
        stringBuilder.Append("0eGsBkhl");
        stringBuilder.Append(path.TrimEnd('?'));
        var bytes = Encoding.UTF8.GetBytes(HashHelper.ComputeMD5(Encoding.UTF8.GetBytes(stringBuilder.ToString())));
        var text = bytes.ToBinary();
        text = text.Substring(6) + text.Substring(0, 6);
        for (var i = 0; i < bytes.Length; i++)
        {
            var text2 = text.Substring(i * 8, 8);
            byte b = 0;
            for (var j = 0; j < 8; j++)
                if (text2[7 - j] == '1')
                    b = (byte)(b | (1 << j));

            bytes[i] = (byte)(b ^ bytes[i]);
        }

        return Convert.ToBase64String(bytes).Substring(0, 16).Replace("+", "m")
            .Replace("/", "o") + "1";
    }

    public static string ComputeDynamicToken(string path, string body, byte[] token)
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.Append(token.ToHex());
        stringBuilder.Append(body);
        stringBuilder.Append("0eGsBkhl");
        stringBuilder.Append(path.TrimEnd('?'));
        var bytes = Encoding.UTF8.GetBytes(HashHelper.ComputeMD5(Encoding.UTF8.GetBytes(stringBuilder.ToString())));
        var text = bytes.ToBinary();
        text = text.Substring(6) + text.Substring(0, 6);
        for (var i = 0; i < bytes.Length; i++)
        {
            var text2 = text.Substring(i * 8, 8);
            byte b = 0;
            for (var j = 0; j < 8; j++)
                if (text2[7 - j] == '1')
                    b = (byte)(b | (1 << j));

            bytes[i] = (byte)(b ^ bytes[i]);
        }

        return Convert.ToBase64String(bytes).Substring(0, 16).Replace("+", "m")
            .Replace("/", "o") + "1";
    }
}

public class AesHelper
{
    public static byte[] AesCbcDecrypt(byte[] key, byte[] data, byte[] iv)
    {
        byte[] array2;
        using (var aes = Aes.Create())
        {
            aes.KeySize = key.Length * 8;
            aes.BlockSize = 128;
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.None;
            using (var cryptoTransform = aes.CreateDecryptor())
            {
                var array = new byte[data.Length];
                cryptoTransform.TransformBlock(data, 0, data.Length, array, 0);
                array2 = array;
            }
        }

        return array2;
    }

    public static byte[] AesCbcEncrypt(byte[] key, byte[] data, byte[] iv)
    {
        byte[] array2;
        using (var aes = Aes.Create())
        {
            aes.KeySize = key.Length * 8;
            aes.BlockSize = 128;
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.None;
            using (var cryptoTransform = aes.CreateEncryptor())
            {
                var array = new byte[data.Length];
                cryptoTransform.TransformBlock(data, 0, data.Length, array, 0);
                array2 = array;
            }
        }

        return array2;
    }

    public static byte[] AesCbc256Encrypt(byte[] key, byte[] toEncrypt, byte[] iv)
    {
        var num = 16 - toEncrypt.Length % 16;
        if (num == 16) num = 0;
        int num2;
        if (toEncrypt.Length >= 16)
        {
            num2 = toEncrypt.Length / 16;
            if (num != 0) num2++;
        }
        else
        {
            num2 = 1;
        }

        var array = new byte[num2 * 16];
        Array.Copy(toEncrypt, array, toEncrypt.Length);
        for (var i = 0; i < num; i++) array[i + toEncrypt.Length] = (byte)num;
        return new RijndaelManaged
        {
            Key = key,
            IV = iv,
            Mode = CipherMode.CBC,
            Padding = PaddingMode.None
        }.CreateEncryptor().TransformFinalBlock(array, 0, array.Length);
    }

    public static ICryptoTransform GetCipherInstance(byte[] key, bool encrypt = true)
    {
        if (key.Length < 16)
            Array.Resize(ref key, 16);
        else if (key.Length < 24)
            Array.Resize(ref key, 24);
        else if (key.Length < 32)
            Array.Resize(ref key, 32);
        else
            Array.Resize(ref key, 32);
        var rijndaelManaged = new RijndaelManaged();
        rijndaelManaged.Mode = CipherMode.ECB;
        rijndaelManaged.KeySize = 128;
        rijndaelManaged.Key = key;
        rijndaelManaged.Padding = PaddingMode.PKCS7;
        rijndaelManaged.BlockSize = 128;
        if (encrypt) return rijndaelManaged.CreateEncryptor();
        return rijndaelManaged.CreateDecryptor();
    }

    public static byte[] Encrypt(byte[] key, byte[] source)
    {
        return GetCipherInstance(key).TransformFinalBlock(source, 0, source.Length);
    }

    public static byte[] Decrypt(byte[] key, byte[] source)
    {
        return GetCipherInstance(key, false).TransformFinalBlock(source, 0, source.Length);
    }

    public static byte[] AesCfbDecrypt(byte[] key, byte[] data, byte[] iv)
    {
        byte[] array3;
        try
        {
            var memoryStream = new MemoryStream(data);
            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CFB;
                aes.Padding = PaddingMode.Zeros;
                var cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Read);
                try
                {
                    var array = new byte[data.Length + 32];
                    var num = cryptoStream.Read(array, 0, data.Length + 32);
                    var array2 = new byte[num];
                    Array.Copy(array, 0, array2, 0, num);
                    array3 = array2;
                }
                finally
                {
                    cryptoStream.Close();
                    memoryStream.Close();
                    aes.Clear();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            array3 = null;
        }

        return array3;
    }

    public static byte[] AesEcbEncrypt(byte[] key, byte[] data)
    {
        byte[] array2;
        using (var aes = Aes.Create())
        {
            aes.KeySize = key.Length * 8;
            aes.BlockSize = 128;
            aes.Key = key;
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.None;
            using (var cryptoTransform = aes.CreateEncryptor())
            {
                var array = new byte[data.Length];
                cryptoTransform.TransformBlock(data, 0, data.Length, array, 0);
                array2 = array;
            }
        }

        return array2;
    }
}

public static class ByteArrayExtensions
{
    public static string ToHex(this byte[] bytes, bool toUpper = false)
    {
        var stringBuilder = new StringBuilder();
        foreach (var b in bytes) stringBuilder.AppendFormat(!toUpper ? "{0:x2}" : "{0:X2}", b);
        return stringBuilder.ToString();
    }

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

public static class HashExtensions
{
    public static byte[] ComputeFileHash(this MD5 md5, string filePath)
    {
        byte[] array;
        using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            array = md5.ComputeHash(fileStream);
        }

        return array;
    }
}

public static class HashHelper
{
    public static string ComputeMD5(byte[] bytes)
    {
        try
        {
            var array = MD5.Create().ComputeHash(bytes);
            var stringBuilder = new StringBuilder();
            foreach (var b in array) stringBuilder.Append(b.ToString("x2"));
            return stringBuilder.ToString();
        }
        catch
        {
        }

        return null;
    }
}

public static class StringExtensions
{
    public static string RandStringRunes(int length)
    {
        var text = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string((from s in Enumerable.Repeat(text, length)
            select s[random.Next(s.Length)]).ToArray());
    }
}