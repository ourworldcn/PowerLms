using Microsoft.EntityFrameworkCore;
using OW.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerLms.Data
{
    /// <summary>
    /// 业务费用收付款申请单。
    /// </summary>
    public class DocFeeRequisition : GuidKeyObjectBase
    {
        public Guid? OrgId { get; set; }

        /// <summary>
        /// 申请单号,其他编码规则中【申请单号】自动生成
        /// </summary>
        [Comment("申请单号,其他编码规则中【申请单号】自动生成")]
        [MaxLength(64)]
        public string FrNo { get; set; }

        /// <summary>
        /// 收付，false支出，true收入。
        /// </summary>
        [Comment("收付，false支出，true收入。")]
        public bool IO { get; set; }

        /// <summary>
        /// 制单人Id。员工Id。
        /// </summary>
        [Comment("制单人Id。员工Id。")]
        public Guid? MakerId { get; set; }

        /// <summary>
        /// 制单时间。
        /// </summary>
        [Column(TypeName = "datetime2(2)")]
        [Comment("制单时间")]
        public DateTime? MakeDateTime { get; set; }

        /// <summary>
        /// 结算类型。简单字典ApplyType
        /// </summary>
        [Comment("结算类型。简单字典ApplyType")]
        public Guid? ApplyTypeId { get; set; }

        /// <summary>
        /// 结算单位Id。客户资料中选择
        /// </summary>
        [Comment("结算单位Id。客户资料中选择")]
        public Guid? BalanceId { get; set; }

        /// <summary>
        /// 结算单位账号,选择后可修改.
        /// </summary>
        [MaxLength(64)]
        [Comment("结算单位账号,选择后可修改")]
        public string BlanceAccountNo { get; set; }

        /// <summary>
        /// 结算单位开户行，选择后可修改
        /// </summary>
        [MaxLength(64)]
        [Comment("结算单位开户行，选择后可修改")]
        public string Bank { get; set; }

        /// <summary>
        /// 结算单位联系人，选择后可修改
        /// </summary>
        [MaxLength(64)]
        [Comment("结算单位联系人，选择后可修改")]
        public string Contact { get; set; }

        /// <summary>
        /// 结算单位联系人电话
        /// </summary>
        [MaxLength(32), Phone]
        [Comment("结算单位联系人电话")]
        public string Tel { get; set; }

        /// <summary>
        /// 要求开发票,true=要求，false=未要求。
        /// </summary>
        [Comment("要求开发票,true=要求，false=未要求。")]
        public bool IsNeedInvoice { get; set; }

        /// <summary>
        /// 发票类型Id，简单字典InvoiceType
        /// </summary>
        [Comment("发票类型Id，简单字典InvoiceType")]
        public Guid? InvoiceTypeId { get; set; }

        /// <summary>
        /// 发票抬头。
        /// </summary>
        [Comment("发票抬头")]
        [MaxLength(64)]
        public string InvoiceTitle { get; set; }

        /// <summary>
        /// 预计回款时间。
        /// </summary>
        [Column(TypeName = "datetime2(2)")]
        [Comment("预计回款时间")]
        public DateTime? PreReturnDate { get; set; }

        /// <summary>
        /// 实际回款时间。
        /// </summary>
        [Column(TypeName = "datetime2(2)")]
        [Comment("实际回款时间")]
        public DateTime? ReturnDate { get; set; }

        /// <summary>
        /// 币种。标准货币缩写。
        /// </summary>
        [MaxLength(4), Unicode(false)]
        [Comment("币种。标准货币缩写。")]
        public string Currency { get; set; }

        /// <summary>
        /// 保留未用！金额,可以不用实体字段，明细合计显示也行.
        /// </summary>
        [Comment("金额,可以不用实体字段，明细合计显示也行.")]
        [Precision(18, 4)]
        public decimal Amount { get; set; }

        /// <summary>
        /// 备注。
        /// </summary>
        [Comment("备注")]
        public string Remark { get; set; }

    }

    /// <summary>
    /// 业务费用收付款申请单明细项。
    /// </summary>
    public class DocFeeRequisitionItem : GuidKeyObjectBase
    {
        /// <summary>
        /// 申请单Id。
        /// </summary>
        [Comment("申请单Id")]
        public Guid? ParentId { get; set; }

        /// <summary>
        /// 绑定的费用Id。
        /// </summary>
        [Comment("绑定的费用Id")]
        public Guid? FeeId { get; set; }

        /// <summary>
        /// 本次申请金额。
        /// </summary>
        [Comment("本次申请金额")]
        [Precision(18, 4)]
        public decimal Amount { get; set; }

        ///// <summary>
        ///// 费用所属工作号。从源费用或工作号带出显示在明细列表
        ///// </summary>
        //[MaxLength(64)]
        //[Comment("费用所属工作号。从源费用或工作号带出显示在明细列表")]
        //public string JobNo { get; set; }

        ///// <summary>
        ///// 操作员Id,从源费用或工作号带出显示在明细列表
        ///// </summary>
        //[Comment("操作员Id,从源费用或工作号带出显示在明细列表")]
        //public Guid? OpertorId { get; set; }

        ///// <summary>
        ///// 业务员Id,从源费用或工作号带出显示在明细列表
        ///// </summary>
        //[Comment("业务员Id,从源费用或工作号带出显示在明细列表")]
        //public Guid? SalesId { get; set; }

        ///// <summary>
        ///// 费用种类Id，从源费用或工作号带出显示在明细列表
        ///// </summary>
        //[Comment("费用种类Id，从源费用或工作号带出显示在明细列表")]
        //public Guid? FeeTypeId { get; set; }

        ///// <summary>
        ///// 单价,从源费用或工作号带出显示在明细列表。
        ///// </summary>
        //[Comment("单价,从源费用或工作号带出显示在明细列表")]
        //[Precision(18, 4)]
        //public decimal Price { get; set; }

        ///// <summary>
        ///// 数量,从源费用或工作号带出显示在明细列表。
        ///// </summary>
        //[Comment("数量,从源费用或工作号带出显示在明细列表")]
        //[Precision(18, 4)]
        //public decimal Count { get; set; }

        ///// <summary>
        ///// 币种。标准货币缩写。从源费用或工作号带出显示在明细列表
        ///// </summary>
        //[MaxLength(4), Unicode(false)]
        //[Comment("币种。标准货币缩写。从源费用或工作号带出显示在明细列表")]
        //public string Currency { get; set; }

        ///// <summary>
        ///// 主单号.从源费用或工作号带出显示在明细列表
        ///// </summary>
        //[Comment("主单号,从源费用或工作号带出显示在明细列表")]
        //[MaxLength(128)]
        //public string MblNo { get; set; }

        ///// <summary>
        ///// 分单号字符串，/分隔多个分单号.从源费用或工作号带出显示在明细列表
        ///// </summary>
        //[Comment("分单号字符串，/分隔多个分单号,从源费用或工作号带出显示在明细列表")]
        //public string HblNoString { get; set; }

        ///// <summary>
        ///// 运输工具号，空运显示为航班号，海运显示为船名、陆运显示为卡车号.
        ///// </summary>
        //[Comment("运输工具号，空运显示为航班号，海运显示为船名、陆运显示为卡车号")]
        //[MaxLength(64)]
        //public string CarrierNo { get; set; }

    }
}
