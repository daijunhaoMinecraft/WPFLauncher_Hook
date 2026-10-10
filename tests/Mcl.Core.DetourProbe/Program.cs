using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Mcl.Core.Dotnetdetour.CoreEngine.DetourWays;
using Mcl.Core.Dotnetdetour.Utilities.Diagnostics;

internal static class Program
{
    private static string _log;

    private static int Main()
    {
        ConsoleEnvironment.TryInitialize();
        _log = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
            Debugger.IsAttached ? "probe-debug.txt" : "probe-run.txt");
        File.WriteAllText(_log, "", new UTF8Encoding(false));
        Write("Debugger=" + Debugger.IsAttached + " PointerSize=" + IntPtr.Size);
        Write("UTF8-switch=" + Environment.GetEnvironmentVariable("MCL_CONSOLE_UTF8") + " OutputCodePage=" + Console.OutputEncoding.CodePage);
        Write("中文输出验证");
        LauncherLogging.Configure(true, false, AppDomain.CurrentDomain.BaseDirectory);
        LauncherLogging.Info("[MCL][Hook] [Web] 服务器已启动：中文输出验证");
        var target = typeof(Program).GetMethod(nameof(Target), BindingFlags.Static | BindingFlags.NonPublic);
        var hook = typeof(Program).GetMethod(nameof(Hook), BindingFlags.Static | BindingFlags.NonPublic);
        var original = typeof(Program).GetMethod(nameof(Original), BindingFlags.Static | BindingFlags.NonPublic);
        RuntimeHelpers.PrepareMethod(target.MethodHandle);
        Dump("target-before", target);
        Dump("hook-entry", hook);
        new NativeDetourFor32Bit().Patch(target, hook, original);
        Dump("target-after", target);
        Dump("original-after", original);
        Write("Calling target: expected 14");
        var result = Target(4);
        Write("Result=" + result);
        if (Debugger.IsAttached) Debugger.Break();
        return result == 14 ? 0 : 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    private static int Target(int value) => value + 3;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    private static int Hook(int value) => Original(value) * 2;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    private static int Original(int value) => -1000;

    private static void Dump(string label, MethodInfo method)
    {
        var pointer = method.MethodHandle.GetFunctionPointer();
        var bytes = new byte[16];
        Marshal.Copy(pointer, bytes, 0, bytes.Length);
        Write(label + " address=0x" + pointer.ToInt64().ToString("X8") + " bytes=" + BitConverter.ToString(bytes));
        if (bytes[0] == 0xE9)
            Write(label + " rel32-target=0x" + unchecked((uint)(pointer.ToInt64() + 5 + BitConverter.ToInt32(bytes, 1))).ToString("X8"));
    }

    private static void Write(string text)
    {
        File.AppendAllText(_log, text + Environment.NewLine, new UTF8Encoding(false));
        Console.WriteLine(text);
    }
}
