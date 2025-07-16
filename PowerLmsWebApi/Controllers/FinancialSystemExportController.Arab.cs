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

                // Ԥ����������
                var feesQuery = _DbContext.DocFees
                    .Where(f => f.IO == true && // ֻͳ������
                               f.CreateDateTime >= startDate && 
                               f.CreateDateTime <= endDate);

                var feeCount = feesQuery.Count();
                if (feeCount == 0)
                {
                    result.HasError = true;
                    result.ErrorCode = 404;
                    result.DebugMessage = "û���ҵ����������ķ������ݣ��������ѯ����";
                    return result;
                }

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

        #region ��̬�������� - ARAB

        /// <summary>
        /// ARAB����������
        /// </summary>
        public class ArabGroupDataItem
        {
            /// <summary>
            /// ���㵥λID
            /// </summary>
            public Guid? BalanceId { get; set; }
            
            /// <summary>
            /// �ͻ�����
            /// </summary>
            public string CustomerName { get; set; }
            
            /// <summary>
            /// �ͻ����
            /// </summary>
            public string CustomerShortName { get; set; }
            
            /// <summary>
            /// �ͻ��������
            /// </summary>
            public string CustomerFinanceCode { get; set; }
            
            /// <summary>
            /// �Ƿ���ڿͻ�
            /// </summary>
            public bool IsDomestic { get; set; }
            
            /// <summary>
            /// �Ƿ�������
            /// </summary>
            public bool IsAdvance { get; set; }
            
            /// <summary>
            /// �ܽ���λ�ң�
            /// </summary>
            public decimal TotalAmount { get; set; }
        }

        /// <summary>
        /// ����ARAB DBF��������
        /// </summary>
        public static object ProcessArabDbfExportTask(Guid taskId, Dictionary<string, string> parameters, IServiceProvider serviceProvider)
        {
            string currentStep = "������֤";
            try
            {
                if (serviceProvider == null)
                    throw new ArgumentNullException(nameof(serviceProvider), "�����ṩ�߲���Ϊ��");
                if (parameters == null)
                    throw new ArgumentNullException(nameof(parameters), "�����������Ϊ��");

                currentStep = "������������";
                var dbContextFactory = serviceProvider.GetService<IDbContextFactory<PowerLmsUserDbContext>>() ??
                    throw new InvalidOperationException("�޷���ȡ���ݿ������Ĺ���");
                var fileService = serviceProvider.GetService<OwFileService<PowerLmsUserDbContext>>() ??
                    throw new InvalidOperationException("�޷���ȡ�ļ�����");

                currentStep = "�����������";
                if (!parameters.TryGetValue("StartDate", out var startDateStr) || !DateTime.TryParse(startDateStr, out var startDate))
                    throw new InvalidOperationException("ȱ�ٻ���Ч�Ŀ�ʼ���ڲ���");
                if (!parameters.TryGetValue("EndDate", out var endDateStr) || !DateTime.TryParse(endDateStr, out var endDate))
                    throw new InvalidOperationException("ȱ�ٻ���Ч�Ľ������ڲ���");
                if (!parameters.TryGetValue("AccountingDate", out var accountingDateStr) || !DateTime.TryParse(accountingDateStr, out var accountingDate))
                    throw new InvalidOperationException("ȱ�ٻ���Ч�ļ������ڲ���");
                if (!parameters.TryGetValue("UserId", out var userIdStr) || !Guid.TryParse(userIdStr, out var userId))
                    throw new InvalidOperationException("ȱ�ٻ���Ч���û�ID����");

                Guid? orgId = null;
                if (parameters.TryGetValue("OrgId", out var orgIdStr) && !string.IsNullOrEmpty(orgIdStr))
                {
                    if (!Guid.TryParse(orgIdStr, out var parsedOrgId))
                        throw new InvalidOperationException($"��Ч����֯ID��ʽ: {orgIdStr}");
                    orgId = parsedOrgId;
                }

                var displayName = parameters.GetValueOrDefault("DisplayName", "");
                var remark = parameters.GetValueOrDefault("Remark", "");

                // ������������
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
                        throw new InvalidOperationException($"��������JSON��ʽ����: {ex.Message}");
                    }
                }
                conditions ??= new Dictionary<string, string>();

                currentStep = "�������ݿ�������";
                using var dbContext = dbContextFactory.CreateDbContext();

                currentStep = "���ؿ�Ŀ����";
                var subjectConfigs = LoadArabSubjectConfigurations(dbContext, orgId);
                if (!subjectConfigs.Any())
                    throw new InvalidOperationException($"ARAB��Ŀ����δ�ҵ����޷�����ƾ֤����֯ID: {orgId}");

                currentStep = "��ѯ��������";
                var feesQuery = dbContext.DocFees
                    .Where(f => f.IO == true && // ֻͳ������
                               f.CreateDateTime >= startDate && 
                               f.CreateDateTime <= endDate);

                // Ӧ�ö���Ĳ�ѯ����
                if (conditions != null && conditions.Any())
                {
                    feesQuery = EfHelper.GenerateWhereAnd(feesQuery, conditions);
                }

                // Ӧ����֯Ȩ�޹���
                var taskUser = dbContext.Accounts?.FirstOrDefault(a => a.Id == userId);
                if (taskUser != null)
                {
                    feesQuery = ApplyOrganizationFilterForFeesStatic(feesQuery, taskUser, dbContext, serviceProvider);
                }

                currentStep = "��ҵ��������ͳ��";
                // ARABҵ���߼���IO=���룬sum(Amount*ExchangeRate) as Totalamount���� ����.���㵥λ�����㵥λ.���𡢷�������.���� ����
                var arabGroupData = (from fee in feesQuery
                                   join customer in dbContext.PlCustomers on fee.BalanceId equals customer.Id into customerGroup
                                   from cust in customerGroup.DefaultIfEmpty()
                                   join feeType in dbContext.DD_SimpleDataDics on fee.FeeTypeId equals feeType.Id into feeTypeGroup
                                   from feeTypeDict in feeTypeGroup.DefaultIfEmpty()
                                   group new { fee, cust, feeTypeDict } by new
                                   {
                                       BalanceId = fee.BalanceId,
                                       CustomerName = cust != null ? cust.Name_DisplayName : "δ֪�ͻ�",
                                       CustomerShortName = cust != null ? cust.Name_ShortName : "",
                                       CustomerFinanceCode = cust != null ? cust.TacCountNo : "",
                                       IsDomestic = cust != null ? (cust.IsDomestic ?? true) : true,
                                       IsAdvance = feeTypeDict != null && feeTypeDict.Remark != null && feeTypeDict.Remark.Contains("����")
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
                    throw new InvalidOperationException("û���ҵ����������ķ�������");

                currentStep = "���ɽ��ƾ֤����";
                var kingdeeVouchers = GenerateArabKingdeeVouchers(arabGroupData, accountingDate, subjectConfigs);

                currentStep = "����DBF�ļ�";
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

                currentStep = "�����ļ���¼";
                PlFileInfo fileInfoRecord;
                long fileSize;
                var memoryStream = new MemoryStream(1024 * 1024 * 1024);
                try
                {
                    DotNetDbfUtil.WriteToStream(kingdeeVouchers, memoryStream, kingdeeFieldMappings, customFieldTypes);
                    fileSize = memoryStream.Length;
                    if (fileSize == 0)
                        throw new InvalidOperationException("DBF�ļ�����ʧ�ܣ��ļ�Ϊ��");
                    memoryStream.Position = 0;
                    
                    var finalDisplayName = !string.IsNullOrWhiteSpace(displayName) ? 
                        displayName : $"ARAB���ᵼ��-{DateTime.Now:yyyy��MM��dd��}";
                    var finalRemark = !string.IsNullOrWhiteSpace(remark) ? 
                        remark : $"ARAB����DBF�����ļ�����{arabGroupData.Count}���ͻ����飬{kingdeeVouchers.Count}����Ʒ�¼������ʱ�䣺{DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                    
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
                    throw new InvalidOperationException("fileService.CreateFile ���� null");

                currentStep = "��֤�����ļ������ؽ��";
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
                    TotalAmount = arabGroupData.Sum(g => g.TotalAmount),
                    FilePath = fileInfoRecord.FilePath,
                    ExportDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    FileSize = actualFileSize,
                    FileExists = fileExists,
                    OriginalFileSize = fileSize
                };
            }
            catch (Exception ex)
            {
                var contextualError = $"ARAB DBF��������ʧ�ܣ���ǰ����: {currentStep}, ����ID: {taskId}";
                if (parameters != null)
                    contextualError += $"\n�������: {string.Join(", ", parameters.Select(kv => $"{kv.Key}={kv.Value}"))}";

                throw new InvalidOperationException(contextualError, ex);
            }
        }

        /// <summary>
        /// ����ARAB��Ŀ���ã���̬�汾��
        /// </summary>
        private static Dictionary<string, SubjectConfiguration> LoadArabSubjectConfigurations(PowerLmsUserDbContext dbContext, Guid? orgId)
        {
            var requiredCodes = new List<string>
            {
                "ARAB_TOTAL",      // ������Ӧ��
                "ARAB_IN_CUS",     // ����Ӧ�չ���-�ͻ�
                "ARAB_IN_TAR",     // ����Ӧ�չ���-��˰
                "ARAB_OUT_CUS",    // ����Ӧ�չ���-�ͻ�
                "ARAB_OUT_TAR",    // ����Ӧ�չ���-��˰
                "GEN_PREPARER",    // �Ƶ���
                "GEN_VOUCHER_GROUP" // ƾ֤�����
            };

            var configs = dbContext.SubjectConfigurations
                .Where(c => !c.IsDelete && c.OrgId == orgId && requiredCodes.Contains(c.Code))
                .ToList();

            return configs.ToDictionary(c => c.Code, c => c);
        }

        /// <summary>
        /// ����ARAB���ƾ֤����
        /// </summary>
        private static List<KingdeeVoucher> GenerateArabKingdeeVouchers(
            List<ArabGroupDataItem> arabGroupData,
            DateTime accountingDate,
            Dictionary<string, SubjectConfiguration> subjectConfigs)
        {
            var vouchers = new List<KingdeeVoucher>();
            var voucherNumber = 1;
            
            // ��ȡͨ������
            var preparerName = subjectConfigs.ContainsKey("GEN_PREPARER") ?
                (subjectConfigs["GEN_PREPARER"]?.Preparer ?? "ϵͳ����") : "ϵͳ����";
            var voucherGroup = subjectConfigs.ContainsKey("GEN_VOUCHER_GROUP") ?
                (subjectConfigs["GEN_VOUCHER_GROUP"]?.VoucherGroup ?? "ת") : "ת";

            // �����ܽ��
            var totalAmount = arabGroupData.Sum(g => g.TotalAmount);

            int entryId = 0;

            // ������ϸ��¼���跽��
            foreach (var group in arabGroupData)
            {
                string subjectCode;
                string description;
                
                // ���ݹ�����ʹ�������ȷ����Ŀ
                if (group.IsDomestic)
                {
                    if (group.IsAdvance)
                    {
                        subjectCode = "ARAB_IN_TAR";
                        description = $"����Ӧ�չ���-��˰-{group.CustomerName} {group.TotalAmount:F2}Ԫ";
                    }
                    else
                    {
                        subjectCode = "ARAB_IN_CUS";
                        description = $"����Ӧ�չ���-�ͻ�-{group.CustomerName} {group.TotalAmount:F2}Ԫ";
                    }
                }
                else
                {
                    if (group.IsAdvance)
                    {
                        subjectCode = "ARAB_OUT_TAR";
                        description = $"����Ӧ�չ���-��˰-{group.CustomerName} {group.TotalAmount:F2}Ԫ";
                    }
                    else
                    {
                        subjectCode = "ARAB_OUT_CUS";
                        description = $"����Ӧ�չ���-�ͻ�-{group.CustomerName} {group.TotalAmount:F2}Ԫ";
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
                        FCLSNAME1 = config.AccountingCategory ?? "�ͻ�",
                        FOBJID1 = group.CustomerShortName ?? group.CustomerFinanceCode ?? "CUSTOMER",
                        FOBJNAME1 = group.CustomerName,
                        FTRANSID = group.CustomerFinanceCode ?? "",
                        FCYID = "RMB",
                        FEXCHRATE = 1.0000000m,
                        FDC = 0, // �跽
                        FFCYAMT = group.TotalAmount,
                        FDEBIT = group.TotalAmount,
                        FCREDIT = 0,
                        FPREPARE = preparerName,
                        FMODULE = "GL",
                        FDELETED = false
                    });
                }
            }

            // �����ܿ�Ŀ��¼��������
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
                    FEXP = $"����{accountingDate:yyyy��MM��}��Ӧ�� {totalAmount:F2}Ԫ",
                    FACCTID = totalConfig.SubjectNumber,
                    FCYID = "RMB",
                    FEXCHRATE = 1.0000000m,
                    FDC = 1, // ����
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
        /// ��Է������ݵ���֯Ȩ�޹��˷�������̬�汾��
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

            // ��ȡ�û������̻�ID
            var merchantId = orgManager.GetMerchantIdByUserId(user.Id);
            if (!merchantId.HasValue)
            {
                return feesQuery.Where(f => false);
            }

            HashSet<Guid?> allowedOrgIds;

            if (user.IsMerchantAdmin)
            {
                // �̻�����Ա���Է��������̻��µ�������֯����
                var allOrgIds = orgManager.GetOrLoadOrgCacheItem(merchantId.Value).Orgs.Keys.ToList();
                allowedOrgIds = new HashSet<Guid?>(allOrgIds.Cast<Guid?>());
                allowedOrgIds.Add(merchantId.Value); // ����̻�ID����
            }
            else
            {
                // ��ͨ�û�ֻ�ܷ����䵱ǰ��¼�Ĺ�˾����������
                var companyId = user.OrgId.HasValue ? orgManager.GetCompanyIdByOrgId(user.OrgId.Value) : null;
                if (!companyId.HasValue)
                {
                    return feesQuery.Where(f => false);
                }
                
                var companyOrgIds = orgManager.GetOrgIdsByCompanyId(companyId.Value).ToList();
                allowedOrgIds = new HashSet<Guid?>(companyOrgIds.Cast<Guid?>());
                allowedOrgIds.Add(merchantId.Value); // ����̻�ID����
            }

            // ͨ��������ҵ����˷���
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