using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;
using Mcl.Core.Dotnetdetour.Models.Config;
using Mcl.Core.Dotnetdetour.Services.CloudSave;
using Mcl.Core.Dotnetdetour.Utilities.Diagnostics;
using WPFLauncher.Model;

namespace Mcl.Core.Dotnetdetour.Features.NetworkAndRoom;

/// <summary>
/// After a local-multiplayer process exits, offers the same cloud upload
/// confirmation that the launcher shows for single-player worlds. The Java
/// and C++ LAN models have separate cv overrides, so both are targeted.
/// </summary>
internal sealed class LanGameCloudSaveExitHook : IMethodHook
{
    [OriginalMethod]
    private static void JavaLanExitOriginal(GameM game, Process process)
    {
    }

    [HookMethod("WPFLauncher.Model.ait", "cv", "JavaLanExitOriginal")]
    private static void JavaLanExit(GameM game, Process process)
    {
        JavaLanExitOriginal(game, process);
        ScheduleUploadPrompt(game, process);
    }

    [OriginalMethod]
    private static void CppLanExitOriginal(GameM game, Process process)
    {
    }

    [HookMethod("WPFLauncher.Model.Game.CppGame.CppLanGame.alz", "cv", "CppLanExitOriginal")]
    private static void CppLanExit(GameM game, Process process)
    {
        CppLanExitOriginal(game, process);
        ScheduleUploadPrompt(game, process);
    }

    private static void ScheduleUploadPrompt(GameM game, Process process)
    {
        if (!WpfConfig.EnableLanCloudSave || game == null || process == null ||
            game.Type != GType.LAN_GAME)
        {
            return;
        }

        try
        {
            if (!process.HasExited)
            {
                PluginLog.Debug("CloudSave", $"LAN process is still running: guid={game.GUID}.");
                return;
            }

            var exitCode = process.ExitCode;
            if (!LanCloudSaveExitPolicy.IsSuccessfulExit(game.IsCpp, exitCode,
                    game.IsAlreadyEnterWorld))
            {
                PluginLog.Debug("CloudSave",
                    $"LAN game exited without offering cloud upload: guid={game.GUID}, cpp={game.IsCpp}, " +
                    $"exitCode={exitCode}, enteredWorld={game.IsAlreadyEnterWorld}.");
                return;
            }
        }
        catch (Exception exception)
        {
            PluginLog.Debug("CloudSave",
                $"Reading the LAN game exit code failed: {exception.Message}");
            return;
        }

        PluginLog.Info("CloudSave",
            $"LAN game exit detected; scheduling cloud upload prompt for {game.GUID}.");
        Task.Run(async () =>
        {
            // Let asynchronous room cleanup finish before showing a modal dialog.
            await Task.Delay(300);
            LanCloudSaveService.HandleGameExit(game);
        });
    }
}
