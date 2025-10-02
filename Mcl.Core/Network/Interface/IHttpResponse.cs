using System;
using System.Collections.Generic;
using System.Net;

namespace Mcl.Core.Network.Interface
{
	// Token: 0x0200001E RID: 30
	public interface IHttpResponse
	{
		// Token: 0x1700009F RID: 159
		// (get) Token: 0x060001D5 RID: 469
		// (set) Token: 0x060001D6 RID: 470
		Version ProtocolVersion { get; set; }

		// Token: 0x170000A0 RID: 160
		// (get) Token: 0x060001D7 RID: 471
		// (set) Token: 0x060001D8 RID: 472
		string ContentType { get; set; }

		// Token: 0x170000A1 RID: 161
		// (get) Token: 0x060001D9 RID: 473
		// (set) Token: 0x060001DA RID: 474
		long ContentLength { get; set; }

		// Token: 0x170000A2 RID: 162
		// (get) Token: 0x060001DB RID: 475
		// (set) Token: 0x060001DC RID: 476
		string ContentEncoding { get; set; }

		// Token: 0x170000A3 RID: 163
		// (get) Token: 0x060001DD RID: 477
		string Content { get; }

		// Token: 0x170000A4 RID: 164
		// (get) Token: 0x060001DE RID: 478
		// (set) Token: 0x060001DF RID: 479
		HttpStatusCode StatusCode { get; set; }

		// Token: 0x170000A5 RID: 165
		// (get) Token: 0x060001E0 RID: 480
		// (set) Token: 0x060001E1 RID: 481
		string StatusDescription { get; set; }

		// Token: 0x170000A6 RID: 166
		// (get) Token: 0x060001E2 RID: 482
		// (set) Token: 0x060001E3 RID: 483
		byte[] RawBytes { get; set; }

		// Token: 0x170000A7 RID: 167
		// (get) Token: 0x060001E4 RID: 484
		// (set) Token: 0x060001E5 RID: 485
		Uri ResponseUri { get; set; }

		// Token: 0x170000A8 RID: 168
		// (get) Token: 0x060001E6 RID: 486
		// (set) Token: 0x060001E7 RID: 487
		string Server { get; set; }

		// Token: 0x170000A9 RID: 169
		// (get) Token: 0x060001E8 RID: 488
		IList<HttpHeader> Headers { get; }

		// Token: 0x170000AA RID: 170
		// (get) Token: 0x060001E9 RID: 489
		IList<HttpCookie> Cookies { get; }

		// Token: 0x170000AB RID: 171
		// (get) Token: 0x060001EA RID: 490
		// (set) Token: 0x060001EB RID: 491
		ResponseStatus ResponseStatus { get; set; }

		// Token: 0x170000AC RID: 172
		// (get) Token: 0x060001EC RID: 492
		// (set) Token: 0x060001ED RID: 493
		string ErrorMessage { get; set; }

		// Token: 0x170000AD RID: 173
		// (get) Token: 0x060001EE RID: 494
		// (set) Token: 0x060001EF RID: 495
		Exception ErrorException { get; set; }
	}
}
