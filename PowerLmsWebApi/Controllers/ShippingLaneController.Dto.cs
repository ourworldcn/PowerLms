using PowerLms.Data;
using PowerLmsWebApi.Dto;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using AutoMapper;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// JSON数值转换器。
    /// </summary>
    public class NullableDecimalConvert : JsonConverter<decimal?>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="typeToConvert"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public override decimal? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var str = reader.GetString();
            return decimal.TryParse(str, out var deci) ? deci : null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        /// <param name="options"></param>
        public override void Write(Utf8JsonWriter writer, decimal? value, JsonSerializerOptions options)
        {
            if (value is null) writer.WriteNullValue();
            else writer.WriteNumberValue(value.Value);
        }
    }
    
    /// <summary>
    /// 航线价格从Excel导入时的转换封装类。
    /// </summary>
    [AutoMap(typeof(ShippingLane), ReverseMap = true)]
    public class ShippingLaneEto : GuidKeyObjectBase
    {
        /// <summary>
        /// 起运港编码。
        /// </summary>
        [Comment("起运港编码")]
        [Column(TypeName = "varchar"), MaxLength(32), Required(AllowEmptyStrings = false)]
        [JsonPropertyName("起运港")]
        public virtual string StartCode { get; set; }

        /// <summary>
        /// 目的港编码。
        /// </summary>
        [Comment("目的港编码")]
        [Column(TypeName = "varchar"), MaxLength(32), Required(AllowEmptyStrings = false)]
        [JsonPropertyName("目的港")]
        public virtual string EndCode { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [Comment("船运公司")]
        [MaxLength(64)]
        [JsonPropertyName("船运公司")]
        public string Shipper { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [Comment("船舶信息")]
        [MaxLength(64)]
        [JsonPropertyName("船舶信息")]
        public string VesslRate { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [Comment("到达时间，单位:天。")]
        [JsonPropertyName("到达天数")]
        public decimal? ArrivalTimeInDay { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [Comment("包装规范")]
        [MaxLength(32)]
        [JsonPropertyName("包装规范")]
        public string Packing { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [Comment("KGS M"), Precision(18, 4)]
        [JsonPropertyName("KGSm")]
        public decimal? KgsM { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [Comment("KGS N"), Precision(18, 4)]
        [JsonPropertyName("KGSN")]
        public decimal? KgsN { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [Comment("KGS45"), Precision(18, 4)]
        [JsonPropertyName("KGS45")]
        public decimal? A45 { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [Comment("KGS100"), Precision(18, 4)]
        [JsonPropertyName("KGS100")]
        public decimal? A100 { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [Comment("KGS300"), Precision(18, 4)]
        [JsonPropertyName("KGS300")]
        public decimal? A300 { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [Comment("KGS500"), Precision(18, 4)]
        [JsonPropertyName("KGS500")]
        public decimal? A500 { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [Comment("KGS1000"), Precision(18, 4)]
        [JsonPropertyName("KGS1000")]
        public decimal? A1000 { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [Comment("KGS2000"), Precision(18, 4)]
        [JsonPropertyName("KGS2000")]
        public decimal? A2000 { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [Comment("有效时间")]
        [Precision(3)]
        [JsonPropertyName("有效日期")]
        public DateTime? StartDateTime { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [Comment("截止时间")]
        [Precision(3)]
        [JsonPropertyName("失效日期")]
        public DateTime? EndDateTime { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [Comment("备注")]
        [MaxLength(128)]
        [JsonPropertyName("备注")]
        public string Remark { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [Comment("联系人。")]
        [MaxLength(64)]
        [JsonPropertyName("船运联系方式")]
        public string Contact { get; set; }
    }

    #region MyRegion
    /// <summary>
    /// 
    /// </summary>
    public class ImportShippingLaneReturnDto
    {
    }
    /// <summary>
    /// 
    /// </summary>
    public class RemoveShippingLanePatamsDto : RemoveItemsParamsDtoBase
    {
    }
    /// <summary>
    /// 
    /// </summary>
    public class RemoveShippingLaneReturnDto : RemoveItemsReturnDtoBase
    {
    }
    /// <summary>
    /// 
    /// </summary>
    public class ModifyShippingLaneReturnDto : ModifyReturnDtoBase
    {
    }
    /// <summary>
    /// 
    /// </summary>
    public class ModifyShippingLaneParamsDto : ModifyParamsDtoBase<ShippingLane>
    {
    }

    /// <summary>
    /// 
    /// </summary>
    public class GetAllShippingLaneReturnDto : PagingReturnDtoBase<ShippingLane>
    {
    }

    /// <summary>
    /// 
    /// </summary>
    public class AddShippingLaneParamsDto : AddParamsDtoBase<ShippingLane>
    {
    }

    /// <summary>
    /// 
    /// </summary>
    public class AddShippingLaneReturnDto : AddReturnDtoBase
    {
    }
    #endregion
}
