using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Base;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;

internal static partial class Program
{
    private static void HookInstallationTests()
    {
        // Real discovery/installation coordinator, but native code writes are replaced by a recording backend.
        // No production DLL/module initializer, launcher or real native patch is executed.
        var patches = new List<Tuple<MethodBase, MethodBase, MethodBase>>();
        var entered = false;
        Func<IDetour> create = null;
        create = () => new RecordingDetour((target, hook, original) =>
        {
            patches.Add(Tuple.Create(target, hook, original));
            if (!entered)
            {
                entered = true;
                MethodHook.InstallTypes(new[] { typeof(FirstFixtureHook) }, create);
            }
        });
        MethodHook.InstallTypes(new[] { typeof(FirstFixtureHook) }, create);
        Check(patches.Count == 1 && FirstFixtureHook.SetCount == 1, "initial hook and reentrant installation patch only once");
        MethodHook.InstallTypes(new[] { typeof(SecondFixtureHook) }, create);
        Check(patches.Count == 2, "second bootstrap stage does not repatch first-stage hooks");
        MethodHook.Install(null, create);
        Check(patches.Count == 4 && patches.Select(p => p.Item1).Distinct().Count() == 4, "full installation preserves bootstrap patches and adds only missing hooks");
        Check(patches.All(p => p.Item3 != null), "original placeholders are preserved during staged discovery");
        MethodHook.Install(null, create);
        MethodHook.InstallTypes(new[] { typeof(FirstFixtureHook), typeof(SecondFixtureHook) }, create);
        MethodHook.InstallAssembly(typeof(Program).Assembly, create);
        Check(patches.Count == 4 && FirstFixtureHook.SetCount == 1, "repeated installation and assembly callbacks never overwrite installed trampolines");
        Parallel.For(0, 32, index => MethodHook.InstallTypes(new[] { typeof(FirstFixtureHook) }, create));
        Check(patches.Count == 4, "concurrent installation requests do not duplicate native writes");
        Check(FixtureTarget.First(3) == 4, "recording backend never patches native fixture code");
        MethodHook.InstallTypes(new[] { typeof(ConstructorFixtureHook) }, create);
        Check(patches.Count == 4 && patches.Any(p => p.Item1 is ConstructorInfo && p.Item3 != null),
            "constructor hooks resolve with an instance-aware original");

        var targetMethod = typeof(FixtureTarget).GetMethod(nameof(FixtureTarget.First));
        var hookMethod = typeof(FirstFixtureHook).GetMethod(nameof(FirstFixtureHook.Replacement));
        var registry = new HookPatchRegistry();
        var writes = 0;
        Parallel.For(0, 64, index => registry.InstallOnce(targetMethod, hookMethod, () => Interlocked.Increment(ref writes)));
        Check(writes == 1, "patch registry serializes concurrent attempts");
        Check(registry.InstallOnce(typeof(FixtureTarget).GetMethod(nameof(FixtureTarget.Second)), hookMethod, () => writes++) && writes == 2,
            "registry identity includes the target method");
        var failed = new HookPatchRegistry();
        var attempts = 0;
        try
        {
            failed.InstallOnce(targetMethod, hookMethod, () => { attempts++; throw new IOException("simulated partial native write"); });
            throw new Exception("Expected native write failure");
        }
        catch (IOException) { _checks++; }
        try
        {
            failed.InstallOnce(targetMethod, hookMethod, () => attempts++);
            throw new Exception("Expected failed-patch retry rejection");
        }
        catch (InvalidOperationException) { _checks++; }
        Check(attempts == 1, "failed native patch is never blindly retried within the same process");
    }

    public static class FixtureTarget
    {
        public static int First(int value) => value + 1;
        public static int Second(int value) => value + 2;
        public static int Third(int value) => value + 3;
    }

    public sealed class FirstFixtureHook : IMethodHookWithSet
    {
        internal static int SetCount;
        public void HookMethod(MethodBase method) => SetCount++;
        [HookMethod(typeof(FixtureTarget), nameof(FixtureTarget.First))]
        public static int Replacement(int value) => value;
        [OriginalMethod]
        public static int Original(int value) => value;
    }

    public sealed class SecondFixtureHook : IMethodHook
    {
        [HookMethod(typeof(FixtureTarget), nameof(FixtureTarget.Second))]
        public static int Replacement(int value) => value;
        [OriginalMethod]
        public static int Original(int value) => value;
    }

    public sealed class ConstructorFixtureTarget
    {
        public ConstructorFixtureTarget()
        {
        }
    }

    public sealed class ConstructorFixtureHook : IMethodHook
    {
        [HookMethod(typeof(ConstructorFixtureTarget), ".ctor")]
        public static void Replacement(ConstructorFixtureTarget instance)
        {
        }

        [OriginalMethod]
        public static void Original(ConstructorFixtureTarget instance)
        {
        }
    }

    public sealed class FullInstallFixtureHook : IMethodHook
    {
        [HookMethod(typeof(FixtureTarget), nameof(FixtureTarget.Third))]
        public static int Replacement(int value) => value;
        [OriginalMethod]
        public static int Original(int value) => value;
    }

    private sealed class RecordingDetour : IDetour
    {
        private readonly Action<MethodBase, MethodBase, MethodBase> _patch;
        internal RecordingDetour(Action<MethodBase, MethodBase, MethodBase> patch) => _patch = patch;
        public void Patch(MethodBase rawMethod, MethodBase hookMethod, MethodBase originalMethod) => _patch(rawMethod, hookMethod, originalMethod);
    }
}
