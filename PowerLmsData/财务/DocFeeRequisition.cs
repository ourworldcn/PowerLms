﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using OW.Data;
using OW.EntityFrameworkCore;
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
        /// <summary>
        /// 机构Id。冗余属性。
        /// </summary>
        public Guid? OrgId { get; set; }

        /// <summary>
        /// 申请单号,其他编码规则中【申请单号】自动生成
        /// </summary>
        [Comment("申请单号,其他编码规则中【申请单号】自动生成")]
        [MaxLength(64)]
        public string FrNo { get; set; }

        /// <summary>
        /// 收付，false支出，true收入。自动计算强制改变。0算支出。
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
        [Comment("制单时间")]
        [Precision(3)]
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
        [Comment("预计回款时间")]
        [Precision(3)]
        public DateTime? PreReturnDate { get; set; }

        /// <summary>
        /// 实际回款时间。
        /// </summary>
        [Comment("实际回款时间")]
        [Precision(3)]
        public DateTime? ReturnDate { get; set; }

        /// <summary>
        /// 币种。标准货币缩写。
        /// </summary>
        [MaxLength(4), Unicode(false)]
        [Comment("币种。标准货币缩写。")]
        public string Currency { get; set; }

        /// <summary>
        /// 金额,所有子项的金额的求和（需要换算币种）。
        /// </summary>
        [Comment("金额,所有子项的金额的求和（需要换算币种）。")]
        [Precision(18, 2)]
        public decimal Amount { get; set; }

        /// <summary>
        /// 备注。
        /// </summary>
        [Comment("备注")]
        public string Remark { get; set; }

        /// <summary>
        /// 已经结算的金额。计算属性。
        /// </summary>
        [Comment("已经结算的金额。计算属性。")]
        [Precision(18, 2)]
        public decimal TotalSettledAmount { get; set; }

        /// <summary>
        /// 关联的发票Id。冗余属性。
        /// </summary>
        [Comment("关联的发票Id，冗余属性")]
        public Guid? TaxInvoiceId { get; set; }

        /// <summary>发票号。</summary>
        [Comment("发票号")]
        [MaxLength(64)]
        public string InvoiceNumber { get; set; }
    }

    /// <summary>
    /// 申请单明细项。
    /// </summary>
    public class DocFeeRequisitionItem : GuidKeyObjectBase
    {
        /// <summary>
        /// 申请单Id。关联到 <see cref="DocFeeRequisition"/> 的Id。
        /// </summary>
        [Comment("申请单Id")]
        public Guid? ParentId { get; set; }

        /// <summary>
        /// 绑定的费用Id。关联到 <see cref="DocFee"/> 的Id。
        /// </summary>
        [Comment("绑定的费用Id")]
        public Guid? FeeId { get; set; }

        /// <summary>
        /// 本次申请金额。与对应费用的币种一致。
        /// </summary>
        [Comment("本次申请金额")]
        [Precision(18, 2)]
        public decimal Amount { get; set; }

        /// <summary>
        /// 已经结算的金额。计算属性。
        /// </summary>
        [Comment("已经结算的金额。计算属性。")]
        [Precision(18, 2)]
        public decimal TotalSettledAmount { get; set; }
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

    public static class DocFeeRequisitionExtensions
    {
        /// <summary>
        /// 获取申请单的费用明细。
        /// </summary>
        /// <param name="item"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public static DocFee GetDocFee(this DocFeeRequisitionItem item, DbContext db)
        {
            return item.FeeId is null ? null : db.Set<DocFee>().Find(item.FeeId);
        }

        public static IQueryable<DocFeeRequisitionItem> GetChildren(this DocFeeRequisition requisition, DbContext db)
        {
            return db.Set<DocFeeRequisitionItem>().WhereWithLocal(x => x.ParentId == requisition.Id).AsQueryable();
        }

        public static DocFeeRequisition GetParent(this DocFeeRequisitionItem item, DbContext db)
        {
            return item.ParentId is null ? null : db.Set<DocFeeRequisition>().Find(item.ParentId);
        }

        /// <summary>
        /// 获取相关的 结算 对象。
        /// </summary>
        /// <param name="requisitionItem"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public static IQueryable<PlInvoicesItem> GetInvoicesItems(this DocFeeRequisitionItem requisitionItem, DbContext db)
        {
            return db.Set<PlInvoicesItem>().Where(x => x.RequisitionItemId == requisitionItem.Id);
        }
    }

    /// <summary>
    /// 保存发票信息时，更新关联的申请单。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(IDbContextSaving<TaxInvoiceInfo>))]
    public class DocFeeRequisitionSaving : IDbContextSaving<TaxInvoiceInfo>
    {
        public void Saving(IEnumerable<EntityEntry> entity, IServiceProvider serviceProvider, Dictionary<object, object> states)
        {
            var db = entity.FirstOrDefault().Context;
            entity.Where(c => c.Entity is TaxInvoiceInfo).ToArray().ForEach(c =>
            {
                var item = (TaxInvoiceInfo)c.Entity;
                switch (c.State)
                {
                    case EntityState.Detached:
                        break;
                    case EntityState.Unchanged:
                        break;
                    case EntityState.Deleted:
                        {
                            if (c.OriginalValues.TryGetValue<Guid?>(nameof(TaxInvoiceInfo.DocFeeRequisitionId), out var rId))
                            {
                                if (db.Set<DocFeeRequisition>().Find(item.DocFeeRequisitionId.GetValueOrDefault()) is DocFeeRequisition requisition)
                                {
                                    requisition.TaxInvoiceId = null;
                                }
                            }
                        }
                        break;
                    case EntityState.Modified:
                    case EntityState.Added:
                        {
                            if (db.Set<DocFeeRequisition>().Find(item.DocFeeRequisitionId.GetValueOrDefault()) is DocFeeRequisition requisition)
                            {
                                requisition.TaxInvoiceId = item.Id;
                                requisition.InvoiceNumber = item.InvoiceNumber;
                            }
                        }
                        break;
                    default:
                        break;
                }
            });
        }
    }
}
