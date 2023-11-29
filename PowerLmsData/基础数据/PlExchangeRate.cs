using Microsoft.EntityFrameworkCore;
using OW.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerLms.Data
{
    /// <summary>
    /// 汇率。
    /// </summary>
    [Comment("汇率")]
    public class PlExchangeRate : GuidKeyObjectBase
    {
        /// <summary>
        /// 业务类型Id。
        /// </summary>
        [Comment("业务类型Id")]
        public Guid BusinessTypeId { get; set; }

        /// <summary>
        /// 所属组织机构Id。
        /// </summary>
        [Comment("所属组织机构Id")]
        public Guid? OrgId { get; set; }

        /// <summary>
        /// 源币种。
        /// </summary>
        [Comment("源币种")]
        public Guid SCurrencyId { get; set; }

        /// <summary>
        /// 宿币种。
        /// </summary>
        [Comment("宿币种")]
        public Guid DCurrencyId { get; set; }

        /// <summary>
        /// 基准，此处默认为100。
        /// </summary>
        [Comment("基准，此处默认为100")]
        public float Radix { get; set; } = 100;

        /// <summary>
        /// 兑换率。
        /// </summary>
        [Comment("兑换率")]
        public float Exchange { get; set; }

        /// <summary>
        /// 生效时点。
        /// </summary>
        [Comment("生效时点")]
        public DateTime BeginDate { get; set; }

        /// <summary>
        /// 失效时点。
        /// </summary>
        [Comment("失效时点")]
        public DateTime EndData { get; set; }
    }
}
