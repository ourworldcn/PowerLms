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
    /// JSON��ֵת������
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
    /// ���߼۸��Excel����ʱ��ת����װ�ࡣ
    /// </summary>
    [AutoMap(typeof(ShippingLane), ReverseMap = true)]
    public class ShippingLaneEto : GuidKeyObjectBase
    {
        /// <summary>
        /// ���˸۱��롣
        /// </summary>
        [Comment("���˸۱���")]
        [Column(TypeName = "varchar"), MaxLength(32), Required(AllowEmptyStrings = false)]
        [JsonPropertyName("���˸�")]
        public virtual string StartCode { get; set; }

        /// <summary>
        /// Ŀ�ĸ۱��롣
        /// </summary>
        [Comment("Ŀ�ĸ۱���")]
        [Column(TypeName = "varchar"), MaxLength(32), Required(AllowEmptyStrings = false)]
        [JsonPropertyName("Ŀ�ĸ�")]
        public virtual string EndCode { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [Comment("���˹�˾")]
        [MaxLength(64)]
        [JsonPropertyName("���˹�˾")]
        public string Shipper { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [Comment("������Ϣ")]
        [MaxLength(64)]
        [JsonPropertyName("������Ϣ")]
        public string VesslRate { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [Comment("����ʱ�䣬��λ:�졣")]
        [JsonPropertyName("��������")]
        public decimal? ArrivalTimeInDay { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [Comment("��װ�淶")]
        [MaxLength(32)]
        [JsonPropertyName("��װ�淶")]
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
        [Comment("��Чʱ��")]
        [Precision(3)]
        [JsonPropertyName("��Ч����")]
        public DateTime? StartDateTime { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [Comment("��ֹʱ��")]
        [Precision(3)]
        [JsonPropertyName("ʧЧ����")]
        public DateTime? EndDateTime { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [Comment("��ע")]
        [MaxLength(128)]
        [JsonPropertyName("��ע")]
        public string Remark { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [Comment("��ϵ�ˡ�")]
        [MaxLength(64)]
        [JsonPropertyName("������ϵ��ʽ")]
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
