using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Threading;
using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;
using Mcl.Core.Dotnetdetour.UI.Themes;
using Mcl.Core.Dotnetdetour.Utilities.Diagnostics;
using Newtonsoft.Json;
using WPFLauncher.Code;
using WPFLauncher.Common;
using WPFLauncher.Manager;
using WPFLauncher.Model;
using WPFLauncher.Network.Launcher;
using WPFLauncher.View.Chat.ChatPanelUC;

namespace Mcl.Core.Dotnetdetour.Features.GeneralHooks;

internal sealed class FriendStatusRefreshHook : IMethodHook
{
    private static readonly ConditionalWeakTable<FriendListUC, RefreshView> Views =
        new ConditionalWeakTable<FriendListUC, RefreshView>();

    [OriginalMethod]
    private static void FriendListConstructorOriginal(FriendListUC instance)
    {
    }

    [HookMethod("WPFLauncher.View.Chat.ChatPanelUC.FriendListUC", ".ctor",
        "FriendListConstructorOriginal")]
    private static void FriendListConstructorHook(FriendListUC instance)
    {
        FriendListConstructorOriginal(instance);
        if (instance == null) return;

        try
        {
            AttachRefreshBar(instance);
        }
        catch (Exception exception)
        {
            PluginLog.Error("Friend", exception, "Adding the friend-status refresh button failed.");
        }
    }

    private static void AttachRefreshBar(FriendListUC view)
    {
        if (view.Content is not UIElement originalContent || Views.TryGetValue(view, out _)) return;

        var layout = new Grid();
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var actionBar = new DockPanel
        {
            Margin = new Thickness(8, 4, 8, 6),
            MinHeight = 32
        };
        FluentTheme.Apply(actionBar);

        var button = new Button
        {
            Content = "刷新状态",
            Height = 32,
            MinWidth = 96,
            Padding = new Thickness(12, 0, 12, 0),
            ToolTip = "从服务器重新获取好友在线状态"
        };
        AutomationProperties.SetName(button, "刷新好友在线状态");
        button.SetResourceReference(FrameworkElement.StyleProperty, "PrimaryButton");
        DockPanel.SetDock(button, Dock.Right);

        var statusText = new TextBlock
        {
            Text = "好友在线状态",
            FontSize = 12,
            Margin = new Thickness(0, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        statusText.SetResourceReference(TextBlock.ForegroundProperty, "SecondaryTextBrush");

        actionBar.Children.Add(button);
        actionBar.Children.Add(statusText);

        view.Content = null;
        try
        {
            Grid.SetRow(originalContent, 1);
            layout.Children.Add(originalContent);
            Grid.SetRow(actionBar, 0);
            layout.Children.Add(actionBar);
            view.Content = layout;
        }
        catch
        {
            Grid.SetRow(originalContent, 0);
            view.Content = originalContent;
            throw;
        }

        var refreshView = new RefreshView(button, statusText);
        Views.Add(view, refreshView);
        button.Click += (_, _) => RequestRefresh(refreshView);
    }

    private static void RequestRefresh(RefreshView view)
    {
        if (Interlocked.Exchange(ref view.IsRefreshing, 1) != 0) return;

        view.Button.IsEnabled = false;
        SetStatusText(view, "正在从服务器刷新好友状态...", "SecondaryTextBrush");

        var timeout = new DispatcherTimer(DispatcherPriority.Background, view.Button.Dispatcher)
        {
            Interval = TimeSpan.FromSeconds(15)
        };
        timeout.Tick += (_, _) => CompleteRefresh(view, timeout, "刷新超时，请稍后重试", false);
        timeout.Start();

        try
        {
            acp.p(response =>
            {
                string message;
                var success = false;
                try
                {
                    var updated = ApplyResponse(response);
                    message = $"已刷新 {updated} 位好友";
                    success = true;
                }
                catch (Exception exception)
                {
                    PluginLog.Error("Friend", exception, "Refreshing friend statuses failed.");
                    message = "刷新失败，请稍后重试";
                }

                if (view.Button.Dispatcher.HasShutdownStarted) return;
                view.Button.Dispatcher.BeginInvoke(
                    new Action(() => CompleteRefresh(view, timeout, message, success)),
                    DispatcherPriority.Background);
            });
        }
        catch (Exception exception)
        {
            PluginLog.Error("Friend", exception, "Starting the friend-status refresh failed.");
            CompleteRefresh(view, timeout, "刷新失败，请稍后重试", false);
        }
    }

    private static void CompleteRefresh(RefreshView view, DispatcherTimer timeout, string message, bool success)
    {
        timeout.Stop();
        if (Interlocked.Exchange(ref view.IsRefreshing, 0) == 0) return;

        view.Button.IsEnabled = true;
        SetStatusText(view, message, success ? "SuccessBrush" : "DangerBrush");
    }

    private static void SetStatusText(RefreshView view, string text, string brushKey)
    {
        view.StatusText.Text = text;
        view.StatusText.SetResourceReference(TextBlock.ForegroundProperty, brushKey);
    }

    private static int ApplyResponse(EntityListResponse<FriendWithDetailEntity> response)
    {
        if (response?.entities == null)
            throw new InvalidOperationException("The friend-status response did not contain entities.");

        var clusterList = azf<arg>.Instance.ClusterList;
        if (clusterList == null)
            throw new InvalidOperationException("The launcher friend list is not available.");

        var updated = 0;
        foreach (var friend in response.entities)
        {
            if (friend == null) continue;

            var presence = ParsePresence(friend);
            FriendStatusStore.SetPresence(friend.uid,
                presence.State != OnLineState.OFFLINE && presence.State != OnLineState.LOGOUT);

            var user = clusterList.f(friend.uid);
            if (user == null) continue;

            if (uint.TryParse(friend.tLogout, out var logoutTime)) user.TLogout = logoutTime;
            clusterList.r(friend.uid, presence.State, presence.GameName,
                presence.Game, presence.Terminal);
            updated++;
        }

        return updated;
    }

    private static FriendPresence ParsePresence(FriendWithDetailEntity friend)
    {
        if (string.IsNullOrWhiteSpace(friend.online_status))
            return new FriendPresence(OnLineState.OFFLINE, OnlineTerminal.OFFLINE, null, string.Empty);

        try
        {
            var status = JsonConvert.DeserializeObject<UserStatusEntity>(friend.online_status);
            if (status == null)
                return new FriendPresence(OnLineState.OFFLINE, OnlineTerminal.OFFLINE, null, string.Empty);

            UserStatusGameEntity game = null;
            var gameName = status.hint ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(status.hint))
            {
                try
                {
                    game = JsonConvert.DeserializeObject<UserStatusGameEntity>(status.hint);
                    if (game != null) gameName = FormatGameName(game);
                }
                catch (JsonException)
                {
                    // Some online hints are display text rather than a game entity.
                }
            }

            return new FriendPresence(MapOnlineState(status.status), MapTerminal(friend.online_pcpe),
                game, gameName);
        }
        catch (Exception exception)
        {
            PluginLog.Debug("Friend", $"Ignoring malformed friend status for UID {friend.uid}: {exception.Message}");
            return new FriendPresence(OnLineState.OFFLINE, OnlineTerminal.OFFLINE, null, string.Empty);
        }
    }

    private static OnLineState MapOnlineState(uint status)
    {
        switch (status)
        {
            case 1: return OnLineState.ONLINE;
            case 2: return OnLineState.BUSY;
            case 3: return OnLineState.LEFT;
            default: return OnLineState.OFFLINE;
        }
    }

    private static OnlineTerminal MapTerminal(uint terminal)
        => Enum.IsDefined(typeof(OnlineTerminal), (int)terminal)
            ? (OnlineTerminal)terminal
            : OnlineTerminal.OFFLINE;

    private static string FormatGameName(UserStatusGameEntity game)
    {
        if (game == null || string.IsNullOrWhiteSpace(game.game_name)) return string.Empty;

        try
        {
            var resources = azf<arh>.Instance;
            return resources.c<string>("CHAT_MAIN/ONLINE_PLAYING_GAME1") +
                   game.game_name +
                   resources.c<string>("CHAT_MAIN/ONLINE_PLAYING_GAME2");
        }
        catch
        {
            return game.game_name;
        }
    }

    private sealed class RefreshView
    {
        public RefreshView(Button button, TextBlock statusText)
        {
            Button = button;
            StatusText = statusText;
        }

        public Button Button { get; }
        public TextBlock StatusText { get; }
        public int IsRefreshing;
    }

    private sealed class FriendPresence
    {
        public FriendPresence(OnLineState state, OnlineTerminal terminal,
            UserStatusGameEntity game, string gameName)
        {
            State = state;
            Terminal = terminal;
            Game = game;
            GameName = gameName;
        }

        public OnLineState State { get; }
        public OnlineTerminal Terminal { get; }
        public UserStatusGameEntity Game { get; }
        public string GameName { get; }
    }
}
