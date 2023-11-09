/*
 * 与人员相关的字典表
 */
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerLms.Data
{
    /// <summary>
    /// 工作状态字典。
    /// </summary>
    public class WorkingStatusDataDic: DataDicBase
    {
        /// <summary>
        /// 工作状态编码。
        /// </summary>
        [Comment("工作状态编码")]
        [Column(TypeName = "varchar"), MaxLength(12)]   //至多12个ASCII字符
        public string Code { get; set; }

        /// <summary>
        /// 工作状态的多语言Id。
        /// </summary>
        [Comment("工作状态的多语言Id")]
        [MaxLength(64)]
        public override string DisplayNameMlId { get; set; }
    }

    /// <summary>
    /// 在职状态字典。
    /// </summary>
    public class IncumbencyDataDic: DataDicBase
    {
        /// <summary>
        /// 在职状态编码。
        /// </summary>
        [Comment("在职状态编码")]
        [Column(TypeName = "varchar"), MaxLength(12)]   //至多12个ASCII字符
        public string Code { get; set; }

        /// <summary>
        /// 在职状态名称多语言Id。
        /// </summary>
        [Comment("在职状态名称多语言Id")]
        [MaxLength(64)]
        public override string DisplayNameMlId { get; set; }
    }

    /// <summary>
    /// 性别字典。
    /// </summary>
    public class GenderDataDic: DataDicBase
    {
        /// <summary>
        /// 性别编码。
        /// </summary>
        [Comment("在职状态编码")]
        [Column(TypeName = "varchar"), MaxLength(12)]   //至多12个ASCII字符
        public string Code { get; set; }

        /// <summary>
        /// 性别的多语言Id。
        /// </summary>
        [Comment("性别的多语言Id")]
        [MaxLength(64)]
        public override string DisplayNameMlId { get; set; }
    }

    /// <summary>
    /// 学历字典。
    /// </summary>
    public class QualificationsDataDic : DataDicBase
    {
        /// <summary>
        /// 性别编码。
        /// </summary>
        [Comment("学历编码")]
        [Column(TypeName = "varchar"), MaxLength(12)]   //至多12个ASCII字符
        public string Code { get; set; }

        /// <summary>
        /// 学历名称的多语言Id。
        /// </summary>
        [Comment("学历名称的多语言Id")]
        [MaxLength(64)]
        public override string DisplayNameMlId { get; set; }
    }
}
