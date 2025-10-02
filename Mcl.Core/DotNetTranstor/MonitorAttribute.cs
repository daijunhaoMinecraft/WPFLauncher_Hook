using System;

namespace DotNetTranstor
{
	// Token: 0x0200000E RID: 14
	[Obsolete("此类已变更为HookMethodAttribute")]
	[AttributeUsage(AttributeTargets.Method)]
	public class MonitorAttribute : HookMethodAttribute
	{
		// Token: 0x0600002E RID: 46 RVA: 0x0000324D File Offset: 0x0000144D
		public MonitorAttribute(string NamespaceName, string ClassName)
			: base(NamespaceName + "." + ClassName, null, null)
		{
		}

		// Token: 0x0600002F RID: 47 RVA: 0x00003265 File Offset: 0x00001465
		public MonitorAttribute(Type type)
			: base(type, null, null)
		{
		}
	}
}
