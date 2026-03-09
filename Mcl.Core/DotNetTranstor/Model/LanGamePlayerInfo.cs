using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Mcl.Core.DotNetTranstor.Model;

public class LanGamePlayerInfo
{
    public string Name { get; set; }      // 玩家名称
    public string UserID { get; set; }    // 玩家数字ID
    public string PeerId { get; set; }    // WebRTC 隧道ID
    public string VirtualIp { get; set; } // 分配的内网IP
    public string Status { get; set; }    // 状态 (在线/延迟等)
}

/// <summary>
/// 局域网游戏协议辅助工具
/// 负责玩家列表数据的二进制序列化与反序列化
/// </summary>
public static class LanGameProtocolHelper
{
    #region 协议定义

    // Magic Header: "MCLP" (Mcl Core Lan Player)
    public static readonly byte[] MagicHeader = new byte[] { 0x4D, 0x43, 0x4C, 0x50 };

    public const byte PacketType_Handshake = 0x01;
    public const byte PacketType_PlayerList = 0x02;
    public const byte PacketType_Heartbeat = 0x03;

    // 头部固定长度: Magic(4) + Type(1) + Length(2) = 7 bytes
    private const int HeaderSize = 7;

    #endregion

    /// <summary>
    /// [发送端] 构建玩家列表数据包
    /// 将玩家列表序列化为二进制数组，包含 Magic 头和长度信息
    /// </summary>
    /// <param name="players">玩家信息列表</param>
    /// <returns>完整的可发送字节数组</returns>
    public static byte[] BuildPlayerListPacket(ObservableCollection<LanGamePlayerInfo> players)
    {
        if (players == null) players = new ObservableCollection<LanGamePlayerInfo>();

        // 1. 序列化 Payload (实际数据部分)
        List<byte> payloadBuffer = new List<byte>();

        // 写入玩家数量 (1 byte, 限制 255 人，如需更多改为 2 bytes)
        if (players.Count > 255)
            throw new OverflowException("Player count exceeds 255 limit for this protocol version.");
        
        payloadBuffer.Add((byte)players.Count);

        foreach (var p in players)
        {
            // 依次写入：Name, UserID, PeerId, VirtualIp, Status
            WriteString(payloadBuffer, p.Name);
            WriteString(payloadBuffer, p.UserID);
            WriteString(payloadBuffer, p.PeerId);
            WriteString(payloadBuffer, p.VirtualIp);
            WriteString(payloadBuffer, p.Status);
        }

        byte[] payload = payloadBuffer.ToArray();
        ushort payloadLength = (ushort)payload.Length;

        // 2. 构建完整数据包
        byte[] packet = new byte[HeaderSize + payloadLength];

        // A. 复制 Magic Header
        Array.Copy(MagicHeader, 0, packet, 0, 4);

        // B. 设置包类型
        packet[4] = PacketType_PlayerList;

        // C. 设置数据长度 (小端序 Little-Endian)
        byte[] lenBytes = BitConverter.GetBytes(payloadLength);
        Array.Copy(lenBytes, 0, packet, 5, 2);

        // D. 复制 Payload
        Array.Copy(payload, 0, packet, HeaderSize, payloadLength);

        return packet;
    }

    /// <summary>
    /// [接收端] 解析收到的数据包
    /// 验证 Magic 头并提取玩家列表
    /// </summary>
    /// <param name="data">接收到的原始字节数组</param>
    /// <param name="players">解析出的玩家列表 (输出参数)</param>
    /// <returns>
    /// true: 解析成功
    /// false: 格式错误、Magic 不匹配或数据不完整
    /// </returns>
    public static bool TryParsePlayerListPacket(byte[] data, out ObservableCollection<LanGamePlayerInfo> players)
    {
        players = new ObservableCollection<LanGamePlayerInfo>();

        // 1. 基础长度检查
        if (data == null || data.Length < HeaderSize)
            return false;

        // 2. 校验 Magic Header
        for (int i = 0; i < 4; i++)
        {
            if (data[i] != MagicHeader[i])
                return false; // Magic 不匹配
        }

        // 3. 校验包类型 (可选，确保是玩家列表包)
        if (data[4] != PacketType_PlayerList)
            return false; 

        // 4. 读取数据长度
        ushort payloadLen = BitConverter.ToUInt16(data, 5);

        // 5. 校验总长度是否足够
        if (data.Length < HeaderSize + payloadLen)
            return false; // 数据截断

        // 6. 开始解析 Payload
        int offset = HeaderSize;
        int endOffset = offset + payloadLen;

        try
        {
            // 读取玩家数量
            if (offset >= endOffset) return false;
            int count = data[offset++];

            for (int i = 0; i < count; i++)
            {
                var player = new LanGamePlayerInfo();

                player.Name = ReadString(data, ref offset, endOffset);
                player.UserID = ReadString(data, ref offset, endOffset);
                player.PeerId = ReadString(data, ref offset, endOffset);
                player.VirtualIp = ReadString(data, ref offset, endOffset);
                player.Status = ReadString(data, ref offset, endOffset);

                players.Add(player);
            }

            return true;
        }
        catch (Exception)
        {
            // 解析过程中出现越界或格式错误
            players.Clear();
            return false;
        }
    }

    #region 内部辅助方法

    /// <summary>
    /// 将字符串写入缓冲区：[1字节长度][UTF8字节...]
    /// </summary>
    private static void WriteString(List<byte> buffer, string str)
    {
        if (string.IsNullOrEmpty(str)) str = string.Empty;

        byte[] bytes = Encoding.UTF8.GetBytes(str);

        // 简单协议：限制单字段长度 255 字节
        if (bytes.Length > 255)
        {
            // 策略：截断 (或者抛出异常，视需求而定)
            bytes = bytes.Take(255).ToArray();
        }

        buffer.Add((byte)bytes.Length);
        buffer.AddRange(bytes);
    }

    /// <summary>
    /// 从缓冲区读取字符串
    /// </summary>
    private static string ReadString(byte[] data, ref int offset, int limit)
    {
        if (offset >= limit) return string.Empty;

        int len = data[offset++];
        if (len == 0) return string.Empty;

        if (offset + len > limit)
            throw new FormatException("String length exceeds buffer boundary.");

        string result = Encoding.UTF8.GetString(data, offset, len);
        offset += len;
        return result;
    }

    public static bool IsGamePlayerInfoMagicHeader(byte[] data)
    {
        for (int i = 0; i < MagicHeader.Length; i++)
        {
            if (MagicHeader[i] != data[i])
            {
                return false;
            }
        }

        return true;
    }

    #endregion
}