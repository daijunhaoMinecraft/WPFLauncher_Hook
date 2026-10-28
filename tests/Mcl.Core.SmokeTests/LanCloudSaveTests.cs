using System;
using System.IO;
using Mcl.Core.Dotnetdetour.Services.CloudSave;

internal static partial class Program
{
    private static void LanCloudSaveTests()
    {
        var area = Path.Combine(_output, "lan-cloud-" + Guid.NewGuid().ToString("N"));
        var path = Path.Combine(area, "LanCloudSaves.json");
        var store = new LanCloudSaveLinkStore(path);

        string cloudId;
        Check(!store.TryGet(null, out cloudId), "LAN cloud link rejects empty game ID");
        Check(!store.TryGet("lan-world-a", out cloudId), "LAN cloud link starts empty");

        store.Set("lan-world-a", "cloud-save-1");
        store.Set("lan-world-b", "cloud-save-2");
        Check(store.TryGet("lan-world-a", out cloudId) && cloudId == "cloud-save-1",
            "LAN cloud link stores game mapping");

        var reloaded = new LanCloudSaveLinkStore(path);
        Check(reloaded.TryGet("lan-world-b", out cloudId) && cloudId == "cloud-save-2",
            "LAN cloud link survives restart");
        Check(reloaded.Remove("lan-world-a"), "LAN cloud link removes existing mapping");
        Check(!reloaded.TryGet("lan-world-a", out cloudId) && reloaded.TryGet("lan-world-b", out cloudId),
            "LAN cloud link keeps unrelated mappings");

        File.WriteAllText(path, "{broken");
        Check(!reloaded.TryGet("lan-world-b", out cloudId), "LAN cloud link ignores corrupt file");
        reloaded.Set("lan-world-c", "cloud-save-3");
        Check(reloaded.TryGet("lan-world-c", out cloudId) && cloudId == "cloud-save-3",
            "LAN cloud link recovers after corrupt file");

        Check(LanCloudSaveExitPolicy.IsSuccessfulExit(false, 0, false),
            "Java LAN exit code 0 offers cloud upload");
        Check(LanCloudSaveExitPolicy.IsSuccessfulExit(false, 1, true),
            "Java LAN exit code 1 after entering world offers cloud upload");
        Check(!LanCloudSaveExitPolicy.IsSuccessfulExit(false, 1, false),
            "Java LAN exit code 1 without entering world skips cloud upload");
        Check(LanCloudSaveExitPolicy.IsSuccessfulExit(false, -1, true),
            "Java LAN forced teardown after entering world offers cloud upload");
        Check(LanCloudSaveExitPolicy.IsSuccessfulExit(true, 0, true),
            "C++ LAN clean exit offers cloud upload");
        Check(!LanCloudSaveExitPolicy.IsSuccessfulExit(true, 1, true),
            "C++ LAN non-zero exit skips cloud upload");
    }
}
