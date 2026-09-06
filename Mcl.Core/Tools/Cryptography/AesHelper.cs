using System;
using System.IO;
using System.Security.Cryptography;

namespace Mcl.Core.Tools;

/// <summary>Protocol-compatible AES primitives. Padding choices are part of the wire format.</summary>
public class AesHelper
{
    public static byte[] AesCbcDecrypt(byte[] key, byte[] data, byte[] iv) =>
        Transform(key, data, iv, CipherMode.CBC, PaddingMode.None, false);

    public static byte[] AesCbcEncrypt(byte[] key, byte[] data, byte[] iv) =>
        Transform(key, data, iv, CipherMode.CBC, PaddingMode.None, true);

    public static byte[] AesEcbEncrypt(byte[] key, byte[] data) =>
        Transform(key, data, null, CipherMode.ECB, PaddingMode.None, true);

    public static byte[] AesCbc256Encrypt(byte[] key, byte[] data, byte[] iv)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        // Legacy CBC padding: aligned input gets no extra block; empty input gets one zero block.
        var padding = (16 - data.Length % 16) % 16;
        var padded = new byte[Math.Max(16, checked(data.Length + padding))];
        Buffer.BlockCopy(data, 0, padded, 0, data.Length);
        for (var index = data.Length; index < data.Length + padding; index++) padded[index] = (byte)padding;
        return AesCbcEncrypt(key, padded, iv);
    }

    public static ICryptoTransform GetCipherInstance(byte[] key, bool encrypt = true)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
        // Preserve the host's historical key normalization, including exact 16/24-byte inputs.
        var normalized = new byte[key.Length < 16 ? 16 : key.Length < 24 ? 24 : 32];
        Buffer.BlockCopy(key, 0, normalized, 0, Math.Min(key.Length, normalized.Length));
        var aes = Aes.Create();
        try
        {
            aes.Key = normalized;
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.PKCS7;
            return new OwnedTransform(aes, encrypt ? aes.CreateEncryptor() : aes.CreateDecryptor());
        }
        catch
        {
            aes.Dispose();
            throw;
        }
    }

    public static byte[] Encrypt(byte[] key, byte[] source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        using (var transform = GetCipherInstance(key))
            return transform.TransformFinalBlock(source, 0, source.Length);
    }

    public static byte[] Decrypt(byte[] key, byte[] source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        using (var transform = GetCipherInstance(key, false))
            return transform.TransformFinalBlock(source, 0, source.Length);
    }

    public static byte[] AesCfbDecrypt(byte[] key, byte[] data, byte[] iv)
    {
        try
        {
            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CFB;
                aes.Padding = PaddingMode.Zeros;
                using (var input = new MemoryStream(data))
                using (var transform = aes.CreateDecryptor())
                using (var stream = new CryptoStream(input, transform, CryptoStreamMode.Read))
                using (var output = new MemoryStream())
                {
                    stream.CopyTo(output);
                    return output.ToArray();
                }
            }
        }
        catch (CryptographicException)
        {
            return null;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    internal static byte[] Transform(byte[] key, byte[] data, byte[] iv, CipherMode mode, PaddingMode padding,
        bool encrypt)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (mode != CipherMode.ECB && iv == null) throw new ArgumentNullException(nameof(iv));
        using (var aes = Aes.Create())
        {
            aes.Key = key;
            if (iv != null) aes.IV = iv;
            aes.Mode = mode;
            aes.Padding = padding;
            using (var transform = encrypt ? aes.CreateEncryptor() : aes.CreateDecryptor())
                return transform.TransformFinalBlock(data, 0, data.Length);
        }
    }

    private sealed class OwnedTransform : ICryptoTransform
    {
        private readonly SymmetricAlgorithm _algorithm;
        private readonly ICryptoTransform _transform;

        public OwnedTransform(SymmetricAlgorithm algorithm, ICryptoTransform transform)
        {
            _algorithm = algorithm;
            _transform = transform;
        }

        public bool CanReuseTransform => _transform.CanReuseTransform;
        public bool CanTransformMultipleBlocks => _transform.CanTransformMultipleBlocks;
        public int InputBlockSize => _transform.InputBlockSize;
        public int OutputBlockSize => _transform.OutputBlockSize;

        public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer,
            int outputOffset) =>
            _transform.TransformBlock(inputBuffer, inputOffset, inputCount, outputBuffer, outputOffset);

        public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount) =>
            _transform.TransformFinalBlock(inputBuffer, inputOffset, inputCount);

        public void Dispose()
        {
            _transform.Dispose();
            _algorithm.Dispose();
        }
    }
}