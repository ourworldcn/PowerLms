using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PowerLmsWebApi.Controllers;

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
        /// 返回日志项。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public ActionResult<GetLoggerItemReturnDto> GetLoggerItem(GetLoggerItemParamsDto model)
        {
            var result = new GetLoggerItemReturnDto { };
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
    public class GetLoggerItemParamsDto
    {
    }

    /// <summary>
    /// 返回日志项功能的返回值封装类。
    /// </summary>
    public class GetLoggerItemReturnDto
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
