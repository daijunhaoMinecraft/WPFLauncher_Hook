using System;
using System.Collections.Generic;
using System.Net;

namespace Mcl.Core.Network.Interface
{
	// Token: 0x02000021 RID: 33
	public interface INetResponse
	{
		// Token: 0x170000C7 RID: 199
		// (get) Token: 0x06000244 RID: 580
		// (set) Token: 0x06000245 RID: 581
		Version ProtocolVersion { get; set; }

		// Token: 0x170000C8 RID: 200
		// (get) Token: 0x06000246 RID: 582
		// (set) Token: 0x06000247 RID: 583
		INetRequest Request { get; set; }

		// Token: 0x170000C9 RID: 201
		// (get) Token: 0x06000248 RID: 584
		// (set) Token: 0x06000249 RID: 585
		string ContentType { get; set; }

		// Token: 0x170000CA RID: 202
		// (get) Token: 0x0600024A RID: 586
		// (set) Token: 0x0600024B RID: 587
		long ContentLength { get; set; }

		// Token: 0x170000CB RID: 203
		// (get) Token: 0x0600024C RID: 588
		// (set) Token: 0x0600024D RID: 589
		string ContentEncoding { get; set; }

		// Token: 0x170000CC RID: 204
		// (get) Token: 0x0600024E RID: 590
		// (set) Token: 0x0600024F RID: 591
		string Content { get; set; }

		// Token: 0x170000CD RID: 205
		// (get) Token: 0x06000250 RID: 592
		// (set) Token: 0x06000251 RID: 593
		HttpStatusCode StatusCode { get; set; }

		// Token: 0x170000CE RID: 206
		// (get) Token: 0x06000252 RID: 594
		bool IsSuccessful { get; }

		// Token: 0x170000CF RID: 207
		// (get) Token: 0x06000253 RID: 595
		// (set) Token: 0x06000254 RID: 596
		string StatusDescription { get; set; }

		// Token: 0x170000D0 RID: 208
		// (get) Token: 0x06000255 RID: 597
		// (set) Token: 0x06000256 RID: 598
		byte[] RawBytes { get; set; }

		// Token: 0x170000D1 RID: 209
		// (get) Token: 0x06000257 RID: 599
		// (set) Token: 0x06000258 RID: 600
		Uri ResponseUri { get; set; }

		// Token: 0x170000D2 RID: 210
		// (get) Token: 0x06000259 RID: 601
		// (set) Token: 0x0600025A RID: 602
		string Server { get; set; }

		// Token: 0x170000D3 RID: 211
		// (get) Token: 0x0600025B RID: 603
		IList<NetResponseCookie> Cookies { get; }

		// Token: 0x170000D4 RID: 212
		// (get) Token: 0x0600025C RID: 604
		IList<Parameter> Headers { get; }

		// Token: 0x170000D5 RID: 213
		// (get) Token: 0x0600025D RID: 605
		// (set) Token: 0x0600025E RID: 606
		ResponseStatus ResponseStatus { get; set; }

		// Token: 0x170000D6 RID: 214
		// (get) Token: 0x0600025F RID: 607
		// (set) Token: 0x06000260 RID: 608
		string ErrorMessage { get; set; }

		// Token: 0x170000D7 RID: 215
		// (get) Token: 0x06000261 RID: 609
		// (set) Token: 0x06000262 RID: 610
		Exception ErrorException { get; set; }
	}
}
