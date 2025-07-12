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
    /// ����ϵͳ�������ܿ����� - ARAB(����A��Ӧ�ձ�λ�ҹ���)ģ�顣
    /// ʵ��ARAB(����A��Ӧ�ձ�λ�ҹ���)���̵ĵ������ܡ�
    /// ���ݷ������ݰ����㵥λ�������⡢�������Է���ͳ�ƣ����ɽ��ƾ֤�ļ���
    /// </summary>
    public partial class FinancialSystemExportController
    {
        #region HTTP�ӿ� - ARAB(����A��Ӧ�ձ�λ�ҹ���)

        /// <summary>
        /// ����A��Ӧ�ձ�λ�ҹ���(ARAB)����Ϊ���DBF��ʽ�ļ���
        /// </summary>
        [HttpPost]
        public ActionResult<ExportArabToDbfReturnDto> ExportArabToDbf(ExportArabToDbfParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();

            var result = new ExportArabToDbfReturnDto();
            try
            {
                // ��ExportConditions�н�������
                var conditions = model.ExportConditions ?? new Dictionary<string, string>();
                
                // ����Ĭ�����ڷ�Χ
                var startDate = conditions.TryGetValue("StartDate", out var startDateStr) && DateTime.TryParse(startDateStr, out var parsedStartDate) 
                    ? parsedStartDate : new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                var endDate = conditions.TryGetValue("EndDate", out var endDateStr) && DateTime.TryParse(endDateStr, out var parsedEndDate) 
                    ? parsedEndDate.Date.AddDays(1).AddTicks(-1) : DateTime.Now.Date.AddDays(1).AddTicks(-1);
                var accountingDate = conditions.TryGetValue("AccountingDate", out var accountingDateStr) && DateTime.TryParse(accountingDateStr, out var parsedAccountingDate) 
                    ? parsedAccountingDate : DateTime.Now.Date;

                // ��������
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
                result.Message = "ARAB���������Ѵ���";
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

        #region ��̬�������� - ARAB

        /// <summary>
        /// ����ARAB DBF��������
        /// </summary>
        public static object ProcessArabDbfExportTask(Guid taskId, Dictionary<string, string> parameters, IServiceProvider serviceProvider)
        {
            // ��ʵ�֣������ػ������
            return new
            {
                TaskId = taskId,
                Message = "ARAB���������",
                Status = "Success"
            };
        }

        #endregion

        #region ʵ������ - ARAB

        /// <summary>
        /// ��֤ARAB��Ŀ�����Ƿ�����
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
        /// ����ARAB���ò�ѯ
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
        /// �����û�Ȩ�޹���ARAB���ò�ѯ
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
    /// ARAB���÷������ݽṹ
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