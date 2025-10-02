using System;
using System.Diagnostics;
using Mcl.Core.Network.Interface;

namespace Mcl.Core.Network
{
	// Token: 0x02000015 RID: 21
	[DebuggerDisplay("{DebuggerDisplay()}")]
	public class NetResponse<T> : NetResponseBase, INetResponse<T>, INetResponse
	{
		// Token: 0x17000066 RID: 102
		// (get) Token: 0x0600012C RID: 300 RVA: 0x00005206 File Offset: 0x00003406
		// (set) Token: 0x0600012D RID: 301 RVA: 0x0000520E File Offset: 0x0000340E
		public T Data { get; set; }

		// Token: 0x0600012E RID: 302 RVA: 0x00005218 File Offset: 0x00003418
		public static explicit operator NetResponse<T>(NetResponse response)
		{
			return new NetResponse<T>
			{
				ContentEncoding = response.ContentEncoding,
				ContentLength = response.ContentLength,
				ContentType = response.ContentType,
				Cookies = response.Cookies,
				ErrorMessage = response.ErrorMessage,
				ErrorException = response.ErrorException,
				Headers = response.Headers,
				RawBytes = response.RawBytes,
				ResponseStatus = response.ResponseStatus,
				ResponseUri = response.ResponseUri,
				Server = response.Server,
				StatusCode = response.StatusCode,
				StatusDescription = response.StatusDescription,
				Request = response.Request,
				ProtocolVersion = response.ProtocolVersion
			};
		}
	}
}
