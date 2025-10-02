using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Mcl.Core.Network.Interface
{
	// Token: 0x0200001B RID: 27
	public interface IHttp
	{
		// Token: 0x17000089 RID: 137
		// (get) Token: 0x0600019A RID: 410
		// (set) Token: 0x0600019B RID: 411
		Action<Stream> ResponseWriter { get; set; }

		// Token: 0x1700008A RID: 138
		// (get) Token: 0x0600019C RID: 412
		// (set) Token: 0x0600019D RID: 413
		CookieContainer CookieContainer { get; set; }

		// Token: 0x1700008B RID: 139
		// (get) Token: 0x0600019E RID: 414
		// (set) Token: 0x0600019F RID: 415
		ICredentials Credentials { get; set; }

		// Token: 0x1700008C RID: 140
		// (get) Token: 0x060001A0 RID: 416
		// (set) Token: 0x060001A1 RID: 417
		bool AlwaysMultipartFormData { get; set; }

		// Token: 0x1700008D RID: 141
		// (get) Token: 0x060001A2 RID: 418
		// (set) Token: 0x060001A3 RID: 419
		string UserAgent { get; set; }

		// Token: 0x1700008E RID: 142
		// (get) Token: 0x060001A4 RID: 420
		// (set) Token: 0x060001A5 RID: 421
		int Timeout { get; set; }

		// Token: 0x1700008F RID: 143
		// (get) Token: 0x060001A6 RID: 422
		// (set) Token: 0x060001A7 RID: 423
		int ReadWriteTimeout { get; set; }

		// Token: 0x17000090 RID: 144
		// (get) Token: 0x060001A8 RID: 424
		// (set) Token: 0x060001A9 RID: 425
		bool FollowRedirects { get; set; }

		// Token: 0x17000091 RID: 145
		// (get) Token: 0x060001AA RID: 426
		// (set) Token: 0x060001AB RID: 427
		X509CertificateCollection ClientCertificates { get; set; }

		// Token: 0x17000092 RID: 146
		// (get) Token: 0x060001AC RID: 428
		// (set) Token: 0x060001AD RID: 429
		int? MaxRedirects { get; set; }

		// Token: 0x17000093 RID: 147
		// (get) Token: 0x060001AE RID: 430
		// (set) Token: 0x060001AF RID: 431
		bool UseDefaultCredentials { get; set; }

		// Token: 0x17000094 RID: 148
		// (get) Token: 0x060001B0 RID: 432
		// (set) Token: 0x060001B1 RID: 433
		Encoding Encoding { get; set; }

		// Token: 0x17000095 RID: 149
		// (get) Token: 0x060001B2 RID: 434
		IList<HttpHeader> Headers { get; }

		// Token: 0x17000096 RID: 150
		// (get) Token: 0x060001B3 RID: 435
		IList<HttpParameter> Parameters { get; }

		// Token: 0x17000097 RID: 151
		// (get) Token: 0x060001B4 RID: 436
		IList<HttpFile> Files { get; }

		// Token: 0x17000098 RID: 152
		// (get) Token: 0x060001B5 RID: 437
		IList<HttpCookie> Cookies { get; }

		// Token: 0x17000099 RID: 153
		// (get) Token: 0x060001B6 RID: 438
		// (set) Token: 0x060001B7 RID: 439
		string RequestBody { get; set; }

		// Token: 0x1700009A RID: 154
		// (get) Token: 0x060001B8 RID: 440
		// (set) Token: 0x060001B9 RID: 441
		string RequestContentType { get; set; }

		// Token: 0x1700009B RID: 155
		// (get) Token: 0x060001BA RID: 442
		// (set) Token: 0x060001BB RID: 443
		bool PreAuthenticate { get; set; }

		// Token: 0x1700009C RID: 156
		// (get) Token: 0x060001BC RID: 444
		// (set) Token: 0x060001BD RID: 445
		RequestCachePolicy CachePolicy { get; set; }

		// Token: 0x1700009D RID: 157
		// (get) Token: 0x060001BE RID: 446
		// (set) Token: 0x060001BF RID: 447
		byte[] RequestBodyBytes { get; set; }

		// Token: 0x1700009E RID: 158
		// (get) Token: 0x060001C0 RID: 448
		// (set) Token: 0x060001C1 RID: 449
		Uri Url { get; set; }

		// Token: 0x060001C2 RID: 450
		HttpWebRequest DeleteAsync(Action<HttpResponse> action);

		// Token: 0x060001C3 RID: 451
		HttpWebRequest GetAsync(Action<HttpResponse> action);

		// Token: 0x060001C4 RID: 452
		HttpWebRequest HeadAsync(Action<HttpResponse> action);

		// Token: 0x060001C5 RID: 453
		HttpWebRequest PostAsync(Action<HttpResponse> action);

		// Token: 0x060001C6 RID: 454
		HttpWebRequest PutAsync(Action<HttpResponse> action);

		// Token: 0x060001C7 RID: 455
		HttpWebRequest PatchAsync(Action<HttpResponse> action);

		// Token: 0x060001C8 RID: 456
		HttpWebRequest AsPostAsync(Action<HttpResponse> action, string httpMethod);

		// Token: 0x060001C9 RID: 457
		HttpWebRequest AsGetAsync(Action<HttpResponse> action, string httpMethod);

		// Token: 0x060001CA RID: 458
		HttpResponse Delete();

		// Token: 0x060001CB RID: 459
		HttpResponse Get();

		// Token: 0x060001CC RID: 460
		HttpResponse Head();

		// Token: 0x060001CD RID: 461
		HttpResponse Post();

		// Token: 0x060001CE RID: 462
		HttpResponse Put();

		// Token: 0x060001CF RID: 463
		HttpResponse Patch();

		// Token: 0x060001D0 RID: 464
		HttpResponse AsPost(string httpMethod);

		// Token: 0x060001D1 RID: 465
		HttpResponse AsGet(string httpMethod);
	}
}
