using System;

namespace Mcl.Core.Network
{
	// Token: 0x02000016 RID: 22
	public class NetResponseCookie
	{
		// Token: 0x17000067 RID: 103
		// (get) Token: 0x06000130 RID: 304 RVA: 0x000052FB File Offset: 0x000034FB
		// (set) Token: 0x06000131 RID: 305 RVA: 0x00005303 File Offset: 0x00003503
		public string Comment { get; set; }

		// Token: 0x17000068 RID: 104
		// (get) Token: 0x06000132 RID: 306 RVA: 0x0000530C File Offset: 0x0000350C
		// (set) Token: 0x06000133 RID: 307 RVA: 0x00005314 File Offset: 0x00003514
		public Uri CommentUri { get; set; }

		// Token: 0x17000069 RID: 105
		// (get) Token: 0x06000134 RID: 308 RVA: 0x0000531D File Offset: 0x0000351D
		// (set) Token: 0x06000135 RID: 309 RVA: 0x00005325 File Offset: 0x00003525
		public bool Discard { get; set; }

		// Token: 0x1700006A RID: 106
		// (get) Token: 0x06000136 RID: 310 RVA: 0x0000532E File Offset: 0x0000352E
		// (set) Token: 0x06000137 RID: 311 RVA: 0x00005336 File Offset: 0x00003536
		public string Domain { get; set; }

		// Token: 0x1700006B RID: 107
		// (get) Token: 0x06000138 RID: 312 RVA: 0x0000533F File Offset: 0x0000353F
		// (set) Token: 0x06000139 RID: 313 RVA: 0x00005347 File Offset: 0x00003547
		public bool Expired { get; set; }

		// Token: 0x1700006C RID: 108
		// (get) Token: 0x0600013A RID: 314 RVA: 0x00005350 File Offset: 0x00003550
		// (set) Token: 0x0600013B RID: 315 RVA: 0x00005358 File Offset: 0x00003558
		public DateTime Expires { get; set; }

		// Token: 0x1700006D RID: 109
		// (get) Token: 0x0600013C RID: 316 RVA: 0x00005361 File Offset: 0x00003561
		// (set) Token: 0x0600013D RID: 317 RVA: 0x00005369 File Offset: 0x00003569
		public bool HttpOnly { get; set; }

		// Token: 0x1700006E RID: 110
		// (get) Token: 0x0600013E RID: 318 RVA: 0x00005372 File Offset: 0x00003572
		// (set) Token: 0x0600013F RID: 319 RVA: 0x0000537A File Offset: 0x0000357A
		public string Name { get; set; }

		// Token: 0x1700006F RID: 111
		// (get) Token: 0x06000140 RID: 320 RVA: 0x00005383 File Offset: 0x00003583
		// (set) Token: 0x06000141 RID: 321 RVA: 0x0000538B File Offset: 0x0000358B
		public string Path { get; set; }

		// Token: 0x17000070 RID: 112
		// (get) Token: 0x06000142 RID: 322 RVA: 0x00005394 File Offset: 0x00003594
		// (set) Token: 0x06000143 RID: 323 RVA: 0x0000539C File Offset: 0x0000359C
		public string Port { get; set; }

		// Token: 0x17000071 RID: 113
		// (get) Token: 0x06000144 RID: 324 RVA: 0x000053A5 File Offset: 0x000035A5
		// (set) Token: 0x06000145 RID: 325 RVA: 0x000053AD File Offset: 0x000035AD
		public bool Secure { get; set; }

		// Token: 0x17000072 RID: 114
		// (get) Token: 0x06000146 RID: 326 RVA: 0x000053B6 File Offset: 0x000035B6
		// (set) Token: 0x06000147 RID: 327 RVA: 0x000053BE File Offset: 0x000035BE
		public DateTime TimeStamp { get; set; }

		// Token: 0x17000073 RID: 115
		// (get) Token: 0x06000148 RID: 328 RVA: 0x000053C7 File Offset: 0x000035C7
		// (set) Token: 0x06000149 RID: 329 RVA: 0x000053CF File Offset: 0x000035CF
		public string Value { get; set; }

		// Token: 0x17000074 RID: 116
		// (get) Token: 0x0600014A RID: 330 RVA: 0x000053D8 File Offset: 0x000035D8
		// (set) Token: 0x0600014B RID: 331 RVA: 0x000053E0 File Offset: 0x000035E0
		public int Version { get; set; }
	}
}
