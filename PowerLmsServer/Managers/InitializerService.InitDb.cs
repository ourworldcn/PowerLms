/*
 * PowerLms - 货运物流业务管理系统
 * 系统初始化服务 - 数据库基础配置初始化
 * 
 * 功能说明：
 * - 税务发票通道配置初始化
 * - 财务科目配置的标准化管理
 * - 应收应付计提科目配置
 * - 基础业务数据的预设配置
 * 
 * 技术特点：
 * - 增量配置策略，避免重复初始化
 * - 统一的配置标识和编码规范
 * - 详细的业务含义注释说明
 * - 支持多租户和权限隔离
 * 
 * 作者：PowerLms开发团队
 * 创建时间：2024年
 * 最后修改：2024年12月 - 统一日志记录器命名规范
 */

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OW;
using PowerLms.Data;
using PowerLmsServer.EfData;

namespace PowerLmsServer.Managers
{
    /// <summary>
    /// 初始化服务的数据库配置部分
    /// </summary>
    public partial class InitializerService
    {
        #region 数据库基础配置初始化

        /// <summary>
        /// 初始化数据库基础配置数据
        /// </summary>
        /// <param name="services">服务提供者</param>
        private void InitDb(IServiceProvider services)
        {
            var dbContext = services.GetRequiredService<PowerLmsUserDbContext>();
            InitializeTaxInvoiceChannels(dbContext); // 初始化税务发票通道
            InitializeSubjectConfigurations(dbContext); // 初始化科目配置
        }

        /// <summary>
        /// 初始化税务发票通道配置
        /// </summary>
        /// <param name="dbContext">数据库上下文</param>
        private void InitializeTaxInvoiceChannels(PowerLmsUserDbContext dbContext)
        {
            var nuoNuoChannelId = typeof(NuoNuoManager).GUID;
            if (!dbContext.TaxInvoiceChannels.Any(c => c.Id == nuoNuoChannelId))
            {
                dbContext.TaxInvoiceChannels.Add(new TaxInvoiceChannel
                {
                    Id = nuoNuoChannelId,
                    DisplayName = "诺诺发票",
                    InvoiceChannel = nameof(NuoNuoManager),
                    InvoiceChannelParams = "{}",
                });
                _logger.LogInformation("添加诺诺发票通道配置");
            }
            var manualChannelId = typeof(ManualInvoicingManager).GUID;
            if (!dbContext.TaxInvoiceChannels.Any(c => c.Id == manualChannelId))
            {
                dbContext.TaxInvoiceChannels.Add(new TaxInvoiceChannel
                {
                    Id = manualChannelId,
                    DisplayName = "手工开票",
                    InvoiceChannel = nameof(ManualInvoicingManager),
                    InvoiceChannelParams = "{}",
                });
                _logger.LogInformation("添加手工开票通道配置");
            }
        }

        /// <summary>
        /// 初始化科目配置信息
        /// </summary>
        /// <param name="dbContext">数据库上下文</param>
        private void InitializeSubjectConfigurations(PowerLmsUserDbContext dbContext)
        {
            bool hasNewConfigurations = false;
            hasNewConfigurations |= AddPrimaryBusinessSubjects(dbContext); // 添加主营业务科目
            hasNewConfigurations |= AddMainTaskSubjects(dbContext); // 添加主任务科目
            hasNewConfigurations |= AddBasicSubjects(dbContext); // 添加基础科目
            hasNewConfigurations |= AddReceivableAccrualSubjects(dbContext); // 添加应收计提科目
            hasNewConfigurations |= AddPayableAccrualSubjects(dbContext); // 添加应付计提科目
            if (hasNewConfigurations)
            {
                dbContext.SaveChanges();
                _logger.LogInformation("科目配置初始化完成，已保存所有新增配置");
            }
            else
            {
                _logger.LogDebug("所有科目配置均已存在，跳过初始化");
            }
        }

        /// <summary>
        /// 添加主营业务科目配置
        /// </summary>
        /// <param name="dbContext">数据库上下文</param>
        /// <returns>是否有新增配置</returns>
        private bool AddPrimaryBusinessSubjects(PowerLmsUserDbContext dbContext)
        {
            bool hasNew = false;
            var salesRevenueId = Guid.Parse("{E8B5C4D7-3F1A-4C2E-8D9A-1B5E7F9C3A6D}");
            if (!dbContext.SubjectConfigurations.Any(c => c.Id == salesRevenueId))
            {
                dbContext.SubjectConfigurations.Add(new SubjectConfiguration
                {
                    Id = salesRevenueId,
                    Code = "PBI_SALES_REVENUE",
                    SubjectNumber = "6001",
                    DisplayName = "主营业务收入",
                    VoucherGroup = "转",
                    AccountingCategory = "客户",
                    Remark = "发票挂账使用的主营业务收入科目，用于记录开票产生的收入金额（价税合计减去税额）",
                    CreateBy = null,
                    CreateDateTime = OwHelper.WorldNow,
                    IsDelete = false
                });
                _logger.LogInformation("添加PBI主营业务收入科目配置");
                hasNew = true;
            }
            var taxPayableId = Guid.Parse("{F2A6D8E9-4B7C-5E3F-9A1B-2C6F8E0D4A7C}");
            if (!dbContext.SubjectConfigurations.Any(c => c.Id == taxPayableId))
            {
                dbContext.SubjectConfigurations.Add(new SubjectConfiguration
                {
                    Id = taxPayableId,
                    Code = "PBI_TAX_PAYABLE",
                    SubjectNumber = "2221",
                    DisplayName = "应交税金",
                    VoucherGroup = "转",
                    AccountingCategory = "客户",
                    Remark = "发票挂账使用的应交税金科目，用于记录开票产生的税额部分",
                    CreateBy = null,
                    CreateDateTime = OwHelper.WorldNow,
                    IsDelete = false
                });
                _logger.LogInformation("添加PBI应交税金科目配置");
                hasNew = true;
            }
            var accReceivableId = Guid.Parse("{A3B7E1F5-6C8D-7A2B-3E4F-9D1C5B8A7E6F}");
            if (!dbContext.SubjectConfigurations.Any(c => c.Id == accReceivableId))
            {
                dbContext.SubjectConfigurations.Add(new SubjectConfiguration
                {
                    Id = accReceivableId,
                    Code = "PBI_ACC_RECEIVABLE",
                    SubjectNumber = "1122",
                    DisplayName = "应收账款",
                    VoucherGroup = "转",
                    AccountingCategory = "客户",
                    Remark = "发票挂账使用的应收账款科目，用于记录开票产生的应收款项（价税合计）",
                    CreateBy = null,
                    CreateDateTime = OwHelper.WorldNow,
                    IsDelete = false
                });
                _logger.LogInformation("添加PBI应收账款科目配置");
                hasNew = true;
            }
            return hasNew;
        }

        /// <summary>
        /// 添加主任务/流程科目配置
        /// </summary>
        /// <param name="dbContext">数据库上下文</param>
        /// <returns>是否有新增配置</returns>
        private bool AddMainTaskSubjects(PowerLmsUserDbContext dbContext)
        {
            bool hasNew = false;
            if (!dbContext.SubjectConfigurations.Any(c => c.Code == "SETTLEMENT_RECEIPT"))
            {
                dbContext.SubjectConfigurations.Add(new SubjectConfiguration
                {
                    Id = Guid.Parse("{F5A6B7C8-9D0E-1F2A-3B4C-5D6E7F8A9B0C}"),
                    Code = "SETTLEMENT_RECEIPT",
                    SubjectNumber = "",
                    DisplayName = "实收",
                    VoucherGroup = "收",
                    AccountingCategory = "",
                    Preparer = "系统导出",
                    Remark = "实收结算单导出科目配置",
                    CreateBy = null,
                    CreateDateTime = OwHelper.WorldNow,
                    IsDelete = false
                });
                _logger.LogInformation("添加实收科目配置");
                hasNew = true;
            }
            if (!dbContext.SubjectConfigurations.Any(c => c.Code == "SETTLEMENT_PAYMENT"))
            {
                dbContext.SubjectConfigurations.Add(new SubjectConfiguration
                {
                    Id = Guid.Parse("{A6B7C8D9-0E1F-2A3B-4C5D-6E7F8A9B0C1D}"),
                    Code = "SETTLEMENT_PAYMENT",
                    SubjectNumber = "",
                    DisplayName = "实付",
                    VoucherGroup = "付",
                    AccountingCategory = "",
                    Preparer = "系统导出",
                    Remark = "实付结算单导出科目配置",
                    CreateBy = null,
                    CreateDateTime = OwHelper.WorldNow,
                    IsDelete = false
                });
                _logger.LogInformation("添加实付科目配置");
                hasNew = true;
            }
            if (!dbContext.SubjectConfigurations.Any(c => c.Code == "ACCRUAL_TAX_REVENUE"))
            {
                dbContext.SubjectConfigurations.Add(new SubjectConfiguration
                {
                    Id = Guid.Parse("{A8B9C0D1-2E3F-4A5B-6C7D-8E9F0A1B2C3D}"),
                    Code = "ACCRUAL_TAX_REVENUE",
                    SubjectNumber = "",
                    DisplayName = "计提税金及主营业务收入",
                    VoucherGroup = "转",
                    AccountingCategory = "",
                    Preparer = "系统导出",
                    Remark = "计提税金及主营业务收入流程配置",
                    CreateBy = null,
                    CreateDateTime = OwHelper.WorldNow,
                    IsDelete = false
                });
                _logger.LogInformation("添加计提税金及主营业务收入配置");
                hasNew = true;
            }
            return hasNew;
        }

        /// <summary>
        /// 添加基础科目配置
        /// </summary>
        /// <param name="dbContext">数据库上下文</param>
        /// <returns>是否有新增配置</returns>
        private bool AddBasicSubjects(PowerLmsUserDbContext dbContext)
        {
            bool hasNew = false;
            var basicSubjects = new[]
            {
                new { Id = Guid.Parse("{C2D3E4F5-6A7B-8C9D-0E1F-2A3B4C5D6E7F}"), Code = "AR_CODE", Number = "1131", Name = "应收账款" },
                new { Id = Guid.Parse("{B1C2D3E4-5F6A-7B8C-9D0E-1F2A3B4C5D6E}"), Code = "REVENUE_CODE", Number = "5101", Name = "主营业务收入" },
                new { Id = Guid.Parse("{D3E4F5A6-7B8C-9D0E-1F2A-3B4C5D6E7F8A}"), Code = "TAX_CODE", Number = "2171.01.05", Name = "应交税金" },
                new { Id = Guid.Parse("{E4F5A6B7-8C9D-0E1F-2A3B-4C5D6E7F8A9B}"), Code = "BANK_CODE", Number = "1002", Name = "银行存款" },
                new { Id = Guid.Parse("{F7A8B9C0-1D2E-3F4A-5B6C-7D8E9F0A1B2C}"), Code = "FPREPARE", Number = "", Name = "制单人" }
            };
            foreach (var subject in basicSubjects)
            {
                if (!dbContext.SubjectConfigurations.Any(c => c.Code == subject.Code))
                {
                    dbContext.SubjectConfigurations.Add(new SubjectConfiguration
                    {
                        Id = subject.Id,
                        Code = subject.Code,
                        SubjectNumber = subject.Number,
                        DisplayName = subject.Name,
                        VoucherGroup = "",
                        AccountingCategory = "",
                        Preparer = "系统导出",
                        Remark = $"基础科目-{subject.Name}",
                        CreateBy = null,
                        CreateDateTime = OwHelper.WorldNow,
                        IsDelete = false
                    });
                    _logger.LogInformation("添加基础{SubjectName}科目配置", subject.Name);
                    hasNew = true;
                }
            }
            return hasNew;
        }

        /// <summary>
        /// 添加A账应收计提科目配置
        /// </summary>
        /// <param name="dbContext">数据库上下文</param>
        /// <returns>是否有新增配置</returns>
        private bool AddReceivableAccrualSubjects(PowerLmsUserDbContext dbContext)
        {
            bool hasNew = false;
            var receivableSubjects = new[]
            {
                new { Id = Guid.Parse("{B7C8D9E0-1F2A-3B4C-5D6E-7F8A9B0C1D2E}"), Code = "ARAB_TOTAL", Number = "531", Name = "计提总应收", Category = "" },
                new { Id = Guid.Parse("{C8D9E0F1-2A3B-4C5D-6E7F-8A9B0C1D2E3F}"), Code = "ARAB_IN_CUS", Number = "113.001.01", Name = "计提应收国内-客户", Category = "客户" },
                new { Id = Guid.Parse("{D9E0F1A2-3B4C-5D6E-7F8A-9B0C1D2E3F4A}"), Code = "ARAB_IN_TAR", Number = "113.001.02", Name = "计提应收国内-关税", Category = "客户" },
                new { Id = Guid.Parse("{E0F1A2B3-4C5D-6E7F-8A9B-0C1D2E3F4A5B}"), Code = "ARAB_OUT_CUS", Number = "113.002", Name = "计提应收国外-客户", Category = "客户" },
                new { Id = Guid.Parse("{F1A2B3C4-5D6E-7F8A-9B0C-1D2E3F4A5B6C}"), Code = "ARAB_OUT_TAR", Number = "", Name = "计提应收国外-关税", Category = "客户" }
            };
            foreach (var subject in receivableSubjects)
            {
                if (!dbContext.SubjectConfigurations.Any(c => c.Code == subject.Code))
                {
                    dbContext.SubjectConfigurations.Add(new SubjectConfiguration
                    {
                        Id = subject.Id,
                        Code = subject.Code,
                        SubjectNumber = subject.Number,
                        DisplayName = subject.Name,
                        VoucherGroup = "转",
                        AccountingCategory = subject.Category,
                        Preparer = "系统导出",
                        Remark = $"A账应收计提-{subject.Name.Replace("计提", "")}科目",
                        CreateBy = null,
                        CreateDateTime = OwHelper.WorldNow,
                        IsDelete = false
                    });
                    _logger.LogInformation("添加ARAB{SubjectName}科目配置", subject.Name);
                    hasNew = true;
                }
            }
            return hasNew;
        }

        /// <summary>
        /// 添加A账应付计提科目配置
        /// </summary>
        /// <param name="dbContext">数据库上下文</param>
        /// <returns>是否有新增配置</returns>
        private bool AddPayableAccrualSubjects(PowerLmsUserDbContext dbContext)
        {
            bool hasNew = false;
            var payableSubjects = new[]
            {
                new { Id = Guid.Parse("{A2B3C4D5-6E7F-8A9B-0C1D-2E3F4A5B6C7D}"), Code = "APAB_TOTAL", Number = "532", Name = "计提总应付", Category = "" },
                new { Id = Guid.Parse("{B3C4D5E6-7F8A-9B0C-1D2E-3F4A5B6C7D8E}"), Code = "APAB_IN_SUP", Number = "203.001.01", Name = "计提应付国内-供应商", Category = "供应商" },
                new { Id = Guid.Parse("{C4D5E6F7-8A9B-0C1D-2E3F-4A5B6C7D8E9F}"), Code = "APAB_IN_TAR", Number = "203.001.02", Name = "计提应付国内-关税", Category = "供应商" },
                new { Id = Guid.Parse("{D5E6F7A8-9B0C-1D2E-3F4A-5B6C7D8E9F0A}"), Code = "APAB_OUT_SUP", Number = "203.002", Name = "计提应付国外-供应商", Category = "供应商" },
                new { Id = Guid.Parse("{E6F7A8B9-0C1D-2E3F-4A5B-6C7D8E9F0A1B}"), Code = "APAB_OUT_TAR", Number = "", Name = "计提应付国外-关税", Category = "供应商" }
            };
            foreach (var subject in payableSubjects)
            {
                if (!dbContext.SubjectConfigurations.Any(c => c.Code == subject.Code))
                {
                    dbContext.SubjectConfigurations.Add(new SubjectConfiguration
                    {
                        Id = subject.Id,
                        Code = subject.Code,
                        SubjectNumber = subject.Number,
                        DisplayName = subject.Name,
                        VoucherGroup = "转",
                        AccountingCategory = subject.Category,
                        Preparer = "系统导出",
                        Remark = $"A账应付计提-{subject.Name.Replace("计提", "")}科目",
                        CreateBy = null,
                        CreateDateTime = OwHelper.WorldNow,
                        IsDelete = false
                    });
                    _logger.LogInformation("添加APAB{SubjectName}科目配置", subject.Name);
                    hasNew = true;
                }
            }
            return hasNew;
        }

        #endregion
    }
}