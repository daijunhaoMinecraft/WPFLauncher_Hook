using System;
using System.Runtime.InteropServices;

public class MemoryUtils
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private class MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
        public MEMORYSTATUSEX()
        {
            this.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
        }
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

    /// <summary>
    /// 获取适合游戏玩家的 Java 最大内存 (单位：MB)
    /// 策略：在保障系统不崩的前提下，尽可能分配更多内存
    /// </summary>
    public static int GetPerfectMemory()
    {
        try
        {
            MEMORYSTATUSEX memStatus = new MEMORYSTATUSEX();
            if (!GlobalMemoryStatusEx(memStatus))
            {
                return 4096; // 获取失败默认给 4GB
            }

            long totalMemoryMB = (long)(memStatus.ullTotalPhys / 1024 / 1024);
            int recommendedMemory;

            // --- 激进分配策略 ---

            // 1. 系统保留内存 (OS Reserve)
            // 玩家通常希望榨干性能，所以我们只保留最低安全值
            // 低内存电脑保留 2GB，高内存电脑保留 3GB (给 Discord/浏览器留点空间)
            int osReserve = (totalMemoryMB <= 8192) ? 2048 : 3072;

            // 2. 计算理论最大值 (总内存 - 系统保留)
            long maxAvailable = totalMemoryMB - osReserve;

            // 3. 性能阈值上限 (GC Threshold)
            // 超过 12GB 后，Java GC 停顿时间会显著增加，导致游戏卡顿
            // 除非是 500+ 模组的极端整合包，否则 12GB 是性能甜点
            int gcLimit = 12288; 

            // 4. 最低启动保障
            int minLimit = 2048;

            // 5. 最终计算
            if (maxAvailable < minLimit)
            {
                recommendedMemory = minLimit; // 内存太小，强制保底
            }
            else if (maxAvailable > gcLimit)
            {
                // 内存很大，但为了游戏流畅度，限制在 12GB 以内
                // 如果用户手动想改更大，允许他们在 UI 上手动改，但默认推荐这个
                recommendedMemory = gcLimit;
            }
            else
            {
                // 中间情况：能分多少分多少
                recommendedMemory = (int)maxAvailable;
            }

            // 6. 对齐到 1024MB (1GB) 倍数，符合玩家直觉 (4GB, 8GB, 12GB)
            recommendedMemory = (recommendedMemory / 1024) * 1024;

            return recommendedMemory;
        }
        catch
        {
            return 4096;
        }
    }
}