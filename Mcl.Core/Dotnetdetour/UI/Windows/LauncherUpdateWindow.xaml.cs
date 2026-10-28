using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
    /// 启动器更新窗口（WinUI 3 风格）：提示 → 下载/解压（带进度）→ 就绪，失败时显示错误。
    /// 下载与解压在这里完成后，由 UpdateManager 交给 DeferredPatchApplier 在宿主退出后应用。
    /// </summary>
    public partial class LauncherUpdateWindow : Window
    {
        private static readonly string[] ExcludedHookFiles =
        {
            DeferredPatchApplier.HookAssemblyName,
            DeferredPatchApplier.HookSymbolName
        };

        private readonly LauncherUpdatePlan _plan;
        private readonly string _installDirectory;
        private readonly string _hostExecutable;
        private readonly string _sessionDirectory;
        private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();
        private readonly Stopwatch _speedWatch = Stopwatch.StartNew();

        private long _packageBaseBytes;
        private long _lastProgressBytes;
        private long _lastProgressMilliseconds;
        private double _smoothedSpeed;
        private int _packageIndex;
        private bool _working;
        private bool _closingAfterCancel;

        public LauncherUpdateWindow(
            LauncherUpdatePlan plan,
            LauncherChannelProfile profile,
            string notice,
            string installDirectory,
            string hostExecutablePath,
            string sessionDirectory)
        {
            InitializeComponent();
            _plan = plan ?? throw new ArgumentNullException(nameof(plan));
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }
            _installDirectory = installDirectory;
            _hostExecutable = hostExecutablePath;
            _sessionDirectory = sessionDirectory;

            CurrentVersionText.Text = plan.CurrentVersion?.ToString() ?? "未知";
            NewestVersionText.Text = plan.NewestVersion?.ToString() ?? "未知";
            ChannelText.Text = $"更新渠道：{profile.DisplayName} · 启动器与插件共用官方更新源";
            NotesText.Text = string.IsNullOrWhiteSpace(notice) ? "官方暂未提供本次更新的说明。" : notice;
            SizeText.Text = $"下载大小约 {FormatSize(plan.TotalDownloadSize)} · 共 {plan.PackageCount} 个更新包（已排除插件文件）";

            Loaded += OnWindowLoaded;
            Closing += OnWindowClosing;
            Closed += (_, __) => _cancellation.Dispose();
        }

        /// <summary>更新准备完成后的应用请求；用户取消或未开始时为 null。</summary>
        public DeferredPatchApplyRequest ApplyRequest { get; private set; }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnWindowLoaded;

            // WinUI 风格的轻微入场动画。
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
                // 下载过程中不允许直接关窗，先取消并等待循环安全退出。
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

        private async void BtnUpdate_Click(object sender, RoutedEventArgs e)
        {
            _working = true;
            ShowWorking();

            try
            {
                Directory.CreateDirectory(_sessionDirectory);
                var sevenZipPath = SevenZipExtractor.ResolveSevenZipPath(_installDirectory);

                string majorInstaller = null;
                string minorPatchDirectory = null;

                if (_plan.MajorVersion != null)
                {
                    majorInstaller = await PreparePackageAsync(sevenZipPath, _plan.MajorVersion, _plan.MajorPatch, true);
                }

                if (_plan.MinorVersion != null)
                {
                    minorPatchDirectory = await PreparePackageAsync(sevenZipPath, _plan.MinorVersion, _plan.MinorPatch, false);
                }

                ApplyRequest = new DeferredPatchApplyRequest
                {
                    HostProcessId = Process.GetCurrentProcess().Id,
                    HostExecutablePath = _hostExecutable,
                    LauncherDirectory = _installDirectory,
                    SessionDirectory = _sessionDirectory,
                    MajorInstallerPath = majorInstaller,
                    MinorPatchDirectory = minorPatchDirectory
                };

                SetStepState(StepApplyChip, StepApplyText, StepState.Active);
                await Task.Delay(300);
                ShowReady();
                await Task.Delay(1400);
                _working = false;
                _closingAfterCancel = true;
                Close();
            }
            catch (OperationCanceledException)
            {
                _working = false;
                _closingAfterCancel = true;
                Close();
            }
            catch (Exception ex)
            {
                _working = false;
                PluginLog.Error("Update", ex, "准备启动器更新失败");
                ShowError(ex.Message);
            }
        }

        private async Task<string> PreparePackageAsync(
            string sevenZipPath,
            Version version,
            LauncherPatchInfo patch,
            bool isInstaller)
        {
            if (patch == null || string.IsNullOrWhiteSpace(patch.Url))
            {
                throw new InvalidOperationException($"更新包 {version} 缺少下载地址");
            }

            _packageIndex++;
            var archivePath = Path.Combine(_sessionDirectory, version + ".7z");
            var extractDirectory = Path.Combine(_sessionDirectory, version.ToString());

            SetStepState(StepDownloadChip, StepDownloadText, StepState.Active);
            SetStageTexts($"正在下载更新包 {_packageIndex}/{_plan.PackageCount}", version.ToString());
            ResetDownloadProgress();

            var downloadResult = await Task.Run(() => LauncherPatchDownloader.Download(
                patch.Url,
                archivePath,
                patch.Size,
                patch.Md5,
                (bytes, total) => ReportDownloadProgress(bytes, total),
                _cancellation.Token));

            if (downloadResult == LauncherPatchDownloader.Cancelled || _cancellation.IsCancellationRequested)
            {
                throw new OperationCanceledException();
            }

            if (downloadResult != LauncherPatchDownloader.Ok)
            {
                throw new IOException($"下载更新包 {version} 失败：{LauncherPatchDownloader.GetMessage(downloadResult)}");
            }

            _packageBaseBytes += patch.Size;
            SetStepState(StepDownloadChip, StepDownloadText, StepState.Done);

            SetStepState(StepExtractChip, StepExtractText, StepState.Active);
            SetStageTexts($"正在解压更新包 {_packageIndex}/{_plan.PackageCount}", version + " · 已跳过插件文件");
            StageProgress.IsIndeterminate = true;
            PercentText.Text = string.Empty;
            TrafficText.Text = string.Empty;

            var extractResult = await Task.Run(() => SevenZipExtractor.Extract(
                sevenZipPath,
                archivePath,
                extractDirectory,
                ExcludedHookFiles,
                _cancellation.Token));

            StageProgress.IsIndeterminate = false;
            if (extractResult == SevenZipExtractor.Cancelled || _cancellation.IsCancellationRequested)
            {
                throw new OperationCanceledException();
            }

            if (extractResult != SevenZipExtractor.Ok)
            {
                throw new IOException($"解压更新包 {version} 失败：{SevenZipExtractor.GetMessage(extractResult)}");
            }

            SetStepState(StepExtractChip, StepExtractText, StepState.Done);

            if (isInstaller)
            {
                var files = Directory.GetFiles(extractDirectory, "*", SearchOption.AllDirectories);
                var installer = files.FirstOrDefault(file => file.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));
                if (files.Length != 1 || installer == null)
                {
                    throw new IOException("大版本更新包内容无效（应只包含一个安装器）");
                }

                return installer;
            }

            if (!Directory.EnumerateFiles(extractDirectory, "*", SearchOption.AllDirectories).Any())
            {
                throw new IOException("更新包解压后没有任何文件");
            }

            return extractDirectory;
        }

        private void ResetDownloadProgress()
        {
            _lastProgressBytes = 0;
            _lastProgressMilliseconds = _speedWatch.ElapsedMilliseconds;
            _smoothedSpeed = 0;
            StageProgress.IsIndeterminate = false;
            StageProgress.Value = 0;
            PercentText.Text = "0%";
            TrafficText.Text = "正在连接…";
        }

        private void ReportDownloadProgress(long bytes, long total)
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

            var totalBytes = Math.Max(1, _plan.TotalDownloadSize);
            var completed = Math.Min(totalBytes, _packageBaseBytes + bytes);
            var percent = completed * 100d / totalBytes;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                StageProgress.Value = Math.Max(0, Math.Min(100, percent));
                PercentText.Text = percent.ToString("F1") + "%";
                var traffic = $"{FormatSize(bytes)} / {FormatSize(total)}";
                if (_smoothedSpeed > 1024)
                {
                    traffic += " · " + FormatSize((long)_smoothedSpeed) + "/s";
                }

                TrafficText.Text = traffic;
            }));
        }

        private void SetStageTexts(string title, string detail)
        {
            StageTitle.Text = title;
            StageDetail.Text = detail;
        }

        private void ShowWorking()
        {
            PromptPanel.Visibility = Visibility.Collapsed;
            ReadyPanel.Visibility = Visibility.Collapsed;
            ErrorPanel.Visibility = Visibility.Collapsed;
            WorkingPanel.Visibility = Visibility.Visible;
            PromptButtons.Visibility = Visibility.Collapsed;
            ErrorButtons.Visibility = Visibility.Collapsed;
            WorkButtons.Visibility = Visibility.Visible;
            BtnCloseX.IsEnabled = false;
            FooterHint.Text = "更新期间请勿关闭启动器，下载完成后会自动重启。";
        }

        private void ShowReady()
        {
            _working = false;
            WorkingPanel.Visibility = Visibility.Collapsed;
            ReadyPanel.Visibility = Visibility.Visible;
            WorkButtons.Visibility = Visibility.Collapsed;
            FooterHint.Text = string.Empty;
            ReadyDetail.Text = $"新版本 {_plan.NewestVersion} 将在启动器重启后生效。";
        }

        private void ShowError(string message)
        {
            _closingAfterCancel = true;
            PromptPanel.Visibility = Visibility.Collapsed;
            WorkingPanel.Visibility = Visibility.Collapsed;
            ReadyPanel.Visibility = Visibility.Collapsed;
            ErrorPanel.Visibility = Visibility.Visible;
            PromptButtons.Visibility = Visibility.Collapsed;
            WorkButtons.Visibility = Visibility.Collapsed;
            ErrorButtons.Visibility = Visibility.Visible;
            BtnCloseX.IsEnabled = true;
            ErrorText.Text = message;
            FooterHint.Text = "可稍后重试，或在启动器 Updater 目录手动运行 MCLauncherUpdater.exe。";
        }

        private enum StepState
        {
            Idle,
            Active,
            Done
        }

        private static void SetStepState(FrameworkElement chip, System.Windows.Controls.TextBlock text, StepState state)
        {
            switch (state)
            {
                case StepState.Active:
                    chip.SetResourceReference(System.Windows.Controls.Border.BackgroundProperty, "AccentBrush");
                    text.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, "OnAccentBrush");
                    break;
                case StepState.Done:
                    chip.SetResourceReference(System.Windows.Controls.Border.BackgroundProperty, "SelectionBrush");
                    text.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, "AccentBrush");
                    break;
                default:
                    chip.SetResourceReference(System.Windows.Controls.Border.BackgroundProperty, "SubtleSurfaceBrush");
                    text.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, "SecondaryTextBrush");
                    break;
            }
        }

        private static string FormatSize(long bytes)
        {
            if (bytes <= 0)
            {
                return "0 MB";
            }

            var mb = bytes / 1024d / 1024d;
            return mb >= 1024 ? (mb / 1024d).ToString("F2") + " GB" : mb.ToString("F1") + " MB";
        }
    }
}
