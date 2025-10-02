using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace Mcl.Core.Network.Interface
{
	// Token: 0x02000020 RID: 32
	public interface INetRequest
	{
		// Token: 0x170000BB RID: 187
		// (get) Token: 0x06000217 RID: 535
		// (set) Token: 0x06000218 RID: 536
		bool AlwaysMultipartFormData { get; set; }

		// Token: 0x170000BC RID: 188
		// (get) Token: 0x06000219 RID: 537
		// (set) Token: 0x0600021A RID: 538
		Action<Stream> ResponseWriter { get; set; }

		// Token: 0x170000BD RID: 189
		// (get) Token: 0x0600021B RID: 539
		List<Parameter> Parameters { get; }

		// Token: 0x170000BE RID: 190
		// (get) Token: 0x0600021C RID: 540
		List<FileParameter> Files { get; }

		// Token: 0x170000BF RID: 191
		// (get) Token: 0x0600021D RID: 541
		// (set) Token: 0x0600021E RID: 542
		Method Method { get; set; }

		// Token: 0x170000C0 RID: 192
		// (get) Token: 0x0600021F RID: 543
		// (set) Token: 0x06000220 RID: 544
		string Resource { get; set; }

		// Token: 0x170000C1 RID: 193
		// (get) Token: 0x06000221 RID: 545
		// (set) Token: 0x06000222 RID: 546
		ICredentials Credentials { get; set; }

		// Token: 0x170000C2 RID: 194
		// (get) Token: 0x06000223 RID: 547
		// (set) Token: 0x06000224 RID: 548
		int Timeout { get; set; }

		// Token: 0x170000C3 RID: 195
		// (get) Token: 0x06000225 RID: 549
		// (set) Token: 0x06000226 RID: 550
		int ReadWriteTimeout { get; set; }

		// Token: 0x170000C4 RID: 196
		// (get) Token: 0x06000227 RID: 551
		int Attempts { get; }

		// Token: 0x170000C5 RID: 197
		// (get) Token: 0x06000228 RID: 552
		// (set) Token: 0x06000229 RID: 553
		bool UseDefaultCredentials { get; set; }

		// Token: 0x170000C6 RID: 198
		// (get) Token: 0x0600022A RID: 554
		IList<DecompressionMethods> AllowedDecompressionMethods { get; }

		// Token: 0x0600022B RID: 555
		INetRequest AddFile(string name, string path, string contentType = null);

		// Token: 0x0600022C RID: 556
		INetRequest AddFile(string name, byte[] bytes, string fileName, string contentType = null);

		// Token: 0x0600022D RID: 557
		INetRequest AddFile(string name, Action<Stream> writer, string fileName, long contentLength, string contentType = null);

		// Token: 0x0600022E RID: 558
		INetRequest AddFileBytes(string name, byte[] bytes, string filename, string contentType = "application/x-gzip");

		// Token: 0x0600022F RID: 559
		INetRequest AddBody(object obj, string xmlNamespace);

		// Token: 0x06000230 RID: 560
		INetRequest AddBody(object obj);

		// Token: 0x06000231 RID: 561
		INetRequest AddJsonBody(object obj);

		// Token: 0x06000232 RID: 562
		INetRequest AddXmlBody(object obj);

		// Token: 0x06000233 RID: 563
		INetRequest AddXmlBody(object obj, string xmlNamespace);

		// Token: 0x06000234 RID: 564
		INetRequest AddObject(object obj, params string[] includedProperties);

		// Token: 0x06000235 RID: 565
		INetRequest AddObject(object obj);

		// Token: 0x06000236 RID: 566
		INetRequest AddParameter(Parameter p);

		// Token: 0x06000237 RID: 567
		INetRequest AddParameter(string name, object value);

		// Token: 0x06000238 RID: 568
		INetRequest AddParameter(string name, object value, ParameterType type);

		// Token: 0x06000239 RID: 569
		INetRequest AddParameter(string name, object value, string contentType, ParameterType type);

		// Token: 0x0600023A RID: 570
		INetRequest AddOrUpdateParameter(Parameter p);

		// Token: 0x0600023B RID: 571
		INetRequest AddOrUpdateParameter(string name, object value);

		// Token: 0x0600023C RID: 572
		INetRequest AddOrUpdateParameter(string name, object value, ParameterType type);

		// Token: 0x0600023D RID: 573
		INetRequest AddOrUpdateParameter(string name, object value, string contentType, ParameterType type);

		// Token: 0x0600023E RID: 574
		INetRequest AddHeader(string name, string value);

		// Token: 0x0600023F RID: 575
		INetRequest AddCookie(string name, string value);

		// Token: 0x06000240 RID: 576
		INetRequest AddUrlSegment(string name, string value);

		// Token: 0x06000241 RID: 577
		INetRequest AddQueryParameter(string name, string value);

		// Token: 0x06000242 RID: 578
		INetRequest AddDecompressionMethod(DecompressionMethods decompressionMethod);

		// Token: 0x06000243 RID: 579
		void IncreaseNumAttempts();
	}
}
