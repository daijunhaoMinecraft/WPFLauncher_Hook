using System;
using System.IO;

namespace Mcl.Core.Network
{
	// Token: 0x0200000C RID: 12
	public class HttpFile
	{
		// Token: 0x1700002E RID: 46
		// (get) Token: 0x06000098 RID: 152 RVA: 0x00003BBF File Offset: 0x00001DBF
		// (set) Token: 0x06000099 RID: 153 RVA: 0x00003BC7 File Offset: 0x00001DC7
		public long ContentLength { get; set; }

		// Token: 0x1700002F RID: 47
		// (get) Token: 0x0600009A RID: 154 RVA: 0x00003BD0 File Offset: 0x00001DD0
		// (set) Token: 0x0600009B RID: 155 RVA: 0x00003BD8 File Offset: 0x00001DD8
		public Action<Stream> Writer { get; set; }

		// Token: 0x17000030 RID: 48
		// (get) Token: 0x0600009C RID: 156 RVA: 0x00003BE1 File Offset: 0x00001DE1
		// (set) Token: 0x0600009D RID: 157 RVA: 0x00003BE9 File Offset: 0x00001DE9
		public string FileName { get; set; }

		// Token: 0x17000031 RID: 49
		// (get) Token: 0x0600009E RID: 158 RVA: 0x00003BF2 File Offset: 0x00001DF2
		// (set) Token: 0x0600009F RID: 159 RVA: 0x00003BFA File Offset: 0x00001DFA
		public string ContentType { get; set; }

		// Token: 0x17000032 RID: 50
		// (get) Token: 0x060000A0 RID: 160 RVA: 0x00003C03 File Offset: 0x00001E03
		// (set) Token: 0x060000A1 RID: 161 RVA: 0x00003C0B File Offset: 0x00001E0B
		public string Name { get; set; }
	}
}
