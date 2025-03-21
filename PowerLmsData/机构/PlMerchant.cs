﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OW.Data;
using PowerLmsServer.EfData;
using System.Collections.Concurrent;
using System.Text.Json.Serialization;

namespace PowerLms.Data
{
    /// <summary>
    /// 商户。
    /// </summary>
    [Comment("商户")]
    public class PlMerchant : GuidKeyObjectBase, IMarkDelete, ICreatorInfo
    {
        /// <summary>
        /// 名称类。
        /// </summary>
        [Comment("名称嵌入类")]
        public PlOwnedName Name { get; set; }

        /// <summary>
        /// 描述。
        /// </summary>
        [Comment("描述")]
        public string Description { get; set; }

        /// <summary>
        /// 快捷输入码。服务器不使用。8个ASCII字符不足的尾部填充空格（写入时可不填充，但读回后会自动加入）。
        /// </summary>
        [Column(TypeName = "char"), MaxLength(8)]
        [Comment("快捷输入码。服务器不使用。")]
        public string ShortcutCode { get; set; }

        /// <summary>
        /// 机构地址。
        /// </summary>
        public PlSimpleOwnedAddress Address { get; set; }

        /// <summary>
        /// 状态码。0=正常，1=停用。
        /// </summary>
        [Comment("状态码。0=正常，1=停用。")]
        public int StatusCode { get; set; }

        /// <summary>
        /// 是否已标记为删除。false(默认)未标记为删除，true标记为删除。
        /// </summary>
        [Comment("是否已标记为删除。false(默认)未标记为删除，true标记为删除。")]
        public bool IsDelete { get; set; }

        /// <summary>
        /// 创建者的唯一标识。
        /// </summary>
        [Comment("创建者的唯一标识")]
        public Guid? CreateBy { get; set; }

        /// <summary>
        /// 创建的时间。
        /// </summary>
        [Comment("创建的时间")]
        public DateTime CreateDateTime { get; set; } = OwHelper.WorldNow;

        #region 瞬时属性
        ConcurrentDictionary<string, object> _RuntimeProperties = new ConcurrentDictionary<string, object>();

        /// <summary>
        /// 记录瞬时属性的字典。
        /// </summary>
        [NotMapped, JsonIgnore]
        public ConcurrentDictionary<string, object> RuntimeProperties { get => _RuntimeProperties; }

        /// <summary>
        /// 获取或设置存储使用的数据库上下文。
        /// </summary>
        [NotMapped, JsonIgnore]
        public PowerLmsUserDbContext DbContext
        {
            get => RuntimeProperties.GetValueOrDefault(nameof(DbContext), null) as PowerLmsUserDbContext;
            set => RuntimeProperties[nameof(DbContext)] = value;
        }
        #endregion 瞬时属性

    }
}
