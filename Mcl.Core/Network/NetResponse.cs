using System;
using System.Collections.Generic;
using System.Diagnostics;
using Mcl.Core.Network.Interface;

namespace Mcl.Core.Network
{
	// Token: 0x02000014 RID: 20
	[DebuggerDisplay("{DebuggerDisplay()}")]
	public class NetResponse : NetResponseBase, INetResponse
	{
		// Token: 0x0600012B RID: 299 RVA: 0x000051F0 File Offset: 0x000033F0
		public NetResponse()
		{
			base.Headers = new List<Parameter>();
		}
	}
}
