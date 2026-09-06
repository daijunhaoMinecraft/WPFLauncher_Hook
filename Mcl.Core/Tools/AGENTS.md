# Tools 维护说明

## 组织

- `Cryptography`：AES、X19 帧/动态令牌/签名、XXTEA、Skip32、哈希。
- `Encoding`：字节编码与随机字符串工具。
- `Identifiers`：UID 平台标志操作。
- 公共 `Mcl.Core.Tools` 与 `Net.Nekocurit.Cipher` 命名空间保留；文件夹分组不代表已经做公共 API 迁移。
- 不依赖 WPF、宿主初始化器或 UI；工具类应可独立链接测试。

## 协议注意事项

- MD5、固定协议盐、AES 模式/填充与密钥规范化是现有协议的一部分，不能未经迁移就“升级算法”。
- AES 只在 `AesHelper` 实现；`Dotnetdetour/Utilities/Crypto/AESHelper` 是旧方法名的转发层。
- `AesCbc256Encrypt` 的旧填充不是标准 PKCS7：对齐输入不增加块，空输入产生一个零块。
- `GetCipherInstance` 保留旧密钥长度规范化边界，精确 16/24 字节的处理尤其不能擅改。
- XXTEA 使用宿主特有的 64 位变体，不是标准的 32 位实现。保留整数宽度与字节序。
- Skip32 的构造函数拥有密钥副本；调用方修改原数组不能改变已有实例。
- UUID 编码/反向读取存在历史字节序约定，不要根据直觉交换大小端。
- AES/MD5/流/变换器应确定性释放；返回的 ICryptoTransform 由调用方 Dispose。
- 随机字符串辅助方法不是密码生成器，不用于创建新的安全凭据。
- 拒绝损坏的帧、无完整块的十六进制和越界输入，避免扫描超出缓冲区。

## 测试要求

- 改算法前先保留旧实现的固定输出向量，再运行 source-linked 烟雾测试。
- 不仅验证加解密互逆：错误的两个实现也可能互逆，必须与旧协议的固定向量比较。
- 覆盖空值、空内容、Unicode、块边界、最高位 UID、错误输入与并发调用。
- 不为工具测试加载生产 Mcl.Core.dll，它有模块初始化副作用。
