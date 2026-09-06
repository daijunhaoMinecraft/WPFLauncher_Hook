using System;

namespace Mcl.Core.Dotnetdetour
{
	[Obsolete("此类已变更为HookMethodAttribute")]
	[AttributeUsage(AttributeTargets.Method)]
	public class MonitorAttribute : HookMethodAttribute
	{
		public MonitorAttribute(string NamespaceName, string ClassName)
			: base(NamespaceName + "." + ClassName, null, null)
		{
		}

		public MonitorAttribute(Type type)
			: base(type, null, null)
		{
		}
	}
}
