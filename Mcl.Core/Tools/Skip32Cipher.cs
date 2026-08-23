using System;
using System.Security.Cryptography;
using System.Text;

namespace Net.Nekocurit.Cipher
{
    public class Skip32Cipher
    {
        public const int KeySize = 10;
        
        private static readonly uint[] FTable = new uint[]
        {
            163, 215, 9, 131, 248, 72, 246, 244, 179, 33, 21, 120, 153, 177, 175, 249,
            231, 45, 77, 138, 206, 76, 202, 46, 82, 149, 217, 30, 78, 56, 68, 40,
            10, 223, 2, 160, 23, 241, 96, 104, 18, 183, 122, 195, 233, 250, 61, 83,
            150, 132, 107, 186, 242, 99, 154, 25, 124, 174, 229, 245, 247, 22, 106, 162,
            57, 182, 123, 15, 193, 147, 129, 27, 238, 180, 26, 234, 208, 145, 47, 184,
            85, 185, 218, 133, 63, 65, 191, 224, 90, 88, 128, 95, 102, 11, 216, 144,
            53, 213, 192, 167, 51, 6, 101, 105, 69, 0, 148, 86, 109, 152, 155, 118,
            151, 252, 178, 194, 176, 254, 219, 32, 225, 235, 214, 228, 221, 71, 74, 29,
            66, 237, 158, 110, 73, 60, 205, 67, 39, 210, 7, 212, 222, 199, 103, 24,
            137, 203, 48, 31, 141, 198, 143, 170, 200, 116, 220, 201, 93, 92, 49, 164,
            112, 136, 97, 44, 159, 13, 43, 135, 80, 130, 84, 100, 38, 125, 3, 64,
            52, 75, 28, 115, 209, 196, 253, 59, 204, 251, 127, 171, 230, 62, 91, 165,
            173, 4, 35, 156, 20, 81, 34, 240, 41, 121, 113, 126, 255, 140, 14, 226,
            12, 239, 188, 114, 117, 111, 55, 161, 236, 211, 142, 98, 139, 134, 16, 232,
            8, 119, 17, 190, 146, 79, 36, 197, 50, 54, 157, 207, 243, 166, 187, 172,
            94, 108, 169, 19, 87, 37, 181, 227, 189, 168, 58, 1, 5, 89, 42, 70
        };

        private readonly byte[] _key;

        public Skip32Cipher(byte[] key = null)
        {
            _key = key ?? Encoding.UTF8.GetBytes("SaintSteve");
            if (_key.Length != KeySize)
            {
                throw new ArgumentException($"Key must be {KeySize} bytes.");
            }
        }

        private uint RoundG(int k, uint w)
        {
            uint num1 = (w >> 8) & 0xFF;
            uint num2 = w & 0xFF;
            
            uint num3 = FTable[(int)(num2 ^ _key[(4 * k) % 10])] ^ num1;
            uint num4 = FTable[(int)(num3 ^ _key[(4 * k + 1) % 10])] ^ num2;
            uint num5 = FTable[(int)(num4 ^ _key[(4 * k + 2) % 10])] ^ num3;
            uint num6 = FTable[(int)(num5 ^ _key[(4 * k + 3) % 10])] ^ num4;

            return ((num5 & 0xFF) << 8) | (num6 & 0xFF);
        }

        private void Skip32(uint[] data, bool encrypt)
        {
            int step = encrypt ? 1 : -1;
            int k = encrypt ? 0 : 23;

            uint w1 = (data[0] << 8) | data[1];
            uint w2 = (data[2] << 8) | data[3];

            for (int i = 0; i < 12; i++)
            {
                w2 = w2 ^ RoundG(k, w1) ^ (uint)k;
                k += step;
                w1 = w1 ^ RoundG(k, w2) ^ (uint)k;
                k += step;
            }

            data[0] = (w2 >> 8) & 0xFF;
            data[1] = w2 & 0xFF;
            data[2] = (w1 >> 8) & 0xFF;
            data[3] = w1 & 0xFF;
        }

        public uint Encrypt(uint value)
        {
            uint[] data = new uint[]
            {
                (value >> 24) & 0xFF,
                (value >> 16) & 0xFF,
                (value >> 8) & 0xFF,
                value & 0xFF
            };
            
            Skip32(data, true);
            
            return (data[0] << 24) | (data[1] << 16) | (data[2] << 8) | data[3];
        }

        public uint Decrypt(uint value)
        {
            uint[] data = new uint[]
            {
                (value >> 24) & 0xFF,
                (value >> 16) & 0xFF,
                (value >> 8) & 0xFF,
                value & 0xFF
            };
            
            Skip32(data, false);
            
            return (data[0] << 24) | (data[1] << 16) | (data[2] << 8) | data[3];
        }

        public string GenerateRoleUuid(string name, ulong id)
        {
            byte[] hashBytes;
            using (MD5 md5 = MD5.Create())
            {
                hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(name));
            }

            uint encrypted = Encrypt((uint)id);

            hashBytes[12] = (byte)(encrypted >> 24);
            hashBytes[13] = (byte)(encrypted >> 16);
            hashBytes[14] = (byte)(encrypted >> 8);
            hashBytes[15] = (byte)encrypted;

            hashBytes[6] = (byte)((hashBytes[6] & 0x0F) | 0x40);
            hashBytes[8] = (byte)((hashBytes[8] & 0x3F) | 0x80);

            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }

        public ulong ComputeUserIdFromUuid(string uuid)
        {
            string cleanUuid = uuid.Replace("-", "");
            if (cleanUuid.Length != 32)
            {
                throw new ArgumentException("长度不符");
            }

            byte[] bytes = new byte[16];
            for (int i = 0; i < 16; i++)
            {
                bytes[i] = Convert.ToByte(cleanUuid.Substring(i * 2, 2), 16);
            }

            uint encryptedInt = (uint)bytes[12] |
                                ((uint)bytes[13] << 8) |
                                ((uint)bytes[14] << 16) |
                                ((uint)bytes[15] << 24);

            uint decrypted = Decrypt(encryptedInt);

            return decrypted; 
        }

        public string IntToHex(uint value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            string hexString = BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
            return hexString;
        }
    }
}