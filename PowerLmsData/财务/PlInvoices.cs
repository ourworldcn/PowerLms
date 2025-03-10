﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;
using OW.Data;
using OW.DDD;
using PowerLmsServer.EfData;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerLms.Data
{
    /// <summary>
    /// 结算单。
    /// </summary>
    public class PlInvoices : GuidKeyObjectBase, ICreatorInfo
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public PlInvoices()
        {

        }

        /// <summary>
        /// 收付凭证号
        /// </summary>
        [Comment("收付凭证号")]
        public string IoPingzhengNo { get; set; }

        /// <summary>
        /// 首付日期。
        /// </summary>
        [Comment("首付日期")]
        public DateTime? IoDateTime { get; set; }

        /// <summary>
        /// 收付银行账号,本公司信息中银行id
        /// </summary>
        [Comment("收付银行账号,本公司信息中银行id")]
        public Guid? BankId { get; set; }

        /// <summary>
        /// 币种。标准货币缩写。结算后的币种。
        /// </summary>
        [MaxLength(4), Unicode(false)]
        [Comment("币种。标准货币缩写。")]
        public string Currency { get; set; }

        /// <summary>
        /// 金额。下属结算单明细的合计。
        /// </summary>
        [Comment("金额。下属结算单明细的合计。")]
        [Precision(18, 4)]
        public decimal Amount { get; set; }

        /// <summary>
        /// 结算单位Id。客户资料的id.
        /// </summary>
        [Comment("结算单位Id。客户资料的id.")]
        public Guid? JiesuanDanweiId { get; set; }

        /// <summary>
        /// 摘要。
        /// </summary>
        [Comment("摘要。")]
        public string Remark { get; set; }

        /// <summary>
        /// 是否预收付
        /// </summary>
        [Comment("是否预收付。")]
        public bool IsYushoufu { get; set; }

        /// <summary>
        /// 余额。
        /// </summary>
        [Comment("余额。")]
        [Precision(18, 4)]
        public decimal Surplus { get; set; }

        /// <summary>
        /// 财务费用。
        /// </summary>
        [Comment("财务费用。")]
        [Precision(18, 4)]
        public decimal FinanceFee { get; set; }

        /// <summary>
        /// 汇差损益。外币结算时损失的部分
        /// </summary>
        [Comment("汇差损益。外币结算时损失的部分")]
        [Precision(18, 4)]
        public decimal ExchangeLoss { get; set; }

        /// <summary>
        /// 附加说明。
        /// </summary>
        [Comment("附加说明。")]
        public string Remark2 { get; set; }

        /// <summary>
        /// 银行流水号。
        /// </summary>
        [Comment("银行流水号。")]
        public string BankSerialNo { get; set; }

        /// <summary>
        /// 财务日期
        /// </summary>
        [Comment("财务日期。")]
        public DateTime? FinanceDateTime { get; set; }

        //public bool IsConfirm { get; set; }

        /// <summary>
        /// 确认时间,为null表示未确认。
        /// </summary>
        [Comment("确认时间。")]
        public DateTime? ConfirmDateTime { get; set; }

        /// <summary>
        /// 确认人Id.
        /// </summary>
        [Comment("确认人Id。")]
        public Guid? ConfirmId { get; set; }

        #region ICreatorInfo

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
        #endregion ICreatorInfo

        /// <summary>
        /// 收付，false支出，true收入。自动计算强制改变，0算支出。。
        /// </summary>
        [Comment("收付，false支出，true收入。")]
        public bool IO { get; set; }

    }

    /// <summary>
    /// 结算单明细。
    /// </summary>
    [Index(nameof(ParentId), IsUnique = false)]
    public class PlInvoicesItem : GuidKeyObjectBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public PlInvoicesItem()
        {

        }

        /// <summary>
        /// 结算单id
        /// </summary>
        public Guid? ParentId { get; set; }

        /// <summary>
        /// 本次核销（结算）金额（按申请单的币种）。汇款方汇款的金额（如可能是美元）。
        /// </summary>
        [Comment("本次核销（结算）金额。")]
        [Precision(18, 4)]
        public decimal Amount { get; set; }

        /// <summary>
        /// 结算汇率，用户手工填写。乘以Amount得到结算金额（结算金额的币种是总单中的PlInvoices.Currency）。
        /// </summary>
        [Comment("结算汇率，用户手工填写。")]
        [Precision(18, 4)]
        public decimal ExchangeRate { get; set; }

        /// <summary>
        /// 申请单明细id,对应申请明细的费用。
        /// </summary>
        [Comment("申请单明细id")]
        public Guid? RequisitionItemId { get; set; }

    }

    public static class PlInvoicesExtensions
    {
        public static DocFeeRequisitionItem GetRequisitionItem(this PlInvoicesItem invoicesItem, DbContext db)
        {
            return invoicesItem.RequisitionItemId is null ? null : db.Set<DocFeeRequisitionItem>().Find(invoicesItem.RequisitionItemId);
        }

        public static IQueryable<PlInvoicesItem> GetChildren(this PlInvoices invoices, DbContext db)
        {
            return db.Set<PlInvoicesItem>().Where(x => x.ParentId == invoices.Id);
        }

        public static PlInvoices GetParent(this PlInvoicesItem invoicesItem, DbContext db)
        {
            return invoicesItem.ParentId is null ? null : db.Set<PlInvoices>().Find(invoicesItem.ParentId);
        }

        /// <summary>
        /// 获取结算单明细的结算金额（宿币种）。
        /// </summary>
        /// <param name="invoicesItem"></param>
        /// <param name="precision"></param>
        /// <returns></returns>
        public static decimal GetDAmount(this PlInvoicesItem invoicesItem, int precision = 4)
        {
            return Math.Round(invoicesItem.ExchangeRate * invoicesItem.Amount, precision, MidpointRounding.AwayFromZero);
        }
    }
}
