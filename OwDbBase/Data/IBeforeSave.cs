
using Microsoft.EntityFrameworkCore;

namespace OW.Data
{
    /// <summary>
    /// POCO类在被保存前需要调用此接口将一些数据写入可存储的字段中。
    /// </summary>
    public interface IBeforeSave
    {
        /// <summary>
        /// 实体类在被保存前需要调用该成员。应该仅写入自身拥有的直接存储于数据库的简单字段。
        /// 不要引用其他存储于数据库中的实体。否则，需要考虑重载其他实体的该接口方法，保证不会反复提交，或者是有序的保存。
        /// </summary>
        /// <param name="db">该实体类将被保存到的数据库上下文。</param>
        void PrepareSaving(DbContext db);

        /// <summary>
        /// 是否取消<see cref="PrepareSaving"/>的调用。
        /// </summary>
        /// <value>true不会调用保存方法，false(默认值)在保存前调用保存方法。</value>
        bool SuppressSave => false;
    }

}