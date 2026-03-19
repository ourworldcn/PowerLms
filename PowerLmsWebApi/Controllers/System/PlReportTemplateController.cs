/*
 * 项目：PowerLmsWebApi | 模块：System
 * 功能：报表模板（PlReportTemplate）的CRUD接口
 * 技术要点：继承PlControllerBase，依赖注入，EF Core操作
 * 作者：zc | 创建：2026-03
 */
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PowerLms.Data;
using PowerLmsServer.EfData;
using PowerLmsServer.Helpers;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Dto;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// 报表模板（PlReportTemplate）的CRUD控制器。
    /// </summary>
    public class PlReportTemplateController : PlControllerBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public PlReportTemplateController(AccountManager accountManager, IServiceProvider serviceProvider,
            PowerLmsUserDbContext dbContext, IMapper mapper, EntityManager entityManager,
            AuthorizationManager authorizationManager, ILogger<PlReportTemplateController> logger,
            OrgManager<PowerLmsUserDbContext> orgManager)
        {
            _AccountManager = accountManager;
            _ServiceProvider = serviceProvider;
            _DbContext = dbContext;
            _Mapper = mapper;
            _EntityManager = entityManager;
            _AuthorizationManager = authorizationManager;
            _Logger = logger;
            _OrgManager = orgManager;
        }

        private readonly AccountManager _AccountManager;
        private readonly IServiceProvider _ServiceProvider;
        private readonly PowerLmsUserDbContext _DbContext;
        private readonly IMapper _Mapper;
        private readonly EntityManager _EntityManager;
        private readonly AuthorizationManager _AuthorizationManager;
        private readonly ILogger<PlReportTemplateController> _Logger;
        private readonly OrgManager<PowerLmsUserDbContext> _OrgManager;

        #region PlReportTemplate CRUD

        /// <summary>
        /// 获取报表模板列表。
        /// </summary>
        /// <param name="model">分页参数。</param>
        /// <param name="conditional">查询条件。实体属性名不区分大小写。
        /// 通用条件写法：所有条件都是字符串，对区间的写法是用逗号分隔（字符串类型暂时不支持区间且都是模糊查询）如"2024-1-1,2024-1-2"。
        /// 对强制取null的约束，则写"null"。</param>
        /// <returns>报表模板列表。</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllPlReportTemplateReturnDto> GetAllPlReportTemplate(
            [FromQuery] GetAllPlReportTemplateParamsDto model,
            [FromQuery][ModelBinder(typeof(DotKeyDictionaryModelBinder))] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllPlReportTemplateReturnDto();
            try
            {
                var dbSet = _DbContext.PlReportTemplates.AsQueryable();
                if (_AccountManager.IsAdmin(context.User))
                    dbSet = dbSet.Where(c => c.ParentId == null);
                else
                {
                    var merchantId = _OrgManager.GetMerchantIdByUserId(context.User.Id);
                    if (!merchantId.HasValue) return BadRequest("未知的商户Id");
                    dbSet = dbSet.Where(c => c.ParentId == merchantId);
                }
                if (conditional != null)
                    dbSet = QueryHelper.GenerateWhereAnd(dbSet, conditional);
                var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
                var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
                _Mapper.Map(prb, result);
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "获取报表模板列表时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"获取数据时发生错误：{ex.Message}";
            }
            return result;
        }

        /// <summary>
        /// 增加报表模板。
        /// </summary>
        /// <param name="model">包含新实体数据的参数对象。</param>
        /// <returns>操作结果，包含新创建实体的Id。</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpPost]
        public ActionResult<AddPlReportTemplateReturnDto> AddPlReportTemplate(AddPlReportTemplateParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new AddPlReportTemplateReturnDto();
            try
            {
                var entity = model.Item;
                entity.GenerateNewId();
                entity.ExtraGuid = context.User.Id;
                entity.ExtraDateTime = OwHelper.WorldNow;
                if (_AccountManager.IsAdmin(context.User))
                    entity.ParentId = null;
                else
                {
                    var merchantId = _OrgManager.GetMerchantIdByUserId(context.User.Id);
                    if (!merchantId.HasValue) return BadRequest("未知的商户Id");
                    entity.ParentId = merchantId.Value;
                }
                _DbContext.PlReportTemplates.Add(entity);
                _DbContext.SaveChanges();
                result.Id = entity.Id;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "增加报表模板时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"增加实体时发生错误：{ex.Message}";
            }
            return result;
        }

        /// <summary>
        /// 修改报表模板。
        /// </summary>
        /// <param name="model">包含要修改实体集合的参数对象，其中每个实体的Id必须已存在。</param>
        /// <returns>操作结果。</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的实体不存在。</response>  
        [HttpPut]
        public ActionResult<ModifyPlReportTemplateReturnDto> ModifyPlReportTemplate(ModifyPlReportTemplateParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyPlReportTemplateReturnDto();
            var isAdmin = _AccountManager.IsAdmin(context.User);
            Guid? allowedMerchantId = isAdmin ? (Guid?)null : _OrgManager.GetMerchantIdByUserId(context.User.Id);
            if (!isAdmin && !allowedMerchantId.HasValue) return BadRequest("未知的商户Id");
            try
            {
                foreach (var item in model.Items)
                {
                    var existing = _DbContext.PlReportTemplates.Find(item.Id);
                    if (existing is null) return NotFound();
                    if (existing.ParentId != allowedMerchantId) return StatusCode(403, "无权操作其他商户的报表模板");
                    item.ParentId = allowedMerchantId;
                    _Mapper.Map(item, existing);
                }
                _DbContext.SaveChanges();
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "修改报表模板时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"修改实体时发生错误：{ex.Message}";
            }
            return result;
        }

        /// <summary>
        /// 删除指定Id的报表模板。
        /// </summary>
        /// <param name="model">包含要删除实体Id的参数对象。</param>
        /// <returns>操作结果。</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的实体不存在。</response>  
        [HttpDelete]
        public ActionResult<RemovePlReportTemplateReturnDto> RemovePlReportTemplate(RemovePlReportTemplateParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemovePlReportTemplateReturnDto();
            var isAdmin = _AccountManager.IsAdmin(context.User);
            Guid? allowedMerchantId = isAdmin ? (Guid?)null : _OrgManager.GetMerchantIdByUserId(context.User.Id);
            if (!isAdmin && !allowedMerchantId.HasValue) return BadRequest("未知的商户Id");
            try
            {
                var item = _DbContext.PlReportTemplates.Find(model.Id);
                if (item is null) return NotFound();
                if (item.ParentId != allowedMerchantId) return StatusCode(403, "无权操作其他商户的报表模板");
                _DbContext.OwSystemLogs.Add(new OwSystemLog
                {
                    OrgId = context.User.OrgId,
                    ActionId = $"Delete.{nameof(PlReportTemplate)}.{item.Id}",
                    ExtraGuid = context.User.Id,
                    WorldDateTime = OwHelper.WorldNow
                });
                _EntityManager.Remove(item);
                _DbContext.SaveChanges();
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "删除报表模板时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"删除实体时发生错误：{ex.Message}";
            }
            return result;
        }

        #endregion PlReportTemplate CRUD
    }
}
