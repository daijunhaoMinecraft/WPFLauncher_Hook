using System;

namespace Mcl.Core.Network
{
	// Token: 0x0200000B RID: 11
	public class HttpCookie
	{
		// Token: 0x17000020 RID: 32
		// (get) Token: 0x0600007B RID: 123 RVA: 0x00003AD1 File Offset: 0x00001CD1
		// (set) Token: 0x0600007C RID: 124 RVA: 0x00003AD9 File Offset: 0x00001CD9
		public string Comment { get; set; }

		// Token: 0x17000021 RID: 33
		// (get) Token: 0x0600007D RID: 125 RVA: 0x00003AE2 File Offset: 0x00001CE2
		// (set) Token: 0x0600007E RID: 126 RVA: 0x00003AEA File Offset: 0x00001CEA
		public Uri CommentUri { get; set; }

		// Token: 0x17000022 RID: 34
		// (get) Token: 0x0600007F RID: 127 RVA: 0x00003AF3 File Offset: 0x00001CF3
		// (set) Token: 0x06000080 RID: 128 RVA: 0x00003AFB File Offset: 0x00001CFB
		public bool Discard { get; set; }

		// Token: 0x17000023 RID: 35
		// (get) Token: 0x06000081 RID: 129 RVA: 0x00003B04 File Offset: 0x00001D04
		// (set) Token: 0x06000082 RID: 130 RVA: 0x00003B0C File Offset: 0x00001D0C
		public string Domain { get; set; }

		// Token: 0x17000024 RID: 36
		// (get) Token: 0x06000083 RID: 131 RVA: 0x00003B15 File Offset: 0x00001D15
		// (set) Token: 0x06000084 RID: 132 RVA: 0x00003B1D File Offset: 0x00001D1D
		public bool Expired { get; set; }

		// Token: 0x17000025 RID: 37
		// (get) Token: 0x06000085 RID: 133 RVA: 0x00003B26 File Offset: 0x00001D26
		// (set) Token: 0x06000086 RID: 134 RVA: 0x00003B2E File Offset: 0x00001D2E
		public DateTime Expires { get; set; }

		// Token: 0x17000026 RID: 38
		// (get) Token: 0x06000087 RID: 135 RVA: 0x00003B37 File Offset: 0x00001D37
		// (set) Token: 0x06000088 RID: 136 RVA: 0x00003B3F File Offset: 0x00001D3F
		public bool HttpOnly { get; set; }

		// Token: 0x17000027 RID: 39
		// (get) Token: 0x06000089 RID: 137 RVA: 0x00003B48 File Offset: 0x00001D48
		// (set) Token: 0x0600008A RID: 138 RVA: 0x00003B50 File Offset: 0x00001D50
		public string Name { get; set; }

		// Token: 0x17000028 RID: 40
		// (get) Token: 0x0600008B RID: 139 RVA: 0x00003B59 File Offset: 0x00001D59
		// (set) Token: 0x0600008C RID: 140 RVA: 0x00003B61 File Offset: 0x00001D61
		public string Path { get; set; }

		// Token: 0x17000029 RID: 41
		// (get) Token: 0x0600008D RID: 141 RVA: 0x00003B6A File Offset: 0x00001D6A
		// (set) Token: 0x0600008E RID: 142 RVA: 0x00003B72 File Offset: 0x00001D72
		public string Port { get; set; }

		// Token: 0x1700002A RID: 42
		// (get) Token: 0x0600008F RID: 143 RVA: 0x00003B7B File Offset: 0x00001D7B
		// (set) Token: 0x06000090 RID: 144 RVA: 0x00003B83 File Offset: 0x00001D83
		public bool Secure { get; set; }

		// Token: 0x1700002B RID: 43
		// (get) Token: 0x06000091 RID: 145 RVA: 0x00003B8C File Offset: 0x00001D8C
		// (set) Token: 0x06000092 RID: 146 RVA: 0x00003B94 File Offset: 0x00001D94
		public DateTime TimeStamp { get; set; }

		// Token: 0x1700002C RID: 44
		// (get) Token: 0x06000093 RID: 147 RVA: 0x00003B9D File Offset: 0x00001D9D
		// (set) Token: 0x06000094 RID: 148 RVA: 0x00003BA5 File Offset: 0x00001DA5
		public string Value { get; set; }

		// Token: 0x1700002D RID: 45
		// (get) Token: 0x06000095 RID: 149 RVA: 0x00003BAE File Offset: 0x00001DAE
		// (set) Token: 0x06000096 RID: 150 RVA: 0x00003BB6 File Offset: 0x00001DB6
		public int Version { get; set; }
	}
}
