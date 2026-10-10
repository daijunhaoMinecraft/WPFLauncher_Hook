using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Mcl.Core.Dotnetdetour.Models.Config;
using Mcl.Core.Dotnetdetour.Utilities.Diagnostics;

namespace Mcl.Core.Dotnetdetour.Services.Bedrock;

internal static class BedrockCacheService
{
    // Upper bound for idle backoff; the default 800ms pass interval doubles up to this while nothing changes.
    private const int MaxIdleIntervalMilliseconds = 30000;
    private static readonly SemaphoreSlim Gate = new SemaphoreSlim(1, 1);

    internal static bool IsBedrockExecutable(string path) => !string.IsNullOrWhiteSpace(path) &&
                                                             string.Equals(Path.GetFileName(path.Trim().Trim('"')),
                                                                 "Minecraft.Windows.exe",
                                                                 StringComparison.OrdinalIgnoreCase);

    internal static void Attach(Process process, string executable)
    {
        if (!IsBedrockExecutable(executable))
        {
            PluginLog.Debug("BedrockCache", "Attach skipped: executable is not Minecraft.Windows.exe.");
            return;
        }

        if (!WpfConfig.EnableBedrockCacheCleanup && !WpfConfig.EnableBedrockMcpCopy)
        {
            PluginLog.Debug("BedrockCache", "Attach skipped: cleanup and MCP copy are both disabled.");
            return;
        }

        try
        {
            var options = ConfigManager.GetBedrockCacheOptions();
            PluginLog.Debug("BedrockCache", "Attach prepared: root={0}, cleanup={1}, copy={2}, sources={3}.",
                options.Root, options.Cleanup, options.Copy, options.Sources.Length);
            // The host starts the returned Process after the hook. Never scan other running processes.
            _ = Task.Run(() => RunAsync(process, options));
        }
        catch (Exception exception) when (exception is ArgumentException || BedrockCacheWorker.IsFileError(exception))
        {
            PluginLog.Error("BedrockCache", exception, "Cache automation was not started; check the configuration.");
        }
    }

    // One-shot manual repair used by the settings action: empties the configured packcache directory but
    // keeps the folder itself. Returns the number of top-level entries removed; failures are collected.
    internal static int ClearPackcache(string directory, ICollection<Exception> failures)
    {
        BedrockCacheOptions.ValidateEntry(nameof(WpfConfig.BedrockPackcacheDirectory), directory);
        var removed = BedrockCacheWorker.ClearContents(directory,
            exception => failures?.Add(exception));
        PluginLog.Info("BedrockCache", "Manual packcache clear removed {0} entry(ies) from {1}.", removed,
            BedrockCacheOptions.FullPath(directory));
        return removed;
    }

    private static bool? IsRunning(Process process)
    {
        try
        {
            return !process.HasExited;
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return null;
        } // Not started yet.
    }

    private static async Task RunAsync(Process process, BedrockCacheOptions options)
    {
        using (var cancellation = new CancellationTokenSource())
        {
            EventHandler exited = (sender, args) =>
            {
                try
                {
                    cancellation.Cancel();
                }
                catch (ObjectDisposedException)
                {
                } // An already dispatched event can race final unsubscription.
            };
            try
            {
                process.Exited += exited;
                process.EnableRaisingEvents = true;
                await RunSessionAsync(() => IsRunning(process), options,
                    () => WpfConfig.EnableBedrockCacheCleanup, () => WpfConfig.EnableBedrockMcpCopy,
                    cancellation.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                PluginLog.Error("BedrockCache", exception,
                    "File automation stopped; the game process is not affected.");
            }
            finally
            {
                process.Exited -= exited;
                PluginLog.Debug("BedrockCache", "File automation stopped.");
            }
        }
    }

    // The state probe allows source-linked lifecycle tests without starting or loading the host/game.
    internal static async Task RunSessionAsync(Func<bool?> running, BedrockCacheOptions options,
        Func<bool> cleanupEnabled, Func<bool> copyEnabled, CancellationToken token,
        int intervalMilliseconds = 800, int startupTimeoutMilliseconds = 60000)
    {
        if (!options.Cleanup && !options.Copy)
        {
            PluginLog.Debug("BedrockCache", "Session not started: cleanup and copy are both disabled.");
            return;
        }

        var started = Stopwatch.StartNew();
        while (running() == null && started.ElapsedMilliseconds < startupTimeoutMilliseconds)
            await Task.Delay(Math.Min(100, intervalMilliseconds), token).ConfigureAwait(false);
        if (running() != true)
        {
            PluginLog.Debug("BedrockCache", "Session not started: game process did not report as running.");
            return;
        }

        var nextWarning = DateTime.MinValue;
        Action<Exception> report = exception =>
        {
            if (DateTime.UtcNow < nextWarning) return;
            nextWarning = DateTime.UtcNow.AddMinutes(1);
            PluginLog.Warn("BedrockCache", "Cache operation failed; it will be retried: {0}", exception.Message);
        };
        var worker = new BedrockCacheWorker(options, report);
        Func<bool> cleanup = () => cleanupEnabled() && running() == true;
        Func<bool> copy = () => copyEnabled() && running() == true;
        PluginLog.Info("BedrockCache", "File automation started for Minecraft.Windows.exe.");

        // Measure the optional duration cap from the moment the game is observed running, not from Attach.
        started.Restart();
        var duration = TimeSpan.FromSeconds(options.DurationSeconds);
        var pollMilliseconds = intervalMilliseconds;
        while (!token.IsCancellationRequested && running() == true &&
               ((options.Cleanup && cleanup()) || (options.Copy && copy())) &&
               (options.DurationSeconds <= 0 || started.Elapsed < duration))
        {
            var changed = false;
            await Gate.WaitAsync(token).ConfigureAwait(false);
            try
            {
                changed = worker.RunPass(cleanup, copy, token);
            }
            catch (OperationCanceledException) when (!token.IsCancellationRequested)
            {
            }
            catch (Exception exception) when (BedrockCacheWorker.IsFileError(exception))
            {
                report(exception);
            }
            finally
            {
                Gate.Release();
            }

            // Back off scanning while the game is not regenerating cache; any change restores the fast interval.
            pollMilliseconds = changed ? intervalMilliseconds : Math.Min(pollMilliseconds * 2, MaxIdleIntervalMilliseconds);
            await Task.Delay(pollMilliseconds, token).ConfigureAwait(false);
        }

        if (options.DurationSeconds > 0 && running() == true && started.Elapsed >= duration)
            PluginLog.Info("BedrockCache", "File automation stopped after the configured {0}s duration.",
                options.DurationSeconds);
        else
            PluginLog.Debug("BedrockCache", "Session loop ended: cancelled={0}, running={1}.", token.IsCancellationRequested,
                running());
    }
}