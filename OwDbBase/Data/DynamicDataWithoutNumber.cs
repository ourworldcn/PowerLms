using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
namespace OW.Data
{
    /// <summary>
    /// 动态数据存储实体。
    /// </summary>
    [Index(nameof(ParentId), IsUnique = false)]
    [Index(nameof(ExtraGuid), nameof(ExtraDateTime), IsUnique = false)]
    [Index(nameof(ExtraGuid), nameof(ExtraString), IsUnique = false)]
    public class DynamicDataWithoutNumber : GuidKeyObjectBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public DynamicDataWithoutNumber()
        {
        }
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="parentId">父条目的ID。</param>
        public DynamicDataWithoutNumber(Guid parentId)
        {
            ParentId = parentId;
        }
        /// <summary>
        /// 父条目的ID。
        /// </summary>
        public Guid? ParentId { get; set; }
        /// <summary>
        /// Json字符串，存储动态数据。
        /// </summary>
        [Unicode(false)]
        public string DataJson { get; set; }
        /// <summary>
        /// 额外的Guid数据。
        /// </summary>
        public Guid? ExtraGuid { get; set; }
        /// <summary>
        /// 额外的字符串数据。
        /// </summary>
        [MaxLength(128)]
        public string ExtraString { get; set; }
        /// <summary>
        /// 额外的日期时间数据。
        /// </summary>
        public DateTime? ExtraDateTime { get; set; }
        private Dictionary<string, object> _DataDic;
        /// <summary>
        /// 反序列化后的数据字典。
        /// </summary>
        [NotMapped]
        public Dictionary<string, object> DataDic
        {
            get
            {
                if (_DataDic == null && !string.IsNullOrEmpty(DataJson))
                {
                    _DataDic = JsonSerializer.Deserialize<Dictionary<string, object>>(DataJson);
                }
                return _DataDic ??= new Dictionary<string, object>();
            }
        }
        /// <summary>
        /// 将当前数据字典序列化为JSON。
        /// </summary>
        public void SerializeData()
        {
            if (_DataDic != null)
            {
                DataJson = JsonSerializer.Serialize(_DataDic);
            }
        }
    }
}
