using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerLms.Data
{
    /// <summary>
    /// 数据字典的标识接口。
    /// </summary>
    public interface IDataDic
    {

    }

    /// <summary>
    /// 数据字典的基础类。
    /// </summary>
    public abstract class DataDicBase:GuidKeyObjectBase, IDataDic
    {
        /// <summary>
        /// 显示名的多语言Id。
        /// </summary>
        abstract public string DisplayNameMlId { get; set; }
    }
}
