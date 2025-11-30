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
        /// <param name="languageTag">语言的标准缩写名（IETF BCP 47标准），如：zh-CN、en-US、ja-JP。未登录则必须填写。</param>
        /// <param name="prefix">键值前缀。理论上可以通过空来获取所有资源键值，但这样存在性能隐患。</param>
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

            // 基于联合主键(Key, LanguageTag)处理删除
            foreach (var deleteItem in model.DeleteDatas)
            {
                var entity = _Db.Multilinguals
                    .FirstOrDefault(m => m.Key == deleteItem.Key 
                        && m.LanguageTag == deleteItem.LanguageTag);
                if (entity != null)
                {
                    _Db.Multilinguals.Remove(entity);
                }
            }

            // 更新或添加
            foreach (var item in model.AddOrUpdateDatas)
            {
                var entity = _Db.Multilinguals
                    .FirstOrDefault(m => m.Key == item.Key 
                        && m.LanguageTag == item.LanguageTag);
                if (entity != null)
                {
                    // 更新
                    entity.Text = item.Text;
                }
                else
                {
                    // 添加
                    _Db.Multilinguals.Add(item);
                }
            }

            _Db.SaveChanges();
            return result;
        }
    }

}
