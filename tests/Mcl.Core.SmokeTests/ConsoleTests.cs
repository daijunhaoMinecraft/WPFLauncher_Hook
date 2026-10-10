using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Text;
using Mcl.Core.Dotnetdetour.Utilities.Diagnostics;

internal static partial class Program
{
    private static void ConsoleTests()
    {
        var allocations = 0;
        var configurations = 0;
        Func<bool> allocate = () => { allocations++; return true; };
        Action configure = () => configurations++;
        Check(!ConsoleEnvironment.TryInitialize(() => true, () => false, allocate, configure), "redirected handles do not require interactive console setup");
        Check(allocations == 0 && configurations == 0, "Rider pipes preserve output destination and encoding");
        Check(ConsoleEnvironment.TryInitialize(() => true, () => true, allocate, configure), "existing interactive console is recognized");
        Check(allocations == 0 && configurations == 0, "host-owned console is not reallocated or reconfigured");
        Check(!ConsoleEnvironment.TryInitialize(() => false, () => false, () => false, configure), "failed AllocConsole is nonfatal");
        Check(!ConsoleEnvironment.TryInitialize(() => false, () => false, allocate, configure) && configurations == 0, "allocation with unusable output skips console configuration");
        Check(ConsoleEnvironment.TryInitialize(() => false, () => true, allocate, configure) && configurations == 1, "new usable console is configured once");
        Check(!ConsoleEnvironment.TryInitialize(() => throw new IOException("invalid handle"), () => true, allocate, configure), "invalid handle probe does not escape console initialization");
        Check(!ConsoleEnvironment.TryInitialize(() => false, () => true, () => throw new InvalidOperationException("no console"), configure), "unavailable console allocation is nonfatal");
        Check(!ConsoleEnvironment.TryInitialize(() => false, () => true, allocate, () => throw new IOException("encoding handle invalid")), "encoding failure does not escape console initialization");
        var encodingChanges = 0;
        var allocationsBefore = allocations;
        Check(!ConsoleEnvironment.TryInitialize(() => true, () => false, allocate, configure, () => encodingChanges++),
            "explicit UTF-8 configuration does not require a console screen buffer");
        Check(encodingChanges == 1 && allocations == allocationsBefore, "debug encoding handshake changes encoding without allocating or replacing output");
        Check(!ConsoleEnvironment.TryInitialize(() => true, () => false, allocate, configure,
            () => throw new IOException("encoding handle unavailable")), "redirected encoding failure remains nonfatal");
        var utf8 = ConsoleEnvironment.SelectOutputEncoding(true);
        Check(utf8.CodePage == 65001 && utf8.GetPreamble().Length == 0, "debug console encoding is BOM-free UTF-8");
        Check(utf8.GetBytes("中文").SequenceEqual(new byte[] { 0xE4, 0xB8, 0xAD, 0xE6, 0x96, 0x87 }), "Chinese console output has known UTF-8 bytes");
        Check(ConsoleEnvironment.SelectOutputEncoding(false).CodePage == 936, "ordinary newly-created console retains legacy GBK encoding");
        foreach (var argument in new[] { "--console-encoding-probe", "--logging-encoding-probe" })
        using (var process = new Process
        {
            StartInfo = new ProcessStartInfo(typeof(Program).Assembly.Location, argument)
            {
                UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true
            }
        })
        using (var bytes = new MemoryStream())
        {
            process.StartInfo.EnvironmentVariables["MCL_CONSOLE_UTF8"] = "1";
            process.Start();
            var copyOutput = Task.Run(() => process.StandardOutput.BaseStream.CopyTo(bytes));
            var errorOutput = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(10000))
            {
                process.Kill(); // Only the test child created above, never a launcher/game process.
                throw new Exception("Console encoding probe timed out");
            }
            if (!copyOutput.Wait(5000) || !errorOutput.Wait(5000)) throw new Exception("Console probe output did not close");
            Check(process.ExitCode == 0, "source-linked console encoding child exits normally: " + errorOutput.Result);
            if (argument == "--console-encoding-probe")
                Check(bytes.ToArray().SequenceEqual(new byte[] { 0xE4, 0xB8, 0xAD, 0xE6, 0x96, 0x87 }),
                    "actual redirected process emits Chinese as UTF-8 without BOM or console escape bytes");
            else
                Check(new UTF8Encoding(false, true).GetString(bytes.ToArray()).Contains("服务器中文输出"),
                    "NLog reapplies debug UTF-8 after a host encoding reset");
        }
        try
        {
            ConsoleEnvironment.TryInitialize(() => false, () => true, allocate, () => throw new NullReferenceException("programming error"));
            throw new Exception("Unexpected blanket exception suppression");
        }
        catch (NullReferenceException) { _checks++; }

        var redirected = new FakeConsoleDevice { HasColor = false, FailRead = true, FailSet = true };
        ConsoleOutput.Write("redirected 中文", ConsoleColor.Blue, redirected);
        Check(redirected.Text.ToString() == "redirected 中文" && redirected.Reads == 0 && redirected.Sets == 0, "redirected writes never query or restore console colors");
        var interactive = new FakeConsoleDevice { HasColor = true };
        ConsoleOutput.Write("interactive", ConsoleColor.Blue, interactive);
        Check(interactive.Text.ToString() == "interactive" && interactive.Reads == 1 && interactive.Sets == 2 && interactive.CurrentColor == ConsoleColor.Gray, "interactive writes restore the previous color");
        var invalidColor = new FakeConsoleDevice { HasColor = true, FailRead = true };
        ConsoleOutput.Write("plain fallback", ConsoleColor.Blue, invalidColor);
        Check(invalidColor.Text.ToString() == "plain fallback" && invalidColor.Sets == 0, "color getter failure falls back to plain text");
        var invalidSet = new FakeConsoleDevice { HasColor = true, FailSet = true };
        ConsoleOutput.Write("still writable", ConsoleColor.Blue, invalidSet);
        Check(invalidSet.Text.ToString() == "still writable", "color setter failure does not drop writable text");
        var invalidProbe = new FakeConsoleDevice { FailProbe = true };
        ConsoleOutput.Write("probe fallback", ConsoleColor.Blue, invalidProbe);
        Check(invalidProbe.Text.ToString() == "probe fallback", "color capability failure falls back to plain text");
        var closed = new FakeConsoleDevice { HasColor = true, FailWrite = true };
        ConsoleOutput.Write("closed", ConsoleColor.Blue, closed);
        Check(closed.CurrentColor == ConsoleColor.Gray && closed.Sets == 2, "closed output does not escape and still attempts color restoration");
        var invalidRestore = new FakeConsoleDevice { HasColor = true, FailRestore = true };
        ConsoleOutput.Write("written", ConsoleColor.Blue, invalidRestore);
        Check(invalidRestore.Text.ToString() == "written" && invalidRestore.Sets == 2, "color restoration failure is nonfatal");
        var untouched = new FakeConsoleDevice { FailProbe = true, FailWrite = true };
        ConsoleOutput.Write(null, ConsoleColor.Blue, untouched);
        Check(untouched.Writes == 0, "null output remains a no-op");

        var originalWriter = Console.Out;
        var capture = new StringWriter();
        try
        {
            Console.SetOut(capture); // Test-only host writer; production bootstrap must preserve it.
            var installedWriter = Console.Out;
            ConsoleOutput.WriteLine("capture 中文");
            Check(capture.ToString() == "capture 中文" + Environment.NewLine && ReferenceEquals(Console.Out, installedWriter),
                "public console output writes through the host-owned writer");
        }
        finally { Console.SetOut(originalWriter); }

        var bootstrap = File.ReadAllText(Path.Combine(_root, "Mcl.Core", "Dotnetdetour", "Initialize.cs"));
        Check(bootstrap.Contains("ConsoleEnvironment.TryInitialize()") && !bootstrap.Contains("Console.CursorVisible") &&
              !bootstrap.Contains("Console.SetOut(") && !bootstrap.Contains("Console.OpenStandardOutput("),
            "module initializer uses optional console setup without cursor or global writer replacement");
    }

    private sealed class FakeConsoleDevice : ConsoleOutput.IConsoleDevice
    {
        internal bool HasColor, FailProbe, FailRead, FailSet, FailRestore, FailWrite;
        internal int Reads, Sets, Writes;
        internal ConsoleColor CurrentColor = ConsoleColor.Gray;
        internal readonly StringBuilder Text = new StringBuilder();

        public bool SupportsColor => FailProbe ? throw new IOException("probe invalid") : HasColor;
        public ConsoleColor ForegroundColor
        {
            get
            {
                Reads++;
                if (FailRead) throw new IOException("color handle invalid");
                return CurrentColor;
            }
            set
            {
                Sets++;
                if (FailSet || (FailRestore && Sets > 1)) throw new IOException("color update invalid");
                CurrentColor = value;
            }
        }

        public void Write(string message)
        {
            Writes++;
            if (FailWrite) throw new IOException("output closed");
            Text.Append(message);
        }
    }
}
