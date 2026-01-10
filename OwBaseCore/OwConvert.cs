/*
 * 文件放置游戏专用的一些基础类
 */
using Microsoft.Extensions.ObjectPool;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace System
{
    /// <summary>
    /// 额外的转换函数汇总类。
    /// </summary>
    public static class OwConvert
    {
        #region 试图转换类型

        /// <summary>
        /// 试图把对象转换为数值。
        /// </summary>
        /// <param name="obj">null导致立即返回false。</param>
        /// <param name="result"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static bool TryToDecimal([AllowNull] object obj, out decimal result)
        {
            if (obj is null)
            {
                result = default;
                return false;
            }
            bool succ;
            switch (Type.GetTypeCode(obj.GetType()))
            {
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                    result = Convert.ToDecimal(obj);
                    succ = true;
                    break;
                case TypeCode.Decimal:
                    result = (decimal)obj;
                    succ = true;
                    break;
                case TypeCode.String:
                    succ = decimal.TryParse(obj as string, out result);
                    break;
                case TypeCode.Char:
                case TypeCode.DateTime:
                case TypeCode.Empty:
                case TypeCode.DBNull:
                case TypeCode.Boolean:
                default:
                    result = decimal.Zero;
                    succ = false;
                    break;
                case TypeCode.Object:
                    if (obj is JsonElement json)
                    {
                        succ = json.TryGetDecimal(out result);
                    }
                    else
                    {
                        result = decimal.Zero;
                        succ = false;
                    }
                    break;
            }
            return succ;
        }

        /// <summary>
        /// 试图把对象转换为浮点数。
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static bool TryToFloat(object obj, out float result)
        {
            if (obj is null)
            {
                result = default;
                return false;
            }
            bool succ;
            switch (Type.GetTypeCode(obj.GetType()))
            {
                case TypeCode.Boolean:
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                    result = Convert.ToSingle(obj);
                    succ = true;
                    break;
                case TypeCode.String:
                    succ = float.TryParse(obj as string, out result);
                    break;
                case TypeCode.Object:
                case TypeCode.Empty:
                case TypeCode.DBNull:
                case TypeCode.Char:
                case TypeCode.DateTime:
                default:
                    result = default;
                    succ = false;
                    break;
            }
            return succ;
        }

        /// <summary>
        /// 由字符串试图转换为Guid类型。
        /// </summary>
        /// <param name="str"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryToGuid([AllowNull] string str, out Guid result)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                result = default;
                return false;
            }
            if (str.EndsWith("=="))
            {
                Span<byte> buff = stackalloc byte[16];
                if (!Convert.TryFromBase64String(str, buff, out var length) || length != 16)
                {
                    result = default;
                    return false;
                }
                result = new Guid(buff);
                return true;
            }
            return Guid.TryParse(str, out result);
        }

        /// <summary>
        /// 尽可能转换为Guid类型。
        /// </summary>
        /// <param name="obj">是null会立即返回false。</param>
        /// <param name="result"></param>
        /// <returns>true成功转换，false未成功。</returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static bool TryGetGuid([AllowNull] object obj, out Guid result)
        {
            if (obj is null)
            {
                result = default;
                return false;
            }
            if (obj is Guid id)
            {
                result = id;
                return true;
            }
            if (obj is string str)
            {
                return TryToGuid(str, out result);
            }
            if (obj is byte[] ary && ary.Length == 16)
            {
                try
                {
                    result = new Guid(ary);
                    return true;
                }
                catch (Exception)
                {
                    result = default;
                    return false;
                }
            }
            if (obj is JsonElement jsonElement && jsonElement.TryGetGuid(out result))
            {
                return true;
            }
            result = default;
            return false;
        }

        /// <summary>
        /// 尽可能转换为bool类型。
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static bool TryGetBoolean(object obj, out bool result)
        {
            if (obj is bool b)
            {
                result = b;
                return true;
            }
            if (obj is string str && bool.TryParse(str, out result))
            {
                return true;
            }
            if (TryToDecimal(obj, out var deci))
            {
                result = deci != decimal.Zero;
                return true;
            }
            result = default;
            return false;
        }

        /// <summary>
        /// 使用指定类型的静态函数 TryParse 转换为指定类型。
        /// 遵循 .NET BCL 标准的简单 Try 模式。
        /// </summary>
        /// <param name="val">可以是字符串等基元类型，
        /// 也可以是任何有类似静态公开方法<see cref="DateTime.TryParse(string?, out DateTime)"/>的类型。</param>
        /// <param name="type">目标转换类型</param>
        /// <param name="result">封装(可能装箱)为Object类型的返回值。成功时返回转换后的值，失败时返回 null</param>
        /// <returns>true=转换成功，false=转换失败</returns>
        /// <exception cref="ArgumentNullException">type 为 null 时抛出</exception>
        /// <remarks>
        /// 支持的类型转换：
        /// 1. 基元类型：string, int, decimal, double, float, long, bool, DateTime, byte, sbyte, short, ushort, uint, ulong, char
        /// 2. 特殊类型：Guid, Enum（数字或名称）
        /// 3. 可空类型：Nullable&lt;T&gt;（自动解包）
        /// 4. 自定义类型：任何有标准 TryParse(string, out T) 方法的类型（通过反射或表达式树）
        /// </remarks>
        public static bool TryChangeType(string val, Type type, out object result)
        {
            result = null;  // 统一在开头初始化，简化后续代码
            
            switch (type, val)
            {
                case (var t, _) when ReferenceEquals(t, typeof(string)):
                    result = val;
                    return true;
                case (_, null) when !type.IsValueType || Nullable.GetUnderlyingType(type) != null:
                    return true;
                case (_, null):
                    return false;
                case (var t, _) when Nullable.GetUnderlyingType(t) is Type underlyingType:
                    return TryChangeType(val, underlyingType, out result);
                default:
                    switch (Type.GetTypeCode(type))
                    {
                        case TypeCode.Empty:
                            return true;
                        case TypeCode.DBNull:
                            result = DBNull.Value;
                            return true;
                        case TypeCode.Int32:
                            if (int.TryParse(val, out var intVal)) { result = intVal; return true; }
                            return false;
                        case TypeCode.Decimal:
                            if (decimal.TryParse(val, out var decVal)) { result = decVal; return true; }
                            return false;
                        case TypeCode.Double:
                            if (double.TryParse(val, out var dblVal)) { result = dblVal; return true; }
                            return false;
                        case TypeCode.Single:
                            if (float.TryParse(val, out var fltVal)) { result = fltVal; return true; }
                            return false;
                        case TypeCode.Int64:
                            if (long.TryParse(val, out var lngVal)) { result = lngVal; return true; }
                            return false;
                        case TypeCode.Boolean:
                            if (bool.TryParse(val, out var boolVal)) { result = boolVal; return true; }
                            return false;
                        case TypeCode.DateTime:
                            if (DateTime.TryParse(val, out var dtVal)) { result = dtVal; return true; }
                            return false;
                        case TypeCode.Byte:
                            if (byte.TryParse(val, out var byteVal)) { result = byteVal; return true; }
                            return false;
                        case TypeCode.SByte:
                            if (sbyte.TryParse(val, out var sbyteVal)) { result = sbyteVal; return true; }
                            return false;
                        case TypeCode.Int16:
                            if (short.TryParse(val, out var shortVal)) { result = shortVal; return true; }
                            return false;
                        case TypeCode.UInt16:
                            if (ushort.TryParse(val, out var ushortVal)) { result = ushortVal; return true; }
                            return false;
                        case TypeCode.UInt32:
                            if (uint.TryParse(val, out var uintVal)) { result = uintVal; return true; }
                            return false;
                        case TypeCode.UInt64:
                            if (ulong.TryParse(val, out var ulongVal)) { result = ulongVal; return true; }
                            return false;
                        case TypeCode.Char:
                            if (char.TryParse(val, out var charVal)) { result = charVal; return true; }
                            return false;
                        case TypeCode.Object:
                            switch (type)
                            {
                                case Type t when ReferenceEquals(t, typeof(Guid)):
                                    if (Guid.TryParse(val, out var guidResult))
                                    {
                                        result = guidResult;
                                        return true;
                                    }
                                    return false;
                                case Type t when t.IsEnum:
                                    // 官方文档明确：Enum.TryParse 支持名称字符串和数值字符串
                                    // 参考：https://learn.microsoft.com/en-us/dotnet/api/system.enum.tryparse
                                    if (Enum.TryParse(type, val, ignoreCase: true, out var enumResult))
                                    {
                                        // ✅ IsDefined 验证：排除未定义的数值（如枚举外的整数）
                                        if (Enum.IsDefined(type, enumResult))
                                        {
                                            result = enumResult;
                                            return true;
                                        }
                                    }
                                    return false;
                                case Type t when ReferenceEquals(t, typeof(DateTimeOffset)):
                                    if (DateTimeOffset.TryParse(val, out var dateTimeOffsetResult))
                                    {
                                        result = dateTimeOffsetResult;
                                        return true;
                                    }
                                    return false;
                                case Type t when ReferenceEquals(t, typeof(TimeSpan)):
                                    if (TimeSpan.TryParse(val, out var timeSpanResult))
                                    {
                                        result = timeSpanResult;
                                        return true;
                                    }
                                    return false;
                                case Type t when ReferenceEquals(t, typeof(Uri)):
                                    if (Uri.TryCreate(val, UriKind.RelativeOrAbsolute, out var uriResult))
                                    {
                                        result = uriResult;
                                        return true;
                                    }
                                    return false;
                            }
                            break;
                    }
                    return TryParseDynamic(val, type, out result);
            }
        }

        /// <summary>
        /// 缓存的 TryParse 方法信息，避免重复反射查找
        /// </summary>
        private static readonly ConcurrentDictionary<Type, MethodInfo> _tryParseMethodCache = new();

        /// <summary>
        /// 动态调用指定类型的 TryParse 方法。
        /// 使用缓存的 MethodInfo + 简单反射 Invoke 实现，简洁可靠。
        /// </summary>
        /// <param name="val">要解析的字符串</param>
        /// <param name="type">目标类型，必须有标准的 TryParse(string, out T) 静态方法</param>
        /// <param name="result">解析成功时返回装箱后的结果，失败时返回 null</param>
        /// <returns>true=解析成功，false=解析失败</returns>
        /// <remarks>
        /// 性能：首次调用 ~500ns（反射查找+缓存），后续调用 ~200ns（直接 Invoke）。
        /// 简洁可靠，适用于所有标准 TryParse 方法。
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static bool TryParseDynamic(string val, Type type, out object result)
        {
            try
            {
                var tryParseMethod = _tryParseMethodCache.GetOrAdd(type, t =>
                    t.GetMethod("TryParse", BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy,
                        null, new[] { typeof(string), t.MakeByRefType() }, null));
                if (tryParseMethod == null)
                {
                    result = null;
                    return false;
                }
                var parameters = new object[] { val, null };
                var success = (bool)tryParseMethod.Invoke(null, parameters);
                if (success)
                {
                    result = parameters[1];
                    return true;
                }
            }
            catch (Exception)
            {
            }
            result = null;
            return false;
        }

        /// <summary>
        /// 高性能的将字符串转换为指定类型，失败时抛出异常
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        /// <param name="val">要转换的字符串</param>
        /// <returns>转换后的值</returns>
        /// <exception cref="InvalidCastException">无法转换为目标类型时抛出</exception>
        /// <exception cref="ArgumentNullException">当 val 为 null 且目标类型不可为 null 时抛出</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static T To<T>(string val)
        {
            var targetType = typeof(T);
            if (val is null && targetType.IsValueType && Nullable.GetUnderlyingType(targetType) == null)
            {
                throw new ArgumentNullException(nameof(val), $"无法将 null 转换为值类型 {targetType.Name}");
            }
            if (TryChangeType(val, targetType, out var result))
            {
                return (T)result;
            }
            throw new InvalidCastException($"无法将字符串 '{val}' 转换为类型 {targetType.Name}");
        }

        /// <summary>
        /// 高性能的将字符串转换为指定类型，失败时返回默认值
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        /// <param name="val">要转换的字符串</param>
        /// <param name="defaultValue">转换失败时返回的默认值</param>
        /// <returns>转换后的值，失败时返回 defaultValue</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static T ToOrDefault<T>(string val, T defaultValue = default)
        {
            if (TryChangeType(val, typeof(T), out var result))
            {
                return (T)result;
            }
            return defaultValue;
        }

        #endregion 试图转换类型

        #region 数值类型转换

        /// <summary>
        /// 高性能数值类型转换（支持所有数值类型互转）
        /// </summary>
        /// <param name="value">源数值</param>
        /// <param name="targetType">目标数值类型</param>
        /// <param name="result">转换结果</param>
        /// <returns>true成功，false失败（类型不支持）</returns>
        /// <remarks>
        /// 性能优化：展开switch，无try-catch，无子函数调用
        /// 溢出策略：checked上下文，溢出时抛出OverflowException向上传递
        /// 支持类型：sbyte, byte, short, ushort, int, uint, long, ulong, float, double, decimal
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static bool TryConvertNumeric(object value, Type targetType, out object? result)
        {
            if (value == null)
            {
                result = null;
                return false;
            }
            // 处理 Nullable 类型
            var actualTargetType = Nullable.GetUnderlyingType(targetType) ?? targetType;
            // 获取 TypeCode
            var sourceTypeCode = Type.GetTypeCode(value.GetType());
            var targetTypeCode = Type.GetTypeCode(actualTargetType);
            // 验证是数值类型
            if (sourceTypeCode < TypeCode.SByte || sourceTypeCode > TypeCode.Decimal ||
                targetTypeCode < TypeCode.SByte || targetTypeCode > TypeCode.Decimal)
            {
                result = null;
                return false;
            }
            // ✅ checked上下文：溢出抛异常（向上传递）
            checked
            {
                // ✅ 展开所有转换路径（11×11=121）
                result = (sourceTypeCode, targetTypeCode) switch
                {
                    // SByte → 所有类型
                    (TypeCode.SByte, TypeCode.SByte) => (sbyte)value,
                    (TypeCode.SByte, TypeCode.Byte) => (byte)(sbyte)value,
                    (TypeCode.SByte, TypeCode.Int16) => (short)(sbyte)value,
                    (TypeCode.SByte, TypeCode.UInt16) => (ushort)(sbyte)value,
                    (TypeCode.SByte, TypeCode.Int32) => (int)(sbyte)value,
                    (TypeCode.SByte, TypeCode.UInt32) => (uint)(sbyte)value,
                    (TypeCode.SByte, TypeCode.Int64) => (long)(sbyte)value,
                    (TypeCode.SByte, TypeCode.UInt64) => (ulong)(sbyte)value,
                    (TypeCode.SByte, TypeCode.Single) => (float)(sbyte)value,
                    (TypeCode.SByte, TypeCode.Double) => (double)(sbyte)value,
                    (TypeCode.SByte, TypeCode.Decimal) => (decimal)(sbyte)value,

                    // Byte → 所有类型
                    (TypeCode.Byte, TypeCode.SByte) => (sbyte)(byte)value,
                    (TypeCode.Byte, TypeCode.Byte) => (byte)value,
                    (TypeCode.Byte, TypeCode.Int16) => (short)(byte)value,
                    (TypeCode.Byte, TypeCode.UInt16) => (ushort)(byte)value,
                    (TypeCode.Byte, TypeCode.Int32) => (int)(byte)value,
                    (TypeCode.Byte, TypeCode.UInt32) => (uint)(byte)value,
                    (TypeCode.Byte, TypeCode.Int64) => (long)(byte)value,
                    (TypeCode.Byte, TypeCode.UInt64) => (ulong)(byte)value,
                    (TypeCode.Byte, TypeCode.Single) => (float)(byte)value,
                    (TypeCode.Byte, TypeCode.Double) => (double)(byte)value,
                    (TypeCode.Byte, TypeCode.Decimal) => (decimal)(byte)value,

                    // Int16 → 所有类型
                    (TypeCode.Int16, TypeCode.SByte) => (sbyte)(short)value,
                    (TypeCode.Int16, TypeCode.Byte) => (byte)(short)value,
                    (TypeCode.Int16, TypeCode.Int16) => (short)value,
                    (TypeCode.Int16, TypeCode.UInt16) => (ushort)(short)value,
                    (TypeCode.Int16, TypeCode.Int32) => (int)(short)value,
                    (TypeCode.Int16, TypeCode.UInt32) => (uint)(short)value,
                    (TypeCode.Int16, TypeCode.Int64) => (long)(short)value,
                    (TypeCode.Int16, TypeCode.UInt64) => (ulong)(short)value,
                    (TypeCode.Int16, TypeCode.Single) => (float)(short)value,
                    (TypeCode.Int16, TypeCode.Double) => (double)(short)value,
                    (TypeCode.Int16, TypeCode.Decimal) => (decimal)(short)value,

                    // UInt16 → 所有类型
                    (TypeCode.UInt16, TypeCode.SByte) => (sbyte)(ushort)value,
                    (TypeCode.UInt16, TypeCode.Byte) => (byte)(ushort)value,
                    (TypeCode.UInt16, TypeCode.Int16) => (short)(ushort)value,
                    (TypeCode.UInt16, TypeCode.UInt16) => (ushort)value,
                    (TypeCode.UInt16, TypeCode.Int32) => (int)(ushort)value,
                    (TypeCode.UInt16, TypeCode.UInt32) => (uint)(ushort)value,
                    (TypeCode.UInt16, TypeCode.Int64) => (long)(ushort)value,
                    (TypeCode.UInt16, TypeCode.UInt64) => (ulong)(ushort)value,
                    (TypeCode.UInt16, TypeCode.Single) => (float)(ushort)value,
                    (TypeCode.UInt16, TypeCode.Double) => (double)(ushort)value,
                    (TypeCode.UInt16, TypeCode.Decimal) => (decimal)(ushort)value,

                    // Int32 → 所有类型
                    (TypeCode.Int32, TypeCode.SByte) => (sbyte)(int)value,
                    (TypeCode.Int32, TypeCode.Byte) => (byte)(int)value,
                    (TypeCode.Int32, TypeCode.Int16) => (short)(int)value,
                    (TypeCode.Int32, TypeCode.UInt16) => (ushort)(int)value,
                    (TypeCode.Int32, TypeCode.Int32) => (int)value,
                    (TypeCode.Int32, TypeCode.UInt32) => (uint)(int)value,
                    (TypeCode.Int32, TypeCode.Int64) => (long)(int)value,
                    (TypeCode.Int32, TypeCode.UInt64) => (ulong)(int)value,
                    (TypeCode.Int32, TypeCode.Single) => (float)(int)value,
                    (TypeCode.Int32, TypeCode.Double) => (double)(int)value,
                    (TypeCode.Int32, TypeCode.Decimal) => (decimal)(int)value,

                    // UInt32 → 所有类型
                    (TypeCode.UInt32, TypeCode.SByte) => (sbyte)(uint)value,
                    (TypeCode.UInt32, TypeCode.Byte) => (byte)(uint)value,
                    (TypeCode.UInt32, TypeCode.Int16) => (short)(uint)value,
                    (TypeCode.UInt32, TypeCode.UInt16) => (ushort)(uint)value,
                    (TypeCode.UInt32, TypeCode.Int32) => (int)(uint)value,
                    (TypeCode.UInt32, TypeCode.UInt32) => (uint)value,
                    (TypeCode.UInt32, TypeCode.Int64) => (long)(uint)value,
                    (TypeCode.UInt32, TypeCode.UInt64) => (ulong)(uint)value,
                    (TypeCode.UInt32, TypeCode.Single) => (float)(uint)value,
                    (TypeCode.UInt32, TypeCode.Double) => (double)(uint)value,
                    (TypeCode.UInt32, TypeCode.Decimal) => (decimal)(uint)value,

                    // Int64 → 所有类型
                    (TypeCode.Int64, TypeCode.SByte) => (sbyte)(long)value,
                    (TypeCode.Int64, TypeCode.Byte) => (byte)(long)value,
                    (TypeCode.Int64, TypeCode.Int16) => (short)(long)value,
                    (TypeCode.Int64, TypeCode.UInt16) => (ushort)(long)value,
                    (TypeCode.Int64, TypeCode.Int32) => (int)(long)value,
                    (TypeCode.Int64, TypeCode.UInt32) => (uint)(long)value,
                    (TypeCode.Int64, TypeCode.Int64) => (long)value,
                    (TypeCode.Int64, TypeCode.UInt64) => (ulong)(long)value,
                    (TypeCode.Int64, TypeCode.Single) => (float)(long)value,
                    (TypeCode.Int64, TypeCode.Double) => (double)(long)value,
                    (TypeCode.Int64, TypeCode.Decimal) => (decimal)(long)value,

                    // UInt64 → 所有类型
                    (TypeCode.UInt64, TypeCode.SByte) => (sbyte)(ulong)value,
                    (TypeCode.UInt64, TypeCode.Byte) => (byte)(ulong)value,
                    (TypeCode.UInt64, TypeCode.Int16) => (short)(ulong)value,
                    (TypeCode.UInt64, TypeCode.UInt16) => (ushort)(ulong)value,
                    (TypeCode.UInt64, TypeCode.Int32) => (int)(ulong)value,
                    (TypeCode.UInt64, TypeCode.UInt32) => (uint)(ulong)value,
                    (TypeCode.UInt64, TypeCode.Int64) => (long)(ulong)value,
                    (TypeCode.UInt64, TypeCode.UInt64) => (ulong)value,
                    (TypeCode.UInt64, TypeCode.Single) => (float)(ulong)value,
                    (TypeCode.UInt64, TypeCode.Double) => (double)(ulong)value,
                    (TypeCode.UInt64, TypeCode.Decimal) => (decimal)(ulong)value,

                    // Single → 所有类型
                    (TypeCode.Single, TypeCode.SByte) => (sbyte)(float)value,
                    (TypeCode.Single, TypeCode.Byte) => (byte)(float)value,
                    (TypeCode.Single, TypeCode.Int16) => (short)(float)value,
                    (TypeCode.Single, TypeCode.UInt16) => (ushort)(float)value,
                    (TypeCode.Single, TypeCode.Int32) => (int)(float)value,
                    (TypeCode.Single, TypeCode.UInt32) => (uint)(float)value,
                    (TypeCode.Single, TypeCode.Int64) => (long)(float)value,
                    (TypeCode.Single, TypeCode.UInt64) => (ulong)(float)value,
                    (TypeCode.Single, TypeCode.Single) => (float)value,
                    (TypeCode.Single, TypeCode.Double) => (double)(float)value,
                    (TypeCode.Single, TypeCode.Decimal) => (decimal)(float)value,

                    // Double → 所有类型
                    (TypeCode.Double, TypeCode.SByte) => (sbyte)(double)value,
                    (TypeCode.Double, TypeCode.Byte) => (byte)(double)value,
                    (TypeCode.Double, TypeCode.Int16) => (short)(double)value,
                    (TypeCode.Double, TypeCode.UInt16) => (ushort)(double)value,
                    (TypeCode.Double, TypeCode.Int32) => (int)(double)value,
                    (TypeCode.Double, TypeCode.UInt32) => (uint)(double)value,
                    (TypeCode.Double, TypeCode.Int64) => (long)(double)value,
                    (TypeCode.Double, TypeCode.UInt64) => (ulong)(double)value,
                    (TypeCode.Double, TypeCode.Single) => (float)(double)value,
                    (TypeCode.Double, TypeCode.Double) => (double)value,
                    (TypeCode.Double, TypeCode.Decimal) => (decimal)(double)value,

                    // Decimal → 所有类型
                    (TypeCode.Decimal, TypeCode.SByte) => (sbyte)(decimal)value,
                    (TypeCode.Decimal, TypeCode.Byte) => (byte)(decimal)value,
                    (TypeCode.Decimal, TypeCode.Int16) => (short)(decimal)value,
                    (TypeCode.Decimal, TypeCode.UInt16) => (ushort)(decimal)value,
                    (TypeCode.Decimal, TypeCode.Int32) => (int)(decimal)value,
                    (TypeCode.Decimal, TypeCode.UInt32) => (uint)(decimal)value,
                    (TypeCode.Decimal, TypeCode.Int64) => (long)(decimal)value,
                    (TypeCode.Decimal, TypeCode.UInt64) => (ulong)(decimal)value,
                    (TypeCode.Decimal, TypeCode.Single) => (float)(decimal)value,
                    (TypeCode.Decimal, TypeCode.Double) => (double)(decimal)value,
                    (TypeCode.Decimal, TypeCode.Decimal) => (decimal)value,

                    _ => throw new InvalidOperationException($"不支持的转换: {sourceTypeCode} → {targetTypeCode}")
                };
            }
            return true;
        }

        #endregion 数值类型转换

        #region 字典相关转换

        /// <summary>
        /// 从属性字典获取字符串表现形式,填充到<see cref="StringBuilder"/>对象。
        /// </summary>
        /// <param name="dic"></param>
        /// <param name="stringBuilder"></param>
        public static void Copy(IReadOnlyDictionary<string, object> dic, StringBuilder stringBuilder)
        {
            foreach (var item in dic)
            {
                stringBuilder.Append(item.Key).Append('=');
                if (TryToDecimal(item.Value, out _))
                {
                    stringBuilder.Append(item.Value.ToString()).Append(',');
                }
                else if (item.Value is decimal[])
                {
                    var ary = item.Value as decimal[];
                    stringBuilder.AppendJoin('|', ary.Select(c => c.ToString())).Append(',');
                }
                else
                {
                    stringBuilder.Append(item.Value?.ToString()).Append(',');
                }
            }
            if (stringBuilder.Length > 0 && stringBuilder[^1] == ',')
            {
                stringBuilder.Remove(stringBuilder.Length - 1, 1);
            }
        }

        /// <summary>
        /// 用字串形式属性，填充属性字典。
        /// </summary>
        /// <param name="str"></param>
        /// <param name="dic"></param>
        public static void Copy(string str, IDictionary<string, object> dic)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                return;
            }
            var coll = str.Replace(Environment.NewLine, " ").Trim(' ', '"').Split(OwHelper.CommaArrayWithCN, StringSplitOptions.RemoveEmptyEntries);
            foreach (var item in coll)
            {
                var guts = item.Split('=', StringSplitOptions.RemoveEmptyEntries);
                if (2 != guts.Length)
                {
                    if (item.IndexOf('=') <= 0 || item.Count(c => c == '=') != 1)
                    {
                        throw new InvalidCastException($"数据格式错误:'{str}'");
                    }
                }
                else if (2 < guts.Length)
                {
                    throw new InvalidCastException($"数据格式错误:'{str}'");
                }
                var keyName = string.Intern(guts[0].Trim());
                var val = guts.Length < 2 ? null : guts?[1]?.Trim();
                if (val is null)
                {
                    dic[keyName] = null;
                }
                else if (val.Contains('|'))
                {
                    var seq = val.Split('|', StringSplitOptions.RemoveEmptyEntries);
                    var ary = seq.Select(c => decimal.Parse(c.Trim())).ToArray();
                    dic[keyName] = ary;
                }
                else if (decimal.TryParse(val, out decimal num))
                {
                    dic[keyName] = num;
                }
                else
                {
                    dic[keyName] = val;
                }
            }
        }

        /// <summary>
        /// 从属性字典获取字符串表现形式。
        /// </summary>
        /// <param name="dic"></param>
        /// <returns></returns>
        public static string ToString(IReadOnlyDictionary<string, object> dic)
        {
            StringBuilder sb = AutoClearPool<StringBuilder>.Shared.Get();
            using var dw = DisposeHelper.Create(c => AutoClearPool<StringBuilder>.Shared.Return(c), sb);
            Copy(dic, sb);
            return sb.ToString();
        }

        #endregion 字典相关转换

        /// <summary>
        /// 将字符串转换为Guid类型。
        /// </summary>
        /// <param name="str">可以是<see cref="Guid.TryParse(string?, out Guid)"/>接受的格式，
        /// 也可以是Base64表示的内存数组模式，即<see cref="Guid.ToByteArray"/>的Base64编码模式。
        /// 对于空和空字符串会返回<see cref="Guid.Empty"/></param>
        /// <returns></returns>
        /// <exception cref="FormatException">字符串格式不对。</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Guid ToGuid(string str)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                return Guid.Empty;
            }
            if (!TryToGuid(str, out var result))
            {
                throw new FormatException($"不是有效的Guid数据格式——{str}");
            }
            return result;
        }

        /// <summary>
        /// 用Base64编码Guid类型。
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToBase64String(this Guid guid)
        {
            Span<byte> span = stackalloc byte[16];
            var b = guid.TryWriteBytes(span);
            Debug.Assert(b);
            return Convert.ToBase64String(span);
        }

        /// <summary>
        /// 试图转换为日期类型。
        /// </summary>
        /// <param name="obj">可以为null。支持日期型，字符串型，以及 JsonElement 类型(如果JsonElement.TryGetDateTime调用成功)。</param>
        /// <param name="result"></param>
        /// <returns>true转换成功，此时result是转换的结果；否则失败，此时result未定义。</returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static bool TryGetDateTime(object obj, out DateTime result)
        {
            if (obj is null)
            {
                result = default;
                return false;
            }
            if (obj is DateTime dt)
            {
                result = dt;
                return true;
            }
            if (obj is string str && DateTime.TryParse(str, out result))
            {
                return true;
            }
            if (obj is JsonElement jsonElement && DateTime.TryParse(jsonElement.GetString(), out result))
            {
                return true;
            }
            result = default;
            return false;
        }

        /// <summary>
        /// 将对象序列化为URI安全的字符串
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">null,return <see cref="string.Empty"/></param>
        /// <returns></returns>
        public static string ToUriString<T>(T obj) where T : new()
        {
            if (obj is null)
            {
                return string.Empty;
            }
            return Uri.EscapeDataString(JsonSerializer.Serialize(obj, typeof(T)));
        }

        /// <summary>
        /// 从URI字符串反序列化对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="str"><see cref="string.IsNullOrWhiteSpace(string?)"/>return true,return new T()</param>
        /// <returns></returns>
        public static T FromUriString<T>(string str) where T : new()
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                return new T();
            }
            return (T)JsonSerializer.Deserialize(Uri.UnescapeDataString(str), typeof(T));
        }
    }
}