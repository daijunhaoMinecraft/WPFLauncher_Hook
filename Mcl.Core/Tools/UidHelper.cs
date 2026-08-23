namespace Mcl.Core.Tools
{
    public static class UidHelper
    {
        // 标记位：第32位 (2147483648)
        private const uint MOBILE_FLAG = 1u << 31; 
        
        // 掩码位：去掉第32位的剩余部分 (2147483647)
        private const uint BASE_UID_MASK = ~MOBILE_FLAG; 

        /// <summary>
        /// 判断该 UID 是否为手机端 UID
        /// </summary>
        public static bool IsMobileUid(uint uid)
        {
            return (uid & MOBILE_FLAG) != 0;
        }

        /// <summary>
        /// 判断该 UID 是否为电脑端 UID
        /// </summary>
        public static bool IsPcUid(uint uid)
        {
            return (uid & MOBILE_FLAG) == 0;
        }

        /// <summary>
        /// 获取基础 UID（也就是强制转换为电脑端 UID格式）
        /// </summary>
        public static uint GetBaseUid(uint uid)
        {
            return uid & BASE_UID_MASK;
        }

        /// <summary>
        /// 获取对应的手机端 UID
        /// </summary>
        public static uint ToMobileUid(uint uid)
        {
            return uid | MOBILE_FLAG;
        }

        /// <summary>
        /// 翻转平台：手机端转电脑端，电脑端转手机端
        /// </summary>
        public static uint TogglePlatform(uint uid)
        {
            return uid ^ MOBILE_FLAG; 
        }
    }
}