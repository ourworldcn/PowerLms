using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OW.Data;
using OW.EntityFrameworkCore;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Collections.Generic.OwEnumerableExtensions;

namespace PowerLms.Data
{
    /// <summary>
    /// 业务单的账单。
    /// </summary>
    public class DocBill : GuidKeyObjectBase, ICreatorInfo
    {
        /// <summary>
        /// 账单号。
        /// </summary>
        [Comment("账单号。")]
        public string BillNo { get; set; }

        /// <summary>
        /// 业务编号,默认为该业务的JobNo，可修改,不绑定业务表的id
        /// </summary>
        [Comment("业务编号,默认为该业务的JobNo，可修改,不绑定业务表的id。")]
        public string DocNo { get; set; }

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
        /// 付款人,选Id。
        /// </summary>
        [Comment("付款人。选Id")]
        public Guid? PayerId { get; set; }

        /// <summary>
        /// Inscribe	抬头
        /// </summary>
        public Guid? InscribeId { get; set; }

        /// <summary>
        /// 金额。冗余字段，所属费用的合计。关联到DocFee表的金额字段。
        /// </summary>
        [Comment("金额。冗余字段，所属费用的合计。")]
        [Precision(18, 2)]
        public decimal Amount { get; set; }

        /// <summary>
        /// 币种码。
        /// </summary>
        [Comment("币种码")]
        public string CurrTypeId { get; set; }

        /// <summary>
        /// 已核销金额
        /// </summary>
        [Comment("已核销金额")]
        [Precision(18, 4)]
        public decimal ClearAmount { get; set; }

        /// <summary>
        /// 审核日期，为空则未审核。
        /// </summary>
        [Comment("审核日期，为空则未审核"), Column(TypeName = "datetime2(2)")]
        public DateTime? CheckDate { get; set; }

        /// <summary>
        /// 审核人Id，为空则未审核。
        /// </summary>
        [Comment("审核人Id，为空则未审核")]
        public Guid? ChechManId { get; set; }

        /// <summary>
        /// 是否有效。
        /// </summary>
        [Comment("是否有效")]
        public bool IsEnable { get; set; }

        /// <summary>
        /// 备注。
        /// </summary>
        [Comment("备注")]
        public string Remark { get; set; }

        /// <summary>
        /// 进出口日期。
        /// </summary>
        [Comment("审核日期，为空则未审核"), Column(TypeName = "datetime2(2)")]
        public DateTime? IODate { get; set; }

        /// <summary>
        /// 船名或航班号
        /// </summary>
        [Comment("船名或航班号")]
        public string Vessel { get; set; }

        /// <summary>
        /// 航次
        /// </summary>
        [Comment("航次")]
        public string Voyage { get; set; }

        /// <summary>
        /// 主单号
        /// </summary>
        [Comment("主单号")]
        public string MblNo { get; set; }

        /// <summary>
        /// 分单号
        /// </summary>
        [Comment("分单号")]
        public string HblNo { get; set; }

        /// <summary>
        /// 起始港编码。
        /// </summary>
        [Comment("起始港编码")]
        [MaxLength(4), Unicode(false)]
        public string LoadingCode { get; set; }

        /// <summary>
        /// 中转港编码。
        /// </summary>
        [Comment("中转港编码")]
        [MaxLength(4), Unicode(false)]
        public string DischargeCode { get; set; }

        /// <summary>
        /// 起始港编码。
        /// </summary>
        [Comment("目的港编码")]
        [MaxLength(4), Unicode(false)]
        public string DestinationCode { get; set; }

        /// <summary>
        /// 开航日期。
        /// </summary>
        [Comment("开航日期。")]
        [Column(TypeName = "datetime2(2)")]
        public DateTime Etd { get; set; }

        /// <summary>
        /// 到港日期。
        /// </summary>
        [Comment("到港日期。")]
        [Column(TypeName = "datetime2(2)")]
        public DateTime Eta { get; set; }

        /// <summary>
        /// So编号。
        /// </summary>
        [Comment("So编号。")]
        [MaxLength(64)]
        public string SoNo { get; set; }

        /// <summary>
        /// 订舱编号。
        /// </summary>
        [Comment("订舱编号。")]
        [MaxLength(64)]
        public string BookingNo { get; set; }

        /// <summary>
        /// 货物名称。
        /// </summary>
        [Comment("货物名称。")]
        public string GoodsName { get; set; }

        /// <summary>
        /// 件数。
        /// </summary>
        [Comment("件数。")]
        public int PkgsCount { get; set; }

        /// <summary>
        /// 重量，3位小数。
        /// </summary>
        [Comment("结算计费重量，3位小数")]
        [Precision(18, 3)]
        public decimal Weight { get; set; }

        /// <summary>
        /// 计费重量，单位Kg，3位小数。
        /// </summary>
        [Comment("计费重量，单位Kg，3位小数。")]
        [Precision(18, 3)]
        public decimal ChargeWeight { get; set; }

        /// <summary>
        /// 包装类型Id。关联简单字典PackType。
        /// </summary>
        [Comment("包装类型Id。关联简单字典PackType。")]
        public Guid? PackTypeId { get; set; }

        /// <summary>
        /// 重量，3位小数。
        /// </summary>
        [Comment("体积，3位小数")]
        [Precision(18, 3)]
        public decimal MeasureMent { get; set; }

        /// <summary>
        /// 箱量。
        /// </summary>
        [Comment("箱量")]
        public string ContainerNum { get; set; }

        /// <summary>
        /// 发货人
        /// </summary>
        [Comment("发货人")]
        public string Consignor { get; set; }

        /// <summary>
        /// 收货人
        /// </summary>
        [Comment("收货人")]
        public string Consignee { get; set; }

        /// <summary>
        /// 承运人
        /// </summary>
        [Comment("承运人")]
        public string Carrier { get; set; }

        /// <summary>
        /// 客户联系人。客户联系人可以从客户资料中维护的联系人中选择，也可以临时输入，所以这里不是关联联系人的id，是字符串
        /// </summary>
        [MaxLength(32)]
        [Comment("客户联系人")]
        public string LinkMan { get; set; }

        /// <summary>
        /// 联系人电话。客户联系人可以从客户资料中维护的联系人中选择，也可以临时输入，所以这里不是关联联系人的id，是字符串
        /// </summary>
        [MaxLength(24), DataType("varchar(24)"), Phone]
        [Comment("联系人电话")]
        public string LinkTel { get; set; }

        /// <summary>
        /// 联系人传真。客户联系人可以从客户资料中维护的联系人中选择，也可以临时输入，所以这里不是关联联系人的id，是字符串
        /// </summary>
        [MaxLength(24), DataType("varchar(24)"), Phone]
        [Comment("联系人传真")]
        public string LinkFax { get; set; }

        /// <summary>
        /// 合同号。
        /// </summary>
        [Comment("合同号")]
        public string ContractNo { get; set; }
    }

    public static class DocBillExtensions
    {
        public static IQueryable<DocFee> GetFees(this DocBill bill, DbContext db)
        {
            return db.Set<DocFee>().Where(DocFee => DocFee.BillId == bill.Id);
        }
    }

    /// <summary>
    /// DocBill 触发器处理类，负责计算 DocBill.Amount 的值。
    /// 当 DocFee 或 DocBill 发生变更时，自动更新关联账单的金额。
    /// </summary>
    //[OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(IDbContextSaving<DocFee>))]
    //[OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(IDbContextSaving<DocBill>))]
    //[OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(IAfterDbContextSaving<DocFee>))]
    //[OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(IAfterDbContextSaving<DocBill>))]
    public class DocBillAmountCalculator : IDbContextSaving<DocFee>, IDbContextSaving<DocBill>,
                                         IAfterDbContextSaving<DocFee>, IAfterDbContextSaving<DocBill>
    {
        private readonly ILogger<DocBillAmountCalculator> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        public DocBillAmountCalculator(ILogger<DocBillAmountCalculator> logger) =>
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        /// <summary>
        /// 处理实体保存前事件，收集受影响账单的ID
        /// </summary>
        public void Saving(IEnumerable<EntityEntry> entities, IServiceProvider serviceProvider, Dictionary<object, object> states)
        {
            if (!entities.Any()) return;

            var dbContext = entities.First().Context;
            if (dbContext == null) return;

            // 收集受影响的账单ID
            var billIds = new HashSet<Guid>();

            // 处理 DocFee 类型的实体 - 包括新增、修改和删除
            entities.Where(e => e.Entity is DocFee)
                .ForEach(entry =>
                {
                    // 获取当前和原始的 DocFee 实体
                    var fee = entry.Entity as DocFee;

                    // 对于删除的费用，获取其原始 BillId 值
                    if (entry.State == EntityState.Deleted)
                    {
                        var originalBillId = entry.Property("BillId").OriginalValue as Guid?;
                        if (originalBillId.HasValue)
                            billIds.Add(originalBillId.Value);
                    }
                    // 对于新增或修改的费用
                    else if (fee?.BillId.HasValue ?? false)
                    {
                        billIds.Add(fee.BillId.Value);
                    }
                });

            // 处理 DocBill 类型的实体
            entities.Where(e => e.Entity is DocBill && e.State != EntityState.Deleted)
                .Select(e => e.Entity as DocBill)
                .Where(b => b != null)
                .ForEach(bill => billIds.Add(bill.Id));

            // 如果有需要更新的账单ID，记录到状态字典中
            if (billIds.Count > 0)
            {
                if (states.TryGetValue("DocBillIdsToUpdate", out var existingIds))
                    ((HashSet<Guid>)existingIds).UnionWith(billIds);
                else
                    states["DocBillIdsToUpdate"] = billIds;
            }
        }

        /// <summary>
        /// 保存后处理，计算并更新账单金额
        /// </summary>
        public void AfterSaving(DbContext dbContext, IServiceProvider serviceProvider, Dictionary<object, object> states)
        {
            // 检查是否有需要更新的账单ID
            if (!states.TryGetValue("DocBillIdsToUpdate", out var billIdsObj) ||
                billIdsObj is not HashSet<Guid> billIds ||
                !billIds.Any())
            {
                return;
            }

            try
            {
                // 加载并更新账单
                var bills = dbContext.Set<DocBill>()
                    .Where(bill => billIds.Contains(bill.Id))
                    .ToList();

                foreach (var bill in bills)
                {
                    // 获取关联的费用明细
                    var fees = bill.GetFees(dbContext).ToList();

                    // 计算金额
                    decimal amount = 0;
                    if (fees.Any())
                    {
                        var incomingAmount = fees.Where(f => f.IO).Sum(f => f.Amount);
                        var outgoingAmount = fees.Where(f => !f.IO).Sum(f => f.Amount);
                        amount = Math.Abs(incomingAmount - outgoingAmount);
                    }

                    // 只在金额有变化时更新
                    if (bill.Amount != amount)
                    {
                        bill.Amount = amount;
                        _logger.LogDebug($"更新账单 {bill.Id} 的金额为 {amount}");
                    }
                }

                // 保存更改
                dbContext.SaveChanges();

                // 清理使用过的状态
                states.Remove("DocBillIdsToUpdate");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "在保存后更新账单金额时出错");
            }
        }
    }
}