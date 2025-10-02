using System;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Noya.LocalServer.Common.Extensions
{
	// Token: 0x02000006 RID: 6
	public static class StringExtensions
	{
		// Token: 0x0600001B RID: 27 RVA: 0x00002C4C File Offset: 0x00000E4C
		public static string RandStringRunes(int length)
		{
			string text = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
			Random random = new Random();
			return new string((from s in Enumerable.Repeat<string>(text, length)
				select s[random.Next(s.Length)]).ToArray<char>());
		}

		// Token: 0x0600001C RID: 28 RVA: 0x00002C90 File Offset: 0x00000E90
		public static string RandomLetter(int length)
		{
			Random random = new Random();
			return new string((from s in Enumerable.Repeat<string>("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz", length)
				select s[random.Next(s.Length)]).ToArray<char>());
		}

		// Token: 0x0600001D RID: 29 RVA: 0x00002CD4 File Offset: 0x00000ED4
		public static uint SafeParseToUInt32(this string numStr)
		{
			uint num;
			uint.TryParse(numStr, out num);
			return num;
		}

		// Token: 0x0600001E RID: 30 RVA: 0x00002CEC File Offset: 0x00000EEC
		public static byte[] HexToBytes(this string hex)
		{
			if (string.IsNullOrEmpty(hex))
			{
				return null;
			}
			byte[] array = new byte[hex.Length / 2];
			for (int i = 0; i < array.Length; i++)
			{
				try
				{
					array[i] = byte.Parse(hex.Substring(i * 2, 2), NumberStyles.HexNumber);
				}
				catch (Exception ex)
				{
					throw new FormatException("hex is not a valid hex number!", ex);
				}
			}
			return array;
		}
	}
}
