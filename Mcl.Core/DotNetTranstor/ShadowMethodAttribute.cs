using System;

namespace DotNetTranstor
{
	// Token: 0x02000015 RID: 21
	[Obsolete("此类已变更为无参数的OriginalMethodAttribute，此特性因为带参数已无法兼容", true)]
	[AttributeUsage(AttributeTargets.Method)]
	public class ShadowMethodAttribute : OriginalMethodAttribute
	{
		// Token: 0x0600003B RID: 59 RVA: 0x000032DA File Offset: 0x000014DA
		[Obsolete("此类已变更为无参数的OriginalMethodAttribute，此特性因为带参数已无法兼容", true)]
		public ShadowMethodAttribute(string targetTypeName, string methodName)
		{
		}

		// Token: 0x0600003C RID: 60 RVA: 0x000032DA File Offset: 0x000014DA
		[Obsolete("此类已变更为无参数的OriginalMethodAttribute，此特性因为带参数已无法兼容", true)]
		public ShadowMethodAttribute(Type classType, string methodName)
		{
		}
	}
}
