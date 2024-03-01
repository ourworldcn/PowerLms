using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PowerLms.Data;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Dto;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// 航线管理控制器。
    /// </summary>
    public class ShippingLaneController : PlControllerBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public ShippingLaneController(IServiceProvider serviceProvider, AccountManager accountManager, PowerLmsUserDbContext dbContext, OrganizationManager organizationManager, EntityManager entityManager, IMapper mapper)
        {
            _ServiceProvider = serviceProvider;
            _AccountManager = accountManager;
            _DbContext = dbContext;
            _OrganizationManager = organizationManager;
            _EntityManager = entityManager;
            _Mapper = mapper;
        }
        IServiceProvider _ServiceProvider;
        AccountManager _AccountManager;

        readonly PowerLmsUserDbContext _DbContext;
        OrganizationManager _OrganizationManager;

        EntityManager _EntityManager;
        IMapper _Mapper;

        /// <summary>
        /// 增加新航线费用。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpPost]
        public ActionResult<AddShippingLaneReturnDto> AddShippingLane(AddShippingLaneParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new AddShippingLaneReturnDto();
            model.Item.GenerateNewId();

            _DbContext.ShippingLanes.Add(model.Item);
            model.Item.CreateDateTime = OwHelper.WorldNow;
            model.Item.CreateBy = context.User.Id;
            model.Item.OrgId = context.User.OrgId;
            _DbContext.SaveChanges();
            result.Id = model.Item.Id;
            return result;
        }

        /// <summary>
        /// 获取全部航线费用。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">查询的条件。支持 ValidDateTime(有效期)， StartCreateDateTime（开始创建/更新时间）,EndCreateDateTime（结束创建/更新时间），StartCode，EndCode，Id。不区分大小写。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllShippingLaneReturnDto> GetAllShippingLane([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllShippingLaneReturnDto();
            var dbSet = _DbContext.ShippingLanes.Where(c => c.OrgId == context.User.OrgId);
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            foreach (var item in conditional)
                if (string.Equals(item.Key, "StartCreateDateTime", StringComparison.OrdinalIgnoreCase))
                {
                    if (DateTime.TryParse(item.Value, out var date))
                        coll = coll.Where(c => c.UpdateDateTime >= date);
                }
                else if (string.Equals(item.Key, "EndCreateDateTime", StringComparison.OrdinalIgnoreCase))
                {
                    if (DateTime.TryParse(item.Value, out var date))
                        coll = coll.Where(c => c.UpdateDateTime <= date);
                }
                else if (string.Equals(item.Key, "ValidDateTime", StringComparison.OrdinalIgnoreCase))
                {
                    if (DateTime.TryParse(item.Value, out var date))
                        coll = coll.Where(c => c.StartDateTime <= date && c.EndDateTime >= date);
                }
                else if (string.Equals(item.Key, "Id", StringComparison.OrdinalIgnoreCase))
                {
                    if (Guid.TryParse(item.Value, out var id))
                        coll = coll.Where(c => c.Id == id);
                }
                else if (string.Equals(item.Key, "StartCode", StringComparison.OrdinalIgnoreCase))
                {
                    coll = coll.Where(c => c.StartCode.Contains(item.Value));
                }
                else if (string.Equals(item.Key, "EndCode", StringComparison.OrdinalIgnoreCase))
                {
                    coll = coll.Where(c => c.EndCode.Contains(item.Value));
                }
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 修改航线费用信息。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的航线费用不存在。</response>  
        [HttpPut]
        public ActionResult<ModifyShippingLaneReturnDto> ModifyShippingLane(ModifyShippingLaneParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyShippingLaneReturnDto();
            if (!_EntityManager.Modify(model.Items)) return NotFound();
            foreach (var item in model.Items)
            {
                item.UpdateBy = context.User.Id;
                item.UpdateDateTime = OwHelper.WorldNow;
            }
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 批量删除航线信息。(物理硬删除)
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id中，至少有一个不存在相应实体。</response>  
        [HttpDelete]
        public ActionResult<RemoveShippingLaneReturnDto> RemoveShippingLane(RemoveShippingLanePatamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemoveShippingLaneReturnDto();

            var dbSet = _DbContext.BankInfos;
            var items = dbSet.Where(c => model.Ids.Contains(c.Id)).ToArray();
            if (items.Length != model.Ids.Count) return BadRequest("指定Id中，至少有一个不存在相应实体。");
            _DbContext.RemoveRange(items);
            _DbContext.SaveChanges();
            return result;
        }
    }

    /// <summary>
    /// 批量删除航线信息功能参数封装类。
    /// </summary>
    public class RemoveShippingLanePatamsDto : RemoveItemsParamsDtoBase
    {

    }

    /// <summary>
    /// 批量删除航线信息功能返回值封装类。
    /// </summary>
    public class RemoveShippingLaneReturnDto : RemoveItemsReturnDtoBase
    {
    }

    /// <summary>
    /// 修改航线费用功能的返回值封装类。
    /// </summary>
    public class ModifyShippingLaneReturnDto : ModifyReturnDtoBase
    {
    }

    /// <summary>
    /// 修改航线费用功能的参数封装类。
    /// </summary>
    public class ModifyShippingLaneParamsDto : ModifyParamsDtoBase<ShippingLane>
    {
    }

    /// <summary>
    /// 查询航线费用对象返回值封装类。
    /// </summary>
    public class GetAllShippingLaneReturnDto : PagingReturnDtoBase<ShippingLane>
    {
    }

    /// <summary>
    /// 增加航线费用对象功能参数封装类。
    /// </summary>
    public class AddShippingLaneParamsDto : AddParamsDtoBase<ShippingLane>
    {
    }

    /// <summary>
    /// 增加航线费用对象功能返回值封装类。
    /// </summary>
    public class AddShippingLaneReturnDto : AddReturnDtoBase
    {
    }
}
