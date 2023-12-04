using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerLms.Data
{
    /// <summary>
    /// 业务大类。
    /// </summary>
    [Comment("业务大类")]
    public class BusinessTypeDataDic : DataDicBase
    {
        /// <summary>
        /// 排序序号。越小的越靠前。
        /// </summary>
        [Comment("排序序号。越小的越靠前")]
        public short OrderNumber { get; set; }
    }
}
