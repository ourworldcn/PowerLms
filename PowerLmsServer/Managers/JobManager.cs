using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using Microsoft.IdentityModel.Protocols;
using NPOI.SS.Formula.Functions;
using Org.BouncyCastle.Asn1.IsisMtt.X509;
using PowerLms.Data;
using PowerLmsServer.EfData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    }
}
