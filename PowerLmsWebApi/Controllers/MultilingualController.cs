using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PowerLmsWebApi.Dto;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// 多语言资源相关功能控制器。
    /// </summary>
    public class MultilingualController : OwControllerBase
    {
        /// <summary>
        /// 
        /// </summary>
        public MultilingualController()
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Token"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult<MultilingualGetReturnDto> Get(Guid? Token, string prefix)
        {
            return new MultilingualGetReturnDto();
        }
    }
}
