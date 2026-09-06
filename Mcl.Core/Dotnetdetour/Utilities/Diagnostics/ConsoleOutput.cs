using System;
using System.IO;

namespace Mcl.Core.Dotnetdetour.Utilities.Diagnostics;

/// <summary>Writes opt-in game output without leaking a color or interleaving concurrent writes.</summary>
public static class ConsoleOutput
{
    private static readonly object Sync = new object();

    public static void WriteLine(string message, ConsoleColor color = ConsoleColor.Gray)
    {
        Write(message + Environment.NewLine, color);
    }

    public static void Write(string message, ConsoleColor color = ConsoleColor.Gray)
    {
        if (message == null) return;
        lock (Sync)
        {
            var previous = Console.ForegroundColor;
            try
            {
                if (Console.IsOutputRedirected) Console.Out.Write(message);
                else
                {
                    Console.ForegroundColor = color;
                    Console.Write(message);
                }
            }
            catch (IOException)
            {
                // The host may close its console while a redirected process is still flushing output.
            }
            finally
            {
                try { Console.ForegroundColor = previous; }
                catch (IOException) { }
            }
        }
    }
}
