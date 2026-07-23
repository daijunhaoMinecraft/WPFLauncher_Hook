using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Mcl.Core.Dotnetdetour.Utilities.Common;

// Token: 0x0200001A RID: 26
public static class TypeExtensions
{
    // Token: 0x0600004B RID: 75 RVA: 0x000038B4 File Offset: 0x00001AB4
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

    // Token: 0x0600004C RID: 76 RVA: 0x000038F0 File Offset: 0x00001AF0
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