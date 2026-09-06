using System;

namespace Mcl.Core.Dotnetdetour.CoreEngine.Attributes;

[Obsolete("此类已变更为OriginalMethodAttribute")]
[AttributeUsage(AttributeTargets.Method)]
public class OriginalAttribute : OriginalMethodAttribute
{
}