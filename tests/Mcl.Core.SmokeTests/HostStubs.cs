// Only external host/update side effects are stubbed. Configuration, crypto, logs and UI are production source.
// Namespace-only host imports in RegistryHelper; the real helper is linked but registry operations are never invoked.
namespace WPFLauncher.SQLite { }
namespace WPFLauncher.Unisdk { }
namespace WPFLauncher.Util
{
    public static class tb { public const string s = @"C:\Games\Bedrock"; }
}
namespace WPFLauncher.Code
{
    public class EntityResponse<T> { }
}
namespace WPFLauncher.Network.Protocol.LobbyGame
{
    public class LobbyGameRoomEntity { }
}
namespace WPFLauncher.View.UI
{
    public class CustomLoadingWindow { }
}
namespace Mcl.Core.Updater
{
    public sealed class UpdateConfig
    {
        public bool DisableUpdate { get; set; }
        public bool IsBuildChannel { get; set; }
        public bool DisableLauncherUpdate { get; set; }
    }
    public static class UpdateManager
    {
        public static UpdateConfig CurrentConfig { get; } = new UpdateConfig();
        public static void SaveConfig() { }
    }
}
