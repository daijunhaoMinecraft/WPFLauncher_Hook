using System;
using System.Collections.Generic;
using System.Net;
using Mcl.Core.Extensions;
using Mcl.Core.Network.Interface;

namespace Mcl.Core.Network
{
	// Token: 0x02000013 RID: 19
	public abstract class NetResponseBase
	{
		// Token: 0x06000108 RID: 264 RVA: 0x00005028 File Offset: 0x00003228
		protected NetResponseBase()
		{
			this.ResponseStatus = ResponseStatus.None;
			this.Headers = new List<Parameter>();
			this.Cookies = new List<NetResponseCookie>();
		}

		// Token: 0x17000055 RID: 85
		// (get) Token: 0x06000109 RID: 265 RVA: 0x00005052 File Offset: 0x00003252
		// (set) Token: 0x0600010A RID: 266 RVA: 0x0000505A File Offset: 0x0000325A
		public INetRequest Request { get; set; }

		// Token: 0x17000056 RID: 86
		// (get) Token: 0x0600010B RID: 267 RVA: 0x00005063 File Offset: 0x00003263
		// (set) Token: 0x0600010C RID: 268 RVA: 0x0000506B File Offset: 0x0000326B
		public string ContentType { get; set; }

		// Token: 0x17000057 RID: 87
		// (get) Token: 0x0600010D RID: 269 RVA: 0x00005074 File Offset: 0x00003274
		// (set) Token: 0x0600010E RID: 270 RVA: 0x0000507C File Offset: 0x0000327C
		public long ContentLength { get; set; }

		// Token: 0x17000058 RID: 88
		// (get) Token: 0x0600010F RID: 271 RVA: 0x00005085 File Offset: 0x00003285
		// (set) Token: 0x06000110 RID: 272 RVA: 0x0000508D File Offset: 0x0000328D
		public string ContentEncoding { get; set; }

		// Token: 0x17000059 RID: 89
		// (get) Token: 0x06000111 RID: 273 RVA: 0x00005098 File Offset: 0x00003298
		// (set) Token: 0x06000112 RID: 274 RVA: 0x000050C8 File Offset: 0x000032C8
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
			set
			{
				this.content = value;
			}
		}

		// Token: 0x1700005A RID: 90
		// (get) Token: 0x06000113 RID: 275 RVA: 0x000050D2 File Offset: 0x000032D2
		// (set) Token: 0x06000114 RID: 276 RVA: 0x000050DA File Offset: 0x000032DA
		public HttpStatusCode StatusCode { get; set; }

		// Token: 0x1700005B RID: 91
		// (get) Token: 0x06000115 RID: 277 RVA: 0x000050E3 File Offset: 0x000032E3
		public bool IsSuccessful
		{
			get
			{
				return this.StatusCode >= HttpStatusCode.OK && this.StatusCode <= (HttpStatusCode)299 && this.ResponseStatus == ResponseStatus.Completed;
			}
		}

		// Token: 0x1700005C RID: 92
		// (get) Token: 0x06000116 RID: 278 RVA: 0x0000510B File Offset: 0x0000330B
		// (set) Token: 0x06000117 RID: 279 RVA: 0x00005113 File Offset: 0x00003313
		public string StatusDescription { get; set; }

		// Token: 0x1700005D RID: 93
		// (get) Token: 0x06000118 RID: 280 RVA: 0x0000511C File Offset: 0x0000331C
		// (set) Token: 0x06000119 RID: 281 RVA: 0x00005124 File Offset: 0x00003324
		public byte[] RawBytes { get; set; }

		// Token: 0x1700005E RID: 94
		// (get) Token: 0x0600011A RID: 282 RVA: 0x0000512D File Offset: 0x0000332D
		// (set) Token: 0x0600011B RID: 283 RVA: 0x00005135 File Offset: 0x00003335
		public Uri ResponseUri { get; set; }

		// Token: 0x1700005F RID: 95
		// (get) Token: 0x0600011C RID: 284 RVA: 0x0000513E File Offset: 0x0000333E
		// (set) Token: 0x0600011D RID: 285 RVA: 0x00005146 File Offset: 0x00003346
		public string Server { get; set; }

		// Token: 0x17000060 RID: 96
		// (get) Token: 0x0600011E RID: 286 RVA: 0x0000514F File Offset: 0x0000334F
		// (set) Token: 0x0600011F RID: 287 RVA: 0x00005157 File Offset: 0x00003357
		public IList<NetResponseCookie> Cookies { get; protected internal set; }

		// Token: 0x17000061 RID: 97
		// (get) Token: 0x06000120 RID: 288 RVA: 0x00005160 File Offset: 0x00003360
		// (set) Token: 0x06000121 RID: 289 RVA: 0x00005168 File Offset: 0x00003368
		public IList<Parameter> Headers { get; protected internal set; }

		// Token: 0x17000062 RID: 98
		// (get) Token: 0x06000122 RID: 290 RVA: 0x00005171 File Offset: 0x00003371
		// (set) Token: 0x06000123 RID: 291 RVA: 0x00005179 File Offset: 0x00003379
		public ResponseStatus ResponseStatus { get; set; }

		// Token: 0x17000063 RID: 99
		// (get) Token: 0x06000124 RID: 292 RVA: 0x00005182 File Offset: 0x00003382
		// (set) Token: 0x06000125 RID: 293 RVA: 0x0000518A File Offset: 0x0000338A
		public string ErrorMessage { get; set; }

		// Token: 0x17000064 RID: 100
		// (get) Token: 0x06000126 RID: 294 RVA: 0x00005193 File Offset: 0x00003393
		// (set) Token: 0x06000127 RID: 295 RVA: 0x0000519B File Offset: 0x0000339B
		public Exception ErrorException { get; set; }

		// Token: 0x17000065 RID: 101
		// (get) Token: 0x06000128 RID: 296 RVA: 0x000051A4 File Offset: 0x000033A4
		// (set) Token: 0x06000129 RID: 297 RVA: 0x000051AC File Offset: 0x000033AC
		public Version ProtocolVersion { get; set; }

		// Token: 0x0600012A RID: 298 RVA: 0x000051B8 File Offset: 0x000033B8
		protected string DebuggerDisplay()
		{
			return string.Format("StatusCode: {0}, Content-Type: {1}, Content-Length: {2})", this.StatusCode, this.ContentType, this.ContentLength);
		}

		// Token: 0x0400007B RID: 123
		private string content;
	}
}
