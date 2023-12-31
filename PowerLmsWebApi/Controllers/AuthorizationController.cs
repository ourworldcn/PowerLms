﻿using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NPOI.SS.Formula.Functions;
using PowerLms.Data;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Dto;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

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
        public AuthorizationController(IServiceProvider serviceProvider, AccountManager accountManager, PowerLmsUserDbContext dbContext, EntityManager entityManager, IMapper mapper, AuthorizationManager authorizationManager)
        {
            _ServiceProvider = serviceProvider;
            _AccountManager = accountManager;
            _DbContext = dbContext;
            _EntityManager = entityManager;
            _Mapper = mapper;
            _AuthorizationManager = authorizationManager;
        }

        readonly IServiceProvider _ServiceProvider;
        readonly AccountManager _AccountManager;
        readonly PowerLmsUserDbContext _DbContext;
        readonly EntityManager _EntityManager;
        readonly IMapper _Mapper;
        readonly AuthorizationManager _AuthorizationManager;

        #region 角色的CRUD

        /// <summary>
        /// 获取全部角色。
        /// </summary>
        /// <param name="token">登录令牌。</param>
        /// <param name="startIndex">起始位置，从0开始。</param>
        /// <param name="count">最大返回数量。</param>
        /// <param name="conditional">查询的条件。支持 name，ShortName，displayname，Id。不区分大小写。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllPlRoleReturnDto> GetAllPlRole(Guid token, [Range(0, int.MaxValue, ErrorMessage = "必须大于或等于0.")] int startIndex, [Range(-1, int.MaxValue)] int count = -1,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetAccountFromToken(token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllPlRoleReturnDto();
            var coll = _DbContext.PlRoles.AsNoTracking().OrderBy(c => c.Id).Skip(startIndex);
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
            var prb = _EntityManager.GetAll(coll, startIndex, count);
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
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new AddPlRoleReturnDto();
            model.PlRole.GenerateNewId();
            _DbContext.PlRoles.Add(model.PlRole);
            _DbContext.SaveChanges();
            result.Id = model.PlRole.Id;
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
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemovePlRoleReturnDto();
            var id = model.Id;
            var dbSet = _DbContext.PlRoles;
            var item = dbSet.Find(id);
            if (item is null) return BadRequest();
            _DbContext.SaveChanges();
            return result;
        }
        #endregion 角色的CRUD

        #region 权限的CRUD

        /// <summary>
        /// 获取全部权限。
        /// </summary>
        /// <param name="token">登录令牌。</param>
        /// <param name="startIndex">起始位置，从0开始。</param>
        /// <param name="count">最大返回数量。</param>
        /// <param name="conditional">查询的条件。支持 name，ShortName，displayname。不区分大小写。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllPlPermissionReturnDto> GetAllPlPermission(Guid token, [Range(0, int.MaxValue, ErrorMessage = "必须大于或等于0.")] int startIndex, [Range(-1, int.MaxValue)] int count = -1,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetAccountFromToken(token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllPlPermissionReturnDto();
            var coll = _DbContext.PlPermissions.AsNoTracking().OrderBy(c => c.Name).Skip(startIndex);
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
            var prb = _EntityManager.GetAll(coll, startIndex, count);
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
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new AddPlPermissionReturnDto();
            //model.PlPermission.GenerateNewId();
            _DbContext.PlPermissions.Add(model.PlPermission);
            _DbContext.SaveChanges();
            result.Id = model.PlPermission.Name;
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
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemovePlPermissionReturnDto();
            var id = model.Id;
            var dbSet = _DbContext.PlPermissions;
            var item = dbSet.Find(id);
            if (item is null) return BadRequest();
            _DbContext.SaveChanges();
            return result;
        }
        #endregion 权限的CRUD

        #region 用户-角色关系的CRUD

        /// <summary>
        /// 获取全部用户-角色关系。
        /// </summary>
        /// <param name="token">登录令牌。</param>
        /// <param name="startIndex">起始位置，从0开始。</param>
        /// <param name="count">最大返回数量。</param>
        /// <param name="conditional">查询的条件。支持 UserId，RoleId。不区分大小写。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllAccountRoleReturnDto> GetAllAccountRole(Guid token, [Range(0, int.MaxValue, ErrorMessage = "必须大于或等于0.")] int startIndex, [Range(-1, int.MaxValue)] int count = -1,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetAccountFromToken(token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllAccountRoleReturnDto();
            var coll = _DbContext.PlAccountRoles.AsNoTracking().OrderBy(c => c.UserId).Skip(startIndex);
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
            var prb = _EntityManager.GetAll(coll, startIndex, count);
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
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new AddAccountRoleReturnDto();
            //model.AccountRole.GenerateNewId();
            _DbContext.PlAccountRoles.Add(model.AccountRole);
            _DbContext.SaveChanges();
            //result.Id = model.AccountRole.Id;
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
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemoveAccountRoleReturnDto();
            var id = model.Id;
            var dbSet = _DbContext.PlAccountRoles;
            var item = dbSet.Find(id);
            if (item is null) return BadRequest();
            _DbContext.SaveChanges();
            return result;
        }
        #endregion 用户-角色关系的CRUD

        #region 角色-权限关系的CRUD

        /// <summary>
        /// 获取全部角色-权限关系。
        /// </summary>
        /// <param name="token">登录令牌。</param>
        /// <param name="startIndex">起始位置，从0开始。</param>
        /// <param name="count">最大返回数量。</param>
        /// <param name="conditional">查询的条件。支持 PermissionId，RoleId。不区分大小写。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllRolePermissionReturnDto> GetAllRolePermission(Guid token, [Range(0, int.MaxValue, ErrorMessage = "必须大于或等于0.")] int startIndex, [Range(-1, int.MaxValue)] int count = -1,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetAccountFromToken(token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllRolePermissionReturnDto();
            var coll = _DbContext.PlRolePermissions.AsNoTracking().OrderBy(c => c.RoleId).Skip(startIndex);
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
            var prb = _EntityManager.GetAll(coll, startIndex, count);
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
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new AddRolePermissionReturnDto();
            //model.RolePermission.GenerateNewId();
            _DbContext.PlRolePermissions.Add(model.RolePermission);
            _DbContext.SaveChanges();
            //result.Id = model.RolePermission.Id;
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
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemoveRolePermissionReturnDto();
            var id = model.Id;
            var dbSet = _DbContext.PlRolePermissions;
            var item = dbSet.Find(id);
            if (item is null) return BadRequest();
            _DbContext.SaveChanges();
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
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
            return result;
        }

    }

    /// <summary>
    /// 设置角色的许可权限功能的参数封装类。
    /// </summary>
    public class SetPermissionsParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 角色的Id。
        /// </summary>
        public Guid RoleId { get; set; }

        /// <summary>
        /// 所属许可的Id的集合。未在此集合指定的与角色的关系均被删除。
        /// </summary>
        public List<string> PermissionIds { get; set; } = new List<string>();
    }

    /// <summary>
    /// 设置角色的许可权限功能的返回值封装类。
    /// </summary>
    public class SetPermissionsReturnDto : ReturnDtoBase
    {
    }

    /// <summary>
    /// 设置角色的所属用户功能的参数封装类。
    /// </summary>
    public class SetUsersParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 角色的Id。
        /// </summary>
        public Guid RoleId { get; set; }

        /// <summary>
        /// 所属用户的Id的集合。未在此集合指定的与用户的关系均被删除。
        /// </summary>
        public List<Guid> UserIds { get; set; } = new List<Guid>();
    }

    /// <summary>
    /// 设置角色的所属用户功能的返回值封装类。
    /// </summary>
    public class SetUsersReturnDto : ReturnDtoBase
    {
    }

    #region 角色-权限关系
    /// <summary>
    /// 标记删除角色-权限关系功能的参数封装类。
    /// </summary>
    public class RemoveRolePermissionParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 标记删除角色-权限关系功能的返回值封装类。
    /// </summary>
    public class RemoveRolePermissionReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 获取所有角色-权限关系功能的返回值封装类。
    /// </summary>
    public class GetAllRolePermissionReturnDto : PagingReturnDtoBase<RolePermission>
    {
    }

    /// <summary>
    /// 增加新角色-权限关系功能参数封装类。
    /// </summary>
    public class AddRolePermissionParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 新角色-权限关系信息。其中Id可以是任何值，返回时会指定新值。
        /// </summary>
        public RolePermission RolePermission { get; set; }
    }

    /// <summary>
    /// 增加新角色-权限关系功能返回值封装类。
    /// </summary>
    public class AddRolePermissionReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 如果成功添加，这里返回新角色-权限关系的Id。
        /// </summary>
        public Guid Id { get; set; }
    }

    #endregion 角色-权限关系

    #region 用户-角色关系
    /// <summary>
    /// 标记删除用户-角色关系功能的参数封装类。
    /// </summary>
    public class RemoveAccountRoleParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 标记删除用户-角色关系功能的返回值封装类。
    /// </summary>
    public class RemoveAccountRoleReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 获取所有用户-角色关系功能的返回值封装类。
    /// </summary>
    public class GetAllAccountRoleReturnDto : PagingReturnDtoBase<AccountRole>
    {
    }

    /// <summary>
    /// 增加新用户-角色关系功能参数封装类。
    /// </summary>
    public class AddAccountRoleParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 新用户-角色关系信息。其中Id可以是任何值，返回时会指定新值。
        /// </summary>
        public AccountRole AccountRole { get; set; }
    }

    /// <summary>
    /// 增加新用户-角色关系功能返回值封装类。
    /// </summary>
    public class AddAccountRoleReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 如果成功添加，这里返回新用户-角色关系的Id。
        /// </summary>
        public Guid Id { get; set; }
    }

    #endregion 用户-角色关系

    #region 权限
    /// <summary>
    /// 标记删除权限功能的参数封装类。
    /// </summary>
    public class RemovePlPermissionParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 标记删除权限功能的返回值封装类。
    /// </summary>
    public class RemovePlPermissionReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 获取所有权限功能的返回值封装类。
    /// </summary>
    public class GetAllPlPermissionReturnDto : PagingReturnDtoBase<PlPermission>
    {
    }

    /// <summary>
    /// 增加新权限功能参数封装类。
    /// </summary>
    public class AddPlPermissionParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 新权限信息。其中Id可以是任何值，返回时会指定新值。
        /// </summary>
        public PlPermission PlPermission { get; set; }
    }

    /// <summary>
    /// 增加新权限功能返回值封装类。
    /// </summary>
    public class AddPlPermissionReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 如果成功添加，这里返回新权限的Id。
        /// </summary>
        public string Id { get; set; }
    }

    /// <summary>
    /// 修改权限信息功能参数封装类。
    /// </summary>
    public class ModifyPlPermissionParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 权限数据。
        /// </summary>
        public PlPermission Item { get; set; }
    }

    /// <summary>
    /// 修改权限信息功能返回值封装类。
    /// </summary>
    public class ModifyPlPermissionReturnDto : ReturnDtoBase
    {
    }
    #endregion 权限

    #region 角色
    /// <summary>
    /// 标记删除角色功能的参数封装类。
    /// </summary>
    public class RemovePlRoleParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 标记删除角色功能的返回值封装类。
    /// </summary>
    public class RemovePlRoleReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 获取所有角色功能的返回值封装类。
    /// </summary>
    public class GetAllPlRoleReturnDto : PagingReturnDtoBase<PlRole>
    {
    }

    /// <summary>
    /// 增加新角色功能参数封装类。
    /// </summary>
    public class AddPlRoleParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 新角色信息。其中Id可以是任何值，返回时会指定新值。
        /// </summary>
        public PlRole PlRole { get; set; }
    }

    /// <summary>
    /// 增加新角色功能返回值封装类。
    /// </summary>
    public class AddPlRoleReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 如果成功添加，这里返回新角色的Id。
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 修改角色信息功能参数封装类。
    /// </summary>
    public class ModifyPlRoleParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 角色数据。
        /// </summary>
        public PlRole PlRole { get; set; }
    }

    /// <summary>
    /// 修改角色信息功能返回值封装类。
    /// </summary>
    public class ModifyPlRoleReturnDto : ReturnDtoBase
    {
    }
    #endregion 角色

}
