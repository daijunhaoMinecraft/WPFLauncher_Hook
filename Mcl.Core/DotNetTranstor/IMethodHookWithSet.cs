using System;
using System.Reflection;

namespace DotNetTranstor
{
	// Token: 0x02000007 RID: 7
	public interface IMethodHookWithSet : IMethodHook
	{
		// Token: 0x06000011 RID: 17
		void HookMethod(MethodBase method);
	}
}
