using System;
using System.Reflection;

namespace DotNetTranstor
{
	// Token: 0x02000004 RID: 4
	public interface IDetour
	{
		// Token: 0x06000010 RID: 16
		void Patch(MethodBase rawMethod, MethodBase hookMethod, MethodBase originalMethod);
	}
}
