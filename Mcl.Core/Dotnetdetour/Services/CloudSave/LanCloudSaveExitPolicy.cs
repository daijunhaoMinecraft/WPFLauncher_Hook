namespace Mcl.Core.Dotnetdetour.Services.CloudSave;

internal static class LanCloudSaveExitPolicy
{
    /// <summary>
    /// C++ games must exit cleanly. Java LAN worlds offer the prompt once the
    /// player has entered the world because the host may terminate javaw with
    /// a non-standard exit code during normal room teardown.
    /// </summary>
    public static bool IsSuccessfulExit(bool isCpp, int exitCode, bool enteredWorld)
    {
        if (isCpp) return exitCode == 0;
        return exitCode == 0 || enteredWorld;
    }
}
