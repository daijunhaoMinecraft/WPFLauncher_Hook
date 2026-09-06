using System;

namespace Mcl.Core.Dotnetdetour.CoreEngine.Attributes;

[AttributeUsage(AttributeTargets.Parameter)]
public class RememberTypeAttribute : Attribute
{
    public RememberTypeAttribute(string fullName = null, bool isGeneric = false)
    {
        TypeFullNameOrNull = fullName;
        IsGeneric = isGeneric;
    }

    public string TypeFullNameOrNull { get; private set; }

    public bool IsGeneric { get; private set; }
}