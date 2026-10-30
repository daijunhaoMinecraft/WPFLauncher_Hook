using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Mcl.Core.Dotnetdetour.UI.Themes;
using Mcl.Core.Dotnetdetour.Utilities.Diagnostics;
using Mcl.Core.NeteaseProtocol;
using Newtonsoft.Json.Linq;
using WPFLauncher.Code;
using WPFLauncher.Model;
using WPFLauncher.Network.Launcher;
using WPFLauncher.ViewModel.Chat;

namespace Mcl.Core.Dotnetdetour.UI.Controls;

public partial class AddFriendPanel : UserControl
{
    private const string NoSignature = "暂无签名";

    private readonly ObservableCollection<FriendSearchResult> _uidResults = new();
    private readonly ObservableCollection<FriendSearchResult> _nameResults = new();
    private ne _viewModel;
    private DispatcherTimer _searchTimeout;
    private int _searchVersion;

    public AddFriendPanel()
    {
        InitializeComponent();
        FluentTheme.Apply(this);
        UidResultsList.ItemsSource = _uidResults;
        NameResultsList.ItemsSource = _nameResults;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    internal void Attach(ne viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        SearchBox.Focus();
        SearchBox.SelectAll();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        Interlocked.Increment(ref _searchVersion);
        StopSearchTimeout();
    }

    private void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        StartSearch();
    }

    private void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        e.Handled = true;
        StartSearch();
    }

    private void StartSearch()
    {
        if (_viewModel == null) return;

        var version = Interlocked.Increment(ref _searchVersion);
        StopSearchTimeout();
        SetSearching(false);

        var query = SearchBox.Text?.Trim() ?? string.Empty;
        if (query.Length == 0)
        {
            ShowMessageState("请输入搜索内容", "输入玩家昵称或纯数字 UserID。");
            return;
        }

        var numericQuery = IsNumericQuery(query);
        var userId = 0U;
        if (numericQuery)
        {
            if (!uint.TryParse(query, NumberStyles.None, CultureInfo.InvariantCulture, out userId) ||
                userId == 0)
            {
                ShowMessageState("玩家编号无效", "请输入有效的 UserID 数字。");
                return;
            }
        }
        else if (query.Length < 2)
        {
            ShowMessageState("昵称太短", "按昵称搜索时请至少输入 2 个字符。");
            return;
        }

        ClearResults();
        SetSearching(true);
        SetBusyState("正在搜索", numericQuery
            ? "正在同时查询 UserID 和玩家名称..."
            : "正在查询玩家名称...");

        StartSearchTimeout(version);
        try
        {
            if (numericQuery)
                SearchByUserIdAndName(version, userId);
            else
                SearchByName(version, query);
        }
        catch (Exception exception)
        {
            PluginLog.Error("Chat", exception, "Starting a friend search failed.");
            CompleteSearch(version, () =>
                ShowMessageState("搜索失败", "无法发起好友搜索，请稍后重试。"));
        }
    }

    private void SearchByUserIdAndName(int version, uint userId)
    {
        var uidTask = new TaskCompletionSource<List<FriendSearchResult>>();
        var nameTask = new TaskCompletionSource<List<FriendSearchResult>>();

        try
        {
            acp.ae(userId, response =>
            {
                try
                {
                    uidTask.TrySetResult(MapUidProfile(userId, response?.entity));
                }
                catch (Exception exception)
                {
                    PluginLog.Debug("Chat", $"Mapping the UserID result failed: {exception.Message}");
                    uidTask.TrySetResult(new List<FriendSearchResult>());
                }
            });
        }
        catch (Exception exception)
        {
            PluginLog.Debug("Chat", $"Starting the UserID search failed: {exception.Message}");
            uidTask.TrySetResult(new List<FriendSearchResult>());
        }

        try
        {
            acp.x(userId.ToString(CultureInfo.InvariantCulture), response =>
            {
                try
                {
                    nameTask.TrySetResult(MapSearchResults(response));
                }
                catch (Exception exception)
                {
                    PluginLog.Debug("Chat", $"Mapping the player-name result failed: {exception.Message}");
                    nameTask.TrySetResult(new List<FriendSearchResult>());
                }
            });
        }
        catch (Exception exception)
        {
            PluginLog.Debug("Chat", $"Starting the player-name search failed: {exception.Message}");
            nameTask.TrySetResult(new List<FriendSearchResult>());
        }

        Task.WhenAll(uidTask.Task, nameTask.Task).ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                PluginLog.Error("Chat", task.Exception, "Combining UserID search results failed.");
                CompleteSearch(version, () =>
                    ShowMessageState("搜索失败", "无法合并 UserID 与玩家名称搜索结果，请稍后重试。"));
                return;
            }

            var uidResults = uidTask.Task.Result ?? new List<FriendSearchResult>();
            var nameResults = nameTask.Task.Result ?? new List<FriendSearchResult>();
            EnrichResults(uidResults.Concat(nameResults));
            CompleteSearch(version, () =>
            {
                ApplyResults(uidResults, nameResults, true);
                ResultSummaryText.Text = "已返回两类搜索结果";
            });
        }, TaskScheduler.Default);
    }

    private void SearchByName(int version, string query)
    {
        acp.x(query, response =>
        {
            if (response == null)
            {
                CompleteSearch(version, () =>
                    ShowMessageState("搜索失败", "服务器没有返回搜索结果。"));
                return;
            }

            if (response.code == 113)
            {
                CompleteSearch(version, () =>
                    ShowMessageState("搜索过于频繁", "请稍等几秒后再试。"));
                return;
            }

            if (response.code != 0)
            {
                CompleteSearch(version, () =>
                    ShowMessageState("搜索失败", GetResponseMessage(response.message)));
                return;
            }

            List<FriendSearchResult> results;
            try
            {
                results = MapSearchResults(response);
            }
            catch (Exception exception)
            {
                PluginLog.Error("Chat", exception, "Mapping player search results failed.");
                CompleteSearch(version, () =>
                    ShowMessageState("搜索失败", "无法解析服务器返回的玩家搜索结果。"));
                return;
            }

            Task.Run(() => EnrichResults(results)).ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    PluginLog.Debug("Chat", $"Enriching player search results failed: {task.Exception}");
                }

                var enriched = task.IsFaulted ? results : task.Result;
                CompleteSearch(version, () => ApplyResults(
                    new List<FriendSearchResult>(), enriched, false));
            }, TaskScheduler.Default);
        });
    }

    private void ResultList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count == 0)
        {
            if (UidResultsList.SelectedItem == null && NameResultsList.SelectedItem == null)
                ClearSelectedResult();
            return;
        }

        if (e.AddedItems[0] is not FriendSearchResult result) return;

        var otherList = ReferenceEquals(sender, UidResultsList) ? NameResultsList : UidResultsList;
        if (otherList.SelectedItem != null) otherList.SelectedItem = null;
        SelectResult(result);
    }

    private void ClearSelectedResult()
    {
        if (_viewModel == null) return;
        _viewModel.SelectedUser = null;
        FriendInfoPanel.Visibility = Visibility.Collapsed;
        FriendInfoEmptyPanel.Visibility = Visibility.Visible;
    }

    private void SelectResult(FriendSearchResult result)
    {
        _viewModel.SelectedUser = result.ToUserModel();
        SelectedAvatar.HeadImgResource = result.AvatarUrl ?? string.Empty;
        SelectedNicknameText.Text = result.Nickname;
        SelectedUserIdText.Text = result.UserIdText;
        SelectedSignatureText.Text = string.IsNullOrWhiteSpace(result.Signature)
            ? NoSignature
            : result.Signature;
        FriendInfoEmptyPanel.Visibility = Visibility.Collapsed;
        FriendInfoPanel.Visibility = Visibility.Visible;
    }

    private void ApplyResults(List<FriendSearchResult> uidResults,
        List<FriendSearchResult> nameResults, bool showUidResults)
    {
        ClearResults();
        ResultsSectionsPanel.Visibility = Visibility.Visible;
        EmptyStatePanel.Visibility = Visibility.Collapsed;
        UidResultsSection.Visibility = showUidResults ? Visibility.Visible : Visibility.Collapsed;

        foreach (var result in uidResults) _uidResults.Add(result);
        foreach (var result in nameResults) _nameResults.Add(result);
        foreach (var result in uidResults.Concat(nameResults))
            _viewModel.SearchFirendList.Add(result.ToUserModel());

        UidResultCountText.Text = showUidResults ? $"{uidResults.Count} 位" : string.Empty;
        UidResultsList.Visibility = uidResults.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        UidResultsEmptyText.Visibility = uidResults.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        NameResultCountText.Text = $"{nameResults.Count} 位";
        NameResultsList.Visibility = nameResults.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        NameResultsEmptyText.Visibility = nameResults.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        if (!showUidResults)
            ResultSummaryText.Text = nameResults.Count == 0
                ? "没有匹配结果"
                : $"找到 {nameResults.Count} 位玩家";

        if (uidResults.Count > 0)
            UidResultsList.SelectedItem = uidResults[0];
        else if (nameResults.Count > 0)
            NameResultsList.SelectedItem = nameResults[0];
    }

    private void ClearResults()
    {
        UidResultsList.SelectedItem = null;
        NameResultsList.SelectedItem = null;
        _uidResults.Clear();
        _nameResults.Clear();
        _viewModel.SearchFirendList.Clear();
        _viewModel.SelectedUser = null;
        UidResultCountText.Text = string.Empty;
        NameResultCountText.Text = string.Empty;
        UidResultsEmptyText.Visibility = Visibility.Collapsed;
        NameResultsEmptyText.Visibility = Visibility.Collapsed;
        FriendInfoPanel.Visibility = Visibility.Collapsed;
        FriendInfoEmptyPanel.Visibility = Visibility.Visible;
    }

    private void SetBusyState(string title, string detail)
    {
        ResultsSectionsPanel.Visibility = Visibility.Collapsed;
        EmptyStatePanel.Visibility = Visibility.Visible;
        EmptyStateTitle.Text = title;
        EmptyStateDetail.Text = detail;
        ResultSummaryText.Text = "正在搜索";
    }

    private void ShowMessageState(string title, string detail)
    {
        ClearResults();
        ResultsSectionsPanel.Visibility = Visibility.Collapsed;
        EmptyStatePanel.Visibility = Visibility.Visible;
        EmptyStateTitle.Text = title;
        EmptyStateDetail.Text = detail;
        ResultSummaryText.Text = "搜索未完成";
    }

    private void SetSearching(bool searching)
    {
        SearchButton.IsEnabled = !searching;
        SearchButton.Content = searching ? "搜索中" : "搜索";
        SearchProgress.Visibility = searching ? Visibility.Visible : Visibility.Collapsed;
    }

    private void CompleteSearch(int version, Action apply)
    {
        if (Dispatcher.HasShutdownStarted) return;
        Dispatcher.BeginInvoke(new Action(() =>
        {
            if (version != _searchVersion) return;
            StopSearchTimeout();
            SetSearching(false);
            apply();
        }), DispatcherPriority.Background);
    }

    private void StartSearchTimeout(int version)
    {
        StopSearchTimeout();
        _searchTimeout = new DispatcherTimer(DispatcherPriority.Background, Dispatcher)
        {
            Interval = TimeSpan.FromSeconds(15)
        };
        _searchTimeout.Tick += (_, _) =>
        {
            if (version != _searchVersion) return;
            Interlocked.Increment(ref _searchVersion);
            StopSearchTimeout();
            SetSearching(false);
            ShowMessageState("搜索超时", "服务器响应超时，请稍后重试。");
        };
        _searchTimeout.Start();
    }

    private void StopSearchTimeout()
    {
        if (_searchTimeout == null) return;
        _searchTimeout.Stop();
        _searchTimeout = null;
    }

    private static bool IsNumericQuery(string query)
    {
        return query.Length > 0 && query.All(char.IsDigit);
    }

    private static List<FriendSearchResult> MapUidProfile(uint userId,
        acm.UserProfileEntity profile)
    {
        if (profile == null) return new List<FriendSearchResult>();

        return new List<FriendSearchResult>
        {
            new()
            {
                UserId = userId,
                Nickname = string.IsNullOrWhiteSpace(profile.name) ? $"玩家 {userId}" : profile.name,
                AvatarUrl = profile.avatar_image_url,
                Signature = NoSignature
            }
        };
    }

    private static List<FriendSearchResult> MapSearchResults(
        EntityListResponse<SearchFriendEntity> response)
    {
        if (response == null || response.code != 0 || response.entities == null)
            return new List<FriendSearchResult>();

        return response.entities
            .Where(entity => entity != null)
            .Select(entity => new FriendSearchResult
            {
                UserId = entity.uid,
                Nickname = string.IsNullOrWhiteSpace(entity.nickname)
                    ? $"玩家 {entity.uid}"
                    : entity.nickname,
                AvatarUrl = entity.headimage,
                Signature = NoSignature
            })
            .ToList();
    }

    private static List<FriendSearchResult> EnrichResults(IEnumerable<FriendSearchResult> source)
    {
        var results = source
            .Where(result => result != null)
            .ToList();
        if (results.Count == 0) return results;

        try
        {
            var entityIds = results
                .Select(result => result.UserIdText)
                .Distinct()
                .ToList();
            var response = X19Http.GetPlayersInfo(entityIds);
            var entities = response["entities"] as JArray;
            if (entities == null && response["entity"] is JObject singleEntity)
                entities = new JArray(singleEntity);
            if (entities == null) return results;

            var details = new Dictionary<uint, PlayerDetails>();
            foreach (var entity in entities.OfType<JObject>())
            {
                var userIdText = entity["entity_id"]?.ToString() ??
                                 entity["uid"]?.ToString() ??
                                 entity["user_id"]?.ToString();
                if (!uint.TryParse(userIdText, out var userId)) continue;
                details[userId] = new PlayerDetails
                {
                    Nickname = entity["name"]?.ToString(),
                    AvatarUrl = entity["avatar_image_url"]?.ToString(),
                    Signature = entity["signature"]?.ToString()
                };
            }

            foreach (var result in results)
            {
                if (!details.TryGetValue(result.UserId, out var detail)) continue;
                if (!string.IsNullOrWhiteSpace(detail.Nickname)) result.Nickname = detail.Nickname;
                if (!string.IsNullOrWhiteSpace(detail.AvatarUrl)) result.AvatarUrl = detail.AvatarUrl;
                result.Signature = string.IsNullOrWhiteSpace(detail.Signature)
                    ? NoSignature
                    : detail.Signature;
            }
        }
        catch (Exception exception)
        {
            PluginLog.Debug("Chat", $"Loading player signatures failed: {exception.Message}");
        }

        return results;
    }

    private static string GetResponseMessage(string message)
    {
        return string.IsNullOrWhiteSpace(message) ? "服务器拒绝了本次请求，请稍后重试。" : message;
    }

    private sealed class PlayerDetails
    {
        public string Nickname { get; set; }
        public string AvatarUrl { get; set; }
        public string Signature { get; set; }
    }
}

public sealed class FriendSearchResult
{
    public uint UserId { get; set; }
    public string Nickname { get; set; }
    public string AvatarUrl { get; set; }
    public string Signature { get; set; } = "暂无签名";
    public string UserIdText => UserId.ToString(CultureInfo.InvariantCulture);

    public UserM ToUserModel()
    {
        return new UserM
        {
            UserID = UserId,
            Nickname = Nickname,
            HeadImage = AvatarUrl,
            Signature = Signature
        };
    }
}
