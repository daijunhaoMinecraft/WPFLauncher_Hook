using System.Reflection;

namespace Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;

public interface IDetour
{
    void Patch(MethodBase rawMethod, MethodBase hookMethod, MethodBase originalMethod);
}