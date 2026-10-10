using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace Mcl.Core.Dotnetdetour.Utilities.Diagnostics;

/// <summary>Optional console setup. This must not depend on WpfConfig or invoke logging during module initialization.</summary>
internal static class ConsoleEnvironment
{
    private const int StandardOutput = -11;
    private const int StandardError = -12;
    private const uint FileTypeUnknown = 0;
    private const uint FileTypeChar = 2;

    internal static bool IsUtf8Requested => string.Equals(
        Environment.GetEnvironmentVariable("MCL_CONSOLE_UTF8"), "1", StringComparison.Ordinal);

    internal static bool TryInitialize()
    {
        // Explicit handshake with the Rider run configuration; ordinary hosts keep their existing encoding.
        var utf8 = IsUtf8Requested;
        return TryInitialize(HasOutputDestination, HasInteractiveOutput, AllocConsole,
            () => Console.OutputEncoding = SelectOutputEncoding(utf8),
            utf8 ? (Action)(() => Console.OutputEncoding = SelectOutputEncoding(true)) : null);
    }

    internal static Encoding SelectOutputEncoding(bool utf8) =>
        utf8 ? new UTF8Encoding(false) : Encoding.GetEncoding(936);

    // The delegates isolate OS/console side effects for source-linked tests, without loading the module initializer.
    internal static bool TryInitialize(Func<bool> hasOutputDestination, Func<bool> hasInteractiveOutput,
        Func<bool> allocateConsole, Action configureNewConsole, Action configureExistingOutput = null)
    {
        try
        {
            // Preserve the output destination; encoding changes require the explicit debug-profile handshake.
            if (hasOutputDestination())
            {
                configureExistingOutput?.Invoke();
                return hasInteractiveOutput();
            }

            if (!allocateConsole() || !hasInteractiveOutput()) return false;
            configureNewConsole();
            return true;
        }
        catch (Exception exception) when (IsUnavailable(exception))
        {
            // A console is optional. Config loading and hook installation must still run.
            return false;
        }
    }

    internal static bool HasInteractiveOutput()
    {
        try
        {
            var handle = GetStdHandle(StandardOutput);
            return IsValid(handle) && GetFileType(handle) == FileTypeChar && GetConsoleScreenBufferInfo(handle, out _);
        }
        catch (Exception exception) when (IsUnavailable(exception))
        {
            return false;
        }
    }

    internal static bool HasOutputDestination() => HasDestination(StandardOutput) || HasDestination(StandardError);

    private static bool HasDestination(int standardHandle)
    {
        var handle = GetStdHandle(standardHandle);
        return IsValid(handle) && GetFileType(handle) != FileTypeUnknown;
    }

    private static bool IsValid(IntPtr handle) => handle != IntPtr.Zero && handle != new IntPtr(-1);

    internal static bool IsUnavailable(Exception exception) => exception is IOException ||
                                                               exception is UnauthorizedAccessException ||
                                                               exception is SecurityException ||
                                                               exception is InvalidOperationException ||
                                                               exception is ArgumentException ||
                                                               exception is NotSupportedException ||
                                                               exception is DllNotFoundException ||
                                                               exception is EntryPointNotFoundException;

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AllocConsole();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int standardHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint GetFileType(IntPtr handle);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetConsoleScreenBufferInfo(IntPtr handle, out ConsoleScreenBufferInfo info);

    [StructLayout(LayoutKind.Sequential)]
    private struct ConsoleScreenBufferInfo
    {
        public short SizeX, SizeY;
        public short CursorX, CursorY;
        public ushort Attributes;
        public short WindowLeft, WindowTop, WindowRight, WindowBottom;
        public short MaximumSizeX, MaximumSizeY;
    }
}