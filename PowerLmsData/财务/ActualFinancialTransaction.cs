/*
 * 项目：PowerLms财务系统
 * 模块：财务数据管理 - 实际收付记录表
 * 文件说明：
 * - 功能1：记录结算单的实际收付款情况，支持分次收付场景
 * - 功能2：最小化字段设计，通过关联查询获取冗余信息
 * - 功能3：支持软删除和恢复功能
 * 技术要点：
 * - 基于Entity Framework Core的数据建模
 * - 多租户数据隔离支持
 * - 通用父单据ID设计
 * - 实现IMarkDelete接口支持软删除
 * 作者：zc
 * 创建：2025-01
 * 修改：2025-01-27 删除Status字段，调整时间精度为毫秒，移除备注长度限制
 */
using Microsoft.EntityFrameworkCore;
using OW.Data;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
namespace PowerLms.Data
{
    /// <summary>
    /// 实际收付记录表。
    /// 用于记录结算单的实际收付款情况，支持一笔结算分多次收/付款的业务场景。
    /// 通用化设计，挂靠在父单据上，通过关联查询获取详细信息。
    /// </summary>
    [Comment("实际收付记录表")]
    [Index(nameof(ParentId), IsUnique = false)]
    public class ActualFinancialTransaction : GuidKeyObjectBase, ICreatorInfo, IMarkDelete
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public ActualFinancialTransaction()
        {
            TransactionDate = DateTime.Today;
            CreateDateTime = OwHelper.WorldNow;
        }
        /// <summary>
        /// 挂靠的父单据Id。通用设计，当前主要关联到结算单(PlInvoices)。
        /// 父单据包含币种、汇率、结算单位等基本信息，避免冗余存储。
        /// </summary>
        [Comment("挂靠的父单据Id，通用设计，当前主要关联到结算单")]
        public Guid? ParentId { get; set; }
        /// <summary>
        /// 收付款日期。实际发生收付款的业务日期，精确到毫秒。
        /// </summary>
        [Comment("收付款日期，实际发生收付款的业务日期，精确到毫秒")]
        [Precision(3)]
        public DateTime TransactionDate { get; set; }
        /// <summary>
        /// 实收付金额。本次实际收付的金额，2位小数精度。
        /// 多条记录的sum后，应对应结算单的收付金额。
        /// </summary>
        [Comment("实收付金额，本次实际收付的金额，2位小数精度")]
        [Precision(18, 2)]
        public decimal Amount { get; set; }
        /// <summary>
        /// 手续费。本次收付产生的手续费，2位小数精度。
        /// 包括银行手续费、汇款费等各类收付相关费用。
        /// </summary>
        [Comment("手续费，本次收付产生的手续费，2位小数精度")]
        [Precision(18, 2)]
        public decimal ServiceFee { get; set; }
        /// <summary>
        /// 银行流水号(水单号)。银行转账的流水号或水单号，用于对账和确认。
        /// </summary>
        [Comment("银行流水号(水单号)，银行转账的流水号，用于对账和确认")]
        [MaxLength(64)]
        public string BankFlowNumber { get; set; }
        /// <summary>
        /// 结算账号Id。本公司信息中的银行账号ID，关联到BankInfo表。
        /// 标识使用哪个银行账号进行收付款。
        /// </summary>
        [Comment("结算账号Id，本公司信息中的银行账号ID，关联到BankInfo表")]
        public Guid? BankAccountId { get; set; }
        /// <summary>
        /// 备注。记录收付款的备注信息，如收付原因、特殊说明等。
        /// </summary>
        [Comment("备注，记录收付款的备注信息")]
        public string Remark { get; set; }
        #region ICreatorInfo
        /// <summary>
        /// 创建者Id。记录创建这条收付记录的操作员。
        /// </summary>
        [Comment("创建者Id，记录创建这条收付记录的操作员")]
        public Guid? CreateBy { get; set; }
        /// <summary>
        /// 创建时间。记录创建的时间，精确到毫秒。
        /// </summary>
        [Comment("创建时间，记录创建的时间，精确到毫秒")]
        [Precision(3)]
        public DateTime CreateDateTime { get; set; }
        #endregion
        #region IMarkDelete
        /// <summary>
        /// 是否已标记为删除。false(默认)未标记为删除，true标记为删除。
        /// </summary>
        [Comment("是否已标记为删除。false(默认)未标记为删除，true标记为删除。")]
        public bool IsDelete { get; set; }
        #endregion
    }
    /// <summary>
    /// 实际收付记录扩展方法
    /// </summary>
    public static class ActualFinancialTransactionExtensions
    {
        /// <summary>
        /// 获取关联的银行账户信息
        /// </summary>
        /// <param name="transaction">收付记录</param>
        /// <param name="context">数据库上下文</param>
        /// <returns>银行账户信息</returns>
        public static BankInfo GetBankAccount(this ActualFinancialTransaction transaction, DbContext context)
        {
            return transaction.BankAccountId.HasValue ? 
                context.Set<BankInfo>().Find(transaction.BankAccountId.Value) : null;
        }
        /// <summary>
        /// 获取关联的结算单信息
        /// </summary>
        /// <param name="transaction">收付记录</param>
        /// <param name="context">数据库上下文</param>
        /// <returns>结算单信息</returns>
        public static PlInvoices GetParentSettlement(this ActualFinancialTransaction transaction, DbContext context)
        {
            return transaction.ParentId.HasValue ? 
                context.Set<PlInvoices>().Find(transaction.ParentId.Value) : null;
        }
        /// <summary>
        /// 获取创建人信息
        /// </summary>
        /// <param name="transaction">收付记录</param>
        /// <param name="context">数据库上下文</param>
        /// <returns>创建人账户信息</returns>
        public static Account GetCreator(this ActualFinancialTransaction transaction, DbContext context)
        {
            return transaction.CreateBy.HasValue ? 
                context.Set<Account>().Find(transaction.CreateBy.Value) : null;
        }
        /// <summary>
        /// 获取某个结算单的所有实际收付记录
        /// </summary>
        /// <param name="context">数据库上下文</param>
        /// <param name="settlementId">结算单Id</param>
        /// <returns>实际收付记录查询</returns>
        public static IQueryable<ActualFinancialTransaction> GetTransactionsBySettlement(this DbContext context, Guid settlementId)
        {
            return context.Set<ActualFinancialTransaction>()
                .Where(t => t.ParentId == settlementId && !t.IsDelete);
        }
        /// <summary>
        /// 计算某个结算单的实收付金额合计（仅包含未删除的记录）
        /// </summary>
        /// <param name="context">数据库上下文</param>
        /// <param name="settlementId">结算单Id</param>
        /// <returns>实收付金额合计</returns>
        public static decimal GetTotalAmountBySettlement(this DbContext context, Guid settlementId)
        {
            return context.Set<ActualFinancialTransaction>()
                .Where(t => t.ParentId == settlementId && !t.IsDelete)
                .Sum(t => t.Amount);
        }
    }
}
