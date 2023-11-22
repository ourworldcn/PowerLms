﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PowerLms.Data;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Dto;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// 组织机构控制器。
    /// </summary>
    public class OrganizationController : OwControllerBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public OrganizationController(AccountManager accountManager, IServiceProvider serviceProvider, PowerLmsUserDbContext dbContext)
        {
            _AccountManager = accountManager;
            _ServiceProvider = serviceProvider;
            _DbContext = dbContext;
        }

        AccountManager _AccountManager;
        IServiceProvider _ServiceProvider;
        PowerLmsUserDbContext _DbContext;

        /// <summary>
        /// 获取组织机构。暂不考虑分页。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpPost]
        public ActionResult<GetOrgReturnDto> GetOrg(GetOrgParamsDto model)
        {
            var result = new GetOrgReturnDto();
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized(OwHelper.GetLastErrorMessage());
            var root = _DbContext.PlOrganizations.FirstOrDefault(c => c.Id == model.RootId);
            if (!model.IncludeChildren)
            {
                _DbContext.Entry(root).State = EntityState.Detached;
                root.Children.Clear();
            }
            result.Result = root;
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
            foreach (var item in model.Items)
            {
                //_DbContext.UpdateRange(item);
                _DbContext.Entry(item).CurrentValues.SetValues(item);
            }
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
