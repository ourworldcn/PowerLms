using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.EntityFrameworkCore;
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
            
            _Db.AddOrUpdate(model.AddOrUpdateDatas);
            _Db.Delete<Multilingual>(model.DeleteIds);
            _Db.SaveChanges();
            return result;
        }

        /// <summary>
        /// 获取语言字典表。此功能暂时不考虑分页。此表暂时不考虑追加/修改/删除。
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult<GetLanguageDataDicReturnDto> GetLanguageDataDic()
        {
            var result = new GetLanguageDataDicReturnDto();
            var coll=from tmp in _Db.LanguageDataDics
                     select tmp;
            result.Results.AddRange(coll.AsNoTracking());
            return result;
        }
    }

}
