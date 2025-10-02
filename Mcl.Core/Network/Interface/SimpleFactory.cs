using System;

namespace Mcl.Core.Network.Interface
{
	// Token: 0x0200001D RID: 29
	public class SimpleFactory<T> : IHttpFactory where T : IHttp, new()
	{
		// Token: 0x060001D3 RID: 467 RVA: 0x00005D4C File Offset: 0x00003F4C
		public IHttp Create()
		{
			return new T();
		}
	}
}
