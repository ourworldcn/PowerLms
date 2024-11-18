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
        public OrganizationController(AccountManager accountManager, IServiceProvider serviceProvider, PowerLmsUserDbContext dbContext, OrganizationManager organizationManager, IMapper mapper, EntityManager entityManager, DataDicManager dataManager)
        {
            _AccountManager = accountManager;
            _ServiceProvider = serviceProvider;
            _DbContext = dbContext;
            _OrganizationManager = organizationManager;
            _Mapper = mapper;
            _EntityManager = entityManager;
            _DataManager = dataManager;
        }

        readonly AccountManager _AccountManager;
        readonly IServiceProvider _ServiceProvider;
        readonly PowerLmsUserDbContext _DbContext;
        readonly OrganizationManager _OrganizationManager;
        readonly IMapper _Mapper;
        readonly EntityManager _EntityManager;
        readonly DataDicManager _DataManager;

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
            if (_AccountManager.GetOrLoadAccountFromToken(token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
            if (_AccountManager.GetOrLoadAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
        /// 修改组织机构。不能修改父子关系。请使用 AddOrgRelation 和 RemoveOrgRelation修改。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpPut]
        public ActionResult<ModifyOrgReturnDto> ModifyOrg(ModifyOrgParamsDto model)
        {
            if (_AccountManager.GetOrLoadAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized(OwHelper.GetLastErrorMessage());
            var result = new ModifyOrgReturnDto();
            var list = new List<PlOrganization>();
            var res = model.Items.Select(c => (_DbContext.PlOrganizations.Find(c.Id), _DbContext.PlOrganizations.Find(c.Id).Children.ToArray())).ToArray();
            if (!_EntityManager.Modify(model.Items, list)) return NotFound();

            //list.ForEach(tmp =>
            //{
            //    var entity = _DbContext.Entry(tmp);
            //    entity.Navigation(nameof(PlOrganization.Children)).IsModified = false;
            //    entity.Collection(c => c.Children).IsModified = false;
            //});
            res.ForEach(c =>
            {
                c.Item1.Children.Clear();
                c.Item1.Children.AddRange(c.Item2);
            });
            _DbContext.SaveChanges();
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
            if (_AccountManager.GetOrLoadAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
            if (_AccountManager.GetOrLoadAccountFromToken(token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
            if (_AccountManager.GetOrLoadAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
            if (_AccountManager.GetOrLoadAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
            if (_AccountManager.GetOrLoadAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
            if (_AccountManager.GetOrLoadAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
            if (_AccountManager.GetOrLoadAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
            if (_AccountManager.GetOrLoadAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
        /// <response code="404">指定Id的客户开户行信息不存在。</response>  
        [HttpPut]
        public ActionResult<ModifyBankInfoReturnDto> ModifyBankInfo(ModifyBankInfoParamsDto model)
        {
            if (_AccountManager.GetOrLoadAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
            if (_AccountManager.GetOrLoadAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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

    /// <summary>
    /// 删除机构父子关系的功能参数封装类。
    /// </summary>
    public class RemoveOrgRelationParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 父Id。
        /// </summary>
        public Guid ParentId { get; set; }

        /// <summary>
        /// 子Id。
        /// </summary>
        public Guid ChildId { get; set; }
    }

    /// <summary>
    /// 删除机构父子关系的功能返回值封装类。
    /// </summary>
    public class RemoveOrgRelationReturnDto : ReturnDtoBase
    {
    }

    /// <summary>
    /// 增加机构父子关系的功能参数封装类。
    /// </summary>
    public class AddOrgRelationParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 父Id。
        /// </summary>
        public Guid ParentId { get; set; }

        /// <summary>
        /// 子Id。
        /// </summary>
        public Guid ChildId { get; set; }
    }

    /// <summary>
    /// 增加机构父子关系的功能返回值封装类。
    /// </summary>
    public class AddOrgRelationReturnDto : ReturnDtoBase
    {
    }

    #region 开户行信息
    /// <summary>
    /// 标记删除开户行信息功能的参数封装类。
    /// </summary>
    public class RemoveBankInfoParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 标记删除开户行信息功能的返回值封装类。
    /// </summary>
    public class RemoveBankInfoReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 获取所有开户行信息功能的返回值封装类。
    /// </summary>
    public class GetAllBankInfoReturnDto : PagingReturnDtoBase<BankInfo>
    {
    }

    /// <summary>
    /// 增加新开户行信息功能参数封装类。
    /// </summary>
    public class AddBankInfoParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 新开户行信息信息。其中Id可以是任何值，返回时会指定新值。
        /// </summary>
        public BankInfo BankInfo { get; set; }
    }

    /// <summary>
    /// 增加新开户行信息功能返回值封装类。
    /// </summary>
    public class AddBankInfoReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 如果成功添加，这里返回新开户行信息的Id。
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 修改开户行信息信息功能参数封装类。
    /// </summary>
    public class ModifyBankInfoParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 开户行信息数据。
        /// </summary>
        public BankInfo BankInfo { get; set; }
    }

    /// <summary>
    /// 修改开户行信息信息功能返回值封装类。
    /// </summary>
    public class ModifyBankInfoReturnDto : ReturnDtoBase
    {
    }
    #endregion 开户行信息

    /// <summary>
    /// 初始化机构的功能参数封装类。
    /// </summary>
    public class CopyDataDicParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 初始化机构的Id。
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 初始化机构的功能返回值封装类。
    /// </summary>
    public class CopyDataDicReturnDto : ReturnDtoBase
    {
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
        /// <summary>
        /// 是否给新加入的机构复制一份完整的数据字典。
        /// </summary>
        public bool IsCopyDataDic { get; set; }
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
