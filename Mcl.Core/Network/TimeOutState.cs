using System;
using System.Net;

namespace Mcl.Core.Network
{
	// Token: 0x0200001A RID: 26
	public class TimeOutState
	{
		// Token: 0x17000087 RID: 135
		// (get) Token: 0x06000195 RID: 405 RVA: 0x00005D27 File Offset: 0x00003F27
		// (set) Token: 0x06000196 RID: 406 RVA: 0x00005D2F File Offset: 0x00003F2F
		public bool TimedOut { get; set; }

		// Token: 0x17000088 RID: 136
		// (get) Token: 0x06000197 RID: 407 RVA: 0x00005D38 File Offset: 0x00003F38
		// (set) Token: 0x06000198 RID: 408 RVA: 0x00005D40 File Offset: 0x00003F40
		public HttpWebRequest Request { get; set; }
	}
}
