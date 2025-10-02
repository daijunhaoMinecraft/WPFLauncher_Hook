using System;
using System.Net;

namespace JS4399MC
{
	// Token: 0x0200006B RID: 107
	public class JS4399HttpConfig
	{
		// Token: 0x170000EF RID: 239
		// (get) Token: 0x060005D0 RID: 1488 RVA: 0x00003F8F File Offset: 0x0000218F
		// (set) Token: 0x060005D1 RID: 1489 RVA: 0x0001CFBC File Offset: 0x0001B1BC
		public WebProxy Proxy { get; set; }

		// Token: 0x170000F0 RID: 240
		// (get) Token: 0x060005D2 RID: 1490 RVA: 0x00003F97 File Offset: 0x00002197
		// (set) Token: 0x060005D3 RID: 1491 RVA: 0x0001CFD0 File Offset: 0x0001B1D0
		public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10.0);
	}
}
