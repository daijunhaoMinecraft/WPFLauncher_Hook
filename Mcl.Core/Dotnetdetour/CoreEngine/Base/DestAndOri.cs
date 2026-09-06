using System.Reflection;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;

namespace Mcl.Core.Dotnetdetour.CoreEngine.Base;

internal class DestAndOri
{
    public IMethodHook Obj;

    public MethodBase HookMethod { get; set; }

    public MethodBase OriginalMethod { get; set; }
}