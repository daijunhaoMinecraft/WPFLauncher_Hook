using System;
using System.IO;
using System.Security.Cryptography;

namespace Noya.LocalServer.Common.Extensions
{
	// Token: 0x02000005 RID: 5
	public static class HashExtensions
	{
		// Token: 0x0600001A RID: 26 RVA: 0x00002C10 File Offset: 0x00000E10
		public static byte[] CompleteMD5FromFile(this MD5 md5, string filePath)
		{
			byte[] array;
			using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				array = md5.ComputeHash(fileStream);
			}
			return array;
		}
	}
}
