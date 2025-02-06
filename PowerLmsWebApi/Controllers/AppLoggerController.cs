using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PowerLmsWebApi.Controllers;
using PowerLmsWebApi.Dto;

namespace GY02.Controllers
{
    /// <summary>
    /// 应用日志控制器。
    /// </summary>
    public class AppLoggerController : PlControllerBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public AppLoggerController()
        {

        }

        /// <summary>
        /// 返回日志项。超管/商管可以调用，商管只能看自己商户的日志。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定类别Id无效。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpGet]
        public ActionResult<GetAllAppLogItemReturnDto> GetAllAppLogItem([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            var result = new GetAllAppLogItemReturnDto { };
            return result;
        }

        /// <summary>
        /// 追加一个日志项。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public ActionResult<AddLoggerItemReturnDto> AddLoggerItem(AddLoggerItemParamsDto model)
        {
            var result = new AddLoggerItemReturnDto();
            return result;
        }

        /// <summary>
        /// 清除日志项。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public ActionResult<RemoveAllLoggerItemReturnDto> RemoveAllLoggerItem(RemoveAllLoggerItemParamsDto model)
        {
            var result = new RemoveAllLoggerItemReturnDto();
            return result;
        }

        /// <summary>
        /// 导出日志功能。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public ActionResult<ExportLoggerReturnDto> ExportLogger(ExportLoggerParamsDto model)
        {
            var result = new ExportLoggerReturnDto();
            return result;
        }
    }

    /// <summary>
    /// 导出日志功能的参数封装类。
    /// </summary>
    public class ExportLoggerParamsDto
    {
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
    public class RemoveAllLoggerItemParamsDto
    {
    }

    /// <summary>
    /// 清除日志项功能的返回值封装类。
    /// </summary>
    public class RemoveAllLoggerItemReturnDto
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
    public class GetAllAppLogItemReturnDto
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
