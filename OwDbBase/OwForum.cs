using Microsoft.EntityFrameworkCore;
using OW.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace OW.Forum
{
    /// <summary>论坛板块实体。</summary>
    [Index(nameof(ParentId))]
    [Index(nameof(AuthorId))]
    public class OwForumCategory : GuidKeyObjectBase
    {
        /// <summary>所属实体，通常是组织ID(字符串形式),最长128字符。对应非多租户版本这里可以是空。</summary>
        [MaxLength(128)]
        public string ParentId { get; set; }

        /// <summary>作者标识，通常是用户名或用户ID(字符串形式),最长128字符。</summary>
        [MaxLength(128)]
        public string AuthorId { get; set; }

        /// <summary>作者的显示名称，这是实体字段,用于永久固定作者显示名字。最长128字符。</summary>
        [MaxLength(128)]
        public string AuthorDisplayName { get; set; }

        /// <summary>标题。最长64字符。</summary>
        [MaxLength(64)]
        public string Title { get; set; }

        /// <summary>板块的详细说明。</summary>
        public string Remark { get; set; }

        /// <summary>创建时间。精确到毫秒。</summary>
        [Precision(3)]
        public DateTime CreatedAt { get; set; }

        /// <summary>最后编辑时间。刚创建时为null。精确到毫秒。</summary>
        [Precision(3)]
        public DateTime? EditedAt { get; set; }
    }

    /// <summary>帖子实体。</summary>
    [Index(nameof(ParentId))]
    [Index(nameof(AuthorId))]
    [Index(nameof(CreatedAt))]
    public class OwPost : GuidKeyObjectBase
    {
        /// <summary>所属板块Id。</summary>
        public Guid ParentId { get; set; }

        /// <summary>作者标识，通常是用户名或用户ID(字符串形式),最长128字符。</summary>
        [MaxLength(128)]
        public string AuthorId { get; set; }

        /// <summary>作者的显示名称，这是实体字段,用于永久固定作者显示名字（更改需要特别机制）。最长128字符。</summary>
        [MaxLength(128)]
        public string AuthorDisplayName { get; set; }

        /// <summary>标题。最长64字符。</summary>
        [MaxLength(64)]
        public string Titel { get; set; }

        /// <summary>正文，内部可能是html / Markdown / BBCode等格式。由前端决定如何渲染。</summary>
        public string Content { get; set; }

        /// <summary>创建时间。精确到毫秒。</summary>
        [Precision(3)]
        public DateTime CreatedAt { get; set; }

        /// <summary>最后编辑时间。刚创建时为null。精确到毫秒。</summary>
        [Precision(3)]
        public DateTime? EditedAt { get; set; }

        /// <summary>关键词标签，便于搜索。多个标签用逗号分隔,最长128字符。</summary>
        [MaxLength(128)]
        public string Tag { get; set; }

        /// <summary>是否置顶。置顶帖子按创建时间降序排列在前面。</summary>
        /// <value>默认值：false=不置顶。此属性需要额外方法设置，不在CRUD内设置。</value>
        public bool IsTop { get; set; }
    }

    /// <summary>回复实体。</summary>
    [Index(nameof(ParentId))]
    [Index(nameof(AuthorId))]
    [Index(nameof(CreatedAt))]
    [Index(nameof(ParentReplyId))]
    public class OwReply : GuidKeyObjectBase
    {
        /// <summary>所属帖子Id。</summary>
        public Guid ParentId { get; set; }

        /// <summary>父回复Id，用于嵌套回复。顶级回复时为null。暂时保留为null,不支持嵌套回复。</summary>
        public Guid? ParentReplyId { get; set; }

        /// <summary>作者标识，通常是用户名或用户ID(字符串形式),最长128字符。</summary>
        [MaxLength(128)]
        public string AuthorId { get; set; }

        /// <summary>作者的显示名称，这是实体字段,用于永久固定作者显示名字。最长128字符。</summary>
        [MaxLength(128)]
        public string AuthorDisplayName { get; set; }

        /// <summary>回复内容，内部可能是html / Markdown / BBCode等格式。由前端决定如何渲染。</summary>
        public string Content { get; set; }

        /// <summary>创建时间。精确到毫秒。</summary>
        [Precision(3)]
        public DateTime CreatedAt { get; set; }

        /// <summary>最后编辑时间。刚创建时为null。精确到毫秒。</summary>
        [Precision(3)]
        public DateTime? EditedAt { get; set; }

    }
}
