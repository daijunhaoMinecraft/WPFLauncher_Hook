using System;

namespace Mcl.Core.Dotnetdetour
{
	[Obsolete("此类已变更为OriginalMethodAttribute")]
	[AttributeUsage(AttributeTargets.Method)]
	public class OriginalAttribute : OriginalMethodAttribute
	{
	}
}
