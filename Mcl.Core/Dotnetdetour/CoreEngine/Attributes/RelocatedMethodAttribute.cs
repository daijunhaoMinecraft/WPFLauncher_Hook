using System;

namespace Mcl.Core.Dotnetdetour.CoreEngine.Attributes;

[Obsolete("此类已变更为HookMethodAttribute")]
[AttributeUsage(AttributeTargets.Method)]
public class RelocatedMethodAttribute : HookMethodAttribute
{
    public RelocatedMethodAttribute(string targetTypeFullName, string targetMethodName)
        : base(targetTypeFullName, targetMethodName)
    {
    }

    public RelocatedMethodAttribute(Type type, string targetMethodName)
        : base(type, targetMethodName)
    {
    }
}