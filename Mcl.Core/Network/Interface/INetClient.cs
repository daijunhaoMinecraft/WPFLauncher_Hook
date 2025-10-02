using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Cache;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mcl.Core.Network.Interface
{
	// Token: 0x0200001F RID: 31
	public interface INetClient
	{
		// Token: 0x170000AE RID: 174
		// (get) Token: 0x060001F0 RID: 496
		// (set) Token: 0x060001F1 RID: 497
		CookieContainer CookieContainer { get; set; }

		// Token: 0x170000AF RID: 175
		// (get) Token: 0x060001F2 RID: 498
		// (set) Token: 0x060001F3 RID: 499
		int? MaxRedirects { get; set; }

		// Token: 0x170000B0 RID: 176
		// (get) Token: 0x060001F4 RID: 500
		// (set) Token: 0x060001F5 RID: 501
		string UserAgent { get; set; }

		// Token: 0x170000B1 RID: 177
		// (get) Token: 0x060001F6 RID: 502
		// (set) Token: 0x060001F7 RID: 503
		int Timeout { get; set; }

		// Token: 0x170000B2 RID: 178
		// (get) Token: 0x060001F8 RID: 504
		// (set) Token: 0x060001F9 RID: 505
		int ReadWriteTimeout { get; set; }

		// Token: 0x170000B3 RID: 179
		// (get) Token: 0x060001FA RID: 506
		// (set) Token: 0x060001FB RID: 507
		bool UseSynchronizationContext { get; set; }

		// Token: 0x170000B4 RID: 180
		// (get) Token: 0x060001FC RID: 508
		// (set) Token: 0x060001FD RID: 509
		Uri BaseUrl { get; set; }

		// Token: 0x170000B5 RID: 181
		// (get) Token: 0x060001FE RID: 510
		// (set) Token: 0x060001FF RID: 511
		Encoding Encoding { get; set; }

		// Token: 0x170000B6 RID: 182
		// (get) Token: 0x06000200 RID: 512
		// (set) Token: 0x06000201 RID: 513
		bool PreAuthenticate { get; set; }

		// Token: 0x170000B7 RID: 183
		// (get) Token: 0x06000202 RID: 514
		IList<Parameter> DefaultParameters { get; }

		// Token: 0x06000203 RID: 515
		NetRequestAsyncHandle ExecuteAsync(INetRequest request, Action<INetResponse, NetRequestAsyncHandle> callback);

		// Token: 0x06000204 RID: 516
		INetResponse Execute(INetRequest request);

		// Token: 0x06000205 RID: 517
		byte[] DownloadData(INetRequest request);

		// Token: 0x170000B8 RID: 184
		// (get) Token: 0x06000206 RID: 518
		// (set) Token: 0x06000207 RID: 519
		X509CertificateCollection ClientCertificates { get; set; }

		// Token: 0x170000B9 RID: 185
		// (get) Token: 0x06000208 RID: 520
		// (set) Token: 0x06000209 RID: 521
		RequestCachePolicy CachePolicy { get; set; }

		// Token: 0x170000BA RID: 186
		// (get) Token: 0x0600020A RID: 522
		// (set) Token: 0x0600020B RID: 523
		bool FollowRedirects { get; set; }

		// Token: 0x0600020C RID: 524
		Uri BuildUri(INetRequest request);

		// Token: 0x0600020D RID: 525
		NetRequestAsyncHandle ExecuteAsyncGet(INetRequest request, Action<INetResponse, NetRequestAsyncHandle> callback, string httpMethod);

		// Token: 0x0600020E RID: 526
		NetRequestAsyncHandle ExecuteAsyncPost(INetRequest request, Action<INetResponse, NetRequestAsyncHandle> callback, string httpMethod);

		// Token: 0x0600020F RID: 527
		INetResponse ExecuteAsGet(INetRequest request, string httpMethod);

		// Token: 0x06000210 RID: 528
		INetResponse ExecuteAsPost(INetRequest request, string httpMethod);

		// Token: 0x06000211 RID: 529
		Task<INetResponse> ExecuteTaskAsync(INetRequest request, CancellationToken token);

		// Token: 0x06000212 RID: 530
		Task<INetResponse> ExecuteTaskAsync(INetRequest request);

		// Token: 0x06000213 RID: 531
		Task<INetResponse> ExecuteGetTaskAsync(INetRequest request);

		// Token: 0x06000214 RID: 532
		Task<INetResponse> ExecuteGetTaskAsync(INetRequest request, CancellationToken token);

		// Token: 0x06000215 RID: 533
		Task<INetResponse> ExecutePostTaskAsync(INetRequest request);

		// Token: 0x06000216 RID: 534
		Task<INetResponse> ExecutePostTaskAsync(INetRequest request, CancellationToken token);
	}
}
