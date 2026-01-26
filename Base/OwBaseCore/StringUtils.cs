using OW;

namespace PowerLmsServer.Utils
{
    /// <summary>
    /// 字符串工具类，使用 OwBaseCore 的 OwStringUtils 实现。
    /// </summary>
    public static class StringUtils
    {
        /// <summary>
        /// 生成指定长度的随机密码。
        /// </summary>
        /// <param name="length">密码长度</param>
        /// <param name="includeSpecialChars">是否包含特殊字符</param>
        /// <returns>生成的随机密码</returns>
        public static string GeneratePassword(int length, bool includeSpecialChars = false)
        {
            return OwStringUtils.GeneratePassword(length, includeSpecialChars);
        }

        /// <summary>
        /// 检查字符串是否为null或空白字符串。
        /// </summary>
        /// <param name="value">要检查的字符串</param>
        /// <returns>如果字符串为null、空字符串或仅包含空白字符，则返回true；否则返回false</returns>
        public static bool IsNullOrWhiteSpace(string value)
        {
            return OwStringUtils.IsNullOrWhiteSpace(value);
        }

        /// <summary>
        /// 安全截取字符串，如果长度超出则截取指定长度。
        /// </summary>
        /// <param name="value">原字符串</param>
        /// <param name="maxLength">最大长度</param>
        /// <param name="suffix">超出长度时的后缀，默认为"..."</param>
        /// <returns>截取后的字符串</returns>
        public static string SafeSubstring(string value, int maxLength, string suffix = "...")
        {
            return OwStringUtils.SafeSubstring(value, maxLength, suffix);
        }

        /// <summary>
        /// 移除字符串中的所有空白字符。
        /// </summary>
        /// <param name="value">原字符串</param>
        /// <returns>移除空白字符后的字符串</returns>
        public static string RemoveWhiteSpaces(string value)
        {
            return OwStringUtils.RemoveWhiteSpaces(value);
        }

        /// <summary>
        /// 首字母大写。
        /// </summary>
        /// <param name="value">原字符串</param>
        /// <returns>首字母大写的字符串</returns>
        public static string Capitalize(string value)
        {
            return OwStringUtils.Capitalize(value);
        }

        /// <summary>
        /// 驼峰命名转换为下划线命名。
        /// </summary>
        /// <param name="value">驼峰命名字符串</param>
        /// <returns>下划线命名字符串</returns>
        public static string CamelToSnakeCase(string value)
        {
            return OwStringUtils.CamelToSnakeCase(value);
        }

        /// <summary>
        /// 下划线命名转换为驼峰命名。
        /// </summary>
        /// <param name="value">下划线命名字符串</param>
        /// <returns>驼峰命名字符串</returns>
        public static string SnakeToCamelCase(string value)
        {
            return OwStringUtils.SnakeToCamelCase(value);
        }

        /// <summary>
        /// 将字符串转换为指定长度的固定字符串，不足部分用指定字符填充。
        /// </summary>
        /// <param name="value">原字符串</param>
        /// <param name="totalWidth">目标长度</param>
        /// <param name="paddingChar">填充字符，默认为空格</param>
        /// <param name="leftAlign">是否左对齐，默认为true</param>
        /// <returns>填充后的字符串</returns>
        public static string PadToWidth(string value, int totalWidth, char paddingChar = ' ', bool leftAlign = true)
        {
            return OwStringUtils.PadToWidth(value, totalWidth, paddingChar, leftAlign);
        }
    }
}