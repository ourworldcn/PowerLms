using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NuGet.Common;
using PowerLms.Data;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Dto;
using System.ComponentModel.DataAnnotations;

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
        /// <param name="accountManager"></param>
        /// <param name="serviceProvider"></param>
        public MerchantController(PowerLmsUserDbContext dbContext, AccountManager accountManager, IServiceProvider serviceProvider)
        {
            _DbContext = dbContext;
            _AccountManager = accountManager;
            _ServiceProvider = serviceProvider;
        }

        PowerLmsUserDbContext _DbContext;
        AccountManager _AccountManager;
        IServiceProvider _ServiceProvider;

        /// <summary>
        /// 获取全部商户。
        /// </summary>
        /// <param name="token">登录令牌。</param>
        /// <param name="startIndex">起始位置，从0开始。</param>
        /// <param name="count">最大返回数量。</param>
        /// <param name="conditional">查询的条件。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetMerchantReturnDto> GetAll(Guid token, [Range(0, int.MaxValue, ErrorMessage = "必须大于或等于0.")] int startIndex, [Range(-1, int.MaxValue)] int count = -1,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetAccountFromToken(token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetMerchantReturnDto();
            var coll = _DbContext.Merchants.AsNoTracking().OrderBy(c => c.Id).Skip(startIndex);
            foreach (var item in conditional)
                if (string.Equals(item.Key, "id", StringComparison.OrdinalIgnoreCase))
                {
                    if (OwConvert.TryToGuid(item.Value, out var id))
                        coll = coll.Where(c => c.Id == id);
                }
                else if (string.Equals(item.Key, "code", StringComparison.OrdinalIgnoreCase))
                {
                    coll = coll.Where(c => c.Name.Name == item.Value);
                }
                else if (string.Equals(item.Key, "displayname", StringComparison.OrdinalIgnoreCase))
                {
                    coll = coll.Where(c => c.Name.DisplayName.Contains(item.Value));
                }
            if (count > -1)
                coll = coll.Take(count);
            result.Total = _DbContext.Merchants.Count();
            result.Result.AddRange(coll);
            return result;
        }

        /// <summary>
        /// 获取指定商户的数据。
        /// </summary>
        /// <param name="token"></param>
        /// <param name="id"></param>
        /// <returns>可能返回空值，表示没有找到。</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<PlMerchant> Get(Guid token, Guid id)
        {
            if (_AccountManager.GetAccountFromToken(token, _ServiceProvider) is not OwContext context) return Unauthorized();
            return _DbContext.Merchants.Find(id);
        }

        /// <summary>
        /// 修改商户信息。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的商户不存在。</response>  
        [HttpPut]
        public ActionResult<ModifyMerchantReturnDto> ModifyMerchant(ModifyMerchantParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyMerchantReturnDto();
            if (_DbContext.Merchants.Find(model.Merchant.Id) is not PlMerchant mcht) return NotFound();
            _DbContext.Entry(mcht).CurrentValues.SetValues(model.Merchant);
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 增加新商户。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpPost]
        public ActionResult<AddMerchantReturnDto> AddMerchant(AddMerchantParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new AddMerchantReturnDto();
            model.Merchant.GenerateNewId();
            _DbContext.Merchants.Add(model.Merchant);
            _DbContext.SaveChanges();
            result.Id = model.Merchant.Id;
            return result;
        }

        /// <summary>
        /// 删除指定Id的商户。慎用！
        /// </summary>
        /// <param name="token">令牌。</param>
        /// <param name="id">商户Id。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的商户不存在。</response>  
        [HttpDelete]
        public ActionResult Remove(Guid token, Guid id)
        {
            if (_AccountManager.GetAccountFromToken(token, _ServiceProvider) is not OwContext context) return Unauthorized();
            if (_DbContext.Merchants.Find(id) is not PlMerchant mcht) return NotFound();
            _DbContext.Remove(mcht);
            //TODO 连锁删除所有信息，待最后实施
            _DbContext.SaveChanges();
            return Ok();
        }
    }

    /// <summary>
    /// 获取所有商户功能的返回值封装类。
    /// </summary>
    public class GetMerchantReturnDto : PagingReturnDtoBase<PlMerchant>
    {
    }

    /// <summary>
    /// 增加新商户功能参数封装类。
    /// </summary>
    public class AddMerchantParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 新商户信息。其中Id可以是任何值，返回时会指定新值。
        /// </summary>
        public PlMerchant Merchant { get; set; }
    }

    /// <summary>
    /// 增加新商户功能返回值封装类。
    /// </summary>
    public class AddMerchantReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 如果成功添加，这里返回新商户的Id。
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 修改商户信息功能参数封装类。
    /// </summary>
    public class ModifyMerchantParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 商户数据。
        /// </summary>
        public PlMerchant Merchant { get; set; }
    }

    /// <summary>
    /// 修改商户信息功能返回值封装类。
    /// </summary>
    public class ModifyMerchantReturnDto : ReturnDtoBase
    {
    }
}
