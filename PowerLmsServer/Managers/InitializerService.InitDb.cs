using AutoMapper;
using DotNetDBF;
using Microsoft.Data.Sql;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Primitives;
using NPOI.SS.Formula.Functions;
using NPOI.SS.UserModel;
using OW;
using OW.Data;
using OW.EntityFrameworkCore;
using PowerLms.Data;
using PowerLmsServer.EfData;
using System;
using System.Data;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Net.Mail;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace PowerLmsServer.Managers
{
    /// <summary>
    /// 初始化服务。
    /// </summary>
    public partial class InitializerService : BackgroundService
    {
        /// <summary>
        /// 初始化所有数据库所需的数据。
        /// </summary>
        /// <param name="svc"></param>
        private void InitDb(IServiceProvider svc)
        {
            var db = svc.GetRequiredService<PowerLmsUserDbContext>();

            #region 税务发票通道初始数据
            // 检查诺诺发票通道是否已存在，如不存在则添加
            var nuoNuoChannelId = typeof(NuoNuoManager).GUID;
            if (!db.TaxInvoiceChannels.Any(c => c.Id == nuoNuoChannelId))
            {
                db.TaxInvoiceChannels.Add(new TaxInvoiceChannel
                {
                    Id = nuoNuoChannelId,
                    DisplayName = "诺诺发票",
                    InvoiceChannel = nameof(NuoNuoManager),
                    InvoiceChannelParams = "{}",
                });
                _Logger.LogInformation("添加诺诺发票通道配置");
            }
            // 检查手工开票通道是否已存在，如不存在则添加
            var manualChannelId = typeof(ManualInvoicingManager).GUID;
            if (!db.TaxInvoiceChannels.Any(c => c.Id == manualChannelId))
            {
                db.TaxInvoiceChannels.Add(new TaxInvoiceChannel
                {
                    Id = manualChannelId,
                    DisplayName = "手工开票",
                    InvoiceChannel = nameof(ManualInvoicingManager),
                    InvoiceChannelParams = "{}",
                });
                _Logger.LogInformation("添加手工开票通道配置");
            }
            #endregion 税务发票通道初始数据

            #region 初始化科目配置信息（严格按照设计文档）
            // 检查诺诺发票通道是否已存在，如不存在则添加
            nuoNuoChannelId = typeof(NuoNuoManager).GUID;
            if (!db.TaxInvoiceChannels.Any(c => c.Id == nuoNuoChannelId))
            {
                db.TaxInvoiceChannels.Add(new TaxInvoiceChannel
                {
                    Id = nuoNuoChannelId,
                    DisplayName = "诺诺发票",
                    InvoiceChannel = nameof(NuoNuoManager),
                    InvoiceChannelParams = "{}",
                });
                _Logger.LogInformation("添加诺诺发票通道配置");
            }

            // 检查手工开票通道是否已存在，如不存在则添加
            manualChannelId = typeof(ManualInvoicingManager).GUID;
            if (!db.TaxInvoiceChannels.Any(c => c.Id == manualChannelId))
            {
                db.TaxInvoiceChannels.Add(new TaxInvoiceChannel
                {
                    Id = manualChannelId,
                    DisplayName = "手工开票",
                    InvoiceChannel = nameof(ManualInvoicingManager),
                    InvoiceChannelParams = "{}",
                });
                _Logger.LogInformation("添加手工开票通道配置");
            }
            #endregion 税务发票通道初始数据

            #region 初始化科目配置信息
            // PBI_SALES_REVENUE - 主营业务收入科目配置
            var salesRevenueId = Guid.Parse("{E8B5C4D7-3F1A-4C2E-8D9A-1B5E7F9C3A6D}");
            if (!db.SubjectConfigurations.Any(c => c.Id == salesRevenueId))
            {
                db.SubjectConfigurations.Add(new SubjectConfiguration
                {
                    Id = salesRevenueId,
                    Code = "PBI_SALES_REVENUE",
                    SubjectNumber = "6001",
                    DisplayName = "主营业务收入",
                    VoucherGroup = "转", // 转账凭证
                    AccountingCategory = "客户", // 核算类别为客户
                    Remark = "发票挂账使用的主营业务收入科目，用于记录开票产生的收入金额（价税合计减去税额）",
                    CreateBy = null, // 系统初始化，无具体创建人
                    CreateDateTime = OwHelper.WorldNow,
                    IsDelete = false
                });
                _Logger.LogInformation("添加PBI主营业务收入科目配置");
            }
            else
            {
                _Logger.LogDebug("PBI主营业务收入科目配置已存在，跳过初始化");
            }

            // PBI_TAX_PAYABLE - 应交税金科目配置
            var taxPayableId = Guid.Parse("{F2A6D8E9-4B7C-5E3F-9A1B-2C6F8E0D4A7C}");
            if (!db.SubjectConfigurations.Any(c => c.Id == taxPayableId))
            {
                db.SubjectConfigurations.Add(new SubjectConfiguration
                {
                    Id = taxPayableId,
                    Code = "PBI_TAX_PAYABLE",
                    SubjectNumber = "2221",
                    DisplayName = "应交税金",
                    VoucherGroup = "转", // 转账凭证
                    AccountingCategory = "客户", // 核算类别为客户
                    Remark = "发票挂账使用的应交税金科目，用于记录开票产生的税额部分",
                    CreateBy = null, // 系统初始化，无具体创建人
                    CreateDateTime = OwHelper.WorldNow,
                    IsDelete = false
                });
                _Logger.LogInformation("添加PBI应交税金科目配置");
            }
            else
            {
                _Logger.LogDebug("PBI应交税金科目配置已存在，跳过初始化");
            }

            // PBI_ACC_RECEIVABLE - 应收账款科目配置
            var accReceivableId = Guid.Parse("{A3B7E1F5-6C8D-7A2B-3E4F-9D1C5B8A7E6F}");
            if (!db.SubjectConfigurations.Any(c => c.Id == accReceivableId))
            {
                db.SubjectConfigurations.Add(new SubjectConfiguration
                {
                    Id = accReceivableId,
                    Code = "PBI_ACC_RECEIVABLE",
                    SubjectNumber = "1122",
                    DisplayName = "应收账款",
                    VoucherGroup = "转", // 转账凭证
                    AccountingCategory = "客户", // 核算类别为客户
                    Remark = "发票挂账使用的应收账款科目，用于记录开票产生的应收款项（价税合计）",
                    CreateBy = null, // 系统初始化，无具体创建人
                    CreateDateTime = OwHelper.WorldNow,
                    IsDelete = false
                });
                _Logger.LogInformation("添加PBI应收账款科目配置");
            }
            else
            {
                _Logger.LogDebug("PBI应收账款科目配置已存在，跳过初始化");
            }

            bool hasNewConfigurations = false;

            #region 主任务/流程科目
            // SETTLEMENT_RECEIPT - 实收
            if (!db.SubjectConfigurations.Any(c => c.Code == "SETTLEMENT_RECEIPT"))
            {
                db.SubjectConfigurations.Add(new SubjectConfiguration
                {
                    Id = Guid.Parse("{F5A6B7C8-9D0E-1F2A-3B4C-5D6E7F8A9B0C}"),
                    Code = "SETTLEMENT_RECEIPT",
                    SubjectNumber = "", // 待定
                    DisplayName = "实收",
                    VoucherGroup = "收",
                    AccountingCategory = "",
                    Preparer = "系统导出",
                    Remark = "实收结算单导出科目配置",
                    CreateBy = null,
                    CreateDateTime = OwHelper.WorldNow,
                    IsDelete = false
                });
                _Logger.LogInformation("添加实收科目配置");
                hasNewConfigurations = true;
            }
            // SETTLEMENT_PAYMENT - 实付
            if (!db.SubjectConfigurations.Any(c => c.Code == "SETTLEMENT_PAYMENT"))
            {
                db.SubjectConfigurations.Add(new SubjectConfiguration
                {
                    Id = Guid.Parse("{A6B7C8D9-0E1F-2A3B-4C5D-6E7F8A9B0C1D}"),
                    Code = "SETTLEMENT_PAYMENT",
                    SubjectNumber = "", // 待定
                    DisplayName = "实付",
                    VoucherGroup = "付",
                    AccountingCategory = "",
                    Preparer = "系统导出",
                    Remark = "实付结算单导出科目配置",
                    CreateBy = null,
                    CreateDateTime = OwHelper.WorldNow,
                    IsDelete = false
                });
                _Logger.LogInformation("添加实付科目配置");
                hasNewConfigurations = true;
            }
            // ACCRUAL_TAX_REVENUE - 计提税金及主营业务收入
            if (!db.SubjectConfigurations.Any(c => c.Code == "ACCRUAL_TAX_REVENUE"))
            {
                db.SubjectConfigurations.Add(new SubjectConfiguration
                {
                    Id = Guid.Parse("{A8B9C0D1-2E3F-4A5B-6C7D-8E9F0A1B2C3D}"),
                    Code = "ACCRUAL_TAX_REVENUE",
                    SubjectNumber = "", // 待定
                    DisplayName = "计提税金及主营业务收入",
                    VoucherGroup = "转",
                    AccountingCategory = "",
                    Preparer = "系统导出",
                    Remark = "计提税金及主营业务收入流程配置",
                    CreateBy = null,
                    CreateDateTime = OwHelper.WorldNow,
                    IsDelete = false
                });
                _Logger.LogInformation("添加计提税金及主营业务收入配置");
                hasNewConfigurations = true;
            }
            #endregion

            #region 基础科目
            // AR_CODE - 应收账款
            if (!db.SubjectConfigurations.Any(c => c.Code == "AR_CODE"))
            {
                db.SubjectConfigurations.Add(new SubjectConfiguration
                {
                    Id = Guid.Parse("{C2D3E4F5-6A7B-8C9D-0E1F-2A3B4C5D6E7F}"),
                    Code = "AR_CODE",
                    SubjectNumber = "1131",
                    DisplayName = "应收账款",
                    VoucherGroup = "",
                    AccountingCategory = "",
                    Preparer = "系统导出",
                    Remark = "基础科目-应收账款",
                    CreateBy = null,
                    CreateDateTime = OwHelper.WorldNow,
                    IsDelete = false
                });
                _Logger.LogInformation("添加基础应收账款科目配置");
                hasNewConfigurations = true;
            }
            // REVENUE_CODE - 主营业务收入
            if (!db.SubjectConfigurations.Any(c => c.Code == "REVENUE_CODE"))
            {
                db.SubjectConfigurations.Add(new SubjectConfiguration
                {
                    Id = Guid.Parse("{B1C2D3E4-5F6A-7B8C-9D0E-1F2A3B4C5D6E}"),
                    Code = "REVENUE_CODE",
                    SubjectNumber = "5101",
                    DisplayName = "主营业务收入",
                    VoucherGroup = "",
                    AccountingCategory = "",
                    Preparer = "系统导出",
                    Remark = "基础科目-主营业务收入",
                    CreateBy = null,
                    CreateDateTime = OwHelper.WorldNow,
                    IsDelete = false
                });
                _Logger.LogInformation("添加基础主营业务收入科目配置");
                hasNewConfigurations = true;
            }
            // TAX_CODE - 应交税金
            if (!db.SubjectConfigurations.Any(c => c.Code == "TAX_CODE"))
            {
                db.SubjectConfigurations.Add(new SubjectConfiguration
                {
                    Id = Guid.Parse("{D3E4F5A6-7B8C-9D0E-1F2A-3B4C5D6E7F8A}"),
                    Code = "TAX_CODE",
                    SubjectNumber = "2171.01.05",
                    DisplayName = "应交税金",
                    VoucherGroup = "",
                    AccountingCategory = "",
                    Preparer = "系统导出",
                    Remark = "基础科目-应交税金",
                    CreateBy = null,
                    CreateDateTime = OwHelper.WorldNow,
                    IsDelete = false
                });
                _Logger.LogInformation("添加基础应交税金科目配置");
                hasNewConfigurations = true;
            }
            // BANK_CODE - 银行存款
            if (!db.SubjectConfigurations.Any(c => c.Code == "BANK_CODE"))
            {
                db.SubjectConfigurations.Add(new SubjectConfiguration
                {
                    Id = Guid.Parse("{E4F5A6B7-8C9D-0E1F-2A3B-4C5D6E7F8A9B}"),
                    Code = "BANK_CODE",
                    SubjectNumber = "1002",
                    DisplayName = "银行存款",
                    VoucherGroup = "",
                    AccountingCategory = "",
                    Preparer = "系统导出",
                    Remark = "基础科目-银行存款",
                    CreateBy = null,
                    CreateDateTime = OwHelper.WorldNow,
                    IsDelete = false
                });
                _Logger.LogInformation("添加基础银行存款科目配置");
                hasNewConfigurations = true;
            }
            // FPREPARE - 制单人
            if (!db.SubjectConfigurations.Any(c => c.Code == "FPREPARE"))
            {
                db.SubjectConfigurations.Add(new SubjectConfiguration
                {
                    Id = Guid.Parse("{F7A8B9C0-1D2E-3F4A-5B6C-7D8E9F0A1B2C}"),
                    Code = "FPREPARE",
                    SubjectNumber = "", // 该项无科目
                    DisplayName = "制单人",
                    VoucherGroup = "",
                    AccountingCategory = "",
                    Preparer = "系统导出",
                    Remark = "制单人配置",
                    CreateBy = null,
                    CreateDateTime = OwHelper.WorldNow,
                    IsDelete = false
                });
                _Logger.LogInformation("添加制单人配置");
                hasNewConfigurations = true;
            }
            #endregion

            #region A账应收计提科目
            // ARAB_TOTAL - 计提总应收
            if (!db.SubjectConfigurations.Any(c => c.Code == "ARAB_TOTAL"))
            {
                db.SubjectConfigurations.Add(new SubjectConfiguration
                {
                    Id = Guid.Parse("{B7C8D9E0-1F2A-3B4C-5D6E-7F8A9B0C1D2E}"),
                    Code = "ARAB_TOTAL",
                    SubjectNumber = "531",
                    DisplayName = "计提总应收",
                    VoucherGroup = "转",
                    AccountingCategory = "",
                    Preparer = "系统导出",
                    Remark = "A账应收计提-总应收科目",
                    CreateBy = null,
                    CreateDateTime = OwHelper.WorldNow,
                    IsDelete = false
                });
                _Logger.LogInformation("添加ARAB计提总应收科目配置");
                hasNewConfigurations = true;
            }
            // ARAB_IN_CUS - 计提应收国内-客户
            if (!db.SubjectConfigurations.Any(c => c.Code == "ARAB_IN_CUS"))
            {
                db.SubjectConfigurations.Add(new SubjectConfiguration
                {
                    Id = Guid.Parse("{C8D9E0F1-2A3B-4C5D-6E7F-8A9B0C1D2E3F}"),
                    Code = "ARAB_IN_CUS",
                    SubjectNumber = "113.001.01",
                    DisplayName = "计提应收国内-客户",
                    VoucherGroup = "转",
                    AccountingCategory = "客户",
                    Preparer = "系统导出",
                    Remark = "A账应收计提-国内客户科目",
                    CreateBy = null,
                    CreateDateTime = OwHelper.WorldNow,
                    IsDelete = false
                });
                _Logger.LogInformation("添加ARAB计提应收国内-客户科目配置");
                hasNewConfigurations = true;
            }
            // ARAB_IN_TAR - 计提应收国内-关税
            if (!db.SubjectConfigurations.Any(c => c.Code == "ARAB_IN_TAR"))
            {
                db.SubjectConfigurations.Add(new SubjectConfiguration
                {
                    Id = Guid.Parse("{D9E0F1A2-3B4C-5D6E-7F8A-9B0C1D2E3F4A}"),
                    Code = "ARAB_IN_TAR",
                    SubjectNumber = "113.001.02",
                    DisplayName = "计提应收国内-关税",
                    VoucherGroup = "转",
                    AccountingCategory = "客户",
                    Preparer = "系统导出",
                    Remark = "A账应收计提-国内关税科目",
                    CreateBy = null,
                    CreateDateTime = OwHelper.WorldNow,
                    IsDelete = false
                });
                _Logger.LogInformation("添加ARAB计提应收国内-关税科目配置");
                hasNewConfigurations = true;
            }
            // ARAB_OUT_CUS - 计提应收国外-客户
            if (!db.SubjectConfigurations.Any(c => c.Code == "ARAB_OUT_CUS"))
            {
                db.SubjectConfigurations.Add(new SubjectConfiguration
                {
                    Id = Guid.Parse("{E0F1A2B3-4C5D-6E7F-8A9B-0C1D2E3F4A5B}"),
                    Code = "ARAB_OUT_CUS",
                    SubjectNumber = "113.002",
                    DisplayName = "计提应收国外-客户",
                    VoucherGroup = "转",
                    AccountingCategory = "客户",
                    Preparer = "系统导出",
                    Remark = "A账应收计提-国外客户科目",
                    CreateBy = null,
                    CreateDateTime = OwHelper.WorldNow,
                    IsDelete = false
                });
                _Logger.LogInformation("添加ARAB计提应收国外-客户科目配置");
                hasNewConfigurations = true;
            }
            // ARAB_OUT_TAR - 计提应收国外-关税
            if (!db.SubjectConfigurations.Any(c => c.Code == "ARAB_OUT_TAR"))
            {
                db.SubjectConfigurations.Add(new SubjectConfiguration
                {
                    Id = Guid.Parse("{F1A2B3C4-5D6E-7F8A-9B0C-1D2E3F4A5B6C}"),
                    Code = "ARAB_OUT_TAR",
                    SubjectNumber = "", // 待补充
                    DisplayName = "计提应收国外-关税",
                    VoucherGroup = "转",
                    AccountingCategory = "客户",
                    Preparer = "系统导出",
                    Remark = "A账应收计提-国外关税科目（待补充科目号）",
                    CreateBy = null,
                    CreateDateTime = OwHelper.WorldNow,
                    IsDelete = false
                });
                _Logger.LogInformation("添加ARAB计提应收国外-关税科目配置");
                hasNewConfigurations = true;
            }
            #endregion

            #region A账应付计提科目
            // APAB_TOTAL - 计提总应付
            if (!db.SubjectConfigurations.Any(c => c.Code == "APAB_TOTAL"))
            {
                db.SubjectConfigurations.Add(new SubjectConfiguration
                {
                    Id = Guid.Parse("{A2B3C4D5-6E7F-8A9B-0C1D-2E3F4A5B6C7D}"),
                    Code = "APAB_TOTAL",
                    SubjectNumber = "532",
                    DisplayName = "计提总应付",
                    VoucherGroup = "转",
                    AccountingCategory = "",
                    Preparer = "系统导出",
                    Remark = "A账应付计提-总应付科目",
                    CreateBy = null,
                    CreateDateTime = OwHelper.WorldNow,
                    IsDelete = false
                });
                _Logger.LogInformation("添加APAB计提总应付科目配置");
                hasNewConfigurations = true;
            }
            // APAB_IN_SUP - 计提应付国内-供应商
            if (!db.SubjectConfigurations.Any(c => c.Code == "APAB_IN_SUP"))
            {
                db.SubjectConfigurations.Add(new SubjectConfiguration
                {
                    Id = Guid.Parse("{B3C4D5E6-7F8A-9B0C-1D2E-3F4A5B6C7D8E}"),
                    Code = "APAB_IN_SUP",
                    SubjectNumber = "203.001.01",
                    DisplayName = "计提应付国内-供应商",
                    VoucherGroup = "转",
                    AccountingCategory = "供应商",
                    Preparer = "系统导出",
                    Remark = "A账应付计提-国内供应商科目",
                    CreateBy = null,
                    CreateDateTime = OwHelper.WorldNow,
                    IsDelete = false
                });
                _Logger.LogInformation("添加APAB计提应付国内-供应商科目配置");
                hasNewConfigurations = true;
            }
            // APAB_IN_TAR - 计提应付国内-关税
            if (!db.SubjectConfigurations.Any(c => c.Code == "APAB_IN_TAR"))
            {
                db.SubjectConfigurations.Add(new SubjectConfiguration
                {
                    Id = Guid.Parse("{C4D5E6F7-8A9B-0C1D-2E3F-4A5B6C7D8E9F}"),
                    Code = "APAB_IN_TAR",
                    SubjectNumber = "203.001.02",
                    DisplayName = "计提应付国内-关税",
                    VoucherGroup = "转",
                    AccountingCategory = "供应商",
                    Preparer = "系统导出",
                    Remark = "A账应付计提-国内关税科目",
                    CreateBy = null,
                    CreateDateTime = OwHelper.WorldNow,
                    IsDelete = false
                });
                _Logger.LogInformation("添加APAB计提应付国内-关税科目配置");
                hasNewConfigurations = true;
            }
            // APAB_OUT_SUP - 计提应付国外-供应商
            if (!db.SubjectConfigurations.Any(c => c.Code == "APAB_OUT_SUP"))
            {
                db.SubjectConfigurations.Add(new SubjectConfiguration
                {
                    Id = Guid.Parse("{D5E6F7A8-9B0C-1D2E-3F4A-5B6C7D8E9F0A}"),
                    Code = "APAB_OUT_SUP",
                    SubjectNumber = "203.002",
                    DisplayName = "计提应付国外-供应商",
                    VoucherGroup = "转",
                    AccountingCategory = "供应商",
                    Preparer = "系统导出",
                    Remark = "A账应付计提-国外供应商科目",
                    CreateBy = null,
                    CreateDateTime = OwHelper.WorldNow,
                    IsDelete = false
                });
                _Logger.LogInformation("添加APAB计提应付国外-供应商科目配置");
                hasNewConfigurations = true;
            }
            // APAB_OUT_TAR - 计提应付国外-关税
            if (!db.SubjectConfigurations.Any(c => c.Code == "APAB_OUT_TAR"))
            {
                db.SubjectConfigurations.Add(new SubjectConfiguration
                {
                    Id = Guid.Parse("{E6F7A8B9-0C1D-2E3F-4A5B-6C7D8E9F0A1B}"),
                    Code = "APAB_OUT_TAR",
                    SubjectNumber = "", // 待补充
                    DisplayName = "计提应付国外-关税",
                    VoucherGroup = "转",
                    AccountingCategory = "供应商",
                    Preparer = "系统导出",
                    Remark = "A账应付计提-国外关税科目（待补充科目号）",
                    CreateBy = null,
                    CreateDateTime = OwHelper.WorldNow,
                    IsDelete = false
                });
                _Logger.LogInformation("添加APAB计提应付国外-关税科目配置");
                hasNewConfigurations = true;
            }
            #endregion

            // 如果有新增配置，则保存更改
            if (hasNewConfigurations)
            {
                db.SaveChanges();
                _Logger.LogInformation("科目配置初始化完成，已保存所有新增配置");
            }
            else
            {
                _Logger.LogDebug("所有科目配置均已存在，跳过初始化");
            }
            #endregion 初始化科目配置信息
        }
    }
}