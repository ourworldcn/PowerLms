using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using NPOI.SS.Formula.Functions;
using PowerLms.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PowerLmsServer.Managers
{
    /// <summary>
    /// 编号生成管理器。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Singleton)]
    public class JobNumberManager
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
                    string tmp = new string('0', c.Length - 2);
                    return account.JobNumber.Value.ToString(tmp);
                }
            });
            //序号替换
            str = Regex.Replace(str, @"\<[0]*?\>", c =>
            {
                if (!c.Success) return string.Empty;
                if (c.Length < 1) return string.Empty;
                string tmp = new string('0', c.Length - 2);
                return seq.ToString(tmp);
            });
            return str;
        }

    }
}
