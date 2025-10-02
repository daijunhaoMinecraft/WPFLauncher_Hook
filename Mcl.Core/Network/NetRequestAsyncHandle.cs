using System;
using System.Net;

namespace Mcl.Core.Network
{
	// Token: 0x02000018 RID: 24
	public class NetRequestAsyncHandle
	{
		// Token: 0x06000157 RID: 343 RVA: 0x00005458 File Offset: 0x00003658
		public NetRequestAsyncHandle()
		{
		}

		// Token: 0x06000158 RID: 344 RVA: 0x00005462 File Offset: 0x00003662
		public NetRequestAsyncHandle(HttpWebRequest webRequest)
		{
			this.WebRequest = webRequest;
		}

		// Token: 0x06000159 RID: 345 RVA: 0x00005473 File Offset: 0x00003673
		public void Abort()
		{
			HttpWebRequest webRequest = this.WebRequest;
			if (webRequest != null)
			{
				webRequest.Abort();
			}
		}

		// Token: 0x0400009E RID: 158
		public HttpWebRequest WebRequest;
	}
}
