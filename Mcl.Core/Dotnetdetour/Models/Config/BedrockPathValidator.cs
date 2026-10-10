using System;
using System.IO;
using System.Linq;

namespace Mcl.Core.Dotnetdetour.Models.Config;

public static class BedrockPathValidator
{
    public const string HelpText = "请选择客户端的上级目录，目录结构应为：<选择的文件夹>\\<客户端名称文件夹>\\Minecraft.Windows.exe。不是直接选择 exe 所在文件夹。";

    public static void Validate(string directory)
    {
        try
        {
            if (Directory.Exists(directory) && Directory.EnumerateDirectories(directory)
                    .Any(child => File.Exists(Path.Combine(child, "Minecraft.Windows.exe")))) return;
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
        {
            throw new ArgumentException("无法读取基岩版目录。" + HelpText, exception);
        }

        throw new ArgumentException(HelpText);
    }
}