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

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// 财务系统导出功能控制器 - ARAB(计提A账应收本位币挂账)模块。
    /// 实现ARAB(计提A账应收本位币挂账)过程的导出功能。
    /// 根据费用数据按结算单位、国内外、代垫属性分组统计，生成金蝶凭证文件。
    /// </summary>
    public partial class FinancialSystemExportController
    {
        #region HTTP接口 - ARAB(计提A账应收本位币挂账)

        /// <summary>
        /// 导出A账应收本位币挂账(ARAB)数据为金蝶DBF格式文件。
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

                // 预检查费用数据
                var feesQuery = _DbContext.DocFees
                    .Where(f => f.IO == true && // 只统计收入
                               f.CreateDateTime >= startDate && 
                               f.CreateDateTime <= endDate);

                var feeCount = feesQuery.Count();
                if (feeCount == 0)
                {
                    result.HasError = true;
                    result.ErrorCode = 404;
                    result.DebugMessage = "没有找到符合条件的费用数据，请调整查询条件";
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

                currentStep = "解析服务依赖";
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

                currentStep = "查询费用数据";
                var feesQuery = dbContext.DocFees
                    .Where(f => f.IO == true && // 只统计收入
                               f.CreateDateTime >= startDate && 
                               f.CreateDateTime <= endDate);

                // 应用额外的查询条件
                if (conditions != null && conditions.Any())
                {
                    feesQuery = EfHelper.GenerateWhereAnd(feesQuery, conditions);
                }

                // 应用组织权限过滤
                var taskUser = dbContext.Accounts?.FirstOrDefault(a => a.Id == userId);
                if (taskUser != null)
                {
                    feesQuery = ApplyOrganizationFilterForFees(feesQuery, taskUser, dbContext, serviceProvider);
                }

                var arabGroupData = (from fee in feesQuery
                                   join customer in dbContext.PlCustomers on fee.BalanceId equals customer.Id into customerGroup
                                   from cust in customerGroup.DefaultIfEmpty()
                                   group new { fee, cust } by new
                                   {
                                       BalanceId = fee.BalanceId,
                                       CustomerName = cust != null ? cust.Name_DisplayName : "未知客户",
                                       CustomerFinanceCode = cust != null ? cust.TacCountNo : "",
                                       IsDomestic = cust != null ? (cust.IsDomestic ?? true) : true
                                   } into g
                                   select new
                                   {
                                       BalanceId = g.Key.BalanceId,
                                       CustomerName = g.Key.CustomerName,
                                       CustomerFinanceCode = g.Key.CustomerFinanceCode,
                                       IsDomestic = g.Key.IsDomestic,
                                       TotalAmount = g.Sum(x => x.fee.Amount * x.fee.ExchangeRate)
                                   }).ToList();

                if (!arabGroupData.Any())
                    throw new InvalidOperationException("没有找到符合条件的费用数据");

                currentStep = "生成凭证数据";
                var kingdeeVouchers = new List<KingdeeVoucher>();
                
                // 生成计提总应收分录（借方）
                var totalAmount = arabGroupData.Sum(g => g.TotalAmount);
                kingdeeVouchers.Add(new KingdeeVoucher
                {
                    Id = Guid.NewGuid(),
                    FDATE = accountingDate,
                    FTRANSDATE = accountingDate,
                    FPERIOD = accountingDate.Month,
                    FGROUP = "转",
                    FNUM = 1,
                    FENTRYID = 0,
                    FEXP = $"计提{accountingDate:yyyy年MM月}总应收 {totalAmount:F2}元",
                    FACCTID = "1122",
                    FCYID = "RMB",
                    FEXCHRATE = 1.0000000m,
                    FDC = 0, // 借方
                    FFCYAMT = totalAmount,
                    FDEBIT = totalAmount,
                    FCREDIT = 0,
                    FPREPARE = "系统导出",
                    FMODULE = "GL",
                    FDELETED = false
                });

                // 生成明细分录（贷方）
                int entryId = 1;
                foreach (var group in arabGroupData)
                {
                    kingdeeVouchers.Add(new KingdeeVoucher
                    {
                        Id = Guid.NewGuid(),
                        FDATE = accountingDate,
                        FTRANSDATE = accountingDate,
                        FPERIOD = accountingDate.Month,
                        FGROUP = "转",
                        FNUM = 1,
                        FENTRYID = entryId++,
                        FEXP = $"应收账款-{group.CustomerName} {group.TotalAmount:F2}元",
                        FACCTID = group.IsDomestic ? "1123" : "1124",
                        FCLSNAME1 = "客户",
                        FOBJID1 = group.CustomerFinanceCode ?? "CUSTOMER",
                        FOBJNAME1 = group.CustomerName,
                        FTRANSID = group.CustomerFinanceCode ?? "",
                        FCYID = "RMB",
                        FEXCHRATE = 1.0000000m,
                        FDC = 1, // 贷方
                        FFCYAMT = group.TotalAmount,
                        FDEBIT = 0,
                        FCREDIT = group.TotalAmount,
                        FPREPARE = "系统导出",
                        FMODULE = "GL",
                        FDELETED = false
                    });
                }

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

                currentStep = "创建文件记录";
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
                        displayName : $"ARAB计提导出-{DateTime.Now:yyyy年MM月dd日}";
                    var finalRemark = !string.IsNullOrWhiteSpace(remark) ? 
                        remark : $"ARAB计提DBF导出文件，共{arabGroupData.Count}个客户分组，{kingdeeVouchers.Count}条会计分录，导出时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                    
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

                currentStep = "验证最终文件并返回结果";
                long actualFileSize = 0;
                bool fileExists = false;
                try
                {
                    if (System.IO.File.Exists(fileInfoRecord.FilePath))
                    {
                        actualFileSize = new FileInfo(fileInfoRecord.FilePath).Length;
                        fileExists = true;
                    }
                }
                catch { }

                return new
                {
                    FileId = fileInfoRecord.Id,
                    FileName = fileName,
                    FeeGroupCount = arabGroupData.Count,
                    VoucherCount = kingdeeVouchers.Count,
                    TotalAmount = totalAmount,
                    FilePath = fileInfoRecord.FilePath,
                    ExportDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    FileSize = actualFileSize,
                    FileExists = fileExists,
                    OriginalFileSize = fileSize
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
        /// 针对费用数据的组织权限过滤方法（静态版本）
        /// </summary>
        private static IQueryable<DocFee> ApplyOrganizationFilterForFees(IQueryable<DocFee> feesQuery, Account user,
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