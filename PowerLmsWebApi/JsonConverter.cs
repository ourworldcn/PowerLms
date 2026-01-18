/*
 * 项目特定的Json转换器
 */
using System.Text.Json;
namespace PowerLmsWebApi
{
    /// <summary>
    /// 海关日期时间格式(格式类似2009-06-15 13:45:30.000)。
    /// </summary>
    public class CustomsJsonConverter :System.Text.Json.Serialization.JsonConverter<DateTime>
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="typeToConvert"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var str = reader.GetString();
            return DateTime.Parse(str);
        }
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        /// <param name="options"></param>
        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString("yyyy-MM-dd HH:mm:ss.fff"));
        }
    }
}
