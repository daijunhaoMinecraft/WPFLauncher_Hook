using Mcl.Core.Dotnetdetour.Utilities.Diagnostics;

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;
using Mcl.Core.Dotnetdetour.Models.Config;
using WPFLauncher.Util;
using WPFLauncher.Util.Zip;

namespace Mcl.Core.Dotnetdetour.Features.GeneralHooks;

public class DownloadEvent : IMethodHook
{
    // HttpClient
    private static readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(120)
    };

    private long _globalDownloadedBytes;

    // 用于控制渲染线程
    private CancellationTokenSource _renderCts;
    private Task _renderTask;

    private long _totalBytes;

    // 你可以随时在这里修改线程数
    private int threadCount = 4;

    [OriginalMethod]
    private void Original_c()
    {
    }

    [HookMethod("WPFLauncher.Network.acd", "c")]
    public void DownloadFileHook(object instance)
    {
        if (!WpfConfig.EnableParallelDownloads)
        {
            Original_c();
            return;
        }

        ServicePointManager.DefaultConnectionLimit = WpfConfig.DownloadWorkerCount + 10;
        ServicePointManager.Expect100Continue = false;
        ServicePointManager.UseNagleAlgorithm = false;
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 |
                                               SecurityProtocolType.Tls13;

        threadCount = WpfConfig.DownloadWorkerCount;
        if (WpfConfig.EnableVerboseLogging) PluginLog.Debug("Download", "\n[MultiDown] === Hook Started ===");

        var realInstance = ResolveRealInstance(instance);
        if (realInstance == null)
        {
            PluginLog.Error("Download", "[MultiDown] Error: Cannot resolve instance.");
            Original_c();
            return;
        }

        var type = realInstance.GetType();
        var url = GetField<string>(type, realInstance, "f");
        var filePath = GetField<string>(type, realInstance, "g");
        var contentLength = GetField<long>(type, realInstance, "i");

        PluginLog.Debug("Download", $"[MultiDown] Target: {Path.GetFileName(filePath)}");
        PluginLog.Debug("Download", $"[MultiDown] Size: {FormatSize(contentLength)}");
        PluginLog.Debug("Download", $"[MultiDown] URL: {url}");

        if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(filePath) || contentLength <= 0)
        {
            PluginLog.Debug("Download", "[MultiDown] Invalid parameters. Fallback.");
            Original_c();
            return;
        }

        if (contentLength < WpfConfig.ParallelDownloadThresholdMb * 1024 * 1024)
        {
            PluginLog.Debug("Download", "[MultiDown] Small file. Fallback.");
            Original_c();
            return;
        }

        try
        {
            // 1. 初始化全局变量
            _globalDownloadedBytes = 0;
            _totalBytes = contentLength;

            // 2. 清理旧文件
            CleanupFiles(filePath);

            // 3. 检查 Range 支持
            if (!IsRangeSupported(url))
            {
                PluginLog.Debug("Download", "[MultiDown] No Range support. Fallback.");
                Original_c();
                return;
            }

            // 4. 启动独立的【渲染线程】 (每秒刷新一次进度条)
            _renderCts = new CancellationTokenSource();
            var progressObj = GetField<object>(type, realInstance, "k");
            _renderTask = Task.Run(() => RenderProgressLoop(_renderCts.Token, progressObj));

            // 5. 获取回调对象
            var onCompleteAction = GetField<Action>(type, realInstance, "o");
            var shouldUnzip = GetField<bool>(type, realInstance, "s");
            var unzipDest = GetField<string>(type, realInstance, "t");
            var unzipOverwrite = GetField<bool>(type, realInstance, "u");
            var unzipCallback = GetField<Action<object>>(type, realInstance, "l");

            PluginLog.Debug("Download", $"[MultiDown] Starting {threadCount} threads...");

            // 6. 执行下载
            if (threadCount == 1)
                DownloadChunk(url, filePath, 0, contentLength - 1, progressObj);
            else
                PerformMultiThreadDownload(url, filePath, contentLength, progressObj);


            // // 通知渲染线程结束并等待它画完最后一帧
            // _renderCts.Cancel();
            // try { _renderTask.Wait(2000); } catch { } // 最多等 2 秒
            PluginLog.Debug("Download", "[MultiDown] Download & Merge Complete.");

            // 8. 更新实例状态
            SetField(type, realInstance, "p", contentLength);
            SetField(type, realInstance, "e", 0);
            SetField(type, realInstance, "d", null);
            SetField(type, realInstance, "b", false);
            SetField(type, realInstance, "c", false);

            // 9. 触发后续
            if (shouldUnzip && !string.IsNullOrEmpty(unzipDest))
            {
                PluginLog.Debug("Download", "[MultiDown] Unzipping...");
                TryTriggerUnzip(type.Assembly, filePath, unzipDest, unzipOverwrite, unzipCallback, type, realInstance);
            }
            else if (onCompleteAction != null)
            {
                onCompleteAction.Invoke();
            }

            PluginLog.Debug("Download", "[MultiDown] All Done.");
        }
        catch (Exception ex)
        {
            if (_renderCts != null) _renderCts.Cancel();
            PluginLog.Error("Download", $"[MultiDown] FATAL ERROR: {ex.GetType().Name}");
            PluginLog.Debug("Download", $"[MultiDown] Message: {ex.Message}");
            if (ex is AggregateException agg)
                foreach (var inner in agg.InnerExceptions)
                    PluginLog.Debug("Download", $"  -> {inner.Message}");
            CleanupFiles(filePath);
            Original_c();
        }
        finally
        {
            _renderCts = null;
            _renderTask = null;
        }
    }

    // ==========================================
    // 核心功能：独立渲染线程 (每秒执行一次)
    // 同时更新：1. 启动器 UI 进度条  2. 控制台进度条
    // ==========================================
    private void RenderProgressLoop(CancellationToken token, object uiProgressObj)
    {
        long lastBytes = 0;
        var lastTicks = DateTime.UtcNow.Ticks;
        double speedMBps = 0;
        var isFinished = false; // 标记是否已完成

        while (!token.IsCancellationRequested)
        {
            Thread.Sleep(50);

            var currentBytes = _globalDownloadedBytes;
            var nowTicks = DateTime.UtcNow.Ticks;
            var elapsedSeconds = (nowTicks - lastTicks) / 10000000.0;

            if (elapsedSeconds > 0)
            {
                var deltaBytes = currentBytes - lastBytes;
                speedMBps = deltaBytes / 1024.0 / 1024.0 / elapsedSeconds;
            }

            var etaStr = "Calculating...";

            // 【关键修改 1】判断是否真正完成
            if (currentBytes >= _totalBytes && _totalBytes > 0)
            {
                etaStr = "Done";
                speedMBps = 0;
                isFinished = true;
            }
            else if (speedMBps > 0.01)
            {
                double remainingBytes = _totalBytes - currentBytes;
                if (remainingBytes < 0) remainingBytes = 0;
                var remainingSecs = remainingBytes / 1024.0 / 1024.0 / speedMBps;

                if (remainingSecs < 60)
                {
                    etaStr = $"{(int)remainingSecs}s";
                }
                else
                {
                    var ts = TimeSpan.FromSeconds(remainingSecs);
                    etaStr = ts.TotalHours >= 1 ? ts.ToString(@"hh\:mm\:ss") : ts.ToString(@"mm\:ss");
                }
            }

            // 更新 UI (如果有)
            if (uiProgressObj is IProgress<long> uiProgress)
                try
                {
                    uiProgress.Report(currentBytes);
                }
                catch
                {
                }

            // 控制台绘制
            var percent = _totalBytes > 0 ? (double)currentBytes / _totalBytes : 0;
            var barWidth = 40;
            var filled = (int)(percent * barWidth);
            if (filled > barWidth) filled = barWidth;

            var bar = new string('=', filled) + new string('-', barWidth - filled);
            var curStr = FormatSize(currentBytes);
            var totStr = FormatSize(_totalBytes);

            var output = $"\r[MultiDown] [{bar}] {curStr} / {totStr} | {speedMBps:F1} MB/s | ETA: {etaStr}   ";

            try
            {
                PluginLog.Debug("Download", output);
                Console.Out.Flush();
            }
            catch
            {
            }

            lastBytes = currentBytes;
            lastTicks = nowTicks;

            // 【关键修改 2】如果已完成，主动退出循环，不要再睡了，也不要再打印了
            if (isFinished) break;
        }

        // 循环结束后的收尾工作

        if (uiProgressObj is IProgress<long> finalProgress)
            try
            {
                finalProgress.Report(_totalBytes);
            }
            catch
            {
            }
    }

    private void PerformMultiThreadDownload(string url, string path, long total, object progress)
    {
        var tasks = new Task[threadCount];
        var chunk = total / threadCount;
        var temps = new string[threadCount];

        // 确保目录存在
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        for (var i = 0; i < threadCount; i++)
        {
            var idx = i;
            var start = i * chunk;
            var end = i == threadCount - 1 ? total - 1 : start + chunk - 1;
            var tempPath = path + ".part" + i;
            temps[i] = tempPath;
            tasks[i] = DownloadChunk(url, tempPath, start, end, progress);
            // tasks[i] = Task.Run(() => DownloadChunk(url, tempPath, start, end, progress));
        }

        Task.WaitAll(tasks);

        // 验证并合并
        PluginLog.Debug("Download", "\r[MultiDown] Merging files... ");
        using (var outFs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            foreach (var t in temps)
            {
                if (!File.Exists(t)) throw new FileNotFoundException($"Missing: {t}");
                using (var inFs = new FileStream(t, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    inFs.CopyTo(outFs);
                }

                File.Delete(t);
            }
        }

        PluginLog.Debug("Download", "Done.          "); // 多加空格清除残留字符
    }


    private async Task DownloadChunk(string url, string path, long start, long end, object progress)
    {
        var retries = 0;
        while (retries < 3)
            try
            {
                // ✅ 创建 HTTP 请求，设置 Range 头
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Range = new RangeHeaderValue(start, end);
                request.Headers.ConnectionClose = false;

                // ✅ 发送请求
                using var response = await _httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    CancellationToken.None);

                if (response.StatusCode != HttpStatusCode.OK &&
                    response.StatusCode != HttpStatusCode.PartialContent)
                    throw new HttpRequestException($"Status: {response.StatusCode}");

                // ✅ 异步流式写入文件
                using var rs = await response.Content.ReadAsStreamAsync();
                using var fs = new FileStream(
                    path,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.Read,
                    1024 * 1024, // 1MB 缓冲区
                    true); // 启用异步 IO

                var buffer = new byte[1024 * 1024];
                int read;
                while ((read = await rs.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fs.WriteAsync(buffer, 0, read);

                    // ✅ 原子累加全局进度
                    Interlocked.Add(ref _globalDownloadedBytes, read);
                }

                return;
            }
            catch (Exception ex)
            {
                retries++;
                if (retries >= 3) throw;
                await Task.Delay(2000 * retries);
            }
    }

    private void CleanupFiles(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
            for (var i = 0; i < threadCount; i++)
            {
                var p = path + ".part" + i;
                if (File.Exists(p)) File.Delete(p);
            }
        }
        catch
        {
        }
    }

    private bool IsRangeSupported(string url)
    {
        try
        {
            var req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "HEAD";
            req.Timeout = 10000;
            req.Proxy = null;
            req.UserAgent = "Mozilla/5.0";
            using (var r = (HttpWebResponse)req.GetResponse())
            {
                if (r.StatusCode == HttpStatusCode.OK)
                {
                    var h = r.Headers["Accept-Ranges"];
                    if (!string.IsNullOrEmpty(h) && h.ToLower().Contains("bytes")) return true;

                    var test = (HttpWebRequest)WebRequest.Create(url);
                    test.AddRange(0, 0);
                    test.Timeout = 5000;
                    test.Proxy = null;
                    test.UserAgent = "Mozilla/5.0";
                    using (var tr = (HttpWebResponse)test.GetResponse())
                    {
                        return tr.StatusCode == HttpStatusCode.PartialContent;
                    }
                }
            }
        }
        catch
        {
        }

        return false;
    }

    private object ResolveRealInstance(object instance)
    {
        if (instance == null) return null;
        if (instance.GetType().FullName == "WPFLauncher.Network.acd") return instance;
        if (instance is Delegate del) return del.Target;
        var f = instance.GetType().GetField("_target", BindingFlags.NonPublic | BindingFlags.Instance);
        if (f != null)
        {
            var t = f.GetValue(instance);
            return t != null ? ResolveRealInstance(t) : null;
        }

        return null;
    }

    private T GetField<T>(Type type, object obj, string name)
    {
        var f = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        return f != null ? (T)f.GetValue(obj) : default;
    }

    private void SetField(Type type, object obj, string name, object val)
    {
        var f = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        f?.SetValue(obj, val);
    }

    private void TryTriggerUnzip(Assembly asm, string zip, string dest, bool overwrite, Action<object> cb, Type type,
        object realInstance)
    {
        // int code = (int)method.Invoke(null, new object[] { zip, dest, "", overwrite, wkA, cb });

        var num4 = wa.a(zip, dest, "", overwrite, wk.a, cb);
        if (num4 != 0)
        {
            PluginLog.Error("Download", "[Unzip Error] code={0} dest={1} path={2}", num4, dest, zip);
            SetField(type, realInstance, "e", 3);
            SetField(type, realInstance, "d", null);
        }
    }

    private string FormatSize(long bytes)
    {
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        var len = bytes / 1024.0 / 1024.0;
        if (len >= 1024) return $"{len / 1024:F1} GB";
        return $"{len:F1} MB";
    }
}