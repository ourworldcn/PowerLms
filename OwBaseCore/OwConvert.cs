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
        /// 
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
        /// 又字符串试图转换为Guid类型。
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
            else if (str.EndsWith("=="))
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
            else
                return Guid.TryParse(str, out result);
        }

        /// <summary>
        /// 尽可能转换为Guid类型。
        /// </summary>
        /// <param name="obj">是null会立即返回flase。</param>
        /// <param name="result"></param>
        /// <returns>true成功转换，false未成功。</returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static bool TryGetGuid([AllowNull] object obj, out Guid result)
        {
            bool succ = false;
            if (obj is null) result = default;
            else if (obj is Guid id)
            {
                result = id;
                succ = true;
            }
            else if (obj is string str) succ = TryToGuid(str, out result);
            else if (obj is byte[] ary && ary.Length == 16)
            {
                try
                {
                    result = new Guid(ary);
                    succ = true;
                }
                catch (Exception)
                {
                    result = default;
                }
            }
            else if (obj is JsonElement jsonElement && jsonElement.TryGetGuid(out result)) succ = true;
            else result = default;
            return succ;
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
            bool success = false;
            if (obj is bool b)
            {
                result = b;
                success = true;
            }
            else if (obj is string str && bool.TryParse(str, out result)) success = true;
            else if (TryToDecimal(obj, out var deci))
            {
                result = deci != decimal.Zero;
                success = true;
            }
            else result = default;
            return success;
        }

        /// <summary>
        /// 使用指定类型的静态函数 TryParse 转换为指定类型。
        /// 遵循 .NET BCL 标准的简单 Try 模式。
        /// </summary>
        /// <param name="val">可以是字符串等基元类型，
        /// 也可以是任何有类似静态公开方法<see cref="DateTime.TryParse(string?, out DateTime)"/>的类型。</param>
        /// <param name="type">目标转换类型</param>
        /// <param name="result">封装(可能装箱)为Object类型的返回值。</param>
        /// <returns>true转换成功，false转换失败</returns>
        public static bool TryChangeType(string val, Type type, out object result)
        {
            return TryChangeType(val, type, out result, out _); // 委托给详细版本，忽略错误信息
        }

        /// <summary>
        /// 使用指定类型的静态函数 TryParse 转换为指定类型。
        /// 返回详细的错误信息，供非底层调用者使用。
        /// </summary>
        /// <param name="val">可以是字符串等基元类型，
        /// 也可以是任何有类似静态公开方法<see cref="DateTime.TryParse(string?, out DateTime)"/>的类型。</param>
        /// <param name="type">目标转换类型</param>
        /// <param name="result">封装(可能装箱)为Object类型的返回值。</param>
        /// <param name="errorMessage">转换失败时的详细错误信息。成功时为null。</param>
        /// <returns>true转换成功，false转换失败</returns>
        public static bool TryChangeType(string val, Type type, out object result, out string errorMessage)
        {
            result = null;
            errorMessage = null;
            
            // 字符串类型快速路径
            if (type == typeof(string))
            {
                result = val;
                return true;
            }
            
            // null 值处理
            if (val is null || string.Equals(val, "null", StringComparison.OrdinalIgnoreCase))
            {
                if (type.IsClass || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)))
                {
                    result = null;
                    return true;
                }
                errorMessage = $"不可空类型 '{type.Name}' 不能接受 null 值";
                return false;
            }
            
            // 可空类型处理
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                var underlyingType = Nullable.GetUnderlyingType(type);
                if (TryChangeType(val, underlyingType, out var underlyingResult, out errorMessage))
                {
                    result = Activator.CreateInstance(type, underlyingResult);
                    return true;
                }
                return false; // errorMessage 已由递归调用设置
            }
            
            // 直接转换
            if (TryDirectConvert(val, type, out result, out errorMessage))
            {
                return true;
            }
            
            // 反射转换
            return TryConvertUsingReflection(val, type, out result, out errorMessage);
        }

        /// <summary>
        /// 高性能直接转换层：使用 TypeCode 和内联优化，覆盖95%+的使用场景
        /// </summary>
        /// <param name="val">要转换的字符串值</param>
        /// <param name="type">目标类型</param>
        /// <param name="result">转换结果</param>
        /// <returns>是否转换成功</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static bool TryDirectConvert(string val, Type type, out object result)
        {
            return TryDirectConvert(val, type, out result, out _); // 委托给详细版本，忽略错误信息
        }

        /// <summary>
        /// 高性能直接转换层：使用 TypeCode 和内联优化，覆盖95%+的使用场景
        /// </summary>
        /// <param name="val">要转换的字符串值</param>
        /// <param name="type">目标类型</param>
        /// <param name="result">转换结果</param>
        /// <param name="errorMessage">转换失败时的详细错误信息</param>
        /// <returns>是否转换成功</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static bool TryDirectConvert(string val, Type type, out object result, out string errorMessage)
        {
            result = null;
            errorMessage = null;
            
            try
            {
                // 使用 TypeCode 优化分支预测，直接静态调用获得最佳性能
                var typeCode = Type.GetTypeCode(type);
                var success = typeCode switch
                {
                    // 空值类型
                    TypeCode.Empty => (result = null, true).Item2,
                    TypeCode.DBNull => (result = DBNull.Value, true).Item2,
                    
                    // 常用数值类型（按使用频率排序）
                    TypeCode.Int32 => TryParseAndAssign(int.TryParse(val, out var r), r, out result),
                    TypeCode.Decimal => TryParseAndAssign(decimal.TryParse(val, out var r), r, out result),
                    TypeCode.Double => TryParseAndAssign(double.TryParse(val, out var r), r, out result),
                    TypeCode.Single => TryParseAndAssign(float.TryParse(val, out var r), r, out result),
                    TypeCode.Int64 => TryParseAndAssign(long.TryParse(val, out var r), r, out result),
                    TypeCode.Boolean => TryParseAndAssign(bool.TryParse(val, out var r), r, out result),
                    TypeCode.DateTime => TryParseAndAssign(DateTime.TryParse(val, out var r), r, out result),
                    
                    // 整数类型（完整覆盖）
                    TypeCode.Byte => TryParseAndAssign(byte.TryParse(val, out var r), r, out result),
                    TypeCode.SByte => TryParseAndAssign(sbyte.TryParse(val, out var r), r, out result),
                    TypeCode.Int16 => TryParseAndAssign(short.TryParse(val, out var r), r, out result),
                    TypeCode.UInt16 => TryParseAndAssign(ushort.TryParse(val, out var r), r, out result),
                    TypeCode.UInt32 => TryParseAndAssign(uint.TryParse(val, out var r), r, out result),
                    TypeCode.UInt64 => TryParseAndAssign(ulong.TryParse(val, out var r), r, out result),
                    
                    // 字符和字符串类型
                    TypeCode.Char => TryParseAndAssign(char.TryParse(val, out var r), r, out result),
                    TypeCode.String => (result = val, true).Item2, // 字符串直接返回
                    
                    // 复杂对象类型
                    TypeCode.Object => TryConvertObjectType(val, type, out result), // 处理 Guid 和枚举
                    
                    // 其他未知类型交给反射层处理
                    _ => false
                };

                if (!success && result == null)
                {
                    errorMessage = $"无法将字符串 '{val}' 转换为 {type.Name} 类型";
                }

                return success;
            }
            catch (Exception ex)
            {
                errorMessage = $"转换过程中发生异常：{ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// 处理 TypeCode.Object 类型的直接转换（如 Guid、枚举等）
        /// </summary>
        /// <param name="val">要转换的字符串值</param>
        /// <param name="type">目标类型</param>
        /// <param name="result">转换结果</param>
        /// <returns>是否转换成功</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryConvertObjectType(string val, Type type, out object result)
        {
            // 处理常见的 Object 类型，保持静态调用性能
            if (type == typeof(Guid))
            {
                return TryParseAndAssign(Guid.TryParse(val, out var guidResult), guidResult, out result);
            }
            
            if (type.IsEnum)
            {
                return TryParseAndAssign(Enum.TryParse(type, val, true, out var enumResult), enumResult, out result);
            }
            
            result = null;
            return false; // 不支持的 Object 类型交给反射层
        }

        /// <summary>
        /// 辅助方法：统一处理 TryParse 结果的赋值逻辑
        /// </summary>
        /// <typeparam name="T">解析结果的类型</typeparam>
        /// <param name="success">TryParse 是否成功</param>
        /// <param name="value">解析的值</param>
        /// <param name="result">输出结果</param>
        /// <returns>是否成功</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryParseAndAssign<T>(bool success, T value, out object result)
        {
            if (success)
            {
                result = value;
                return true;
            }
            result = null;
            return false;
        }

        /// <summary>
        /// 缓存的 TryParse 方法信息，避免重复反射查找
        /// </summary>
        private static readonly ConcurrentDictionary<Type, MethodInfo> _tryParseMethodCache = new();

        /// <summary>
        /// 使用缓存反射转换类型（优化版兜底方案）
        /// </summary>
        /// <param name="val">要转换的字符串值</param>
        /// <param name="type">目标类型</param>
        /// <param name="result">转换结果</param>
        /// <returns>是否转换成功</returns>
        private static bool TryConvertUsingReflection(string val, Type type, out object result)
        {
            return TryConvertUsingReflection(val, type, out result, out _); // 委托给详细版本，忽略错误信息
        }

        /// <summary>
        /// 使用缓存反射转换类型（优化版兜底方案）
        /// </summary>
        /// <param name="val">要转换的字符串值</param>
        /// <param name="type">目标类型</param>
        /// <param name="result">转换结果</param>
        /// <param name="errorMessage">转换失败时的详细错误信息</param>
        /// <returns>是否转换成功</returns>
        private static bool TryConvertUsingReflection(string val, Type type, out object result, out string errorMessage)
        {
            result = default;
            errorMessage = null;
            
            try
            {
                // 从缓存获取或创建 TryParse 方法信息
                var tryParseMethod = _tryParseMethodCache.GetOrAdd(type, t =>
                {
                    return t.GetMethod("TryParse", BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy, 
                        null, new[] { typeof(string), t.MakeByRefType() }, null);
                });

                if (tryParseMethod == null)
                {
                    errorMessage = $"类型 '{type.Name}' 不支持 TryParse 方法";
                    return false;
                }

                var parameters = new object[] { val, null }; // 准备参数数组
                var success = (bool)tryParseMethod.Invoke(null, parameters); // 调用 TryParse

                if (success)
                {
                    result = parameters[1]; // 获取解析结果
                    return true;
                }

                errorMessage = $"无法将字符串 '{val}' 转换为 {type.Name} 类型";
                return false;
            }
            catch (Exception ex)
            {
                errorMessage = $"反射转换失败：{ex.Message}";
                return false;
            }
        }
        #endregion 试图转换类型

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
                if (TryToDecimal(item.Value, out _))   //如果可以转换为数字
                {
                    stringBuilder.Append(item.Value.ToString()).Append(',');
                }
                else if (item.Value is decimal[])
                {
                    var ary = item.Value as decimal[];
                    stringBuilder.AppendJoin('|', ary.Select(c => c.ToString())).Append(',');
                }
                else //字符串
                {
                    stringBuilder.Append(item.Value?.ToString()).Append(',');
                }
            }
            if (stringBuilder.Length > 0 && stringBuilder[^1] == ',')   //若尾部是逗号
                stringBuilder.Remove(stringBuilder.Length - 1, 1);
        }

        /// <summary>
        /// 用字串形式属性，填充属性字典。
        /// </summary>
        /// <param name="str"></param>
        /// <param name="dic"></param>
        public static void Copy(string str, IDictionary<string, object> dic)
        {
            if (string.IsNullOrWhiteSpace(str))
                return;
            var coll = str.Replace(Environment.NewLine, " ").Trim(' ', '"').Split(OwHelper.CommaArrayWithCN, StringSplitOptions.RemoveEmptyEntries);
            foreach (var item in coll)
            {
                var guts = item.Split('=', StringSplitOptions.RemoveEmptyEntries);
                if (2 != guts.Length)
                {
                    if (item.IndexOf('=') <= 0 || item.Count(c => c == '=') != 1)  //若是xxx= 格式，解释为xxx=null
                        throw new InvalidCastException($"数据格式错误:'{str}'");   //TO DO
                }
                else if (2 < guts.Length)
                    throw new InvalidCastException($"数据格式错误:'{str}'");   //TO DO
                var keyName = string.Intern(guts[0].Trim());
                var val = guts.Length < 2 ? null : guts?[1]?.Trim();
                if (val is null)
                {
                    dic[keyName] = null;
                }
                else if (val.Contains('|'))  //若是序列属性
                {
                    var seq = val.Split('|', StringSplitOptions.RemoveEmptyEntries);
                    var ary = seq.Select(c => decimal.Parse(c.Trim())).ToArray();
                    dic[keyName] = ary;
                }
                else if (decimal.TryParse(val, out decimal num))   //若是数值属性
                {
                    dic[keyName] = num;
                }
                else //若是字符串属性
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
                return Guid.Empty;
            if (!TryToGuid(str, out var result))
                throw new FormatException($"不是有效的Guid数据格式——{str}");
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
            Span<byte> span = stackalloc byte[/*Marshal.SizeOf<Guid>()*/16];
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
            bool b = false;
            if (obj is null)
                result = default;
            else if (obj is DateTime dt)
            {
                result = dt;
                b = true;
            }
            else if (obj is string str && DateTime.TryParse(str, out result))
                b = true;
            else if (obj is JsonElement jsonElement && DateTime.TryParse(jsonElement.GetString(), out result))
                b = true;
            else
                result = default;
            return b;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">null,return <see cref="string.Empty"/></param>
        /// <returns></returns>
        public static string ToUriString<T>(T obj) where T : new()
        {
            if (obj is null)
                return string.Empty;
            return Uri.EscapeDataString(JsonSerializer.Serialize(obj, typeof(T)));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="str"><see cref="string.IsNullOrWhiteSpace(string?)"/>return true,return new T()</param>
        /// <returns></returns>
        public static T FromUriString<T>(string str) where T : new()
        {
            if (string.IsNullOrWhiteSpace(str))
                return new T();
            return (T)JsonSerializer.Deserialize(Uri.UnescapeDataString(str), typeof(T));
        }
    }
}