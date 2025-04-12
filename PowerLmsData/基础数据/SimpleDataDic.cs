/*
 * 简单数据字典，用于存储通用字典数据并关联到数据字典目录
 */
using Microsoft.EntityFrameworkCore;
using OW.Data;
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
    /// 简单数据字典条目类，用于存储按目录分类的通用字典数据。
    /// 继承自DataDicBase基类并添加了关联和创建信息。
    /// </summary>
    [Index(nameof(DataDicId), nameof(Code), IsUnique = false)]
    public class SimpleDataDic : DataDicBase, ICloneable
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public SimpleDataDic()
        {
        }

        /// <summary>
        /// 海关码。用于与海关EDI系统交换数据时的标识码。
        /// </summary>
        [Comment("海关码。项目类型决定有大量与海关的EDI行为，交换使用的码。")]
        [MaxLength(32), Unicode(false)]
        public string CustomsCode { get; set; }

        /// <summary>
        /// 创建人账号Id。记录创建此字典项的用户Id。
        /// </summary>
        [Comment("创建人账号Id")]
        public Guid? CreateAccountId { get; set; }

        /// <summary>
        /// 创建时间。记录字典项创建的时间点。
        /// </summary>
        [Comment("创建时间")]
        public DateTime? CreateDateTime { get; set; }

        /// <summary>
        /// 所属数据字典目录的Id。通过此字段与DataDicCatalog关联。
        /// </summary>
        [Comment("所属数据字典目录的Id")]
        public virtual Guid? DataDicId { get; set; }

        /// <summary>
        /// 创建此对象的浅表副本，具有相同的属性值但新的Id。
        /// </summary>
        /// <returns>返回除Id属性外，其余属性完全相同的对象副本。</returns>
        public object Clone()
        {
            // 创建并返回一个新的SimpleDataDic实例，复制当前对象的所有属性值
            return new SimpleDataDic
            {
                DataDicId = DataDicId,
                CreateAccountId = CreateAccountId,
                CreateDateTime = CreateDateTime,
                IsDelete = IsDelete,
                Code = Code,
                CustomsCode = CustomsCode,
                DisplayName = DisplayName,
                ShortcutName = ShortcutName,
                ShortName = ShortName,
                Remark = Remark, // 添加了Remark字段的复制
            };
        }
    }
}
