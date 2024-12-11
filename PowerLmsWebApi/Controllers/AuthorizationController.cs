using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NPOI.SS.Formula.Functions;
using PowerLms.Data;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Dto;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Net;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// 权限系统相关操作的控制器。
    /// </summary>
    public class AuthorizationController : PlControllerBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public AuthorizationController(IServiceProvider serviceProvider, AccountManager accountManager, PowerLmsUserDbContext dbContext, EntityManager entityManager,
            IMapper mapper, AuthorizationManager authorizationManager, OrganizationManager organizationManager, MerchantManager merchantManager,
            PermissionManager permissionManager, RoleManager roleManager)
        {
            _ServiceProvider = serviceProvider;
            _AccountManager = accountManager;
            _DbContext = dbContext;
            _EntityManager = entityManager;
            _Mapper = mapper;
            _AuthorizationManager = authorizationManager;
            _OrganizationManager = organizationManager;
            _MerchantManager = merchantManager;
            _PermissionManager = permissionManager;
            _RoleManager = roleManager;
        }

        readonly IServiceProvider _ServiceProvider;
        readonly AccountManager _AccountManager;
        readonly PowerLmsUserDbContext _DbContext;
        readonly EntityManager _EntityManager;
        readonly IMapper _Mapper;
        readonly AuthorizationManager _AuthorizationManager;
        readonly OrganizationManager _OrganizationManager;
        readonly MerchantManager _MerchantManager;
        readonly PermissionManager _PermissionManager;
        readonly RoleManager _RoleManager;

        #region 角色的CRUD

        /// <summary>
        /// 
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional"></param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult<bool> IsAuthorized1([FromQuery] PagingParamsBase model, [FromQuery] Dictionary<string, string> conditional = null)
        {
            return true;
        }

        /// <summary>
        /// 获取全部角色。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">查询的条件。支持 name，ShortName，displayname，Id。不区分大小写。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllPlRoleReturnDto> GetAllPlRole([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllPlRoleReturnDto();
            var dbSet = _DbContext.PlRoles;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
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
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 增加新角色。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpPost]
        public ActionResult<AddPlRoleReturnDto> AddPlRole(AddPlRoleParamsDto model)
        {
            if (_AccountManager.GetOrLoadAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new AddPlRoleReturnDto();
            model.Item.GenerateNewId();
            if (!model.Item.OrgId.HasValue)
            {
                if (_MerchantManager.GetMerchantIdByUserId(context.User.Id, out var mId))
                    model.Item.OrgId = mId;
            }
            model.Item.CreateBy ??= context.User.Id;
            model.Item.CreateDateTime = OwHelper.WorldNow;
            _DbContext.PlRoles.Add(model.Item);
            _DbContext.SaveChanges();
            result.Id = model.Item.Id;


            if (model.Item.OrgId.HasValue)
            {
                if (_MerchantManager.GetMerchantIdByOrgId(model.Item.OrgId.Value, out var merchId))
                    _RoleManager.GetRolesCacheItemByMerchantId(merchId.Value)?.CancellationTokenSource.Cancel();
            }
            return result;
        }

        /// <summary>
        /// 修改角色信息。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的角色不存在。</response>  
        [HttpPut]
        public ActionResult<ModifyPlRoleReturnDto> ModifyPlRole(ModifyPlRoleParamsDto model)
        {
            if (_AccountManager.GetOrLoadAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyPlRoleReturnDto();
            if (!_EntityManager.Modify(new[] { model.PlRole })) return NotFound();
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 删除指定Id的角色。慎用！
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的角色不存在。</response>  
        [HttpDelete]
        public ActionResult<RemovePlRoleReturnDto> RemovePlRole(RemovePlRoleParamsDto model)
        {
            if (_AccountManager.GetOrLoadAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemovePlRoleReturnDto();
            var userIds = _DbContext.PlAccountRoles.Where(c => model.Id == c.RoleId).Select(c => c.UserId).ToArray();
            var id = model.Id;
            var dbSet = _DbContext.PlRoles;
            var item = dbSet.Find(id);
            if (item is null) return BadRequest();
            Guid? merchantId = null;
            if (item.OrgId.HasValue)
                _MerchantManager.GetMerchantIdByOrgId(item.OrgId.Value, out merchantId);
            _EntityManager.Remove(item);
            _DbContext.SaveChanges();

            if (merchantId.HasValue)
                _OrganizationManager.GetOrgsCacheItemByMerchantId(merchantId.Value)?.CancellationTokenSource.Cancel();
            return result;
        }
        #endregion 角色的CRUD

        #region 权限的CRUD

        /// <summary>
        /// 获取全部权限。Name=root返回总根。由于返回嵌套类，无法支持分页。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">查询的条件。支持 name，ShortName，displayname。不区分大小写。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllPlPermissionReturnDto> GetAllPlPermission([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllPlPermissionReturnDto();
            var dbSet = _DbContext.PlPermissions;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsQueryable();
            foreach (var item in conditional)
                if (string.Equals(item.Key, "name", StringComparison.OrdinalIgnoreCase))
                {
                    coll = coll.Where(c => c.Name.Contains(item.Value));
                }
                else if (string.Equals(item.Key, "ShortName", StringComparison.OrdinalIgnoreCase))
                {
                    coll = coll.Where(c => c.ShortName.Contains(item.Value));
                }
                else if (string.Equals(item.Key, "displayname", StringComparison.OrdinalIgnoreCase))
                {
                    coll = coll.Where(c => c.DisplayName.Contains(item.Value));
                }
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 增加新权限。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpPost]
        public ActionResult<AddPlPermissionReturnDto> AddPlPermission(AddPlPermissionParamsDto model)
        {
            if (_AccountManager.GetOrLoadAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new AddPlPermissionReturnDto();
            //model.PlPermission.GenerateNewId();
            _DbContext.PlPermissions.Add(model.PlPermission);
            _DbContext.SaveChanges();
            result.Id = model.PlPermission.Name;
            _PermissionManager.GetPermission()?.CancellationTokenSource.Cancel();
            return result;
        }

        /// <summary>
        /// 修改权限信息。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的权限不存在。</response>  
        [HttpPut]
        public ActionResult<ModifyPlPermissionReturnDto> ModifyPlPermission(ModifyPlPermissionParamsDto model)
        {
            if (_AccountManager.GetOrLoadAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyPlPermissionReturnDto();
            var dbSet = _DbContext.PlPermissions;
            var tmp = dbSet.Find(model.Item.Name);
            Debug.Assert(tmp is not null);
            var entity = _DbContext.Entry(tmp);
            entity.CurrentValues.SetValues(model.Item);
            try
            {
                _Mapper.Map(model.Item, tmp);
            }
            catch (AutoMapperMappingException)  //忽略不能映射的情况
            {
            }

            if (tmp is ICreatorInfo ci) //若实现创建信息接口
            {
                entity.Property(nameof(ci.CreateBy)).IsModified = false;
                entity.Property(nameof(ci.CreateDateTime)).IsModified = false;
            }

            _DbContext.SaveChanges();
            _PermissionManager.GetPermission()?.CancellationTokenSource.Cancel();
            return result;
        }

        /// <summary>
        /// 删除指定Id的权限。慎用！
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的权限不存在。</response>  
        [HttpDelete]
        public ActionResult<RemovePlPermissionReturnDto> RemovePlPermission(RemovePlPermissionParamsDto model)
        {
            if (_AccountManager.GetOrLoadAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemovePlPermissionReturnDto();
            var id = model.Id;
            var dbSet = _DbContext.PlPermissions;
            var item = dbSet.Find(id);
            if (item is null) return BadRequest();
            _EntityManager.Remove(item);
            _DbContext.SaveChanges();
            _PermissionManager.GetPermission()?.CancellationTokenSource.Cancel();
            return result;
        }
        #endregion 权限的CRUD

        #region 用户-角色关系的CRUD

        /// <summary>
        /// 获取全部用户-角色关系。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">查询的条件。支持 UserId，RoleId。不区分大小写。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllAccountRoleReturnDto> GetAllAccountRole([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllAccountRoleReturnDto();
            var dbSet = _DbContext.PlAccountRoles;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            foreach (var item in conditional)
                if (string.Equals(item.Key, "UserId", StringComparison.OrdinalIgnoreCase))
                {
                    if (Guid.TryParse(item.Value, out var id))
                        coll = coll.Where(c => c.UserId == id);
                }
                else if (string.Equals(item.Key, "RoleId", StringComparison.OrdinalIgnoreCase))
                {
                    if (Guid.TryParse(item.Value, out var id))
                        coll = coll.Where(c => c.RoleId == id);
                }
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 增加新用户-角色关系。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpPost]
        public ActionResult<AddAccountRoleReturnDto> AddAccountRole(AddAccountRoleParamsDto model)
        {
            if (_AccountManager.GetOrLoadAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new AddAccountRoleReturnDto();
            //model.AccountRole.GenerateNewId();
            _DbContext.PlAccountRoles.Add(model.AccountRole);
            _DbContext.SaveChanges();

            _RoleManager.GetCurrentRolesCacheItem(model.AccountRole.UserId)?.CancellationTokenSource.Cancel();
            return result;
        }

        /// <summary>
        /// 删除指定Id的用户-角色关系。慎用！
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的用户-角色关系不存在。</response>  
        [HttpDelete]
        public ActionResult<RemoveAccountRoleReturnDto> RemoveAccountRole(RemoveAccountRoleParamsDto model)
        {
            if (_AccountManager.GetOrLoadAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemoveAccountRoleReturnDto();

            var dbSet = _DbContext.PlAccountRoles;
            var item = dbSet.Find(model.UserId, model.RoleId);
            if (item is null) return BadRequest();
            _EntityManager.Remove(item);
            _DbContext.SaveChanges();
            _RoleManager.GetCurrentRolesCacheItem(model.UserId)?.CancellationTokenSource.Cancel();
            return result;
        }
        #endregion 用户-角色关系的CRUD

        #region 角色-权限关系的CRUD

        /// <summary>
        /// 获取全部角色-权限关系。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">查询的条件。支持 PermissionId，RoleId。不区分大小写。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllRolePermissionReturnDto> GetAllRolePermission([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllRolePermissionReturnDto();
            var dbSet = _DbContext.PlRolePermissions;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            foreach (var item in conditional)
                if (string.Equals(item.Key, "PermissionId", StringComparison.OrdinalIgnoreCase))
                {
                    coll = coll.Where(c => c.PermissionId == item.Value);
                }
                else if (string.Equals(item.Key, "RoleId", StringComparison.OrdinalIgnoreCase))
                {
                    if (Guid.TryParse(item.Value, out var id))
                        coll = coll.Where(c => c.RoleId == id);
                }
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 增加新角色-权限关系。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpPost]
        public ActionResult<AddRolePermissionReturnDto> AddRolePermission(AddRolePermissionParamsDto model)
        {
            if (_AccountManager.GetOrLoadAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new AddRolePermissionReturnDto();

            _DbContext.PlRolePermissions.Add(model.RolePermission);
            _DbContext.SaveChanges();

            var userIds = _DbContext.PlAccountRoles.Where(c => model.RolePermission.RoleId == c.RoleId).Select(c => c.UserId).ToArray();

            userIds.ForEach(c => _PermissionManager.GetCurrentPermissions(c)?.CancellationTokenSource.Cancel());
            return result;
        }

        /// <summary>
        /// 删除指定Id的角色-权限关系。慎用！
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的角色-权限关系不存在。</response>  
        [HttpDelete]
        public ActionResult<RemoveRolePermissionReturnDto> RemoveRolePermission(RemoveRolePermissionParamsDto model)
        {
            if (_AccountManager.GetOrLoadAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemoveRolePermissionReturnDto();

            var dbSet = _DbContext.PlRolePermissions;
            var item = dbSet.Find(model.RoleId, model.PermissionId);
            if (item is null) return BadRequest();
            _EntityManager.Remove(item);
            _DbContext.SaveChanges();
            var userIds = _DbContext.PlAccountRoles.Where(c => model.RoleId == c.RoleId).Select(c => c.UserId).ToArray();
            userIds.ForEach(c => _PermissionManager.GetCurrentPermissions(c)?.CancellationTokenSource.Cancel());
            return result;
        }
        #endregion 角色-权限关系的CRUD

        /// <summary>
        /// 设置角色的所属用户。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="400">参数错误。</response>  
        [HttpPut]
        public ActionResult<SetUsersReturnDto> SetUsers(SetUsersParamsDto model)
        {
            if (_AccountManager.GetOrLoadAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new SetUsersReturnDto();
            var ids = new HashSet<Guid>(model.UserIds);
            if (ids.Count != model.UserIds.Count) return BadRequest($"{nameof(model.UserIds)}中有重复键值。");

            var count = _DbContext.PlRoles.Count(c => ids.Contains(c.Id));
            if (count != ids.Count) return BadRequest($"{nameof(model.UserIds)}中至少有一个用户Id不存在。");

            var removes = _DbContext.PlAccountRoles.Where(c => c.RoleId == model.RoleId && !ids.Contains(c.UserId));
            _DbContext.PlAccountRoles.RemoveRange(removes);

            var adds = ids.Except(_DbContext.PlAccountRoles.Where(c => c.RoleId == model.RoleId).Select(c => c.UserId).AsEnumerable()).ToArray();
            _DbContext.PlAccountRoles.AddRange(adds.Select(c => new AccountRole { RoleId = model.RoleId, UserId = c }));
            _DbContext.SaveChanges();

            model.UserIds.ForEach(c => _PermissionManager.GetCurrentPermissions(c)?.CancellationTokenSource.Cancel());

            return result;
        }

        /// <summary>
        /// 设置角色的许可权限。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="400">参数错误。</response>  
        [HttpPut]
        public ActionResult<SetPermissionsReturnDto> SetPermissions(SetPermissionsParamsDto model)
        {
            if (_AccountManager.GetOrLoadAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new SetPermissionsReturnDto();

            var ids = new HashSet<string>(model.PermissionIds);
            if (ids.Count != model.PermissionIds.Count) return BadRequest($"{nameof(model.PermissionIds)}中有重复键值。");

            var count = _DbContext.PlPermissions.Count(c => ids.Contains(c.Name));
            if (count != ids.Count) return BadRequest($"{nameof(model.PermissionIds)}中至少有一个许可的Id不存在。");

            var setRela = _DbContext.PlRolePermissions;
            var removes = setRela.Where(c => c.RoleId == model.RoleId && !ids.Contains(c.PermissionId));
            setRela.RemoveRange(removes);

            var adds = ids.Except(setRela.Where(c => c.RoleId == model.RoleId).Select(c => c.PermissionId).AsEnumerable()).ToArray();
            setRela.AddRange(adds.Select(c => new RolePermission { RoleId = model.RoleId, PermissionId = c, CreateBy = context.User.Id }));
            _DbContext.SaveChanges();
            var userIds = _DbContext.PlAccountRoles.Where(c => model.RoleId == c.RoleId).Select(c => c.UserId).ToArray();
            userIds.ForEach(c => _PermissionManager.GetCurrentPermissions(c)?.CancellationTokenSource.Cancel());
            return result;
        }

        /// <summary>
        /// 获取当前用户在当前机构下的所有权限。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="400">参数错误。</response>  
        [HttpGet]
        public ActionResult<GetAllPermissionsInCurrentUserReturnDto> GetAllPermissionsInCurrentUser([FromQuery] GetAllPermissionsInCurrentUserParamsDto model)
        {
            if (_AccountManager.GetOrLoadAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllPermissionsInCurrentUserReturnDto();
            result.Permissions.AddRange(_PermissionManager.GetOrLoadCurrentPermissionsByUser(context.User).Data.Values);
            return result;
        }
    }


}
