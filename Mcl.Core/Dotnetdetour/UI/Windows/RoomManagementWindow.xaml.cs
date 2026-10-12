using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Mcl.Core.Dotnetdetour.Features.NetworkAndRoom;
using Mcl.Core.Dotnetdetour.Models.Config;
using Mcl.Core.Dotnetdetour.UI.Themes;
using Mcl.Core.Dotnetdetour.Utilities.Diagnostics;
using Mcl.Core.NeteaseProtocol;
using Mcl.Core.Tools;
using Newtonsoft.Json.Linq;
using WPFLauncher.Common;
using WPFLauncher.Manager;
using WPFLauncher.Model.Game;
using WPFLauncher.Network.TransService;
using WPFLauncher.View.Launcher.LobbyGame;

namespace Mcl.Core.Dotnetdetour.UI.Windows;

/// <summary>
///     联机大厅房间管理页面: 查看房间状态、修改房门密码、调整可见性/存档并维护房间玩家。
///     密码修改通过 <see cref="RoomSettingsUpdater" /> 的 detour 请求完成。
/// </summary>
public partial class RoomManagementWindow : Window
{
    private sealed class PlayerRow
    {
        public string UserId { get; set; }
        public string DisplayName { get; set; }
        public string AvatarUrl { get; set; }
        public string Role { get; set; }
        public bool CanManage { get; set; }
        public bool CanInteract { get; set; }
    }

    private const string DefaultAvatarUrl = "https://x19.fp.ps.netease.com/file/5a34e0777f9d2a8a4ea3d36eza31LhW3";

    private delegate bool HostRoomAction(out string error);

    private bool busy;
    private bool closingWithoutLeave;
    private LobbyGameRoomManagerView hostRoomView;
    private INotifyPropertyChanged hostGameNotifier;
    private readonly DispatcherTimer hostStateTimer;

    public RoomManagementWindow()
    {
        InitializeComponent();
        FluentTheme.Apply(this);
        InitializeMouseWheelForwarding();
        // 宿主房间状态由宿主自身线程更新, 这里做周期性校正, 保证按钮不会停留在旧状态。
        hostStateTimer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher)
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        hostStateTimer.Tick += (sender, args) => RefreshHostState();
        hostStateTimer.Start();
        RefreshFromConfig();
    }

    /// <summary>在 UI 线程上打开唯一的房间管理窗口。</summary>
    public static void ShowForCurrentRoom()
    {
        ShowForRoom(RoomWindowReplacement.GetHostRoomView());
    }

    /// <summary>由房间窗口替换 Hook 调用: 绑定宿主房间窗口并显示房间管理窗口。</summary>
    public static void ShowForRoom(LobbyGameRoomManagerView roomView)
    {
        var application = Application.Current;
        if (application == null) return;

        if (roomView != null)
        {
            roomView.Closed -= OnHostRoomViewClosed;
            roomView.Closed += OnHostRoomViewClosed;
        }

        RoomManagementWindow existing = null;
        foreach (Window window in application.Windows)
        {
            if (!(window is RoomManagementWindow managementWindow)) continue;
            existing = managementWindow;
            break;
        }

        if (existing == null)
        {
            var window = new RoomManagementWindow { hostRoomView = roomView };
            window.RefreshFromConfig();
            window.Show();
            return;
        }

        existing.hostRoomView = roomView;
        existing.RefreshFromConfig();
        existing.Activate();
    }

    private static void OnHostRoomViewClosed(object sender, EventArgs e)
    {
        CloseForRoom(sender as LobbyGameRoomManagerView);
    }

    protected override void OnClosed(EventArgs e)
    {
        hostStateTimer.Stop();
        if (hostGameNotifier != null)
        {
            hostGameNotifier.PropertyChanged -= OnHostGamePropertyChanged;
            hostGameNotifier = null;
        }

        base.OnClosed(e);
    }

    /// <summary>右上角关闭按钮等同于退出房间 (与启动器房间窗口一致)。</summary>
    protected override void OnClosing(CancelEventArgs e)
    {
        var shuttingDown = Application.Current?.Dispatcher?.HasShutdownStarted == true;
        if (!closingWithoutLeave && !shuttingDown && WpfConfig.RoomInfo?.entity != null && RoomOperations.HasHostRoom)
        {
            e.Cancel = true;
            // 必须在 Closing 结束之后再执行宿主命令: 宿主确认框会以当前活动窗口为 owner,
            // 在窗口处于 closing 状态时设置 owner / 取句柄会抛 InvalidOperationException。
            Dispatcher.BeginInvoke(DispatcherPriority.Background,
                new Action(() => RunHostAction(RoomOperations.TryLeaveRoom, "已请求退出房间。")));
            return;
        }

        base.OnClosing(e);
    }

    /// <summary>内层列表/下拉框会吃掉滚轮, 这里把滚轮转发给页面 ScrollViewer。</summary>
    private void InitializeMouseWheelForwarding()
    {
        foreach (var element in new UIElement[]
                 {
                     PlayerListView, ActivityList, VisibilityCombo, PasswordInput, PasswordInputVisible
                 })
            element.PreviewMouseWheel += ForwardMouseWheelToPage;
    }

    private void ForwardMouseWheelToPage(object sender, MouseWheelEventArgs e)
    {
        if (e.Handled || PageScrollViewer == null) return;

        // 内部控件自己还能滚动时保持原有行为, 否则交给页面滚动。
        if (sender is DependencyObject owner)
        {
            var inner = FindVisualChild<ScrollViewer>(owner);
            if (inner != null)
            {
                var canScroll = e.Delta > 0
                    ? inner.VerticalOffset > 0
                    : inner.VerticalOffset < inner.ScrollableHeight;
                if (canScroll) return;
            }
        }

        e.Handled = true;
        PageScrollViewer.RaiseEvent(new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
        {
            RoutedEvent = UIElement.MouseWheelEvent
        });
    }

    private static T FindVisualChild<T>(DependencyObject root) where T : DependencyObject
    {
        if (root == null) return null;
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is T match) return match;
            var nested = FindVisualChild<T>(child);
            if (nested != null) return nested;
        }

        return null;
    }

    /// <summary>房间/游戏状态推送到达时刷新按钮与状态显示 (由网络层调用, 可在任意线程)。</summary>
    public static void NotifyRoomStateChanged()
    {
        RunOnWindows(window => window.RefreshHostState());
    }

    /// <summary>玩家加入/离开或成员列表刷新时同步玩家列表。</summary>
    public static void NotifyRoomPlayersChanged()
    {
        RunOnWindows(window =>
        {
            window.RefreshHostState();
            _ = window.RefreshPlayersAsync();
        });
    }

    /// <summary>房间信息推送到达: 同步可见性/允许存档等控件, 并把本次变化写进房间动态。</summary>
    public static void NotifyRoomInfoChanged(IReadOnlyList<string> changes)
    {
        RunOnWindows(window => window.ApplyRoomInfoChanges(changes));
    }

    private void ApplyRoomInfoChanges(IReadOnlyList<string> changes)
    {
        RefreshRoomSettings();
        RefreshHostState();
        if (changes == null) return;
        foreach (var change in changes) AddActivity($"房间更新: {change}");
    }

    /// <summary>按本地房间实体刷新设置控件 (房间推送到达后实体已是最新值)。</summary>
    private void RefreshRoomSettings()
    {
        var entity = WpfConfig.RoomInfo?.entity;
        BuildVisibilityOptions(entity?.visibility);
        AllowSaveToggle.IsChecked = entity != null && entity.allow_save;
        VisibilityText.Text = entity != null ? DescribeVisibility(entity.visibility) : Text(null);
        AllowSaveText.Text = entity != null ? (entity.allow_save ? "允许" : "不允许") : Text(null);
        PasswordStateText.Text = entity != null ? (entity.password ? "已开启" : "未开启") : Text(null);
        SetEditingEnabled(entity != null && IsCurrentUserOwner(entity.owner_id) && !busy);
    }

    /// <summary>
    ///     网络线程调用: 把窗口操作派发到 UI 线程。
    ///     注意枚举 <see cref="Application.Windows" /> 本身也必须在 UI 线程上执行。
    /// </summary>
    private static void RunOnWindows(Action<RoomManagementWindow> action)
    {
        var application = Application.Current;
        if (application == null) return;

        var dispatcher = application.Dispatcher;
        if (dispatcher == null || dispatcher.HasShutdownStarted) return;

        void Run()
        {
            foreach (var window in application.Windows.OfType<RoomManagementWindow>().ToArray())
            {
                try
                {
                    action(window);
                }
                catch (Exception exception)
                {
                    PluginLog.Debug("RoomManage", $"Updating the room management window failed: {exception.Message}");
                }
            }
        }

        if (dispatcher.CheckAccess())
            Run();
        else
            dispatcher.BeginInvoke((Action)Run);
    }

    /// <summary>宿主房间窗口关闭(退出房间/被踢)时关闭对应的房间管理窗口。</summary>
    public static void CloseForRoom(LobbyGameRoomManagerView roomView)
    {
        foreach (var window in Application.Current?.Windows.OfType<RoomManagementWindow>().ToArray() ??
                               Array.Empty<RoomManagementWindow>())
        {
            if (window.hostRoomView != null && window.hostRoomView != roomView) continue;
            if (window.closingWithoutLeave) continue;
            window.closingWithoutLeave = true;
            CloseWindow(window);
        }
    }

    /// <summary>离开房间时关闭所有房间管理窗口。</summary>
    public static void CloseAll()
    {
        foreach (var window in Application.Current?.Windows.OfType<RoomManagementWindow>().ToArray() ??
                               Array.Empty<RoomManagementWindow>())
        {
            if (window.closingWithoutLeave) continue;
            window.closingWithoutLeave = true;
            CloseWindow(window);
        }
    }

    /// <summary>关闭窗口; 窗口自己正在 Closing 中时忽略重复关闭。</summary>
    private static void CloseWindow(RoomManagementWindow window)
    {
        try
        {
            window.Close();
        }
        catch (InvalidOperationException exception)
        {
            PluginLog.Debug("RoomManage", $"Closing the room management window was skipped: {exception.Message}");
        }
    }

    private static string CurrentUserId
    {
        get
        {
            try
            {
                var userId = azf<arg>.Instance?.User?.Id;
                if (!string.IsNullOrEmpty(userId)) return userId;
            }
            catch (Exception)
            {
                // 宿主尚未完成登录时退回插件记录的账号 ID。
            }

            return X19Crypt.UserId ?? string.Empty;
        }
    }

    /// <summary>
    ///     刷新页面。refreshSettings 为 false 时保留用户刚提交的可见性/允许存档选择,
    ///     等到服务器推送回来再把控件对齐到服务器状态。
    /// </summary>
    private void RefreshFromConfig(bool refreshSettings = true)
    {
        var entity = WpfConfig.RoomInfo?.entity;
        var hasRoom = entity != null;

        RoomSubtitleText.Text = hasRoom
            ? $"房间号: {entity.room_name}    房间ID: {entity.entity_id}"
            : "当前未加入联机大厅房间。";

        RoomNameText.Text = Text(entity?.room_name);
        RoomIdText.Text = Text(entity?.entity_id);
        OwnerText.Text = Text(entity?.owner_id);
        PlayerCountText.Text = hasRoom ? entity.cur_num.ToString() : Text(null);
        MaxCountText.Text = hasRoom ? entity.max_count.ToString() : Text(null);
        VisibilityText.Text = hasRoom ? DescribeVisibility(entity.visibility) : Text(null);
        AllowSaveText.Text = hasRoom ? (entity.allow_save ? "允许" : "不允许") : Text(null);
        PasswordStateText.Text = hasRoom ? (entity.password ? "已开启" : "未开启") : Text(null);
        GameStatusText.Text = DescribeGameState(hasRoom);
        VersionText.Text = Text(entity?.version);
        ResIdText.Text = Text(entity?.res_id);

        if (refreshSettings)
        {
            BuildVisibilityOptions(entity?.visibility);
            AllowSaveToggle.IsChecked = hasRoom && entity.allow_save;
        }

        var isOwner = hasRoom && IsCurrentUserOwner(entity.owner_id);
        SetEditingEnabled(hasRoom && isOwner && !busy);
        PlayersHintText.Text = !hasRoom
            ? "当前未加入房间，无法读取玩家列表。"
            : isOwner
                ? "你是房主，可以踢出玩家、转移房主、维护黑名单，也可以修改房间密码与设置。"
                : "只有房主才能修改房间设置；所有成员都可以加好友或举报房间。";

        RefreshHostActions(isOwner);
        RefreshBlacklist();
        _ = RefreshPlayersAsync();
    }

    private string DescribeGameState(bool hasRoom)
    {
        if (!hasRoom) return Text(null);
        switch (RoomOperations.GetGameState())
        {
            case LobbyGameState.SERVER_ON:
                return "在线（可进入）";
            case LobbyGameState.SERVER_OFF:
                return "未开启";
            case LobbyGameState.SERVER_OPENING:
                return "正在开启…";
            case LobbyGameState.SERVER_CLOSING:
                return "正在关闭…";
            case LobbyGameState.SERVER_RESETTING:
                return "正在重置…";
        }

        // 宿主状态不可用时退回房间推送里的 game_status
        return WpfConfig.RoomInfo?.entity?.game_status == 1 ? "在线（可进入）" : "未开启";
    }

    private LobbyGameState GetHostGameState()
    {
        var state = RoomOperations.GetGameState();
        if (state != LobbyGameState.None) return state;

        var entity = WpfConfig.RoomInfo?.entity;
        if (entity == null) return LobbyGameState.None;
        return entity.game_status == 1 ? LobbyGameState.SERVER_ON : LobbyGameState.SERVER_OFF;
    }

    /// <summary>房间操作按钮直接执行宿主房间窗口的命令, 因此状态与宿主保持一致。</summary>
    private void RefreshHostActions(bool? ownerOverride = null)
    {
        BindHostGameNotifier();
        var hasHostRoom = hostRoomView != null || RoomOperations.HasHostRoom;
        var state = GetHostGameState();
        var isOwner = ownerOverride ?? IsCurrentUserOwner(WpfConfig.RoomInfo?.entity?.owner_id);
        var ready = hasHostRoom && !busy;

        GameOperateButton.Content = state == LobbyGameState.SERVER_ON ? "进入游戏" : "开始游戏";
        GameOperateButton.IsEnabled = ready &&
                                      (state == LobbyGameState.SERVER_OFF || state == LobbyGameState.SERVER_ON);
        InviteButton.IsEnabled = ready;
        BackupSaveButton.IsEnabled = ready && isOwner;
        ResetSaveButton.IsEnabled = ready && isOwner;
        ReportRoomButton.IsEnabled = ready;
        LeaveRoomButton.IsEnabled = ready;

        OperationStatusText.Text = hasHostRoom
            ? "以上操作直接调用启动器房间命令, 结果与启动器自带房间窗口一致。"
            : "未检测到启动器房间窗口, 房间操作暂不可用（可在设置中关闭窗口替换后使用启动器界面）。";
    }

    /// <summary>宿主房间状态 (SERVER_OFF/SERVER_OPENING/SERVER_ON) 变化时立即刷新按钮。</summary>
    private void BindHostGameNotifier()
    {
        var notifier = RoomOperations.GetSelectedGame() as INotifyPropertyChanged;
        if (ReferenceEquals(notifier, hostGameNotifier)) return;

        if (hostGameNotifier != null) hostGameNotifier.PropertyChanged -= OnHostGamePropertyChanged;
        hostGameNotifier = notifier;
        if (hostGameNotifier != null) hostGameNotifier.PropertyChanged += OnHostGamePropertyChanged;
    }

    private void OnHostGamePropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        OnUi(RefreshHostState);
    }

    /// <summary>刷新游戏状态显示与房间操作按钮 (读取宿主房间 ViewModel 的当前状态)。</summary>
    private void RefreshHostState()
    {
        GameStatusText.Text = DescribeGameState(WpfConfig.RoomInfo?.entity != null);
        RefreshHostActions();
    }

    private static string Text(string value) => string.IsNullOrEmpty(value) ? "—" : value;

    private static bool IsCurrentUserOwner(string ownerId)
    {
        if (string.IsNullOrEmpty(ownerId)) return false;
        return string.Equals(CurrentUserId, ownerId, StringComparison.Ordinal);
    }

    private void SetEditingEnabled(bool enabled)
    {
        PasswordInput.IsEnabled = enabled;
        PasswordInputVisible.IsEnabled = enabled;
        ShowPasswordCheck.IsEnabled = enabled;
        SavePasswordButton.IsEnabled = enabled;
        ClearPasswordButton.IsEnabled = enabled;
        VisibilityCombo.IsEnabled = enabled;
        AllowSaveToggle.IsEnabled = enabled;
        ApplySettingsButton.IsEnabled = enabled;
    }

    private static string DescribeVisibility(RoomVisibleStatus visibility)
    {
        var field = visibility.GetType().GetField(visibility.ToString());
        var description = (DescriptionAttribute)field?
            .GetCustomAttributes(typeof(DescriptionAttribute), false)
            .FirstOrDefault();
        return string.IsNullOrEmpty(description?.Description) ? visibility.ToString() : description.Description;
    }

    private void BuildVisibilityOptions(RoomVisibleStatus? selected)
    {
        VisibilityCombo.Items.Clear();
        foreach (RoomVisibleStatus value in Enum.GetValues(typeof(RoomVisibleStatus)))
            VisibilityCombo.Items.Add(new ComboBoxItem { Content = DescribeVisibility(value), Tag = value });

        if (!selected.HasValue) return;
        foreach (var item in VisibilityCombo.Items.OfType<ComboBoxItem>())
        {
            if (!Equals(item.Tag, selected.Value)) continue;
            VisibilityCombo.SelectedItem = item;
            break;
        }
    }

    private RoomVisibleStatus GetSelectedVisibility()
    {
        if (VisibilityCombo.SelectedItem is ComboBoxItem item && item.Tag is RoomVisibleStatus value) return value;
        return WpfConfig.RoomInfo?.entity?.visibility ?? RoomVisibleStatus.OPEN;
    }

    private void OnShowPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (ShowPasswordCheck.IsChecked == true)
        {
            PasswordInputVisible.Text = PasswordInput.Password;
            PasswordInputVisible.Visibility = Visibility.Visible;
            PasswordInput.Visibility = Visibility.Collapsed;
        }
        else
        {
            PasswordInput.Password = PasswordInputVisible.Text;
            PasswordInput.Visibility = Visibility.Visible;
            PasswordInputVisible.Visibility = Visibility.Collapsed;
        }
    }

    private string ReadPasswordInput()
    {
        var password = ShowPasswordCheck.IsChecked == true ? PasswordInputVisible.Text : PasswordInput.Password;
        return password ?? string.Empty;
    }

    private void ClearPasswordInput()
    {
        PasswordInput.Clear();
        PasswordInputVisible.Clear();
    }

    private void OnSavePasswordClick(object sender, RoutedEventArgs e)
    {
        var password = ReadPasswordInput();
        if (string.IsNullOrWhiteSpace(password))
        {
            PasswordStatusText.Text = "请输入新密码，或使用“取消密码保护”。";
            return;
        }

        if (password.Length > 32)
        {
            PasswordStatusText.Text = "密码最长 32 个字符。";
            return;
        }

        ApplyRoomUpdate(password, "房间密码已更新。", message => PasswordStatusText.Text = message);
    }

    private void OnClearPasswordClick(object sender, RoutedEventArgs e)
    {
        var confirm = MessageBox.Show(this, "取消密码保护后，其他玩家无需密码即可进入房间。是否继续？",
            "取消密码保护", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.OK) return;

        ApplyRoomUpdate(string.Empty, "已取消房间密码保护。", message => PasswordStatusText.Text = message);
    }

    private void OnApplySettingsClick(object sender, RoutedEventArgs e)
    {
        ApplyRoomUpdate(null, "房间设置已更新，密码保持不变。", message => SettingsStatusText.Text = message);
    }

    private void ApplyRoomUpdate(string newPassword, string successMessage, Action<string> report)
    {
        var entity = WpfConfig.RoomInfo?.entity;
        if (entity == null)
        {
            report("当前未加入联机大厅房间。");
            return;
        }

        if (!IsCurrentUserOwner(entity.owner_id))
        {
            report("只有房主才能修改房间设置。");
            return;
        }

        busy = true;
        SetEditingEnabled(false);
        var visibility = GetSelectedVisibility();
        var allowSave = AllowSaveToggle.IsChecked == true;

        RoomSettingsUpdater.ApplyRoomSettings(entity.entity_id, visibility, allowSave, entity.owner_id, newPassword,
            result => OnUi(() =>
            {
                busy = false;
                report(result.Success
                    ? successMessage
                    : $"操作失败（{result.Code}）：{result.Message}");
                if (!result.Success)
                {
                    SetEditingEnabled(IsCurrentUserOwner(WpfConfig.RoomInfo?.entity?.owner_id));
                    return;
                }

                AddActivity(successMessage);
                if (result.PasswordChangeApplied) ClearPasswordInput();
                // 保留刚提交的选择, 等房间推送回来再对齐服务器状态。
                RefreshFromConfig(refreshSettings: false);
            }));
    }

    private async void OnRefreshClick(object sender, RoutedEventArgs e)
    {
        RefreshButton.IsEnabled = false;
        try
        {
            var error = await Task.Run(() =>
                RoomOperations.TryRefreshRoomInfo(out var message) ? null : message);

            RefreshFromConfig();
            StatusText.Text = string.IsNullOrEmpty(error)
                ? "已从服务器刷新当前房间信息。"
                : $"刷新失败（仍显示本地缓存）：{error}";
        }
        finally
        {
            RefreshButton.IsEnabled = true;
        }
    }

    private void OnRefreshPlayersClick(object sender, RoutedEventArgs e)
    {
        _ = RefreshPlayersAsync();
    }

    private async Task RefreshPlayersAsync()
    {
        var entity = WpfConfig.RoomInfo?.entity;
        var ids = entity?.fids?.Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
        if (ids == null || ids.Count == 0)
        {
            RefreshPlayers(new List<string>(), new Dictionary<string, PlayerProfile>());
            return;
        }

        var profiles = await Task.Run(() =>
        {
            var map = new Dictionary<string, PlayerProfile>();
            try
            {
                var players = X19Http.GetPlayersInfo(ids);
                foreach (var player in players["entities"] ?? new JArray())
                {
                    var id = player?["entity_id"]?.ToString();
                    if (string.IsNullOrEmpty(id)) continue;
                    map[id] = new PlayerProfile
                    {
                        Name = player["name"]?.ToString() ?? id,
                        AvatarUrl = player["avatar_image_url"]?.ToString()
                    };
                }
            }
            catch (Exception exception)
            {
                PluginLog.Debug("RoomManage", $"Loading room player names failed: {exception.Message}");
            }

            return map;
        });

        OnUi(() => RefreshPlayers(ids, profiles));
    }

    private sealed class PlayerProfile
    {
        public string Name { get; set; }
        public string AvatarUrl { get; set; }
    }

    private void RefreshPlayers(List<string> ids, Dictionary<string, PlayerProfile> profiles)
    {
        var entity = WpfConfig.RoomInfo?.entity;
        if (entity == null || ids == null)
        {
            PlayerListView.ItemsSource = null;
            return;
        }

        var ownerId = entity.owner_id;
        var currentUserId = CurrentUserId;
        var isOwner = IsCurrentUserOwner(ownerId);
        var rows = ids.Select(id =>
        {
            PlayerProfile profile = null;
            profiles?.TryGetValue(id, out profile);
            return new PlayerRow
            {
                UserId = id,
                DisplayName = profile?.Name ?? id,
                AvatarUrl = string.IsNullOrEmpty(profile?.AvatarUrl) ? DefaultAvatarUrl : profile.AvatarUrl,
                Role = (string.Equals(id, ownerId, StringComparison.Ordinal) ? "房主" : "成员") +
                       (string.Equals(id, currentUserId, StringComparison.Ordinal) ? "（我）" : string.Empty),
                CanManage = isOwner && !string.Equals(id, currentUserId, StringComparison.Ordinal),
                CanInteract = !string.Equals(id, currentUserId, StringComparison.Ordinal)
            };
        }).ToList();

        PlayerListView.ItemsSource = rows;
    }

    private void OnKickPlayerClick(object sender, RoutedEventArgs e)
    {
        var userId = ReadRowUserId(sender);
        if (userId == null || busy) return;

        if (!RoomOperations.TryKickPlayer(userId, out var error))
        {
            StatusText.Text = error;
            return;
        }

        var message = $"已请求踢出玩家 {userId}，结果由启动器提示。";
        AddActivity(message);
        StatusText.Text = message;
    }

    private void OnAddFriendClick(object sender, RoutedEventArgs e)
    {
        var userId = ReadRowUserId(sender);
        if (userId == null || busy) return;

        if (!RoomOperations.TryAddFriend(userId, out var error))
        {
            StatusText.Text = error;
            return;
        }

        var message = $"已为玩家 {userId} 打开添加好友窗口。";
        AddActivity(message);
        StatusText.Text = message;
    }

    private void RunHostAction(HostRoomAction action, string successMessage)
    {
        if (busy) return;

        if (!action(out var error))
        {
            OperationStatusText.Text = error;
            StatusText.Text = error;
            return;
        }

        OperationStatusText.Text = successMessage;
        StatusText.Text = successMessage;
        AddActivity(successMessage);
        RefreshHostActions();
    }

    private void OnGameOperateClick(object sender, RoutedEventArgs e)
    {
        RunHostAction(RoomOperations.TryOperateGame, "已通知启动器开始/进入游戏。");
    }

    private void OnInviteClick(object sender, RoutedEventArgs e)
    {
        RunHostAction(RoomOperations.TryInvitePlayers, "已打开邀请好友窗口。");
    }

    private void OnBackupSaveClick(object sender, RoutedEventArgs e)
    {
        RunHostAction(RoomOperations.TryBackupSaves, "已打开存档备份窗口。");
    }

    private void OnResetSaveClick(object sender, RoutedEventArgs e)
    {
        RunHostAction(RoomOperations.TryResetSave, "已提交重置存档请求。");
    }

    private void OnReportRoomClick(object sender, RoutedEventArgs e)
    {
        RunHostAction(RoomOperations.TryReportRoom, "已打开举报房间窗口。");
    }

    private void OnLeaveRoomClick(object sender, RoutedEventArgs e)
    {
        RunHostAction(RoomOperations.TryLeaveRoom, "已请求退出房间。");
    }

    private string BuildRoomPageLink()
    {
        return RoomInviteSharing.BuildRoomPageLink(WpfConfig.RoomInfo?.entity?.entity_id);
    }

    /// <summary>房间状态文本 (不含密码明文), 用于右键复制。</summary>
    private string BuildRoomStatusText()
    {
        var entity = WpfConfig.RoomInfo?.entity;
        if (entity == null) return null;

        var builder = new StringBuilder();
        builder.AppendLine($"房间号: {entity.room_name}");
        builder.AppendLine($"房间ID: {entity.entity_id}");
        builder.AppendLine($"房主: {entity.owner_id}");
        builder.AppendLine($"当前人数: {entity.cur_num}/{entity.max_count}");
        builder.AppendLine($"可见性: {DescribeVisibility(entity.visibility)}");
        builder.AppendLine($"允许存档: {(entity.allow_save ? "是" : "否")}");
        builder.AppendLine($"密码保护: {(entity.password ? "已开启" : "未开启")}");
        builder.AppendLine($"游戏状态: {DescribeGameState(true)}");
        builder.AppendLine($"版本号: {entity.version}");
        builder.AppendLine($"资源ID: {entity.res_id}");
        return builder.ToString().TrimEnd();
    }

    private void OnCopyRoomStatusClick(object sender, RoutedEventArgs e)
    {
        var text = BuildRoomStatusText();
        if (string.IsNullOrEmpty(text))
        {
            StatusText.Text = "当前未加入联机大厅房间。";
            return;
        }

        if (!TrySetClipboard(text)) return;
        StatusText.Text = "房间状态已复制到剪贴板。";
        AddActivity("已复制房间状态到剪贴板。");
    }

    private void OnOpenRoomPageClick(object sender, RoutedEventArgs e)
    {
        var link = BuildRoomPageLink();
        if (link == null)
        {
            ShareStatusText.Text = "当前未加入联机大厅房间。";
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(link) { UseShellExecute = true });
            ShareStatusText.Text = "已在默认浏览器中打开房间信息页面。";
        }
        catch (Exception exception)
        {
            ShareStatusText.Text = $"打开浏览器失败：{exception.Message}";
        }
    }

    private void OnCopyRoomLinkClick(object sender, RoutedEventArgs e)
    {
        var link = BuildRoomPageLink();
        if (link == null)
        {
            ShareStatusText.Text = "当前未加入联机大厅房间。";
            return;
        }

        if (TrySetClipboard(link)) ShareStatusText.Text = $"房间链接已复制：{link}";
    }

    private async void OnCopyRoomTextClick(object sender, RoutedEventArgs e)
    {
        CopyRoomTextButton.IsEnabled = false;
        try
        {
            // 房间信息包含房主名称查询(网络请求), 放到后台线程避免卡住界面。
            var text = await Task.Run(() => RoomInviteSharing.BuildRoomText());
            if (string.IsNullOrEmpty(text))
            {
                ShareStatusText.Text = "当前未加入联机大厅房间。";
                return;
            }

            if (!TrySetClipboard(text)) return;
            ShareStatusText.Text = "已复制中文房间信息(含密码)。";
            AddActivity("已复制中文房间信息(含密码)到剪贴板。");
        }
        finally
        {
            CopyRoomTextButton.IsEnabled = true;
        }
    }

    private bool TrySetClipboard(string text)
    {
        try
        {
            Clipboard.SetText(text);
            return true;
        }
        catch (Exception exception)
        {
            ShareStatusText.Text = $"写入剪贴板失败：{exception.Message}";
            return false;
        }
    }

    private void OnTransferHostClick(object sender, RoutedEventArgs e)
    {
        var userId = ReadRowUserId(sender);
        if (userId == null) return;
        var entity = WpfConfig.RoomInfo?.entity;
        if (entity == null || !RequireOwner()) return;

        var confirm = MessageBox.Show(this, $"确定将 {userId} 设为新房主吗？设置后你将成为普通成员。",
            "转移房主", MessageBoxButton.OKCancel, MessageBoxImage.Question);
        if (confirm != MessageBoxResult.OK) return;

        busy = true;
        SetEditingEnabled(false);
        RoomSettingsUpdater.ApplyRoomSettings(entity.entity_id, GetSelectedVisibility(),
            AllowSaveToggle.IsChecked == true, userId, null, result => OnUi(() =>
            {
                busy = false;
                var message = result.Success
                    ? "房主已转移，稍后房间事件会同步最新房主。"
                    : $"转移房主失败（{result.Code}）：{result.Message}";
                AddActivity(message);
                StatusText.Text = message;
                RefreshFromConfig();
            }));
    }

    private void OnToggleBlacklistClick(object sender, RoutedEventArgs e)
    {
        var userId = ReadRowUserId(sender);
        if (userId == null || busy) return;

        if (!WpfConfig.EnableRoomBlacklist)
        {
            StatusText.Text = "房间黑名单功能未开启，可在 MCL 设置中启用。";
            return;
        }

        // 已在黑名单: 移出; 否则加入并立即踢出。
        if (RoomBlacklist.UserIds.Contains(userId))
        {
            RemoveBlacklistEntry(userId);
            return;
        }

        if (!RoomBlacklist.AddUser(userId, out var error))
        {
            StatusText.Text = error;
            BlacklistStatusText.Text = error;
            return;
        }

        RefreshBlacklist();
        KickBlacklistedUser(userId, $"已加入黑名单并踢出玩家 {userId}。");
    }

    private void RefreshBlacklist()
    {
        var enabled = RoomBlacklist.IsEnabled;
        BlacklistHintText.Text = enabled
            ? "加入 UserID 或正则表达式后会自动踢出对应玩家；名单保存在启动器目录 RoomConfig 下（UserID 拉黑 / 名称正则匹配）。"
            : "房间黑名单功能未开启：请在 MCL 设置 → 联机与房间 中启用「启用房间黑名单」。";

        BlacklistList.ItemsSource = RoomBlacklist.UserIds.ToList();
        RegexList.ItemsSource = RoomBlacklist.Patterns.ToList();

        var canEdit = enabled && !busy;
        BlacklistInput.IsEnabled = canEdit;
        AddBlacklistButton.IsEnabled = canEdit;
        RemoveBlacklistButton.IsEnabled = canEdit;
        RegexInput.IsEnabled = canEdit;
        AddRegexButton.IsEnabled = canEdit;
        RemoveRegexButton.IsEnabled = canEdit;
    }

    private async void KickBlacklistedUser(string userId, string successMessage)
    {
        BlacklistStatusText.Text = $"已加入黑名单，正在踢出玩家 {userId}…";
        var error = await Task.Run(() => RoomBlacklist.TryKickPlayer(userId, out var message) ? null : message);

        var text = string.IsNullOrEmpty(error) ? successMessage : $"已加入黑名单，但踢出失败：{error}";
        BlacklistStatusText.Text = text;
        StatusText.Text = text;
        AddActivity(text);
    }

    private void OnAddBlacklistClick(object sender, RoutedEventArgs e)
    {
        if (!RequireBlacklistEnabled()) return;

        var userId = (BlacklistInput.Text ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(userId))
        {
            BlacklistStatusText.Text = "请输入要拉黑的 UserID。";
            return;
        }

        if (!RoomBlacklist.AddUser(userId, out var error))
        {
            BlacklistStatusText.Text = error;
            return;
        }

        BlacklistInput.Clear();
        RefreshBlacklist();
        KickBlacklistedUser(userId, $"已加入黑名单并踢出玩家 {userId}。");
    }

    private void OnRemoveBlacklistClick(object sender, RoutedEventArgs e)
    {
        if (!RequireBlacklistEnabled()) return;
        if (!(BlacklistList.SelectedItem is string userId))
        {
            BlacklistStatusText.Text = "请先在 UserID 列表中选中要移除的条目。";
            return;
        }

        RemoveBlacklistEntry(userId);
    }

    private void OnRemoveBlacklistEntryClick(object sender, RoutedEventArgs e)
    {
        if (!RequireBlacklistEnabled()) return;
        if ((sender as FrameworkElement)?.Tag is string userId) RemoveBlacklistEntry(userId);
    }

    private void RemoveBlacklistEntry(string userId)
    {
        if (!RoomBlacklist.RemoveUser(userId, out var error))
        {
            BlacklistStatusText.Text = error;
            return;
        }

        RefreshBlacklist();
        var message = $"已将 {userId} 移出房间黑名单。";
        BlacklistStatusText.Text = message;
        StatusText.Text = message;
        AddActivity(message);
    }

    private async void OnAddRegexClick(object sender, RoutedEventArgs e)
    {
        if (!RequireBlacklistEnabled()) return;

        var pattern = (RegexInput.Text ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(pattern))
        {
            BlacklistStatusText.Text = "请输入用于匹配玩家名称的正则表达式。";
            return;
        }

        RegexInput.Clear();
        BlacklistStatusText.Text = $"正在加入正则并检查当前房间玩家：{pattern}";
        var outcome = await Task.Run(() =>
        {
            var kicked = RoomBlacklist.KickMatchingPlayers(pattern, out var message);
            return (Message: message, Kicked: kicked);
        });

        RefreshBlacklist();
        var text = string.IsNullOrEmpty(outcome.Message)
            ? outcome.Kicked > 0
                ? $"已加入正则黑名单，并踢出 {outcome.Kicked} 名匹配玩家。"
                : "已加入正则黑名单，当前房间没有匹配的玩家。"
            : $"加入正则失败：{outcome.Message}";
        BlacklistStatusText.Text = text;
        StatusText.Text = text;
        AddActivity(text);
    }

    private void OnRemoveRegexClick(object sender, RoutedEventArgs e)
    {
        if (!RequireBlacklistEnabled()) return;
        if (!(RegexList.SelectedItem is string pattern))
        {
            BlacklistStatusText.Text = "请先在正则列表中选中要移除的条目。";
            return;
        }

        RemoveRegexEntry(pattern);
    }

    private void OnRemoveRegexEntryClick(object sender, RoutedEventArgs e)
    {
        if (!RequireBlacklistEnabled()) return;
        if ((sender as FrameworkElement)?.Tag is string pattern) RemoveRegexEntry(pattern);
    }

    private void RemoveRegexEntry(string pattern)
    {
        if (!RoomBlacklist.RemovePattern(pattern, out var error))
        {
            BlacklistStatusText.Text = error;
            return;
        }

        RefreshBlacklist();
        var message = $"已移除正则黑名单：{pattern}";
        BlacklistStatusText.Text = message;
        StatusText.Text = message;
        AddActivity(message);
    }

    private bool RequireBlacklistEnabled()
    {
        if (WpfConfig.EnableRoomBlacklist) return true;
        BlacklistStatusText.Text = "房间黑名单功能未开启，可在 MCL 设置中启用。";
        return false;
    }

    private bool RequireOwner()
    {
        var entity = WpfConfig.RoomInfo?.entity;
        if (entity != null && IsCurrentUserOwner(entity.owner_id)) return true;
        StatusText.Text = "只有房主才能执行该操作。";
        return false;
    }

    private static string ReadRowUserId(object sender)
    {
        return (sender as FrameworkElement)?.Tag as string;
    }

    private void AddActivity(string message)
    {
        OnUi(() =>
        {
            ActivityList.Items.Insert(0, new TextBlock
            {
                Text = $"[{DateTime.Now:HH:mm:ss}] {message}",
                TextWrapping = TextWrapping.Wrap
            });
            while (ActivityList.Items.Count > 100) ActivityList.Items.RemoveAt(ActivityList.Items.Count - 1);
        });
    }

    private void OnUi(Action action)
    {
        if (Dispatcher.CheckAccess())
            action();
        else
            Dispatcher.Invoke(action);
    }

}
