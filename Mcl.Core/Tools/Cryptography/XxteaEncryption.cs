using System;
using System.Collections.Generic;
using System.Text;

namespace Mcl.Core.Tools
{
    // Host-specific 64-bit variant: do not replace with standard 32-bit XXTEA.
    public static class XxteaEncryption
    {
        private const long Delta = 0x9E3779B9; // 2654435769
        private const int BlockSize = 32;
        private const char PaddingChar = '\0';

        /// <summary>
        /// 加密字符串
        /// </summary>
        public static string Encrypt(this string text, string key)
        {
            byte[] textBytes = Encoding.UTF8.GetBytes(text.PadRight(BlockSize, PaddingChar));
            byte[] keyBytes = Encoding.UTF8.GetBytes(key.PadRight(BlockSize, PaddingChar));

            long[] textBlocks = ConvertBytesToLongs(textBytes);
            long[] keyBlocks = ConvertBytesToLongs(keyBytes);

            long[] encryptedBlocks = XxteaEncrypt(textBlocks, keyBlocks);

            return ConvertLongsToHexString(encryptedBlocks);
        }

        /// <summary>
        /// 解密字符串
        /// </summary>
        public static string Decrypt(this string encryptedText, string key)
        {
            if (string.IsNullOrWhiteSpace(encryptedText))
            {
                return encryptedText;
            }

            long[] encryptedBlocks = ConvertHexStringToLongs(encryptedText);
            long[] keyBlocks = ConvertBytesToLongs(Encoding.UTF8.GetBytes(key.PadRight(BlockSize, PaddingChar)));

            long[] decryptedBlocks = XxteaDecrypt(encryptedBlocks, keyBlocks);
            byte[] decryptedBytes = ConvertLongsToBytes(decryptedBlocks);

            return Encoding.UTF8.GetString(decryptedBytes, 0, decryptedBytes.Length);
        }

        /// <summary>
        /// XXTEA 加密核心算法
        /// </summary>
        private static long[] XxteaEncrypt(long[] data, long[] key)
        {
            int length = data.Length;
            if (length < 1)
            {
                return data;
            }

            long last = data[data.Length - 1];
            long first = data[0];
            long sum = 0;
            long rounds = 6 + 52 / length;

            while (rounds-- > 0)
            {
                sum += Delta;
                long temp = (sum >> 2) & 3;
                long i;

                for (i = 0; i < length - 1; i++)
                {
                    first = data[i + 1];
                    last = (data[i] += CalculateMx(sum, first, last, i, temp, key));
                }

                first = data[0];
                last = (data[length - 1] += CalculateMx(sum, first, last, i, temp, key));
            }

            return data;
        }

        /// <summary>
        /// XXTEA 解密核心算法
        /// </summary>
        private static long[] XxteaDecrypt(long[] data, long[] key)
        {
            int length = data.Length;
            if (length < 1)
            {
                return data;
            }

            long last = data[data.Length - 1];
            long first = data[0];
            long rounds = 6 + 52 / length;

            for (long sum = rounds * Delta; sum != 0; sum -= Delta)
            {
                long temp = (sum >> 2) & 3;
                long i;

                for (i = length - 1; i > 0; i--)
                {
                    last = data[i - 1];
                    first = (data[i] -= CalculateMx(sum, first, last, i, temp, key));
                }

                last = data[length - 1];
                first = (data[0] -= CalculateMx(sum, first, last, i, temp, key));
            }

            return data;
        }

        /// <summary>
        /// XXTEA MX 计算函数
        /// </summary>
        private static long CalculateMx(long sum, long first, long last, long index, long temp, long[] key)
        {
            return (((last >> 5) ^ (first << 2)) + ((first >> 3) ^ (last << 4))) ^
                   ((sum ^ first) + (key[(index & 3) ^ temp] ^ last));
        }

        /// <summary>
        /// 字节数组转换为长整型数组
        /// </summary>
        private static long[] ConvertBytesToLongs(byte[] bytes)
        {
            var blocks = new long[(bytes.Length + 7) / 8];
            Buffer.BlockCopy(bytes, 0, blocks, 0, bytes.Length);
            return blocks;
        }

        /// <summary>
        /// 长整型数组转换为字节数组
        /// </summary>
        private static byte[] ConvertLongsToBytes(long[] longs)
        {
            var bytes = new byte[longs.Length * 8];
            Buffer.BlockCopy(longs, 0, bytes, 0, bytes.Length);
            var length = bytes.Length;
            while (length > 0 && bytes[length - 1] == 0) length--;
            Array.Resize(ref bytes, length);
            return bytes;
        }

        /// <summary>
        /// 长整型数组转换为十六进制字符串
        /// </summary>
        private static string ConvertLongsToHexString(long[] longs)
        {
            StringBuilder sb = new StringBuilder(longs.Length * 16);
            for (int i = 0; i < longs.Length; i++)
            {
                sb.Append(longs[i].ToString("x16"));
            }

            return sb.ToString();
        }

        /// <summary>
        /// 十六进制字符串转换为长整型数组
        /// </summary>
        private static long[] ConvertHexStringToLongs(string hex)
        {
            if (hex.Length == 0 || hex.Length % 16 != 0)
                throw new FormatException("Ciphertext must contain complete 64-bit hexadecimal blocks.");
            int count = hex.Length / 16;
            long[] longs = new long[count];

            for (int i = 0; i < count; i++)
            {
                longs[i] = Convert.ToInt64(hex.Substring(i * 16, 16), 16);
            }

            return longs;
        }
    }
}