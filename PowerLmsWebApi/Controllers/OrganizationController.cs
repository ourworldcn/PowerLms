using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.Internal;
using PowerLms.Data;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Dto;
using System.ComponentModel.DataAnnotations;

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
        public OrganizationController(AccountManager accountManager, IServiceProvider serviceProvider, PowerLmsUserDbContext dbContext, OrganizationManager organizationManager, IMapper mapper, EntityManager entityManager)
        {
            _AccountManager = accountManager;
            _ServiceProvider = serviceProvider;
            _DbContext = dbContext;
            _OrganizationManager = organizationManager;
            _Mapper = mapper;
            _EntityManager = entityManager;
        }

        readonly AccountManager _AccountManager;
        readonly IServiceProvider _ServiceProvider;
        readonly PowerLmsUserDbContext _DbContext;
        readonly OrganizationManager _OrganizationManager;
        readonly IMapper _Mapper;
        readonly EntityManager _EntityManager;

        /// <summary>
        /// 获取组织机构。暂不考虑分页。
        /// </summary>
        /// <param name="token"></param>
        /// <param name="rootId">根组织机构的Id。或商户的Id。省略或为null时，对商管将返回该商户下多个组织机构。</param>
        /// <param name="includeChildren">是否包含子机构。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="400">用户身份错误。通常是非管理员试图获取所有组织机构信息，未来权限定义可能更改这个要求。</response>  
        [HttpGet]
        public ActionResult<GetOrgReturnDto> GetOrg(Guid token, Guid? rootId, bool includeChildren)
        {
            var result = new GetOrgReturnDto();
            if (_AccountManager.GetAccountFromToken(token, _ServiceProvider) is not OwContext context) return Unauthorized();
            if (_DbContext.Merchants.Find(rootId) is PlMerchant merch)   //若指定的是商户
            {
                if ((context.User.State & 8) == 0 && (context.User.State & 4) == 0) return BadRequest();
                _OrganizationManager.GetMerchantId(context.User.Id, out var merchId);

                var orgs = _DbContext.PlOrganizations.Where(c => c.MerchantId == merchId).ToList();

                if (!includeChildren)
                {
                    orgs.ForEach(c =>
                    {
                        _DbContext.Entry(c).State = EntityState.Detached;
                        c.Children.Clear();
                    });
                }
                result.Result.AddRange(orgs);
            }
            else //指定了机构
            {
                var root = _DbContext.PlOrganizations.Find(rootId);  //获取商户
                if (!includeChildren)
                {
                    _DbContext.Entry(root).State = EntityState.Detached;
                    root.Children.Clear();
                }
                result.Result.Add(root);
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
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new AddOrgReturnDto();
            model.Item.GenerateNewId();
            var id = model.Item.Id;
            _DbContext.PlOrganizations.Add(model.Item);
            _DbContext.SaveChanges();
            result.Id = id;
            return result;
        }

        /// <summary>
        /// 修改组织机构。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpPut]
        public ActionResult<ModifyOrgReturnDto> ModifyOrg(ModifyOrgParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized(OwHelper.GetLastErrorMessage());
            var result = new ModifyOrgReturnDto();
            var list = new List<PlOrganization>();
            if (!_EntityManager.Modify(model.Items, list)) return NotFound();

            list.ForEach(tmp =>
            {
                var entity = _DbContext.Entry(tmp);
                entity.Collection(c => c.Children).IsModified = false;
            });
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
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemoveOrgReturnDto();
            var id = model.Id;
            var dbSet = _DbContext.PlOrganizations;
            var item = dbSet.Find(id);
            if (item is null) return BadRequest();
            dbSet.Remove(item);
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
            if (_AccountManager.GetAccountFromToken(token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetMerchantIdReturnDto();
            if (!_OrganizationManager.GetMerchantIdFromOrgId(orgId, out var merchId))
                return BadRequest();
            result.Result = merchId;
            return result;
        }

        #region 用户和商户/组织机构的所属关系的CRUD

        /// <summary>
        /// 获取用户和商户/组织机构的所属关系。
        /// </summary>
        /// <param name="token">登录令牌。</param>
        /// <param name="accountId">账号Id。null表示不限定。和<paramref name="orgId"/>限定为与的关系。</param>
        /// <param name="orgId">商户及组织机构类别的Id,null表示不限定。和<paramref name="accountId"/>限定为与的关系。</param>
        /// <param name="startIndex">起始位置，从0开始。</param>
        /// <param name="count">最大返回数量。-1表示全返回。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定类别Id无效。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllAccountPlOrganizationReturnDto> GetAllAccountPlOrganization(Guid token, Guid? accountId, Guid? orgId,
            [Range(0, int.MaxValue, ErrorMessage = "必须大于或等于0.")] int startIndex, [FromQuery][Range(-1, int.MaxValue)] int count = -1)
        {
            if (_AccountManager.GetAccountFromToken(token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllAccountPlOrganizationReturnDto();
            var coll = _DbContext.AccountPlOrganizations.AsNoTracking();
            if (accountId is not null)
                coll = coll.Where(c => c.UserId == accountId);
            if (orgId is not null)
                coll = coll.Where(c => c.OrgId == orgId);
            var prb = _EntityManager.GetAll(coll, startIndex, count);
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
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemoveAccountPlOrganizationReturnDto();
            DbSet<AccountPlOrganization> dbSet = _DbContext.AccountPlOrganizations;
            var item = dbSet.Find(model.UserId, model.OrgId);
            if (item is null) return BadRequest();
            _DbContext.Remove(item);
            _DbContext.SaveChanges();
            return result;
        }

        #endregion 用户和商户/组织机构的所属关系的CRUD

    }

    #region 用户和商户/组织机构的所属关系的CRUD
    /// <summary>
    /// 获取用户和商户/组织机构的所属关系返回值封装类。
    /// </summary>
    public class GetAllAccountPlOrganizationReturnDto : PagingReturnDtoBase<AccountPlOrganization>
    {
    }

    /// <summary>
    /// 删除用户和商户/组织机构的所属关系的功能参数封装类。
    /// </summary>
    public class RemoveAccountPlOrganizationParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 用户的Id。
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// 商户/组织机构的Id。
        /// </summary>
        public Guid OrgId { get; set; }
    }

    /// <summary>
    /// 删除用户和商户/组织机构的所属关系的功能返回值封装类。
    /// </summary>
    public class RemoveAccountPlOrganizationReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 增加用户和商户/组织机构的所属关系的功能参数封装类，
    /// </summary>
    public class AddAccountPlOrganizationParamsDto : AddParamsDtoBase<AccountPlOrganization>
    {
    }

    /// <summary>
    /// 增加用户和商户/组织机构的所属关系的功能返回值封装类。
    /// </summary>
    public class AddAccountPlOrganizationReturnDto : ReturnDtoBase
    {
    }

    /// <summary>
    /// 获取用户和商户/组织机构的所属关系功能的返回值封装类。
    /// </summary>
    public class GetAllAccountReturnDto : PagingReturnDtoBase<Account>
    {
    }

    #endregion 用户和商户/组织机构的所属关系的CRUD

    /// <summary>
    /// 通过组织机构Id获取所属的商户Id的功能返回值封装类。
    /// </summary>
    public class GetMerchantIdReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 商户Id。注意，理论上允许组织机构不属于任何商户，则此处返回null。
        /// </summary>
        public Guid? Result { get; set; }
    }

    /// <summary>
    /// 删除组织机构的功能参数封装类。
    /// </summary>
    public class RemoveOrgParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 删除组织机构的功能返回值封装类。
    /// </summary>
    public class RemoveOrgReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 增加一个组织机构的功能参数封装类。
    /// </summary>
    public class AddOrgParamsDto : AddParamsDtoBase<PlOrganization>
    {
    }

    /// <summary>
    /// 增加一个组织机构的功能返回值封装类。
    /// </summary>
    public class AddOrgReturnDto : AddReturnDtoBase
    {
    }

    /// <summary>
    /// 修改组织机构功能的参数封装类。
    /// </summary>
    public class ModifyOrgParamsDto : ModifyParamsDtoBase<PlOrganization>
    {

    }

    /// <summary>
    /// 修改组织机构功能的返回值封装类。
    /// </summary>
    public class ModifyOrgReturnDto : ModifyReturnDtoBase
    {
    }
}
