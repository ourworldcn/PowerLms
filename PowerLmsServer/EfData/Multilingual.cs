using Microsoft.EntityFrameworkCore;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerLmsServer.EfData
{
    [Index(nameof(Key), IsUnique = true)]
    public class Multilingual
    {
        public Multilingual()
        {
            
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// 键值字符串。如:未登录.登录.标题。
        /// </summary>
        [MaxLength(64)]
        public string Key { get; set; }

        /// <summary>
        /// 内容。
        /// </summary>
        public string Text { get; set; }
    }
}
