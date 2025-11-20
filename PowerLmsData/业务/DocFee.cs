using Microsoft.EntityFrameworkCore;
using OW.Data;
using OW.EntityFrameworkCore;
using PowerLmsServer.EfData;
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
    /// 业务单的费用。
    /// </summary>
    [Index(nameof(JobId))]
    [Index(nameof(BillId))]
    public class DocFee : GuidKeyObjectBase
    {
        /// <summary>
        /// 业务Id。关联到 <see cref="PlJob"/>。
        /// </summary>
        [Comment("业务Id")]
        public Guid? JobId { get; set; }

        /// <summary>
        /// 账单号。关联<see cref="DocBill"/>。
        /// </summary>
        [Comment("账单表中的id")]
        public Guid? BillId { get; set; }

        /// <summary>
        /// 费用种类字典项Id。
        /// </summary>
        [Comment("费用种类字典项Id")]
        public Guid? FeeTypeId { get; set; }

        /// <summary>
        /// 结算单位，客户资料中为结算单位的客户id。
        /// </summary>
        [Comment("结算单位，客户资料中为结算单位的客户id。")]
        public Guid? BalanceId { get; set; }

        /// <summary>
        /// 收入或支出，true收入，false为支出。
        /// </summary>
        [Comment("收入或支出，true为收入，false为支出。")]
        public bool IO { get; set; }

        /// <summary>
        /// 结算方式，简单字典FeePayType。
        /// </summary>
        [Comment("结算方式，简单字典FeePayType")]
        public Guid? GainTypeId { get; set; }

        /// <summary>
        /// 单位,简单字典ContainerType,按票、按重量等
        /// </summary>
        [Comment("单位，简单字典ContainerType，按票、按重量等")]
        public Guid? ContainerTypeId { get; set; }

        /// <summary>
        /// 数量。
        /// </summary>
        [Comment("数量")]
        public decimal UnitCount { get; set; }

        /// <summary>
        /// 单价，4位小数。
        /// </summary>
        [Comment("单价，4位小数。")]
        [Precision(18, 4)]
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// 金额，两位小数，可以为负数。
        /// </summary>
        [Comment("金额，两位小数，可以为负数。")]
        [Precision(18, 2)]
        public decimal Amount { get; set; }

        /// <summary>
        /// 币种。标准货币缩写。申请或结算时用的原币种。
        /// </summary>
        [MaxLength(4), Unicode(false)]
        [Comment("币种。标准货币缩写。")]
        public string Currency { get; set; }

        /// <summary>
        /// 本位币汇率，默认从汇率表调取，Amount乘以该属性得到本位币金额。
        /// </summary>
        [Comment("本位币汇率，默认从汇率表调取，Amount乘以该属性得到本位币金额。")]
        [Precision(18, 4)]
        public decimal ExchangeRate { get; set; }

        /// <summary>
        /// 备注。
        /// </summary>
        [Comment("备注")]
        public string Remark { get; set; }

        /// <summary>
        /// 创建人，建立时系统默认，默认不可更改。
        /// </summary>
        [Comment("创建人，建立时系统默认，默认不可更改")]
        public Guid? CreateBy { get; set; }

        /// <summary>
        /// 创建时间，系统默认，不能更改。
        /// </summary>
        [Comment("创建时间，系统默认，不能更改。")]
        [Precision(3)]
        public DateTime CreateDateTime { get; set; } = OwHelper.WorldNow;

        /// <summary>
        /// 预计结算日期，客户资料中信用日期自动计算出。
        /// </summary>
        [Comment("预计结算日期，客户资料中信用日期自动计算出。")]
        [Precision(3)]
        public DateTime PreclearDate { get; set; }

        /// <summary>
        /// 审核日期，为空则未审核。
        /// </summary>
        [Comment("审核日期，为空则未审核。")]
        [Precision(3)]
        public DateTime? AuditDateTime { get; set; }

        /// <summary>
        /// 审核人Id，为空则未审核。
        /// </summary>
        [Comment("审核人Id，为空则未审核。")]
        public Guid? AuditOperatorId { get; set; }

        /// <summary>
        /// 已经申请的合计金额。计算属性。
        /// </summary>
        [Comment("已经申请的合计金额。计算属性。")]
        [Precision(18, 2)]
        public decimal TotalRequestedAmount { get; set; }

        /// <summary>
        /// 已经结算的金额。计算属性。
        /// </summary>
        [Comment("已经结算的金额。计算属性。")]
        [Precision(18, 2)]
        public decimal TotalSettledAmount { get; set; }

        /// <summary>
        /// 行版本号。用于开放式并发控制，防止并发更新时的数据覆盖问题。
        /// EF Core 会在更新时自动检查此字段，如果值不匹配则抛出 DbUpdateConcurrencyException。
        /// </summary>
        [Timestamp]
        [Comment("行版本号，用于开放式并发控制")]
        public byte[] RowVersion { get; set; }

        #region 金额计算方法

        /// <summary>
        /// 计算费用的已申请金额。
        /// </summary>
        /// <param name="feeId">费用ID</param>
        /// <param name="dbContext">数据库上下文</param>
        /// <returns>已申请金额（2位小数精度）</returns>
        /// <exception cref="ArgumentNullException">dbContext为空时抛出</exception>
        /// <remarks>
        /// 计算公式：sum(申请单明细.Amount)
        /// 注意事项：
        /// 1. 先加载所有相关数据到本地缓存（确保数据一致性）
        /// 2. 在内存中过滤和计算（反映事务内的最新状态）
        /// 3. 正确处理Added/Modified/Deleted状态
        /// 4. 空集合Sum自动返回0
        /// </remarks>
        public static decimal CalculateTotalRequestedAmount(Guid feeId, DbContext dbContext)
        {
            if (dbContext == null)
                throw new ArgumentNullException(nameof(dbContext));
            dbContext.Set<DocFeeRequisitionItem>()
                .Where(c => c.FeeId == feeId)
                .Load();
            return dbContext.Set<DocFeeRequisitionItem>()
                .Local
                .Where(c => c.FeeId == feeId && dbContext.Entry(c).State != EntityState.Deleted)
                .Sum(c => c.Amount);
        }

        /// <summary>
        /// 计算费用的已结算金额。
        /// </summary>
        /// <param name="feeId">费用ID</param>
        /// <param name="dbContext">数据库上下文</param>
        /// <returns>已结算金额（2位小数精度）</returns>
        /// <exception cref="ArgumentNullException">dbContext为空时抛出</exception>
        /// <remarks>
        /// 计算公式：sum(申请单明细.TotalSettledAmount)
        /// 注意事项：
        /// 1. 先加载所有相关数据到本地缓存（确保数据一致性）
        /// 2. 在内存中过滤和计算（反映事务内的最新状态）
        /// 3. 正确处理Added/Modified/Deleted状态
        /// 4. 空集合Sum自动返回0
        /// 5. TotalSettledAmount 已经是通过 DocFeeRequisitionItem.CalculateTotalSettledAmount 计算的2位小数结果
        /// </remarks>
        public static decimal CalculateTotalSettledAmount(Guid feeId, DbContext dbContext)
        {
            if (dbContext == null)
                throw new ArgumentNullException(nameof(dbContext));
            dbContext.Set<DocFeeRequisitionItem>()
                .Where(c => c.FeeId == feeId)
                .Load();
            return dbContext.Set<DocFeeRequisitionItem>()
                .Local
                .Where(c => c.FeeId == feeId && dbContext.Entry(c).State != EntityState.Deleted)
                .Sum(c => c.TotalSettledAmount);
        }

        /// <summary>
        /// 校验申请单明细是否超额。
        /// </summary>
        /// <param name="feeId">费用ID</param>
        /// <param name="newRequisitionItemAmount">新申请单明细金额（正数或负数）</param>
        /// <param name="excludeRequisitionItemId">要排除的申请单明细ID（修改场景下排除当前明细的原有金额）</param>
        /// <param name="dbContext">数据库上下文</param>
        /// <param name="errorMessage">校验失败时的错误消息</param>
        /// <returns>true=校验通过，false=超额或不合规</returns>
        /// <remarks>
        /// 核心规则：
        /// 1. 已申请金额总和（按绝对值）不应超过原始费用金额（按绝对值）
        /// 2. 申请后的总金额不能小于0（防止过度冲红）
        /// 3. 支持负数金额冲账场景
        /// 
        /// 校验公式：
        /// - 规则1：|当前已申请金额 - 排除明细金额 + 新明细金额| ≤ |费用原始金额|
        /// - 规则2：(当前已申请金额 - 排除明细金额 + 新明细金额) ≥ 0
        /// 
        /// 业务场景：
        /// 1. 新增申请单明细：excludeRequisitionItemId = null
        /// 2. 修改申请单明细：excludeRequisitionItemId = 当前明细ID
        /// 3. 负金额冲账：newRequisitionItemAmount 为负数
        /// 4. 防止过度冲红：申请后总金额不能为负
        /// 
        /// 并发安全：
        /// - 先加载所有相关数据到本地缓存（确保数据一致性）
        /// - 在内存中过滤和计算（反映事务内的最新状态）
        /// - 正确处理Added/Modified/Deleted状态
        /// </remarks>
        public static bool ValidateRequisitionItemAmount(
            Guid feeId,
            decimal newRequisitionItemAmount,
            Guid? excludeRequisitionItemId,
            DbContext dbContext,
            out string errorMessage)
        {
            if (dbContext == null)
                throw new ArgumentNullException(nameof(dbContext));
            var fee = dbContext.Set<DocFee>().Find(feeId);
            if (fee == null)
            {
                errorMessage = $"未找到费用ID:{feeId}";
                return false;
            }
            dbContext.Set<DocFeeRequisitionItem>()
                .Where(c => c.FeeId == feeId)
                .Load();
            var currentTotalRequested = dbContext.Set<DocFeeRequisitionItem>()
                .Local
                .Where(c => c.FeeId == feeId &&
                           dbContext.Entry(c).State != EntityState.Deleted &&
                           (!excludeRequisitionItemId.HasValue || c.Id != excludeRequisitionItemId.Value))
                .Sum(c => c.Amount);
            var newTotalRequested = currentTotalRequested + newRequisitionItemAmount;
            if (Math.Abs(newTotalRequested) > Math.Abs(fee.Amount))
            {
                errorMessage = $"申请金额超额：费用原始金额={fee.Amount:F2}，当前已申请={currentTotalRequested:F2}，" +
                              $"本次申请={newRequisitionItemAmount:F2}，合计={newTotalRequested:F2}（绝对值超过原始金额绝对值）";
                return false;
            }
            if (newTotalRequested < 0)
            {
                errorMessage = $"申请后总金额不能为负：费用原始金额={fee.Amount:F2}，当前已申请={currentTotalRequested:F2}，" +
                              $"本次申请={newRequisitionItemAmount:F2}，合计={newTotalRequested:F2}（过度冲红导致总金额为负）";
                return false;
            }
            errorMessage = null;
            return true;
        }

        #endregion 金额计算方法
    }

    public static class DocFeeExtensions
    {
        /// <summary>
        /// 获取相关的 Job 对象。
        /// </summary>
        /// <param name="docFee">DocFee 对象</param>
        /// <param name="context">数据库上下文</param>
        /// <returns>相关的 Job 对象</returns>
        public static PlJob GetJob(this DocFee docFee, DbContext context)
        {
            return docFee.JobId is null ? null : context.Set<PlJob>().Find(docFee.JobId.Value);
        }

        /// <summary>
        /// 获取相关的 Bill 对象。
        /// </summary>
        /// <param name="docFee">DocFee 对象</param>
        /// <param name="context">数据库上下文</param>
        /// <returns>相关的 Bill 对象</returns>
        public static DocBill GetBill(this DocFee docFee, DbContext context)
        {
            return docFee.BillId is null ? null : context.Set<DocBill>().Find(docFee.BillId.Value);
        }

        /// <summary>
        /// 获取相关的 Balance Customer 对象。
        /// </summary>
        /// <param name="docFee">DocFee 对象</param>
        /// <param name="context">数据库上下文</param>
        /// <returns>相关的 Customer 对象</returns>
        public static PlCustomer GetBalanceCustomer(this DocFee docFee, DbContext context)
        {
            return docFee.BalanceId is null ? null : context.Set<PlCustomer>().Find(docFee.BalanceId.Value);
        }

        /// <summary>
        /// 获取相关的 申请 对象。
        /// </summary>
        /// <param name="fee"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static IQueryable<DocFeeRequisitionItem> GetRequisitionItems(this DocFee fee, DbContext context)
        {
            return context.Set<DocFeeRequisitionItem>().Where(x => x.FeeId == fee.Id);
        }
    }
}

























