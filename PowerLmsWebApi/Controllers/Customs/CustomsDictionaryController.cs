/*
 * 项目：PowerLms | 模块：报关
 * 功能：报关专用字典CRUD控制器（HSCODE、检疫代码、行政区划、国内口岸、检疫地区、报关港口）
 * 技术要点：依赖注入、权限验证B.14、OrgId多租户隔离、EntityManager
 * 作者：zc | 创建：2026-03
 */
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PowerLms.Data;
using PowerLmsServer.EfData;
using PowerLmsServer.Helpers;
using PowerLmsServer.Managers;
using PowerLmsServer.Services;
using PowerLmsWebApi.Dto;
using System.Net;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// 报关专用字典管理控制器。包含HSCODE、检疫代码、行政区划、国内口岸、检疫地区、报关港口六类字典的CRUD操作。
    /// 所有操作需要权限 B.14（报关基础字典）。
    /// </summary>
    public partial class CustomsDictionaryController : PlControllerBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public CustomsDictionaryController(PowerLmsUserDbContext dbContext, AccountManager accountManager,
            IServiceProvider serviceProvider, EntityManager entityManager, IMapper mapper,
            OrgManager<PowerLmsUserDbContext> orgManager, AuthorizationManager authorizationManager,
            ImportExportService importExportService)
        {
            _DbContext = dbContext;
            _AccountManager = accountManager;
            _ServiceProvider = serviceProvider;
            _EntityManager = entityManager;
            _Mapper = mapper;
            _OrgManager = orgManager;
            _AuthorizationManager = authorizationManager;
            _ImportExportService = importExportService;
        }
        readonly PowerLmsUserDbContext _DbContext;
        readonly AccountManager _AccountManager;
        readonly IServiceProvider _ServiceProvider;
        readonly EntityManager _EntityManager;
        readonly IMapper _Mapper;
        readonly OrgManager<PowerLmsUserDbContext> _OrgManager;
        readonly AuthorizationManager _AuthorizationManager;
        readonly ImportExportService _ImportExportService;

        #region HSCODE基础表

        /// <summary>
        /// 获取HSCODE基础表列表。
        /// </summary>
        /// <param name="model">分页排序参数。</param>
        /// <param name="conditional">查询条件字典，支持通用查询条件。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="401">无效令牌。</response>
        [HttpGet]
        public ActionResult<GetAllCustomHsCodeReturnDto> GetAllCustomHsCode([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllCustomHsCodeReturnDto();
            var coll = _DbContext.CdHsCodes.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            if (_AccountManager.IsAdmin(context.User))
            {
            }
            else
            {
                var merchantId = _OrgManager.GetMerchantIdByUserId(context.User.Id);
                if (!merchantId.HasValue) return BadRequest("未知的商户Id");
                coll = coll.Where(c => c.OrgId == (context.User.OrgId ?? merchantId));
            }
            coll = QueryHelper.GenerateWhereAnd(coll, conditional);
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 增加HSCODE基础表记录。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="403">权限不足。</response>
        [HttpPost]
        public ActionResult<AddCustomHsCodeReturnDto> AddCustomHsCode(AddCustomHsCodeParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            if (!_AuthorizationManager.Demand(out string err, "B.14")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new AddCustomHsCodeReturnDto();
            model.Item.GenerateNewId();
            _DbContext.CdHsCodes.Add(model.Item);
            _DbContext.SaveChanges();
            result.Id = model.Item.Id;
            return result;
        }

        /// <summary>
        /// 修改HSCODE基础表记录。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="400">指定实体的Id不存在。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="403">权限不足。</response>
        [HttpPut]
        public ActionResult<ModifyCustomHsCodeReturnDto> ModifyCustomHsCode(ModifyCustomHsCodeParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            if (!_AuthorizationManager.Demand(out string err, "B.14")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new ModifyCustomHsCodeReturnDto();
            if (!_EntityManager.Modify(model.Items))
                return StatusCode(OwHelper.GetLastError());
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 删除HSCODE基础表记录。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="400">指定实体的Id不存在。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="403">权限不足。</response>
        [HttpDelete]
        public ActionResult<RemoveCustomHsCodeReturnDto> RemoveCustomHsCode(RemoveCustomHsCodeParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            if (!_AuthorizationManager.Demand(out string err, "B.14")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new RemoveCustomHsCodeReturnDto();
            var item = _DbContext.CdHsCodes.Find(model.Id);
            if (item is null) return BadRequest();
            _DbContext.CdHsCodes.Remove(item);
            _DbContext.SaveChanges();
            return result;
        }

        #endregion HSCODE基础表

        #region CIQCODE检疫代码表

        /// <summary>
        /// 获取CIQCODE检疫代码表列表。
        /// </summary>
        /// <param name="model">分页排序参数。</param>
        /// <param name="conditional">查询条件字典，支持通用查询条件。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="401">无效令牌。</response>
        [HttpGet]
        public ActionResult<GetAllCustomGoodsVsCiqCodeReturnDto> GetAllCustomGoodsVsCiqCode([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllCustomGoodsVsCiqCodeReturnDto();
            var coll = _DbContext.CdGoodsVsCiqCodes.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            if (_AccountManager.IsAdmin(context.User))
            {
            }
            else
            {
                var merchantId = _OrgManager.GetMerchantIdByUserId(context.User.Id);
                if (!merchantId.HasValue) return BadRequest("未知的商户Id");
                coll = coll.Where(c => c.OrgId == (context.User.OrgId ?? merchantId));
            }
            coll = QueryHelper.GenerateWhereAnd(coll, conditional);
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 增加CIQCODE检疫代码表记录。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="403">权限不足。</response>
        [HttpPost]
        public ActionResult<AddCustomGoodsVsCiqCodeReturnDto> AddCustomGoodsVsCiqCode(AddCustomGoodsVsCiqCodeParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            if (!_AuthorizationManager.Demand(out string err, "B.14")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new AddCustomGoodsVsCiqCodeReturnDto();
            model.Item.GenerateNewId();
            _DbContext.CdGoodsVsCiqCodes.Add(model.Item);
            _DbContext.SaveChanges();
            result.Id = model.Item.Id;
            return result;
        }

        /// <summary>
        /// 修改CIQCODE检疫代码表记录。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="400">指定实体的Id不存在。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="403">权限不足。</response>
        [HttpPut]
        public ActionResult<ModifyCustomGoodsVsCiqCodeReturnDto> ModifyCustomGoodsVsCiqCode(ModifyCustomGoodsVsCiqCodeParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            if (!_AuthorizationManager.Demand(out string err, "B.14")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new ModifyCustomGoodsVsCiqCodeReturnDto();
            if (!_EntityManager.Modify(model.Items))
                return StatusCode(OwHelper.GetLastError());
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 删除CIQCODE检疫代码表记录。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="400">指定实体的Id不存在。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="403">权限不足。</response>
        [HttpDelete]
        public ActionResult<RemoveCustomGoodsVsCiqCodeReturnDto> RemoveCustomGoodsVsCiqCode(RemoveCustomGoodsVsCiqCodeParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            if (!_AuthorizationManager.Demand(out string err, "B.14")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new RemoveCustomGoodsVsCiqCodeReturnDto();
            var item = _DbContext.CdGoodsVsCiqCodes.Find(model.Id);
            if (item is null) return BadRequest();
            _DbContext.CdGoodsVsCiqCodes.Remove(item);
            _DbContext.SaveChanges();
            return result;
        }

        #endregion CIQCODE检疫代码表

        #region 国内行政区划表

        /// <summary>
        /// 获取国内行政区划表列表。
        /// </summary>
        /// <param name="model">分页排序参数。</param>
        /// <param name="conditional">查询条件字典，支持通用查询条件。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="401">无效令牌。</response>
        [HttpGet]
        public ActionResult<GetAllCustomPlaceReturnDto> GetAllCustomPlace([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllCustomPlaceReturnDto();
            var coll = _DbContext.CdPlaces.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            if (_AccountManager.IsAdmin(context.User))
            {
            }
            else
            {
                var merchantId = _OrgManager.GetMerchantIdByUserId(context.User.Id);
                if (!merchantId.HasValue) return BadRequest("未知的商户Id");
                coll = coll.Where(c => c.OrgId == (context.User.OrgId ?? merchantId));
            }
            coll = QueryHelper.GenerateWhereAnd(coll, conditional);
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 增加国内行政区划表记录。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="403">权限不足。</response>
        [HttpPost]
        public ActionResult<AddCustomPlaceReturnDto> AddCustomPlace(AddCustomPlaceParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            if (!_AuthorizationManager.Demand(out string err, "B.14")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new AddCustomPlaceReturnDto();
            model.Item.GenerateNewId();
            _DbContext.CdPlaces.Add(model.Item);
            _DbContext.SaveChanges();
            result.Id = model.Item.Id;
            return result;
        }

        /// <summary>
        /// 修改国内行政区划表记录。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="400">指定实体的Id不存在。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="403">权限不足。</response>
        [HttpPut]
        public ActionResult<ModifyCustomPlaceReturnDto> ModifyCustomPlace(ModifyCustomPlaceParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            if (!_AuthorizationManager.Demand(out string err, "B.14")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new ModifyCustomPlaceReturnDto();
            if (!_EntityManager.Modify(model.Items))
                return StatusCode(OwHelper.GetLastError());
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 删除国内行政区划表记录。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="400">指定实体的Id不存在。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="403">权限不足。</response>
        [HttpDelete]
        public ActionResult<RemoveCustomPlaceReturnDto> RemoveCustomPlace(RemoveCustomPlaceParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            if (!_AuthorizationManager.Demand(out string err, "B.14")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new RemoveCustomPlaceReturnDto();
            var item = _DbContext.CdPlaces.Find(model.Id);
            if (item is null) return BadRequest();
            _DbContext.CdPlaces.Remove(item);
            _DbContext.SaveChanges();
            return result;
        }

        #endregion 国内行政区划表

        #region 国内口岸代码表

        /// <summary>
        /// 获取国内口岸代码表列表。
        /// </summary>
        /// <param name="model">分页排序参数。</param>
        /// <param name="conditional">查询条件字典，支持通用查询条件。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="401">无效令牌。</response>
        [HttpGet]
        public ActionResult<GetAllCustomDomesticPortReturnDto> GetAllCustomDomesticPort([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllCustomDomesticPortReturnDto();
            var coll = _DbContext.CdDomesticPorts.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            if (_AccountManager.IsAdmin(context.User))
            {
            }
            else
            {
                var merchantId = _OrgManager.GetMerchantIdByUserId(context.User.Id);
                if (!merchantId.HasValue) return BadRequest("未知的商户Id");
                coll = coll.Where(c => c.OrgId == (context.User.OrgId ?? merchantId));
            }
            coll = QueryHelper.GenerateWhereAnd(coll, conditional);
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 增加国内口岸代码表记录。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="403">权限不足。</response>
        [HttpPost]
        public ActionResult<AddCustomDomesticPortReturnDto> AddCustomDomesticPort(AddCustomDomesticPortParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            if (!_AuthorizationManager.Demand(out string err, "B.14")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new AddCustomDomesticPortReturnDto();
            model.Item.GenerateNewId();
            _DbContext.CdDomesticPorts.Add(model.Item);
            _DbContext.SaveChanges();
            result.Id = model.Item.Id;
            return result;
        }

        /// <summary>
        /// 修改国内口岸代码表记录。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="400">指定实体的Id不存在。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="403">权限不足。</response>
        [HttpPut]
        public ActionResult<ModifyCustomDomesticPortReturnDto> ModifyCustomDomesticPort(ModifyCustomDomesticPortParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            if (!_AuthorizationManager.Demand(out string err, "B.14")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new ModifyCustomDomesticPortReturnDto();
            if (!_EntityManager.Modify(model.Items))
                return StatusCode(OwHelper.GetLastError());
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 删除国内口岸代码表记录。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="400">指定实体的Id不存在。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="403">权限不足。</response>
        [HttpDelete]
        public ActionResult<RemoveCustomDomesticPortReturnDto> RemoveCustomDomesticPort(RemoveCustomDomesticPortParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            if (!_AuthorizationManager.Demand(out string err, "B.14")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new RemoveCustomDomesticPortReturnDto();
            var item = _DbContext.CdDomesticPorts.Find(model.Id);
            if (item is null) return BadRequest();
            _DbContext.CdDomesticPorts.Remove(item);
            _DbContext.SaveChanges();
            return result;
        }

        #endregion 国内口岸代码表

        #region 国内地区代码（检疫用）表

        /// <summary>
        /// 获取国内地区代码（检疫用）表列表。
        /// </summary>
        /// <param name="model">分页排序参数。</param>
        /// <param name="conditional">查询条件字典，支持通用查询条件。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="401">无效令牌。</response>
        [HttpGet]
        public ActionResult<GetAllCustomInspectionPlaceReturnDto> GetAllCustomInspectionPlace([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllCustomInspectionPlaceReturnDto();
            var coll = _DbContext.CdInspectionPlaces.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            if (_AccountManager.IsAdmin(context.User))
            {
            }
            else
            {
                var merchantId = _OrgManager.GetMerchantIdByUserId(context.User.Id);
                if (!merchantId.HasValue) return BadRequest("未知的商户Id");
                coll = coll.Where(c => c.OrgId == (context.User.OrgId ?? merchantId));
            }
            coll = QueryHelper.GenerateWhereAnd(coll, conditional);
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 增加国内地区代码（检疫用）表记录。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="403">权限不足。</response>
        [HttpPost]
        public ActionResult<AddCustomInspectionPlaceReturnDto> AddCustomInspectionPlace(AddCustomInspectionPlaceParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            if (!_AuthorizationManager.Demand(out string err, "B.14")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new AddCustomInspectionPlaceReturnDto();
            model.Item.GenerateNewId();
            _DbContext.CdInspectionPlaces.Add(model.Item);
            _DbContext.SaveChanges();
            result.Id = model.Item.Id;
            return result;
        }

        /// <summary>
        /// 修改国内地区代码（检疫用）表记录。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="400">指定实体的Id不存在。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="403">权限不足。</response>
        [HttpPut]
        public ActionResult<ModifyCustomInspectionPlaceReturnDto> ModifyCustomInspectionPlace(ModifyCustomInspectionPlaceParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            if (!_AuthorizationManager.Demand(out string err, "B.14")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new ModifyCustomInspectionPlaceReturnDto();
            if (!_EntityManager.Modify(model.Items))
                return StatusCode(OwHelper.GetLastError());
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 删除国内地区代码（检疫用）表记录。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="400">指定实体的Id不存在。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="403">权限不足。</response>
        [HttpDelete]
        public ActionResult<RemoveCustomInspectionPlaceReturnDto> RemoveCustomInspectionPlace(RemoveCustomInspectionPlaceParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            if (!_AuthorizationManager.Demand(out string err, "B.14")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new RemoveCustomInspectionPlaceReturnDto();
            var item = _DbContext.CdInspectionPlaces.Find(model.Id);
            if (item is null) return BadRequest();
            _DbContext.CdInspectionPlaces.Remove(item);
            _DbContext.SaveChanges();
            return result;
        }

        #endregion 国内地区代码（检疫用）表

        #region 报关专用港口表

        /// <summary>
        /// 获取报关专用港口表列表。
        /// </summary>
        /// <param name="model">分页排序参数。</param>
        /// <param name="conditional">查询条件字典，支持通用查询条件。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="401">无效令牌。</response>
        [HttpGet]
        public ActionResult<GetAllCustomPlPortReturnDto> GetAllCustomPlPort([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllCustomPlPortReturnDto();
            var coll = _DbContext.CdPorts.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            if (_AccountManager.IsAdmin(context.User))
            {
            }
            else
            {
                var merchantId = _OrgManager.GetMerchantIdByUserId(context.User.Id);
                if (!merchantId.HasValue) return BadRequest("未知的商户Id");
                coll = coll.Where(c => c.OrgId == (context.User.OrgId ?? merchantId));
            }
            coll = QueryHelper.GenerateWhereAnd(coll, conditional);
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 增加报关专用港口表记录。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="403">权限不足。</response>
        [HttpPost]
        public ActionResult<AddCustomPlPortReturnDto> AddCustomPlPort(AddCustomPlPortParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            if (!_AuthorizationManager.Demand(out string err, "B.14")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new AddCustomPlPortReturnDto();
            model.Item.GenerateNewId();
            _DbContext.CdPorts.Add(model.Item);
            _DbContext.SaveChanges();
            result.Id = model.Item.Id;
            return result;
        }

        /// <summary>
        /// 修改报关专用港口表记录。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="400">指定实体的Id不存在。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="403">权限不足。</response>
        [HttpPut]
        public ActionResult<ModifyCustomPlPortReturnDto> ModifyCustomPlPort(ModifyCustomPlPortParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            if (!_AuthorizationManager.Demand(out string err, "B.14")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new ModifyCustomPlPortReturnDto();
            if (!_EntityManager.Modify(model.Items))
                return StatusCode(OwHelper.GetLastError());
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 删除报关专用港口表记录。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="400">指定实体的Id不存在。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="403">权限不足。</response>
        [HttpDelete]
        public ActionResult<RemoveCustomPlPortReturnDto> RemoveCustomPlPort(RemoveCustomPlPortParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            if (!_AuthorizationManager.Demand(out string err, "B.14")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new RemoveCustomPlPortReturnDto();
            var item = _DbContext.CdPorts.Find(model.Id);
            if (item is null) return BadRequest();
            _DbContext.CdPorts.Remove(item);
            _DbContext.SaveChanges();
            return result;
        }

        #endregion 报关专用港口表
    }
}
