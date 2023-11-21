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
    /// 简单数据字典条目类。
    /// </summary>
    [Index(nameof(OrgId), nameof(DataDicId))]   //大量情况是在特定机构下的
    public class SimpleDataDic : DataDicBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public SimpleDataDic()
        {

        }

        /// <summary>
        /// 海关码。项目类型决定有大量与海关的EDI行为，交换使用的码。
        /// </summary>
        [Comment("海关码。项目类型决定有大量与海关的EDI行为，交换使用的码。")]
        public string CustomsCode { get; set; }

        /// <summary>
        /// 所属数据字典的的Id。
        /// </summary>
        [Comment("所属数据字典的的Id")]
        public virtual Guid? DataDicId { get; set; }

        /// <summary>
        /// 创建人账号Id。
        /// </summary>
        [Comment("创建人账号Id")]
        public Guid? CreateAccountId { get; set; }

        /// <summary>
        /// 创建时间。
        /// </summary>
        [Comment("创建时间")]
        public DateTime? CreateDateTime { get; set; }
    }
}
