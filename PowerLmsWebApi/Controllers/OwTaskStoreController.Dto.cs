using OwDbBase.Tasks;
using PowerLmsWebApi.Dto;

namespace PowerLmsWebApi.Controllers
{
    #region 获取全部任务记录

    /// <summary>
    /// 获取全部任务记录的参数
    /// </summary>
    public class GetAllOwTaskStoreParamsDto : PagingParamsDtoBase
    {
    }

    /// <summary>
    /// 获取全部任务记录的返回结果
    /// </summary>
    public class GetAllOwTaskStoreReturnDto : PagingReturnDtoBase<OwTaskStore>
    {
    }

    #endregion

    /// <summary>
    /// 添加任务记录的参数DTO
    /// </summary>
    public class AddOwTaskStoreParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 要执行的服务类型的完整名称,目前仅支持 OwTaskService 类型。
        /// </summary>
        public string ServiceTypeName { get; set; }

        /// <summary>
        /// 要执行的方法名称，目前仅支持 CreateTask 方法。
        /// </summary>
        public string MethodName { get; set; }

        /// <summary>
        /// 任务参数字典，键为参数名称，值为参数值，对不同任务有不同模式。目前与查询发票时的 Conditional 参数类似。
        /// </summary>
        public Dictionary<string, string> Parameters { get; set; }

    }

    /// <summary>
    /// 添加任务记录的返回结果
    /// </summary>
    public class AddOwTaskStoreReturnDto : AddReturnDtoBase
    {
    }

    #region 删除任务记录

    /// <summary>
    /// 删除任务记录的参数
    /// </summary>
    public class RemoveOwTaskStoreParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 要删除的任务ID
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 删除任务记录的返回结果
    /// </summary>
    public class RemoveOwTaskStoreReturnDto : ReturnDtoBase
    {
    }

    #endregion

    #region 取消任务

    /// <summary>
    /// 取消任务的参数
    /// </summary>
    public class CancelTaskParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 要取消的任务ID
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 取消任务的返回结果
    /// </summary>
    public class CancelTaskReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 操作是否成功
        /// </summary>
        public bool Success { get; set; }
    }
    #endregion
}
