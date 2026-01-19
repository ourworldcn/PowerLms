/*
 * 项目：PowerLms | 模块：空运出口业务数据实体
 * 功能：空运出口主单领入登记
 * 业务背景：
 *   航空公司向一级货运代理发放主单（MAWB），代理需登记领入情况。
 *   领入来源：1)直接向航司登记领用；2)向其他代理过单借领。
 * 技术要点：
 *   - 主单号格式：3位航司前缀+"-"+8位数字（含校验位）
 *   - 支持双格式存储：标准主单号（去空格）+ 显示主单号（保留空格）
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
    /// 空运出口主单领入登记表。
    /// 记录从航空公司或其他代理领入的主单信息。
    /// </summary>
    [Comment("空运出口主单领入登记表")]
    [Index(nameof(MawbNo), IsUnique = false)]
    [Index(nameof(OrgId), IsUnique = false)]
    public class PlEaMawbInbound : GuidKeyObjectBase, ICreatorInfo
    {
        #region 基础字段

        /// <summary>
        /// 所属机构Id。用于多租户数据隔离。
        /// </summary>
        [Comment("所属机构Id")]
        public Guid OrgId { get; set; }

        #endregion 基础字段

        #region 主单号相关

        /// <summary>
        /// 主单号（标准格式）。
        /// 格式：3位航司前缀+"-"+8位数字（含校验位），示例：CZ-12345670。
        /// 存储时已去除空格，用于数据库查询和业务逻辑处理。
        /// </summary>
        [Comment("主单号（标准格式，去空格）")]
        [MaxLength(20)]
        [Required]
        public string MawbNo { get; set; }

        /// <summary>
        /// 主单号（显示格式）。
        /// 保留原始输入的空格格式，用于前端显示。
        /// 部分航司使用4-4格式（如：CZ-1234 5670）。
        /// </summary>
        [Comment("主单号（显示格式，保留空格）")]
        [MaxLength(25)]
        public string MawbNoDisplay { get; set; }

        #endregion 主单号相关

        #region 领入来源

        /// <summary>
        /// 来源类型。
        /// 0=航司登记（直接向航空公司领用），1=过单代理（向其他代理借领）。
        /// </summary>
        [Comment("来源类型：0航司登记/1过单代理")]
        public int SourceType { get; set; }

        /// <summary>
        /// 航空公司Id。
        /// 关联客户资料表（PlCustomer），不建立物理外键约束。
        /// 当SourceType=0时必填。
        /// </summary>
        [Comment("航空公司Id（客户资料）")]
        public Guid? AirlineId { get; set; }

        /// <summary>
        /// 过单代理Id。
        /// 关联客户资料表（PlCustomer），不建立物理外键约束。
        /// 当SourceType=1时必填。
        /// </summary>
        [Comment("过单代理Id（客户资料）")]
        public Guid? TransferAgentId { get; set; }

        #endregion 领入来源

        #region 登记信息

        /// <summary>
        /// 登记日期。
        /// 记录主单领入的实际日期。
        /// </summary>
        [Comment("登记日期")]
        [Precision(3)]
        public DateTime RegisterDate { get; set; }

        /// <summary>
        /// 备注信息。
        /// 用于记录特殊说明或补充信息。
        /// </summary>
        [Comment("备注")]
        [MaxLength(500)]
        public string Remark { get; set; }

        #endregion 登记信息

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
