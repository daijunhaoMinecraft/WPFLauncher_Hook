using System;
using System.Reflection;

namespace DotNetTranstor
{
	// Token: 0x0200000D RID: 13
	[AttributeUsage(AttributeTargets.Method)]
	public class HookMethodAttribute : Attribute
	{
		// Token: 0x17000003 RID: 3
		// (get) Token: 0x06000026 RID: 38 RVA: 0x000031A6 File Offset: 0x000013A6
		// (set) Token: 0x06000027 RID: 39 RVA: 0x000031AE File Offset: 0x000013AE
		public string TargetTypeFullName { get; private set; }

		// Token: 0x17000004 RID: 4
		// (get) Token: 0x06000028 RID: 40 RVA: 0x000031B7 File Offset: 0x000013B7
		// (set) Token: 0x06000029 RID: 41 RVA: 0x000031BF File Offset: 0x000013BF
		public Type TargetType { get; private set; }

		// Token: 0x0600002A RID: 42 RVA: 0x000031C8 File Offset: 0x000013C8
		public string GetTargetMethodName(MethodBase method)
		{
			return (!string.IsNullOrEmpty(this.TargetMethodName)) ? this.TargetMethodName : method.Name;
		}

		// Token: 0x0600002B RID: 43 RVA: 0x000031E5 File Offset: 0x000013E5
		public string GetOriginalMethodName(MethodBase method)
		{
			return (!string.IsNullOrEmpty(this.OriginalMethodName)) ? this.OriginalMethodName : (this.GetTargetMethodName(method) + "_Original");
		}

		// Token: 0x0600002C RID: 44 RVA: 0x0000320D File Offset: 0x0000140D
		public HookMethodAttribute(string targetTypeFullName, string targetMethodName = null, string originalMethodName = null)
		{
			this.TargetTypeFullName = targetTypeFullName;
			this.TargetMethodName = targetMethodName;
			this.OriginalMethodName = originalMethodName;
		}

		// Token: 0x0600002D RID: 45 RVA: 0x0000322D File Offset: 0x0000142D
		public HookMethodAttribute(Type targetType, string targetMethodName = null, string originalMethodName = null)
		{
			this.TargetType = targetType;
			this.TargetMethodName = targetMethodName;
			this.OriginalMethodName = originalMethodName;
		}

		// Token: 0x0400001C RID: 28
		private string TargetMethodName;

		// Token: 0x0400001D RID: 29
		private string OriginalMethodName;
	}
}
