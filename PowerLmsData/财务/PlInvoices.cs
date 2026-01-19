/*
 * 项目：PowerLms货运物流业务管理系统
 * 模块：财务管理 - 主营业务结算单
 * 文件说明：
 * - 功能1：PlInvoices结算单实体定义，支持复杂的财务结算业务
 * - 功能2：PlInvoicesItem结算单明细实体，支持多明细项核销
 * - 功能3：PlInvoicesExtensions扩展方法，提供丰富的业务计算功能
 * 技术要点：
 * - 基于EntityFramework Core的企业级数据建模
 * - 支持多币种计算和汇率处理，精度控制：金额2位小数，汇率4位小数
 * - 2025年1月扩展：新增16个字段支持主营业务结算单功能改造
 * - 保持向后兼容：新增字段均为可空类型，避免影响现有数据
 * 作者：zc
 * 创建：2024年
 * 修改：2025-01-27 基于会议纪要需求，扩展16个新字段支持深度改造
 */

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;
using OW.Data;
using OW.DDD;
using PowerLms.Data.Finance;
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
    /// 主营业务结算单实体，支持海运、空运、陆运等全流程财务结算业务。
    /// 2025年1月功能改造：新增16个字段，支持更复杂的财务计算和多币种处理。
    /// </summary>
    public class PlInvoices : GuidKeyObjectBase, ICreatorInfo, IFinancialExportable
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public PlInvoices()
        {
            // 设置新增字段的默认值
            IsExportToFinancialSoftware = true; // 默认允许导出到财务软件
        }

        #region 原有字段 - 经过长期验证的核心字段

        /// <summary>
        /// 收付凭证号
        /// </summary>
        [Comment("收付凭证号")]
        public string IoPingzhengNo { get; set; }

        /// <summary>
        /// 首付日期。
        /// </summary>
        [Comment("首付日期")]
        [Precision(3)]
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
        /// 金额。由前端直接填写，不再进行自动计算。
        /// </summary>
        [Comment("金额。")]
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
        [Precision(3)]
        public DateTime? FinanceDateTime { get; set; }

        //public bool IsConfirm { get; set; }

        /// <summary>
        /// 确认时间,为null表示未确认。
        /// </summary>
        [Comment("确认时间。")]
        [Precision(3)]
        public DateTime? ConfirmDateTime { get; set; }

        /// <summary>
        /// 确认人Id.
        /// </summary>
        [Comment("确认人Id。")]
        public Guid? ConfirmId { get; set; }

        /// <summary>
        /// 收付，false支出，true收入。自动计算强制改变，0算支出。。
        /// </summary>
        [Comment("收付，false支出，true收入。")]
        public bool IO { get; set; }

        #endregion

        #region 新增字段 - 主营业务结算单功能改造 (2025年1月)

        /// <summary>
        /// 财务支付确认。
        /// 用于财务对账需要，标识该笔收付款是否已经在财务系统中确认。
        /// </summary>
        [Comment("财务支付确认，对账需要")]
        public bool? FinancialPaymentConfirmed { get; set; }

        /// <summary>
        /// 财务凭证号。
        /// 支付账号关联的凭证字自动生成，用于与外部财务系统（如金蝶）的凭证对应。
        /// </summary>
        [MaxLength(64)]
        [Comment("财务凭证号，支付账号关联的凭证字自动生成")]
        public string FinancialVoucherNumber { get; set; }

        /// <summary>
        /// 支付方法。
        /// 简单字典ApplyType，如：银行转账、现金支付、支票等。
        /// </summary>
        [MaxLength(64)]
        [Comment("支付方法，简单字典ApplyType")]
        public string PaymentMethod { get; set; }

        /// <summary>
        /// 财务信息。
        /// 存储与财务相关的补充信息，如特殊说明、审批意见等。
        /// </summary>
        [Comment("财务信息，string类型")]
        public string FinancialInformation { get; set; }

        /// <summary>
        /// 收/付汇率（主汇率）。
        /// 4位小数精度，收付金额对应的汇率，用于本位币金额计算。
        /// </summary>
        [Precision(18, 4)]
        [Comment("收/付汇率（主汇率），4位小数，收付金额对应的汇率")]
        public decimal? PaymentExchangeRate { get; set; }

        /// <summary>
        /// 收/付款合计本位币金额。
        /// 2位小数精度，计算公式：收付金额 × 收付汇率。
        /// </summary>
        [Precision(18, 2)]
        [Comment("收/付款合计本位币金额，2位小数，收付金额*收付汇率")]
        public decimal? PaymentTotalBaseCurrencyAmount { get; set; }

        /// <summary>
        /// 手续费金额。
        /// 2位小数精度，计算公式：收付金额 - 实收金额。
        /// </summary>
        [Precision(18, 2)]
        [Comment("手续费金额，2位小数，收付金额-实收金额")]
        public decimal? ServiceFeeAmount { get; set; }

        /// <summary>
        /// 手续费本位币金额。
        /// 2位小数精度，计算公式：手续费（主币种） × 收付汇率。
        /// </summary>
        [Precision(18, 2)]
        [Comment("手续费本位币金额，2位小数，手续费（主币种）*收付汇率")]
        public decimal? ServiceFeeBaseCurrencyAmount { get; set; }

        /// <summary>
        /// 实收金额。
        /// 2位小数精度，实际收到的金额，可能与收付金额不同（扣除手续费后）。
        /// </summary>
        [Precision(18, 2)]
        [Comment("实收金额，2位小数")]
        public decimal? ActualReceivedAmount { get; set; }

        /// <summary>
        /// 实收金额本位币金额。
        /// 2位小数精度，计算公式：实收金额（主币种） × 收付汇率。
        /// </summary>
        [Precision(18, 2)]
        [Comment("实收金额本位币金额，2位小数，实收金额（主币种）*收付汇率")]
        public decimal? ActualReceivedBaseCurrencyAmount { get; set; }

        /// <summary>
        /// 预收/付金额金额。
        /// 2位小数精度，计算公式：收付金额 - 核销金额（主币种）。
        /// </summary>
        [Precision(18, 2)]
        [Comment("预收/付金额金额，2位小数，收付金额-核销金额（主币种）")]
        public decimal? AdvancePaymentAmount { get; set; }

        /// <summary>
        /// 预收/付金额本位币金额。
        /// 2位小数精度，计算公式：预收金额（主币种） × 收付汇率。
        /// </summary>
        [Precision(18, 2)]
        [Comment("预收/付金额本位币金额，2位小数，预收金额（主币种）*收付汇率")]
        public decimal? AdvancePaymentBaseCurrencyAmount { get; set; }

        /// <summary>
        /// 回款单位。
        /// 选择客户资料中的结算单位，可能与原结算单位不同。
        /// </summary>
        [Comment("回款单位，选择客户资料中的结算单位")]
        public Guid? RefundUnitId { get; set; }

        /// <summary>
        /// 是否导出到财务软件。
        /// true允许导出，false禁止导出，默认true。
        /// 这是一个控制开关（允许/禁止导出），而不是状态记录（已导出/未导出）。
        /// </summary>
        [Comment("是否导出到财务软件，true允许导出，默认true")]
        public bool IsExportToFinancialSoftware { get; set; } = true;

        /// <summary>
        /// 预收/付冲应收金额。
        /// 2位小数精度，用于处理预收预付款项的冲抵计算。
        /// </summary>
        [Precision(18, 2)]
        [Comment("预收/付冲应收金额，2位小数")]
        public decimal? AdvanceOffsetReceivableAmount { get; set; }

        /// <summary>
        /// 预收/付款金额。
        /// 2位小数精度，从以前的预收中获取，用于预收预付的历史追溯。
        /// </summary>
        [Precision(18, 2)]
        [Comment("预收/付款金额，2位小数，从以前的预收中获取")]
        public decimal? AdvancePaymentFromPreviousAmount { get; set; }

        #endregion

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
        [Precision(3)]
        public DateTime CreateDateTime { get; set; }

        #endregion ICreatorInfo

        #region IFinancialExportable

        /// <summary>
        /// 导出时间。null表示未导出，非null表示已导出。
        /// 
        /// <para>**重要：是否已导出以此字段为准！**</para>
        /// <para>判断导出状态的唯一依据是 ExportedDateTime 字段，ExportedUserId 仅用于审计追踪。</para>
        /// <para>即使 ExportedUserId 为空，只要 ExportedDateTime 有值，就视为已导出。</para>
        /// </summary>
        [Comment("导出时间，null表示未导出")]
        [Precision(3)]
        public DateTime? ExportedDateTime { get; set; }

        /// <summary>
        /// 导出用户ID。记录执行导出操作的用户，用于审计和权限验证。
        /// 
        /// <para>**注意：此字段仅用于审计追踪，不作为导出状态的判断依据。**</para>
        /// <para>是否已导出以 ExportedDateTime 字段为准。</para>
        /// </summary>
        [Comment("导出用户ID，用于审计和权限验证")]
        public Guid? ExportedUserId { get; set; }

        #endregion

        /// <summary>
        /// 行版本号。用于开放式并发控制，防止并发更新时的数据覆盖问题。
        /// EF Core会在更新时自动检查此字段，如果值不匹配则抛出DbUpdateConcurrencyException。
        /// SQL Server自动维护此字段，每次更新时自动递增。
        /// </summary>
        [Timestamp]
        [Comment("行版本号，用于开放式并发控制")]
        public byte[] RowVersion { get; set; }
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

        #region 金额计算方法

        /// <summary>
        /// 计算指定申请单明细的已结算金额（汇总所有关联的结算单明细）。
        /// </summary>
        /// <param name="requisitionItemId">申请单明细ID</param>
        /// <param name="dbContext">数据库上下文</param>
        /// <returns>已结算金额（2位小数精度）</returns>
        /// <exception cref="ArgumentNullException">dbContext为空时抛出</exception>
        /// <remarks>
        /// 计算公式：sum(PlInvoicesItem.Amount × PlInvoicesItem.ExchangeRate)，结果四舍五入到2位小数
        /// 优化说明：
        /// 1. 先 .Load() 加载所有相关数据到本地缓存
        /// 2. 从 .Local 中计算，自动包含事务内新增/修改的实体
        /// 3. 正确过滤已删除实体（EntityState.Deleted）
        /// 4. 空集合 Sum 自动返回 0
        /// 5. 计算结果四舍五入到2位小数，与 DocFeeRequisitionItem.TotalSettledAmount 字段精度一致
        /// </remarks>
        public static decimal CalculateTotalSettledAmountForRequisitionItem(Guid requisitionItemId, DbContext dbContext)
        {
            ArgumentNullException.ThrowIfNull(dbContext);

            // 先加载到本地缓存（确保包含数据库和事务内实体）
            dbContext.Set<PlInvoicesItem>()
                .Where(c => c.RequisitionItemId == requisitionItemId)
                .Load();

            // 从本地缓存中计算，自动过滤已删除实体
            return dbContext.Set<PlInvoicesItem>()
                .Local
                .Where(c => c.RequisitionItemId == requisitionItemId &&
                           dbContext.Entry(c).State != EntityState.Deleted)
                .Sum(c => Math.Round(c.Amount * c.ExchangeRate, 2, MidpointRounding.AwayFromZero));
        }

        #endregion 金额计算方法

        /// <summary>
        /// 行版本号。用于开放式并发控制，防止并发更新时的数据覆盖问题。
        /// EF Core会在更新时自动检查此字段，如果值不匹配则抛出DbUpdateConcurrencyException。
        /// SQL Server自动维护此字段，每次更新时自动递增。
        /// </summary>
        [Timestamp]
        [Comment("行版本号，用于开放式并发控制")]
        public byte[] RowVersion { get; set; }
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
