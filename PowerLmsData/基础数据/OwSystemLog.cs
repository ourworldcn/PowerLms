using Microsoft.EntityFrameworkCore;
using OW.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerLms.Data
{
    /// <summary>
    /// 通用数据库存储的日志实体对象。
    /// 此类可能放在玩家数据库中也可能放于专用的日志库中，但可能有些游戏内操作需要此数据。
    /// 当前没有启动第二上下文，暂时放在业务数据库中。
    /// </summary>
    [Index(nameof(OrgId), nameof(ActionId), nameof(WorldDateTime), IsUnique = false)]
    [Index(nameof(OrgId), nameof(WorldDateTime), nameof(ActionId), IsUnique = false)]
    [Comment("通用数据库存储的日志实体对象。")]
    public class OwSystemLog : JsonDynamicPropertyBase
    {
        public OwSystemLog()
        {
        }

        public OwSystemLog(Guid id) : base(id)
        {
        }

        /// <summary>
        /// 所属机构Id。
        /// </summary>
        [Comment("所属机构Id")]
        public Guid? OrgId { get; set; }

        /// <summary>
        /// 行为Id。如操作名.实体名.Id。
        /// </summary>
        [MaxLength(64)]
        [Comment("行为Id。如操作名.实体名.Id")]
        public string ActionId { get; set; }

        /// <summary>
        /// 这个行为发生的世界时间。
        /// </summary>
        /// <value>默认是构造此对象的<see cref="OwHelper.WorldNow"/>时间。</value>
        [Comment("这个行为发生的世界时间。")]
        [Precision(3)]
        public DateTime WorldDateTime { get; set; } = OwHelper.WorldNow;

        /// <summary>
        /// 额外Guid。
        /// </summary>
        [Comment("额外Guid。")]
        public Guid? ExtraGuid { get; set; }

        /// <summary>
        /// 额外的字符串，通常行为Id，最长64字符。
        /// </summary>
        [MaxLength(64)]
        [Comment("额外的字符串，通常行为Id，最长64字符。")]
        public string ExtraString { get; set; }

        /// <summary>
        /// 额外数字，具体意义取决于该条记录的类型。
        /// </summary>
        [Precision(18, 4)]
        [Comment("额外数字，具体意义取决于该条记录的类型。")]
        public decimal ExtraDecimal { get; set; }

    }

}
