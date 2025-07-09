using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PowerLms.Data;
using PowerLms.Data.Finance;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Dto;
using System.Text.Json;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// 财务系统接口导出功能控制器。
    /// 提供发票数据导出为金蝶DBF格式文件的功能。
    /// 严格按照SubjectConfiguration表中的科目配置进行凭证生成。
    /// </summary>
    public class FinancialSystemExportController : PlControllerBase
    {
        /// <summary>
        /// 构造函数，初始化财务系统导出控制器。
        /// </summary>
        /// <param name="accountManager">账户管理器，提供用户身份验证和令牌管理功能</param>
        /// <param name="serviceProvider">服务提供者，用于当前请求作用域内的服务解析</param>
        /// <param name="serviceScopeFactory">服务作用域工厂，用于创建独立的服务作用域</param>
        /// <param name="dbContext">PowerLMS用户数据库上下文，提供对数据库的直接访问能力</param>
        /// <param name="logger">日志记录器，用于记录控制器运行过程中的各种信息</param>
        /// <param name="fileService">文件服务，提供文件系统操作的统一接口</param>
        /// <param name="merchantManager">商户管理器，用于商户相关操作</param>
        /// <param name="organizationManager">机构管理器，用于机构相关操作</param>
        public FinancialSystemExportController(AccountManager accountManager, IServiceProvider serviceProvider, IServiceScopeFactory serviceScopeFactory,
            PowerLmsUserDbContext dbContext, ILogger<FinancialSystemExportController> logger, OwFileService<PowerLmsUserDbContext> fileService,
            MerchantManager merchantManager, OrganizationManager organizationManager)
        {
            _AccountManager = accountManager;
            _ServiceProvider = serviceProvider;
            _ServiceScopeFactory = serviceScopeFactory;
            _DbContext = dbContext;
            _Logger = logger;
            _FileService = fileService;
            _MerchantManager = merchantManager;
            _OrganizationManager = organizationManager;
        }

        private readonly AccountManager _AccountManager;
        private readonly IServiceProvider _ServiceProvider; // 保留，用于当前请求作用域的服务解析
        private readonly IServiceScopeFactory _ServiceScopeFactory; // 用于后台任务的独立作用域创建
        private readonly PowerLmsUserDbContext _DbContext;
        private readonly ILogger<FinancialSystemExportController> _Logger;
        private readonly OwFileService<PowerLmsUserDbContext> _FileService;
        private readonly MerchantManager _MerchantManager;
        private readonly OrganizationManager _OrganizationManager;

        #region HTTP接口

        /// <summary>
        /// 导出发票数据为金蝶DBF格式文件。
        /// 使用改进后的OwTaskService统一任务调度机制。
        /// </summary>
        /// <param name="model">导出参数，包含查询条件和用户令牌</param>
        /// <returns>导出任务信息，包含任务ID用于跟踪进度</returns>
        [HttpPost]
        public ActionResult<ExportInvoiceToDbfReturnDto> ExportInvoiceToDbf(ExportInvoiceToDbfParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();

            var result = new ExportInvoiceToDbfReturnDto();
            try
            {
                // 预检查科目配置是否完整
                var missingConfigs = ValidateSubjectConfiguration(context.User.OrgId);
                if (missingConfigs.Any())
                {
                    result.HasError = true;
                    result.ErrorCode = 500;
                    result.DebugMessage = $"科目配置不完整，缺少以下配置：{string.Join(", ", missingConfigs)}";
                    return result;
                }

                // 预检查发票数量
                var invoicesQuery = _DbContext.TaxInvoiceInfos.Where(c => c.ExportedDateTime == null).AsQueryable();
                if (model.ExportConditions != null && model.ExportConditions.Any())
                {
                    invoicesQuery = EfHelper.GenerateWhereAnd(invoicesQuery, model.ExportConditions);
                }
                invoicesQuery = ApplyOrganizationFilter(invoicesQuery, context.User);

                var invoiceCount = invoicesQuery.Count();
                if (invoiceCount == 0)
                {
                    result.HasError = true;
                    result.ErrorCode = 404;
                    result.DebugMessage = "没有找到符合条件的发票数据，请调整查询条件";
                    return result;
                }

                // 使用OwFileService创建任务（同步方式）
                var taskService = _ServiceProvider.GetRequiredService<OwTaskService<PowerLmsUserDbContext>>();
                var exportDateTime = DateTime.UtcNow;
                
                var taskParameters = new Dictionary<string, string>
                {
                    ["ExportConditions"] = JsonSerializer.Serialize(model.ExportConditions),
                    ["UserId"] = context.User.Id.ToString(),
                    ["OrgId"] = context.User.OrgId?.ToString() ?? "",
                    ["ExpectedCount"] = invoiceCount.ToString(),
                    ["ExportDateTime"] = exportDateTime.ToString("O")
                };

                var taskId = taskService.CreateTask(typeof(FinancialSystemExportController),
                    nameof(ProcessInvoiceDbfExportTask), 
                    taskParameters, 
                    context.User.Id, 
                    context.User.OrgId);

                result.TaskId = taskId;
                result.DebugMessage = $"导出任务已创建，预计处理 {invoiceCount} 张发票。可通过系统任务状态查询接口跟踪进度。";
                result.ExpectedInvoiceCount = invoiceCount;
            }
            catch (Exception ex)
            {
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = ex.Message;
            }
            return result;
        }

        #endregion

        #region 静态任务处理方法

        /// <summary>
        /// 处理发票DBF导出任务（静态方法，由OwTaskService调用）
        /// 保持方法名不变以确保向后兼容性
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <param name="parameters">任务参数</param>
        /// <param name="serviceProvider">服务提供者（由OwTaskService自动注入）</param>
        /// <returns>任务执行结果</returns>
        public static object ProcessInvoiceDbfExportTask(Guid taskId, Dictionary<string, string> parameters, IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
                throw new InvalidOperationException("服务提供者不能为空");

            var dbContextFactory = serviceProvider.GetRequiredService<IDbContextFactory<PowerLmsUserDbContext>>();
            var fileService = serviceProvider.GetRequiredService<OwFileService<PowerLmsUserDbContext>>();

            // 解析任务参数
            if (!parameters.TryGetValue("ExportConditions", out var exportConditionsJson) ||
                !parameters.TryGetValue("UserId", out var userIdStr) ||
                !parameters.TryGetValue("ExportDateTime", out var exportDateTimeStr))
            {
                throw new InvalidOperationException($"任务参数不完整，缺少必要参数。任务ID: {taskId}");
            }

            var conditions = string.IsNullOrEmpty(exportConditionsJson) ? 
                new Dictionary<string, string>() : 
                JsonSerializer.Deserialize<Dictionary<string, string>>(exportConditionsJson);
            
            var userId = Guid.Parse(userIdStr);
            var orgId = parameters.TryGetValue("OrgId", out var orgIdStr) && !string.IsNullOrEmpty(orgIdStr) ? 
                Guid.Parse(orgIdStr) : (Guid?)null;
            var expectedCount = int.Parse(parameters.GetValueOrDefault("ExpectedCount", "0"));
            var exportDateTime = DateTime.Parse(exportDateTimeStr);

            using var dbContext = dbContextFactory.CreateDbContext();

            // 加载科目配置
            var subjectConfigs = LoadSubjectConfigurations(dbContext, orgId);
            if (!subjectConfigs.Any())
            {
                throw new InvalidOperationException($"科目配置未找到，无法生成凭证。组织ID: {orgId}，任务ID: {taskId}");
            }

            // 查询发票数据
            var invoicesQuery = dbContext.TaxInvoiceInfos.Where(i => i.ExportedDateTime == null).AsQueryable();
            if (conditions != null && conditions.Any())
            {
                invoicesQuery = EfHelper.GenerateWhereAnd(invoicesQuery, conditions);
            }

            // 应用权限过滤
            var taskUser = dbContext.Accounts.Find(userId);
            if (taskUser != null)
            {
                invoicesQuery = ApplyOrganizationFilterStatic(invoicesQuery, taskUser, dbContext, serviceProvider);
            }

            var invoices = invoicesQuery.ToList();
            if (!invoices.Any())
            {
                throw new InvalidOperationException($"没有找到符合条件的发票数据。任务ID: {taskId}，预期数量: {expectedCount}，实际数量: 0");
            }

            // 查询发票明细数据
            var invoiceIds = invoices.Select(i => i.Id).ToList();
            var invoiceItems = dbContext.TaxInvoiceInfoItems
                .Where(item => invoiceIds.Contains(item.ParentId.Value))
                .ToList();

            var invoiceItemsDict = invoiceItems.GroupBy(item => item.ParentId.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            // 转换为金蝶凭证格式
            var kingdeeVouchers = ConvertInvoicesToKingdeeVouchersWithConfig(invoices, invoiceItemsDict, subjectConfigs);
            if (!kingdeeVouchers.Any())
            {
                throw new InvalidOperationException($"生成的金蝶凭证记录为空。任务ID: {taskId}，发票数量: {invoices.Count}");
            }

            // 优化的DBF文件生成和保存流程
            var fileName = $"Invoice_Export_{DateTime.Now:yyyyMMdd_HHmmss}.dbf";
            
            // 使用MemoryStream优化大文件写入效率
            using var memoryStream = new MemoryStream();
            
            // 写入DBF内容到内存流
            DotNetDbfUtil.WriteToStream(kingdeeVouchers, memoryStream);
            
            // 关键优化：重置流指针到开始位置，提高后续读取效率
            memoryStream.Position = 0;
            
            // 更新发票导出时间
            foreach (var invoice in invoices)
            {
                invoice.ExportedDateTime = exportDateTime;
            }

            // 使用优化后的OwFileService直接从流创建文件记录
            var fileInfo = fileService.CreateFile(
                fileStream: memoryStream,
                fileName: fileName,
                displayName: $"发票导出-{DateTime.Now:yyyy年MM月dd日}",
                parentId: taskId,
                creatorId: userId,
                remark: $"发票DBF导出文件，共{invoices.Count}张发票，{kingdeeVouchers.Count}条会计分录，导出时间：{exportDateTime:yyyy-MM-dd HH:mm:ss}",
                subDirectory: "FinancialExports",
                skipValidation: true // DBF文件特殊，跳过文件大小和扩展名验证
            );

            // 注意：OwFileService.CreateFile 已经自动保存到数据库，无需手动再次保存

            // 返回任务执行结果
            return new
            {
                FileId = fileInfo.Id,
                FileName = fileName,
                InvoiceCount = invoices.Count,
                VoucherCount = kingdeeVouchers.Count,
                FilePath = fileInfo.FilePath,
                ExportDateTime = exportDateTime.ToString("yyyy-MM-dd HH:mm:ss")
            };
        }

        #endregion

        #region 静态辅助方法

        /// <summary>
        /// 加载科目配置（静态版本）
        /// </summary>
        private static Dictionary<string, SubjectConfiguration> LoadSubjectConfigurations(PowerLmsUserDbContext dbContext, Guid? orgId)
        {
            var requiredCodes = new List<string>
            {
                "PBI_ACC_RECEIVABLE",   // 应收账款
                "PBI_SALES_REVENUE",    // 主营业务收入
                "PBI_TAX_PAYABLE",      // 应交税金
                "GEN_PREPARER",         // 制单人
                "GEN_VOUCHER_GROUP"     // 凭证类别字
            };

            var configs = dbContext.SubjectConfigurations
                .Where(c => !c.IsDelete && c.OrgId == orgId && requiredCodes.Contains(c.Code))
                .ToList();

            return configs.ToDictionary(c => c.Code, c => c);
        }

        /// <summary>
        /// 将发票数据转换为金蝶凭证格式（静态版本）
        /// </summary>
        private static List<KingdeeVoucher> ConvertInvoicesToKingdeeVouchersWithConfig(
            List<TaxInvoiceInfo> invoices,
            Dictionary<Guid, List<TaxInvoiceInfoItem>> invoiceItemsDict,
            Dictionary<string, SubjectConfiguration> subjectConfigs)
        {
            if (invoices == null)
                throw new ArgumentNullException(nameof(invoices));
            if (subjectConfigs == null)
                throw new ArgumentNullException(nameof(subjectConfigs));
            
            invoiceItemsDict ??= new Dictionary<Guid, List<TaxInvoiceInfoItem>>();

            var vouchers = new List<KingdeeVoucher>();
            var voucherNumber = 1;

            // 获取通用配置信息
            var preparerName = subjectConfigs.ContainsKey("GEN_PREPARER") ?
                (subjectConfigs["GEN_PREPARER"]?.DisplayName ?? "系统导出") : "系统导出";

            var voucherGroup = subjectConfigs.ContainsKey("GEN_VOUCHER_GROUP") ?
                (subjectConfigs["GEN_VOUCHER_GROUP"]?.VoucherGroup ?? "转") : "转"; // 默认为转账凭证

            foreach (var invoice in invoices)
            {
                if (invoice == null) continue; // 跳过空发票
                
                try
                {
                    var items = invoiceItemsDict.ContainsKey(invoice.Id) ?
                        invoiceItemsDict[invoice.Id] : new List<TaxInvoiceInfoItem>();

                    var taxRate = items.FirstOrDefault()?.TaxRate ?? 0.06m;
                    var netAmount = invoice.TaxInclusiveAmount / (1 + taxRate);
                    var taxAmount = invoice.TaxInclusiveAmount - netAmount;
                    var invoiceDate = invoice.InvoiceDate ?? DateTime.Now;
                    var description = $"{invoice.BuyerTitle ?? "未知客户"}*{invoice.InvoiceItemName ?? "未知项目"}*{invoice.BuyerTaxNum ?? ""}";
                    var customerCode = string.IsNullOrEmpty(invoice.BuyerTaxNum) ? 
                        "CUSTOMER" : 
                        invoice.BuyerTaxNum.Substring(0, Math.Min(10, invoice.BuyerTaxNum.Length));

                    // 借方：应收账款（PBI_ACC_RECEIVABLE）
                    if (subjectConfigs.TryGetValue("PBI_ACC_RECEIVABLE", out var accReceivableConfig) && accReceivableConfig != null)
                    {
                        vouchers.Add(new KingdeeVoucher
                        {
                            Id = Guid.NewGuid(),
                            FDATE = invoiceDate,
                            FTRANSDATE = invoiceDate,
                            FPERIOD = invoiceDate.Month,
                            FGROUP = voucherGroup,
                            FNUM = voucherNumber,
                            FENTRYID = 0,
                            FEXP = description,
                            FACCTID = accReceivableConfig.SubjectNumber ?? "122101",
                            FCLSNAME1 = accReceivableConfig.AccountingCategory ?? "客户",
                            FOBJID1 = customerCode,
                            FOBJNAME1 = invoice.BuyerTitle ?? "未知客户",
                            FTRANSID = invoice.BuyerTaxNum ?? "",
                            FCYID = "RMB",
                            FEXCHRATE = 1.0000000m,
                            FDC = 0, // 借方
                            FFCYAMT = invoice.TaxInclusiveAmount,
                            FDEBIT = invoice.TaxInclusiveAmount,
                            FCREDIT = 0,
                            FPREPARE = preparerName
                        });
                    }

                    // 贷方：主营业务收入（PBI_SALES_REVENUE）
                    if (subjectConfigs.TryGetValue("PBI_SALES_REVENUE", out var salesRevenueConfig) && salesRevenueConfig != null)
                    {
                        vouchers.Add(new KingdeeVoucher
                        {
                            Id = Guid.NewGuid(),
                            FDATE = invoiceDate,
                            FTRANSDATE = invoiceDate,
                            FPERIOD = invoiceDate.Month,
                            FGROUP = voucherGroup,
                            FNUM = voucherNumber,
                            FENTRYID = 1,
                            FEXP = description,
                            FACCTID = salesRevenueConfig.SubjectNumber ?? "601001",
                            FCLSNAME1 = salesRevenueConfig.AccountingCategory ?? "客户",
                            FOBJID1 = customerCode,
                            FOBJNAME1 = invoice.BuyerTitle ?? "未知客户",
                            FTRANSID = invoice.BuyerTaxNum ?? "",
                            FCYID = "RMB",
                            FEXCHRATE = 1.0000000m,
                            FDC = 1, // 贷方
                            FFCYAMT = netAmount,
                            FDEBIT = 0,
                            FCREDIT = netAmount,
                            FPREPARE = preparerName
                        });
                    }

                    // 贷方：应交税金（PBI_TAX_PAYABLE）
                    if (taxAmount > 0 && subjectConfigs.TryGetValue("PBI_TAX_PAYABLE", out var taxPayableConfig) && taxPayableConfig != null)
                    {
                        vouchers.Add(new KingdeeVoucher
                        {
                            Id = Guid.NewGuid(),
                            FDATE = invoiceDate,
                            FTRANSDATE = invoiceDate,
                            FPERIOD = invoiceDate.Month,
                            FGROUP = voucherGroup,
                            FNUM = voucherNumber,
                            FENTRYID = 2,
                            FEXP = description,
                            FACCTID = taxPayableConfig.SubjectNumber ?? "221001",
                            FCLSNAME1 = taxPayableConfig.AccountingCategory ?? "客户",
                            FOBJID1 = customerCode,
                            FOBJNAME1 = invoice.BuyerTitle ?? "未知客户",
                            FTRANSID = invoice.BuyerTaxNum ?? "",
                            FCYID = "RMB",
                            FEXCHRATE = 1.0000000m,
                            FDC = 1, // 贷方
                            FFCYAMT = taxAmount,
                            FDEBIT = 0,
                            FCREDIT = taxAmount,
                            FPREPARE = preparerName
                        });
                    }
                    voucherNumber++;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"处理发票 {invoice.Id} 时发生错误: {ex.Message}", ex);
                }
            }
            
            if (vouchers.Count == 0)
            {
                throw new InvalidOperationException("未能生成任何金蝶凭证记录，请检查科目配置或发票数据");
            }
            
            return vouchers;
        }

        /// <summary>
        /// 静态版本的组织权限过滤方法
        /// </summary>
        private static IQueryable<TaxInvoiceInfo> ApplyOrganizationFilterStatic(IQueryable<TaxInvoiceInfo> invoicesQuery, Account user, 
            PowerLmsUserDbContext dbContext, IServiceProvider serviceProvider)
        {
            if (user == null)
            {
                return invoicesQuery.Where(i => false);
            }

            if (user.IsSuperAdmin)
            {
                return invoicesQuery;
            }

            var merchantManager = serviceProvider.GetRequiredService<MerchantManager>();
            var organizationManager = serviceProvider.GetRequiredService<OrganizationManager>();

            // 获取用户关联的所有组织机构ID
            var userOrgIds = dbContext.AccountPlOrganizations
                .Where(apo => apo.UserId == user.Id)
                .Select(apo => apo.OrgId)
                .ToList();

            if (!userOrgIds.Any())
            {
                return invoicesQuery.Where(i => false);
            }

            // 获取用户关联的所有商户ID
            var merchantIds = dbContext.PlOrganizations
                .Where(o => userOrgIds.Contains(o.Id) && o.MerchantId.HasValue)
                .Select(o => o.MerchantId.Value)
                .Distinct()
                .ToList();

            if (!merchantIds.Any())
            {
                var directMerchantIds = dbContext.Merchants
                    .Where(m => userOrgIds.Contains(m.Id))
                    .Select(m => m.Id)
                    .ToList();
                
                if (directMerchantIds.Any())
                {
                    merchantIds.AddRange(directMerchantIds);
                }
            }

            if (!merchantIds.Any())
            {
                return invoicesQuery.Where(i => false);
            }

            HashSet<Guid?> allowedOrgIds;

            if (user.IsMerchantAdmin)
            {
                allowedOrgIds = new HashSet<Guid?>();
                
                foreach (var merchantId in merchantIds)
                {
                    allowedOrgIds.Add(merchantId);
                    
                    var merchantOrgIds = dbContext.PlOrganizations
                        .Where(o => o.MerchantId == merchantId)
                        .Select(o => (Guid?)o.Id)
                        .ToList();
                    
                    foreach (var orgId in merchantOrgIds)
                    {
                        allowedOrgIds.Add(orgId);
                    }
                }
            }
            else
            {
                allowedOrgIds = userOrgIds.Select(id => (Guid?)id).ToHashSet();
                
                foreach (var merchantId in merchantIds)
                {
                    allowedOrgIds.Add(merchantId);
                }
            }

            var filteredQuery = from invoice in invoicesQuery
                               join requisition in dbContext.DocFeeRequisitions 
                                   on invoice.DocFeeRequisitionId equals requisition.Id into reqGroup
                               from req in reqGroup.DefaultIfEmpty()
                               join reqItem in dbContext.DocFeeRequisitionItems
                                   on req.Id equals reqItem.ParentId into reqItemGroup
                               from reqItemDetail in reqItemGroup.DefaultIfEmpty()
                               join fee in dbContext.DocFees 
                                   on reqItemDetail.FeeId equals fee.Id into feeGroup
                               from docFee in feeGroup.DefaultIfEmpty()
                               join job in dbContext.PlJobs 
                                   on docFee.JobId equals job.Id into jobGroup
                               from plJob in jobGroup.DefaultIfEmpty()
                               where allowedOrgIds.Contains(req.OrgId) ||
                                     allowedOrgIds.Contains(plJob.OrgId)
                               select invoice;

            return filteredQuery.Distinct();
        }

        #endregion

        #region 实例方法

        /// <summary>
        /// 验证科目配置是否完整。
        /// 检查发票挂账(PBI)流程所需的所有科目是否已配置。
        /// </summary>
        /// <param name="orgId">组织ID，用于查询对应组织的科目配置</param>
        /// <returns>缺失的科目配置代码列表</returns>
        private List<string> ValidateSubjectConfiguration(Guid? orgId)
        {
            var requiredCodes = new List<string>
            {
                "PBI_ACC_RECEIVABLE",   // 应收账款
                "PBI_SALES_REVENUE",    // 主营业务收入
                "PBI_TAX_PAYABLE",      // 应交税金
            };

            var existingCodes = _DbContext.SubjectConfigurations
                .Where(c => !c.IsDelete && c.OrgId == orgId && requiredCodes.Contains(c.Code))
                .Select(c => c.Code)
                .ToList();

            return requiredCodes.Except(existingCodes).ToList();
        }

        /// <summary>
        /// 根据用户权限过滤发票查询，确保用户只能导出有权限访问的发票。
        /// 超级管理员可以导出所有发票，商户管理员可以导出本商户的所有发票，
        /// 普通用户只能导出与其登录机构相关的发票。
        /// </summary>
        /// <param name="invoicesQuery">发票查询对象</param>
        /// <param name="user">当前用户</param>
        /// <param name="dbContext">数据库上下文（可选，用于后台任务）</param>
        /// <returns>过滤后的发票查询对象</returns>
        private IQueryable<TaxInvoiceInfo> ApplyOrganizationFilter(IQueryable<TaxInvoiceInfo> invoicesQuery, Account user, PowerLmsUserDbContext dbContext = null)
        {
            var context = dbContext ?? _DbContext;
            
            if (user.IsSuperAdmin)
            {
                return invoicesQuery;
            }

            if (!_MerchantManager.GetIdByUserId(user.Id, out var userMerchantId) || !userMerchantId.HasValue)
            {
                return invoicesQuery.Where(i => false);
            }

            if (user.IsMerchantAdmin)
            {
                var merchantOrgs = _OrganizationManager.GetOrLoadByMerchantId(userMerchantId.Value);
                var merchantOrgIds = merchantOrgs.Keys.Select(id => (Guid?)id).ToHashSet();
                merchantOrgIds.Add(userMerchantId.Value);
                
                var merchantFilteredQuery = from invoice in invoicesQuery
                                           join requisition in context.DocFeeRequisitions 
                                               on invoice.DocFeeRequisitionId equals requisition.Id into reqGroup
                                           from req in reqGroup.DefaultIfEmpty()
                                           join reqItem in context.DocFeeRequisitionItems
                                               on req.Id equals reqItem.ParentId into reqItemGroup
                                           from reqItemDetail in reqItemGroup.DefaultIfEmpty()
                                           join fee in context.DocFees 
                                               on reqItemDetail.FeeId equals fee.Id into feeGroup
                                           from docFee in feeGroup.DefaultIfEmpty()
                                           join job in context.PlJobs 
                                               on docFee.JobId equals job.Id into jobGroup
                                           from plJob in jobGroup.DefaultIfEmpty()
                                           where merchantOrgIds.Contains(req.OrgId) ||
                                                 merchantOrgIds.Contains(plJob.OrgId)
                                           select invoice;
                
                return merchantFilteredQuery.Distinct();
            }
            else
            {
                var userOrganizations = _OrganizationManager.GetOrLoadCurrentOrgsByUser(user);
                var userOrgIds = userOrganizations.Keys.Select(id => (Guid?)id).ToHashSet();
                userOrgIds.Add(userMerchantId.Value);
                
                if (!userOrgIds.Any())
                {
                    return invoicesQuery.Where(i => false);
                }

                var userFilteredQuery = from invoice in invoicesQuery
                                       join requisition in context.DocFeeRequisitions 
                                           on invoice.DocFeeRequisitionId equals requisition.Id into reqGroup
                                       from req in reqGroup.DefaultIfEmpty()
                                       join reqItem in context.DocFeeRequisitionItems
                                           on req.Id equals reqItem.ParentId into reqItemGroup
                                       from reqItemDetail in reqItemGroup.DefaultIfEmpty()
                                       join fee in context.DocFees 
                                           on reqItemDetail.FeeId equals fee.Id into feeGroup
                                       from docFee in feeGroup.DefaultIfEmpty()
                                       join job in context.PlJobs 
                                           on docFee.JobId equals job.Id into jobGroup
                                       from plJob in jobGroup.DefaultIfEmpty()
                                       where userOrgIds.Contains(req.OrgId) ||
                                             userOrgIds.Contains(plJob.OrgId)
                                       select invoice;
                
                return userFilteredQuery.Distinct();
            }
        }

        #endregion
    }

    #region DTO定义

    /// <summary>
    /// 发票导出为DBF文件的请求参数。
    /// </summary>
    public class ExportInvoiceToDbfParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 导出条件字典，键为字段名，值为条件值。
        /// 用于筛选要导出的发票数据，支持EfHelper.GenerateWhereAnd的所有查询操作。
        /// </summary>
        public Dictionary<string, string> ExportConditions { get; set; } = new();
    }

    /// <summary>
    /// 发票导出为DBF文件的返回结果。
    /// </summary>
    public class ExportInvoiceToDbfReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 导出任务的唯一标识ID。
        /// 可通过系统的通用任务状态查询接口跟踪进度和获取生成的文件。
        /// </summary>
        public Guid TaskId { get; set; }

        /// <summary>
        /// 任务创建成功的提示消息。
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 预计要处理的发票数量。
        /// </summary>
        public int ExpectedInvoiceCount { get; set; }
    }

    #endregion
}
