using System;

namespace Mcl.Core.Network.Interface
{
	// Token: 0x02000022 RID: 34
	public interface INetResponse<T> : INetResponse
	{
		// Token: 0x170000D8 RID: 216
		// (get) Token: 0x06000263 RID: 611
		// (set) Token: 0x06000264 RID: 612
		T Data { get; set; }
	}
}
