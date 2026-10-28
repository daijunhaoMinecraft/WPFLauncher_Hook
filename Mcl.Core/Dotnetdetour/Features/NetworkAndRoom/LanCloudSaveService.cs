using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using Mcl.Core.Dotnetdetour.Models.Config;
using Mcl.Core.Dotnetdetour.Services.CloudSave;
using Mcl.Core.Dotnetdetour.Utilities.Diagnostics;
using WPFLauncher.Code;
using WPFLauncher.Common;
using WPFLauncher.Manager;
using WPFLauncher.Manager.Configuration;
using WPFLauncher.Model;
using WPFLauncher.Network.Protocol.CloudSave;
using WPFLauncher.Util;
using WPFLauncher.View.CloudSave;

namespace Mcl.Core.Dotnetdetour.Features.NetworkAndRoom;

internal sealed class LanCloudSaveState
{
    public bool Supported { get; set; }
    public bool Host { get; set; }
    public bool CloudEnabled { get; set; }
    public bool LocalSaveExists { get; set; }
    public bool HasCloudSave { get; set; }
    public bool Busy { get; set; }
    public bool CanUpload { get; set; }
    public bool CanDownload { get; set; }
    public string Status { get; set; }
    public string Detail { get; set; }
}

internal sealed class LanCloudSaveOperationResult
{
    private LanCloudSaveOperationResult(bool success, string message)
    {
        Success = success;
        Message = message;
    }

    public bool Success { get; }
    public string Message { get; }

    public static LanCloudSaveOperationResult Ok(string message)
    {
        return new LanCloudSaveOperationResult(true, message);
    }

    public static LanCloudSaveOperationResult Fail(string message)
    {
        return new LanCloudSaveOperationResult(false, message);
    }
}

/// <summary>
/// Bridges LAN worlds to the launcher's existing desktop cloud-save protocol.
/// The launcher intentionally limits aqa's create/upload path to MC_GAME, so
/// the plugin creates the remote record itself and keeps the aid dictionaries
/// in the shape the host upload/download implementation expects.
/// </summary>
internal static class LanCloudSaveService
{
    private const BindingFlags InstanceFlags =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    private static readonly object OperationGate = new object();
    private static readonly LanCloudSaveLinkStore LinkStore = LanCloudSaveLinkStore.CreateDefault();
    private static readonly Type CloudSaveProtocolType =
        typeof(aqa).Assembly.GetType("WPFLauncher.Network.Protocol.CloudSave.afg");
    private static readonly HashSet<string> ExitPrompts =
        new HashSet<string>(StringComparer.Ordinal);
    private static string _busyGameId;

    public static event Action<GameM> StateChanged;

    public static bool IsLanGame(GameM game)
    {
        return game != null && game.Type == GType.LAN_GAME;
    }

    public static GameM GetSelectedGame(object dataContext)
    {
        if (dataContext is GameM game) return game;
        if (dataContext == null) return null;

        try
        {
            var property = dataContext.GetType().GetProperty("SelectedGame", InstanceFlags);
            return property?.GetValue(dataContext) as GameM;
        }
        catch (Exception exception)
        {
            PluginLog.Debug("CloudSave", $"Reading the selected LAN game failed: {exception.Message}");
            return null;
        }
    }

    public static LanCloudSaveState ReadState(GameM game)
    {
        var state = new LanCloudSaveState
        {
            Supported = IsLanGame(game),
            Status = "云存档",
            Detail = "未选择本地联机房间。"
        };
        if (!state.Supported) return state;

        state.Host = IsHost(game);
        state.CloudEnabled = IsCloudEnabled();
        state.LocalSaveExists = SafeGet(() => game.IsSaveExist, false);

        if (!state.Host)
        {
            state.Detail = "仅房主可上传或恢复该房间的云存档。";
            state.Status = "仅房主";
            return state;
        }

        if (!state.CloudEnabled)
        {
            state.Detail = "云存档服务尚未就绪，请确认已登录并等待云端配置加载完成。";
            state.Status = "不可用";
            return state;
        }

        string bindError;
        TryHydrateLink(game, true, out bindError);

        var client = game.ClientCloudSave;
        var server = game.ServerCloudSave;
        state.HasCloudSave = client != null && server != null;
        state.Busy = client != null && client.State > aia.a;
        state.CanUpload = state.LocalSaveExists && !state.Busy &&
                          game.Status != GStatus.PLAYING && game.Status != GStatus.DOWNLOADING;
        state.CanDownload = state.HasCloudSave && !state.Busy &&
                            game.Status != GStatus.PLAYING && game.Status != GStatus.DOWNLOADING;

        if (state.Busy)
        {
            state.Status = "同步中";
            state.Detail = DescribeBusyState(client.State);
            return state;
        }

        if (!state.HasCloudSave)
        {
            state.Status = "未创建";
            state.Detail = state.LocalSaveExists
                ? "此房间尚未关联云端存档，创建后会把当前世界上传到云端。"
                : "本地存档不存在，且此房间尚未关联云端存档。";
            return state;
        }

        if (!state.LocalSaveExists)
        {
            state.Status = "可恢复";
            state.Detail = $"云端存档《{client.Name}》可用，本地存档不存在，可点击下载恢复。";
            return state;
        }

        if (client.IsChange)
        {
            state.Status = "待上传";
            state.Detail = $"云端存档《{client.Name}》存在，本地存档有改动，建议上传。";
            return state;
        }

        if (!client.IsChange && client.Version == server.Version)
        {
            state.Status = "已同步";
            state.Detail = $"云端存档《{client.Name}》与本地存档一致。";
            return state;
        }

        state.Status = "版本不同";
        state.Detail = $"云端《{client.Name}》版本 {server.Version}，本地版本 {client.Version}。";
        return state;
    }

    public static LanCloudSaveOperationResult Upload(GameM game)
    {
        string operationError;
        if (!TryBeginOperation(game, out operationError))
            return LanCloudSaveOperationResult.Fail(operationError);

        aid activeClient = null;
        try
        {
            if (!ValidateGame(game, true, out operationError))
                return LanCloudSaveOperationResult.Fail(operationError);
            if (!game.IsSaveExist)
                return LanCloudSaveOperationResult.Fail("本地存档不存在，无法上传。");

            string bindError;
            TryHydrateLink(game, true, out bindError);

            var client = game.ClientCloudSave;
            var server = game.ServerCloudSave;
            activeClient = client;
            if (client == null || server == null)
            {
                if (!TryCreateCloudRecord(game, out client, out server, out operationError))
                    return LanCloudSaveOperationResult.Fail(operationError);
                activeClient = client;
            }

            if (client.State != aia.a)
                return LanCloudSaveOperationResult.Fail(DescribeBusyState(client.State));

            client.Name = game.Name;
            client.GameType = GType.LAN_GAME;
            client.IsChange = true;
            PopulateMetadata(client, game);

            var result = client.i();
            if (result != 0)
            {
                client.State = aia.a;
                client.Down.PackageSize = 0;
                return LanCloudSaveOperationResult.Fail(DescribeUploadFailure(result));
            }

            client.State = aia.a;
            client.IsChange = false;
            SynchronizeServerAid(client, server, game);
            TrySetLink(game, client.ID);
            PersistCloudIndex();
            game.dh();

            return LanCloudSaveOperationResult.Ok($"云端存档《{client.Name}》上传完成。");
        }
        catch (Exception exception)
        {
            if (activeClient != null && activeClient.State > aia.a)
                activeClient.State = aia.a;
            PluginLog.Error("CloudSave", exception, "Uploading the LAN cloud save failed.");
            return LanCloudSaveOperationResult.Fail("上传云存档失败: " + exception.Message);
        }
        finally
        {
            EndOperation();
            NotifyStateChanged(game);
        }
    }

    public static LanCloudSaveOperationResult Download(GameM game)
    {
        string operationError;
        if (!TryBeginOperation(game, out operationError))
            return LanCloudSaveOperationResult.Fail(operationError);

        aid activeClient = null;
        try
        {
            if (!ValidateGame(game, true, out operationError))
                return LanCloudSaveOperationResult.Fail(operationError);

            string bindError;
            if (!TryHydrateLink(game, true, out bindError) ||
                game.ClientCloudSave == null || game.ServerCloudSave == null)
            {
                return LanCloudSaveOperationResult.Fail(
                    string.IsNullOrWhiteSpace(bindError)
                        ? "该房间尚未关联云端存档。"
                        : bindError);
            }

            var client = game.ClientCloudSave;
            var server = game.ServerCloudSave;
            activeClient = client;
            if (client.State != aia.a)
                return LanCloudSaveOperationResult.Fail(DescribeBusyState(client.State));

            if (server.Parts == null || server.Parts.Count == 0)
            {
                bool refreshed;
                TryRefreshServerCloudSaves(out refreshed);
                server = GetServerAid(client.ID);
                if (server == null || server.Parts == null || server.Parts.Count == 0)
                    return LanCloudSaveOperationResult.Fail("云端分片信息不可用，请重新打开页面后重试。");
            }

            client.State = aia.d;
            client.Down.TotalSize = (int)Math.Max(0, client.Size);
            var result = client.x();
            client.State = aia.a;
            client.Down.PackageSize = 0;

            if (result != 0)
                return LanCloudSaveOperationResult.Fail(DescribeDownloadFailure(result));

            ApplyDownloadedMetadata(game, server);
            client.Version = server.Version;
            client.IsChange = false;
            client.IsSyncProperty = true;
            game.CloudStatus = apk.b;
            game.IsSaveExist = !string.IsNullOrEmpty(game.SavePath) && Directory.Exists(game.SavePath);
            game.dh();
            PersistCloudIndex();

            return LanCloudSaveOperationResult.Ok($"云端存档《{client.Name}》已下载并应用。");
        }
        catch (Exception exception)
        {
            if (activeClient != null && activeClient.State > aia.a)
                activeClient.State = aia.a;
            PluginLog.Error("CloudSave", exception, "Downloading the LAN cloud save failed.");
            return LanCloudSaveOperationResult.Fail("下载云存档失败: " + exception.Message);
        }
        finally
        {
            EndOperation();
            NotifyStateChanged(game);
        }
    }

    public static LanCloudSaveOperationResult OpenManager()
    {
        if (!IsCloudEnabled())
            return LanCloudSaveOperationResult.Fail("云存档服务尚未就绪。");

        try
        {
            var manager = azf<apn>.Instance.k<CloudSaveManagerView>();
            if (manager == null)
                manager = azf<apn>.Instance.c<CloudSaveManagerView>(vx.a(), azc.c);

            manager.ShowInTaskbar = true;
            manager.WindowState = WindowState.Normal;
            manager.d();
            manager.Activate();
            manager.a();
            return LanCloudSaveOperationResult.Ok(null);
        }
        catch (Exception exception)
        {
            PluginLog.Error("CloudSave", exception, "Opening the host cloud-save manager failed.");
            return LanCloudSaveOperationResult.Fail("打开云存档管理失败: " + exception.Message);
        }
    }

    public static void HandleGameExit(GameM game)
    {
        if (!TryBeginExitPrompt(game)) return;

        try
        {
            if (!WpfConfig.EnableLanCloudSave)
            {
                PluginLog.Debug("CloudSave", "LAN cloud save is disabled by configuration.");
                return;
            }

            if (!IsLanGame(game))
            {
                PluginLog.Debug("CloudSave", "Exit prompt skipped: selected game is not LAN_GAME.");
                return;
            }

            if (!IsHost(game))
            {
                PluginLog.Debug("CloudSave", "Exit prompt skipped: local player is not the LAN host.");
                return;
            }

            if (!IsCloudEnabled())
            {
                PluginLog.Debug("CloudSave", "Exit prompt skipped: cloud-save service is not ready.");
                return;
            }

            if (!game.IsSaveExist)
            {
                PluginLog.Debug("CloudSave", "Exit prompt skipped: local save does not exist.");
                return;
            }

            string bindError;
            TryHydrateLink(game, true, out bindError);

            var client = game.ClientCloudSave;
            var timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (client != null)
            {
                if (client.State > aia.a) return;

                client.LastPlayTime = timestamp;
                client.IsChange = true;
                PersistCloudIndex();

                if (client.IsAutoSync)
                {
                    UploadAfterExit(game, false);
                    return;
                }

                if (client.IsNeverNote) return;

                var result = ShowExitUploadDialog(client.ID, null);
                if (result == true) UploadAfterExit(game, true);
                return;
            }

            var cloud = azf<aqa>.Instance;
            if (!HasCloudSaveSlot(cloud)) return;

            var pending = aqa.i(game);
            if (pending == null || pending.IsNeverNote) return;

            pending.LastPlayTime = timestamp;
            pending.IsChange = true;
            PopulateMetadata(pending, game);

            var confirm = ShowExitUploadDialog(null, pending);
            if (confirm == true) UploadAfterExit(game, true);
        }
        catch (Exception exception)
        {
            PluginLog.Error("CloudSave", exception,
                "Handling the LAN game exit cloud-save prompt failed.");
        }
        finally
        {
            EndExitPrompt(game);
            NotifyStateChanged(game);
        }
    }

    private static bool ValidateGame(GameM game, bool requireHost, out string error)
    {
        if (!IsLanGame(game))
        {
            error = "当前选中的不是本地联机房间。";
            return false;
        }

        if (requireHost && !IsHost(game))
        {
            error = "仅房主可管理该房间的云存档。";
            return false;
        }

        if (!IsCloudEnabled())
        {
            error = "云存档服务尚未就绪，请确认已登录并等待云端配置加载完成。";
            return false;
        }

        if (game.Status == GStatus.PLAYING)
        {
            error = "游戏运行中不能同步云存档。";
            return false;
        }

        if (game.Status == GStatus.DOWNLOADING)
        {
            error = "游戏正在下载，暂时不能同步云存档。";
            return false;
        }

        error = null;
        return true;
    }

    private static bool TryBeginExitPrompt(GameM game)
    {
        if (game == null || string.IsNullOrWhiteSpace(game.GUID)) return false;

        lock (ExitPrompts)
        {
            return ExitPrompts.Add(game.GUID);
        }
    }

    private static void EndExitPrompt(GameM game)
    {
        if (game == null) return;
        lock (ExitPrompts)
        {
            ExitPrompts.Remove(game.GUID);
        }
    }

    private static bool HasCloudSaveSlot(aqa cloud)
    {
        var user = azf<arg>.Instance.User;
        return user.IsVip
            ? cloud.CloudSaveNumber < cloud.LimitNumber + cloud.VipLimitNumber
            : cloud.NormalCloudSaveNumber < cloud.LimitNumber;
    }

    private static bool? ShowExitUploadDialog(string cloudSaveId, aid pending)
    {
        bool? result = null;
        RunOnUiThread(() =>
        {
            var view = azf<apn>.Instance.e<CloudConfirmUploadView>(vx.a(), azc.c);
            if (!string.IsNullOrWhiteSpace(cloudSaveId))
                view.b(cloudSaveId);
            else if (pending != null)
                view.a(pending);
            else
                return;

            result = view.a();
        });
        return result;
    }

    private static void UploadAfterExit(GameM game, bool showFailure)
    {
        var result = Upload(game);
        if (result.Success)
        {
            PluginLog.Info("CloudSave", result.Message);
            return;
        }

        PluginLog.Warn("CloudSave", result.Message);
        if (!showFailure) return;

        RunOnUiThread(() =>
        {
            var owner = Window.GetWindow(vx.a());
            if (owner == null)
                MessageBox.Show(result.Message, "上传云存档", MessageBoxButton.OK, MessageBoxImage.Error);
            else
                MessageBox.Show(owner, result.Message, "上传云存档", MessageBoxButton.OK,
                    MessageBoxImage.Error);
        });
    }

    private static void RunOnUiThread(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null || dispatcher.CheckAccess())
            action();
        else
            dispatcher.Invoke(action);
    }

    private static void NotifyStateChanged(GameM game)
    {
        var handler = StateChanged;
        if (handler == null || game == null) return;

        foreach (Action<GameM> callback in handler.GetInvocationList())
        {
            try
            {
                callback(game);
            }
            catch (Exception exception)
            {
                PluginLog.Debug("CloudSave",
                    $"A cloud-save state listener failed: {exception.Message}");
            }
        }
    }

    private static bool TryCreateCloudRecord(
        GameM game,
        out aid client,
        out aid server,
        out string error)
    {
        client = null;
        server = null;
        error = null;

        var cloud = azf<aqa>.Instance;
        if (!azf<arg>.Instance.User.IsVip)
        {
            if (cloud.NormalCloudSaveNumber >= cloud.LimitNumber)
            {
                error = "云存档数量已达上限，请先在云存档管理中删除旧存档。";
                return false;
            }
        }
        else if (cloud.CloudSaveNumber >= cloud.LimitNumber + cloud.VipLimitNumber)
        {
            error = "云存档数量已达上限，请先在云存档管理中删除旧存档。";
            return false;
        }

        string createdId = null;
        try
        {
            var response = CreateRemoteCloudSave((long)cloud.CloudSaveNumber >= cloud.MaxSize);
            if (response == null || response.code != 0 || response.entity == null)
            {
                error = response == null
                    ? "创建云存档失败：网络未返回结果。"
                    : "创建云存档失败：" + (response.message ?? ("错误码 " + response.code));
                return false;
            }

            createdId = response.entity.entity_id;
            if (string.IsNullOrWhiteSpace(createdId))
            {
                error = "创建云存档失败：服务器未返回云存档 ID。";
                return false;
            }

            server = new aid
            {
                ID = createdId,
                Name = game.Name,
                GameType = GType.LAN_GAME,
                GameVersion = game.Version,
                IsVip = response.entity.vip_only,
                Version = 0,
                UpdateTime = 0,
                IsChange = false,
                State = aia.a
            };

            client = aqa.i(game);
            if (client == null)
            {
                error = "创建云存档失败：无法为本地联机房间建立云端对象。";
                return false;
            }

            client.ID = createdId;
            client.Name = game.Name;
            client.GameType = GType.LAN_GAME;
            client.GameVersion = game.Version;
            client.IsVip = server.IsVip;
            client.Version = 0;
            client.UpdateTime = 0;
            client.IsChange = true;
            client.State = aia.a;
            PopulateMetadata(client, game);

            EnsureCloudDictionaries(cloud);
            cloud.ServerCloudSaveDict[createdId] = server;
            cloud.ClientCloudSaveDict[createdId] = client;
            game.CloudSaveId = createdId;
            TrySetLink(game, createdId);
            return true;
        }
        catch (Exception exception)
        {
            if (!string.IsNullOrEmpty(createdId))
            {
                    try { DeleteRemoteCloudSave(createdId); }
                catch (Exception cleanupException)
                {
                    PluginLog.Debug("CloudSave",
                        $"Cleaning up the failed cloud-save record failed: {cleanupException.Message}");
                }
            }

            PluginLog.Error("CloudSave", exception, "Creating the LAN cloud-save record failed.");
            error = "创建云存档失败: " + exception.Message;
            return false;
        }
    }

    private static bool TryHydrateLink(GameM game, bool refreshIfMissing, out string error)
    {
        error = null;
        if (!IsLanGame(game)) return false;

        var cloud = azf<aqa>.Instance;
        if (game.ClientCloudSave != null && game.ServerCloudSave != null) return true;

        string cloudSaveId;
        if (!LinkStore.TryGet(game.GUID, out cloudSaveId)) return false;

        EnsureCloudDictionaries(cloud);
        var serverKnown = cloud.ServerCloudSaveDict.ContainsKey(cloudSaveId);
        var refreshed = false;
        if (!serverKnown && refreshIfMissing)
            TryRefreshServerCloudSaves(out refreshed);

        aid server;
        if (!cloud.ServerCloudSaveDict.TryGetValue(cloudSaveId, out server))
        {
            if (refreshed)
            {
                TryRemoveLink(game);
                error = "云端存档记录已不存在，关联信息已清理。";
            }
            else
            {
                error = "无法读取云端存档记录，请检查网络后重试。";
            }

            return false;
        }

        if (server.GameType != GType.LAN_GAME)
        {
            TryRemoveLink(game);
            error = "云端记录不是本地联机存档，已忽略该关联。";
            return false;
        }

        var client = BuildClientAid(server, game);
        cloud.ClientCloudSaveDict[cloudSaveId] = client;
        game.CloudSaveId = cloudSaveId;
        return true;
    }

    private static aid BuildClientAid(aid server, GameM game)
    {
        var client = new aid
        {
            ID = server.ID,
            Name = server.Name,
            GameType = GType.LAN_GAME,
            GameVersion = server.GameVersion,
            IsVip = server.IsVip,
            Version = server.Version,
            UpdateTime = server.UpdateTime,
            IsChange = false,
            State = aia.a,
            Character = server.Character,
            PictureID = server.PictureID,
            ComponentList = CopyList(server.ComponentList),
            OpenedVisualTextureList = CopyList(server.OpenedVisualTextureList),
            OpenedVisualShaderpackList = CopyList(server.OpenedVisualShaderpackList),
            GameMode = server.GameMode,
            IsCheat = server.IsCheat,
            LastPlayTime = server.LastPlayTime,
            TotalGamePlayTime = server.TotalGamePlayTime
        };
        if (server.Parts != null)
        {
            client.Parts = server.Parts;
            client.CurrentPartNum = server.CurrentPartNum;
        }

        client.Game = game;
        client.c();
        return client;
    }

    private static void PopulateMetadata(aid client, GameM game)
    {
        var config = game.GameCfg;
        if (config != null)
        {
            client.ComponentList = CopyList(config.InstallComponents);
            client.OpenedVisualTextureList = CopyList(config.OpenedVisualTextureList);
            client.OpenedVisualShaderpackList = CopyList(config.OpenedVisualShaderpackList);
            client.GameMode = game.GameMode;
            client.IsCheat = config.AllowCheat;
        }

        var role = game.RoleList?.FirstOrDefault();
        if (role != null)
            client.Character = role.Name;

        client.GameVersion = game.Version;
        client.LastPlayTime = game.LastPlayTime;
        client.TotalGamePlayTime = game.TotalPlayTime;
    }

    private static void ApplyDownloadedMetadata(GameM game, aid server)
    {
        var config = game.GameCfg;
        if (config != null)
        {
            if (server.ComponentList != null)
            {
                config.InstallComponents = CopyList(server.ComponentList);
                game.InstalledComponents = config.InstallComponents;
            }

            if (server.OpenedVisualTextureList != null)
                config.OpenedVisualTextureList = CopyList(server.OpenedVisualTextureList);
            if (server.OpenedVisualShaderpackList != null)
                config.OpenedVisualShaderpackList = CopyList(server.OpenedVisualShaderpackList);

            if (!string.IsNullOrWhiteSpace(server.Character))
            {
                if (game.RoleList == null || game.RoleList.Count == 0)
                    game.RoleList = new System.Collections.ObjectModel.ObservableCollection<ail>();
                if (game.RoleList.Count == 0)
                    game.RoleList.Add(new ail(server.Character));
                else
                    game.RoleList[0].Name = server.Character;
                config.RoleList = new List<ail>(game.RoleList);
            }

            config.AllowCheat = server.IsCheat;
            config.GameModeIdx = app.a(server.GameMode);
            config.LastPlayTime = server.LastPlayTime;
            config.TotalPlayTime = server.TotalGamePlayTime;
            config.Save();
        }

        game.LastPlayTime = server.LastPlayTime;
        game.TotalPlayTime = server.TotalGamePlayTime;
    }

    private static void SynchronizeServerAid(aid client, aid server, GameM game)
    {
        server.ID = client.ID;
        server.Name = client.Name;
        server.GameType = GType.LAN_GAME;
        server.GameVersion = client.GameVersion;
        server.IsVip = client.IsVip;
        server.Version = client.Version;
        server.UpdateTime = client.UpdateTime;
        server.Parts = client.Parts;
        server.CurrentPartNum = client.CurrentPartNum;
        server.Game = game;
        server.c();
    }

    private static bool TryRefreshServerCloudSaves(out bool refreshed)
    {
        refreshed = false;
        try
        {
            var userId = azf<arg>.Instance.User.UserID.ToString(CultureInfo.InvariantCulture);
            if (string.IsNullOrWhiteSpace(userId)) return false;

            var fetched = new ConcurrentDictionary<string, aid>();
            if (RefreshRemoteCloudSaveList(userId, ref fetched) != 0) return false;

            var cloud = azf<aqa>.Instance;
            EnsureCloudDictionaries(cloud);
            foreach (var pair in fetched)
            {
                if (!cloud.ServerCloudSaveDict.ContainsKey(pair.Key))
                {
                    cloud.ServerCloudSaveDict[pair.Key] = pair.Value;
                    continue;
                }

                aid existing;
                if (cloud.ServerCloudSaveDict.TryGetValue(pair.Key, out existing) &&
                    (existing.Parts == null || existing.Parts.Count == 0) &&
                    pair.Value.Parts != null && pair.Value.Parts.Count > 0)
                {
                    pair.Value.Game = existing.Game;
                    cloud.ServerCloudSaveDict[pair.Key] = pair.Value;
                }
            }

            refreshed = true;
            return true;
        }
        catch (Exception exception)
        {
            PluginLog.Debug("CloudSave", $"Refreshing the cloud-save list failed: {exception.Message}");
            return false;
        }
    }

    private static void PersistCloudIndex()
    {
        try
        {
            azf<aqa>.Instance.v();
        }
        catch (Exception exception)
        {
            PluginLog.Debug("CloudSave", $"Persisting the cloud-save index failed: {exception.Message}");
        }
    }

    private static void EnsureCloudDictionaries(aqa cloud)
    {
        if (cloud.ServerCloudSaveDict == null)
            cloud.ServerCloudSaveDict = new ConcurrentDictionary<string, aid>();
        if (cloud.ClientCloudSaveDict == null)
            cloud.ClientCloudSaveDict = new ConcurrentDictionary<string, aid>();
    }

    private static aid GetServerAid(string cloudSaveId)
    {
        aid server;
        return azf<aqa>.Instance.ServerCloudSaveDict.TryGetValue(cloudSaveId, out server)
            ? server
            : null;
    }

    private static void TrySetLink(GameM game, string cloudSaveId)
    {
        try
        {
            LinkStore.Set(game.GUID, cloudSaveId);
        }
        catch (Exception exception)
        {
            PluginLog.Warn("CloudSave",
                $"Saving the LAN cloud-save mapping failed: {exception.Message}");
        }
    }

    private static void TryRemoveLink(GameM game)
    {
        try
        {
            LinkStore.Remove(game.GUID);
        }
        catch (Exception exception)
        {
            PluginLog.Debug("CloudSave",
                $"Removing the LAN cloud-save mapping failed: {exception.Message}");
        }
    }

    private static bool IsCloudEnabled()
    {
        return SafeGet(() => azf<aqa>.Instance.Switch, false);
    }

    private static bool IsHost(GameM game)
    {
        if (game == null) return false;
        try
        {
            var property = game.GetType().GetProperty("IsHost", InstanceFlags);
            return property?.GetValue(game) is bool isHost && isHost;
        }
        catch (Exception exception)
        {
            PluginLog.Debug("CloudSave", $"Reading the LAN host flag failed: {exception.Message}");
            return false;
        }
    }

    private static bool TryBeginOperation(GameM game, out string error)
    {
        lock (OperationGate)
        {
            if (_busyGameId != null)
            {
                error = "已有云存档任务正在进行，请稍候。";
                return false;
            }

            _busyGameId = game?.GUID ?? string.Empty;
            error = null;
            return true;
        }
    }

    private static void EndOperation()
    {
        lock (OperationGate)
        {
            _busyGameId = null;
        }
    }

    private static string DescribeBusyState(aia state)
    {
        switch (state)
        {
            case aia.b:
                return "正在压缩存档，请稍候。";
            case aia.c:
            case aia.d:
                return "正在从云端下载存档，请稍候。";
            case aia.e:
            case aia.f:
                return "正在上传存档，请稍候。";
            case aia.g:
                return "游戏运行中不能同步云存档。";
            default:
                return "云存档任务正在进行，请稍候。";
        }
    }

    private static string DescribeUploadFailure(int result)
    {
        switch (result)
        {
            case 1:
                return "上传已取消。";
            case 2:
                return "上传预处理失败，请检查存档文件或云存档容量。";
            case 3:
                return "上传网络请求失败，请检查网络后重试。";
            case 4:
                return "服务器拒绝了云存档上传请求。";
            default:
                return "上传云存档失败，错误码 " + result + "。";
        }
    }

    private static string DescribeDownloadFailure(int result)
    {
        switch (result)
        {
            case 1:
                return "下载已取消。";
            case 2:
                return "云端存档文件校验失败，未应用下载内容。";
            case 3:
                return "下载网络请求失败，请检查网络后重试。";
            default:
                return "下载云存档失败，错误码 " + result + "。";
        }
    }

    private static List<ahz> CopyList(List<ahz> values)
    {
        return values == null ? new List<ahz>() : new List<ahz>(values);
    }

    private static T SafeGet<T>(Func<T> getter, T fallback)
    {
        try
        {
            return getter();
        }
        catch (Exception)
        {
            return fallback;
        }
    }

    private static EntityResponse<CloudSaveEntity> CreateRemoteCloudSave(bool vipOnly)
    {
        var method = GetCloudSaveProtocolMethod("j");
        try
        {
            return (EntityResponse<CloudSaveEntity>)method.Invoke(null, new object[] { vipOnly });
        }
        catch (TargetInvocationException exception)
        {
            throw exception.InnerException ?? exception;
        }
    }

    private static int DeleteRemoteCloudSave(string cloudSaveId)
    {
        var method = GetCloudSaveProtocolMethod("f");
        try
        {
            return (int)method.Invoke(null, new object[] { cloudSaveId });
        }
        catch (TargetInvocationException exception)
        {
            throw exception.InnerException ?? exception;
        }
    }

    private static int RefreshRemoteCloudSaveList(
        string userId,
        ref ConcurrentDictionary<string, aid> cloudSaves)
    {
        var method = GetCloudSaveProtocolMethod("h");
        var arguments = new object[] { userId, cloudSaves ?? new ConcurrentDictionary<string, aid>() };
        try
        {
            var result = (int)method.Invoke(null, arguments);
            cloudSaves = arguments[1] as ConcurrentDictionary<string, aid> ??
                         new ConcurrentDictionary<string, aid>();
            return result;
        }
        catch (TargetInvocationException exception)
        {
            throw exception.InnerException ?? exception;
        }
    }

    private static MethodInfo GetCloudSaveProtocolMethod(string methodName)
    {
        if (CloudSaveProtocolType == null)
            throw new MissingMemberException("WPFLauncher cloud-save protocol type was not found.");

        var method = CloudSaveProtocolType.GetMethod(methodName,
            BindingFlags.Public | BindingFlags.Static);
        if (method == null)
            throw new MissingMethodException(CloudSaveProtocolType.FullName, methodName);
        return method;
    }
}
