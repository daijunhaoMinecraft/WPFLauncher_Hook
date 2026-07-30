using System;
using System.Collections.Generic;
using System.Text;

namespace Mcl.Core.Tools
{
    public static class XxteaEncryption
    {
        private const long Delta = 0x9E3779B9;  // 2654435769
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
            int count = (bytes.Length % 8 == 0 ? 0 : 1) + bytes.Length / 8;
            long[] longs = new long[count];
            
            for (int i = 0; i < count - 1; i++)
            {
                longs[i] = BitConverter.ToInt64(bytes, i * 8);
            }

            byte[] padding = new byte[8];
            Array.Copy(bytes, (count - 1) * 8, padding, 0, bytes.Length - (count - 1) * 8);
            longs[count - 1] = BitConverter.ToInt64(padding, 0);
            
            return longs;
        }

        /// <summary>
        /// 长整型数组转换为字节数组
        /// </summary>
        private static byte[] ConvertLongsToBytes(long[] longs)
        {
            List<byte> bytes = new List<byte>(longs.Length * 8);
            
            for (int i = 0; i < longs.Length; i++)
            {
                bytes.AddRange(BitConverter.GetBytes(longs[i]));
            }

            // 移除末尾的零填充
            while (bytes[bytes.Count - 1] == 0)
            {
                bytes.RemoveAt(bytes.Count - 1);
            }
            
            return bytes.ToArray();
        }

        /// <summary>
        /// 长整型数组转换为十六进制字符串
        /// </summary>
        private static string ConvertLongsToHexString(long[] longs)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < longs.Length; i++)
            {
                sb.Append(longs[i].ToString("x2").PadLeft(16, '0'));
            }
            return sb.ToString();
        }

        /// <summary>
        /// 十六进制字符串转换为长整型数组
        /// </summary>
        private static long[] ConvertHexStringToLongs(string hex)
        {
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