using System;

namespace OW
{
    /// <summary>
    /// 随机密码生成器，兼容性实现，内部使用 OwStringUtils.GeneratePassword。
    /// 建议直接使用 OwStringUtils.GeneratePassword 静态方法。
    /// </summary>
    [Obsolete("建议使用 OwStringUtils.GeneratePassword 静态方法代替此类", false)]
    public class PasswordGenerator
    {
        /// <summary>
        /// 生成指定长度的随机密码。
        /// </summary>
        /// <param name="length">密码长度</param>
        /// <param name="includeSpecialChars">是否包含特殊字符（如 !@#$%^&amp;*()_+-=[]{}|;:,./?）</param>
        /// <returns>生成的随机密码，默认情况下是大小写英文字母和数字，但无l、o、I、O、0、1以避免混淆。</returns>
        /// <exception cref="ArgumentOutOfRangeException">密码长度必须大于0</exception>
        public string Generate(int length, bool includeSpecialChars = false)
        {
            return OwStringUtils.GeneratePassword(length, includeSpecialChars);
        }
    }
}
