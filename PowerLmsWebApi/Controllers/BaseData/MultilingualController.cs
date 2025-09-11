using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.EntityFrameworkCore;
using PowerLms.Data;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Dto;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// 多语言资源相关功能控制器。
    /// </summary>
    public class MultilingualController : PlControllerBase
    {
        /// <summary>
        /// 
        /// </summary>
        public MultilingualController(PowerLmsUserDbContext db)
        {
            _Db = db;
        }

        readonly PowerLmsUserDbContext _Db;

        /// <summary>
        /// 获取一组语言资源。
        /// </summary>
        /// <param name="token">登录令牌，未登录则为空。</param>
        /// <param name="languageTag">语言的标准缩写名。未登录则必须填写。</param>
        /// <param name="prefix">前缀。理论上可以通过空来获取所有资源键值，但这样存在性能隐患。</param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult<MultilingualGetReturnDto> Get(Guid? token, string languageTag, string prefix)
        {
            prefix ??= string.Empty;
            var result = new MultilingualGetReturnDto();

            if (token is null)  //若未登录
            {
                var coll = from tmp in _Db.Multilinguals
                           where tmp.LanguageTag == languageTag && tmp.Key.StartsWith(prefix)
                           select tmp;
                result.Multilinguals.AddRange(coll.AsNoTracking());
            }
            else
            {
            }
            return result;
        }

        /// <summary>
        /// 更新或追加多语言资源。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<MultilingualSetReturnDto> Set(MultilingualSetParamsDto model)
        {
            var result = new MultilingualSetReturnDto();
            //检验Token

            _Db.Delete(model.DeleteIds, nameof(_Db.Multilinguals));
            _Db.AddOrUpdate(model.AddOrUpdateDatas as IEnumerable<Multilingual>);
            _Db.SaveChanges();
            return result;
        }
    }

}
