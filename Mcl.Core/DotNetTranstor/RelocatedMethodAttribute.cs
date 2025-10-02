using System;

namespace DotNetTranstor
{
	// Token: 0x0200000F RID: 15
	[Obsolete("此类已变更为HookMethodAttribute")]
	[AttributeUsage(AttributeTargets.Method)]
	public class RelocatedMethodAttribute : HookMethodAttribute
	{
		// Token: 0x06000030 RID: 48 RVA: 0x00003272 File Offset: 0x00001472
		public RelocatedMethodAttribute(string targetTypeFullName, string targetMethodName)
			: base(targetTypeFullName, targetMethodName, null)
		{
		}

		// Token: 0x06000031 RID: 49 RVA: 0x0000327F File Offset: 0x0000147F
		public RelocatedMethodAttribute(Type type, string targetMethodName)
			: base(type, targetMethodName, null)
		{
		}
	}
}
