using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Mcl.Core.Dotnetdetour.Extensions
{
	public static class TypeExtensions
	{
		public static T GetCustomAttribute<T>(this MemberInfo @this)
		{
			object[] customAttributes = @this.GetCustomAttributes(typeof(T), true);
			IEnumerable<object> enumerable;
			if (customAttributes == null)
			{
				enumerable = null;
			}
			else
			{
				IEnumerable<object> enumerable2 = customAttributes.ToList<object>();
				enumerable = enumerable2;
			}
			return (T)((object)enumerable.FirstOrDefault<object>());
		}

		public static T GetCustomAttribute<T>(this ParameterInfo @this)
		{
			object[] customAttributes = @this.GetCustomAttributes(typeof(T), true);
			IEnumerable<object> enumerable;
			if (customAttributes == null)
			{
				enumerable = null;
			}
			else
			{
				IEnumerable<object> enumerable2 = customAttributes.ToList<object>();
				enumerable = enumerable2;
			}
			return (T)((object)enumerable.FirstOrDefault<object>());
		}
	}
}
