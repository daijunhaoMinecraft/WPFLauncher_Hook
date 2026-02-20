using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using WPFLauncher.Network.Message; // 假设 GameDescription 在这个命名空间

namespace WPFLauncher.Manager.LanGame
{
    /// <summary>
    /// 二进制数据包反序列化器
    /// 用于将 byte[] 网络流自动映射为 C# 结构体
    /// </summary>
    public class PacketDeserializer
    {
        private readonly byte[] _buffer;      // 原始数据缓冲区
        private int _offset;                  // 当前读取指针位置
        private ushort _currentLength;        // 临时存储读取到的长度值 (用于后续复杂类型)

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="data">接收到的原始字节数组</param>
        public PacketDeserializer(byte[] data)
        {
            this._buffer = data;
            this._offset = 0;
            this._currentLength = 0;
        }

        /// <summary>
        /// 将字节数据反序列化到指定的结构体/类实例中
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        /// <param name="target">目标实例 (引用传递)</param>
        public void Deserialize<T>(ref T target)
        {
            Type type = typeof(T);
            FieldInfo[] fields = type.GetFields();

            foreach (FieldInfo field in fields)
            {
                object currentValue = field.GetValue(target);
                Type fieldType = field.FieldType;

                // 根据字段类型从缓冲区读取数据
                this.ReadValue(ref currentValue, fieldType);

                // 将读取到的值写回对象
                field.SetValue(target, currentValue);
                
                // 类型转换确保泛型一致性 (虽然反射SetValue通常不需要这一步，但原代码保留了)
                target = (T)Convert.ChangeType(target, typeof(T));
            }
        }

        /// <summary>
        /// 辅助类型转换
        /// </summary>
        private static T ConvertType<T>(object value)
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }

        /// <summary>
        /// 核心读取逻辑：根据类型从缓冲区读取数据
        /// </summary>
        private void ReadValue(ref object value, Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Object:
                    HandleObjectType(ref value, type);
                    break;

                case TypeCode.Boolean:
                    value = BitConverter.ToBoolean(_buffer, _offset);
                    _offset += 1;
                    break;

                case TypeCode.Byte:
                    value = _buffer[_offset];
                    _offset += 1;
                    break;

                case TypeCode.Int16:
                    value = BitConverter.ToInt16(_buffer, _offset);
                    _offset += 2;
                    break;

                case TypeCode.UInt16:
                    value = BitConverter.ToUInt16(_buffer, _offset);
                    _offset += 2;
                    // 特殊逻辑：将读取到的 UInt16 保存为当前长度，供下一个复杂对象使用
                    _currentLength = (ushort)value; 
                    break;

                case TypeCode.Int32:
                    value = BitConverter.ToInt32(_buffer, _offset);
                    _offset += 4;
                    break;

                case TypeCode.UInt32:
                    value = BitConverter.ToUInt32(_buffer, _offset);
                    _offset += 4;
                    break;

                case TypeCode.Int64:
                    value = BitConverter.ToInt64(_buffer, _offset);
                    _offset += 8;
                    break;

                case TypeCode.UInt64:
                    value = BitConverter.ToUInt64(_buffer, _offset);
                    _offset += 8;
                    break;

                case TypeCode.String:
                    // 字符串格式：[2字节长度][UTF8内容]
                    ushort strLen = BitConverter.ToUInt16(_buffer, _offset);
                    _offset += 2;
                    value = Encoding.UTF8.GetString(_buffer, _offset, strLen);
                    _offset += strLen;
                    break;
            }
        }

        /// <summary>
        /// 处理复杂对象类型 (JSON, 数组, 列表等)
        /// </summary>
        private void HandleObjectType(ref object value, Type type)
        {
            // 1. 处理 GameDescription 对象 (存储为 JSON 字符串)
            if (type == typeof(GameDescription))
            {
                // 使用之前读取到的 _currentLength 作为 JSON 数据的长度
                string jsonStr = Encoding.UTF8.GetString(_buffer, _offset, _currentLength);
                value = JsonConvert.DeserializeObject<GameDescription>(jsonStr);
                _offset += _currentLength;
            }
            // 2. 处理 byte[] 数组
            else if (type == typeof(byte[]))
            {
                // 使用之前读取到的 _currentLength 作为数组长度
                value = _buffer.Skip(_offset).Take(_currentLength).ToArray();
                _offset += _currentLength;
            }
            // 3. 处理 List<uint>
            else if (type == typeof(List<uint>))
            {
                // 使用之前读取到的 _currentLength 作为整个列表的字节总长度
                int remainingBytes = _currentLength;
                List<uint> list = new List<uint>();
                
                while (remainingBytes >= 4)
                {
                    uint item = BitConverter.ToUInt32(_buffer, _offset);
                    list.Add(item);
                    _offset += 4;
                    remainingBytes -= 4;
                }
                value = list;
            }
        }
    }
}