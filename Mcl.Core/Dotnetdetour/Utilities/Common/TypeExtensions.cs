using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Mcl.Core.Dotnetdetour.Utilities.Common;

public static class TypeExtensions
{
    public static T GetCustomAttribute<T>(this MemberInfo @this)
    {
        var customAttributes = @this.GetCustomAttributes(typeof(T), true);
        IEnumerable<object> enumerable;
        if (customAttributes == null)
        {
            enumerable = null;
        }
        else
        {
            IEnumerable<object> enumerable2 = customAttributes.ToList();
            enumerable = enumerable2;
        }

        return (T)enumerable.FirstOrDefault();
    }

    public static T GetCustomAttribute<T>(this ParameterInfo @this)
    {
        var customAttributes = @this.GetCustomAttributes(typeof(T), true);
        IEnumerable<object> enumerable;
        if (customAttributes == null)
        {
            enumerable = null;
        }
        else
        {
            IEnumerable<object> enumerable2 = customAttributes.ToList();
            enumerable = enumerable2;
        }

        return (T)enumerable.FirstOrDefault();
    }
}