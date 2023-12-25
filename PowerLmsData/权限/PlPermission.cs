using Microsoft.EntityFrameworkCore;
using OW.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerLms.Data
{
    /// <summary>
    /// 权限类。
    /// </summary>
    [Comment("权限类。")]
    public class PlPermission : GuidKeyObjectBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public PlPermission()
        {
            
        }

        /// <summary>
        /// 封装名称的对象。
        /// </summary>
        public PlOwnedName Name { get; set; }

    }
}
