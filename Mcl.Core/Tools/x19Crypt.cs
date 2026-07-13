using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Mcl.Core.Tools
{
	public class X19Crypt
	{
		public static string Token { get; set; } = string.Empty;
		public static string UserId { get; set; } = string.Empty;

		public static string GetH5Token()
		{
			if (string.IsNullOrEmpty(Token))
				return string.Empty;
			byte[] hash = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(Token));
			StringBuilder sb = new StringBuilder();
			foreach (byte b in hash)
			{
				sb.Append(b.ToString("x2"));
			}
			return sb.ToString();
		}

		public static byte[] PickKey(byte query)
		{
			return Encoding.UTF8.GetBytes(X19Crypt._keys[(query >> 4) & 15]);
		}

		public static string DecryptX19Body(byte[] body)
		{
			if (body.Length < 18)
			{
				throw new ArgumentException("Input body too short");
			}
			byte[] array = AesHelper.AesCbcDecrypt(X19Crypt.PickKey(body[body.Length - 1]), body.Skip(16).Take(body.Length - 17).ToArray<byte>(), body.Take(16).ToArray<byte>());
			int i = 0;
			int num = array.Length - 1;
			while (i < 16)
			{
				if (array[num] != 0)
				{
					i++;
				}
				num--;
			}
			return Encoding.UTF8.GetString(array.Take(num + 1).ToArray<byte>());
		}

		public static byte[] HttpEncrypt(byte[] bodyIn)
		{
			byte[] array4;
			try
			{
				byte[] array = new byte[(int)Math.Ceiling((double)(bodyIn.Length + 16) / 16.0) * 16];
				Array.Copy(bodyIn, array, bodyIn.Length);
				byte[] bytes = Encoding.ASCII.GetBytes(StringExtensions.RandStringRunes(16));
				for (int i = 0; i < bytes.Length; i++)
				{
					array[i + bodyIn.Length] = bytes[i];
				}
				byte b = (byte)((new Random().Next(0, 15) << 4) | 2);
				byte[] bytes2 = Encoding.ASCII.GetBytes(StringExtensions.RandStringRunes(16));
				byte[] array2 = AesHelper.AesCbcEncrypt(X19Crypt.PickKey(b), array, bytes2);
				byte[] array3 = new byte[16 + array2.Length + 1];
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
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(HashHelper.ComputeMD5(Encoding.UTF8.GetBytes(Token)));
			stringBuilder.Append(body);
			stringBuilder.Append("0eGsBkhl");
			stringBuilder.Append(path.TrimEnd(new char[] { '?' }));
			byte[] bytes = Encoding.UTF8.GetBytes(HashHelper.ComputeMD5(Encoding.UTF8.GetBytes(stringBuilder.ToString())));
			string text = bytes.ToBinary();
			text = text.Substring(6) + text.Substring(0, 6);
			for (int i = 0; i < bytes.Length; i++)
			{
				string text2 = text.Substring(i * 8, 8);
				byte b = 0;
				for (int j = 0; j < 8; j++)
				{
					if (text2[7 - j] == '1')
					{
						b = (byte)((int)b | (1 << j));
					}
				}
				bytes[i] = (byte)(b ^ bytes[i]);
			}
			return Convert.ToBase64String(bytes).Substring(0, 16).Replace("+", "m")
				.Replace("/", "o") + "1";
		}

		public static string ComputeDynamicToken(string path, string body, byte[] token)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(token.ToHex(false));
			stringBuilder.Append(body);
			stringBuilder.Append("0eGsBkhl");
			stringBuilder.Append(path.TrimEnd(new char[] { '?' }));
			byte[] bytes = Encoding.UTF8.GetBytes(HashHelper.ComputeMD5(Encoding.UTF8.GetBytes(stringBuilder.ToString())));
			string text = bytes.ToBinary();
			text = text.Substring(6) + text.Substring(0, 6);
			for (int i = 0; i < bytes.Length; i++)
			{
				string text2 = text.Substring(i * 8, 8);
				byte b = 0;
				for (int j = 0; j < 8; j++)
				{
					if (text2[7 - j] == '1')
					{
						b = (byte)((int)b | (1 << j));
					}
				}
				bytes[i] = (byte)(b ^ bytes[i]);
			}
			return Convert.ToBase64String(bytes).Substring(0, 16).Replace("+", "m")
				.Replace("/", "o") + "1";
		}

		private static readonly string[] _keys = new string[]
		{
			"MK6mipwmOUedplb6", "OtEylfId6dyhrfdn", "VNbhn5mvUaQaeOo9", "bIEoQGQYjKd02U0J", "fuaJrPwaH2cfXXLP", "LEkdyiroouKQ4XN1", "jM1h27H4UROu427W", "DhReQada7gZybTDk", "ZGXfpSTYUvcdKqdY", "AZwKf7MWZrJpGR5W",
			"amuvbcHw38TcSyPU", "SI4QotspbjhyFdT0", "VP4dhjKnDGlSJtbB", "UXDZx4KhZywQ2tcn", "NIK73ZNvNqzva4kd", "WeiW7qU766Q1YQZI"
		};
	}

	public class AesHelper
	{
		public static byte[] AesCbcDecrypt(byte[] key, byte[] data, byte[] iv)
		{
			byte[] array2;
			using (Aes aes = Aes.Create())
			{
				aes.KeySize = key.Length * 8;
				aes.BlockSize = 128;
				aes.Key = key;
				aes.IV = iv;
				aes.Mode = CipherMode.CBC;
				aes.Padding = PaddingMode.None;
				using (ICryptoTransform cryptoTransform = aes.CreateDecryptor())
				{
					byte[] array = new byte[data.Length];
					cryptoTransform.TransformBlock(data, 0, data.Length, array, 0);
					array2 = array;
				}
			}
			return array2;
		}

		public static byte[] AesCbcEncrypt(byte[] key, byte[] data, byte[] iv)
		{
			byte[] array2;
			using (Aes aes = Aes.Create())
			{
				aes.KeySize = key.Length * 8;
				aes.BlockSize = 128;
				aes.Key = key;
				aes.IV = iv;
				aes.Mode = CipherMode.CBC;
				aes.Padding = PaddingMode.None;
				using (ICryptoTransform cryptoTransform = aes.CreateEncryptor())
				{
					byte[] array = new byte[data.Length];
					cryptoTransform.TransformBlock(data, 0, data.Length, array, 0);
					array2 = array;
				}
			}
			return array2;
		}

		public static byte[] AesCbc256Encrypt(byte[] key, byte[] toEncrypt, byte[] iv)
		{
			int num = 16 - toEncrypt.Length % 16;
			if (num == 16)
			{
				num = 0;
			}
			int num2;
			if (toEncrypt.Length >= 16)
			{
				num2 = toEncrypt.Length / 16;
				if (num != 0)
				{
					num2++;
				}
			}
			else
			{
				num2 = 1;
			}
			byte[] array = new byte[num2 * 16];
			Array.Copy(toEncrypt, array, toEncrypt.Length);
			for (int i = 0; i < num; i++)
			{
				array[i + toEncrypt.Length] = (byte)num;
			}
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
			{
				Array.Resize<byte>(ref key, 16);
			}
			else if (key.Length < 24)
			{
				Array.Resize<byte>(ref key, 24);
			}
			else if (key.Length < 32)
			{
				Array.Resize<byte>(ref key, 32);
			}
			else
			{
				Array.Resize<byte>(ref key, 32);
			}
			RijndaelManaged rijndaelManaged = new RijndaelManaged();
			rijndaelManaged.Mode = CipherMode.ECB;
			rijndaelManaged.KeySize = 128;
			rijndaelManaged.Key = key;
			rijndaelManaged.Padding = PaddingMode.PKCS7;
			rijndaelManaged.BlockSize = 128;
			if (encrypt)
			{
				return rijndaelManaged.CreateEncryptor();
			}
			return rijndaelManaged.CreateDecryptor();
		}

		public static byte[] Encrypt(byte[] key, byte[] source)
		{
			return AesHelper.GetCipherInstance(key, true).TransformFinalBlock(source, 0, source.Length);
		}

		public static byte[] Decrypt(byte[] key, byte[] source)
		{
			return AesHelper.GetCipherInstance(key, false).TransformFinalBlock(source, 0, source.Length);
		}

		public static byte[] AesCfbDecrypt(byte[] key, byte[] data, byte[] iv)
		{
			byte[] array3;
			try
			{
				MemoryStream memoryStream = new MemoryStream(data);
				using (Aes aes = Aes.Create())
				{
					aes.Key = key;
					aes.IV = iv;
					aes.Mode = CipherMode.CFB;
					aes.Padding = PaddingMode.Zeros;
					CryptoStream cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Read);
					try
					{
						byte[] array = new byte[data.Length + 32];
						int num = cryptoStream.Read(array, 0, data.Length + 32);
						byte[] array2 = new byte[num];
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
			using (Aes aes = Aes.Create())
			{
				aes.KeySize = key.Length * 8;
				aes.BlockSize = 128;
				aes.Key = key;
				aes.Mode = CipherMode.ECB;
				aes.Padding = PaddingMode.None;
				using (ICryptoTransform cryptoTransform = aes.CreateEncryptor())
				{
					byte[] array = new byte[data.Length];
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
			StringBuilder stringBuilder = new StringBuilder();
			foreach (byte b in bytes)
			{
				stringBuilder.AppendFormat((!toUpper) ? "{0:x2}" : "{0:X2}", b);
			}
			return stringBuilder.ToString();
		}

		public static string ToBinary(this byte[] buffer)
		{
			StringBuilder stringBuilder = new StringBuilder(buffer.Length * 8);
			for (int i = 0; i < buffer.Length; i++)
			{
				string text = Convert.ToString(buffer[i], 2);
				for (int j = 0; j < 8 - text.Length; j++)
				{
					stringBuilder.Append('0');
				}
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
			using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
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
				byte[] array = MD5.Create().ComputeHash(bytes);
				StringBuilder stringBuilder = new StringBuilder();
				foreach (byte b in array)
				{
					stringBuilder.Append(b.ToString("x2"));
				}
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
			string text = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
			Random random = new Random();
			return new string((from s in Enumerable.Repeat<string>(text, length)
				select s[random.Next(s.Length)]).ToArray<char>());
		}
	}
}
