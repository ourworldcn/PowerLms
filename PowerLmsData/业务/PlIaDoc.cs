using Microsoft.EntityFrameworkCore;
using OW.Data;
using PowerLms.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerLms.Data
{
    /// <summary>
    /// 空运进口单。
    /// </summary>
    [Comment("空运进口单")]
    [Index(nameof(JobId), IsUnique = false)]
    public class PlIaDoc : GuidKeyObjectBase, ICreatorInfo, IPlBusinessDoc
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public PlIaDoc()
        {

        }

        /// <summary>
        /// 所属业务Id。
        /// </summary>
        [Comment("所属业务Id")]
        public Guid? JobId { get; set; }

        /// <summary>
        /// 制单人，建立时系统默认，可以更改相当于工作号的所有者。
        /// </summary>
        [Comment("操作员，可以更改相当于工作号的所有者")]
        public Guid? CreateBy { get; set; }

        /// <summary>
        /// 制单时间,系统默认，不能更改
        /// </summary>
        [Comment("新建时间,系统默认，不能更改。")]
        public DateTime CreateDateTime { get; set; }

        /// <summary>
        /// 操作状态。0=初始化单据但尚未操作，128=最后一个状态，此状态下将业务对象状态自动切换为下一个状态。
        /// 0=初始化单据但尚未操作，,已调单=1,已申报=2,已出税=4,海关已放行=8,已入库=16,仓库已放行=128(视同已通知财务)。
        /// </summary>
        [Comment("操作状态。0=初始化单据但尚未操作，,已调单=1,已申报=2,已出税=4,海关已放行=8,已入库=16,仓库已放行=128(视同已通知财务)。")]
        public byte Status { get; set; } = 0;

        /// <summary>
        /// 仓位号。
        /// </summary>
        [Comment("仓位号。")]
        [MaxLength(64)]
        public string PositionNumber { get; set; }

        /// <summary>
        /// 货物状态字典Id。
        /// </summary>
        [Comment("货物状态字典Id。")]
        public Guid? GoodssSatusId { get; set; }

        /// <summary>
        /// 实收件数。
        /// </summary>
        [Comment("实收件数。")]
        public int ShishouCount { get; set; }

        /// <summary>
        /// 入库日期。
        /// </summary>
        [Comment("入库日期。")]
        public DateTime? RukuDateTime { get; set; }

        /// <summary>
        /// 货物类型字典id的字符串集合，逗号分隔。
        /// </summary>
        [Comment("货物类型字典id的字符串集合，逗号分隔。")]
        public string CargoTypeIdString { get; set; }

        /// <summary>
        /// 简单字典进口随机文件FollowAircraftFiles字典Id的字符串集合，逗号分隔.
        /// </summary>
        [Comment("简单字典进口随机文件FollowAircraftFiles字典Id的字符串集合，逗号分隔。")]
        public string FollowAircraftFilesIsString { get; set; }

        /// <summary>
        /// 提货地
        /// </summary>
        [Comment("提货地。")]
        public string PickUpPlace { get; set; }

        /// <summary>
        /// 提货公司
        /// </summary>
        [Comment("提货公司。")]
        public string PickUpCo { get; set; }

        /// <summary>
        /// 提货人
        /// </summary>
        [Comment("提货人。")]
        public string PickUpPerson { get; set; }

        /// <summary>
        /// 提货时间。
        /// </summary>
        [Comment("提货时间。")]
        public DateTime? PickUpDateTime { get; set; }

        /// <summary>
        /// 放行人Id。
        /// </summary>
        [Comment("放行人Id。")]
        public Guid? OperatorId { get; set; }

        /// <summary>
        /// 是否海关查验。
        /// </summary>
        [Comment("是否海关查验。")]
        public bool? IsInspection { get; set; }

        /// <summary>
        /// 是否检疫查验。
        /// </summary>
        [Comment("是否检疫查验。")]
        public bool? IsQuarantine { get; set; }

        /// <summary>
        /// 贸易方式Id。
        /// </summary>
        [Comment("贸易方式Id。")]
        public Guid? TradeModeId { get; set; }

        /// <summary>
        /// 预计消保日期。
        /// </summary>
        [Comment("预计消保日期。")]
        public DateTime? YujiXiaobaoDateTime { get; set; }

        /// <summary>
        /// 实际消保日期，null表示未消保。
        /// </summary>
        [Comment("实际消保日期，null表示未消保。")]
        public DateTime? XiaobaoDateTime { get; set; }

        /// <summary>
        /// 特别说明。
        /// </summary>
        [Comment("特别说明。")]
        public string Remark { get; set; }

    }
}
