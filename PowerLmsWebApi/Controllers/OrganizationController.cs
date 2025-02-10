using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.Internal;
using Microsoft.Extensions.Caching.Memory;
using PowerLms.Data;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Dto;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// 组织机构控制器。
    /// </summary>
    public class OrganizationController : PlControllerBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public OrganizationController(AccountManager accountManager, IServiceProvider serviceProvider, PowerLmsUserDbContext dbContext,
            OrganizationManager organizationManager, IMapper mapper, EntityManager entityManager, DataDicManager dataManager, MerchantManager merchantManager, AuthorizationManager authorizationManager)
        {
            _AccountManager = accountManager;
            _ServiceProvider = serviceProvider;
            _DbContext = dbContext;
            _OrganizationManager = organizationManager;
            _Mapper = mapper;
            _EntityManager = entityManager;
            _DataManager = dataManager;
            _MerchantManager = merchantManager;
            _AuthorizationManager = authorizationManager;
        }

        readonly AccountManager _AccountManager;
        readonly IServiceProvider _ServiceProvider;
        readonly PowerLmsUserDbContext _DbContext;
        readonly OrganizationManager _OrganizationManager;
        readonly IMapper _Mapper;
        readonly EntityManager _EntityManager;
        readonly DataDicManager _DataManager;
        readonly AuthorizationManager _AuthorizationManager;
        readonly MerchantManager _MerchantManager;

        /// <summary>
        /// 获取组织机构。暂不考虑分页。
        /// </summary>
        /// <param name="token"></param>
        /// <param name="rootId">根组织机构的Id。或商户的Id。省略或为null时，对商管将返回该商户下多个组织机构;对一般用户会自动给出当前登录公司及其下属所有机构。
        /// 强行指定的Id，可以获取其省略时的子集，但不能获取到更多数据。</param>
        /// <param name="includeChildren">是否包含子机构。废弃，一定要包含子机构。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpGet]
        public ActionResult<GetOrgReturnDto> GetOrg(Guid token, Guid? rootId, bool includeChildren)
        {
            var result = new GetOrgReturnDto();
            if (_AccountManager.GetOrLoadContextByToken(token, _ServiceProvider) is not OwContext context) return Unauthorized();
            if (!rootId.HasValue && !context.User.IsAdmin()) return StatusCode((int)HttpStatusCode.Forbidden, "只有商管可以获取全商户的机构");
            if (context.User.IsSuperAdmin)   //若是超管
            {
                return BadRequest("超管不能获取具体商户的机构");
            }
            if (_MerchantManager.GetOrLoadByUser(context.User) is not OwCacheItem<PlMerchant> merch)    //若找不到商户
                return BadRequest("找不到用户所属的商户");
            var orgs = _OrganizationManager.GetOrLoadOrgsCacheItemByMerchantId(merch.Data.Id);  //获取其所有机构
            if (rootId.HasValue) //若指定了根机构
            {
                if (!orgs.Data.TryGetValue(rootId.Value, out var org) && rootId.Value != merch.Data.Id) return BadRequest($"找不到指定的机构，Id={rootId}");
                if (context.User.IsMerchantAdmin)    //若是商管
                {
                    if (org is not null)
                        result.Result.Add(org);
                    else
                        result.Result.AddRange(orgs.Data.Values.Where(c => c.ParentId is null));
                }
                else //非商管
                {
                    var currCo = _OrganizationManager.GetCurrentCompanyByUser(context.User);
                    if (currCo is null) return BadRequest("当前用户未登录到一个机构。");
                    if (OwHelper.GetAllSubItemsOfTree(currCo, c => c.Children).FirstOrDefault(c => c.Id == rootId.Value) is not PlOrganization currOrg)
                        return StatusCode((int)HttpStatusCode.Forbidden, "用户只能获取当前登录公司及其子机构");
                    result.Result.Add(currOrg);
                }
            }
            else //若没有指定根
            {
                if (context.User.IsMerchantAdmin)    //若是商管
                {
                    result.Result.AddRange(orgs.Data.Values.Where(c => c.Parent is null));
                }
                else //非商管
                {
                    var currCo = _OrganizationManager.GetCurrentCompanyByUser(context.User);
                    if (currCo is null) return BadRequest("当前用户未登录到一个机构。");
                    result.Result.Add(currCo);
                }
            }
            return result;
        }

        /// <summary>
        /// 增加一个组织机构。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpPost]
        public ActionResult<AddOrgReturnDto> AddOrg(AddOrgParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new AddOrgReturnDto();
            model.Item.GenerateNewId();
            var id = model.Item.Id;
            _DbContext.PlOrganizations.Add(model.Item);
            try
            {
                _DbContext.SaveChanges();
                result.Id = id;
                if (model.IsCopyDataDic) //若需要复制字典
                {
                    var r = CopyDataDic(new CopyDataDicParamsDto { Token = model.Token, Id = id });
                }
            }
            catch (Exception err)
            {
                return BadRequest(err.Message);
            }
            return result;
        }

        /// <summary>
        /// 修改已有组织机构。不能修改父子关系。请使用 AddOrgRelation 和 RemoveOrgRelation修改。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPut]
        public ActionResult<ModifyOrgReturnDto> ModifyOrg(ModifyOrgParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized(OwHelper.GetLastErrorMessage());
            string err;
            if (!_AuthorizationManager.Demand(out err, "B.1")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new ModifyOrgReturnDto();
            var list = new List<PlOrganization>();
            List<(PlOrganization, IEnumerable<PlOrganization>)> restore = new List<(PlOrganization, IEnumerable<PlOrganization>)>();
            var ids = model.Items.Select(c => c.Id).ToArray();
            _DbContext.PlOrganizations.Where(c => ids.Contains(c.Id)).Load();
            foreach (var item in model.Items)
            {
                if (_DbContext.PlOrganizations.Find(item.Id) is PlOrganization org)
                    restore.Add((item, item.Children.ToArray()));
                else
                    return BadRequest($"指定的机构中至少一个不存在，Id={item.Id}");
            }

            if (!_EntityManager.Modify(model.Items, list)) return NotFound();

            //list.ForEach(tmp =>
            //{
            //    var entity = _DbContext.Entry(tmp);
            //    entity.Navigation(nameof(PlOrganization.Children)).IsModified = false;
            //    entity.Collection(c => c.Children).IsModified = false;
            //});
            try
            {
                restore.ForEach(c =>
                {
                    c.Item1.Children.Clear();
                    c.Item1.Children.AddRange(c.Item2);
                });
                _DbContext.SaveChanges();
                var merchIds = restore.Select(c =>
                 {
                     _MerchantManager.TryGetIdByOrgOrMerchantId(c.Item1.Id, out var r);
                     return r;
                 }).Distinct().ToArray();
                foreach (var item in merchIds)
                {
                    if (!item.HasValue) continue;
                    _MerchantManager.GetCacheItemById(item.Value)?.CancellationTokenSource.Cancel();
                }
            }
            catch (Exception excp)
            {
                return BadRequest(excp.Message);
            }
            return result;
        }

        /// <summary>
        /// 增加机构父子关系的功能。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">找不到指定Id的对象。</response>  
        [HttpPost]
        public ActionResult<AddOrgRelationReturnDto> AddOrgRelation(AddOrgRelationParamsDto model)
        {
            var result = new AddOrgRelationReturnDto();
            var parent = _DbContext.PlOrganizations.Find(model.ParentId);
            if (parent is null) return BadRequest($"找不到{model.ParentId}指定的机构对象");
            var child = _DbContext.PlOrganizations.Find(model.ChildId);
            if (child is null) return BadRequest($"找不到{model.ChildId}指定的机构对象");
            parent.Children.Add(child);
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 删除机构父子关系的功能。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">找不到指定Id的对象 -或- 不是父对象的孩子 -或- 子对象还有孩子。</response> 
        [HttpDelete]
        public ActionResult<RemoveOrgRelationReturnDto> RemoveOrgRelation(RemoveOrgRelationParamsDto model)
        {
            var result = new RemoveOrgRelationReturnDto();
            var parent = _DbContext.PlOrganizations.Find(model.ParentId);
            if (parent is null) return BadRequest($"找不到{model.ParentId}指定的机构对象");
            var child = parent.Children.FirstOrDefault(c => c.Id == model.ChildId);
            if (child is null || child.Children.Count > 0) return BadRequest($"找不到{model.ChildId}指定的机构对象或不是父对象的孩子。 -或- 子对象还有孩子");
            parent.Children.Remove(child);
            _DbContext.Remove(child);
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 删除一个组织机构。该机构必须没有子组织机构。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定实体的Id不存在。通常这是Bug.在极端情况下可能是并发问题。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpDelete]
        public ActionResult<RemoveOrgReturnDto> RemoveOrg(RemoveOrgParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemoveOrgReturnDto();
            var id = model.Id;
            var dbSet = _DbContext.PlOrganizations;
            var item = dbSet.Find(id);
            if (item is null) return BadRequest();
            _EntityManager.Remove(item);
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 通过组织机构Id获取所属的商户Id。
        /// </summary>
        /// <param name="token">登录令牌。</param>
        /// <param name="orgId">这个必须是组织机构Id，若是商户Id则返回错误。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定实体的Id不存在。通常这是Bug.在极端情况下可能是并发问题。</response>  
        /// <response code="401">无效令牌。</response>
        [HttpGet]
        public ActionResult<GetMerchantIdReturnDto> GetMerchantId(Guid token, Guid orgId)
        {
            if (_AccountManager.GetOrLoadContextByToken(token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetMerchantIdReturnDto();
            if (!_MerchantManager.TryGetIdByOrgOrMerchantId(orgId, out var merchId))
                return BadRequest();
            result.Result = merchId;
            return result;
        }

        #region 用户和商户/组织机构的所属关系的CRUD

        /// <summary>
        /// 获取用户和商户/组织机构的所属关系。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定类别Id无效。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllAccountPlOrganizationReturnDto> GetAllAccountPlOrganization([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllAccountPlOrganizationReturnDto();

            var dbSet = _DbContext.AccountPlOrganizations;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            foreach (var item in conditional)
                if (string.Equals(item.Key, "accountId", StringComparison.OrdinalIgnoreCase))
                {
                    if (Guid.TryParse(item.Value, out var id))
                        coll = coll.Where(c => c.UserId == id);
                }
                else if (string.Equals(item.Key, "orgId", StringComparison.OrdinalIgnoreCase))
                {
                    if (Guid.TryParse(item.Value, out var id))
                        coll = coll.Where(c => c.OrgId == id);
                }
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 增加用户和商户/组织机构的所属关系。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">在同一类别同一组织机构下指定了重复的Code。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpPost]
        public ActionResult<AddAccountPlOrganizationReturnDto> AddAccountPlOrganization(AddAccountPlOrganizationParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new AddAccountPlOrganizationReturnDto();
            _DbContext.AccountPlOrganizations.Add(model.Item);
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 删除用户和商户/组织机构的所属关系。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定实体的Id不存在。通常这是Bug.在极端情况下可能是并发问题。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpDelete]
        public ActionResult<RemoveAccountPlOrganizationReturnDto> RemoveAccountPlOrganization(RemoveAccountPlOrganizationParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemoveAccountPlOrganizationReturnDto();
            DbSet<AccountPlOrganization> dbSet = _DbContext.AccountPlOrganizations;
            var item = dbSet.Find(model.UserId, model.OrgId);
            if (item is null) return BadRequest();
            _EntityManager.Remove(item);
            _DbContext.SaveChanges();
            return result;
        }

        #endregion 用户和商户/组织机构的所属关系的CRUD

        /// <summary>
        /// 为指定机构复制一份所有数据字典的副本。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的机构不存在。</response>  
        [HttpPut]
        public ActionResult<CopyDataDicReturnDto> CopyDataDic(CopyDataDicParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new CopyDataDicReturnDto();
            var merch = _DbContext.PlOrganizations.Find(model.Id);
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

        #region 开户行信息

        /// <summary>
        /// 获取全部客户开户行信息。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">查询的条件。支持 ParentId(机构id)，Id,Number。不区分大小写。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllBankInfoReturnDto> GetAllBankInfo([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllBankInfoReturnDto();

            var dbSet = _DbContext.BankInfos;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            foreach (var item in conditional)
                if (string.Equals(item.Key, "Id", StringComparison.OrdinalIgnoreCase))
                {
                    if (Guid.TryParse(item.Value, out var id))
                        coll = coll.Where(c => c.Id == id);
                }
                else if (string.Equals(item.Key, "ParentId", StringComparison.OrdinalIgnoreCase))
                {
                    if (Guid.TryParse(item.Value, out var id))
                        coll = coll.Where(c => c.ParentId == id);
                }
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 增加新客户开户行信息。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpPost]
        public ActionResult<AddBankInfoReturnDto> AddBankInfo(AddBankInfoParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new AddBankInfoReturnDto();
            model.BankInfo.GenerateNewId();
            _DbContext.BankInfos.Add(model.BankInfo);
            _DbContext.SaveChanges();
            result.Id = model.BankInfo.Id;
            return result;
        }

        /// <summary>
        /// 修改客户开户行信息信息。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        /// <response code="404">指定Id的客户开户行信息不存在。</response>  
        [HttpPut]
        public ActionResult<ModifyBankInfoReturnDto> ModifyBankInfo(ModifyBankInfoParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            if (!_AuthorizationManager.Demand(out err, "B.1")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new ModifyBankInfoReturnDto();
            if (!_EntityManager.Modify(new[] { model.BankInfo })) return NotFound();
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 删除指定Id的客户开户行信息。慎用！
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的客户开户行信息不存在。</response>  
        [HttpDelete]
        public ActionResult<RemoveBankInfoReturnDto> RemoveBankInfo(RemoveBankInfoParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemoveBankInfoReturnDto();
            var id = model.Id;
            var dbSet = _DbContext.BankInfos;
            var item = dbSet.Find(id);
            if (item is null) return BadRequest();
            _EntityManager.Remove(item);
            _DbContext.SaveChanges();
            return result;
        }

        #endregion 开户行信息

    }

}
