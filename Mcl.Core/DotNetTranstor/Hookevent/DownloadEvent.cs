// using System;
// using System.Reflection;
// using DotNetTranstor;
//
// namespace Mcl.Core.DotNetTranstor.Hookevent;
//
// // Download with Multi Thread
// public class DownloadEvent : IMethodHook
// {
//     [OriginalMethod]
//     private void DownloadFileOriginal()
//     {
//     }
//     [HookMethod("WPFLauncher.Network.acd", "c", null)]
//     private void DownloadFileHook(object instance)
//     {
//         Console.WriteLine("[Download] Download Start");
//     
//         // 1. 先打印 instance 的真实类型确认
//         Console.WriteLine($"[Download] Instance Type: {instance?.GetType().FullName}");
//     
//         // 2. 如果 instance 是委托，尝试从 _target 获取真实对象
//         object realInstance = instance;
//         var targetField = instance.GetType().GetField("_target", BindingFlags.NonPublic | BindingFlags.Instance);
//         if (targetField != null)
//         {
//             var target = targetField.GetValue(instance);
//             if (target != null && target != instance)
//             {
//                 // Console.WriteLine($"[Download] Found real target: {target.GetType().FullName}");
//                 realInstance = target;
//             }
//         }
//     
//         // 3. 用真实实例获取字段
//         Type targetType = realInstance.GetType();
//         var fField = targetType.GetField("f", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
//     
//         // 4. 如果没找到，递归检查基类
//         if (fField == null)
//         {
//             var baseType = targetType.BaseType;
//             while (baseType != null && baseType != typeof(object))
//             {
//                 fField = baseType.GetField("f", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
//                 if (fField != null) break;
//                 baseType = baseType.BaseType;
//             }
//         }
//     
//         if (fField != null)
//         {
//             string fValue = (string)fField.GetValue(realInstance);
//             Console.WriteLine($"[Download] 下载链接: {fValue}");
//         }
//         else
//         {
//             Console.WriteLine("[Download] Field 'f' not found, listing all fields:");
//             foreach (FieldInfo field in targetType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public))
//             {
//                 Console.WriteLine($"  - {field.FieldType.Name} {field.Name}");
//             }
//         }
//         DownloadFileOriginal();
//     }
// }
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using DotNetTranstor;
using DotNetTranstor.Hookevent;
using WPFLauncher.Util;
using WPFLauncher.Util.Zip;
using Application = System.Windows.Application;

namespace Mcl.Core.DotNetTranstor.Hookevent;

public class DownloadEvent : IMethodHook
{
    // 你可以随时在这里修改线程数
    private int threadCount = 4; 
    
    private long _globalDownloadedBytes = 0;
    private long _totalBytes = 0;
    
    // 用于控制渲染线程
    private CancellationTokenSource _renderCts;
    private Task _renderTask;
    
    // HttpClient
    private static readonly HttpClient _httpClient = new HttpClient()
    {
        Timeout = TimeSpan.FromSeconds(120)
    };

    [OriginalMethod]
    private void Original_c() { }

    [HookMethod("WPFLauncher.Network.acd", "c", null)]
    public void DownloadFileHook(object instance)
    {
        if (!Path_Bool.IsDownloadMultiConfig)
        {
            Original_c();
            return;
        }
        
        ServicePointManager.DefaultConnectionLimit = Path_Bool.MaxThread + 10;
        ServicePointManager.Expect100Continue = false;
        ServicePointManager.UseNagleAlgorithm = false;
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | 
                                               SecurityProtocolType.Tls13;

        threadCount = Path_Bool.MaxThread;
        if (Path_Bool.IsDebug)
        {
            Console.WriteLine("\n[MultiDown] === Hook Started ===");
        }

        object realInstance = ResolveRealInstance(instance);
        if (realInstance == null)
        {
            Console.WriteLine("[MultiDown] Error: Cannot resolve instance.");
            Original_c();
            return;
        }

        Type type = realInstance.GetType();
        string url = GetField<string>(type, realInstance, "f");
        string filePath = GetField<string>(type, realInstance, "g");
        long contentLength = GetField<long>(type, realInstance, "i");

        Console.WriteLine($"[MultiDown] Target: {Path.GetFileName(filePath)}");
        Console.WriteLine($"[MultiDown] Size: {FormatSize(contentLength)}");
        Console.WriteLine($"[MultiDown] URL: {url}");
        
        if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(filePath) || contentLength <= 0)
        {
            Console.WriteLine("[MultiDown] Invalid parameters. Fallback.");
            Original_c();
            return;
        }

        if (contentLength < 1024 * 1024)
        {
            Console.WriteLine("[MultiDown] Small file. Fallback.");
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
                Console.WriteLine("[MultiDown] No Range support. Fallback.");
                Original_c();
                return;
            }

            // 4. 启动独立的【渲染线程】 (每秒刷新一次进度条)
            _renderCts = new CancellationTokenSource();
            object progressObj = GetField<object>(type, realInstance, "k");
            _renderTask = Task.Run(() => RenderProgressLoop(_renderCts.Token, progressObj));

            // 5. 获取回调对象
            Action onCompleteAction = GetField<Action>(type, realInstance, "o");
            bool shouldUnzip = GetField<bool>(type, realInstance, "s");
            string unzipDest = GetField<string>(type, realInstance, "t");
            bool unzipOverwrite = GetField<bool>(type, realInstance, "u");
            Action<object> unzipCallback = GetField<Action<object>>(type, realInstance, "l");

            Console.WriteLine($"[MultiDown] Starting {threadCount} threads...");

            // 6. 执行下载
            if (threadCount == 1)
            {
                DownloadChunk(url, filePath, 0, contentLength - 1, progressObj);
            }
            else
            {
                PerformMultiThreadDownload(url, filePath, contentLength, progressObj);
            }

            // // 7. 等待下载完成，强制最后一次更新
            // if (progressObj is IProgress<long> p)
            // {
            //     try { p.Report(contentLength); } catch { }
            // }
            
            // // 通知渲染线程结束并等待它画完最后一帧
            // _renderCts.Cancel();
            // try { _renderTask.Wait(2000); } catch { } // 最多等 2 秒
            
            Console.WriteLine(); // 换行
            Console.WriteLine("[MultiDown] Download & Merge Complete.");

            // 8. 更新实例状态
            SetField(type, realInstance, "p", contentLength);
            SetField(type, realInstance, "e", 0);
            SetField(type, realInstance, "d", null);
            SetField(type, realInstance, "b", false);
            SetField(type, realInstance, "c", false);

            // 9. 触发后续
            if (shouldUnzip && !string.IsNullOrEmpty(unzipDest))
            {
                Console.WriteLine("[MultiDown] Unzipping...");
                TryTriggerUnzip(type.Assembly, filePath, unzipDest, unzipOverwrite, unzipCallback, type, realInstance);
            }
            else if (onCompleteAction != null)
            {
                onCompleteAction.Invoke();
            }

            Console.WriteLine("[MultiDown] All Done.");
        }
        catch (Exception ex)
        {
            if (_renderCts != null) _renderCts.Cancel();
            Console.WriteLine();
            Console.WriteLine($"[MultiDown] FATAL ERROR: {ex.GetType().Name}");
            Console.WriteLine($"[MultiDown] Message: {ex.Message}");
            if (ex is AggregateException agg)
            {
                foreach (var inner in agg.InnerExceptions)
                    Console.WriteLine($"  -> {inner.Message}");
            }
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
        long lastTicks = DateTime.UtcNow.Ticks;
        double speedMBps = 0;
        bool isFinished = false; // 标记是否已完成

        while (!token.IsCancellationRequested)
        {
            Thread.Sleep(50);

            long currentBytes = _globalDownloadedBytes;
            long nowTicks = DateTime.UtcNow.Ticks;
            double elapsedSeconds = (nowTicks - lastTicks) / 10000000.0;

            if (elapsedSeconds > 0)
            {
                long deltaBytes = currentBytes - lastBytes;
                speedMBps = (deltaBytes / 1024.0 / 1024.0) / elapsedSeconds;
            }

            string etaStr = "Calculating...";
            
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
                double remainingSecs = (remainingBytes / 1024.0 / 1024.0) / speedMBps;
                
                if (remainingSecs < 60) etaStr = $"{(int)remainingSecs}s";
                else
                {
                    var ts = TimeSpan.FromSeconds(remainingSecs);
                    etaStr = ts.TotalHours >= 1 ? ts.ToString(@"hh\:mm\:ss") : ts.ToString(@"mm\:ss");
                }
            }
            
            // 更新 UI (如果有)
            if (uiProgressObj is IProgress<long> uiProgress)
            {
                try { uiProgress.Report(currentBytes); } catch { }
            }
            
            // 控制台绘制
            double percent = _totalBytes > 0 ? (double)currentBytes / _totalBytes : 0;
            int barWidth = 40;
            int filled = (int)(percent * barWidth);
            if (filled > barWidth) filled = barWidth;
            
            string bar = new string('=', filled) + new string('-', barWidth - filled);
            string curStr = FormatSize(currentBytes);
            string totStr = FormatSize(_totalBytes);
            
            string output = $"\r[MultiDown] [{bar}] {curStr} / {totStr} | {speedMBps:F1} MB/s | ETA: {etaStr}   ";
            
            try
            {
                Console.Write(output);
                Console.Out.Flush();
            }
            catch { }

            lastBytes = currentBytes;
            lastTicks = nowTicks;

            // 【关键修改 2】如果已完成，主动退出循环，不要再睡了，也不要再打印了
            if (isFinished)
            {
                break; 
            }
        }
        
        // 循环结束后的收尾工作
        Console.WriteLine(); // 换行，确保光标移到下一行，不被后续日志覆盖
        
        if (uiProgressObj is IProgress<long> finalProgress)
        {
            try { finalProgress.Report(_totalBytes); } catch { }
        }
    }
    
    private void PerformMultiThreadDownload(string url, string path, long total, object progress)
    {
        Task[] tasks = new Task[threadCount];
        long chunk = total / threadCount;
        string[] temps = new string[threadCount];

        // 确保目录存在
        string dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        for (int i = 0; i < threadCount; i++)
        {
            int idx = i;
            long start = i * chunk;
            long end = (i == threadCount - 1) ? total - 1 : start + chunk - 1;
            string tempPath = path + ".part" + i;
            temps[i] = tempPath;
            tasks[i] = DownloadChunk(url, tempPath, start, end, progress);
            // tasks[i] = Task.Run(() => DownloadChunk(url, tempPath, start, end, progress));
        }

        Task.WaitAll(tasks);

        // 验证并合并
        Console.Write("\r[MultiDown] Merging files... ");
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
        Console.WriteLine("Done.          "); // 多加空格清除残留字符
    }

    // private void DownloadChunk(string url, string path, long start, long end, object progress)
    // {
    //     // 静默启动，不打印日志以免干扰进度条
    //     int retries = 0;
    //     while (retries < 3)
    //     {
    //         try
    //         {
    //             HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
    //             req.Method = "GET";
    //             if (start > 0 || end < long.MaxValue - 1) 
    //                 req.AddRange(start, end);
    //             
    //             req.Timeout = 120000;
    //             req.ReadWriteTimeout = 120000;
    //             req.Proxy = null;
    //             req.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/91.0";
    //             req.KeepAlive = false;
    //
    //             using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
    //             {
    //                 if (resp.StatusCode != HttpStatusCode.OK && resp.StatusCode != HttpStatusCode.PartialContent)
    //                     throw new WebException($"Status: {resp.StatusCode}");
    //
    //                 using (Stream rs = resp.GetResponseStream())
    //                 using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.SequentialScan))
    //                 {
    //                     byte[] buffer = new byte[65536]; // 64KB
    //                     int read;
    //                     while ((read = rs.Read(buffer, 0, buffer.Length)) > 0)
    //                     {
    //                         fs.Write(buffer, 0, read);
    //                         
    //                         // 原子累加全局进度
    //                         Interlocked.Add(ref _globalDownloadedBytes, (long)read);
    //
    //                         // // 汇报给游戏 UI (频率由游戏内部控制，这里直接报没事)
    //                         // if (progress is IProgress<long> p)
    //                         // {
    //                         //     try { p.Report(totalNow); } catch (Exception e) { Console.WriteLine(e); }
    //                         // }
    //                     }
    //                 }
    //             }
    //             return;
    //         }
    //         catch (Exception ex)
    //         {
    //             retries++;
    //             if (retries >= 3) throw;
    //             Thread.Sleep(2000 * retries);
    //         }
    //     }
    // }

    private async Task DownloadChunk(string url, string path, long start, long end, object progress)
    {
        int retries = 0;
        while (retries < 3)
        {
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
                    bufferSize: 1024 * 1024,  // 1MB 缓冲区
                    useAsync: true);          // 启用异步 IO

                var buffer = new byte[1024 * 1024];
                int read;
                while ((read = await rs.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fs.WriteAsync(buffer, 0, read);

                    // ✅ 原子累加全局进度
                    Interlocked.Add(ref _globalDownloadedBytes, read);

                    // // ✅ 汇报进度
                    // if (progress is IProgress<long> p)
                    // {
                    //     try { p.Report(_globalDownloadedBytes); }
                    //     catch { }
                    // }
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
    }

    private void CleanupFiles(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
            for (int i = 0; i < threadCount; i++)
            {
                string p = path + ".part" + i;
                if (File.Exists(p)) File.Delete(p);
            }
        }
        catch { }
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
                    string h = r.Headers["Accept-Ranges"];
                    if (!string.IsNullOrEmpty(h) && h.ToLower().Contains("bytes")) return true;
                    
                    var test = (HttpWebRequest)WebRequest.Create(url);
                    test.AddRange(0, 0);
                    test.Timeout = 5000;
                    test.Proxy = null;
                    test.UserAgent = "Mozilla/5.0";
                    using (var tr = (HttpWebResponse)test.GetResponse())
                        return tr.StatusCode == HttpStatusCode.PartialContent;
                }
            }
        }
        catch { }
        return false;
    }

    private object ResolveRealInstance(object instance)
    {
        if (instance == null) return null;
        if (instance.GetType().FullName == "WPFLauncher.Network.acd") return instance;
        if (instance is Delegate del) return del.Target;
        var f = instance.GetType().GetField("_target", BindingFlags.NonPublic | BindingFlags.Instance);
        if (f != null) { var t = f.GetValue(instance); return t != null ? ResolveRealInstance(t) : null; }
        return null;
    }

    private T GetField<T>(Type type, object obj, string name)
    {
        var f = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        return f != null ? (T)f.GetValue(obj) : default(T);
    }

    private void SetField(Type type, object obj, string name, object val)
    {
        var f = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        f?.SetValue(obj, val);
    }

    private void TryTriggerUnzip(Assembly asm, string zip, string dest, bool overwrite, Action<object> cb, Type type, object realInstance)
    {
        // int code = (int)method.Invoke(null, new object[] { zip, dest, "", overwrite, wkA, cb });

        int num4 = wa.a(zip, dest, "", overwrite, wk.a, cb);
        if (num4 != 0)
        {
            Console.WriteLine(string.Format("[Unzip Error] code={0} dest={1} path={2}", num4, dest, zip));
            SetField(type, realInstance, "e", 3);
            SetField(type, realInstance, "d", null);
        }
        // var wa = asm.GetType("WPFLauncher.Util.wa");
        // var wk = asm.GetType("WPFLauncher.Util.Zip.wk");
        // if (wa == null) throw new Exception("wa not found");
        // var wkA = wk != null ? Enum.Parse(wk, "a") : (object)0;
        // var method = wa.GetMethods(BindingFlags.Public | BindingFlags.Static)
        //     .FirstOrDefault(m => m.Name == "a" && m.GetParameters().Length >= 5 && (wk == null || m.GetParameters()[4].ParameterType == wk));
        // if (method == null) throw new Exception("wa.a not found");
        // int code = (int)method.Invoke(null, new object[] { zip, dest, "", overwrite, wkA, cb });
        // if (code != 0) Console.WriteLine($"[MultiDown] Unzip error: {code}");
        // else Console.WriteLine("[MultiDown] Unzip OK.");
    }

    private string FormatSize(long bytes)
    {
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        double len = bytes / 1024.0 / 1024.0;
        if (len >= 1024) return $"{len / 1024:F1} GB";
        return $"{len:F1} MB";
    }
}