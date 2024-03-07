using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
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
    /// 总业务数据类。
    /// </summary>
    public class PlJob : GuidKeyObjectBase
    {
        /// <summary>
        /// 所属机构Id。
        /// </summary>
        [Comment("所属机构Id")]
        public Guid? OrgId { get; set; }

        /// <summary>
        /// 工作号.
        /// </summary>
        [Comment("工作号")]
        public string JobNo { get; set; }

        /// <summary>
        /// 客户Id。
        /// </summary>
        [Comment("客户Id")]
        public Guid? CustomId { get; set; }

        /// <summary>
        /// 客户联系人。客户联系人可以从客户资料中维护的联系人中选择，也可以临时输入，所以这里不是关联联系人的id，是字符串
        /// </summary>
        [MaxLength(32)]
        [Comment("客户联系人")]
        public string LinkMan { get; set; }

        /// <summary>
        /// 联系人电话。客户联系人可以从客户资料中维护的联系人中选择，也可以临时输入，所以这里不是关联联系人的id，是字符串
        /// </summary>
        [MaxLength(24),DataType("varchar(24)"),Phone]
        [Comment("联系人电话")]
        public string LinkTel { get; set; }

        /// <summary>
        /// 联系人传真。客户联系人可以从客户资料中维护的联系人中选择，也可以临时输入，所以这里不是关联联系人的id，是字符串
        /// </summary>
        [MaxLength(24), DataType("varchar(24)"), Phone]
        [Comment("联系人传真")]
        public string LinkFax { get; set; }

        /// <summary>
        /// 发货人。
        /// </summary>
        [MaxLength(128)]
        [Comment("发货人")]
        public string Consignor { get; set; }

        /// <summary>
        /// 收货人。
        /// </summary>
        [MaxLength(128)]
        [Comment("收货人")]
        public string Consignee { get; set; }

        /// <summary>
        /// 通知人。
        /// </summary>
        [MaxLength(128)]
        [Comment("通知人")]
        public string Notify { get; set; }

        /// <summary>
        /// 代理人。
        /// </summary>
        [MaxLength(128)]
        [Comment("代理人")]
        public string Agent { get; set; }

        /// <summary>
        /// 主单ID.
        /// </summary>
        [Comment("主单ID")]
        public Guid? MblId { get; set; }

        /// <summary>
        /// 分单ID.
        /// </summary>
        [Comment("分单ID")]
        public Guid? HblId { get; set; }

        /// <summary>
        /// 揽货类型,简单字典HoldType
        /// </summary>
        [Comment("揽货类型,简单字典HoldType")]
        public Guid? HoldtypeId { get; set; }
    }
}
