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

    }

    /// <summary>
    /// 数据字典条目的基础类。
    /// </summary>
    public abstract class DataDicBase : GuidKeyObjectBase, IDataDic
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
        [MaxLength(64)]
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
        [Column(TypeName = "char"), MaxLength(8)]   //8个ASCII字符不足的尾部填充空格
        public virtual string ShortcutName { get; set; }

    }
}
