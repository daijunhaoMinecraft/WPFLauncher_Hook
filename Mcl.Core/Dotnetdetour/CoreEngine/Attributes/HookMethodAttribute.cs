using System;
using System.Reflection;

namespace Mcl.Core.Dotnetdetour.CoreEngine.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class HookMethodAttribute : Attribute
{
    private readonly string OriginalMethodName;

    private readonly string TargetMethodName;

    public HookMethodAttribute(string targetTypeFullName, string targetMethodName = null,
        string originalMethodName = null)
    {
        TargetTypeFullName = targetTypeFullName;
        TargetMethodName = targetMethodName;
        OriginalMethodName = originalMethodName;
    }

    public HookMethodAttribute(Type targetType, string targetMethodName = null, string originalMethodName = null)
    {
        TargetType = targetType;
        TargetMethodName = targetMethodName;
        OriginalMethodName = originalMethodName;
    }

    public string TargetTypeFullName { get; private set; }

    public Type TargetType { get; private set; }

    public string GetTargetMethodName(MethodBase method)
    {
        return !string.IsNullOrEmpty(TargetMethodName) ? TargetMethodName : method.Name;
    }

    public string GetOriginalMethodName(MethodBase method)
    {
        return !string.IsNullOrEmpty(OriginalMethodName)
            ? OriginalMethodName
            : GetTargetMethodName(method) + "_Original";
    }
}