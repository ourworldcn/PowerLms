/*
 * Json相关的成员
 */

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace System.Text.Json.Serialization
{

    /// <summary>
    /// 用于将Guid类型Base64编码的Json转换器。读取时可以识别Base64编码 ，也可以识别默认格式。
    /// </summary>
    public class OwGuidJsonConverter : JsonConverter<Guid>
    {
        public OwGuidJsonConverter()
        {
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="reader"><inheritdoc/></param>
        /// <param name="typeToConvert"><inheritdoc/></param>
        /// <param name="options"><inheritdoc/></param>
        /// <returns><inheritdoc/></returns>
        public override Guid Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Trace.Assert(reader.TokenType == JsonTokenType.String);
            if (reader.TryGetBytesFromBase64(out var bin)) return new Guid(bin);
            if (reader.TryGetGuid(out var id)) return id;
            throw new InvalidCastException($"字符串 {reader.GetString()} 无法转换为Guid类型。");
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="writer"><inheritdoc/></param>
        /// <param name="value"><inheritdoc/></param>
        /// <param name="options"><inheritdoc/></param>
        public override void Write(Utf8JsonWriter writer, Guid value, JsonSerializerOptions options)
        {
            const int len = 16;
            Span<byte> span = stackalloc byte[len];
            if (!value.TryWriteBytes(span)) throw new InvalidOperationException($"检测到Guid类型无法写入{len}字节byte数组！");
            writer.WriteBase64StringValue(span);
        }
    }

    /// <summary>
    /// 读取时可以识别任意有效日期模式，写入则使用标准s写入(格式类似2009-06-15T13:45:30，精确到秒)。
    /// </summary>
    public class OwDateTime_sJsonConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var str = reader.GetString();
            return DateTime.Parse(str);
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString("s"));
        }
    }

    public class TimeSpanJsonConverter : JsonConverter<TimeSpan>
    {
        public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return TimeSpan.Parse(reader.GetString());
        }

        public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString("G"));
        }
    }
}
