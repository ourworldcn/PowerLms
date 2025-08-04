using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerLms.Data.OA
{
    /// <summary>
    /// 凭证序号管理表，用于管理凭证号的序号递增
    /// </summary>
    [Comment("凭证序号管理表")]
    public class VoucherSequence
    {
        /// <summary>
        /// 组织机构ID，多租户隔离，联合主键第一字段
        /// </summary>
        [Required]
        [Comment("组织机构ID，多租户隔离")]
        public Guid OrgId { get; set; }

        /// <summary>
        /// 月份，联合主键第二字段
        /// </summary>
        [Required]
        [Comment("月份")]
        public int Month { get; set; }

        /// <summary>
        /// 凭证字，联合主键第三字段
        /// 直接存储凭证字，避免关联银行信息表，因为凭证字可能会变化
        /// </summary>
        [Required]
        [MaxLength(10)]
        [Comment("凭证字，直接存储避免关联银行信息")]
        public string VoucherCharacter { get; set; }

        /// <summary>
        /// 当前最大序号
        /// </summary>
        [Required]
        [Comment("当前最大序号")]
        public int MaxSequence { get; set; }

        /// <summary>
        /// 行版本，用于乐观锁控制
        /// </summary>
        [Timestamp]
        [Comment("行版本，用于乐观锁控制")]
        public byte[] RowVersion { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [Comment("创建时间")]
        public DateTime CreateDateTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 最后更新时间
        /// </summary>
        [Comment("最后更新时间")]
        public DateTime LastUpdateDateTime { get; set; } = DateTime.Now;
    }
}
