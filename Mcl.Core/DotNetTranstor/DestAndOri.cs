using System;
using System.Reflection;

namespace DotNetTranstor
{
	// Token: 0x02000009 RID: 9
	internal class DestAndOri
	{
		// Token: 0x17000001 RID: 1
		// (get) Token: 0x06000018 RID: 24 RVA: 0x00002A96 File Offset: 0x00000C96
		// (set) Token: 0x06000019 RID: 25 RVA: 0x00002A9E File Offset: 0x00000C9E
		public MethodBase HookMethod { get; set; }

		// Token: 0x17000002 RID: 2
		// (get) Token: 0x0600001A RID: 26 RVA: 0x00002AA7 File Offset: 0x00000CA7
		// (set) Token: 0x0600001B RID: 27 RVA: 0x00002AAF File Offset: 0x00000CAF
		public MethodBase OriginalMethod { get; set; }

		// Token: 0x04000016 RID: 22
		public IMethodHook Obj;
	}
}
