﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
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
    /// 数据字典的标识接口。
    /// </summary>
    public interface IDataDic
    {
        /// <summary>
        /// 编码。对本系统有一定意义的编码。
        /// </summary>
        [Comment("编码，对本系统有一定意义的编码")]
        [Column(TypeName = "varchar"), MaxLength(32), Required(AllowEmptyStrings = false)]   //最多32个ASCII字符
        public abstract string Code { get; set; }

    }

    /// <summary>
    /// 可标记删除实体接口。
    /// </summary>
    public interface IMarkDelete
    {
        /// <summary>
        /// 是否已标记为删除。false(默认)未标记为删除，true标记为删除。
        /// </summary>
        public bool IsDelete { get; set; }
    }

    /// <summary>
    /// 指定对象特定于组织机构。
    /// </summary>
    public interface ISpecificOrg
    {
        /// <summary>
        /// 组织机构的Id。没有则标识是超管的全局管理范围。
        /// </summary>
        public Guid? OrgId { get; set; }
    }

    /// <summary>
    /// 数据字典条目的基础类。
    /// </summary>
    public abstract class DataDicBase : GuidKeyObjectBase, IDataDic, IMarkDelete
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public DataDicBase()
        {

        }

        /// <summary>
        /// 编码。对本系统有一定意义的编码。
        /// </summary>
        [Comment("编码，对本系统有一定意义的编码")]
        [Column(TypeName = "varchar"), MaxLength(32), Required(AllowEmptyStrings = false)]   //最多32个ASCII字符
        public virtual string Code { get; set; }

        /// <summary>
        /// 显示的名称。
        /// </summary>
        [Comment("显示的名称")]
        [MaxLength(128)]
        public virtual string DisplayName { get; set; }

        /// <summary>
        /// 缩写名。
        /// </summary>
        [Comment("缩写名")]
        [MaxLength(32)]
        public string ShortName { get; set; }

        /// <summary>
        /// 快捷输入名。如"as6"则在键盘输入按as6能选择到此项，8个ASCII字符不足的尾部填充空格。服务器并不使用该字段。
        /// </summary>
        [Comment("快捷输入名")]
        [Column(TypeName = "varchar"), MaxLength(8)]
        public virtual string ShortcutName { get; set; }

        /// <summary>
        /// 备注.
        /// </summary>
        [Comment("备注")]
        public string Remark { get; set; }

        /// <summary>
        /// 是否已标记为删除。false(默认)未标记为删除，true标记为删除。
        /// </summary>
        [Comment("是否已标记为删除。false(默认)未标记为删除，true标记为删除。")]
        public bool IsDelete { get; set; }

    }

    /// <summary>
    /// 特殊字典的基类。
    /// </summary>
    public abstract class SpecialDataDicBase : GuidKeyObjectBase, IMarkDelete, ISpecificOrg
    {
        /// <summary>
        /// 所属组织机构Id。
        /// </summary>
        [Comment("所属组织机构Id")]
        public Guid? OrgId { get; set; }

        /// <summary>
        /// 快捷输入名。如"as6"则在键盘输入按as6能选择到此项。服务器并不使用该字段。
        /// </summary>
        [Comment("快捷输入名")]
        [Column(TypeName = "varchar"), MaxLength(8)]
        public virtual string ShortcutName { get; set; }

        /// <summary>
        /// 是否已标记为删除。false(默认)未标记为删除，true标记为删除。
        /// </summary>
        [Comment("是否已标记为删除。false(默认)未标记为删除，true标记为删除。")]
        public bool IsDelete { get; set; }

    }

    /// <summary>
    /// 带多个命名的特殊字典的基类。
    /// </summary>
    public abstract class NamedSpecialDataDicBase : SpecialDataDicBase, IMarkDelete
    {
        /// <summary>
        /// 编码。对本系统有一定意义的编码。
        /// </summary>
        [Comment("编码，对本系统有一定意义的编码")]
        [Column(TypeName = "varchar"), MaxLength(32), Required(AllowEmptyStrings = false)]   //最多32个ASCII字符
        public virtual string Code { get; set; }

        /// <summary>
        /// 显示的名称。
        /// </summary>
        [Comment("显示的名称")]
        [MaxLength(128)]
        public virtual string DisplayName { get; set; }

        /// <summary>
        /// 缩写名。
        /// </summary>
        [Comment("缩写名")]
        [MaxLength(32)]
        public string ShortName { get; set; }

        /// <summary>
        /// 备注.
        /// </summary>
        [Comment("备注")]
        public string Remark { get; set; }
    }
}
