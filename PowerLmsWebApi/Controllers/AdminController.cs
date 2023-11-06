using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PowerLms.Data;
using PowerLmsServer.EfData;
using PowerLmsWebApi.Dto;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// 管理员功能控制器。
    /// </summary>
    public class AdminController : OwControllerBase
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        public AdminController(PowerLmsUserDbContext context)
        {
            _Context = context;
        }

        PowerLmsUserDbContext _Context;

        /// <summary>
        /// 获取系统资源列表。
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult<GetSystemResourceReturnDto> GetSystemResource()
        {
            var result = new GetSystemResourceReturnDto();
            result.Resources.AddRange(_Context.SystemResources);
            return result;
        }
    }

    /// <summary>
    /// 获取系统资源列表功能的返回值封装类。
    /// </summary>
    public class GetSystemResourceReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 系统资源集合。
        /// </summary>
        public List<SystemResource> Resources { get; set; } = new List<SystemResource>();
    }
}
