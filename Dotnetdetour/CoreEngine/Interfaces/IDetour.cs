using System.Reflection;

namespace Mcl.Core.Dotnetdetour
{
	public interface IDetour
	{
		void Patch(MethodBase rawMethod, MethodBase hookMethod, MethodBase originalMethod);
	}
}
