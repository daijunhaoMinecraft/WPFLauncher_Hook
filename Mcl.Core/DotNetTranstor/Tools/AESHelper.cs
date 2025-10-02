using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using MicrosoftTranslator.DotNetTranstor.Tools;

namespace Noya.LocalServer.Common.Cryptography
{
	// Token: 0x0200004C RID: 76
	public class AESHelper
	{
		// Token: 0x06000440 RID: 1088 RVA: 0x00017730 File Offset: 0x00015930
		public static byte[] AES_CBC_Decrypt(byte[] key, byte[] data, byte[] iv)
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

		// Token: 0x06000441 RID: 1089 RVA: 0x000177DC File Offset: 0x000159DC
		public static byte[] AES_CBC_Encrypt(byte[] key, byte[] data, byte[] iv)
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

		// Token: 0x06000442 RID: 1090 RVA: 0x00017888 File Offset: 0x00015A88
		public static byte[] AES_CBC256_Encrypt(byte[] key, byte[] toEncrypt, byte[] iv)
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

		// Token: 0x06000443 RID: 1091 RVA: 0x00017930 File Offset: 0x00015B30
		public static ICryptoTransform getCipherInstance(byte[] Key, bool encrypt = true)
		{
			if (Key.Length < 16)
			{
				Array.Resize<byte>(ref Key, 16);
			}
			else if (Key.Length < 24)
			{
				Array.Resize<byte>(ref Key, 24);
			}
			else if (Key.Length < 32)
			{
				Array.Resize<byte>(ref Key, 32);
			}
			else
			{
				Array.Resize<byte>(ref Key, 32);
			}
			RijndaelManaged rijndaelManaged = new RijndaelManaged();
			rijndaelManaged.Mode = CipherMode.ECB;
			rijndaelManaged.KeySize = 128;
			rijndaelManaged.Key = Key;
			rijndaelManaged.Padding = PaddingMode.PKCS7;
			rijndaelManaged.BlockSize = 128;
			if (encrypt)
			{
				return rijndaelManaged.CreateEncryptor();
			}
			return rijndaelManaged.CreateDecryptor();
		}

		// Token: 0x06000444 RID: 1092 RVA: 0x000039D8 File Offset: 0x00001BD8
		public static byte[] encrypt(byte[] key, byte[] source)
		{
			return AESHelper.getCipherInstance(key, true).TransformFinalBlock(source, 0, source.Length);
		}

		// Token: 0x06000445 RID: 1093 RVA: 0x000039EB File Offset: 0x00001BEB
		public static byte[] decrypt(byte[] key, byte[] source)
		{
			return AESHelper.getCipherInstance(key, false).TransformFinalBlock(source, 0, source.Length);
		}

		// Token: 0x06000446 RID: 1094 RVA: 0x000179C8 File Offset: 0x00015BC8
		public static byte[] AES_CFB_Decrypt(byte[] key, byte[] data, byte[] iv)
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

		// Token: 0x06000447 RID: 1095 RVA: 0x00017AAC File Offset: 0x00015CAC
		public static byte[] AES_ECB_Encrypt(byte[] key, byte[] data)
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
		
		
		// MCL.CORE
		// Token: 0x06000094 RID: 148 RVA: 0x0000B718 File Offset: 0x00009918
		public static string BytesToHex(byte[] bytes)
		{
			string text = "";
			if (bytes != null)
			{
				for (int i = 0; i < bytes.Length; i++)
				{
					text += bytes[i].ToString("X2");
				}
			}
			return text;
		}

		// Token: 0x06000095 RID: 149 RVA: 0x0000B758 File Offset: 0x00009958
		public static byte[] HexToBytes(string hex)
		{
			return (from x in Enumerable.Range(0, hex.Length)
				where x % 2 == 0
				select Convert.ToByte(hex.Substring(x, 2), 16)).ToArray<byte>();
		}

		// Token: 0x06000096 RID: 150 RVA: 0x0000B7C0 File Offset: 0x000099C0
		public static byte[] GetIv(int n)
		{
			char[] array = new char[]
			{
				'a', 'b', 'd', 'c', 'e', 'f', 'g', 'h', 'i', 'j',
				'k', 'l', 'm', 'n', 'p', 'r', 'q', 's', 't', 'u',
				'v', 'w', 'z', 'y', 'x', '0', '1', '2', '3', '4',
				'5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E',
				'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'Q',
				'P', 'R', 'T', 'S', 'V', 'U', 'W', 'X', 'Y', 'Z'
			};
			StringBuilder stringBuilder = new StringBuilder();
			Random random = new Random(DateTime.Now.Millisecond);
			for (int i = 0; i < n; i++)
			{
				stringBuilder.Append(array[random.Next(0, array.Length)].ToString());
			}
			return Encoding.UTF8.GetBytes(stringBuilder.ToString());
		}

		// Token: 0x06000097 RID: 151 RVA: 0x0000B835 File Offset: 0x00009A35
		public static byte[] GetDefaultIv()
		{
			return Encoding.UTF8.GetBytes("1234567890123456");
		}

		// Token: 0x06000098 RID: 152 RVA: 0x0000B848 File Offset: 0x00009A48
		public static byte[] AESEncryptECB128(byte[] data, byte[] keyBytes, byte[] ivBytes)
		{
			return new RijndaelManaged
			{
				Mode = CipherMode.ECB,
				Padding = PaddingMode.PKCS7,
				KeySize = 128,
				BlockSize = 128,
				Key = keyBytes,
				IV = ivBytes
			}.CreateEncryptor().TransformFinalBlock(data, 0, data.Length);
		}

		// Token: 0x06000099 RID: 153 RVA: 0x0000B89C File Offset: 0x00009A9C
		public static byte[] AESDecryptECB128(byte[] data, byte[] keyBytes, byte[] ivBytes)
		{
			return new RijndaelManaged
			{
				Mode = CipherMode.ECB,
				Padding = PaddingMode.PKCS7,
				KeySize = 128,
				BlockSize = 128,
				Key = keyBytes,
				IV = ivBytes
			}.CreateDecryptor().TransformFinalBlock(data, 0, data.Length);
		}

		// Token: 0x0600009A RID: 154 RVA: 0x0000B8F0 File Offset: 0x00009AF0
		public static byte[] AESEncrypt128Ex(byte[] data, byte[] keyBytes, byte[] ivBytes)
		{
			return new RijndaelManaged
			{
				Mode = CipherMode.CBC,
				Padding = PaddingMode.Zeros,
				KeySize = 128,
				BlockSize = 128,
				Key = keyBytes,
				IV = ivBytes
			}.CreateEncryptor().TransformFinalBlock(data, 0, data.Length);
		}

		// Token: 0x0600009B RID: 155 RVA: 0x0000B944 File Offset: 0x00009B44
		public static byte[] AESDecrypt128Ex(byte[] data, byte[] keyBytes, byte[] ivBytes)
		{
			return new RijndaelManaged
			{
				Mode = CipherMode.CBC,
				Padding = PaddingMode.Zeros,
				KeySize = 128,
				BlockSize = 128,
				Key = keyBytes,
				IV = ivBytes
			}.CreateDecryptor().TransformFinalBlock(data, 0, data.Length);
		}
	}
}
