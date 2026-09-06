using System;
using System.Reflection;

namespace Mcl.Core.Dotnetdetour
{
	[AttributeUsage(AttributeTargets.Method)]
	public class HookMethodAttribute : Attribute
	{
		// (get) Token: 0x06000026 RID: 38 RVA: 0x000031A6 File Offset: 0x000013A6
		// (set) Token: 0x06000027 RID: 39 RVA: 0x000031AE File Offset: 0x000013AE
		public string TargetTypeFullName { get; private set; }

		// (get) Token: 0x06000028 RID: 40 RVA: 0x000031B7 File Offset: 0x000013B7
		// (set) Token: 0x06000029 RID: 41 RVA: 0x000031BF File Offset: 0x000013BF
		public Type TargetType { get; private set; }

		public string GetTargetMethodName(MethodBase method)
		{
			return (!string.IsNullOrEmpty(this.TargetMethodName)) ? this.TargetMethodName : method.Name;
		}

		public string GetOriginalMethodName(MethodBase method)
		{
			return (!string.IsNullOrEmpty(this.OriginalMethodName)) ? this.OriginalMethodName : (this.GetTargetMethodName(method) + "_Original");
		}

		public HookMethodAttribute(string targetTypeFullName, string targetMethodName = null, string originalMethodName = null)
		{
			this.TargetTypeFullName = targetTypeFullName;
			this.TargetMethodName = targetMethodName;
			this.OriginalMethodName = originalMethodName;
		}

		public HookMethodAttribute(Type targetType, string targetMethodName = null, string originalMethodName = null)
		{
			this.TargetType = targetType;
			this.TargetMethodName = targetMethodName;
			this.OriginalMethodName = originalMethodName;
		}

		private string TargetMethodName;

		private string OriginalMethodName;
	}
}
