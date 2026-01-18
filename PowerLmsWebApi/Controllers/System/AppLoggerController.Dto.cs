using PowerLms.Data;
using PowerLmsWebApi.Dto;
namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// 导出日志功能的参数封装类。
    /// </summary>
    public class ExportLoggerParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 指定下载文件的名字。不可以含路径。
        /// </summary>
        public string FileName { get; set; }
    }
    /// <summary>
    /// 导出日志功能的返回值封装类。
    /// </summary>
    public class ExportLoggerReturnDto
    {
    }
    /// <summary>
    /// 清除日志项功能的参数封装类。
    /// </summary>
    public class RemoveAllLoggerItemParamsDto : TokenDtoBase
    {
    }
    /// <summary>
    /// 清除日志项功能的返回值封装类。
    /// </summary>
    public class RemoveAllLoggerItemReturnDto : ReturnDtoBase
    {
    }
    /// <summary>
    /// 返回日志项功能的参数封装类。
    /// </summary>
    public class GetAllAppLogItemParamsDto
    {
    }
    /// <summary>
    /// 返回日志项功能的返回值封装类。
    /// </summary>
    public class GetAllAppLogItemReturnDto : PagingReturnDtoBase<OwAppLogView>
    {
    }
    /// <summary>
    /// 追加一个日志项功能的参数封装类。
    /// </summary>
    public class AddLoggerItemParamsDto
    {
    }
    /// <summary>
    /// 追加一个日志项功能的返回值封装类。
    /// </summary>
    public class AddLoggerItemReturnDto
    {
    }
}