using Microsoft.EntityFrameworkCore;
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
        /// 申请单明细id,关联 <see cref="DocFeeRequisitionItem"/> 的Id。
        /// </summary>
        [Comment("申请单明细id")]
        public Guid? RequisitionItemId { get; set; }

    }

    /// <summary>
    /// 提供用于 <see cref="PlInvoices"/> 和 <see cref="PlInvoicesItem"/> 实体的扩展方法。
    /// 这些方法便于访问关联实体和执行常见的财务计算。
    /// </summary>
    public static class PlInvoicesExtensions
    {
        /// <summary>
        /// 获取与结算单明细关联的申请单明细项。
        /// </summary>
        /// <param name="invoicesItem">结算单明细项。</param>
        /// <param name="db">数据库上下文。</param>
        /// <returns>
        /// 关联的申请单明细项；如果 <see cref="PlInvoicesItem.RequisitionItemId"/> 为 null 或找不到对应的明细项，则返回 null。
        /// </returns>
        /// <remarks>
        /// 此方法通过 <see cref="PlInvoicesItem.RequisitionItemId"/> 在数据库中查找关联的 <see cref="DocFeeRequisitionItem"/> 实体。
        /// 在结算流程中，用于获取原始申请信息以便进行核销处理。
        /// </remarks>
        public static DocFeeRequisitionItem GetRequisitionItem(this PlInvoicesItem invoicesItem, DbContext db)
        {
            return invoicesItem.RequisitionItemId is null ? null : db.Set<DocFeeRequisitionItem>().Find(invoicesItem.RequisitionItemId);
        }

        /// <summary>
        /// 获取指定结算单的所有明细项的查询。
        /// </summary>
        /// <param name="invoices">结算单实体。</param>
        /// <param name="db">数据库上下文。</param>
        /// <returns>
        /// 返回一个可查询的集合，包含与该结算单关联的所有明细项。
        /// 通过 <see cref="PlInvoicesItem.ParentId"/> 与结算单的 <see cref="GuidKeyObjectBase.Id"/> 匹配。
        /// </returns>
        /// <remarks>
        /// 此方法返回 IQueryable，允许对结果进行进一步的查询操作，如筛选、排序或投影。
        /// 直到调用如 ToList() 或 FirstOrDefault() 等方法时，实际的数据库查询才会执行。
        /// </remarks>
        public static IQueryable<PlInvoicesItem> GetChildren(this PlInvoices invoices, DbContext db)
        {
            return db.Set<PlInvoicesItem>().Where(x => x.ParentId == invoices.Id);
        }

        /// <summary>
        /// 获取结算单明细项所属的结算单。
        /// </summary>
        /// <param name="invoicesItem">结算单明细项。</param>
        /// <param name="db">数据库上下文。</param>
        /// <returns>
        /// 该明细项所属的结算单实体；如果 <see cref="PlInvoicesItem.ParentId"/> 为 null 或找不到对应的结算单，则返回 null。
        /// </returns>
        /// <remarks>
        /// 此方法通过 <see cref="PlInvoicesItem.ParentId"/> 在数据库中查找关联的 <see cref="PlInvoices"/> 实体。
        /// 用于获取明细项的父级结算单信息，如币种、收付凭证号等。
        /// </remarks>
        public static PlInvoices GetParent(this PlInvoicesItem invoicesItem, DbContext db)
        {
            return invoicesItem.ParentId is null ? null : db.Set<PlInvoices>().Find(invoicesItem.ParentId);
        }

        /// <summary>
        /// 计算结算单明细的结算金额（宿币种）。
        /// </summary>
        /// <param name="invoicesItem">结算单明细项。</param>
        /// <param name="precision">计算结果的小数位精度，默认为 4 位。</param>
        /// <returns>
        /// 返回计算后的结算金额，即明细项汇率与金额的乘积，按指定精度四舍五入。
        /// 该金额的币种与父级结算单 <see cref="PlInvoices.Currency"/> 一致。
        /// </returns>
        /// <remarks>
        /// 此方法用于计算结算单明细项在目标币种（父结算单币种）下的金额。
        /// 计算公式：结算金额 = <see cref="PlInvoicesItem.ExchangeRate"/> × <see cref="PlInvoicesItem.Amount"/>。
        /// 结果使用 <see cref="Math.Round(decimal, int, MidpointRounding)"/> 进行四舍五入。
        /// </remarks>
        public static decimal GetDAmount(this PlInvoicesItem invoicesItem, int precision = 4)
        {
            return Math.Round(invoicesItem.ExchangeRate * invoicesItem.Amount, precision, MidpointRounding.AwayFromZero);
        }
    }
}
