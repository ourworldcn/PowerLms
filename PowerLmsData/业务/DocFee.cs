using Microsoft.EntityFrameworkCore;
using OW.Data;
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
        /// 业务Id。
        /// </summary>
        [Comment("业务Id")]
        public Guid? JobId { get; set; }

        /// <summary>
        /// 账单号。
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
        /// 收入或指出，true收入，false为支出。
        /// </summary>
        [Comment("收入或指出，true收入，false为支出。")]
        public bool IO { get; set; }

        /// <summary>
        /// 结算方式，简单字典FeePayType。
        /// </summary>
        [Comment("结算方式，简单字典FeePayType")]
        public Guid? GainTypeId { get; set; }

        /// <summary>
        /// 单位,简单字典ContainerType,按票、按重量等
        /// </summary>
        [Comment("单位,简单字典ContainerType,按票、按重量等")]
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
        /// 金额,两位小数、可以为负数.
        /// </summary>
        [Comment("金额,两位小数、可以为负数")]
        [Precision(18, 2)]
        public decimal Amount { get; set; }

        /// <summary>
        /// 币种。标准货币缩写。申请或结算时用的原币种。
        /// </summary>
        [MaxLength(4), Unicode(false)]
        [Comment("币种。标准货币缩写。")]
        public string Currency { get; set; }

        /// <summary>
        /// 本位币汇率,默认从汇率表调取,Amount乘以该属性得到本位币金额。
        /// </summary>
        [Comment("本位币汇率,默认从汇率表调取,机构本位币")]
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
        /// 创建时间,系统默认，不能更改
        /// </summary>
        [Comment("新建时间,系统默认，不能更改。")]
        [Column(TypeName = "datetime2(2)")]
        public DateTime CreateDateTime { get; set; } = OwHelper.WorldNow;

        /// <summary>
        /// 预计结算日期，客户资料中信用日期自动计算出
        /// </summary>
        [Comment("预计结算日期，客户资料中信用日期自动计算出")]
        [Column(TypeName = "datetime2(2)")]
        public DateTime PreclearDate { get; set; }

        /// <summary>
        /// 审核日期，为空则未审核。
        /// </summary>
        [Comment("审核日期，为空则未审核"), Column(TypeName = "datetime2(2)")]
        public DateTime? AuditDateTime { get; set; }

        /// <summary>
        /// 审核人Id，为空则未审核。
        /// </summary>
        [Comment("审核人Id，为空则未审核")]
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
    }
}


