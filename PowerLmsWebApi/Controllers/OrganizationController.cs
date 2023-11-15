using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
                _DbContext.Entry(root).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                root.Children.Clear();
            }
            result.Result = root;
            return result;
        }

        /// <summary>
        /// 修改组织机构。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpPost]
        public ActionResult<ModifyOrgReturnDto> ModifyOrg(ModifyOrgParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized(OwHelper.GetLastErrorMessage());
            var result = new ModifyOrgReturnDto();
            if (model.Root is not null)
            {
                var root = _DbContext.PlOrganizations.Find(model.Root.Id);
                if (root is null)   //若新增节点
                {
                    _DbContext.PlOrganizations.Add(model.Root);
                }
                else
                {
                    _DbContext.Entry(root).CurrentValues.SetValues(model.Root);
                }
                _DbContext.SaveChanges();
            }
            _DbContext.Delete(model.DeleteIds, nameof(_DbContext.PlOrganizations));
            return result;
        }
    }

    /// <summary>
    /// 修改组织机构功能的参数封装类。
    /// </summary>
    public class ModifyOrgParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 要修改的组织机构Id的根节点。节点（包括下属节点）的Id若不存在则新加，否则是修改。注意增加顶层节点，必须是商户类型。(<see cref="PlOrganization.Otc"/>=1)
        /// 可以为null，表示不添加/修改节点，仅删除节点。
        /// </summary>
        public PlOrganization Root { get; set; }

        /// <summary>
        /// 要删除了组织机构的Id集合。删除的节点需要在这里额外指定其Id。
        /// </summary>
        public List<Guid> DeleteIds { get; set; } = new List<Guid>();
    }

    /// <summary>
    /// 修改组织机构功能的返回值封装类。
    /// </summary>
    public class ModifyOrgReturnDto : ReturnDtoBase
    {
    }
}
