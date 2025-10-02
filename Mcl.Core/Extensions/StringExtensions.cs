using System;
using System.Text;

namespace Mcl.Core.Extensions
{
	// Token: 0x02000026 RID: 38
	public static class StringExtensions
	{
		// Token: 0x06000270 RID: 624 RVA: 0x000060FC File Offset: 0x000042FC
		public static string UrlEncode(this string value)
		{
			return Uri.EscapeDataString(value);
		}

		// Token: 0x06000271 RID: 625 RVA: 0x00006104 File Offset: 0x00004304
		public static string UrlDecode(this string value)
		{
			return Uri.UnescapeDataString(value);
		}

		// Token: 0x06000272 RID: 626 RVA: 0x0000610C File Offset: 0x0000430C
		public static string AsString(this byte[] buffer)
		{
			bool flag = buffer == null;
			string text;
			if (flag)
			{
				text = "";
			}
			else
			{
				Encoding utf = Encoding.UTF8;
				text = utf.GetString(buffer, 0, buffer.Length);
			}
			return text;
		}

		// Token: 0x06000273 RID: 627 RVA: 0x00006140 File Offset: 0x00004340
		public static bool HasValue(this string input)
		{
			return !string.IsNullOrEmpty(input);
		}
	}
}
