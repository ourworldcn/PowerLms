using AutoMapper;
using PowerLmsServer.Managers;
using System.ComponentModel.DataAnnotations;

namespace PowerLmsWebApi.Dto
{
    /// <summary>
    /// 带有令牌命令的入参基类。
    /// </summary>
    public class TokenDtoBase
    {
        /// <summary>
        /// 令牌。
        /// </summary>
        [Required]
        public Guid Token { get; set; }
    }

    /// <summary>
    /// 返回对象的基类。
    /// </summary>
    public class ReturnDtoBase
    {
        /// <summary>
        /// 
        /// </summary>
        public ReturnDtoBase()
        {

        }

        /// <summary>
        /// 是否有错误。不设置则使用<see cref="ErrorCode"/>来判定。
        /// </summary>
        /// <value>0没有错误，其它数值含义由应用定义。</value>
        public bool HasError { get; set; }

        /// <summary>
        /// 错误码，参见 ErrorCodes。
        /// </summary>
        public int ErrorCode { get; set; }

        /// <summary>
        /// 调试信息，如果发生错误，这里给出简要说明。
        /// </summary>
        public string DebugMessage { get; set; }

    }

    /// <summary>
    /// 分页/排序要求的基类。
    /// </summary>
    [AutoMap(typeof(PagingParamsBase))]
    public class PagingParamsDtoBase : TokenDtoBase
    {
        /// <summary>
        /// 起始位置，从0开始。
        /// </summary>
        [Required, Range(0, int.MaxValue)]
        public int StartIndex { get; set; }

        /// <summary>
        /// 最大返回数量。
        /// 默认值-1，不限定返回数量。
        /// </summary>
        [Range(-1, int.MaxValue)]
        public int Count { get; set; } = -1;

        /// <summary>
        /// 排序的字段名。默认值:"Id"。
        /// </summary>
        public string OrderFieldName { get; set; } = nameof(GuidKeyObjectBase.Id);

        /// <summary>
        /// 是否降序排序：true降序排序，false升序排序（省略或默认）。
        /// </summary>
        public bool IsDesc { get; set; }
    }

    /// <summary>
    /// 返回分页数据的封装类的基类
    /// </summary>
    /// <typeparam name="T">集合元素的类型。</typeparam>
    [AutoMap(typeof(PagingReturnBase<>), IncludeAllDerived = true)]
    public abstract class PagingReturnDtoBase<T> : ReturnDtoBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public PagingReturnDtoBase()
        {

        }

        /// <summary>
        /// 集合元素的最大总数量。
        /// </summary>
        public int Total { get; set; }

        /// <summary>
        /// 返回的集合。
        /// </summary>
        public List<T> Result { get; set; } = new List<T>();
    }

    /// <summary>
    /// 增加实体功能的参数封装类的基类。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class AddParamsDtoBase<T> : TokenDtoBase
    {
        /// <summary>
        /// 要增加的项。其中Id被忽略，在返回时会指定。此实体的Id不起作用，操作成功在返回时返回指定的有效Id。
        /// </summary>
        public T Item { get; set; }
    }

    /// <summary>
    /// 增加实体功能的返回值封装类的基类.
    /// </summary>
    public class AddReturnDtoBase : ReturnDtoBase
    {
        /// <summary>
        /// 增加实体的Id。
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 修改实体功能的参数封装类的基类。
    /// </summary>
    /// <typeparam name="T">实体的类型。</typeparam>
    public class ModifyParamsDtoBase<T> : TokenDtoBase
    {
        /// <summary>
        /// 要修改的实体集合，其中Id必须已经存在。
        /// </summary>
        public List<T> Items { get; set; } = new List<T>();
    }

    /// <summary>
    /// 修改实体功能的返回值封装类的基类。
    /// </summary>
    public class ModifyReturnDtoBase : ReturnDtoBase
    {

    }

    /// <summary>
    /// 删除实体功能的参数封装类的基类。
    /// </summary>
    public class RemoveParamsDtoBase : TokenDtoBase
    {
        /// <summary>
        /// 要删除实体的Id。
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 删除实体功能的返回值封装类的基类。
    /// </summary>
    public class RemoveReturnDtoBase : ReturnDtoBase
    {

    }

    /// <summary>
    /// 恢复被软删除实体功能的参数封装类的基类。
    /// </summary>
    public class RestoreParamsDtoBase : TokenDtoBase
    {
        /// <summary>
        /// 要恢复的实体唯一Id。
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 恢复被软删除实体功能的返回值封装类的基类。
    /// </summary>
    public class RestoreReturnDtoBase : ReturnDtoBase
    {

    }
}
