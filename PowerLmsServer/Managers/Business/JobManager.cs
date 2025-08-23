using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using Microsoft.IdentityModel.Protocols;
using NPOI.SS.Formula.Functions;
using Org.BouncyCastle.Asn1.IsisMtt.X509;
using OW.Data;
using PowerLms.Data;
using PowerLmsServer.EfData;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PowerLmsServer.Managers
{
    /// <summary>
    /// 工作任务管理器。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Singleton)]
    public class JobManager
    {
        /// <summary>
        /// 工作编码的译码表，不含序号。
        /// </summary>
        static readonly Dictionary<string, string> _JobNumberDecodingTable = new()
        {
            { "<yy>","yy"},
            { "<yyyy>","yyyy"},
            { "<M>","M"},
            { "<MM>","MM"},
            { "<d>","d"},
            { "<dd>","dd"},
            //{ "<h>","h"},
            { "<hh>","hh"},
        };

        #region 编号相关

        /// <summary>
        /// 生成编号。
        /// </summary>
        /// <param name="rule"></param>
        /// <param name="account"></param>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public string Generated(JobNumberRule rule, Account account, DateTime dateTime)
        {
            var sb = AutoClearPool<StringBuilder>.Shared.Get(); using var dw = DisposeHelper.Create(c => AutoClearPool<StringBuilder>.Shared.Return(c), sb);
            sb.Append(rule.RuleString);
            //确定序列号
            int seq;
            // 归零方式，0不归零，1按年，2按月，3按日
            switch (rule.RepeatMode)
            {
                case 0:
                    seq = rule.CurrentNumber++;
                    break;
                case 1:
                    if (rule.RepeatDate.Year == dateTime.Year)    //若无需归零
                        seq = rule.CurrentNumber++;
                    else //若须归零
                    {
                        rule.CurrentNumber = rule.StartValue;
                        seq = rule.CurrentNumber++;
                        rule.RepeatDate = dateTime;
                    }
                    break;
                case 2:
                    if (rule.RepeatDate.Year == dateTime.Year && rule.RepeatDate.Month == dateTime.Month)    //若无需归零
                        seq = rule.CurrentNumber++;
                    else //若须归零
                    {
                        rule.CurrentNumber = rule.StartValue;
                        seq = rule.CurrentNumber++;
                        rule.RepeatDate = dateTime;
                    }
                    break;
                case 3:
                    if (rule.RepeatDate.Year == dateTime.Year && rule.RepeatDate.DayOfYear == dateTime.DayOfYear)    //若无需归零
                        seq = rule.CurrentNumber++;
                    else //若须归零
                    {
                        rule.CurrentNumber = rule.StartValue;
                        seq = rule.CurrentNumber++;
                        rule.RepeatDate = dateTime;
                    }
                    break;
                default:
                    throw new ArgumentException("参数有误。", nameof(rule));
            }
            //时间简单规则替换
            foreach (var kvp in _JobNumberDecodingTable)
            {
                sb.Replace(kvp.Key, dateTime.ToString(kvp.Value));
            }
            //工号替换
            var str = Regex.Replace(sb.ToString(), @"\<[X]*?\>", c =>
            {
                if (!c.Success) return string.Empty;
                if (c.Length < 3) return string.Empty;
                if (account?.JobNumber is null) return string.Empty;
                else
                {
                    var tmp = new string('0', c.Length - 2);
                    return account.JobNumber.Value.ToString(tmp);
                }
            });
            //序号替换
            str = Regex.Replace(str, @"\<[0]*?\>", c =>
            {
                if (!c.Success) return string.Empty;
                if (c.Length < 1) return string.Empty;
                var tmp = new string('0', c.Length - 2);
                return seq.ToString(tmp);
            });
            return str;
        }

        /// <summary>
        /// 生成编号。
        /// </summary>
        /// <param name="rule"></param>
        /// <param name="account"></param>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public string Generated(OtherNumberRule rule, Account account, DateTime dateTime)
        {
            var sb = AutoClearPool<StringBuilder>.Shared.Get(); using var dw = DisposeHelper.Create(c => AutoClearPool<StringBuilder>.Shared.Return(c), sb);
            sb.Append(rule.RuleString);
            //确定序列号
            int seq;
            // 归零方式，0不归零，1按年，2按月，3按日
            switch (rule.RepeatMode)
            {
                case 0:
                    seq = rule.CurrentNumber++;
                    break;
                case 1:
                    if (rule.RepeatDate.Year == dateTime.Year)    //若无需归零
                        seq = rule.CurrentNumber++;
                    else //若须归零
                    {
                        rule.CurrentNumber = rule.StartValue;
                        seq = rule.CurrentNumber++;
                        rule.RepeatDate = dateTime;
                    }
                    break;
                case 2:
                    if (rule.RepeatDate.Year == dateTime.Year && rule.RepeatDate.Month == dateTime.Month)    //若无需归零
                        seq = rule.CurrentNumber++;
                    else //若须归零
                    {
                        rule.CurrentNumber = rule.StartValue;
                        seq = rule.CurrentNumber++;
                        rule.RepeatDate = dateTime;
                    }
                    break;
                case 3:
                    if (rule.RepeatDate.Year == dateTime.Year && rule.RepeatDate.DayOfYear == dateTime.DayOfYear)    //若无需归零
                        seq = rule.CurrentNumber++;
                    else //若须归零
                    {
                        rule.CurrentNumber = rule.StartValue;
                        seq = rule.CurrentNumber++;
                        rule.RepeatDate = dateTime;
                    }
                    break;
                default:
                    throw new ArgumentException("参数有误。", nameof(rule));
            }
            //时间简单规则替换
            foreach (var kvp in _JobNumberDecodingTable)
            {
                sb.Replace(kvp.Key, dateTime.ToString(kvp.Value));
            }
            //工号替换
            var str = Regex.Replace(sb.ToString(), @"\<[X]*?\>", c =>
            {
                if (!c.Success) return string.Empty;
                if (c.Length < 3) return string.Empty;
                if (account?.JobNumber is null) return string.Empty;
                else
                {
                    var tmp = new string('0', c.Length - 2);
                    return account.JobNumber.Value.ToString(tmp);
                }
            });
            //序号替换
            str = Regex.Replace(str, @"\<[0]*?\>", c =>
            {
                if (!c.Success) return string.Empty;
                if (c.Length < 1) return string.Empty;
                var tmp = new string('0', c.Length - 2);
                return seq.ToString(tmp);
            });
            return str;
        }
        #endregion 编号相关

        #region 任务相关

        /// <summary>
        /// 审核工作任务并审核所有下属费用。
        /// </summary>
        /// <param name="job"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public bool Audit(PlJob job, OwContext context)
        {
            var db = context.ServiceProvider.GetRequiredService<PowerLmsUserDbContext>();
            if (job.JobState != 4)
            {
                OwHelper.SetLastErrorAndMessage(400, $"{nameof(job.JobState)}必须是4才能审核");
                return false;
            }
            job.JobState = 8;
            job.AuditDateTime = context.CreateDateTime;
            job.AuditOperatorId = context.User.Id;
            var fees = db.DocFees.Where(c => c.JobId == job.Id).ToList();
            fees.ForEach(c =>
            {
                c.AuditDateTime = context.CreateDateTime;
                c.AuditOperatorId = context.User.Id;
            });
            return true;
        }

        /// <summary>
        /// 取消审核工作任务并取消审核所有下属费用。
        /// </summary>
        /// <param name="job"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public bool UnAudit(PlJob job, OwContext context)
        {
            var db = context.ServiceProvider.GetRequiredService<PowerLmsUserDbContext>();
            if (job.JobState != 8)
            {
                OwHelper.SetLastErrorAndMessage(400, $"{nameof(job.JobState)}必须是8才能取消审核");
                return false;
            }
            job.JobState = 4;
            job.AuditDateTime = null;
            job.AuditOperatorId = null;
            var fees = db.DocFees.Where(c => c.JobId == job.Id).ToList();
            fees.ForEach(c =>
            {
                c.AuditDateTime = null;
                c.AuditOperatorId = null;
            });
            return true;
        }

        /// <summary>
        /// 用指定的任务Id获取其下属业务对象。
        /// </summary>
        /// <param name="jobId"></param>
        /// <param name="context"></param>
        /// <returns>没有找到则返回null。</returns>
        public IPlBusinessDoc GetBusinessDoc(Guid jobId, DbContext context)
        {
            if (context.Set<PlEaDoc>().FirstOrDefault(c => c.JobId == jobId) is PlEaDoc ea) return ea;
            if (context.Set<PlIaDoc>().FirstOrDefault(c => c.JobId == jobId) is PlIaDoc ia) return ia;
            if (context.Set<PlEsDoc>().FirstOrDefault(c => c.JobId == jobId) is PlEsDoc es) return es;
            if (context.Set<PlIsDoc>().FirstOrDefault(c => c.JobId == jobId) is PlIsDoc isDoc) return isDoc;
            //if (context.Set<PlIaDoc>().FirstOrDefault(c => c.JobId == jobId) is PlIaDoc ia) return ia;
            //if (context.Set<PlIaDoc>().FirstOrDefault(c => c.JobId == jobId) is PlIaDoc ia) return ia;
            return null;
        }
        #endregion 任务相关

        #region 财务日期填充

        /// <summary>
        /// 为一组工作任务对象填充财务日期。
        /// 批量处理，注重性能，避免N+1查询问题。
        /// </summary>
        /// <param name="jobs">工作任务对象集合</param>
        /// <param name="context">数据库上下文</param>
        public void FillFinancialDates(IEnumerable<PlJob> jobs, DbContext context)
        {
            if (jobs?.Any() != true) return;

            var jobList = jobs.ToList();
            var jobIds = jobList.Select(j => j.Id).ToHashSet();

            // 批量查询所有相关的业务方向，避免N+1问题
            var businessDirections = GetBusinessDirectionsBatch(jobIds, context);

            // 为每个job填充财务日期
            foreach (var job in jobList)
            {
                job.FinancialDate = CalculateFinancialDate(job, businessDirections.GetValueOrDefault(job.Id));
            }
        }

        /// <summary>
        /// 为单个工作任务对象填充财务日期
        /// </summary>
        /// <param name="job">工作任务对象</param>
        /// <param name="context">数据库上下文</param>
        public void FillFinancialDate(PlJob job, DbContext context)
        {
            if (job == null) return;

            // 优化：只查询业务方向，不获取完整单据对象
            var businessDirections = GetBusinessDirectionsBatch(new HashSet<Guid> { job.Id }, context);
            job.FinancialDate = CalculateFinancialDate(job, businessDirections.GetValueOrDefault(job.Id));
        }

        /// <summary>
        /// 批量获取业务单据类型，避免N+1查询
        /// 优化：只查询业务方向判断所需的最小数据，不返回完整单据对象
        /// </summary>
        /// <param name="jobIds">工作任务ID集合</param>
        /// <param name="context">数据库上下文</param>
        /// <returns>JobId到业务方向的映射（true=进口，false=出口，null=无单据）</returns>
        private Dictionary<Guid, bool?> GetBusinessDirectionsBatch(HashSet<Guid> jobIds, DbContext context)
        {
            var result = new Dictionary<Guid, bool?>();

            // 批量查询各类业务单据的类型信息（只查询JobId，不查询完整对象）
            // 空运出口单 - 出口方向
            var eaDocJobIds = context.Set<PlEaDoc>()
                .Where(d => d.JobId.HasValue && jobIds.Contains(d.JobId.Value))
                .Select(d => d.JobId.Value)
                .ToList();

            // 空运进口单 - 进口方向
            var iaDocJobIds = context.Set<PlIaDoc>()
                .Where(d => d.JobId.HasValue && jobIds.Contains(d.JobId.Value))
                .Select(d => d.JobId.Value)
                .ToList();

            // 海运出口单 - 出口方向
            var esDocJobIds = context.Set<PlEsDoc>()
                .Where(d => d.JobId.HasValue && jobIds.Contains(d.JobId.Value))
                .Select(d => d.JobId.Value)
                .ToList();

            // 海运进口单 - 进口方向
            var isDocJobIds = context.Set<PlIsDoc>()
                .Where(d => d.JobId.HasValue && jobIds.Contains(d.JobId.Value))
                .Select(d => d.JobId.Value)
                .ToList();

            // 填充结果：true=进口，false=出口
            foreach (var jobId in eaDocJobIds)
                result[jobId] = false; // 空运出口

            foreach (var jobId in iaDocJobIds)
                result[jobId] = true;  // 空运进口

            foreach (var jobId in esDocJobIds)
                result[jobId] = false; // 海运出口

            foreach (var jobId in isDocJobIds)
                result[jobId] = true;  // 海运进口

            return result;
        }

        /// <summary>
        /// 计算财务日期
        /// 优化版本：基于业务方向而非完整业务单据对象
        /// 业务规则：
        /// - 进口业务：财务日期 = 到港日期(ETA)
        /// - 出口业务：财务日期 = 开航日期(Etd)
        /// - 当对应日期为空时，财务日期也为空
        /// </summary>
        /// <param name="job">工作任务对象</param>
        /// <param name="isImport">业务方向：true=进口，false=出口，null=无业务单据</param>
        /// <returns>计算出的财务日期</returns>
        private DateTime? CalculateFinancialDate(PlJob job, bool? isImport)
        {
            if (!isImport.HasValue) return null;

            return isImport.Value switch
            {
                true => job.ETA,   // 进口业务：使用到港日期
                false => job.Etd   // 出口业务：使用开航日期
            };
        }

        #endregion 财务日期填充
    }
}
