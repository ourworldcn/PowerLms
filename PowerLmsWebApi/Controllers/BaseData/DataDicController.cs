using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PowerLms.Data;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsServer.Services;
using PowerLmsWebApi.Dto;
using System.Net;
using OW.Data;
using AutoMapper;
using PowerLmsServer;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// 数据字典控制器。
    /// </summary>
    public partial class DataDicController : PlControllerBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public DataDicController(PowerLmsUserDbContext context, AccountManager accountManager, 
            IServiceProvider serviceProvider, EntityManager entityManager, IMapper mapper, 
            OrgManager<PowerLmsUserDbContext> orgManager, AuthorizationManager authorizationManager,
            ILogger<DataDicController> logger, DataDicManager dataDicManager)
        {
            _DbContext = context;
            _AccountManager = accountManager;
            _ServiceProvider = serviceProvider;
            _EntityManager = entityManager;
            _Mapper = mapper;
            _OrgManager = orgManager;
            _AuthorizationManager = authorizationManager;
            _Logger = logger;
            _DataDicManager = dataDicManager;
        }

        readonly PowerLmsUserDbContext _DbContext;
        readonly AccountManager _AccountManager;
        readonly IServiceProvider _ServiceProvider;
        readonly EntityManager _EntityManager;
        readonly IMapper _Mapper;
        readonly OrgManager<PowerLmsUserDbContext> _OrgManager;
        readonly AuthorizationManager _AuthorizationManager;
        readonly ILogger<DataDicController> _Logger;
        readonly DataDicManager _DataDicManager;

        #region 日常费用种类相关

        /// <summary>
        /// 获取日常费用类型。超管可以查看系统级，其他用户只能看到公司/组织下的实体。
        /// </summary>
        /// <param name="model">分页参数</param>
        /// <param name="conditional">支持通用查询条件</param>
        /// <returns>日常费用类型列表</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定类别Id无效。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllDailyFeesTypeReturnDto> GetAllDailyFeesType([FromQuery] PagingParamsDtoBase model, [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllDailyFeesTypeReturnDto();
            var dbSet = _DbContext.DD_DailyFeesTypes;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            
            // 使用统一的组织权限控制方法
            var allowedOrgIds = GetOrgIds(context.User, _OrgManager);
            coll = coll.Where(c => allowedOrgIds.Contains(c.OrgId));
            
            coll = EfHelper.GenerateWhereAnd(coll, conditional);
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 增加日常费用种类记录。
        /// </summary>
        /// <param name="model">增加参数</param>
        /// <returns>操作结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">参数错误。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPost]
        public ActionResult<AddDailyFeesTypeReturnDto> AddDailyFeesType(AddDailyFeesTypeParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            if (!_AuthorizationManager.Demand(out string err, "B.8")) return StatusCode((int)HttpStatusCode.Forbidden, err);

            var result = new AddDailyFeesTypeReturnDto();

            // 确保使用当前用户的组织机构ID
            model.Item.OrgId = context.User.OrgId;

            // 生成主记录ID
            model.Item.GenerateNewId();
            var id = model.Item.Id;

            // 添加主记录
            _DbContext.DD_DailyFeesTypes.Add(model.Item);

            // 如果需要同步到子机构
            if (model.CopyToChildren)
            {
                // 获取用户管辖范围内的公司型组织机构ID
                var merchantId = _OrgManager.GetMerchantIdByUserId(context.User.Id);
                if (merchantId.HasValue)
                {
                    var allOrgs = _OrgManager.GetOrLoadOrgCacheItem(merchantId.Value).Orgs.Values.ToArray();
                    var companyIds = allOrgs.Where(o => o.Otc == 2).Select(o => o.Id);

                    // 修复Bug：排除本机构，避免重复创建
                    foreach (var orgId in companyIds.Where(id => id != context.User.OrgId))
                    {
                        // 检查是否已存在相同Code的记录
                        if (_DbContext.DD_DailyFeesTypes.Any(f => f.OrgId == orgId && f.Code == model.Item.Code))
                            continue;

                        // 使用Clone方法创建深表副本
                        var newItem = (DailyFeesType)model.Item.Clone();
                        newItem.OrgId = orgId;
                        newItem.GenerateNewId(); // 确保新记录有唯一ID

                        _DbContext.DD_DailyFeesTypes.Add(newItem);
                    }
                }
            }

            _DbContext.SaveChanges();
            result.Id = id;
            return result;
        }

        /// <summary>
        /// 修改日常费用种类记录。
        /// </summary>
        /// <param name="model">修改参数</param>
        /// <returns>操作结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定实体的Id不存在。通常这是Bug.在极端情况下可能是并发问题。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPut]
        public ActionResult<ModifyDailyFeesTypeReturnDto> ModifyDailyFeesType(ModifyDailyFeesTypeParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            if (!_AuthorizationManager.Demand(out string err, "B.8")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new ModifyDailyFeesTypeReturnDto();
            if (!_EntityManager.ModifyWithMarkDelete(model.Items))
            {
                var errResult = new StatusCodeResult(OwHelper.GetLastError()) { };
                return errResult;
            }
            foreach (var item in model.Items)
            {
                _DbContext.Entry(item).Property(c => c.OrgId).IsModified = false;
            }
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 删除日常费用种类的记录。
        /// </summary>
        /// <param name="model">删除参数</param>
        /// <returns>操作结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定实体的Id不存在。通常这是Bug.在极端情况下可能是并发问题。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpDelete]
        public ActionResult<RemoveDailyFeesTypeReturnDto> RemoveDailyFeesType(RemoveDailyFeesTypeParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            if (!_AuthorizationManager.Demand(out string err, "B.8")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new RemoveDailyFeesTypeReturnDto();
            var id = model.Id;
            var dbSet = _DbContext.DD_DailyFeesTypes;
            var item = dbSet.Find(id);
            if (item is null) return BadRequest();
            _EntityManager.Remove(item);
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 恢复指定的被删除日常费用种类记录。
        /// </summary>
        /// <param name="model">恢复参数</param>
        /// <returns>操作结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定实体的Id不存在。通常这是Bug.在极端情况下可能是并发问题。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPost]
        public ActionResult<RestoreDailyFeesTypeReturnDto> RestoreDailyFeesType(RestoreDailyFeesTypeParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            if (!_AuthorizationManager.Demand(out string err, "B.8")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new RestoreDailyFeesTypeReturnDto();
            if (!_EntityManager.Restore<DailyFeesType>(model.Id))
            {
                var errResult = new StatusCodeResult(OwHelper.GetLastError()) { };
                return errResult;
            }
            _DbContext.SaveChanges();
            return result;
        }

        #endregion 日常费用种类相关
    }
}