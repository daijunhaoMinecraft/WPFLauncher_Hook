using System;

namespace Mcl.Core.Dotnetdetour.CoreEngine.Attributes;

[Obsolete("此类已变更为RememberTypeAttribute")]
[AttributeUsage(AttributeTargets.Parameter)]
public class NonPublicParameterTypeAttribute : RememberTypeAttribute
{
    public NonPublicParameterTypeAttribute(string fullName)
        : base(fullName)
    {
    }
}