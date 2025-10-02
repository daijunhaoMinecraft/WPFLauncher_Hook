using System;

namespace JS4399MC
{
	// Token: 0x0200006C RID: 108
	public class JS4399Result
	{
		// Token: 0x170000F1 RID: 241
		// (get) Token: 0x060005D6 RID: 1494 RVA: 0x00003F9F File Offset: 0x0000219F
		// (set) Token: 0x060005D7 RID: 1495 RVA: 0x0001D00C File Offset: 0x0001B20C
		public bool Success { get; set; }

		// Token: 0x170000F2 RID: 242
		// (get) Token: 0x060005D8 RID: 1496 RVA: 0x00003FA7 File Offset: 0x000021A7
		// (set) Token: 0x060005D9 RID: 1497 RVA: 0x0001D020 File Offset: 0x0001B220
		public string Message { get; set; } = "";

		// Token: 0x170000F3 RID: 243
		// (get) Token: 0x060005DA RID: 1498 RVA: 0x00003FAF File Offset: 0x000021AF
		// (set) Token: 0x060005DB RID: 1499 RVA: 0x0001D034 File Offset: 0x0001B234
		public string Username { get; set; }

		// Token: 0x170000F4 RID: 244
		// (get) Token: 0x060005DC RID: 1500 RVA: 0x00003FB7 File Offset: 0x000021B7
		// (set) Token: 0x060005DD RID: 1501 RVA: 0x0001D048 File Offset: 0x0001B248
		public string Password { get; set; }

		// Token: 0x170000F5 RID: 245
		// (get) Token: 0x060005DE RID: 1502 RVA: 0x00003FBF File Offset: 0x000021BF
		// (set) Token: 0x060005DF RID: 1503 RVA: 0x0001D05C File Offset: 0x0001B25C
		public string SauthJson { get; set; }

		// Token: 0x170000F6 RID: 246
		// (get) Token: 0x060005E0 RID: 1504 RVA: 0x00003FC7 File Offset: 0x000021C7
		// (set) Token: 0x060005E1 RID: 1505 RVA: 0x0001D070 File Offset: 0x0001B270
		public string SauthJsonValue { get; set; }
	}
}
