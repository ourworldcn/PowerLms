using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;

namespace OW
{
    /// <summary>
    /// 字符串工具类，提供密码生成、字符串处理等功能。
    /// </summary>
    public static class OwStringUtils
    {
        #region 密码生成功能

        // 定义字符集常量
        private static readonly char[] LowercaseChars = "abcdefghjkmnpqrstuvwxyz".ToCharArray(); // 无l、o
        private static readonly char[] UppercaseChars = "ABCDEFGHJKMNPQRSTUVWXYZ".ToCharArray(); // 无I、O
        private static readonly char[] NumberChars = "23456789".ToCharArray(); // 无0、1
        private static readonly char[] SpecialChars = "!@#$%^&*()_+-=[]{}|;:,./?".ToCharArray();

        private static readonly char[] _defaultChars;
        private static readonly char[] _charsWithSpecial;

        /// <summary>
        /// 静态构造函数，初始化字符集。
        /// </summary>
        static OwStringUtils()
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
        /// <param name="includeSpecialChars">是否包含特殊字符（如 !@#$%^&amp;*()_+-=[]{}|;:,./?）</param>
        /// <returns>生成的随机密码，默认情况下是大小写英文字母和数字，但无l、o、I、O、0、1以避免混淆。</returns>
        /// <exception cref="ArgumentOutOfRangeException">密码长度必须大于0</exception>
        public static string GeneratePassword(int length, bool includeSpecialChars = false)
        {
            if (length <= 0)
                throw new ArgumentOutOfRangeException(nameof(length), "密码长度必须大于0");

            // 选择字符集
            char[] chars = includeSpecialChars ? _charsWithSpecial : _defaultChars;
            int charSetLength = chars.Length;

            // 使用 string.Create 避免内存复制，直接在 string 的内部缓冲区中构建
            return string.Create(length, (chars, charSetLength), (span, state) =>
            {
                var (sourceChars, setLength) = state;
                for (int i = 0; i < span.Length; i++)
                {
                    int index = OwHelper.Random.Next(setLength);
                    span[i] = sourceChars[index];
                }
            });
        }

        #endregion 密码生成功能

        #region 字符串处理工具

        /// <summary>
        /// 检查字符串是否为null或空白字符串。
        /// </summary>
        /// <param name="value">要检查的字符串</param>
        /// <returns>如果字符串为null、空字符串或仅包含空白字符，则返回true；否则返回false</returns>
        public static bool IsNullOrWhiteSpace(string value)
        {
            return string.IsNullOrWhiteSpace(value);
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
            if (string.IsNullOrEmpty(value) || maxLength <= 0)
                return string.Empty;

            if (value.Length <= maxLength)
                return value;

            if (maxLength <= suffix.Length)
                return suffix[..maxLength];

            return value[..(maxLength - suffix.Length)] + suffix;
        }

        /// <summary>
        /// 移除字符串中的所有空白字符。
        /// </summary>
        /// <param name="value">原字符串</param>
        /// <returns>移除空白字符后的字符串</returns>
        public static string RemoveWhiteSpaces(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            return new string(value.Where(c => !char.IsWhiteSpace(c)).ToArray());
        }

        /// <summary>
        /// 首字母大写。
        /// </summary>
        /// <param name="value">原字符串</param>
        /// <returns>首字母大写的字符串</returns>
        public static string Capitalize(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            if (value.Length == 1)
                return value.ToUpper();

            return char.ToUpper(value[0]) + value[1..];
        }

        /// <summary>
        /// 驼峰命名转换为下划线命名。
        /// </summary>
        /// <param name="value">驼峰命名字符串</param>
        /// <returns>下划线命名字符串</returns>
        public static string CamelToSnakeCase(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            var result = new StringBuilder();
            for (int i = 0; i < value.Length; i++)
            {
                if (char.IsUpper(value[i]) && i > 0)
                    result.Append('_');
                result.Append(char.ToLower(value[i]));
            }
            return result.ToString();
        }

        /// <summary>
        /// 下划线命名转换为驼峰命名。
        /// </summary>
        /// <param name="value">下划线命名字符串</param>
        /// <returns>驼峰命名字符串</returns>
        public static string SnakeToCamelCase(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            var parts = value.Split('_', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return value;

            var result = new StringBuilder(parts[0].ToLower());
            for (int i = 1; i < parts.Length; i++)
            {
                if (parts[i].Length > 0)
                    result.Append(Capitalize(parts[i].ToLower()));
            }
            return result.ToString();
        }

        /// <summary>
        /// 字符串格式化，类似于 string.Format 但提供更好的性能。
        /// </summary>
        /// <param name="format">格式字符串</param>
        /// <param name="args">参数</param>
        /// <returns>格式化后的字符串</returns>
        public static string FastFormat(string format, params object[] args)
        {
            if (args == null || args.Length == 0)
                return format;

            return string.Format(format, args);
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
            if (string.IsNullOrEmpty(value))
                value = string.Empty;

            if (value.Length >= totalWidth)
                return value;

            int padCount = totalWidth - value.Length;
            string padding = new string(paddingChar, padCount);

            return leftAlign ? value + padding : padding + value;
        }

        #endregion 字符串处理工具
    }

    #region 辅助工具类

    /// <summary>
    /// 反射工具类，提供对象属性提取功能。
    /// PURPOSE: 为调试和序列化提供对象属性值的快速提取
    /// </summary>
    public static class ReflectionUtils
    {
        /// <summary>
        /// 提取对象的所有属性名称和值对。
        /// PURPOSE: 将对象的属性反射为键值对集合，用于日志记录、调试或数据导出
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="data">要提取属性的对象</param>
        /// <returns>属性名称和值的元组列表</returns>
        /// <exception cref="ArgumentNullException">当data参数为null时抛出</exception>
        public static List<(string Name, string Value)> ExtractProperties<T>(T data)
        {
            var result = new List<(string, string)>();
            var pis = typeof(T).GetProperties();
            string name, val;
            var type = data!.GetType();
            foreach (var pi in pis)
            {
                if (pi.GetCustomAttribute<IgnoreDataMemberAttribute>() is not null) continue;   //忽略
                name = pi.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? pi.Name; //优先JsonPropertyName
                if (pi.PropertyType == typeof(string))
                {
                    if (pi.GetValue(data) is not string tmp || tmp == null) continue;
                    val = tmp;
                }
                else if (pi.PropertyType.IsGenericType && typeof(Nullable<>) == pi.PropertyType.GetGenericTypeDefinition())    //若是可空类型
                {
                    var tmp = pi.GetValue(data);
                    if (tmp is null) continue;
                    dynamic dyn = tmp;
                    if (dyn is null) continue;
                    val = dyn.ToString();
                }
                else
                    val = pi.GetValue(data)?.ToString() ?? string.Empty;
                result.Add((name, val));
            }
            return result;
        }

    }

    #endregion 辅助工具类
}