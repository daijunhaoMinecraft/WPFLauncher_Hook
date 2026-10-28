using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Mcl.Core.Dotnetdetour.Features.NetworkAndRoom;
using Mcl.Core.Dotnetdetour.UI.Themes;
using Mcl.Core.Dotnetdetour.Utilities.Diagnostics;
using WPFLauncher.Model;

namespace Mcl.Core.Dotnetdetour.UI.Controls;

public partial class LanCloudSavePanel : UserControl
{
    private GameM _game;
    private INotifyPropertyChanged _observedGame;
    private bool _operationRunning;
    private int _refreshVersion;

    public LanCloudSavePanel()
    {
        InitializeComponent();
        FluentTheme.Apply(this);
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        DataContextChanged += (sender, args) =>
            SetGame(LanCloudSaveService.GetSelectedGame(DataContext));
    }

    internal void SetGame(GameM game)
    {
        if (_observedGame != null)
        {
            _observedGame.PropertyChanged -= OnGamePropertyChanged;
            _observedGame = null;
        }

        if (!LanCloudSaveService.IsLanGame(game))
        {
            _game = null;
            Visibility = Visibility.Collapsed;
            return;
        }

        _game = game;
        ObserveGame(game);
        Visibility = Visibility.Visible;
        _ = RefreshAsync();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        LanCloudSaveService.StateChanged -= OnCloudStateChanged;
        LanCloudSaveService.StateChanged += OnCloudStateChanged;
        ObserveGame(_game);
        _ = RefreshAsync();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        LanCloudSaveService.StateChanged -= OnCloudStateChanged;
        if (_observedGame != null)
        {
            _observedGame.PropertyChanged -= OnGamePropertyChanged;
            _observedGame = null;
        }
    }

    private void OnCloudStateChanged(GameM game)
    {
        if (!ReferenceEquals(game, _game)) return;
        Dispatcher.BeginInvoke(new Action(() =>
        {
            if (IsLoaded) _ = RefreshAsync();
        }), DispatcherPriority.Background);
    }

    private void OnGamePropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != "Status" && e.PropertyName != "IsSaveExist" &&
            e.PropertyName != "CloudStatus" && e.PropertyName != "IsLaunched")
        {
            return;
        }

        Dispatcher.BeginInvoke(new Action(() => _ = RefreshAsync()), DispatcherPriority.Background);
    }

    private void ObserveGame(GameM game)
    {
        if (_observedGame != null || game == null) return;
        _observedGame = game as INotifyPropertyChanged;
        if (_observedGame != null)
            _observedGame.PropertyChanged += OnGamePropertyChanged;
    }

    private async Task RefreshAsync()
    {
        var game = _game;
        if (game == null)
        {
            Visibility = Visibility.Collapsed;
            return;
        }

        var version = ++_refreshVersion;
        StatusText.Text = "读取中";
        DetailText.Text = "正在读取本地联机云存档状态...";
        SetButtonsEnabled(false);

        LanCloudSaveState state;
        try
        {
            state = await Task.Run(() => LanCloudSaveService.ReadState(game));
        }
        catch (Exception)
        {
            if (version != _refreshVersion || !ReferenceEquals(game, _game)) return;
            StatusText.Text = "读取失败";
            DetailText.Text = "无法读取云存档状态，请稍后重试。";
            SetButtonsEnabled(false);
            return;
        }

        if (version != _refreshVersion || !ReferenceEquals(game, _game)) return;
        ApplyState(state);
    }

    private void ApplyState(LanCloudSaveState state)
    {
        PluginLog.Debug("CloudSave",
            $"LAN panel state: guid={_game?.GUID}, status={_game?.Status}, host={state.Host}, " +
            $"cloud={state.CloudEnabled}, localSave={state.LocalSaveExists}, hasCloud={state.HasCloudSave}, " +
            $"busy={state.Busy}, canUpload={state.CanUpload}, canDownload={state.CanDownload}.");

        Visibility = state.Supported ? Visibility.Visible : Visibility.Collapsed;
        StatusText.Text = state.Status ?? "云存档";
        DetailText.Text = state.Detail ?? string.Empty;

        UploadButton.Content = "云端上传";
        UploadButton.IsEnabled = !_operationRunning && state.CanUpload;
        DownloadButton.IsEnabled = !_operationRunning && state.CanDownload;
        ManageButton.IsEnabled = !_operationRunning && state.CloudEnabled;

        UploadButton.ToolTip = state.Host
            ? (state.HasCloudSave ? "把当前本地联机存档上传到云端" : "创建云存档并上传当前本地联机存档")
            : "仅房主可上传云存档";
        DownloadButton.ToolTip = state.HasCloudSave
            ? "用云端存档覆盖当前本地联机存档"
            : "当前房间尚未关联云端存档";
        ManageButton.ToolTip = state.CloudEnabled
            ? "打开启动器云存档管理"
            : "云存档服务尚未就绪";
    }

    private async void Upload_Click(object sender, RoutedEventArgs e)
    {
        var game = _game;
        if (game == null) return;

        SetBusy(true, "正在上传", "正在压缩并上传当前本地联机存档...");
        LanCloudSaveOperationResult result;
        try
        {
            result = await Task.Run(() => LanCloudSaveService.Upload(game));
        }
        catch (Exception exception)
        {
            result = LanCloudSaveOperationResult.Fail("上传云存档失败: " + exception.Message);
        }
        finally
        {
            SetBusy(false, null, null);
        }

        if (!result.Success)
            ShowMessage(result.Message, "上传云存档", MessageBoxImage.Error);
        await RefreshAsync();
    }

    private async void Download_Click(object sender, RoutedEventArgs e)
    {
        var game = _game;
        if (game == null) return;

        var choice = ShowQuestion(
            "从云端下载会用云端存档覆盖当前本地联机存档。确定继续吗？",
            "下载云存档");
        if (choice != MessageBoxResult.Yes) return;

        SetBusy(true, "正在下载", "正在下载并应用云端存档...");
        LanCloudSaveOperationResult result;
        try
        {
            result = await Task.Run(() => LanCloudSaveService.Download(game));
        }
        catch (Exception exception)
        {
            result = LanCloudSaveOperationResult.Fail("下载云存档失败: " + exception.Message);
        }
        finally
        {
            SetBusy(false, null, null);
        }

        if (!result.Success)
            ShowMessage(result.Message, "下载云存档", MessageBoxImage.Error);
        await RefreshAsync();
    }

    private async void Manage_Click(object sender, RoutedEventArgs e)
    {
        var result = LanCloudSaveService.OpenManager();
        if (!result.Success)
        {
            ShowMessage(result.Message, "云存档管理", MessageBoxImage.Error);
            return;
        }

        await RefreshAsync();
    }

    private void SetBusy(bool busy, string status, string detail)
    {
        _operationRunning = busy;
        SetButtonsEnabled(!busy);
        if (!string.IsNullOrWhiteSpace(status)) StatusText.Text = status;
        if (!string.IsNullOrWhiteSpace(detail)) DetailText.Text = detail;
    }

    private void SetButtonsEnabled(bool enabled)
    {
        UploadButton.IsEnabled = enabled;
        DownloadButton.IsEnabled = enabled;
        ManageButton.IsEnabled = enabled;
    }

    private void ShowMessage(string message, string title, MessageBoxImage image)
    {
        if (string.IsNullOrWhiteSpace(message)) return;
        var owner = Window.GetWindow(this);
        if (owner == null)
            MessageBox.Show(message, title, MessageBoxButton.OK, image);
        else
            MessageBox.Show(owner, message, title, MessageBoxButton.OK, image);
    }

    private MessageBoxResult ShowQuestion(string message, string title)
    {
        var owner = Window.GetWindow(this);
        return owner == null
            ? MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning)
            : MessageBox.Show(owner, message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning);
    }
}
