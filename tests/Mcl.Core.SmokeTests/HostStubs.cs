// Only external host/update side effects are stubbed. Configuration, crypto, logs and UI are production source.
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
    }
    public static class UpdateManager
    {
        public static UpdateConfig CurrentConfig { get; } = new UpdateConfig();
        public static void SaveConfig() { }
    }
}
