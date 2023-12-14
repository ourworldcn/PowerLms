using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NPOI.Util;
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
    public class MerchantController : PlControllerBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="accountManager"></param>
        /// <param name="serviceProvider"></param>
        /// <param name="dataManager"></param>
        /// <param name="entityManager"></param>
        /// <param name="mapper"></param>
        public MerchantController(PowerLmsUserDbContext dbContext, AccountManager accountManager, IServiceProvider serviceProvider, DataDicManager dataManager, EntityManager entityManager, IMapper mapper)
        {
            _DbContext = dbContext;
            _AccountManager = accountManager;
            _ServiceProvider = serviceProvider;
            _DataManager = dataManager;
            _EntityManager = entityManager;
            _Mapper = mapper;
        }

        readonly PowerLmsUserDbContext _DbContext;
        readonly AccountManager _AccountManager;
        readonly IServiceProvider _ServiceProvider;
        readonly DataDicManager _DataManager;
        readonly EntityManager _EntityManager;
        private readonly IMapper _Mapper;

        #region 简单CRUD

        /// <summary>
        /// 获取全部商户。
        /// </summary>
        /// <param name="token">登录令牌。</param>
        /// <param name="startIndex">起始位置，从0开始。</param>
        /// <param name="count">最大返回数量。</param>
        /// <param name="conditional">查询的条件。支持 name，ShortName，displayname，ShortcutCode，Id。不区分大小写。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllMerchantReturnDto> GetAllMerchant(Guid token, [Range(0, int.MaxValue, ErrorMessage = "必须大于或等于0.")] int startIndex, [Range(-1, int.MaxValue)] int count = -1,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetAccountFromToken(token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllMerchantReturnDto();
            var coll = _DbContext.Merchants.AsNoTracking().OrderBy(c => c.Id).Skip(startIndex);
            foreach (var item in conditional)
                if (string.Equals(item.Key, "name", StringComparison.OrdinalIgnoreCase))
                {
                    coll = coll.Where(c => c.Name.Name.Contains(item.Value));
                }
                else if (string.Equals(item.Key, "Id", StringComparison.OrdinalIgnoreCase))
                {
                    if (Guid.TryParse(item.Value, out var id))
                        coll = coll.Where(c => c.Id == id);
                }
                else if (string.Equals(item.Key, "ShortName", StringComparison.OrdinalIgnoreCase))
                {
                    coll = coll.Where(c => c.Name.ShortName.Contains(item.Value));
                }
                else if (string.Equals(item.Key, "displayname", StringComparison.OrdinalIgnoreCase))
                {
                    coll = coll.Where(c => c.Name.DisplayName.Contains(item.Value));
                }
                else if (string.Equals(item.Key, "ShortcutCode", StringComparison.OrdinalIgnoreCase))
                {
                    coll = coll.Where(c => c.ShortcutCode.Contains(item.Value));
                }
            var prb = _EntityManager.GetAll(coll, startIndex, count);
            _Mapper.Map(prb, result);
            return result;
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
            var r = InitializeMerchant(new InitializeMerchantParamsDto
            {
                Id = result.Id,
                Token = model.Token,
            });
            return result;
        }

        /// <summary>
        /// 删除指定Id的商户。慎用！
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的商户不存在。</response>  
        [HttpDelete]
        public ActionResult<RemoveMerchantReturnDto> RemoveMerchant(RemoveMerchantParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemoveMerchantReturnDto();
            var id = model.Id;
            var dbSet = _DbContext.Merchants;
            var item = dbSet.Find(id);
            if (item is null) return BadRequest();
            item.IsDelete = true;
            _DbContext.SaveChanges();
            //if (item.DataDicType == 1) //若是简单字典
            //    _DbContext.Database.ExecuteSqlRaw($"delete from {nameof(_DbContext.SimpleDataDics)} where {nameof(SimpleDataDic.DataDicId)}='{id.ToString()}'");
            //else //其他字典待定
            //{

            //}
            return result;
        }
        #endregion 简单CRUD

        /// <summary>
        /// 初始化商户。商户已有信息会被复位为初始化状态。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的商户不存在。</response>  
        [HttpPost]
        public ActionResult<InitializeMerchantReturnDto> InitializeMerchant(InitializeMerchantParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new InitializeMerchantReturnDto();
            var merch = _DbContext.Merchants.Find(model.Id);
            if (merch == null) return NotFound();
            #region 复制简单字典
            var baseCatalogs = _DbContext.DD_DataDicCatalogs.Where(c => c.OrgId == null).AsNoTracking();  //基本字典目录集合
            foreach (var catalog in baseCatalogs)
            {
                _DataManager.CopyTo(catalog, model.Id);
            }
            _DataManager.CopyAllSpecialDataDicBase(model.Id);
            #endregion 复制简单字典

            _DbContext.SaveChanges();
            return result;
        }
    }

    /// <summary>
    /// 标记删除商户功能的参数封装类。
    /// </summary>
    public class RemoveMerchantParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 标记删除商户功能的返回值封装类。
    /// </summary>
    public class RemoveMerchantReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 初始化商户的功能参数封装类。
    /// </summary>
    public class InitializeMerchantParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 初始化商户的Id。
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 初始化商户的功能返回值封装类。
    /// </summary>
    public class InitializeMerchantReturnDto : ReturnDtoBase
    {
    }

    /// <summary>
    /// 获取所有商户功能的返回值封装类。
    /// </summary>
    public class GetAllMerchantReturnDto : PagingReturnDtoBase<PlMerchant>
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
