using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OW.Data;

namespace PowerLms.Data
{
    /// <summary>
    /// 商户。
    /// </summary>
    [Comment("商户")]
    public class PlMerchant : GuidKeyObjectBase
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
        [Comment("快捷输入码。服务器不使用。8个ASCII字符不足的尾部填充空格（写入时可不填充，但读回后会自动加入）。")]
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
    }
}
