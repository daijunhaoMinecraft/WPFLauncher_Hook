using System;

namespace DotNetTranstor
{
	// Token: 0x02000016 RID: 22
	[Obsolete("此类已变更为RememberTypeAttribute")]
	[AttributeUsage(AttributeTargets.Parameter)]
	public class NonPublicParameterTypeAttribute : RememberTypeAttribute
	{
		// Token: 0x0600003D RID: 61 RVA: 0x000032E4 File Offset: 0x000014E4
		public NonPublicParameterTypeAttribute(string fullName)
			: base(fullName, false)
		{
		}
	}
}
