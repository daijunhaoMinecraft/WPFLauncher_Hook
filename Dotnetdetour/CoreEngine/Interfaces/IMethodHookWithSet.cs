using System.Reflection;

namespace Mcl.Core.Dotnetdetour
{
	public interface IMethodHookWithSet : IMethodHook
	{
		void HookMethod(MethodBase method);
	}
}
