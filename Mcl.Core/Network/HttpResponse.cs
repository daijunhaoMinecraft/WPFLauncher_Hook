using System;
using System.Collections.Generic;
using System.Net;
using Mcl.Core.Extensions;
using Mcl.Core.Network.Interface;

namespace Mcl.Core.Network
{
	// Token: 0x02000011 RID: 17
	public class HttpResponse : IHttpResponse
	{
		// Token: 0x17000038 RID: 56
		// (get) Token: 0x060000B0 RID: 176 RVA: 0x00003C69 File Offset: 0x00001E69
		// (set) Token: 0x060000B1 RID: 177 RVA: 0x00003C71 File Offset: 0x00001E71
		public Version ProtocolVersion { get; set; }

		// Token: 0x060000B2 RID: 178 RVA: 0x00003C7A File Offset: 0x00001E7A
		public HttpResponse()
		{
			this.ResponseStatus = ResponseStatus.None;
			this.Headers = new List<HttpHeader>();
			this.Cookies = new List<HttpCookie>();
		}

		// Token: 0x17000039 RID: 57
		// (get) Token: 0x060000B3 RID: 179 RVA: 0x00003CA4 File Offset: 0x00001EA4
		// (set) Token: 0x060000B4 RID: 180 RVA: 0x00003CAC File Offset: 0x00001EAC
		public string ContentType { get; set; }

		// Token: 0x1700003A RID: 58
		// (get) Token: 0x060000B5 RID: 181 RVA: 0x00003CB5 File Offset: 0x00001EB5
		// (set) Token: 0x060000B6 RID: 182 RVA: 0x00003CBD File Offset: 0x00001EBD
		public long ContentLength { get; set; }

		// Token: 0x1700003B RID: 59
		// (get) Token: 0x060000B7 RID: 183 RVA: 0x00003CC6 File Offset: 0x00001EC6
		// (set) Token: 0x060000B8 RID: 184 RVA: 0x00003CCE File Offset: 0x00001ECE
		public string ContentEncoding { get; set; }

		// Token: 0x1700003C RID: 60
		// (get) Token: 0x060000B9 RID: 185 RVA: 0x00003CD8 File Offset: 0x00001ED8
		public string Content
		{
			get
			{
				string text;
				if ((text = this.content) == null)
				{
					text = (this.content = this.RawBytes.AsString());
				}
				return text;
			}
		}

		// Token: 0x1700003D RID: 61
		// (get) Token: 0x060000BA RID: 186 RVA: 0x00003D08 File Offset: 0x00001F08
		// (set) Token: 0x060000BB RID: 187 RVA: 0x00003D10 File Offset: 0x00001F10
		public HttpStatusCode StatusCode { get; set; }

		// Token: 0x1700003E RID: 62
		// (get) Token: 0x060000BC RID: 188 RVA: 0x00003D19 File Offset: 0x00001F19
		// (set) Token: 0x060000BD RID: 189 RVA: 0x00003D21 File Offset: 0x00001F21
		public string StatusDescription { get; set; }

		// Token: 0x1700003F RID: 63
		// (get) Token: 0x060000BE RID: 190 RVA: 0x00003D2A File Offset: 0x00001F2A
		// (set) Token: 0x060000BF RID: 191 RVA: 0x00003D32 File Offset: 0x00001F32
		public byte[] RawBytes { get; set; }

		// Token: 0x17000040 RID: 64
		// (get) Token: 0x060000C0 RID: 192 RVA: 0x00003D3B File Offset: 0x00001F3B
		// (set) Token: 0x060000C1 RID: 193 RVA: 0x00003D43 File Offset: 0x00001F43
		public Uri ResponseUri { get; set; }

		// Token: 0x17000041 RID: 65
		// (get) Token: 0x060000C2 RID: 194 RVA: 0x00003D4C File Offset: 0x00001F4C
		// (set) Token: 0x060000C3 RID: 195 RVA: 0x00003D54 File Offset: 0x00001F54
		public string Server { get; set; }

		// Token: 0x17000042 RID: 66
		// (get) Token: 0x060000C4 RID: 196 RVA: 0x00003D5D File Offset: 0x00001F5D
		// (set) Token: 0x060000C5 RID: 197 RVA: 0x00003D65 File Offset: 0x00001F65
		public IList<HttpHeader> Headers { get; private set; }

		// Token: 0x17000043 RID: 67
		// (get) Token: 0x060000C6 RID: 198 RVA: 0x00003D6E File Offset: 0x00001F6E
		// (set) Token: 0x060000C7 RID: 199 RVA: 0x00003D76 File Offset: 0x00001F76
		public IList<HttpCookie> Cookies { get; private set; }

		// Token: 0x17000044 RID: 68
		// (get) Token: 0x060000C8 RID: 200 RVA: 0x00003D7F File Offset: 0x00001F7F
		// (set) Token: 0x060000C9 RID: 201 RVA: 0x00003D87 File Offset: 0x00001F87
		public ResponseStatus ResponseStatus { get; set; }

		// Token: 0x17000045 RID: 69
		// (get) Token: 0x060000CA RID: 202 RVA: 0x00003D90 File Offset: 0x00001F90
		// (set) Token: 0x060000CB RID: 203 RVA: 0x00003D98 File Offset: 0x00001F98
		public string ErrorMessage { get; set; }

		// Token: 0x17000046 RID: 70
		// (get) Token: 0x060000CC RID: 204 RVA: 0x00003DA1 File Offset: 0x00001FA1
		// (set) Token: 0x060000CD RID: 205 RVA: 0x00003DA9 File Offset: 0x00001FA9
		public Exception ErrorException { get; set; }

		// Token: 0x0400005B RID: 91
		private string content;
	}
}
