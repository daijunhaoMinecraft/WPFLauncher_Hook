using System;

namespace Mcl.Core.Dotnetdetour.Tools;

public static class Logger
{
    public enum Level
    {
        Info,
        Success,
        Warning,
        Error
    }

    /// <summary>
    /// 输出带有时间戳和颜色的控制台日志
    /// </summary>
    /// <param name="message">日志内容</param>
    /// <param name="logLevel">日志级别，决定输出的颜色和标签</param>
    public static void Log(string message, Level logLevel = Level.Info)
    {
        // 获取当前时间戳，格式精确到秒
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        string tag = "";

        // 根据日志级别设置颜色和标签
        switch (logLevel)
        {
            case Level.Info:
                Console.ForegroundColor = ConsoleColor.Cyan;
                tag = "[INFO]";
                break;
            case Level.Success:
                Console.ForegroundColor = ConsoleColor.Green;
                tag = "[ OK ]";
                break;
            case Level.Warning:
                Console.ForegroundColor = ConsoleColor.Yellow;
                tag = "[WARN]";
                break;
            case Level.Error:
                Console.ForegroundColor = ConsoleColor.Red;
                tag = "[FAIL]";
                break;
        }

        // 输出日志行
        Console.WriteLine($"[{timestamp}] {tag} {message}");

        // 按照要求，输出完成后恢复为白色
        Console.ForegroundColor = ConsoleColor.White;
        
        // 补充说明：如果你想彻底恢复到控制台的“默认颜色”（有些终端默认是灰白色），
        // 也可以使用 Console.ResetColor(); 替代上一行代码。
    }

    // 提供几个快捷调用的封装方法，让外部调用更简洁
    public static void Info(string message) => Log(message, Level.Info);
    public static void Success(string message) => Log(message, Level.Success);
    public static void Warn(string message) => Log(message, Level.Warning);
    public static void Error(string message) => Log(message, Level.Error);
}