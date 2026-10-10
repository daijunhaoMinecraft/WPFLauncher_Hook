using System;

namespace Mcl.Core.Dotnetdetour.Utilities.Diagnostics;

/// <summary>Writes opt-in game output without leaking a color or interleaving concurrent writes.</summary>
public static class ConsoleOutput
{
    private static readonly object Sync = new object();
    private static readonly IConsoleDevice Device = new SystemConsoleDevice();

    public static void WriteLine(string message, ConsoleColor color = ConsoleColor.Gray)
    {
        Write(message + Environment.NewLine, color);
    }

    public static void Write(string message, ConsoleColor color = ConsoleColor.Gray)
    {
        Write(message, color, Device);
    }

    internal static void Write(string message, ConsoleColor color, IConsoleDevice device)
    {
        if (message == null) return;
        lock (Sync)
        {
            ConsoleColor? previous = null;
            try
            {
                try
                {
                    if (device.SupportsColor)
                    {
                        previous = device.ForegroundColor;
                        device.ForegroundColor = color;
                    }
                }
                catch (Exception exception) when (ConsoleEnvironment.IsUnavailable(exception))
                {
                }

                // Losing color support must not lose otherwise writable redirected/plain output.
                device.Write(message);
            }
            catch (Exception exception) when (ConsoleEnvironment.IsUnavailable(exception))
            {
                // The host may close its console while a redirected process is still flushing output.
            }
            finally
            {
                if (previous.HasValue)
                {
                    try
                    {
                        device.ForegroundColor = previous.Value;
                    }
                    catch (Exception exception) when (ConsoleEnvironment.IsUnavailable(exception))
                    {
                    }
                }
            }
        }
    }

    internal interface IConsoleDevice
    {
        bool SupportsColor { get; }
        ConsoleColor ForegroundColor { get; set; }
        void Write(string message);
    }

    private sealed class SystemConsoleDevice : IConsoleDevice
    {
        public bool SupportsColor => ConsoleEnvironment.HasInteractiveOutput();

        public ConsoleColor ForegroundColor
        {
            get => Console.ForegroundColor;
            set => Console.ForegroundColor = value;
        }

        public void Write(string message) => Console.Out.Write(message);
    }
}