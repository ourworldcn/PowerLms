using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using OW.Data;

namespace PowerLms.Data
{
    /// <summary>
    /// 航线运价数据类。
    /// </summary>
    public class ShippingLane : GuidKeyObjectBase, IMarkDelete, ICreatorInfo
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        [Comment("是否已标记为删除。false(默认)未标记为删除，true标记为删除。")]
        public bool IsDelete { get; set; }

        /// <summary>
        /// 创建者Id。
        /// </summary>
        [Comment("创建者的唯一标识。")]
        public Guid? CreateBy { get; set; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        [Comment("创建的时间。")]
        public DateTime CreateDateTime { get; set; }

        /// <summary>
        /// 最后更新者的唯一标识。
        /// </summary>
        [Comment("最后更新者的唯一标识。")]
        public Guid? UpdateBy { get; set; }

        /// <summary>
        /// 最后更新的时间。
        /// </summary>
        [Comment("最后更新的时间。")]
        public DateTime UpdateDateTime { get; set; }

        /// <summary>
        /// 所属机构Id。
        /// </summary>
        [Comment("所属机构Id。")]
        public Guid? OrgId { get; set; }

        /// <summary>
        /// 启运港编码。
        /// </summary>
        [Comment("启运港编码")]
        [Column(TypeName = "varchar"), MaxLength(32), Required(AllowEmptyStrings = false)]   //最多32个ASCII字符
        public virtual string StartCode { get; set; }

        /// <summary>
        /// 目的港编码。
        /// </summary>
        [Comment("目的港编码")]
        [Column(TypeName = "varchar"), MaxLength(32), Required(AllowEmptyStrings = false)]   //最多32个ASCII字符
        public virtual string EndCode { get; set; }

        /// <summary>
        /// 航空公司。
        /// </summary>
        [Comment("航空公司")]
        [MaxLength(64)]
        public string Shipper { get; set; }

        /// <summary>
        /// 航班周期。
        /// </summary>
        [Comment("航班周期")]
        [MaxLength(64)]
        public string VesslRate { get; set; }

        /// <summary>
        /// 到达时长。
        /// </summary>
        [Comment("到达时长")]
        public TimeSpan ArrivalTime { get; set; }

        /// <summary>
        /// 包装规范。
        /// </summary>
        [Comment("包装规范")]
        [MaxLength(32)]
        public string Packing { get; set; }

        /// <summary>
        /// KGS M.
        /// </summary>
        [Comment("KGS M"), Precision(18, 4)]
        public decimal? KgsM { get; set; }

        /// <summary>
        /// KGS N.
        /// </summary>
        [Comment("KGS N"), Precision(18, 4)]
        public decimal? KgsN { get; set; }

        /// <summary>
        /// KGS45.
        /// </summary>
        [Comment("KGS45"), Precision(18, 4)]
        public decimal? A45 { get; set; }

        /// <summary>
        /// KGS100.
        /// </summary>
        [Comment("KGS100"), Precision(18, 4)]
        public decimal? A100 { get; set; }

        /// <summary>
        /// KGS300.
        /// </summary>
        [Comment("KGS300"), Precision(18, 4)]
        public decimal? A300 { get; set; }

        /// <summary>
        /// KGS500.
        /// </summary>
        [Comment("KGS500"), Precision(18, 4)]
        public decimal? A500 { get; set; }

        /// <summary>
        /// KGS1000.
        /// </summary>
        [Comment("KGS1000"), Precision(18, 4)]
        public decimal? A1000 { get; set; }

        /// <summary>
        /// KGS2000.
        /// </summary>
        [Comment("KGS2000"), Precision(18, 4)]
        public decimal? A2000 { get; set; }

        /// <summary>
        /// 生效日期。
        /// </summary>
        [Comment("生效日期")]
        [Column(TypeName = "datetime2(2)")]
        public DateTime StartDateTime { get; set; }

        /// <summary>
        /// 终止日期。
        /// </summary>
        [Comment("终止日期")]
        [Column(TypeName = "datetime2(2)")]
        public DateTime EndDateTime { get; set; }

        /// <summary>
        /// 备注。
        /// </summary>
        [Comment("备注")]
        [MaxLength(128)]
        public string Remark { get; set; }

        /// <summary>
        /// 联系人。
        /// </summary>
        [Comment("联系人。")]
        [MaxLength(64)]
        public string Contact { get; set; }
    }
}
