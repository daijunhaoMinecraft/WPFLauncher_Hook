using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using Mcl.Core.Updater;

namespace Mcl.Core.Dotnetdetour.UI.Windows
{
    public partial class DownloadProgressWindow : Window
    {
        private readonly string _originalDownloadUrl;
        private readonly string _targetFileName;
        private readonly int _mirrorChoice;

        public DownloadProgressWindow(string downloadUrl, string targetFileName, int mirrorChoice)
        {
            InitializeComponent();
            _originalDownloadUrl = downloadUrl;
            _targetFileName = targetFileName;
            _mirrorChoice = mirrorChoice;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await PerformDownloadAndRestartAsync();
        }

        private async Task PerformDownloadAndRestartAsync()
        {
            string[] downloadLines;

            // 0:自动 1:直连 2:gh-proxy 3:ghproxy.net
            switch (_mirrorChoice)
            {
                case 1: downloadLines = new[] { _originalDownloadUrl }; break;
                case 2: downloadLines = new[] { $"https://gh-proxy.com/{_originalDownloadUrl}" }; break;
                case 3: downloadLines = new[] { $"https://ghproxy.net/{_originalDownloadUrl}" }; break;
                default:
                    downloadLines = new[] {
                        $"https://gh-proxy.com/{_originalDownloadUrl}", 
                        $"https://ghproxy.net/{_originalDownloadUrl}",
                        _originalDownloadUrl
                    };
                    break;
            }

            bool downloadSuccess = false;
            Exception lastException = null;

            foreach (var downloadUrl in downloadLines)
            {
                try
                {
                    TxtStatus.Text = $"正在连接节点...";
                    using (var response = await UpdateManager.SharedHttpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
                    {
                        response.EnsureSuccessStatusCode();

                        long? totalBytes = response.Content.Headers.ContentLength;
                        
                        using (var stream = await response.Content.ReadAsStreamAsync())
                        using (var fileStream = new FileStream(_targetFileName + ".temp", FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            byte[] buffer = new byte[81920];
                            long totalRead = 0;
                            int bytesRead;

                            // 分块读取数据，并刷新进度条
                            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                            {
                                await fileStream.WriteAsync(buffer, 0, bytesRead);
                                totalRead += bytesRead;

                                if (totalBytes.HasValue)
                                {
                                    double percentage = (double)totalRead / totalBytes.Value * 100;
                                    PbDownload.Value = percentage;
                                    TxtStatus.Text = $"正在下载... {percentage:F1}%";
                                    TxtSpeed.Text = $"{totalRead / 1024 / 1024.0:F2} MB / {totalBytes.Value / 1024 / 1024.0:F2} MB";
                                }
                                else
                                {
                                    PbDownload.IsIndeterminate = true;
                                    TxtStatus.Text = $"正在下载 (未知大小)...";
                                    TxtSpeed.Text = $"{totalRead / 1024 / 1024.0:F2} MB 已下载";
                                }
                            }
                        }
                    }
                    downloadSuccess = true;
                    break; // 只要有一条线路成功，跳出循环
                }
                catch (Exception ex)
                {
                    lastException = ex;
                }
            }

            if (!downloadSuccess)
            {
                MessageBox.Show($"更新包下载失败，所选线路无法连通，请检查您的网络！\n错误信息: {lastException?.Message}",
                    "下载失败", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close(); // 下载失败关闭窗口，主线程恢复启动原版
                return;
            }

            try
            {
                TxtStatus.Text = "下载完成，正在替换文件...";
                PbDownload.IsIndeterminate = true;

                // Windows 黑科技: 即使 DLL 正在运行(被加载)，依然可以重命名它
                if (File.Exists(_targetFileName)) File.Move(_targetFileName, _targetFileName + ".old");
                File.Move(_targetFileName + ".temp", _targetFileName);

                Process.Start("WPFLauncher.exe");
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                MessageBox.Show("下载成功但更新替换失败，可能被安全软件拦截: " + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
        }
    }
}