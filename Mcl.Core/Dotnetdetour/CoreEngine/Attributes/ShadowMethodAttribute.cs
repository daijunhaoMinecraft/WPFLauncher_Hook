using System;

namespace Mcl.Core.Dotnetdetour.CoreEngine.Attributes;

[Obsolete("此类已变更为无参数的OriginalMethodAttribute，此特性因为带参数已无法兼容", true)]
[AttributeUsage(AttributeTargets.Method)]
public class ShadowMethodAttribute : OriginalMethodAttribute
{
    [Obsolete("此类已变更为无参数的OriginalMethodAttribute，此特性因为带参数已无法兼容", true)]
    public ShadowMethodAttribute(string targetTypeName, string methodName)
    {
    }

    [Obsolete("此类已变更为无参数的OriginalMethodAttribute，此特性因为带参数已无法兼容", true)]
    public ShadowMethodAttribute(Type classType, string methodName)
    {
    }
}