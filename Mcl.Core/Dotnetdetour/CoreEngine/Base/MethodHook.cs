using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;

namespace Mcl.Core.Dotnetdetour.CoreEngine.Base;

public class MethodHook
{
    public static BindingFlags AllFlag =
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static;

    private static bool installed;
    private static readonly List<DestAndOri> destAndOris = new();

    /// <summary>
    ///     精确安装指定的 Hook 类 (不传则安装全部)
    /// </summary>
    public static void InstallTypes(Type[] specificHookTypes = null)
    {
        if (installed && specificHookTypes == null) return;
        if (specificHookTypes == null) installed = true;

        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        IEnumerable<IMethodHook> monitors;

        // 核心修改：如果传了特定类型，只实例化这些特定的 Hook 类
        if (specificHookTypes != null)
            monitors = specificHookTypes.Select(t => (IMethodHook)Activator.CreateInstance(t));
        else
            monitors = new[] { Assembly.GetExecutingAssembly() }
                .SelectMany(t => t.GetImplementedObjectsByInterface<IMethodHook>());

        foreach (var monitor in monitors)
        {
            // 如果这个 monitor 已经被解析过了，就跳过防止重复
            if (destAndOris.Any(d => d.Obj.GetType() == monitor.GetType())) continue;

            var all = monitor.GetType().GetMethods(AllFlag);
            var hookMethods = all.Where(t =>
                t.CustomAttributes.Any(a => typeof(HookMethodAttribute).IsAssignableFrom(a.AttributeType)));
            var originalMethods = all.Where(t =>
                    t.CustomAttributes.Any(a => typeof(OriginalMethodAttribute).IsAssignableFrom(a.AttributeType)))
                .ToArray();

            var destCount = hookMethods.Count();
            foreach (var hookMethod in hookMethods)
            {
                var destAndOri = new DestAndOri();
                destAndOri.Obj = monitor;
                destAndOri.HookMethod = hookMethod;
                if (destCount == 1)
                {
                    destAndOri.OriginalMethod = originalMethods.FirstOrDefault();
                }
                else
                {
                    var originalMethodName = hookMethod.GetCustomAttribute<HookMethodAttribute>()
                        .GetOriginalMethodName(hookMethod);
                    destAndOri.OriginalMethod = FindMethod(originalMethods, originalMethodName, hookMethod, assemblies);
                }

                destAndOris.Add(destAndOri);
            }
        }

        // InstallInternal 保持你最原始的代码即可，不需要做任何拦截
        InstallInternal(true, assemblies);
    }

    /// <summary>
    ///     安装监视器
    /// </summary>
    public static void Install(string dir = null)
    {
        if (installed)
            return;
        installed = true;
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        IEnumerable<IMethodHook> monitors;
        if (string.IsNullOrEmpty(dir))
        {
            // 只扫描当前程序集(Mcl.Core.dll)中的IMethodHook实现
            // 避免对目标程序集(WPFLauncher.exe)调用GetTypes()导致过早触发静态构造函数
            // (如tb类的static readonly字段初始化, 在模块初始化阶段依赖的状态尚未就绪)
            monitors = new[] { Assembly.GetExecutingAssembly() }
                .SelectMany(t => t.GetImplementedObjectsByInterface<IMethodHook>());
        }
        else
        {
            assemblies = assemblies.Concat(Directory
                    .GetFiles(dir, "*.dll")
                    .Select(d =>
                    {
                        try
                        {
                            return Assembly.LoadFrom(d);
                        }
                        catch
                        {
                            return null;
                        }
                    })
                    .Where(x => x != null))
                .Distinct()
                .ToArray();
            monitors = assemblies
                .SelectMany(d => d.GetImplementedObjectsByInterface<IMethodHook>());
        }

        foreach (var monitor in monitors)
        {
            var all = monitor.GetType().GetMethods(AllFlag);
            var hookMethods = all.Where(t =>
                t.CustomAttributes.Any(a => typeof(HookMethodAttribute).IsAssignableFrom(a.AttributeType)));
            var originalMethods = all.Where(t =>
                    t.CustomAttributes.Any(a => typeof(OriginalMethodAttribute).IsAssignableFrom(a.AttributeType)))
                .ToArray();

            var destCount = hookMethods.Count();
            foreach (var hookMethod in hookMethods)
            {
                var destAndOri = new DestAndOri();
                destAndOri.Obj = monitor;
                destAndOri.HookMethod = hookMethod;
                if (destCount == 1)
                {
                    destAndOri.OriginalMethod = originalMethods.FirstOrDefault();
                }
                else
                {
                    var originalMethodName = hookMethod.GetCustomAttribute<HookMethodAttribute>()
                        .GetOriginalMethodName(hookMethod);

                    destAndOri.OriginalMethod = FindMethod(originalMethods, originalMethodName, hookMethod, assemblies);
                }

                destAndOris.Add(destAndOri);
            }
        }

        InstallInternal(true, assemblies);
        AppDomain.CurrentDomain.AssemblyLoad += CurrentDomain_AssemblyLoad;
    }

    private static void InstallInternal(bool isInstall, Assembly[] assemblies)
    {
        foreach (var detour in destAndOris)
        {
            var hookMethod = detour.HookMethod;
            var hookMethodAttribute = hookMethod.GetCustomAttribute<HookMethodAttribute>();

            //获取当前程序集中的基础类型
            var typeName = hookMethodAttribute.TargetTypeFullName;
            if (hookMethodAttribute.TargetType != null) typeName = hookMethodAttribute.TargetType.FullName;
            var type = TypeResolver(typeName, assemblies);
            if (type != null && !assemblies.Contains(type.Assembly)) type = null;

            //获取方法
            var methodName = hookMethodAttribute.GetTargetMethodName(hookMethod);
            MethodBase rawMethod = null;
            if (type != null)
            {
                MethodBase[] methods;

                if (methodName == type.Name || methodName == ".ctor") //构造方法
                {
                    methods = type.GetConstructors(AllFlag);
                    methodName = ".ctor";
                }
                else
                {
                    methods = type.GetMethods(AllFlag);
                }

                rawMethod = FindMethod(methods, methodName, hookMethod, assemblies);
            }

            if (rawMethod != null && rawMethod.IsGenericMethod)
                // 处理泛型方法
                rawMethod = ((MethodInfo)rawMethod).MakeGenericMethod(hookMethod.GetParameters().Select(o =>
                {
                    var rt = o.ParameterType;
                    var attr = o.GetCustomAttribute<RememberTypeAttribute>();
                    if (attr != null && attr.TypeFullNameOrNull != null)
                        rt = TypeResolver(attr.TypeFullNameOrNull, assemblies);
                    return rt;
                }).ToArray());

            if (rawMethod == null)
            {
                if (isInstall)
                    Debug.WriteLine("没有找到与试图Hook的方法\"{0}, {1}\"匹配的目标方法.", hookMethod.ReflectedType.FullName,
                        hookMethod);
                continue;
            }

            if (detour.Obj is IMethodHookWithSet) ((IMethodHookWithSet)detour.Obj).HookMethod(rawMethod);

            var originalMethod = detour.OriginalMethod;
            var engine = DetourFactory.CreateDetourEngine();
            engine.Patch(rawMethod, hookMethod, originalMethod);
            // Console.WriteLine($"[Detour] Hook成功! Target:{rawMethod.DeclaringType.Name}.{rawMethod.Name} -> Hook:{hookMethod.Name}");
            Debug.WriteLine("已将目标方法 \"{0}, {1}\" 的调用指向 \"{2}, {3}\" Ori: \"{4}\".", rawMethod.ReflectedType.FullName,
                rawMethod
                , hookMethod.ReflectedType.FullName, hookMethod
                , originalMethod == null ? " (无)" : originalMethod.ToString());
        }
    }

    private static Type TypeResolver(string typeName, Assembly[] assemblies)
    {
        return Type.GetType(typeName, null, (a, b, c) =>
        {
            Type rt;
            if (a != null)
            {
                rt = a.GetType(b);
                if (rt != null) return rt;
            }

            rt = Type.GetType(b);
            if (rt != null) return rt;
            foreach (var asm in assemblies)
            {
                rt = asm.GetType(b);
                if (rt != null) return rt;
            }

            return null;
        });
    }

    // 查找匹配函数
    // private static MethodBase FindMethod(MethodBase[] methods, string name, MethodBase like, Assembly[] assemblies)
    // {
    //     var likeParams = like.GetParameters();
    //     foreach (var item in methods)
    //     {
    //         if (item.Name != name)
    //         {
    //             continue;
    //         }
    //
    //         var paramArr = item.GetParameters();
    //         var len = paramArr.Count();
    //         if (len != likeParams.Count())
    //         {
    //             continue;
    //         }
    //
    //         for (var i = 0; i < len; i++)
    //         {
    //             var t1 = likeParams[i];
    //             var t2 = paramArr[i];
    //             //类型相同 或者 fullname都为null的泛型参数
    //             if (t1.ParameterType.FullName == t2.ParameterType.FullName)
    //             {
    //                 continue;
    //             }
    //
    //             //手动保持的类型
    //             var rmtype = t1.GetCustomAttribute<RememberTypeAttribute>();
    //             if (rmtype != null)
    //             {
    //                 //泛型参数
    //                 if (rmtype.IsGeneric && t2.ParameterType.FullName == null)
    //                 {
    //                     continue;
    //                 }
    //                 //查找实际类型
    //                 if (rmtype.TypeFullNameOrNull != null)
    //                 {
    //                     if (rmtype.TypeFullNameOrNull == t2.ParameterType.FullName)
    //                     {
    //                         continue;
    //                     }
    //
    //                     var type = TypeResolver(rmtype.TypeFullNameOrNull, assemblies);
    //                     if (type == t2.ParameterType)
    //                     {
    //                         continue;
    //                     }
    //                 }
    //             }
    //             goto next;
    //         }
    //         return item;
    //     next:
    //         continue;
    //     }
    //     return null;
    // }

    // Fix By Gemini AI
    private static MethodBase FindMethod(MethodBase[] methods, string name, MethodBase like, Assembly[] assemblies)
    {
        var likeParams = like.GetParameters();
        foreach (var item in methods)
        {
            if (item.Name != name) continue;

            var paramArr = item.GetParameters();
            var targetLen = paramArr.Length;
            var hookLen = likeParams.Length;

            // 情况1：完全匹配
            // 情况2：Hook方法多一个参数（用于接收 target/this）
            var isInstanceMatch = !item.IsStatic && hookLen == targetLen + 1;
            var isNormalMatch = hookLen == targetLen;

            if (!isInstanceMatch && !isNormalMatch) continue;

            var offset = isInstanceMatch ? 1 : 0;

            for (var i = 0; i < targetLen; i++)
            {
                var t1 = likeParams[i + offset]; // Hook方法的参数
                var t2 = paramArr[i]; // 目标方法的参数

                // 基础类型全名比对
                if (t1.ParameterType.FullName != null && t2.ParameterType.FullName != null)
                {
                    if (t1.ParameterType.FullName == t2.ParameterType.FullName)
                        continue; // 匹配成功，检查下一个
                }
                else if (t1.ParameterType.FullName == null && t2.ParameterType.FullName == null)
                {
                    // 泛型参数通常 FullName 为空
                    continue;
                }

                // --- 重要：恢复被你省略的 RememberTypeAttribute 逻辑 ---
                var rmtype = t1.GetCustomAttribute<RememberTypeAttribute>();
                if (rmtype != null)
                {
                    if (rmtype.IsGeneric && t2.ParameterType.FullName == null) continue;
                    if (rmtype.TypeFullNameOrNull != null)
                    {
                        if (rmtype.TypeFullNameOrNull == t2.ParameterType.FullName) continue;
                        var type = TypeResolver(rmtype.TypeFullNameOrNull, assemblies);
                        if (type == t2.ParameterType) continue;
                    }
                }
                // -------------------------------------------------------

                goto next; // 类型不匹配，跳到下一个 MethodBase
            }

            return item; // 所有参数匹配成功
            next: ;
        }

        return null;
    }

    private static void CurrentDomain_AssemblyLoad(object sender, AssemblyLoadEventArgs args)
    {
        InstallInternal(false, new[] { args.LoadedAssembly });
    }
}