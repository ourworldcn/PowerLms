using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerLms.Data
{
    /// <summary>
    /// 创建者信息接口。
    /// </summary>
    public interface ICreatorInfo
    {
        /// <summary>
        /// 创建者的唯一标识。
        /// </summary>
        public Guid? CreateBy { get; set; }

        /// <summary>
        /// 创建的时间。
        /// </summary>
        public DateTime CreateDateTime { get; set; }
    }

    /// <summary>
    /// 创建者信息信息的基类。
    /// </summary>
    public class CreatorInfoBase : ICreatorInfo
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public Guid? CreateBy { get; set; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public DateTime CreateDateTime { get; set; }
    }
}
