using Microsoft.EntityFrameworkCore;
using OW.Data;
using PowerLms.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace PowerLms.Data
{
    /// <summary>
    /// 专门针对数据字典的目录。
    /// </summary>
    [Comment("专门针对数据字典的目录。")]
    [Index(nameof(OrgId), nameof(Code), IsUnique = true)]
    public class DataDicCatalog : GuidKeyObjectBase, ICloneable, ISpecificOrg
    {
        /// <summary>
        /// 数据字典的代码。
        /// </summary>
        [MaxLength(32)]
        [Comment("数据字典的代码。")]
        public string Code { get; set; }
        /// <summary>
        /// 显示名称。
        /// </summary>
        [Comment("显示名称")]
        public string DisplayName { get; set; }
        /// <summary>
        /// 所属组织机构Id。通常这里为null则有不同解释，如通用的模板或超管使用的数据字典。
        /// </summary>
        [Comment("所属组织机构Id。通常这里为null则有不同解释，如通用的模板或超管使用的数据字典。")]
        public Guid? OrgId { get; set; }
        /// <summary>
        /// 为指定对象生成一个深表副本。
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            var result = new DataDicCatalog
            {
                Id = Id,
                Code = Code,
                DisplayName = DisplayName,
                OrgId = OrgId,
            };
            return result;
        }
    }
}
