using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Mcl.Core.Dotnetdetour.Utilities.Diagnostics;
using Mcl.Core.Updater;

namespace Mcl.Core.Dotnetdetour.UI.Windows
{
    /// <summary>
    /// Mcl.Core.dll 自更新窗口（WinUI 3 风格）：提示/节点选择 → 下载 → 替换 → 就绪，失败时显示错误。
    /// 与启动器更新窗口相互独立；替换完成后由 HookUpdateManager 调度重启宿主。
    /// </summary>
    public partial class HookUpdateWindow : Window
    {
        private readonly HookUpdateInfo _info;
        private readonly string _hookPath;
        private readonly string _hostExecutable;
        private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();
        private readonly Stopwatch _speedWatch = Stopwatch.StartNew();

        private long _lastProgressBytes;
        private long _lastProgressMilliseconds;
        private double _smoothedSpeed;
        private bool _working;
        private bool _closingAfterCancel;

        public HookUpdateWindow(HookUpdateInfo info, string hookPath, string hostExecutablePath)
        {
            InitializeComponent();
            _info = info ?? throw new ArgumentNullException(nameof(info));
            _hookPath = hookPath;
            _hostExecutable = hostExecutablePath;

            VersionText.Text = $"{info.Version} · Mcl.Core.dll 插件更新（与启动器更新相互独立）";
            SizeText.Text = info.Size > 0
                ? $"下载大小约 {FormatSize(info.Size)} · 校验方式：{FormatHashKind(info.HashType)}"
                : $"校验方式：{FormatHashKind(info.HashType)}";
            NotesViewer.Document = MarkdownWpfParser.Parse(info.Notes);

            Loaded += OnWindowLoaded;
            Closing += OnWindowClosing;
            Closed += (_, __) => _cancellation.Dispose();
        }

        /// <summary>文件已替换并等待重启时为 true。</summary>
        public bool Updated { get; private set; }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnWindowLoaded;

            var duration = TimeSpan.FromMilliseconds(180);
            var scale = new ScaleTransform(0.985, 0.985);
            RootBorder.RenderTransform = scale;
            RootBorder.RenderTransformOrigin = new Point(0.5, 0.5);
            BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, duration));
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(0.985, 1, duration));
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(0.985, 1, duration));
        }

        private void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_working && !_closingAfterCancel)
            {
                e.Cancel = true;
                RequestCancel();
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BtnLater_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            RequestCancel();
        }

        private void RequestCancel()
        {
            if (_cancellation.IsCancellationRequested)
            {
                return;
            }

            _cancellation.Cancel();
            BtnCancel.IsEnabled = false;
            BtnCancel.Content = "正在取消…";
            StageTitle.Text = "正在取消更新…";
        }

        private void BtnViewChanges_Click(object sender, RoutedEventArgs e)
        {
            var url = _info.PageUrl;
            if (string.IsNullOrEmpty(url))
            {
                return;
            }

            // 与下载节点保持一致，便于大陆网络打开 GitHub 页面。
            if (CmbMirror.SelectedIndex == 2)
            {
                url = "https://gh-proxy.com/" + url;
            }
            else if (CmbMirror.SelectedIndex == 3)
            {
                url = "https://ghproxy.net/" + url;
            }

            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                PluginLog.Warn("Update", "打开更新页面失败: " + ex.Message);
            }
        }

        private async void BtnUpdate_Click(object sender, RoutedEventArgs e)
        {
            _working = true;
            ShowWorking();

            var downloadPath = _hookPath + ".update-download";
            try
            {
                var urls = HookUpdateManager.BuildMirrorUrls(_info.DownloadUrl, CmbMirror.SelectedIndex);
                ResetProgress();

                var downloaded = await Task.Run(() =>
                {
                    var ok = HookUpdateManager.Download(urls, downloadPath, _info.Size,
                        (bytes, total) => ReportProgress(bytes, total), _cancellation.Token, out var error);
                    return (Ok: ok, Error: error);
                });

                if (_cancellation.IsCancellationRequested)
                {
                    throw new OperationCanceledException();
                }

                if (!downloaded.Ok)
                {
                    throw new IOException("下载更新包失败：" + downloaded.Error);
                }

                StageTitle.Text = "正在校验更新包…";
                StageDetail.Text = FormatHashKind(_info.HashType);
                StageProgress.IsIndeterminate = true;
                PercentText.Text = string.Empty;
                TrafficText.Text = string.Empty;

                var verify = await Task.Run(() =>
                {
                    var ok = HookUpdateManager.VerifyHash(downloadPath, _info, out var error);
                    return (Ok: ok, Error: error);
                });
                if (!verify.Ok)
                {
                    throw new IOException(verify.Error);
                }

                StageTitle.Text = "正在替换 Mcl.Core.dll…";
                var replace = await Task.Run(() =>
                {
                    var ok = HookUpdateManager.ReplaceHookFile(downloadPath, _hookPath, out var error);
                    return (Ok: ok, Error: error);
                });
                StageProgress.IsIndeterminate = false;
                if (!replace.Ok)
                {
                    throw new IOException("替换 Mcl.Core.dll 失败：" + replace.Error);
                }

                Updated = true;
                ShowReady();
                await Task.Delay(1200);
                _working = false;
                _closingAfterCancel = true;
                Close();
            }
            catch (OperationCanceledException)
            {
                _working = false;
                _closingAfterCancel = true;
                TryDelete(downloadPath);
                Close();
            }
            catch (Exception ex)
            {
                _working = false;
                TryDelete(downloadPath);
                PluginLog.Error("Update", ex, "Mcl.Core.dll 自更新失败");
                ShowError(ex.Message);
            }
        }

        private void ResetProgress()
        {
            StageTitle.Text = "正在下载更新包…";
            StageDetail.Text = "Mcl.Core.dll";
            _lastProgressBytes = 0;
            _lastProgressMilliseconds = _speedWatch.ElapsedMilliseconds;
            _smoothedSpeed = 0;
            StageProgress.IsIndeterminate = false;
            StageProgress.Value = 0;
            PercentText.Text = "0%";
            TrafficText.Text = "正在连接…";
        }

        private void ReportProgress(long bytes, long total)
        {
            var now = _speedWatch.ElapsedMilliseconds;
            var elapsed = now - _lastProgressMilliseconds;
            if (elapsed >= 80)
            {
                var instant = (bytes - _lastProgressBytes) * 1000d / Math.Max(1, elapsed);
                _smoothedSpeed = _smoothedSpeed <= 0 ? instant : (_smoothedSpeed * 0.65) + (instant * 0.35);
                _lastProgressBytes = bytes;
                _lastProgressMilliseconds = now;
            }

            var percent = total > 0 ? Math.Min(100d, bytes * 100d / total) : 0d;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                StageProgress.Value = percent;
                PercentText.Text = total > 0 ? percent.ToString("F1") + "%" : FormatSize(bytes);
                var traffic = total > 0 ? $"{FormatSize(bytes)} / {FormatSize(total)}" : FormatSize(bytes);
                if (_smoothedSpeed > 1024)
                {
                    traffic += " · " + FormatSize((long)_smoothedSpeed) + "/s";
                }

                TrafficText.Text = traffic;
            }));
        }

        private void ShowWorking()
        {
            PromptPanel.Visibility = Visibility.Collapsed;
            ReadyPanel.Visibility = Visibility.Collapsed;
            ErrorPanel.Visibility = Visibility.Collapsed;
            WorkingPanel.Visibility = Visibility.Visible;
            PromptExtras.Visibility = Visibility.Collapsed;
            PromptButtons.Visibility = Visibility.Collapsed;
            ErrorButtons.Visibility = Visibility.Collapsed;
            WorkButtons.Visibility = Visibility.Visible;
            BtnCloseX.IsEnabled = false;
        }

        private void ShowReady()
        {
            _working = false;
            WorkingPanel.Visibility = Visibility.Collapsed;
            ReadyPanel.Visibility = Visibility.Visible;
            WorkButtons.Visibility = Visibility.Collapsed;
            ReadyDetail.Text = $"{_info.Version} 已在启动器重启后生效。";
        }

        private void ShowError(string message)
        {
            _closingAfterCancel = true;
            PromptPanel.Visibility = Visibility.Collapsed;
            WorkingPanel.Visibility = Visibility.Collapsed;
            ReadyPanel.Visibility = Visibility.Collapsed;
            ErrorPanel.Visibility = Visibility.Visible;
            PromptExtras.Visibility = Visibility.Collapsed;
            PromptButtons.Visibility = Visibility.Collapsed;
            WorkButtons.Visibility = Visibility.Collapsed;
            ErrorButtons.Visibility = Visibility.Visible;
            BtnCloseX.IsEnabled = true;
            ErrorText.Text = message;
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.SetAttributes(path, FileAttributes.Normal);
                    File.Delete(path);
                }
            }
            catch (Exception ex)
            {
                PluginLog.Debug("Update", "清理临时更新文件失败: " + ex.Message);
            }
        }

        private static string FormatHashKind(HookUpdateHashKind kind)
        {
            switch (kind)
            {
                case HookUpdateHashKind.GitBlobSha1: return "Git blob SHA1（与仓库内容一致）";
                case HookUpdateHashKind.Sha256: return "SHA256（Release 资产 digest）";
                default: return "无（Release 未提供 digest）";
            }
        }

        private static string FormatSize(long bytes)
        {
            if (bytes <= 0)
            {
                return "0 MB";
            }

            var mb = bytes / 1024d / 1024d;
            return mb >= 1024 ? (mb / 1024d).ToString("F2") + " GB" : mb.ToString("F2") + " MB";
        }
    }
}
