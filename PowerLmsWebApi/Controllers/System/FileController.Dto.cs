using PowerLms.Data;
using PowerLmsWebApi.Dto;
using System.ComponentModel.DataAnnotations;
namespace PowerLmsWebApi.Controllers
{
    #region 通用文件管理
    /// <summary>
    /// 获取全部通用文件信息返回值封装类。
    /// </summary>
    public class GetAllFileInfoReturnDto : PagingReturnDtoBase<PlFileInfo>
    {
    }
    /// <summary>
    /// 下载文件通用功能参数封装类。
    /// 已废弃：GetFile 接口现在直接使用参数绑定特性，不再需要此 DTO 类。
    /// </summary>
    [Obsolete("GetFile 接口已改用参数绑定特性，此类不再使用")]
    public class GetFileParamsDto
    {
        /// <summary>
        /// 登录令牌。可选，优先从查询参数获取，其次从Header(Authorization: Bearer token)，最后从Cookie(token)。
        /// </summary>
        public Guid? Token { get; set; }
        /// <summary>
        /// 文件Id。
        /// </summary>
        public Guid FileId { get; set; }
    }
    /// <summary>
    /// 上传文件通用接口的参数封装类。
    /// </summary>
    public class AddFileParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 文件。
        /// </summary>
        public IFormFile File { get; set; }
        /// <summary>
        /// 所附属实体的Id，如附属在Ea单上则是Ea单的Id,附属在货场出重条目上的则是那个条目的Id。
        /// </summary>
        public Guid ParentId { get; set; }
        /// <summary>
        /// 显示名。
        /// </summary>
        public string DisplayName { get; set; }
        /// <summary>
        /// 备注。
        /// </summary>
        public string Remark { get; set; }
        /// <summary>
        /// 客户端字符串。客户端可以写入任意字符串信息。
        /// </summary>
        public string ClientString { get; set; }
        /// <summary>
        /// 文件类型Id。关联字典FileType。可能是null，表示这是一个通用文件。
        /// </summary>
        public Guid? FileTypeId { get; set; }
    }
    /// <summary>
    /// 上传文件通用接口的返回值封装类。
    /// </summary>
    public class AddFileReturnDto : AddReturnDtoBase
    {
    }
    /// <summary>
    /// 删除存储的文件功能参数封装类。
    /// </summary>
    public class RemoveFileParamsDto : RemoveParamsDtoBase
    {
    }
    /// <summary>
    /// 删除存储的文件功能返回值封装类。
    /// </summary>
    public class RemoveFileReturnDto : RemoveReturnDtoBase
    {
    }
    #endregion 通用文件管理
    /// <summary>
    /// 获取文件列表功能的返回值封装类。
    /// </summary>
    public class GetAllCustomerFileListReturnDto : PagingReturnDtoBase<PlFileInfo>
    {
    }
    /// <summary>
    /// 上传文件的功能参数封装类。
    /// </summary>
    public class UploadCustomerFileParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 文件的显示名。这是个友好名称。任意设置。
        /// </summary>
        public string DisplayName { get; set; }
        /// <summary>
        /// 类型字典Id。
        /// </summary>
        public Guid FileTypeId { get; set; }
        /// <summary>
        /// 所属实体的Id。
        /// </summary>
        public Guid? ParentId { get; set; }
    }
    /// <summary>
    /// 上传文件的功能返回值封装类。
    /// </summary>
    public class UploadCustomerFileReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 文件的唯一Id。
        /// </summary>
        public Guid Result { get; set; }
    }
}
