using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PowerLms.Data;
using PowerLms.Data.Finance;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Dto;
using System.Text.Json;
using OW.Data;
using DotNetDBF;
using SysIO = System.IO;
namespace PowerLmsWebApi.Controllers.Financial
{
    /// <summary>
    /// 财务系统导出控制器 - ARAB(计提A账应收)部分
    /// </summary>
    public partial class FinancialSystemExportController
    {
        #region HTTP接口 - ARAB(计提A账应收)
        /// <summary>
        /// 计提A账应收抵位币汇差(ARAB)导出为金蝶DBF格式文件。
        /// </summary>
        [HttpPost]
        public ActionResult<ExportArabToDbfReturnDto> ExportArabToDbf(ExportArabToDbfParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();
            var result = new ExportArabToDbfReturnDto();
            try
            {
                // 从ExportConditions中解析条件
                var conditions = model.ExportConditions ?? new Dictionary<string, string>();
                // 设置默认日期范围
                var startDate = conditions.TryGetValue("StartDate", out var startDateStr) && DateTime.TryParse(startDateStr, out var parsedStartDate) 
                    ? parsedStartDate : new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                var endDate = conditions.TryGetValue("EndDate", out var endDateStr) && DateTime.TryParse(endDateStr, out var parsedEndDate) 
                    ? parsedEndDate.Date.AddDays(1).AddTicks(-1) : DateTime.Now.Date.AddDays(1).AddTicks(-1);
                var accountingDate = conditions.TryGetValue("AccountingDate", out var accountingDateStr) && DateTime.TryParse(accountingDateStr, out var parsedAccountingDate) 
                    ? parsedAccountingDate : DateTime.Now.Date;
                // 预检查数据数量 - 使用Manager方法过滤未导出数据
                var exportManager = _ServiceProvider.GetRequiredService<FinancialSystemExportManager>();
                var baseFeesQuery = from fee in _DbContext.DocFees
                                   join job in _DbContext.PlJobs on fee.JobId equals job.Id
                                   where fee.IO == true && // 只统计收入
                                         job.AccountDate >= startDate && 
                                         job.AccountDate <= endDate &&
                                         job.JobState == 16 // 工作号已关闭状态
                                   select fee;
                var feesQuery = exportManager.FilterUnexported(baseFeesQuery);
                var feeCount = feesQuery.Count();
                if (feeCount == 0)
                {
                    result.HasError = true;
                    result.ErrorCode = 404;
                    result.DebugMessage = "没有找到符合条件的费用数据，请检查查询条件";
                    return result;
                }
                // 创建任务
                var taskService = _ServiceProvider.GetRequiredService<OwTaskService<PowerLmsUserDbContext>>();
                var taskParameters = new Dictionary<string, string>
                {
                    ["ExportConditions"] = JsonSerializer.Serialize(conditions),
                    ["StartDate"] = startDate.ToString("O"),
                    ["EndDate"] = endDate.ToString("O"),
                    ["AccountingDate"] = accountingDate.ToString("O"),
                    ["UserId"] = context.User.Id.ToString(),
                    ["OrgId"] = context.User.OrgId?.ToString() ?? "",
                    ["DisplayName"] = model.DisplayName ?? "",
                    ["Remark"] = model.Remark ?? ""
                };
                var taskId = taskService.CreateTask(typeof(FinancialSystemExportController),
                    nameof(ProcessArabDbfExportTask),
                    taskParameters,
                    context.User.Id,
                    context.User.OrgId);
                result.TaskId = taskId;
                result.Message = "ARAB导出任务已创建";
                result.ExpectedFeeCount = feeCount;
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
        #region 静态任务处理方法 - ARAB
        /// <summary>
        /// ARAB分组数据项
        /// </summary>
        public class ArabGroupDataItem
        {
            /// <summary>
            /// 结算单位ID
            /// </summary>
            public Guid? BalanceId { get; set; }
            /// <summary>
            /// 客户名称
            /// </summary>
            public string CustomerName { get; set; }
            /// <summary>
            /// 客户简称
            /// </summary>
            public string CustomerShortName { get; set; }
            /// <summary>
            /// 客户财务编码
            /// </summary>
            public string CustomerFinanceCode { get; set; }
            /// <summary>
            /// 是否国内客户
            /// </summary>
            public bool IsDomestic { get; set; }
            /// <summary>
            /// 是否代垫费用
            /// </summary>
            public bool IsAdvance { get; set; }
            /// <summary>
            /// 总金额（本位币）
            /// </summary>
            public decimal TotalAmount { get; set; }
        }
        /// <summary>
        /// 处理ARAB DBF导出任务
        /// </summary>
        public static object ProcessArabDbfExportTask(Guid taskId, Dictionary<string, string> parameters, IServiceProvider serviceProvider)
        {
            string currentStep = "参数验证";
            try
            {
                if (serviceProvider == null)
                    throw new ArgumentNullException(nameof(serviceProvider), "服务提供者不能为空");
                if (parameters == null)
                    throw new ArgumentNullException(nameof(parameters), "任务参数不能为空");
                currentStep = "初始化服务";
                var dbContextFactory = serviceProvider.GetService<IDbContextFactory<PowerLmsUserDbContext>>() ??
                    throw new InvalidOperationException("无法获取数据库上下文工厂");
                var fileService = serviceProvider.GetService<OwFileService<PowerLmsUserDbContext>>() ??
                    throw new InvalidOperationException("无法获取文件服务");
                currentStep = "解析任务参数";
                if (!parameters.TryGetValue("StartDate", out var startDateStr) || !DateTime.TryParse(startDateStr, out var startDate))
                    throw new InvalidOperationException("缺少或无效的开始日期参数");
                if (!parameters.TryGetValue("EndDate", out var endDateStr) || !DateTime.TryParse(endDateStr, out var endDate))
                    throw new InvalidOperationException("缺少或无效的结束日期参数");
                if (!parameters.TryGetValue("AccountingDate", out var accountingDateStr) || !DateTime.TryParse(accountingDateStr, out var accountingDate))
                    throw new InvalidOperationException("缺少或无效的记账日期参数");
                if (!parameters.TryGetValue("UserId", out var userIdStr) || !Guid.TryParse(userIdStr, out var userId))
                    throw new InvalidOperationException("缺少或无效的用户ID参数");
                Guid? orgId = null;
                if (parameters.TryGetValue("OrgId", out var orgIdStr) && !string.IsNullOrEmpty(orgIdStr))
                {
                    if (!Guid.TryParse(orgIdStr, out var parsedOrgId))
                        throw new InvalidOperationException($"无效的组织ID格式: {orgIdStr}");
                    orgId = parsedOrgId;
                }
                var displayName = parameters.GetValueOrDefault("DisplayName", "");
                var remark = parameters.GetValueOrDefault("Remark", "");
                // 解析导出条件
                var exportConditionsJson = parameters.GetValueOrDefault("ExportConditions", "{}");
                Dictionary<string, string> conditions = null;
                if (!string.IsNullOrEmpty(exportConditionsJson))
                {
                    try
                    {
                        conditions = JsonSerializer.Deserialize<Dictionary<string, string>>(exportConditionsJson);
                    }
                    catch (JsonException ex)
                    {
                        throw new InvalidOperationException($"导出条件JSON格式错误: {ex.Message}");
                    }
                }
                conditions ??= new Dictionary<string, string>();
                currentStep = "创建数据库上下文";
                using var dbContext = dbContextFactory.CreateDbContext();
                currentStep = "加载科目配置";
                var subjectConfigs = LoadArabSubjectConfigurations(dbContext, orgId);
                if (!subjectConfigs.Any())
                    throw new InvalidOperationException($"ARAB科目配置未找到，无法生成凭证，组织ID: {orgId}");
                currentStep = "查询费用数据";
                var exportManager = serviceProvider.GetRequiredService<FinancialSystemExportManager>();
                // 构建基础查询
                var baseFeesQuery = from fee in dbContext.DocFees
                                   join job in dbContext.PlJobs on fee.JobId equals job.Id
                                   where fee.IO == true && // 只统计收入
                                         job.AccountDate >= startDate && 
                                         job.AccountDate <= endDate &&
                                         job.JobState == 16 // 工作号已关闭状态
                                   select fee;
                // 过滤未导出数据
                var feesQuery = exportManager.FilterUnexported(baseFeesQuery);
                // 应用额外的查询条件
                if (conditions != null && conditions.Any())
                {
                    feesQuery = EfHelper.GenerateWhereAnd(feesQuery, conditions);
                }
                // 应用组织权限过滤
                var taskUser = dbContext.Accounts?.FirstOrDefault(a => a.Id == userId);
                if (taskUser != null)
                {
                    feesQuery = ApplyOrganizationFilterForFeesStatic(feesQuery, taskUser, dbContext, serviceProvider);
                }
                currentStep = "业务数据聚合统计";
                // 保存最终查询用于后续标记
                var finalFeesQuery = feesQuery;
                // ARAB业务逻辑：IO=收入，sum(Amount*ExchangeRate) as Totalamount，按 费用.结算单位、结算单位.国别、费用种类.代垫 分组
                var arabGroupData = (from fee in finalFeesQuery
                                   join customer in dbContext.PlCustomers on fee.BalanceId equals customer.Id into customerGroup
                                   from cust in customerGroup.DefaultIfEmpty()
                                   join feeType in dbContext.DD_SimpleDataDics on fee.FeeTypeId equals feeType.Id into feeTypeGroup
                                   from feeTypeDict in feeTypeGroup.DefaultIfEmpty()
                                   group new { fee, cust, feeTypeDict } by new
                                   {
                                       BalanceId = fee.BalanceId,
                                       CustomerName = cust != null ? cust.Name_DisplayName : "未知客户",
                                       CustomerShortName = cust != null ? cust.Name_ShortName : "",
                                       CustomerFinanceCode = cust != null ? cust.TacCountNo : "",
                                       IsDomestic = cust != null ? (cust.IsDomestic ?? true) : true,
                                       IsAdvance = feeTypeDict != null && feeTypeDict.Remark != null && feeTypeDict.Remark.Contains("代垫")
                                   } into g
                                   select new ArabGroupDataItem
                                   {
                                       BalanceId = g.Key.BalanceId,
                                       CustomerName = g.Key.CustomerName,
                                       CustomerShortName = g.Key.CustomerShortName,
                                       CustomerFinanceCode = g.Key.CustomerFinanceCode,
                                       IsDomestic = g.Key.IsDomestic,
                                       IsAdvance = g.Key.IsAdvance,
                                       TotalAmount = g.Sum(x => x.fee.Amount * x.fee.ExchangeRate)
                                   }).ToList();
                if (!arabGroupData.Any())
                    throw new InvalidOperationException("没有找到符合条件的费用数据");
                currentStep = "生成金蝶凭证数据";
                var kingdeeVouchers = GenerateArabKingdeeVouchers(arabGroupData, accountingDate, subjectConfigs);
                currentStep = "生成DBF文件";
                var fileName = $"ARAB_Export_{DateTime.Now:yyyyMMdd_HHmmss}.dbf";
                var kingdeeFieldMappings = new Dictionary<string, string>
                {
                    {"FDATE", "FDATE"}, {"FTRANSDATE", "FTRANSDATE"}, {"FPERIOD", "FPERIOD"}, {"FGROUP", "FGROUP"}, {"FNUM", "FNUM"},
                    {"FENTRYID", "FENTRYID"}, {"FEXP", "FEXP"}, {"FACCTID", "FACCTID"}, {"FCLSNAME1", "FCLSNAME1"}, {"FOBJID1", "FOBJID1"},
                    {"FOBJNAME1", "FOBJNAME1"}, {"FTRANSID", "FTRANSID"}, {"FCYID", "FCYID"}, {"FEXCHRATE", "FEXCHRATE"}, {"FDC", "FDC"},
                    {"FFCYAMT", "FFCYAMT"}, {"FDEBIT", "FDEBIT"}, {"FCREDIT", "FCREDIT"}, {"FPREPARE", "FPREPARE"}, {"FMODULE", "FMODULE"}, {"FDELETED", "FDELETED"}
                };
                var customFieldTypes = new Dictionary<string, NativeDbType>
                {
                    {"FDATE", NativeDbType.Date}, {"FTRANSDATE", NativeDbType.Date}, {"FPERIOD", NativeDbType.Numeric}, {"FGROUP", NativeDbType.Char},
                    {"FNUM", NativeDbType.Numeric}, {"FENTRYID", NativeDbType.Numeric}, {"FEXP", NativeDbType.Char}, {"FACCTID", NativeDbType.Char},
                    {"FCLSNAME1", NativeDbType.Char}, {"FOBJID1", NativeDbType.Char}, {"FOBJNAME1", NativeDbType.Char}, {"FTRANSID", NativeDbType.Char},
                    {"FCYID", NativeDbType.Char}, {"FEXCHRATE", NativeDbType.Numeric}, {"FDC", NativeDbType.Numeric}, {"FFCYAMT", NativeDbType.Numeric},
                    {"FDEBIT", NativeDbType.Numeric}, {"FCREDIT", NativeDbType.Numeric}, {"FPREPARE", NativeDbType.Char}, {"FMODULE", NativeDbType.Char}, {"FDELETED", NativeDbType.Logical}
                };
                currentStep = "保存文件记录";
                PlFileInfo fileInfoRecord;
                long fileSize;
                var memoryStream = new MemoryStream(1024 * 1024 * 1024);
                try
                {
                    DotNetDbfUtil.WriteToStream(kingdeeVouchers, memoryStream, kingdeeFieldMappings, customFieldTypes);
                    fileSize = memoryStream.Length;
                    if (fileSize == 0)
                        throw new InvalidOperationException("DBF文件生成失败，文件为空");
                    memoryStream.Position = 0;
                    var finalDisplayName = !string.IsNullOrWhiteSpace(displayName) ? 
                        displayName : $"ARAB财务导出-{DateTime.Now:yyyy年MM月dd日}";
                    var finalRemark = !string.IsNullOrWhiteSpace(remark) ? 
                        remark : $"ARAB计提DBF导出文件，包含{arabGroupData.Count}个客户分组，{kingdeeVouchers.Count}条分录记录，生成时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                    fileInfoRecord = fileService.CreateFile(
                        fileStream: memoryStream,
                        fileName: fileName,
                        displayName: finalDisplayName,
                        parentId: taskId,
                        creatorId: userId,
                        remark: finalRemark,
                        subDirectory: "FinancialExports",
                        skipValidation: true
                    );
                }
                finally
                {
                    OwHelper.DisposeAndRelease(ref memoryStream);
                }
                if (fileInfoRecord == null)
                    throw new InvalidOperationException("fileService.CreateFile 返回 null");
                currentStep = "标记费用为已导出ARAB";
                var fees = finalFeesQuery.ToList(); // 获取所有匹配的费用对象
                var markedCount = exportManager.MarkAsExported(fees, userId);
                dbContext.SaveChanges(); // 保存导出标记
                currentStep = "验证输出文件并返回结果";
                long actualFileSize = 0;
                bool fileExists = false;
                try
                {
                    if (SysIO.File.Exists(fileInfoRecord.FilePath))
                    {
                        actualFileSize = new SysIO.FileInfo(fileInfoRecord.FilePath).Length;
                        fileExists = true;
                    }
                }
                catch { } // 忽略验证时的异常，不影响主要功能
                return new
                {
                    FileId = fileInfoRecord.Id,
                    FileName = fileName,
                    FeeGroupCount = arabGroupData.Count,
                    VoucherCount = kingdeeVouchers.Count,
                    TotalAmount = arabGroupData.Sum(g => g.TotalAmount),
                    FilePath = fileInfoRecord.FilePath,
                    ExportDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    FileSize = actualFileSize,
                    FileExists = fileExists,
                    OriginalFileSize = fileSize,
                    MarkedFeeCount = markedCount
                };
            }
            catch (Exception ex)
            {
                var contextualError = $"ARAB DBF导出任务失败，当前步骤: {currentStep}, 任务ID: {taskId}";
                if (parameters != null)
                    contextualError += $"\n任务参数: {string.Join(", ", parameters.Select(kv => $"{kv.Key}={kv.Value}"))}";
                throw new InvalidOperationException(contextualError, ex);
            }
        }
        /// <summary>
        /// 加载ARAB科目配置（静态版本）
        /// </summary>
        private static Dictionary<string, SubjectConfiguration> LoadArabSubjectConfigurations(PowerLmsUserDbContext dbContext, Guid? orgId)
        {
            var requiredCodes = new List<string>
            {
                "ARAB_TOTAL",      // 计提总应收
                "ARAB_IN_CUS",     // 计提应收国内-客户
                "ARAB_IN_TAR",     // 计提应收国内-关税
                "ARAB_OUT_CUS",    // 计提应收国外-客户
                "ARAB_OUT_TAR",    // 计提应收国外-关税
                "GEN_PREPARER",    // 制单人
                "GEN_VOUCHER_GROUP" // 凭证类别字
            };
            var configs = dbContext.SubjectConfigurations
                .Where(c => !c.IsDelete && c.OrgId == orgId && requiredCodes.Contains(c.Code))
                .ToList();
            return configs.ToDictionary(c => c.Code, c => c);
        }
        /// <summary>
        /// 生成ARAB金蝶凭证数据
        /// </summary>
        private static List<KingdeeVoucher> GenerateArabKingdeeVouchers(
            List<ArabGroupDataItem> arabGroupData,
            DateTime accountingDate,
            Dictionary<string, SubjectConfiguration> subjectConfigs)
        {
            var vouchers = new List<KingdeeVoucher>();
            var voucherNumber = 1;
            // 获取通用配置
            var preparerName = subjectConfigs.ContainsKey("GEN_PREPARER") ?
                (subjectConfigs["GEN_PREPARER"]?.Preparer ?? "系统导出") : "系统导出";
            var voucherGroup = subjectConfigs.ContainsKey("GEN_VOUCHER_GROUP") ?
                (subjectConfigs["GEN_VOUCHER_GROUP"]?.VoucherGroup ?? "转") : "转";
            // 计算总金额
            var totalAmount = arabGroupData.Sum(g => g.TotalAmount);
            int entryId = 0;
            // 生成明细分录（借方）
            foreach (var group in arabGroupData)
            {
                string subjectCode;
                string description;
                // 根据国内外和代垫属性确定科目
                if (group.IsDomestic)
                {
                    if (group.IsAdvance)
                    {
                        subjectCode = "ARAB_IN_TAR";
                        description = $"计提应收国内-关税-{group.CustomerName} {group.TotalAmount:F2}元";
                    }
                    else
                    {
                        subjectCode = "ARAB_IN_CUS";
                        description = $"计提应收国内-客户-{group.CustomerName} {group.TotalAmount:F2}元";
                    }
                }
                else
                {
                    if (group.IsAdvance)
                    {
                        subjectCode = "ARAB_OUT_TAR";
                        description = $"计提应收国外-关税-{group.CustomerName} {group.TotalAmount:F2}元";
                    }
                    else
                    {
                        subjectCode = "ARAB_OUT_CUS";
                        description = $"计提应收国外-客户-{group.CustomerName} {group.TotalAmount:F2}元";
                    }
                }
                if (subjectConfigs.TryGetValue(subjectCode, out var config) && config != null)
                {
                    vouchers.Add(new KingdeeVoucher
                    {
                        Id = Guid.NewGuid(),
                        FDATE = accountingDate,
                        FTRANSDATE = accountingDate,
                        FPERIOD = accountingDate.Month,
                        FGROUP = voucherGroup,
                        FNUM = voucherNumber,
                        FENTRYID = entryId++,
                        FEXP = description,
                        FACCTID = config.SubjectNumber,
                        FCLSNAME1 = config.AccountingCategory ?? "客户",
                        FOBJID1 = group.CustomerShortName ?? group.CustomerFinanceCode ?? "CUSTOMER",
                        FOBJNAME1 = group.CustomerName,
                        FTRANSID = group.CustomerFinanceCode ?? "",
                        FCYID = "RMB",
                        FEXCHRATE = 1.0000000m,
                        FDC = 0, // 借方
                        FFCYAMT = group.TotalAmount,
                        FDEBIT = group.TotalAmount,
                        FCREDIT = 0,
                        FPREPARE = preparerName,
                        FMODULE = "GL",
                        FDELETED = false
                    });
                }
            }
            // 生成总科目分录（贷方）
            if (subjectConfigs.TryGetValue("ARAB_TOTAL", out var totalConfig) && totalConfig != null)
            {
                vouchers.Add(new KingdeeVoucher
                {
                    Id = Guid.NewGuid(),
                    FDATE = accountingDate,
                    FTRANSDATE = accountingDate,
                    FPERIOD = accountingDate.Month,
                    FGROUP = voucherGroup,
                    FNUM = voucherNumber,
                    FENTRYID = entryId,
                    FEXP = $"计提{accountingDate:yyyy年MM月}总应收 {totalAmount:F2}元",
                    FACCTID = totalConfig.SubjectNumber,
                    FCYID = "RMB",
                    FEXCHRATE = 1.0000000m,
                    FDC = 1, // 贷方
                    FFCYAMT = totalAmount,
                    FDEBIT = 0,
                    FCREDIT = totalAmount,
                    FPREPARE = preparerName,
                    FMODULE = "GL",
                    FDELETED = false
                });
            }
            return vouchers;
        }
        /// <summary>
        /// 针对费用数据的组织权限过滤方法（静态版本）
        /// </summary>
        private static IQueryable<DocFee> ApplyOrganizationFilterForFeesStatic(IQueryable<DocFee> feesQuery, Account user,
            PowerLmsUserDbContext dbContext, IServiceProvider serviceProvider)
        {
            if (user == null)
            {
                return feesQuery.Where(f => false);
            }
            if (user.IsSuperAdmin)
            {
                return feesQuery;
            }
            var orgManager = serviceProvider.GetRequiredService<OrgManager<PowerLmsUserDbContext>>();
            // 获取用户所属商户ID
            var merchantId = orgManager.GetMerchantIdByUserId(user.Id);
            if (!merchantId.HasValue)
            {
                return feesQuery.Where(f => false);
            }
            HashSet<Guid?> allowedOrgIds;
            if (user.IsMerchantAdmin)
            {
                // 商户管理员可以访问整个商户下的所有组织机构
                var allOrgIds = orgManager.GetOrLoadOrgCacheItem(merchantId.Value).Orgs.Keys.ToList();
                allowedOrgIds = new HashSet<Guid?>(allOrgIds.Cast<Guid?>());
                allowedOrgIds.Add(merchantId.Value); // 添加商户ID本身
            }
            else
            {
                // 普通用户只能访问其当前登录的公司及下属机构
                var companyId = user.OrgId.HasValue ? orgManager.GetCompanyIdByOrgId(user.OrgId.Value) : null;
                if (!companyId.HasValue)
                {
                    return feesQuery.Where(f => false);
                }
                var companyOrgIds = orgManager.GetOrgIdsByCompanyId(companyId.Value).ToList();
                allowedOrgIds = new HashSet<Guid?>(companyOrgIds.Cast<Guid?>());
                allowedOrgIds.Add(merchantId.Value); // 添加商户ID本身
            }
            // 通过关联的业务过滤费用
            var filteredQuery = from fee in feesQuery
                               join job in dbContext.PlJobs
                                   on fee.JobId equals job.Id into jobGroup
                               from plJob in jobGroup.DefaultIfEmpty()
                               where allowedOrgIds.Contains(plJob.OrgId)
                               select fee;
            return filteredQuery.Distinct();
        }
        #endregion
    }
}