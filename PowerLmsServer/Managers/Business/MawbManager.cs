/*
 * 项目：PowerLms货运物流管理系统 | 模块：主营业务/空运出口
 * 功能：主单（MAWB）领用登记与台账管理
 * 技术要点：
 *   - 主单号校验算法（3位前缀+8位数字+校验位）
 *   - 批量领入与单张领出业务逻辑
 *   - 台账全生命周期管理（领入→领出→业务使用→作废）
 *   - 多租户数据隔离与权限验证
 *   - 事务处理确保数据一致性
 * 作者：zc | 创建：2025-01 | 修改：2025-01-17 初始创建
 */
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PowerLms.Data;
using PowerLmsServer.EfData;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerLmsServer.Managers
{
    /// <summary>
    /// 主单（MAWB - Master Air Waybill）领用登记与台账管理器。
    /// 负责主单的领入登记、领出登记、台账查询、业务关联等全流程管理。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Scoped)]
    public class MawbManager
    {
        #region 构造函数与依赖注入
        /// <summary>
        /// 初始化MawbManager实例。
        /// </summary>
        /// <param name="dbContext">数据库上下文。</param>
        /// <param name="authManager">权限管理器。</param>
        /// <param name="orgManager">组织管理器。</param>
        /// <param name="accountManager">账号管理器。</param>
        /// <param name="logger">日志记录器。</param>
        public MawbManager(PowerLmsUserDbContext dbContext, AuthorizationManager authManager, OrgManager<PowerLmsUserDbContext> orgManager, AccountManager accountManager, ILogger<MawbManager> logger)
        {
            _DbContext = dbContext;
            _AuthManager = authManager;
            _OrgManager = orgManager;
            _AccountManager = accountManager;
            _Logger = logger;
        }
        readonly PowerLmsUserDbContext _DbContext;
        readonly AuthorizationManager _AuthManager;
        readonly OrgManager<PowerLmsUserDbContext> _OrgManager;
        readonly AccountManager _AccountManager;
        readonly ILogger<MawbManager> _Logger;
        #endregion

        #region 主单号校验与生成

        /// <summary>
        /// 校验主单号格式与校验位（IATA国际标准）。
        /// </summary>
        /// <param name="mawbNo">主单号（支持"999-12345678"或"999-1234 5678"两种格式）。</param>
        /// <returns>
        /// 元组：
        /// - isValid: 是否有效
        /// - errorMsg: 错误信息（有效时为空）
        /// </returns>
        /// <remarks>
        /// 校验规则（IATA国际标准）：
        /// 1. 格式：3位航司代码 + "-" + 8位数字（第8位为校验位）
        /// 2. 支持格式："999-12345678"或"999-1234 5678"
        /// 3. 校验位算法：(前7位数字 - 第8位数字) % 7 == 0
        /// 4. 自动兼容空格输入，内部标准化处理
        /// </remarks>
        public (bool isValid, string errorMsg) ValidateMawbNo(string mawbNo)
        {
            if (string.IsNullOrWhiteSpace(mawbNo))
                return (false, "主单号不能为空");
            var normalized = mawbNo.Replace(" ", "").Replace("-", "");
            if (normalized.Length != 11)
                return (false, "主单号长度不正确，应为3位前缀+8位数字（共11位）");
            var prefix = normalized.Substring(0, 3);
            var numbers = normalized.Substring(3, 8);
            if (!prefix.All(char.IsDigit))
                return (false, "前缀必须为3位数字");
            if (!numbers.All(char.IsDigit))
                return (false, "后8位必须全为数字");
            if (!int.TryParse(numbers.Substring(0, 7), out var first7) || !int.TryParse(numbers.Substring(7, 1), out var checkDigit))
                return (false, "主单号数字格式错误");
            if ((first7 - checkDigit) % 7 != 0)
                return (false, "校验位不正确");
            return (true, string.Empty);
        }

        /// <summary>
        /// 根据当前主单号生成下一个主单号（IATA国际标准）。
        /// </summary>
        /// <param name="prefix">3位航司代码（如"999"）。</param>
        /// <param name="currentNo">当前8位数字部分（如"12345678"）。</param>
        /// <returns>下一个完整主单号（如"999-12345685"）。</returns>
        /// <exception cref="InvalidOperationException">当序列号达到最大值（9999999）无法继续生成时抛出。</exception>
        /// <remarks>
        /// 算法（IATA国际标准）：
        /// 1. 前7位+1
        /// 2. 计算新的第8位校验位，使得 (新前7位 - 第8位) % 7 == 0
        /// 3. 即：第8位 = 新前7位 % 7
        /// 
        /// 边界处理：
        /// - 当前7位达到9999999时，+1会溢出到8位数字（10000000）
        /// - 此时抛出InvalidOperationException，提示序列已达上限
        /// </remarks>
        public string GenerateNextMawbNo(string prefix, string currentNo)
        {
            if (currentNo.Length != 8 || !currentNo.All(char.IsDigit))
                throw new ArgumentException("当前主单号必须为8位数字", nameof(currentNo));
            if (prefix.Length != 3 || !prefix.All(char.IsDigit))
                throw new ArgumentException("前缀必须为3位数字", nameof(prefix));
            var first7 = int.Parse(currentNo.Substring(0, 7));
            if (first7 >= 9999999)
                throw new InvalidOperationException($"主单号序列已达到最大值（{prefix}-99999996），无法继续生成");
            first7++;
            var newCheckDigit = first7 % 7;
            var newNumbers = first7.ToString().PadLeft(7, '0') + newCheckDigit;
            return $"{prefix}-{newNumbers}";
        }

        /// <summary>
        /// 批量生成主单号序列。
        /// </summary>
        /// <param name="prefix">3位航司代码。</param>
        /// <param name="startNo">起始主单号（8位数字部分）。
        /// <strong>重要：</strong>前端传入的是<strong>本次批量生成的第一个号</strong>，不是已存在的号。
        /// 返回的序列<strong>从该号开始</strong>，包含该号本身。
        /// 例如：传入"12345670"，返回的第一个号就是"999-12345670"（包含传入的号）。</param>
        /// <param name="count">生成数量。</param>
        /// <returns>主单号列表（从startNo开始，共count个，包含startNo本身）。</returns>
        /// <exception cref="InvalidOperationException">当生成过程中序列号超过最大值（9999999）时抛出。</exception>
        /// <example>
        /// 示例：
        /// <code>
        /// // 用户要求从 "999-12345670" 开始生成3个主单号
        /// var result = BatchGenerateMawbNos("999", "12345670", 3);
        /// // 返回：["999-12345670", "999-12345681", "999-12345692"]
        /// // 注意：包含传入的"999-12345670"，这是第一个号
        /// </code>
        /// </example>
        public List<string> BatchGenerateMawbNos(string prefix, string startNo, int count)
        {
            if (count <= 0)
                throw new ArgumentException("生成数量必须大于0", nameof(count));
            var validation = ValidateMawbNo($"{prefix}-{startNo}");
            if (!validation.isValid)
                throw new ArgumentException($"起始主单号不合法: {validation.errorMsg}");
            var result = new List<string>(count);
            var first7 = int.Parse(startNo.Substring(0, 7));
            if (first7 + count - 1 > 9999999)
                throw new InvalidOperationException($"批量生成会导致序列号超过最大值9999999，起始号={first7}，数量={count}");
            for (int i = 0; i < count; i++)
            {
                var checkDigit = first7 % 7;
                var currentNo = first7.ToString().PadLeft(7, '0') + checkDigit;
                result.Add($"{prefix}-{currentNo}");
                first7++;
            }
            return result;
        }
        #endregion

        #region 主单号标准化处理

        /// <summary>
        /// 标准化主单号（去除空格，保留连字符）。
        /// </summary>
        /// <param name="mawbNo">原始主单号（支持"999-12345678"或"999-1234 5678"格式）。</param>
        /// <returns>标准化后的主单号（格式：前3位-后8位，如"999-12345678"）。</returns>
        /// <remarks>
        /// IATA国际标准：连字符"-"位置固定，必须保留。
        /// 支持输入格式："999-12345678"或"999-1234 5678"。
        /// </remarks>
        public string NormalizeMawbNo(string mawbNo)
        {
            if (string.IsNullOrWhiteSpace(mawbNo))
                return string.Empty;
            var cleaned = mawbNo.Replace(" ", "");
            if (cleaned.Length == 11 && !cleaned.Contains("-"))
            {
                return $"{cleaned.Substring(0, 3)}-{cleaned.Substring(3, 8)}";
            }
            return cleaned;
        }

        /// <summary>
        /// 格式化主单号为标准显示格式（IATA标准：前3位-后8位）。
        /// </summary>
        /// <param name="mawbNo">主单号（支持"999-12345678"或"999-1234 5678"格式）。</param>
        /// <returns>格式化后的主单号（如"999-12345678"）。</returns>
        public string FormatMawbNo(string mawbNo)
        {
            var normalized = NormalizeMawbNo(mawbNo);
            if (normalized.Length == 12 && normalized[3] == '-')
                return normalized;
            if (normalized.Length == 11)
                return $"{normalized.Substring(0, 3)}-{normalized.Substring(3, 8)}";
            return mawbNo;
        }

        #endregion

        #region 主单领入模块

        /// <summary>
        /// 批量创建主单领入记录。
        /// </summary>
        /// <param name="sourceType">来源类型（0=航司登记，1=过单代理）。</param>
        /// <param name="airlineId">航空公司Id（当SourceType=0时使用）。</param>
        /// <param name="transferAgentId">过单代理Id（当SourceType=1时使用）。</param>
        /// <param name="registerDate">登记日期。</param>
        /// <param name="remark">备注。</param>
        /// <param name="mawbNos">主单号列表（支持"999-12345678"或"999-1234 5678"格式）。</param>
        /// <param name="orgId">所属机构Id。</param>
        /// <param name="createBy">创建人Id。</param>
        /// <returns>元组：(成功数量, 失败数量, 失败详情列表)。</returns>
        public (int successCount, int failureCount, List<string> failureDetails) CreateInbound(
            int sourceType,
            Guid? airlineId,
            Guid? transferAgentId,
            DateTime registerDate,
            string remark,
            List<string> mawbNos,
            Guid orgId,
            Guid createBy)
        {
            var successCount = 0;
            var failureCount = 0;
            var failureDetails = new List<string>();
            var createDateTime = OwHelper.WorldNow;
            foreach (var mawbNoDisplay in mawbNos)
            {
                try
                {
                    var validation = ValidateMawbNo(mawbNoDisplay);
                    if (!validation.isValid)
                    {
                        failureCount++;
                        failureDetails.Add($"主单号 [{mawbNoDisplay}] 校验失败: {validation.errorMsg}");
                        continue;
                    }
                    var normalizedMawbNo = NormalizeMawbNo(mawbNoDisplay);
                    var existing = _DbContext.PlEaMawbInbounds
                        .AsNoTracking()
                        .FirstOrDefault(x => x.MawbNo == normalizedMawbNo && x.OrgId == orgId);
                    if (existing != null)
                    {
                        failureCount++;
                        failureDetails.Add($"主单号 [{mawbNoDisplay}] 已存在，不能重复领入");
                        continue;
                    }
                    var inbound = new PlEaMawbInbound
                    {
                        Id = Guid.NewGuid(),
                        OrgId = orgId,
                        MawbNo = normalizedMawbNo,
                        MawbNoDisplay = mawbNoDisplay,
                        SourceType = sourceType,
                        AirlineId = airlineId,
                        TransferAgentId = transferAgentId,
                        RegisterDate = registerDate,
                        Remark = remark,
                        CreateBy = createBy,
                        CreateDateTime = createDateTime
                    };
                    _DbContext.PlEaMawbInbounds.Add(inbound);
                    successCount++;
                }
                catch (Exception ex)
                {
                    failureCount++;
                    failureDetails.Add($"主单号 [{mawbNoDisplay}] 创建失败: {ex.Message}");
                    _Logger.LogError(ex, "创建主单领入记录时发生异常: {MawbNo}", mawbNoDisplay);
                }
            }
            if (successCount > 0)
            {
                try
                {
                    _DbContext.SaveChanges();
                }
                catch (Exception ex)
                {
                    _Logger.LogError(ex, "保存主单领入记录时发生异常");
                    failureCount += successCount;
                    successCount = 0;
                    failureDetails.Add($"数据库保存失败: {ex.Message}");
                }
            }
            return (successCount, failureCount, failureDetails);
        }

        // TODO: 1.1.2.6 实现GetInboundList方法

        /// <summary>
        /// 修改主单领入记录。
        /// </summary>
        /// <param name="id">领入记录Id。</param>
        /// <param name="airlineId">航空公司Id。</param>
        /// <param name="transferAgentId">过单代理Id。</param>
        /// <param name="registerDate">登记日期。</param>
        /// <param name="remark">备注。</param>
        /// <param name="orgId">所属机构Id（用于权限验证）。</param>
        /// <returns>是否成功。</returns>
        public bool UpdateInbound(
            Guid id,
            Guid? airlineId,
            Guid? transferAgentId,
            DateTime? registerDate,
            string remark,
            Guid orgId)
        {
            var inbound = _DbContext.PlEaMawbInbounds.FirstOrDefault(x => x.Id == id && x.OrgId == orgId);
            if (inbound == null)
            {
                _Logger.LogWarning("主单领入记录不存在或无权访问: {Id}", id);
                return false;
            }
            if (airlineId.HasValue)
                inbound.AirlineId = airlineId.Value;
            if (transferAgentId.HasValue)
                inbound.TransferAgentId = transferAgentId.Value;
            if (registerDate.HasValue)
                inbound.RegisterDate = registerDate.Value;
            if (!string.IsNullOrEmpty(remark))
                inbound.Remark = remark;
            try
            {
                _DbContext.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "修改主单领入记录时发生异常: {Id}", id);
                return false;
            }
        }

        /// <summary>
        /// 删除主单领入记录。
        /// </summary>
        /// <param name="id">领入记录Id。</param>
        /// <param name="orgId">所属机构Id（用于权限验证）。</param>
        /// <returns>元组：(是否成功, 错误信息)。</returns>
        public (bool success, string errorMsg) DeleteInbound(Guid id, Guid orgId)
        {
            var inbound = _DbContext.PlEaMawbInbounds.FirstOrDefault(x => x.Id == id && x.OrgId == orgId);
            if (inbound == null)
            {
                return (false, "主单领入记录不存在或无权访问");
            }
            var outbound = _DbContext.PlEaMawbOutbounds
                .AsNoTracking()
                .FirstOrDefault(x => x.MawbNo == inbound.MawbNo && x.OrgId == orgId);
            if (outbound != null)
            {
                return (false, "主单已领出，不能删除领入记录");
            }
            try
            {
                _DbContext.PlEaMawbInbounds.Remove(inbound);
                _DbContext.SaveChanges();
                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "删除主单领入记录时发生异常: {Id}", id);
                return (false, $"删除失败: {ex.Message}");
            }
        }
        #endregion

        #region 主单领出模块

        /// <summary>
        /// 创建主单领出记录（单张主单）。
        /// </summary>
        /// <param name="mawbNo">主单号（支持"999-12345678"或"999-1234 5678"格式，将自动标准化）。</param>
        /// <param name="agentId">领单代理Id。</param>
        /// <param name="recipientName">领用人姓名。</param>
        /// <param name="issueDate">领用日期。</param>
        /// <param name="plannedReturnDate">预计返回日期。</param>
        /// <param name="remark">备注。</param>
        /// <param name="orgId">所属机构Id。</param>
        /// <param name="createBy">创建人Id。</param>
        /// <returns>元组：(是否成功, 错误信息, 新记录Id)。</returns>
        public (bool success, string errorMsg, Guid? id) CreateOutbound(
            string mawbNo,
            Guid agentId,
            string recipientName,
            DateTime issueDate,
            DateTime? plannedReturnDate,
            string remark,
            Guid orgId,
            Guid createBy)
        {
            var validation = ValidateMawbNo(mawbNo);
            if (!validation.isValid)
            {
                return (false, $"主单号格式错误: {validation.errorMsg}", null);
            }
            var normalizedMawbNo = NormalizeMawbNo(mawbNo);
            var inbound = _DbContext.PlEaMawbInbounds
                .AsNoTracking()
                .FirstOrDefault(x => x.MawbNo == normalizedMawbNo && x.OrgId == orgId);
            if (inbound == null)
            {
                return (false, "主单号不存在，请先进行领入登记", null);
            }
            var existingOutbound = _DbContext.PlEaMawbOutbounds
                .AsNoTracking()
                .FirstOrDefault(x => x.MawbNo == normalizedMawbNo && x.OrgId == orgId);
            if (existingOutbound != null)
            {
                return (false, "主单已领出，不能重复领出", null);
            }
            try
            {
                var outbound = new PlEaMawbOutbound
                {
                    Id = Guid.NewGuid(),
                    OrgId = orgId,
                    MawbNo = normalizedMawbNo,
                    AgentId = agentId,
                    RecipientName = recipientName,
                    IssueDate = issueDate,
                    PlannedReturnDate = plannedReturnDate,
                    Remark = remark,
                    CreateBy = createBy,
                    CreateDateTime = OwHelper.WorldNow
                };
                _DbContext.PlEaMawbOutbounds.Add(outbound);
                _DbContext.SaveChanges();
                return (true, string.Empty, outbound.Id);
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "创建主单领出记录时发生异常: {MawbNo}", mawbNo);
                return (false, $"创建失败: {ex.Message}", null);
            }
        }

        /// <summary>
        /// 修改主单领出记录。
        /// </summary>
        /// <param name="id">领出记录Id。</param>
        /// <param name="agentId">领单代理Id。</param>
        /// <param name="recipientName">领用人姓名。</param>
        /// <param name="issueDate">领用日期。</param>
        /// <param name="plannedReturnDate">预计返回日期。</param>
        /// <param name="actualReturnDate">实际返回日期。</param>
        /// <param name="remark">备注。</param>
        /// <param name="orgId">所属机构Id（用于权限验证）。</param>
        /// <returns>是否成功。</returns>
        public bool UpdateOutbound(
            Guid id,
            Guid? agentId,
            string recipientName,
            DateTime? issueDate,
            DateTime? plannedReturnDate,
            DateTime? actualReturnDate,
            string remark,
            Guid orgId)
        {
            var outbound = _DbContext.PlEaMawbOutbounds.FirstOrDefault(x => x.Id == id && x.OrgId == orgId);
            if (outbound == null)
            {
                _Logger.LogWarning("主单领出记录不存在或无权访问: {Id}", id);
                return false;
            }
            if (agentId.HasValue)
                outbound.AgentId = agentId.Value;
            if (!string.IsNullOrEmpty(recipientName))
                outbound.RecipientName = recipientName;
            if (issueDate.HasValue)
                outbound.IssueDate = issueDate.Value;
            if (plannedReturnDate.HasValue)
                outbound.PlannedReturnDate = plannedReturnDate;
            if (actualReturnDate.HasValue)
                outbound.ActualReturnDate = actualReturnDate;
            if (!string.IsNullOrEmpty(remark))
                outbound.Remark = remark;
            try
            {
                _DbContext.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "修改主单领出记录时发生异常: {Id}", id);
                return false;
            }
        }

        /// <summary>
        /// 删除主单领出记录。
        /// </summary>
        /// <param name="id">领出记录Id。</param>
        /// <param name="orgId">所属机构Id（用于权限验证）。</param>
        /// <returns>元组：(是否成功, 错误信息)。</returns>
        public (bool success, string errorMsg) DeleteOutbound(Guid id, Guid orgId)
        {
            var outbound = _DbContext.PlEaMawbOutbounds.FirstOrDefault(x => x.Id == id && x.OrgId == orgId);
            if (outbound == null)
            {
                return (false, "主单领出记录不存在或无权访问");
            }
            try
            {
                _DbContext.PlEaMawbOutbounds.Remove(outbound);
                _DbContext.SaveChanges();
                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "删除主单领出记录时发生异常: {Id}", id);
                return (false, $"删除失败: {ex.Message}");
            }
        }
        #endregion

        #region 台账管理模块
        // TODO: 1.1.2.13 实现GetLedgerList方法
        // TODO: 1.1.2.14 实现GetUnusedMawbList方法
        // TODO: 1.1.2.15 实现MarkAsUsed方法
        // TODO: 1.1.2.16 实现MarkAsVoid方法
        #endregion

        #region 业务关联模块
        // TODO: 1.1.2.17 实现GetJobInfoByMawbNo方法
        #endregion
    }
}
