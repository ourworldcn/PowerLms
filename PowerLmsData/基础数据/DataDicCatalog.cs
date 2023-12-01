﻿using Microsoft.EntityFrameworkCore;
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
    public class DataDicCatalog : GuidKeyObjectBase
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
        /// 数据字典的类型。1=简单字典；2=复杂字典；3=这是简单字典，但UI需要作为复杂字典处理（实际是掩码D0+D1）；其它值随后逐步定义。
        /// </summary>
        [Comment("数据字典的类型。1=简单字典；2=复杂字典；3=这是简单字典，但UI需要作为复杂字典处理（实际是掩码D0+D1）；其它值随后逐步定义。")]
        public int DataDicType { get; set; }

        /// <summary>
        /// 所属组织机构Id。通常这里为null则有不同解释，如通用的模板或超管使用的数据字典。
        /// </summary>
        [Comment("所属组织机构Id。通常这里为null则有不同解释，如通用的模板或超管使用的数据字典。")]
        public Guid? OrgId { get; set; }


    }
}
