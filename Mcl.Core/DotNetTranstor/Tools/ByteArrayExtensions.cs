using System;
using System.Text;

namespace Noya.LocalServer.Common.Extensions
{
	// Token: 0x02000004 RID: 4
	public static class ByteArrayExtensions
	{
		// Token: 0x06000019 RID: 25 RVA: 0x00002BB4 File Offset: 0x00000DB4
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
}
