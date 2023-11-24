using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OW.Data
{
    /// <summary>
    /// 支持单键值的数据库类的基础接口。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IEntityWithSingleKey<T>
    {
        /// <summary>
        /// 数据库Id。
        /// </summary>
        abstract T Id { get; set; }

    }

    /// <summary>
    /// 以<see cref="Guid"/>为键类型的实体类的基类。
    /// </summary>
    public abstract class GuidKeyObjectBase : IEntityWithSingleKey<Guid>
    {
        #region 构造函数

        /// <summary>
        /// 构造函数。
        /// 会自动用<see cref="Guid.NewGuid"/>生成<see cref="Id"/>属性值。
        /// </summary>
        public GuidKeyObjectBase()
        {
            Id = Guid.NewGuid();
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="id">指定该实体对象的<see cref="Id"/>属性。</param>
        public GuidKeyObjectBase(Guid id)
        {
            Id = id;
        }

        #endregion 构造函数

        /// <summary>
        /// 主键。
        /// </summary>
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None), Column(Order = 0)]
        public Guid Id { get; set; }

        /// <summary>
        /// 如果Id是Guid.Empty则生成新Id,否则立即返回false。
        /// </summary>
        /// <returns>true生成了新Id，false已经有了非空Id。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool GenerateIdIfEmpty()
        {
            if (Guid.Empty != Id)
                return false;
            Id = Guid.NewGuid();
            return true;
        }

        /// <summary>
        /// 强制生成一个新Id。
        /// 通常这是一个危险的操作。仅在克隆副本以后有可能需要调用。
        /// </summary>
        public void GenerateNewId()
        {
            Id = Guid.NewGuid();
        }

        #region 减低内存分配速率

        private string _IdString;

        /// <summary>
        /// 获取或设置Id的字符串表现形式。Id的字符串形式"00000000-0000-0000-0000-000000000000"从 a 到 f 的十六进制数字是小写的。
        /// 该属性第一次读取时才初始化。有利于id的池化处理。
        /// </summary>
        [NotMapped]
        [JsonIgnore]
        public string IdString
        {
            get
            {
                return _IdString ??= Id.ToString();
            }
            internal set
            {
                Id = Guid.Parse(value);
                _IdString = null;
            }
        }

        private string _Base64IdString;

        /// <summary>
        /// 获取或设置Id的Base64字符串表现形式。
        /// 该属性第一次读取时才初始化。并有利于id的池化处理。
        /// </summary>
        [NotMapped]
        [JsonIgnore]
        public string Base64IdString
        {
            get { return _Base64IdString ??= Id.ToBase64String(); }
            internal set
            {
                Id = OwConvert.ToGuid(value);
                _Base64IdString = value;
            }
        }

        #endregion 减低内存分配速率
    }

}
