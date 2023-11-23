/*
 * 文件放置游戏专用的一些基础类
 */
using Microsoft.Extensions.ObjectPool;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Linq;
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
        /// <param name="dic"></param>
        /// <param name="key"></param>
        /// <param name="defaultVal"></param>
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
        /// 尽可能转换为Guid类型。
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
            Trace.Assert(b);
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
            else if (obj is JsonElement jsonElement && jsonElement.TryGetDateTime(out result))
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