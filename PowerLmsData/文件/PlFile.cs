using Microsoft.EntityFrameworkCore;
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
    /// 文件表。
    /// </summary>
    [Comment("文件信息表")]
    [Index(nameof(ParentId), IsUnique = false)]
    public class PlFileInfo : GuidKeyObjectBase
    {
        /// <summary>
        /// 所属实体的Id。如属于客户资料的文件，就设置为客户Id。
        /// </summary>
        [Comment("所属实体的Id")]
        public Guid? ParentId { get; set; }

        /// <summary>
        /// 文件类型Id。关联字典FileType。可能是null，表示这是一个通用文件。
        /// </summary>
        [Comment("文件类型Id。关联字典FileType。可能是null，表示这是一个通用文件。")]
        public Guid? FileTypeId { get; set; }

        /// <summary>
        /// 文件的相对路径和全名。如 Customer\客户尽调资料.Pdf。
        /// </summary>
        [Comment("文件类型Id。文件的相对路径和全名。")]
        [MaxLength(1024)]
        public string FilePath { get; set; }

        /// <summary>
        /// 文件的显示名称。用于列表时的友好名称。
        /// </summary>
        [Comment("文件的显示名称")]
        [MaxLength(64)]
        public string DisplayName { get; set; }

        /// <summary>
        /// 上传时的文件名。
        /// </summary>
        [Comment("上传时的文件名")]
        [MaxLength(64)]
        public string FileName { get; set; }

        /// <summary>
        /// 备注。
        /// </summary>
        [Comment("备注")]
        public string Remark { get; set; }
    }
}
