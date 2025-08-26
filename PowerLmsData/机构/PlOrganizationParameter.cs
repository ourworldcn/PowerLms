/*
 * 项目：PowerLms | 模块：机构参数管理
 * 功能：机构级别参数配置，包括账期管理和报表设置
 * 技术要点：OrgId作为主键，独立实体，账期自动递增
 * 作者：zc | 创建：2025-01 | 修改：2025-01-27 实施机构参数表
 */

using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PowerLms.Data
{
    /// <summary>
    /// 机构参数表，存储机构级别的配置信息。
    /// </summary>
    [Table("PlOrganizationParameters")]
    [Comment("机构参数表，存储机构级别的配置信息")]
    public class PlOrganizationParameter
    {
        /// <summary>
        /// 机构ID，主键。
        /// </summary>
        [Key]
        [Comment("机构ID，主键")]
        public Guid OrgId { get; set; }
        
        /// <summary>
        /// 当前账期，格式YYYYMM（如202501）。只读字段，由"关闭账期"操作更新。
        /// </summary>
        [MaxLength(6)]
        [Comment("当前账期，格式YYYYMM，只读，由关闭账期操作更新")]
        public string CurrentAccountingPeriod { get; set; }
        
        /// <summary>
        /// 账单抬头1，用于报表打印。
        /// </summary>
        [MaxLength(100)]
        [Comment("账单抬头1，用于报表打印")]
        public string BillHeader1 { get; set; }
        
        /// <summary>
        /// 账单抬头2，用于报表打印。
        /// </summary>
        [MaxLength(100)]
        [Comment("账单抬头2，用于报表打印")]
        public string BillHeader2 { get; set; }
        
        /// <summary>
        /// 账单落款，用于报表打印。
        /// </summary>
        [MaxLength(100)]
        [Comment("账单落款，用于报表打印")]
        public string BillFooter { get; set; }
    }
}