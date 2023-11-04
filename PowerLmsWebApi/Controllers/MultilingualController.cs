using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PowerLmsServer.EfData;
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
        public MultilingualController(PowerLmsUserDbContext db)
        {
            _Db = db;
        }

        PowerLmsUserDbContext _Db;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Token"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult<MultilingualGetReturnDto> Get(Guid? Token, string prefix)
        {
            var result=new MultilingualGetReturnDto();
            result.Multilinguals.AddRange(_Db.Multilinguals);
            return result;
        }
    }
}
