/*
 * 项目：PowerLms | 模块：空运进口
 * 功能：空运进口舱单实体（Ia=Import Air，Manifest - 行业标准术语）
 * 技术要点：用于海关报关的舱单数据管理
 * 作者：zc | 创建：2026-02 | 修改：2026-02-08 初始创建，2026-02-08 重命名为标准术语
 */

using Microsoft.EntityFrameworkCore;
using OW.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PowerLms.Data
{
    /// <summary>
    /// 空运进口舱单主表（Ia Manifest，Ia=Import Air）。用于海关报关的舱单数据。
    /// </summary>
    [Comment("空运进口舱单主表")]
    [Index(nameof(OrgId), nameof(MawbNo), IsUnique = false)]
    public class IaManifest : GuidKeyObjectBase
    {
        /// <summary>
        /// 所属机构Id。
        /// </summary>
        [Comment("所属机构Id")]
        public Guid? OrgId { get; set; }

        /// <summary>
        /// 主单号。11位纯数字，无横杠，海关仓单科格式。
        /// </summary>
        [Comment("主单号。11位纯数字，无横杠")]
        [MaxLength(20)]
        [Required]
        public string MawbNo { get; set; }

        /// <summary>
        /// 航班号。
        /// </summary>
        [Comment("航班号")]
        [MaxLength(10)]
        public string FlightNo { get; set; }

        /// <summary>
        /// 航班日期。
        /// </summary>
        [Comment("航班日期")]
        [Precision(3)]
        public DateTime? FlightDate { get; set; }

        /// <summary>
        /// 运输方式代码。空运默认为4。
        /// </summary>
        [Comment("运输方式代码。空运默认为4")]
        [MaxLength(2)]
        public string TypeCode { get; set; }

        /// <summary>
        /// 收货地代码。读取机构参数。
        /// </summary>
        [Comment("收货地代码")]
        [MaxLength(20)]
        public string ReceiverID { get; set; }

        /// <summary>
        /// 申报地海关。
        /// </summary>
        [Comment("申报地海关")]
        [MaxLength(20)]
        public string ExitCustomsOffice { get; set; }

        /// <summary>
        /// 到达卸货地日期。
        /// </summary>
        [Comment("到达卸货地日期")]
        [Precision(3)]
        public DateTime? ArrivalDateTime { get; set; }

        /// <summary>
        /// 运输工具名称。
        /// </summary>
        [Comment("运输工具名称")]
        [MaxLength(50)]
        public string TralTools { get; set; }

        /// <summary>
        /// 运输工具代码。
        /// </summary>
        [Comment("运输工具代码")]
        [MaxLength(20)]
        public string TralToolsCode { get; set; }

        /// <summary>
        /// 理货开始时间。
        /// </summary>
        [Comment("理货开始时间")]
        [Precision(3)]
        public DateTime? ActualDateTime { get; set; }

        /// <summary>
        /// 理货完成时间。
        /// </summary>
        [Comment("理货完成时间")]
        [Precision(3)]
        public DateTime? CompletedDateTime { get; set; }

        /// <summary>
        /// 理货管理部门代码。
        /// </summary>
        [Comment("理货管理部门代码")]
        [MaxLength(20)]
        public string ActualManagerCode { get; set; }

        /// <summary>
        /// 主单理货时间。
        /// </summary>
        [Comment("主单理货时间")]
        [Precision(3)]
        public DateTime? MtallyDate { get; set; }

        /// <summary>
        /// 备注。
        /// </summary>
        [Comment("备注")]
        [MaxLength(200)]
        public string Remark { get; set; }
    }

    /// <summary>
    /// 空运进口舱单明细表（Ia Manifest Detail）。包含主单和分单的详细信息。
    /// </summary>
    [Comment("空运进口舱单明细表")]
    [Index(nameof(ParentId), IsUnique = false)]
    [Index(nameof(MawbNo), nameof(HBLNO), IsUnique = false)]
    [Index(nameof(MawbId), IsUnique = false)]
    public class IaManifestDetail : GuidKeyObjectBase
    {
        /// <summary>
        /// 主表Id。外键关联IaManifest。可为空以保持灵活性。
        /// </summary>
        [Comment("主表Id")]
        public Guid? ParentId { get; set; }

        /// <summary>
        /// 主单号。11位纯数字，海关仓单科格式（原样记录）。
        /// </summary>
        [Comment("主单号。11位纯数字，原样记录")]
        [MaxLength(20)]
        [Required]
        public string MawbNo { get; set; }

        /// <summary>
        /// 分单号。为空表示主单行，不为空表示分单行（原样记录）。
        /// </summary>
        [Comment("分单号。为空表示主单行，原样记录")]
        [MaxLength(20)]
        public string HBLNO { get; set; }

        /// <summary>
        /// 关联的主单或分单Id。
        /// - 如果有分单号（HBLNO不为空）：指向空运出口分单Id（EaHawb.Id）
        /// - 如果无分单号（HBLNO为空）：指向空运出口主单Id（EaMawb.Id）
        /// 不建立物理外键，保持灵活性。
        /// </summary>
        [Comment("关联的主单或分单Id")]
        public Guid? MawbId { get; set; }

        /// <summary>
        /// 委托件数。
        /// </summary>
        [Comment("委托件数")]
        public int Quantity { get; set; }

        /// <summary>
        /// 重量。单位：千克，3位小数。
        /// </summary>
        [Comment("重量。单位：千克")]
        [Precision(18, 3)]
        public decimal TotalGross { get; set; }

        /// <summary>
        /// 付款方式。PP=预付，CC=到付。
        /// </summary>
        [Comment("付款方式。PP=预付，CC=到付")]
        [MaxLength(10)]
        public string PaymentCode { get; set; }

        /// <summary>
        /// 货物描述。
        /// </summary>
        [Comment("货物描述")]
        [MaxLength(200)]
        public string CargoDescription { get; set; }

        /// <summary>
        /// 发货人。
        /// </summary>
        [Comment("发货人")]
        [MaxLength(100)]
        public string Consignor { get; set; }

        /// <summary>
        /// 收货人。
        /// </summary>
        [Comment("收货人")]
        [MaxLength(100)]
        public string Consignee { get; set; }

        /// <summary>
        /// 包装方式。关联简单字典PackType的code。
        /// </summary>
        [Comment("包装方式。关联简单字典PackType的code")]
        [MaxLength(20)]
        public string PkgsType { get; set; }

        /// <summary>
        /// 体积。单位：立方米，3位小数。
        /// </summary>
        [Comment("体积。单位：立方米")]
        [Precision(18, 3)]
        public decimal MeasureMent { get; set; }

        /// <summary>
        /// 起运港。港口三字码。
        /// </summary>
        [Comment("起运港。港口三字码")]
        [MaxLength(5)]
        public string LoadingCode { get; set; }

        /// <summary>
        /// 目的港。港口三字码。
        /// </summary>
        [Comment("目的港。港口三字码")]
        [MaxLength(5)]
        public string DestinationCode { get; set; }

        /// <summary>
        /// 分单理货时间。
        /// </summary>
        [Comment("分单理货时间")]
        [Precision(3)]
        public DateTime? HtallyDate { get; set; }

        /// <summary>
        /// 分单入库时间。
        /// </summary>
        [Comment("分单入库时间")]
        [Precision(3)]
        public DateTime? HInWareDate { get; set; }

        /// <summary>
        /// 分单运抵时间。
        /// </summary>
        [Comment("分单运抵时间")]
        [Precision(3)]
        public DateTime? HarrivalDateTime { get; set; }

        /// <summary>
        /// 备注。
        /// </summary>
        [Comment("备注")]
        public string Remark { get; set; }
    }
}
