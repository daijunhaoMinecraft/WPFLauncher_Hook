using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Threading;
using Mcl.Core.Dotnetdetour.Utilities.Diagnostics;

namespace Mcl.Core.Updater
{
    /// <summary>
    /// 官方补丁下载的移植版本（对应 MCLauncherUpdater 的 FileDownload/DownloadManager）。
    /// 保留原实现的关键行为：清空目标文件、16KB 分块、Accept-Ranges 断点续传重试两次、
    /// 磁盘空间检查、下载完成后 MD5 校验、失败时删除半成品。
    /// </summary>
    public static class LauncherPatchDownloader
    {
        public const int Ok = 0;
        public const int HttpError = -1;
        public const int OtherError = -2;
        public const int DownloadUndone = -3;
        public const int DiskError = -4;
        public const int FileError = -5;
        public const int Md5Error = -6;
        public const int Cancelled = -100;

        private const int ChunkSize = 16384;
        private const int MaxResumeAttempts = 2;

        public static string GetMessage(int code)
        {
            switch (code)
            {
                case Ok: return "成功";
                case HttpError: return "网络异常，请检查网络后重试";
                case OtherError: return "下载出现未知异常，请重试";
                case DownloadUndone: return "文件下载未完成，请重试";
                case DiskError: return "磁盘空间不足或文件被占用";
                case FileError: return "文件错误，目标位置不可写或被占用";
                case Md5Error: return "更新包校验失败（MD5 不匹配），请重试";
                case Cancelled: return "已取消";
                default: return "下载失败（错误码 " + code + "）";
            }
        }

        public static int Download(
            string url,
            string destination,
            long expectedSize,
            string expectedMd5,
            Action<long, long> progress,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return FileError;
            }

            try
            {
                if (File.Exists(destination))
                {
                    File.SetAttributes(destination, FileAttributes.Normal);
                    File.Delete(destination);
                }

                var directory = Path.GetDirectoryName(destination);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error("Update", ex, "准备下载目录失败: " + destination);
                return FileError;
            }

            long written = 0;
            var attempts = 0;
            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return Cancelled;
                }

                var result = DownloadOnce(url, destination, written, expectedSize, progress, cancellationToken,
                    out written, out var resumeAllowed);

                if (result == Ok)
                {
                    break;
                }

                if (result == Cancelled)
                {
                    Cleanup(destination);
                    return Cancelled;
                }

                if (resumeAllowed && attempts < MaxResumeAttempts)
                {
                    attempts++;
                    PluginLog.Warn("Update", $"断点续传第 {attempts} 次重试: {written} 字节已下载");
                    continue;
                }

                Cleanup(destination);
                return result;
            }

            if (!string.IsNullOrWhiteSpace(expectedMd5))
            {
                var actual = ComputeMd5(destination);
                if (!string.Equals(actual, expectedMd5, StringComparison.OrdinalIgnoreCase))
                {
                    PluginLog.Error("Update",
                        $"更新包 MD5 不匹配: {destination}，期望 {expectedMd5}，实际 {actual}");
                    Cleanup(destination);
                    return Md5Error;
                }
            }

            return Ok;
        }

        private static int DownloadOnce(
            string url,
            string destination,
            long written,
            long expectedSize,
            Action<long, long> progress,
            CancellationToken cancellationToken,
            out long bytesWritten,
            out bool resumeAllowed)
        {
            bytesWritten = written;
            resumeAllowed = false;
            HttpWebRequest request = null;
            try
            {
                ServicePointManager.DefaultConnectionLimit = 1024;
                request = (HttpWebRequest)WebRequest.Create(url);
                request.AllowAutoRedirect = true;
                request.Method = "GET";
                request.Timeout = 60000;
                request.ReadWriteTimeout = 60000;
                request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                request.Proxy = null;
                request.ServicePoint.Expect100Continue = false;
                request.ServicePoint.UseNagleAlgorithm = false;
                request.AllowWriteStreamBuffering = false;
                request.KeepAlive = false;
                if (bytesWritten > 0)
                {
                    request.AddRange(bytesWritten);
                }

                using (var response = request.GetResponse())
                {
                    var contentLength = response.ContentLength;
                    resumeAllowed = "bytes".Equals(response.Headers.Get("Accept-Ranges"), StringComparison.OrdinalIgnoreCase);

                    // 服务器忽略 Range 请求时会重发完整内容，此时必须从头写入。
                    if (bytesWritten > 0 && response is HttpWebResponse http && http.StatusCode == HttpStatusCode.OK)
                    {
                        bytesWritten = 0;
                    }

                    var totalSize = contentLength + bytesWritten;
                    if (expectedSize > 0 && contentLength >= 0 && totalSize != expectedSize)
                    {
                        PluginLog.Warn("Update",
                            $"更新包大小与 patchlist 不一致: {url}，服务器 {totalSize}，列表 {expectedSize}");
                    }

                    if (!HasEnoughFreeSpace(destination, Math.Max(totalSize, expectedSize)))
                    {
                        PluginLog.Error("Update", "磁盘空间不足: " + destination);
                        return DiskError;
                    }

                    using (var stream = response.GetResponseStream())
                    {
                        var buffer = new byte[ChunkSize];
                        var mode = bytesWritten == 0 ? FileMode.Create : FileMode.Append;
                        using (var fileStream = new FileStream(destination, mode, FileAccess.Write, FileShare.ReadWrite))
                        {
                            while (true)
                            {
                                if (cancellationToken.IsCancellationRequested)
                                {
                                    return Cancelled;
                                }

                                var read = stream.Read(buffer, 0, buffer.Length);
                                if (read <= 0)
                                {
                                    break;
                                }

                                fileStream.Write(buffer, 0, read);
                                bytesWritten += read;
                                progress?.Invoke(bytesWritten, totalSize);
                            }

                            fileStream.Flush(true);
                        }
                    }

                    if (contentLength >= 0 && bytesWritten < totalSize)
                    {
                        return DownloadUndone;
                    }
                }

                return Ok;
            }
            catch (WebException ex) when (ex.Status == WebExceptionStatus.Timeout && resumeAllowed)
            {
                PluginLog.Warn("Update", $"下载超时，将尝试断点续传: {url}");
                return HttpError;
            }
            catch (IOException ex) when (resumeAllowed && bytesWritten > 0)
            {
                PluginLog.Warn("Update", $"下载中断（{ex.Message}），将尝试断点续传");
                return HttpError;
            }
            catch (WebException ex)
            {
                PluginLog.Error("Update", ex, "下载更新包失败: " + url);
                return HttpError;
            }
            catch (IOException ex)
            {
                PluginLog.Error("Update", ex, "写入更新包失败: " + destination);
                return FileError;
            }
            catch (Exception ex)
            {
                PluginLog.Error("Update", ex, "下载更新包失败: " + url);
                return OtherError;
            }
            finally
            {
                request?.Abort();
            }
        }

        private static bool HasEnoughFreeSpace(string destination, long requiredBytes)
        {
            if (requiredBytes <= 0)
            {
                return true;
            }

            try
            {
                var root = Path.GetPathRoot(Path.GetFullPath(destination));
                if (string.IsNullOrEmpty(root))
                {
                    return true;
                }

                var drive = new DriveInfo(root);
                return drive.AvailableFreeSpace > requiredBytes + (5 * 1024 * 1024);
            }
            catch (Exception ex)
            {
                PluginLog.Warn("Update", "检查磁盘空间失败，继续下载: " + ex.Message);
                return true;
            }
        }

        private static string ComputeMd5(string filePath)
        {
            try
            {
                using (var stream = File.OpenRead(filePath))
                using (var md5 = MD5.Create())
                {
                    return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", string.Empty).ToLowerInvariant();
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error("Update", ex, "计算 MD5 失败: " + filePath);
                return string.Empty;
            }
        }

        private static void Cleanup(string destination)
        {
            try
            {
                if (File.Exists(destination))
                {
                    File.SetAttributes(destination, FileAttributes.Normal);
                    File.Delete(destination);
                }
            }
            catch (Exception ex)
            {
                PluginLog.Debug("Update", "清理临时文件失败: " + ex.Message);
            }
        }
    }
}
