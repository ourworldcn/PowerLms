using Microsoft.AspNetCore.Mvc;
using PowerLms.Data;
using PowerLmsServer.EfData;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// 操作商户的控制器。
    /// </summary>
    public class MerchantController : OwControllerBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="dbContext"></param>
        public MerchantController(PowerLmsUserDbContext dbContext)
        {
            _DbContext = dbContext;
        }

        PowerLmsUserDbContext _DbContext;

        /// <summary>
        /// 获取全部商户。
        /// </summary>
        /// <param name="token">登录令牌。</param>
        /// <returns></returns>
        [HttpGet]
        public IEnumerable<PlMerchant> Get(Guid token)
        {
            return _DbContext.Merchants;
        }

        /// <summary>
        /// 获取指定商户的数据。
        /// </summary>
        /// <param name="token"></param>
        /// <param name="id"></param>
        /// <returns>可能返回空值，表示没有找到。</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        [HttpGet]
        public string Get(Guid token, Guid id)
        {
            return "value";
        }

    }
}
