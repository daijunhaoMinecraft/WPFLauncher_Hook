using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Mcl.Core.Tools;

namespace Mcl.Core.Dotnetdetour.Utilities.Crypto;

/// <summary>Legacy method names delegate to the canonical Tools implementation.</summary>
public class AESHelper
{
    public static byte[] AES_CBC_Decrypt(byte[] key, byte[] data, byte[] iv) => AesHelper.AesCbcDecrypt(key, data, iv);
    public static byte[] AES_CBC_Encrypt(byte[] key, byte[] data, byte[] iv) => AesHelper.AesCbcEncrypt(key, data, iv);
    public static byte[] AES_CBC256_Encrypt(byte[] key, byte[] data, byte[] iv) => AesHelper.AesCbc256Encrypt(key, data, iv);
    public static byte[] AES_CFB_Decrypt(byte[] key, byte[] data, byte[] iv) => AesHelper.AesCfbDecrypt(key, data, iv);
    public static ICryptoTransform GetCipherInstance(byte[] key, bool encrypt = true) => AesHelper.GetCipherInstance(key, encrypt);
    public static byte[] Encrypt(byte[] key, byte[] source) => AesHelper.Encrypt(key, source);
    public static byte[] Decrypt(byte[] key, byte[] source) => AesHelper.Decrypt(key, source);
    [Obsolete("Use GetCipherInstance.")]
    public static ICryptoTransform getCipherInstance(byte[] Key, bool encrypt = true) => GetCipherInstance(Key, encrypt);
    [Obsolete("Use Encrypt.")]
    public static byte[] encrypt(byte[] key, byte[] source) => Encrypt(key, source);
    [Obsolete("Use Decrypt.")]
    public static byte[] decrypt(byte[] key, byte[] source) => Decrypt(key, source);
    public static byte[] AES_ECB_Encrypt(byte[] key, byte[] data) => AesHelper.AesEcbEncrypt(key, data);

    public static string BytesToHex(byte[] bytes) => bytes == null ? string.Empty : Mcl.Core.Tools.ByteArrayExtensions.ToHex(bytes, true);

    public static byte[] HexToBytes(string hex)
    {
        return (from x in Enumerable.Range(0, hex.Length)
            where x % 2 == 0
            select Convert.ToByte(hex.Substring(x, 2), 16)).ToArray();
    }

    public static byte[] GetIv(int n)
    {
        var array = new[]
        {
            'a', 'b', 'd', 'c', 'e', 'f', 'g', 'h', 'i', 'j',
            'k', 'l', 'm', 'n', 'p', 'r', 'q', 's', 't', 'u',
            'v', 'w', 'z', 'y', 'x', '0', '1', '2', '3', '4',
            '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E',
            'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'Q',
            'P', 'R', 'T', 'S', 'V', 'U', 'W', 'X', 'Y', 'Z'
        };
        var stringBuilder = new StringBuilder();
        var random = new Random(DateTime.Now.Millisecond);
        for (var i = 0; i < n; i++) stringBuilder.Append(array[random.Next(0, array.Length)].ToString());
        return Encoding.UTF8.GetBytes(stringBuilder.ToString());
    }

    public static byte[] GetDefaultIv()
    {
        return Encoding.UTF8.GetBytes("1234567890123456");
    }

    public static byte[] AESEncryptECB128(byte[] data, byte[] keyBytes, byte[] ivBytes) =>
        AesHelper.Transform(keyBytes, data, ivBytes, CipherMode.ECB, PaddingMode.PKCS7, true);

    public static byte[] AESDecryptECB128(byte[] data, byte[] keyBytes, byte[] ivBytes) =>
        AesHelper.Transform(keyBytes, data, ivBytes, CipherMode.ECB, PaddingMode.PKCS7, false);

    public static byte[] AESEncrypt128Ex(byte[] data, byte[] keyBytes, byte[] ivBytes) =>
        AesHelper.Transform(keyBytes, data, ivBytes, CipherMode.CBC, PaddingMode.Zeros, true);

    public static byte[] AESDecrypt128Ex(byte[] data, byte[] keyBytes, byte[] ivBytes) =>
        AesHelper.Transform(keyBytes, data, ivBytes, CipherMode.CBC, PaddingMode.Zeros, false);
}