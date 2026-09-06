using System.Reflection;

namespace Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;

public interface IMethodHookWithSet : IMethodHook
{
    void HookMethod(MethodBase method);
}