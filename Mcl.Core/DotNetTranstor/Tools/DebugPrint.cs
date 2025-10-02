using System;
using System.IO;

namespace MicrosoftTranslator.DotNetTranstor.Tools
{
    public class DebugPrint
    {
        public static void LogDebug(string message, ConsoleColor color = ConsoleColor.White)
        {
            try
            {
                Console.ForegroundColor = color;
                string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}";
                Console.WriteLine(logMessage);
                
                // 确保日志目录存在
                //string logDir = Path.GetDirectoryName(LogFilePath);
                // if (!Directory.Exists(logDir))
                // {
                //     Directory.CreateDirectory(logDir);
                // }
                
                // 写入日志文件
                //File.AppendAllText(LogFilePath, logMessage + Environment.NewLine);
                Console.ForegroundColor = ConsoleColor.White;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ERROR] 写入日志失败: {ex.Message}");
                Console.ForegroundColor = ConsoleColor.White;
            }
        }
        public static void LogDebug_NoColorSelect(string message)
        {
            try
            {
                string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}";
                Console.WriteLine(logMessage);
                
                // 确保日志目录存在
                //string logDir = Path.GetDirectoryName(LogFilePath);
                // if (!Directory.Exists(logDir))
                // {
                //     Directory.CreateDirectory(logDir);
                // }
                
                // 写入日志文件
                //File.AppendAllText(LogFilePath, logMessage + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ERROR] 日志输出失败: {ex.Message}");
                Console.ForegroundColor = ConsoleColor.White;
            }
        }
    }
}