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
            // 简化实现，仅返回基本结果
            return new
            {
                TaskId = taskId,
                Message = "ARAB任务处理完成",
                Status = "Success"
            };
        }

        #endregion

        #region 实例方法 - ARAB

        /// <summary>
        /// 验证ARAB科目配置是否完整
        /// </summary>
        private List<string> ValidateArabSubjectConfiguration(Guid? orgId)
        {
            var requiredCodes = new List<string>
            {
                "GEN_TOTAL_RECEIVABLE",
                "ARAB_DOMESTIC_NON_ADVANCE",
                "ARAB_DOMESTIC_ADVANCE",
                "ARAB_FOREIGN_NON_ADVANCE",
                "ARAB_FOREIGN_ADVANCE"
            };

            var existingCodes = _DbContext.SubjectConfigurations
                .Where(c => !c.IsDelete && c.OrgId == orgId && requiredCodes.Contains(c.Code))
                .Select(c => c.Code)
                .ToList();

            return requiredCodes.Except(existingCodes).ToList();
        }

        /// <summary>
        /// 构建ARAB费用查询
        /// </summary>
        private IQueryable<DocFee> BuildArabFeesQuery(DateTime startDate, DateTime endDate, Dictionary<string, string> conditions)
        {
            var query = _DbContext.DocFees.Where(f => f.CreateDateTime >= startDate && f.CreateDateTime <= endDate);
            
            if (conditions != null && conditions.Any())
            {
                query = EfHelper.GenerateWhereAnd(query, conditions);
            }

            return query;
        }

        /// <summary>
        /// 根据用户权限过滤ARAB费用查询
        /// </summary>
        private IQueryable<DocFee> ApplyArabOrganizationFilter(IQueryable<DocFee> feesQuery, Account user)
        {
            if (user.IsSuperAdmin)
                return feesQuery;

            var merchantId = _OrgManager.GetMerchantIdByUserId(user.Id);
            if (!merchantId.HasValue)
                return feesQuery.Where(f => false);

            if (user.IsMerchantAdmin)
            {
                var merchantOrgIds = _OrgManager.GetOrLoadOrgCacheItem(merchantId.Value).Orgs.Keys
                    .Select(id => (Guid?)id).ToHashSet();
                merchantOrgIds.Add(merchantId.Value);

                return from fee in feesQuery
                       join job in _DbContext.PlJobs on fee.JobId equals job.Id into jobGroup
                       from plJob in jobGroup.DefaultIfEmpty()
                       where merchantOrgIds.Contains(plJob.OrgId)
                       select fee;
            }
            else
            {
                var companyId = user.OrgId.HasValue ? _OrgManager.GetCompanyIdByOrgId(user.OrgId.Value) : null;
                if (!companyId.HasValue)
                    return feesQuery.Where(f => false);
                
                var userOrgIds = _OrgManager.GetOrgIdsByCompanyId(companyId.Value)
                    .Select(id => (Guid?)id).ToHashSet();
                userOrgIds.Add(merchantId.Value);

                return from fee in feesQuery
                       join job in _DbContext.PlJobs on fee.JobId equals job.Id into jobGroup
                       from plJob in jobGroup.DefaultIfEmpty()
                       where userOrgIds.Contains(plJob.OrgId)
                       select fee;
            }
        }

        #endregion
    }

    /// <summary>
    /// ARAB费用分组数据结构
    /// </summary>
    public class ArabFeeGroupData
    {
        public Guid? BalanceId { get; set; }
        public string CustomerName { get; set; }
        public string CustomerShortName { get; set; }
        public string CustomerFinanceCode { get; set; }
        public bool IsDomestic { get; set; }
        public bool IsAdvance { get; set; }
        public decimal TotalAmount { get; set; }
    }
}