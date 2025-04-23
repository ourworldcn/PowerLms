using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OW.Data;
using OW.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PowerLms.Data
{
    /// <summary>
    /// 开票信息。记录开的发票实质填写的信息。
    /// </summary>
    public class TaxInvoiceInfo : GuidKeyObjectBase
    {
        #region 基本信息
        /// <summary>开票渠道Id。关联到<see cref="TaxInvoiceChannelAccount"/>。</summary>
        [Comment("开票渠道Id")]
        public Guid? TaxInvoiceChannelAccountlId { get; set; }

        /// <summary>发票状态。0：创建后待审核；1：已审核开票中；2：已开票。</summary>
        [Comment("发票状态。0：创建后待审核；1：已审核开票中；2：已开票")]
        public byte State { get; set; }

        /// <summary>费用申请单Id。关联到<see cref="DocFeeRequisition"/>。</summary>
        [Comment("费用申请单Id")]
        public Guid? DocFeeRequisitionId { get; set; }

        /// <summary>发票号。</summary>
        [Comment("发票号")]
        [MaxLength(64)]
        public string InvoiceNumber { get; set; }

        /// <summary>发票流水号。</summary>
        [Comment("发票流水号")]
        [MaxLength(64)]
        public string InvoiceSerialNum { get; set; }

        /// <summary>
        /// 是否红字发票。true表示红字发票，false表示蓝字发票（默认）。
        /// </summary>
        public bool IsRedLetter { get; set; }

        /// <summary>发票类型。如：增值税专用发票、增值税普通发票等。</summary>
        /// <value>发票种类：p,普通发票(电票)(默认);c,普通发票(纸票); s,专用发票;e,收购发票(电票); f,收购发票(纸质); r,普通发票(卷式); b,增值税电子专用发票; 
        /// j,机动车销售统一发票;u,二手车销售统一发票; bs:电子发票(增值税专用发票)-即数电专票(电子),pc:电子发票(普通发票)-即数电普票(电子),
        /// es:数电纸质发票(增值税专用发票)-即数电专票(纸质); ec:数电纸质发票(普通发票)-即数电普票(纸质)</value>
        [Comment("发票类型")]
        [MaxLength(64)]
        public string InvoiceType { get; set; }

        /// <summary>开票项目名（产品）。</summary>
        [Comment("开票项目名（产品）")]
        [MaxLength(256)]
        public string InvoiceItemName { get; set; }

        /// <summary>备注。</summary>
        [Comment("备注")]
        [MaxLength(256)]
        public string Remark { get; set; }
        #endregion

        #region 时间信息
        /// <summary>申请时间。</summary>
        [Comment("申请时间")]
        [Column(TypeName = "DATETIME2(3)")]
        public DateTime? ApplyDateTime { get; set; }

        /// <summary>审核时间。</summary>
        [Comment("审核时间")]
        [Column(TypeName = "DATETIME2(3)")]
        public DateTime? AuditDateTime { get; set; }

        /// <summary>发送时间。</summary>
        [Comment("发送时间")]
        [Column(TypeName = "DATETIME2(3)")]
        public DateTime? SendTime { get; set; }

        /// <summary>返回发票号时间。</summary>
        [Comment("返回发票号时间")]
        [Column(TypeName = "DATETIME2(3)")]
        public DateTime? ReturnInvoiceTime { get; set; }
        #endregion

        #region 人员信息
        /// <summary>申请人Id。</summary>
        [Comment("申请人Id")]
        public Guid? ApplicantId { get; set; }

        /// <summary>审核人Id。</summary>
        [Comment("审核人Id")]
        public Guid? AuditorId { get; set; }
        #endregion

        #region 联系方式
        /// <summary>推送手机号。设置为空则不推送。</summary>
        [Comment("推送手机号。设置为空则不推送。")]
        [MaxLength(32),Phone]
        public string Mobile { get; set; }

        /// <summary>推送Mail。设置为空则不推送。</summary>
        [Comment("推送Mail。设置为空则不推送。")]
        [MaxLength(256), EmailAddress]
        public string Mail { get; set; }
        #endregion

        #region 销方信息
        /// <summary>销方开票数据。</summary>
        [Comment("销方开票数据")]
        public string SellerInvoiceData { get; set; }

        /// <summary>销方抬头。</summary>
        [Comment("销方抬头")]
        [MaxLength(256)]
        public string SellerTitle { get; set; }

        /// <summary>销方税号。</summary>
        [Comment("销方税号")]
        [MaxLength(64)]
        public string SellerTaxNum { get; set; }

        /// <summary>销方开户行。</summary>
        [Comment("销方开户行")]
        [MaxLength(64)]
        public string SellerBank { get; set; }

        /// <summary>销方账号。</summary>
        [Comment("销方账号")]
        [MaxLength(64)]
        public string SellerAccount { get; set; }

        /// <summary>销方地址。</summary>
        [Comment("销方地址")]
        [MaxLength(256)]
        public string SellerAddress { get; set; }

        /// <summary>销方电话。</summary>
        [Comment("销方电话")]
        [MaxLength(32)]
        public string SellerTel { get; set; }
        #endregion

        #region 购方信息
        /// <summary>购方抬头。</summary>
        [Comment("购方抬头")]
        [MaxLength(256)]
        public string BuyerTitle { get; set; }

        /// <summary>购方税号。</summary>
        [Comment("购方税号")]
        [MaxLength(64)]
        public string BuyerTaxNum { get; set; }

        /// <summary>购方开户行。</summary>
        [Comment("购方开户行")]
        [MaxLength(64)]
        public string BuyerBank { get; set; }

        /// <summary>购方账号。</summary>
        [Comment("购方账号")]
        [MaxLength(64)]
        public string BuyerAccount { get; set; }

        /// <summary>购方地址。</summary>
        [Comment("购方地址")]
        [MaxLength(256)]
        public string BuyerAddress { get; set; }

        /// <summary>购方电话。</summary>
        [Comment("购方电话")]
        [MaxLength(32)]
        public string BuyerTel { get; set; }
        #endregion

        /// <summary>
        /// 含税总金额。计算字段，由关联的 TaxInvoiceInfoItem.TaxInclusiveAmount 合计计算得到。
        /// </summary>
        [Comment("含税总金额。由关联的TaxInvoiceInfoItem.TaxInclusiveAmount 合计计算得到。")]
        [Precision(18, 2)]
        public decimal TaxInclusiveAmount { get; set; }

        /// <summary>
        /// 是否含税。服务器不使用。
        /// </summary>
        [Comment("是否含税。服务器不使用。")]
        public bool WithTax { get; set; }

        /// <summary>
        /// 回调地址。用于接收开票结果通知。
        /// </summary>
        public string CallbackUrl { get; set; }

        #region 需要添加的必要字段

        /// <summary>
        /// 开票日期。
        /// </summary>
        [Comment("开票日期")]
        [Column(TypeName = "DATETIME2(3)")]
        public DateTime? InvoiceDate { get; set; }

        /// <summary>
        /// 发票类型代码。对应诺诺返回的c_fpzl_dm。
        /// </summary>
        [Comment("发票类型代码")]
        [MaxLength(32)]
        public string InvoiceTypeCode { get; set; }

        /// <summary>
        /// PDF文件下载地址。
        /// </summary>
        [Comment("PDF文件下载地址")]
        [MaxLength(512)]
        public string PdfUrl { get; set; }

        /// <summary>
        /// 开票失败原因。
        /// </summary>
        [Comment("开票失败原因")]
        [MaxLength(512)]
        public string FailReason { get; set; }

        #endregion
    }

    /// <summary>
    /// 客户税务信息/开票信息细项。
    /// </summary>
    public class TaxInvoiceInfoItem : GuidKeyObjectBase
    {
        /// <summary>客户税务信息/开票信息Id，关联<see cref="TaxInvoiceInfo"/>。</summary>
        [Comment("客户税务信息/开票信息Id")]
        public Guid? ParentId { get; set; }

        #region 商品信息
        /// <summary>商品名称。必填。</summary>
        [Comment("商品名称。必填。")]
        [Required]
        public string GoodsName { get; set; }

        /// <summary>单位,可选</summary>
        [Comment("单位,可选")]
        public string Unit { get; set; }

        /// <summary>规格型号,可选</summary>
        [Comment("规格型号,可选")]
        public string SpecType { get; set; }
        #endregion 商品信息

        #region 金额信息
        /// <summary>数量。需要正确传入。</summary>
        [Comment("数量")]
        public decimal Quantity { get; set; }

        /// <summary>单价（不含税）。需要正确传入。</summary>
        [Comment("单价（不含税）")]
        [Precision(18, 2)]
        public decimal UnitPrice { get; set; }

        /// <summary>税率。需要正确传入。</summary>
        [Comment("税率")]
        [Precision(18, 2)]
        public decimal TaxRate { get; set; }

        /// <summary>
        /// 含税金额。计算公式：税额 = 单价 * 数量 * 税率。计算结果保留两位小数。这个不是计算字段，需要正确传入。
        /// </summary>
        [Comment("含税金额。计算公式：税额 = 单价 * 数量 * 税率。计算结果保留两位小数。")]
        [Precision(18, 2)]
        public decimal TaxInclusiveAmount { get; set; }
        #endregion
    }

    /// <summary>
    /// 发票金额计算触发器，用于在保存TaxInvoiceInfoItem后计算并更新TaxInvoiceInfo.TaxInclusiveAmount。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(IAfterDbContextSaving<TaxInvoiceInfoItem>))]
    public class TaxInvoiceAmountCalculator : IAfterDbContextSaving<TaxInvoiceInfoItem>
    {
        private readonly ILogger<TaxInvoiceAmountCalculator> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志记录器</param>
        public TaxInvoiceAmountCalculator(ILogger<TaxInvoiceAmountCalculator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 在保存TaxInvoiceInfoItem后执行，计算并更新关联的TaxInvoiceInfo.TaxInclusiveAmount。
        /// </summary>
        /// <param name="dbContext">当前数据库上下文</param>
        /// <param name="serviceProvider">服务提供者</param>
        /// <param name="states">状态字典</param>
        public void AfterSaving(DbContext dbContext, IServiceProvider serviceProvider, Dictionary<object, object> states)
        {
            try
            {
                // 获取所有已修改的TaxInvoiceInfoItem的ParentId
                var modifiedItems = dbContext.ChangeTracker.Entries<TaxInvoiceInfoItem>()
                    .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted)
                    .Select(e => e.Entity)
                    .Where(e => e.ParentId.HasValue)
                    .Select(e => e.ParentId.Value)
                    .Distinct()
                    .ToList();

                if (!modifiedItems.Any())
                {
                    _logger.LogDebug("没有发现需要更新金额的发票");
                    return;
                }

                // 为每个受影响的TaxInvoiceInfo重新计算TaxInclusiveAmount
                foreach (var parentId in modifiedItems)
                {
                    _logger.LogDebug("开始计算发票 {InvoiceId} 的含税总金额", parentId);

                    // 查找发票记录
                    var invoice = dbContext.Set<TaxInvoiceInfo>().Find(parentId);
                    if (invoice == null)
                    {
                        _logger.LogWarning("无法找到ID为 {InvoiceId} 的发票记录", parentId);
                        continue;
                    }

                    // 查询所有相关的明细项
                    var items = dbContext.Set<TaxInvoiceInfoItem>()
                        .Where(item => item.ParentId == parentId)
                        .ToList();

                    // 计算总金额
                    decimal totalAmount = items.Sum(item => item.TaxInclusiveAmount);

                    // 更新发票总金额
                    invoice.TaxInclusiveAmount = totalAmount;

                    _logger.LogDebug("已更新发票 {InvoiceId} 的含税总金额为 {Amount}", parentId, totalAmount);
                }

                // 无需保存更改
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "计算发票含税总金额时发生错误");
            }
        }
    }
}
