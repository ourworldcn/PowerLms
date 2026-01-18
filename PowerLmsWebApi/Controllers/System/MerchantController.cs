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
using System.Linq;
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
        /// <param name="orgManager"></param>
        public MerchantController(PowerLmsUserDbContext dbContext, AccountManager accountManager, IServiceProvider serviceProvider, DataDicManager dataManager, EntityManager entityManager, IMapper mapper, OrgManager<PowerLmsUserDbContext> orgManager)
        {
            _DbContext = dbContext;
            _AccountManager = accountManager;
            _ServiceProvider = serviceProvider;
            _DataManager = dataManager;
            _EntityManager = entityManager;
            _Mapper = mapper;
            _OrgManager = orgManager;
        }
        readonly PowerLmsUserDbContext _DbContext;
        readonly AccountManager _AccountManager;
        readonly IServiceProvider _ServiceProvider;
        readonly DataDicManager _DataManager;
        readonly EntityManager _EntityManager;
        private readonly IMapper _Mapper;
        private readonly OrgManager<PowerLmsUserDbContext> _OrgManager;
        #region 简单CRUD
        /// <summary>
        /// 获取全部商户。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">已支持通用查询——除个别涉及敏感信息字段外，所有实体字段都可作为条件。
        /// 通用条件写法：所有条件都是字符串，对区间的写法是用逗号分隔（字符串类型暂时不支持区间且都是模糊查询）如"2024-1-1,2024-1-2"。
        /// 对强制取null的约束，则写"null"。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllMerchantReturnDto> GetAllMerchant([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllMerchantReturnDto();
            var dbSet = _DbContext.Merchants;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            // 使用通用查询
            coll = EfHelper.GenerateWhereAnd(coll, conditional);
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
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
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyMerchantReturnDto();
            var modifiedEntities = new List<PlMerchant>();
            if (!_EntityManager.Modify(new[] { model.Merchant }, modifiedEntities)) return NotFound();
            _DbContext.SaveChanges();
            _OrgManager.InvalidateOrgCaches(modifiedEntities[0].Id);
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
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemoveMerchantReturnDto();
            var id = model.Id;
            var dbSet = _DbContext.Merchants;
            var item = dbSet.Find(id);
            if (item is null) return BadRequest();
            _EntityManager.Remove(item);
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
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
        /// <summary>
        /// 获取指定商户/机构下（含自身和子机构）的所有用户对象。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">至少一个Id不是商户也非机构Id -或- 存在重复Id。</response>  
        [HttpGet]
        public ActionResult<GetUsersByOrgIdsReturnDto> GetUsersByOrgIds([FromQuery] GetUsersByOrgIdsParamsDto model)
        {
            var result = new GetUsersByOrgIdsReturnDto();
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var merchIds = _DbContext.Merchants.Where(c => model.OrgOrMerchantIds.Contains(c.Id)).Select(c => c.Id).Distinct().ToArray(); //选出商户Id
            var orgIds = _DbContext.PlOrganizations.Where(c => model.OrgOrMerchantIds.Contains(c.Id)).Select(c => c.Id).Distinct().ToArray(); //选出机构Id
            if (merchIds.Length + orgIds.Length != model.OrgOrMerchantIds.Count) return BadRequest("至少一个Id不是商户也非机构Id -或- 存在重复Id。");
            var addOrgIds = _DbContext.PlOrganizations.Where(c => merchIds.Contains(c.MerchantId.Value)).Distinct().Select(c => c.Id).ToList(); //商户直属的机构Id
            addOrgIds.AddRange(orgIds); //合成所有机构Id
            var orgs = _DbContext.PlOrganizations.Where(c => addOrgIds.Contains(c.Id)).ToArray();
            var allOrgIds = OwHelper.GetAllSubItemsOfTree(orgs, c => c.Children).Select(c => c.Id).Concat(merchIds).Distinct().ToArray();    //所有机构Id
            var userIds = _DbContext.AccountPlOrganizations.Where(c => allOrgIds.Contains(c.OrgId)).Select(c => c.UserId).Distinct().ToArray(); //所有用户Id
            var users = _DbContext.Accounts.Where(c => userIds.Contains(c.Id));
            result.Result.AddRange(users);
            return result;
        }
    }
}
