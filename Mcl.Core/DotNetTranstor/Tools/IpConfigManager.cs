using System;
using System.IO;

namespace Mcl.Core.DotNetTranstor.Tools
{
    /// <summary>
    /// IP 配置文件管理器
    /// 负责读写当前目录下的 LanGameIP.txt
    /// </summary>
    public static class IpConfigManager
    {
        private const string ConfigFileName = "LanGameIP.txt";

        /// <summary>
        /// 获取配置文件完整路径 (当前执行目录)
        /// </summary>
        private static string ConfigPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName);

        /// <summary>
        /// 读取保存的 IP 最后一位 (主机位)
        /// </summary>
        /// <returns>如果文件存在且有效，返回整数；否则返回 null</returns>
        public static int? LoadLastOctet()
        {
            if (!File.Exists(ConfigPath))
                return null;

            try
            {
                string content = File.ReadAllText(ConfigPath).Trim();
                if (int.TryParse(content, out int lastOctet))
                {
                    // 再次校验范围，防止配置文件被手动篡改
                    if (lastOctet >= 0 && lastOctet <= 255)
                        return lastOctet;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"读取 IP 配置失败: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// 保存 IP 最后一位到文件
        /// </summary>
        /// <param name="lastOctet">0-255 的整数</param>
        /// <returns>是否保存成功</returns>
        public static bool SaveLastOctet(int lastOctet)
        {
            if (lastOctet < 0 || lastOctet > 255)
                return false;

            try
            {
                // 直接覆盖写入，只保存数字字符串
                File.WriteAllText(ConfigPath, lastOctet.ToString());
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存 IP 配置失败: {ex.Message}");
                return false;
            }
        }
    }
}