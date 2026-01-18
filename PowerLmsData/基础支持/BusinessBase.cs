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
        /// 创建的时间。一般记录Utc时间。
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
    /// <summary>
    /// 业务表单标记。
    /// </summary>
    public interface IPlBusinessDoc
    {
        /// <summary>
        /// 所属业务Id。
        /// </summary>
        public Guid? JobId { get; set; }
        /// <summary>
        /// 操作状态。0=初始化单据但尚未操作，128=最后一个状态，此状态下将业务对象状态自动切换为下一个状态。
        /// </summary>
        public byte Status { get; set; }
    }
}
