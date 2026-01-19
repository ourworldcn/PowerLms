/*
 * 项目：PowerLms | 模块：空运出口业务数据实体
 * 功能：空运出口主单领出登记
 * 业务背景：
 *   一级货运代理向二级代理发放主单，需登记领出情况。
 *   记录领出对象、领用人、领出时间及预计返还时间。
 * 技术要点：
 *   - 单张主单领出，不支持批量
 *   - 记录预计和实际返还日期，便于主单追踪
 *   - 不建立物理外键约束，保持业务灵活性
 * 作者：zc | 创建：2025-01-17
 */

using Microsoft.EntityFrameworkCore;
using OW.Data;
using System;
using System.ComponentModel.DataAnnotations;

namespace PowerLms.Data
{
    /// <summary>
    /// 空运出口主单领出登记表。
    /// 记录向二级代理发放主单的信息。
    /// </summary>
    [Comment("空运出口主单领出登记表")]
    [Index(nameof(MawbNo), IsUnique = false)]
    [Index(nameof(OrgId), IsUnique = false)]
    public class PlEaMawbOutbound : GuidKeyObjectBase, ICreatorInfo
    {
        #region 基础字段

        /// <summary>
        /// 所属机构Id。用于多租户数据隔离。
        /// </summary>
        [Comment("所属机构Id")]
        public Guid OrgId { get; set; }

        #endregion 基础字段

        #region 主单号

        /// <summary>
        /// 主单号（标准格式）。
        /// 格式：3位航司前缀+"-"+8位数字（含校验位），示例：CZ-12345670。
        /// 存储时已去除空格，用于数据库查询和业务逻辑处理。
        /// </summary>
        [Comment("主单号（标准格式，去空格）")]
        [MaxLength(20)]
        [Required]
        public string MawbNo { get; set; }

        #endregion 主单号

        #region 领出对象

        /// <summary>
        /// 领单代理Id。
        /// 关联客户资料表（PlCustomer），通常为二级代理。
        /// 不建立物理外键约束。
        /// </summary>
        [Comment("领单代理Id（客户资料，通常为二级代理）")]
        [Required]
        public Guid AgentId { get; set; }

        /// <summary>
        /// 领用人姓名。
        /// 记录实际领取主单的联系人姓名，支持多人用斜杠分隔（如：张三/李四）。
        /// </summary>
        [Comment("领用人姓名")]
        [MaxLength(50)]
        public string RecipientName { get; set; }

        #endregion 领出对象

        #region 时间信息

        /// <summary>
        /// 领用日期。
        /// 记录主单实际领出的日期。
        /// </summary>
        [Comment("领用日期")]
        [Precision(3)]
        public DateTime IssueDate { get; set; }

        /// <summary>
        /// 预计返回日期。
        /// 主单预计归还的日期，用于主单跟踪管理。
        /// </summary>
        [Comment("预计返回日期")]
        [Precision(3)]
        public DateTime? PlannedReturnDate { get; set; }

        /// <summary>
        /// 实际返回日期。
        /// 主单实际归还的日期，null表示尚未归还。
        /// </summary>
        [Comment("实际返回日期")]
        [Precision(3)]
        public DateTime? ActualReturnDate { get; set; }

        #endregion 时间信息

        #region 备注信息

        /// <summary>
        /// 备注信息。
        /// 用于记录特殊说明或补充信息。
        /// </summary>
        [Comment("备注")]
        [MaxLength(500)]
        public string Remark { get; set; }

        #endregion 备注信息

        #region ICreatorInfo接口实现

        /// <summary>
        /// 创建者Id。
        /// </summary>
        [Comment("创建者Id")]
        public Guid? CreateBy { get; set; }

        /// <summary>
        /// 创建时间。
        /// </summary>
        [Comment("创建时间")]
        [Precision(3)]
        public DateTime CreateDateTime { get; set; }

        #endregion ICreatorInfo接口实现
    }
}
