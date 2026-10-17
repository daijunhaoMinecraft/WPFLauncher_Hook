using System;
using System.Collections.Generic;
using System.IO;
using Mcl.Core.Dotnetdetour.Models.Config;
using Newtonsoft.Json;

namespace Mcl.Core.Dotnetdetour.Services.CloudSave;

internal sealed class LanCloudSaveLink
{
    [JsonProperty("cloud_save_id")]
    public string CloudSaveId { get; set; }

    [JsonProperty("updated_at")]
    public long UpdatedAt { get; set; }
}

/// <summary>
/// Persists the mapping between a LAN world GUID and its desktop cloud-save ID.
/// The host only records cloud IDs for single-player games, so this mapping is
/// needed to reattach a LAN world after the launcher restarts.
/// </summary>
internal sealed class LanCloudSaveLinkStore
{
    private const string DefaultFileName = "LanCloudSaves.json";
    private readonly object _sync = new object();
    private readonly string _path;

    public LanCloudSaveLinkStore(string path)
    {
        _path = Path.GetFullPath(path);
    }

    public static LanCloudSaveLinkStore CreateDefault()
    {
        return new LanCloudSaveLinkStore(Path.Combine(WpfConfig.LauncherRootDirectory, DefaultFileName));
    }

    public bool TryGet(string gameId, out string cloudSaveId)
    {
        cloudSaveId = null;
        if (string.IsNullOrWhiteSpace(gameId)) return false;

        lock (_sync)
        {
            LanCloudSaveLink link;
            if (!Load().TryGetValue(gameId, out link) || link == null ||
                string.IsNullOrWhiteSpace(link.CloudSaveId))
            {
                return false;
            }

            cloudSaveId = link.CloudSaveId;
            return true;
        }
    }

    public void Set(string gameId, string cloudSaveId)
    {
        if (string.IsNullOrWhiteSpace(gameId) || string.IsNullOrWhiteSpace(cloudSaveId))
            throw new ArgumentException("LAN cloud-save mapping requires both game and cloud IDs.");

        lock (_sync)
        {
            var links = Load();
            links[gameId] = new LanCloudSaveLink
            {
                CloudSaveId = cloudSaveId,
                UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
            AtomicFile.WriteAllText(_path, JsonConvert.SerializeObject(links, Formatting.Indented));
        }
    }

    public bool Remove(string gameId)
    {
        if (string.IsNullOrWhiteSpace(gameId)) return false;

        lock (_sync)
        {
            var links = Load();
            if (!links.Remove(gameId)) return false;
            AtomicFile.WriteAllText(_path, JsonConvert.SerializeObject(links, Formatting.Indented));
            return true;
        }
    }

    private Dictionary<string, LanCloudSaveLink> Load()
    {
        try
        {
            if (!File.Exists(_path)) return NewDictionary();

            var json = File.ReadAllText(_path);
            if (string.IsNullOrWhiteSpace(json)) return NewDictionary();

            var links = JsonConvert.DeserializeObject<Dictionary<string, LanCloudSaveLink>>(json);
            return links == null ? NewDictionary() : new Dictionary<string, LanCloudSaveLink>(links, StringComparer.Ordinal);
        }
        catch (IOException)
        {
            return NewDictionary();
        }
        catch (UnauthorizedAccessException)
        {
            return NewDictionary();
        }
        catch (JsonException)
        {
            return NewDictionary();
        }
    }

    private static Dictionary<string, LanCloudSaveLink> NewDictionary()
    {
        return new Dictionary<string, LanCloudSaveLink>(StringComparer.Ordinal);
    }
}
