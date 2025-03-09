using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Unicode;
using System.Threading.Tasks;

namespace OW
{
    /// <summary>
    /// 随机密码生成器，默认使用不易混淆的字符。
    /// </summary>
    public class PasswordGenerator
    {
        // 定义字符集常量
        private static readonly char[] LowercaseChars = "abcdefghjkmnpqrstuvwxyz".ToCharArray(); // 无l、o
        private static readonly char[] UppercaseChars = "ABCDEFGHJKMNPQRSTUVWXYZ".ToCharArray(); // 无I、O
        private static readonly char[] NumberChars = "23456789".ToCharArray(); // 无0、1
        private static readonly char[] SpecialChars = "!@#$%^&*()_+-=[]{}|;:,./?".ToCharArray();

        private readonly char[] _defaultChars;
        private readonly char[] _charsWithSpecial;

        /// <summary>
        /// 初始化随机密码生成器，默认使用不易混淆的字母和数字。
        /// </summary>
        public PasswordGenerator()
        {
            // 默认字符集：不易混淆的大小写字母和数字
            _defaultChars = LowercaseChars
                .Concat(UppercaseChars)
                .Concat(NumberChars)
                .ToArray();

            // 带特殊字符的字符集
            _charsWithSpecial = _defaultChars
                .Concat(SpecialChars)
                .ToArray();
        }

        /// <summary>
        /// 生成指定长度的随机密码。
        /// </summary>
        /// <param name="length">密码长度</param>
        /// <param name="includeSpecialChars">是否包含特殊字符，"!@#$%^&*()_+-=[]{}|;:,./?"</param>
        /// <returns>生成的随机密码，默认情况下是大小写英文字母和数字，但无l、o、I、O、0、1以避免混淆。</returns>
        /// <exception cref="ArgumentException">密码长度必须大于0</exception>
        public string Generate(int length, bool includeSpecialChars = false)
        {
            if (length <= 0)
                throw new ArgumentOutOfRangeException(nameof(length), "密码长度必须大于0");

            // 选择字符集
            char[] chars = includeSpecialChars ? _charsWithSpecial : _defaultChars;
            int charSetLength = chars.Length;

            // 创建缓冲区
            char[] rentedArray = null;
            Span<char> password = length < 1024
                ? stackalloc char[length]
                : (rentedArray = ArrayPool<char>.Shared.Rent(length));

            try
            {
                // 使用Random.Shared生成随机字节序列
                Span<byte> randomIndices = length <= 512
                    ? stackalloc byte[length]
                    : new byte[length];

                Random.Shared.NextBytes(randomIndices);

                // 将随机字节映射到字符集索引
                for (int i = 0; i < length; i++)
                {
                    // 使用取模操作将随机字节映射到字符集范围内
                    int index = randomIndices[i] % charSetLength;
                    password[i] = chars[index];
                }

                return new string(password[..length]);
            }
            finally
            {
                if (rentedArray != null)
                    ArrayPool<char>.Shared.Return(rentedArray);
            }
        }


    }
}
