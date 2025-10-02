using System;
using System.IO;
using Mcl.Core.Network;
using Mcl.Core.Network.Interface;

namespace Mcl.Core.Extensions
{
	// Token: 0x02000024 RID: 36
	public static class MiscExtensions
	{
		// Token: 0x0600026D RID: 621 RVA: 0x00005F5C File Offset: 0x0000415C
		public static byte[] ReadAsBytes(this Stream input)
		{
			byte[] array = new byte[16384];
			byte[] array2;
			using (MemoryStream memoryStream = new MemoryStream())
			{
				int num;
				while ((num = input.Read(array, 0, array.Length)) > 0)
				{
					memoryStream.Write(array, 0, num);
				}
				array2 = memoryStream.ToArray();
			}
			return array2;
		}

		// Token: 0x0600026E RID: 622 RVA: 0x00005FC8 File Offset: 0x000041C8
		public static INetResponse<T> ToAsyncResponse<T>(this INetResponse response)
		{
			return new NetResponse<T>
			{
				ContentEncoding = response.ContentEncoding,
				ContentLength = response.ContentLength,
				ContentType = response.ContentType,
				Cookies = response.Cookies,
				ErrorException = response.ErrorException,
				ErrorMessage = response.ErrorMessage,
				Headers = response.Headers,
				RawBytes = response.RawBytes,
				ResponseStatus = response.ResponseStatus,
				ResponseUri = response.ResponseUri,
				Server = response.Server,
				StatusCode = response.StatusCode,
				StatusDescription = response.StatusDescription
			};
		}
	}
}
