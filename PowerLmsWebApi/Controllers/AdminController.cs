﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NPOI.HSSF.UserModel;
using NuGet.Common;
using PowerLms.Data;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Dto;
using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;
using System.Net;
using OW.Data;
using NuGet.Packaging;
using NuGet.Protocol;
using AutoMapper;
using NPOI.SS.Formula.Functions;
using System.Text;
using Microsoft.Extensions.ObjectPool;
using System.Text.RegularExpressions;
using AutoMapper.Internal.Mappers;
using PowerLmsServer;
using Microsoft.EntityFrameworkCore.Internal;
using System.ComponentModel;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// 管理员功能控制器。
    /// </summary>
    public partial class AdminController : PlControllerBase
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="npoiManager"></param>
        /// <param name="accountManager"></param>
        /// <param name="scope"></param>
        /// <param name="entityManager"></param>
        /// <param name="mapper"></param>
        /// <param name="organizationManager"></param>
        /// <param name="dataManager"></param>
        /// <param name="authorizationManager"></param>
        /// <param name="merchantManager"></param>
        /// <param name="logger"></param>
        public AdminController(PowerLmsUserDbContext context, NpoiManager npoiManager, AccountManager accountManager, IServiceProvider scope, EntityManager entityManager,
            IMapper mapper, OrganizationManager organizationManager, DataDicManager dataManager, AuthorizationManager authorizationManager,
            MerchantManager merchantManager, ILogger<AdminController> logger)
        {
            _DbContext = context;
            _NpoiManager = npoiManager;
            _AccountManager = accountManager;
            _ServiceProvider = scope;
            _EntityManager = entityManager;
            _Mapper = mapper;
            _OrganizationManager = organizationManager;
            _DataManager = dataManager;
            _AuthorizationManager = authorizationManager;
            _MerchantManager = merchantManager;
            _Logger = logger;
        }

        readonly PowerLmsUserDbContext _DbContext;
        private readonly NpoiManager _NpoiManager;
        readonly AccountManager _AccountManager;
        readonly IServiceProvider _ServiceProvider;
        readonly EntityManager _EntityManager;
        readonly IMapper _Mapper;
        readonly OrganizationManager _OrganizationManager;
        readonly DataDicManager _DataManager;
        readonly AuthorizationManager _AuthorizationManager;
        readonly MerchantManager _MerchantManager;
        ILogger<AdminController> _Logger;

        #region 字典目录

        /// <summary>
        /// 获取所有数据字典的目录列表。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">支持通用查询。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllDataDicCatalogReturnDto> GetAllDataDicCatalog([FromQuery] PagingParamsDtoBase model, [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllDataDicCatalogReturnDto();

            var dbSet = _DbContext.DD_DataDicCatalogs;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            if (_AccountManager.IsAdmin(context.User))  //若是超管
                coll = coll.Where(c => c.OrgId == null);
            else
            {
                if (!_MerchantManager.GetIdByUserId(context.User.Id, out var merchId)) return BadRequest("未知的商户Id");
                if (context.User.OrgId is null) //若没有指定机构
                {
                    coll = coll.Where(c => c.OrgId == merchId);
                }
                else
                {
                    coll = coll.Where(c => c.OrgId == context.User.OrgId);
                }
            }

            // 使用EfHelper.GenerateWhereAnd进行通用查询条件处理
            coll = EfHelper.GenerateWhereAnd(coll, conditional);

            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 修改数据字典目录。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定实体的Id不存在。通常这是Bug.在极端情况下可能是并发问题。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPut]
        public ActionResult<ModifyDataDicCatalogReturnDto> ModifyDataDicCatalog(ModifyDataDicCatalogParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            if (!_AuthorizationManager.Demand(out var err, "B.0")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new ModifyDataDicCatalogReturnDto();
            if (!_EntityManager.Modify(model.Items))
            {
                var errResult = new StatusCodeResult(OwHelper.GetLastError()) { };
                return errResult;
            }
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 删除一个数据字典目录，并删除其内容。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定实体的Id不存在。通常这是Bug.在极端情况下可能是并发问题。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpDelete]
        public ActionResult<RemoveDataDicCatalogReturnDto> RemoveDataDicCatalog(RemoveDataDicCatalogParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            if (!_AuthorizationManager.Demand(out err, "B.0")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new RemoveDataDicCatalogReturnDto();
            var id = model.Id;
            var item = _DbContext.DD_DataDicCatalogs.Find(id);
            if (item is null) return BadRequest();
            _EntityManager.Remove(item);
            _DbContext.SaveChanges();
            return result;
        }

        #endregion 字典目录

        /// <summary>
        /// 复制简单数据字典。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response> 
        /// <response code="403">权限不足。</response>  
        [HttpPost]
        public ActionResult<CopySimpleDataDicReturnDto> CopySimpleDataDic(CopySimpleDataDicParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            if (!_AuthorizationManager.Demand(out err, "B.0")) return StatusCode((int)HttpStatusCode.Forbidden, err);

            var result = new CopySimpleDataDicReturnDto();
            //var merch = _DbContext.Merchants.Find(model.SrcOrgId);
            //if (merch == null) return NotFound();
            #region 复制简单字典
            var baseCatalogs = _DbContext.DD_DataDicCatalogs.Where(c => c.OrgId == model.SrcOrgId).AsNoTracking();  //基本字典目录集合
            foreach (var catalog in baseCatalogs)
            {
                if (model.CatalogCodes.Contains(catalog.Code))
                    _DataManager.CopyTo(catalog, model.DestOrgId);
            }
            //_DataManager.CopyAllSpecialDataDicBase(model.Id);
            #endregion 复制简单字典

            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 获取系统资源列表。
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult<GetSystemResourceReturnDto> GetSystemResource()
        {
            var result = new GetSystemResourceReturnDto();
            result.Resources.AddRange(_DbContext.DD_SystemResources.AsNoTracking());
            return result;
        }

        /// <summary>
        /// 通用的导入数据字典。相当于清理表后再导入。
        /// </summary>
        /// <param name="formFile"></param>
        /// <param name="token"></param>
        /// <param name="rId">从资源列表中获取，指定资源的Id。如:6AE3BBB3-BAC9-4509-BF82-C8578830CD24 表示 多语言资源表。Id是不会变化的。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPost]
        [ProducesResponseType(typeof(ImportDataDicReturnDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public ActionResult<ImportDataDicReturnDto> ImportDataDic(IFormFile formFile, Guid token, Guid rId)
        {
            if (_AccountManager.GetOrLoadContextByToken(token, _ServiceProvider) is not OwContext context) return Unauthorized(OwHelper.GetLastErrorMessage());
            string err;
            if (!_AuthorizationManager.Demand(out err, "B.0")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new ImportDataDicReturnDto();
            var srTask = _DbContext.DD_SystemResources.FindAsync(rId).AsTask();
            var workbook = _NpoiManager.GetWorkbookFromStream(formFile.OpenReadStream());
            var sheet = workbook.GetSheetAt(0);
            var sr = srTask.Result;
            switch (sr.Name)
            {
                case nameof(_DbContext.Multilinguals):
                    {
                        _DbContext.TruncateTable(nameof(_DbContext.Multilinguals));
                        _NpoiManager.WriteToDb(sheet, _DbContext, _DbContext.Multilinguals);
                        _DbContext.SaveChanges();
                    }
                    break;
                default:
                    result.ErrorCode = 400;
                    break;
            }
            return result;
        }

        /// <summary>
        /// 通用获取数据字典表功能。
        /// </summary>
        /// <param name="token"></param>
        /// <param name="rId"></param>
        /// <returns></returns>
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        [ProducesResponseType(typeof(FileStreamResult), 200)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public ActionResult ExportDataDic(Guid token, Guid rId)
        {
            if (_AccountManager.GetOrLoadContextByToken(token, _ServiceProvider) is not OwContext context) return Unauthorized(OwHelper.GetLastErrorMessage());
            var srTask = _DbContext.DD_SystemResources.FindAsync(rId).AsTask();
            var sr = srTask.Result;
            using var workbook = new HSSFWorkbook();
            var sheet = workbook.CreateSheet("Sheet0");//创建一个名称为Sheet0的表  
            var fileName = $"{sr.Name}.xls";
            switch (sr.Name)
            {
                case nameof(_DbContext.Multilinguals):
                    {
                        _NpoiManager.WriteToExcel(_DbContext.Multilinguals.AsNoTracking(), typeof(Multilingual).GetProperties().Select(c => c.Name).ToArray(), sheet);
                    }
                    break;
                default:
                    break;
            }
            var stream = new MemoryStream();
            workbook.Write(stream, true);
            workbook.Close();
            stream.Seek(0, SeekOrigin.Begin);
            return File(stream, "application/octet-stream", fileName);
            //var path = Path.Combine(AppContext.BaseDirectory, "系统资源", "系统资源.xlsx");
            //stream = new FileStream(path, FileMode.Open);
            //return new PhysicalFileResult(path, "application/octet-stream") { FileDownloadName = Path.GetFileName(path) };
        }

        /// <summary>
        /// 导出模板。
        /// </summary>
        /// <param name="token"></param>
        /// <param name="rId"></param>
        /// <returns></returns>
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        [ProducesResponseType(typeof(FileStreamResult), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public ActionResult ExportDataDicTemplate(Guid token, Guid rId)
        {
            if (_AccountManager.GetOrLoadContextByToken(token, _ServiceProvider) is not OwContext context) return Unauthorized(OwHelper.GetLastErrorMessage());
            var srTask = _DbContext.DD_SystemResources.FindAsync(rId).AsTask();
            var sr = srTask.Result;
            using var workbook = new HSSFWorkbook();
            var sheet = workbook.CreateSheet("Sheet0");//创建一个名称为Sheet0的表  
            var fileName = $"{sr.Name}.xls";
            switch (sr.Name)
            {
                case nameof(_DbContext.Multilinguals):
                    {
                        _NpoiManager.WriteToExcel(_DbContext.Multilinguals.Take(0), typeof(Multilingual).GetProperties().Select(c => c.Name).ToArray(), sheet);
                    }
                    break;
                default:
                    break;
            }
            var stream = new MemoryStream();
            workbook.Write(stream, true);
            workbook.Close();
            stream.Seek(0, SeekOrigin.Begin);
            return File(stream, "application/octet-stream", fileName);
        }

        #region 简单字典的CRUD

        /// <summary>
        /// 获取指定类别数据字典的全部内容。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">支持通用查询条件。 </param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定类别Id无效。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllDataDicReturnDto> GetAllDataDic([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllDataDicReturnDto();
            var dbSet = _DbContext.DD_SimpleDataDics;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();

            // 使用EfHelper.GenerateWhereAnd进行通用查询条件处理
            coll = EfHelper.GenerateWhereAnd(coll, conditional);

            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 用数据字典目录码获取所有字典项。超管没有具体商户无法使用，仅针对有具体商户归属的用户才可使用。
        /// 仅针对简单字典。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">未知的商户Id。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">找不到指定目录Code的字典。</response>  
        [HttpGet]
        public ActionResult<GetDataDicByCatalogCodeReturnDto> GetDataDicByCatalogCode([FromQuery] GetDataDicByCatalogCodeParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetDataDicByCatalogCodeReturnDto();
            var dbSet = _DbContext.DD_DataDicCatalogs;
            var coll = dbSet.AsNoTracking();
            if (_AccountManager.IsAdmin(context.User))  //若是超管
                coll = coll.Where(c => c.OrgId == null);
            else
            {
                if (!_MerchantManager.GetIdByUserId(context.User.Id, out var merchId)) return BadRequest("未知的商户Id");
                if (context.User.OrgId is null) //若没有指定机构
                {
                    coll = coll.Where(c => c.OrgId == merchId);
                }
                else
                {
                    coll = coll.Where(c => c.OrgId == context.User.OrgId);
                }
            }

            var catalog = coll.FirstOrDefault(c => c.Code == model.SimpleDicCatalogCode);
            if (catalog is null)
            {
                return NotFound("找不到指定目录Code的字典。");
            }
            var dataDics = _DbContext.DD_SimpleDataDics.Where(c => c.DataDicId == catalog.Id).AsNoTracking();
            result.Result.AddRange(dataDics);

            return result;
        }

        /// <summary>
        /// 增加一个数据字典(目录)。无论CopyToChildren的值，对于超管都会增加一个全局字典目录(OrgId为空)，对于商管都会增加一个商户级字典目录(OrgId为商户Id)。
        /// 只有当勾选了复制选项(CopyToChildren=true)时，才会将字典目录复制到公司型机构。超管会复制到所有公司型机构，商管会复制到其商户下所有公司型机构。
        /// 考虑到机构是树状结构，会遵循机构的层级关系进行复制。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="302">需要管理员权限。</response>  
        /// <response code="400">未知的商户Id。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpPost]
        public ActionResult<AddDataDicCatalogReturnDto> AddDataDicCatalog(AddDataDicCatalogParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();

            if (!context.User.IsSuperAdmin) //若非超管
            {
                string err;
                if (!_AuthorizationManager.Demand(out err, "B.0")) return StatusCode((int)HttpStatusCode.Forbidden, err);
                if (!context.User.IsAdmin()) return StatusCode((int)HttpStatusCode.Forbidden, "需要管理员权限。");
            }

            // 根据用户类型设置正确的OrgId
            Guid? targetOrgId = null;
            if (context.User.IsSuperAdmin)
            {
                targetOrgId = null; // 超管使用全局目录
            }
            else if (context.User.IsMerchantAdmin)
            {
                if (!_MerchantManager.GetIdByUserId(context.User.Id, out var merchId))
                    return BadRequest("无法获取商户ID");
                targetOrgId = merchId;
            }
            else if (context.User.OrgId.HasValue)
            {
                targetOrgId = context.User.OrgId;
            }

            // 检查同一机构下是否已存在相同Code的目录
            if (_DbContext.DD_DataDicCatalogs.Any(c => c.OrgId == targetOrgId && c.Code == model.Item.Code))
                return BadRequest($"已存在相同Code的目录: {model.Item.Code}");

            // 创建主字典目录并使用AutoMapper复制属性
            var mainCatalog = _Mapper.Map<DataDicCatalog>(model.Item);
            mainCatalog.OrgId = targetOrgId;
            mainCatalog.GenerateNewId();

            _DbContext.DD_DataDicCatalogs.Add(mainCatalog);

            // 只有勾选了复制选项时才复制到公司型机构
            if (model.CopyToChildren)
            {
                try
                {
                    // 获取目标机构集合 - 使用 ToList() 确保查询执行
                    var targetOrgs = new List<PlOrganization>();

                    if (context.User.IsSuperAdmin)
                    {
                        // 超管：获取所有公司型机构
                        targetOrgs = _DbContext.PlOrganizations
                            .Where(o => o.Otc == 2)
                            .AsNoTracking()
                            .ToList();
                    }
                    else if (context.User.IsMerchantAdmin && targetOrgId.HasValue)
                    {
                        // 商管：获取所属商户下所有公司型机构
                        var allOrgs = _OrganizationManager.GetOrLoadByMerchantId(targetOrgId.Value);
                        if (allOrgs != null)
                        {
                            targetOrgs = allOrgs.Values
                                .Where(o => o.Otc == 2)
                                .ToList();
                        }
                    }

                    // 获取已存在相同Code的目录的机构Id
                    var existingCatalogOrgIds = _DbContext.DD_DataDicCatalogs
                        .Where(c => c.Code == model.Item.Code)
                        .AsNoTracking()
                        .Select(c => c.OrgId)
                        .ToList();

                    var existingIdSet = new HashSet<Guid?>(existingCatalogOrgIds);

                    // 处理每个目标机构
                    foreach (var org in targetOrgs)
                    {
                        // 跳过已存在相同Code的机构
                        if (existingIdSet.Contains(org.Id))
                            continue;

                        // 创建新目录 - 只使用DataDicCatalog实际拥有的属性
                        var newCatalog = new DataDicCatalog
                        {
                            Code = mainCatalog.Code,
                            DisplayName = mainCatalog.DisplayName,
                            OrgId = org.Id
                        };

                        _DbContext.DD_DataDicCatalogs.Add(newCatalog);
                        existingIdSet.Add(org.Id);
                    }
                }
                catch (Exception ex)
                {
                    // 捕获并记录异常，但仍然保存主目录
                    var log = new OwSystemLog
                    {
                        ExtraString = $"复制字典目录到子机构时出错: {ex.Message}",
                        ActionId = "DataDic.AddDataDicCatalog.CopyToChildren",
                        WorldDateTime = DateTime.Now,
                        OrgId = context.User.OrgId,
                    };

                    // 如果需要存储完整的堆栈跟踪，可以使用JsonObjectString属性
                    if (!string.IsNullOrEmpty(ex.StackTrace))
                    {
                        log.JsonObjectString = ex.StackTrace;
                    }

                    _DbContext.OwSystemLogs.Add(log);
                }
            }

            _DbContext.SaveChanges();
            return new AddDataDicCatalogReturnDto { Id = mainCatalog.Id };
        }

        /// <summary>
        /// 给指定简单数据字典增加一项。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">在同一类别同一组织机构下指定了重复的Code。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPost]
        public ActionResult<AddSimpleDataDicReturnDto> AddSimpleDataDic(AddSimpleDataDicParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            if (!_AuthorizationManager.Demand(out err, "B.0")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new AddSimpleDataDicReturnDto();
            if (_DbContext.DD_SimpleDataDics.Any(c => c.DataDicId == model.Item.DataDicId && c.Code == model.Item.Code))   //若重复
                return BadRequest("Id重复");
            if (model.Item.DataDicId is null)
                return BadRequest($"{nameof(model.Item.DataDicId)} 不能为空。");
            model.Item.GenerateNewId();
            var id = model.Item.Id;
            _DbContext.DD_SimpleDataDics.Add(model.Item);
            if (model.CopyToChildren)    //若需要向下传播
            {
                if (_DbContext.DD_DataDicCatalogs.FirstOrDefault(c => c.Id == model.Item.DataDicId) is DataDicCatalog catalog)  //若有字典
                {
                    var allCatalog = _DbContext.DD_DataDicCatalogs.Where(c => c.Code == catalog.Code && c.Id != model.Item.DataDicId);  //排除自身后的字典
                    foreach (var item in allCatalog)
                    {
                        var sdd = (SimpleDataDic)model.Item.Clone();
                        sdd.DataDicId = item.Id;
                        _DbContext.DD_SimpleDataDics.Add(sdd);
                    }
                }
            }
            _DbContext.SaveChanges();
            result.Id = id;
            return result;
        }

        /// <summary>
        /// 修改简单字典的项。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定实体的Id不存在。通常这是Bug.在极端情况下可能是并发问题。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPut]
        public ActionResult<ModifySimpleDataDicReturnDto> ModifySimpleDataDic(ModifySimpleDataDicParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            if (!_AuthorizationManager.Demand(out err, "B.0")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new ModifySimpleDataDicReturnDto();
            if (!_EntityManager.ModifyWithMarkDelete(model.Items))
            {
                var errResult = new StatusCodeResult(OwHelper.GetLastError()) { };
                return errResult;
            }
            foreach (var item in model.Items)
            {
                _DbContext.Entry(item).Property(c => c.DataDicId).IsModified = false;
                _DbContext.Entry(item).Property(c => c.CreateAccountId).IsModified = false;
                _DbContext.Entry(item).Property(c => c.CreateDateTime).IsModified = false;
            }
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 删除简单数据字典中的一项。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定实体的Id不存在。通常这是Bug.在极端情况下可能是并发问题。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpDelete]
        public ActionResult<RemoveSimpleDataDicReturnDto> RemoveSimpleDataDic(RemoveSimpleDataDicParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            if (!_AuthorizationManager.Demand(out err, "B.0")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new RemoveSimpleDataDicReturnDto();
            var id = model.Id;
            DbSet<SimpleDataDic> dbSet = _DbContext.DD_SimpleDataDics;
            var item = dbSet.Find(id);
            if (item is null) return BadRequest();
            _EntityManager.Remove(item);
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 恢复指定的简单数据字典。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定实体的Id不存在。通常这是Bug.在极端情况下可能是并发问题。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPost]
        public ActionResult<RestoreSimpleDataDicReturnDto> RestoreSimpleDataDic(RestoreSimpleDataDicParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            if (!_AuthorizationManager.Demand(out err, "B.0")) return StatusCode((int)HttpStatusCode.Forbidden, err);

            var result = new RestoreSimpleDataDicReturnDto();
            if (!_EntityManager.Restore<SimpleDataDic>(model.Id))
            {
                var errResult = new StatusCodeResult(OwHelper.GetLastError()) { };
                return errResult;
            }
            _DbContext.SaveChanges();
            return result;
        }

        #endregion 简单字典的CRUD

        /// <summary>
        /// 获取所有业务大类的数据。此接口返回的是缓存数据,客户端通常会2分钟才实际刷新一次.
        /// </summary>
        /// <param name="token">登录令牌。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        [ResponseCache(Duration = 120, Location = ResponseCacheLocation.Any)]
        public ActionResult<GetAllBusinessTypeReturnDto> GetAllBusinessType(Guid token)
        {
            if (_AccountManager.GetOrLoadContextByToken(token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllBusinessTypeReturnDto();
            var collBase = _DbContext.DD_BusinessTypeDataDics.OrderBy(c => c.OrderNumber).AsNoTracking();
            var prb = _EntityManager.GetAll(collBase, 0, -1);
            _Mapper.Map(prb, result);
            return result;
        }

        #region 港口相关

        /// <summary>
        /// 获取港口。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">支持通用查询条件。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定类别Id无效。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllPortReturnDto> GetAllPlPort([FromQuery] PagingParamsDtoBase model, [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllPortReturnDto();
            var dbSet = _DbContext.DD_PlPorts;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            if (_AccountManager.IsAdmin(context.User))  //若是超管
                coll = coll.Where(c => c.OrgId == null);
            else
            {
                if (!_MerchantManager.GetIdByUserId(context.User.Id, out var merchId)) return BadRequest("未知的商户Id");
                if (context.User.OrgId is null) //若没有指定机构
                {
                    coll = coll.Where(c => c.OrgId == merchId);
                }
                else
                {
                    coll = coll.Where(c => c.OrgId == context.User.OrgId);
                }
            }

            // 使用EfHelper.GenerateWhereAnd进行通用查询条件处理
            coll = EfHelper.GenerateWhereAnd(coll, conditional);

            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 增加一个港口数据字典。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">在同一类别同一组织机构下指定了重复的Code。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPost]
        public ActionResult<AddPlPortReturnDto> AddPlPort(AddPlPortParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            if (!_AuthorizationManager.Demand(out err, "B.6")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new AddPlPortReturnDto();

            var dbSet = _DbContext.DD_PlPorts;
            if (dbSet.Any(c => c.OrgId == model.Item.OrgId && c.Code == model.Item.Code))   //若重复
                return BadRequest("重复");
            model.Item.GenerateNewId();
            var id = model.Item.Id;
            dbSet.Add(model.Item);
            _DbContext.SaveChanges();
            result.Id = id;
            return result;
        }

        /// <summary>
        /// 修改港口数据字典项。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定实体的Id不存在。通常这是Bug.在极端情况下可能是并发问题。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPut]
        public ActionResult<ModifyPlPortReturnDto> ModifyPlPort(ModifyPlPortParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            if (!_AuthorizationManager.Demand(out err, "B.6")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new ModifyPlPortReturnDto();
            if (!_EntityManager.ModifyWithMarkDelete(model.Items))
            {
                var errResult = new StatusCodeResult(OwHelper.GetLastError()) { };
                return errResult;
            }
            foreach (var item in model.Items)   //避免修改个别属性
            {
                _DbContext.Entry(item).Property(c => c.IsDelete).IsModified = false;
            }
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 删除港口字典中的一项。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定实体的Id不存在。通常这是Bug.在极端情况下可能是并发问题。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpDelete]
        public ActionResult<RemovePlPortReturnDto> RemovePlPort(RemovePlPortParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            if (!_AuthorizationManager.Demand(out err, "B.6")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new RemovePlPortReturnDto();
            var id = model.Id;
            var dbSet = _DbContext.DD_PlPorts;
            var item = dbSet.Find(id);
            if (item is null) return BadRequest();
            _EntityManager.Remove(item);
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 恢复指定的被删除港口字典。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定实体的Id不存在。通常这是Bug.在极端情况下可能是并发问题。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPost]
        public ActionResult<RestorePlPortReturnDto> RestorePlPort(RestorePlPortParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            if (!_AuthorizationManager.Demand(out err, "B.6")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new RestorePlPortReturnDto();
            if (!_EntityManager.Restore<PlPort>(model.Id))
            {
                var errResult = new StatusCodeResult(OwHelper.GetLastError()) { };
                return errResult;
            }
            _DbContext.SaveChanges();
            return result;
        }
        #endregion 港口相关

        #region 航线相关

        /// <summary>
        /// 获取航线。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">支持通用查询条件。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定类别Id无效。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllPlCargoRouteReturnDto> GetAllPlCargoRoute([FromQuery] PagingParamsDtoBase model, [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllPlCargoRouteReturnDto();

            var dbSet = _DbContext.DD_PlCargoRoutes;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            if (_AccountManager.IsAdmin(context.User))  //若是超管
                coll = coll.Where(c => c.OrgId == null);
            else
            {
                if (!_MerchantManager.GetIdByUserId(context.User.Id, out var merchId)) return BadRequest("未知的商户Id");
                if (context.User.OrgId is null) //若没有指定机构
                {
                    coll = coll.Where(c => c.OrgId == merchId);
                }
                else
                {
                    coll = coll.Where(c => c.OrgId == context.User.OrgId);
                }
            }

            // 使用EfHelper.GenerateWhereAnd进行通用查询条件处理
            coll = EfHelper.GenerateWhereAnd(coll, conditional);

            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 增加一个航线数据字典。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">在同一类别同一组织机构下指定了重复的Code。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPost]
        public ActionResult<AddPlCargoRouteReturnDto> AddPlCargoRoute(AddPlCargoRouteParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            if (!_AuthorizationManager.Demand(out err, "B.7")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new AddPlCargoRouteReturnDto();
            var dbSet = _DbContext.DD_PlCargoRoutes;
            if (dbSet.Any(c => c.OrgId == model.Item.OrgId && c.Code == model.Item.Code))   //若重复
                return BadRequest();
            model.Item.GenerateNewId();
            var id = model.Item.Id;
            dbSet.Add(model.Item);
            _DbContext.SaveChanges();
            result.Id = id;
            return result;
        }

        /// <summary>
        /// 修改航线数据字典项。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定实体的Id不存在。通常这是Bug.在极端情况下可能是并发问题。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPut]
        public ActionResult<ModifyPlCargoRouteReturnDto> ModifyPlCargoRoute(ModifyPlCargoRouteParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            if (!_AuthorizationManager.Demand(out err, "B.7")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new ModifyPlCargoRouteReturnDto();
            if (!_EntityManager.ModifyWithMarkDelete(model.Items))
            {
                var errResult = new StatusCodeResult(OwHelper.GetLastError()) { };
                return errResult;
            }
            foreach (var item in model.Items)
            {
                _DbContext.Entry(item).Property(c => c.IsDelete).IsModified = false;
            }
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 删除航线字典中的一项。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定实体的Id不存在。通常这是Bug.在极端情况下可能是并发问题。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpDelete]
        public ActionResult<RemovePlCargoRouteReturnDto> RemovePlCargoRoute(RemoveCargoPlRouteParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            if (!_AuthorizationManager.Demand(out err, "B.7")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new RemovePlCargoRouteReturnDto();
            var id = model.Id;
            var dbSet = _DbContext.DD_PlCargoRoutes;
            var item = dbSet.Find(id);
            if (item is null) return BadRequest();
            _EntityManager.Remove(item);
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 恢复指定的被删除航线字典。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定实体的Id不存在。通常这是Bug.在极端情况下可能是并发问题。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPost]
        public ActionResult<RestorePlCargoRouteReturnDto> RestorePlCargoRoute(RestorePlCargoRouteParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            if (!_AuthorizationManager.Demand(out err, "B.7")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new RestorePlCargoRouteReturnDto();
            if (!_EntityManager.Restore<PlCargoRoute>(model.Id))
            {
                var errResult = new StatusCodeResult(OwHelper.GetLastError()) { };
                return errResult;
            }
            _DbContext.SaveChanges();
            return result;
        }
        #endregion 航线相关

        #region 汇率相关
        /// <summary>
        /// 获取汇率。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">支持通用查询条件。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定类别Id无效。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllPlExchangeRateReturnDto> GetAllPlExchangeRate([FromQuery] PagingParamsDtoBase model, [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllPlExchangeRateReturnDto();
            var dbSet = _DbContext.DD_PlExchangeRates;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            if (_AccountManager.IsAdmin(context.User))  //若是超管
                coll = coll.Where(c => c.OrgId == null);
            else
            {
                if (!_MerchantManager.GetIdByUserId(context.User.Id, out var merchId)) return BadRequest("未知的商户Id");
                if (context.User.OrgId is null) //若没有指定机构
                {
                    coll = coll.Where(c => c.OrgId == merchId);
                }
                else
                {
                    coll = coll.Where(c => c.OrgId == context.User.OrgId);
                }
            }

            // 使用EfHelper.GenerateWhereAnd进行通用查询条件处理
            coll = EfHelper.GenerateWhereAnd(coll, conditional);

            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 扩展获取汇率。返回当前用户登录机构的汇率，且符合条件的所有汇率对象。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定类别Id无效。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetCurrentOrgExchangeRateReturnDto> GetCurrentOrgExchangeRate([FromQuery] GetCurrentOrgExchangeRateParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetCurrentOrgExchangeRateReturnDto();
            model.StartDateTime ??= DateTime.Now;
            model.EndDateTime ??= DateTime.Now;

            var dbSet = _DbContext.DD_PlExchangeRates;
            var coll = dbSet.AsNoTracking();
            coll = coll.Where(c => c.OrgId == context.User.OrgId);
            coll = coll.Where(c => c.BeginDate <= model.StartDateTime && c.EndData >= model.EndDateTime);

            if (!_MerchantManager.GetIdByUserId(context.User.Id, out var merchId))
                return result;

            var orgs = _OrganizationManager.GetOrLoadByMerchantId(merchId.Value);
            if (!orgs.TryGetValue(context.User.OrgId.Value, out var org))
                return BadRequest($"找不到指定的登录公司Id={merchId}");

            if (string.IsNullOrWhiteSpace(org.BaseCurrencyCode))
                return BadRequest($"公司本币设置错误，本币代码为:{org.BaseCurrencyCode}");
            //var curr = _DbContext.DD_PlCurrencys.Find(org.BaseCurrencyId.Value); if (curr is null) return result;

            coll = coll.Where(c => c.DCurrency == org.BaseCurrencyCode);

            result.Result.AddRange(coll);
            return result;
        }

        /// <summary>
        /// 增加一个汇率记录。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">参数错误。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPost]
        public ActionResult<AddPlExchangeRateReturnDto> AddPlExchangeRate(AddPlExchangeRateParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            if (!_AuthorizationManager.Demand(out err, "B.4")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new AddPlExchangeRateReturnDto();
            model.Item.GenerateNewId();
            var id = model.Item.Id;
            _DbContext.DD_PlExchangeRates.Add(model.Item);
            _DbContext.SaveChanges();
            result.Id = id;
            return result;
        }

        /// <summary>
        /// 修改汇率项。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定实体的Id不存在。通常这是Bug.在极端情况下可能是并发问题。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPut]
        public ActionResult<ModifyPlExchangeRateReturnDto> ModifyPlExchangeRate(ModifyPlExchangeRateParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            if (!_AuthorizationManager.Demand(out err, "B.4")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new ModifyPlExchangeRateReturnDto();
            if (!_EntityManager.Modify(model.Items))
            {
                var errResult = new StatusCodeResult(OwHelper.GetLastError()) { };
                return errResult;
            }
            _DbContext.SaveChanges();
            return result;
        }

        #endregion 汇率相关

        #region 单位换算相关
        /// <summary>
        /// 获取单位换算。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">支持通用查询条件。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定类别Id无效。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllUnitConversionReturnDto> GetAllUnitConversion([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllUnitConversionReturnDto();
            var dbSet = _DbContext.DD_UnitConversions;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            if (_AccountManager.IsAdmin(context.User))  //若是超管
                coll = coll.Where(c => c.OrgId == null);
            else
            {
                if (!_MerchantManager.GetIdByUserId(context.User.Id, out var merchId)) return BadRequest("未知的商户Id");
                if (context.User.OrgId is null) //若没有指定机构
                {
                    coll = coll.Where(c => c.OrgId == merchId);
                }
                else
                {
                    coll = coll.Where(c => c.OrgId == context.User.OrgId);
                }
            }

            // 使用EfHelper.GenerateWhereAnd进行通用查询条件处理
            coll = EfHelper.GenerateWhereAnd(coll, conditional);

            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 增加单位换算记录。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">参数错误。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPost]
        public ActionResult<AddUnitConversionReturnDto> AddUnitConversion(AddUnitConversionParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            if (!_AuthorizationManager.Demand(out err, "B.10")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new AddUnitConversionReturnDto();
            model.Item.GenerateNewId();
            var id = model.Item.Id;
            _DbContext.DD_UnitConversions.Add(model.Item);
            _DbContext.SaveChanges();
            result.Id = id;
            return result;
        }

        /// <summary>
        /// 修改单位换算记录。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定实体的Id不存在。通常这是Bug.在极端情况下可能是并发问题。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPut]
        public ActionResult<ModifyUnitConversionReturnDto> ModifyUnitConversion(ModifyUnitConversionParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            if (!_AuthorizationManager.Demand(out err, "B.10")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new ModifyUnitConversionReturnDto();
            if (!_EntityManager.ModifyWithMarkDelete(model.Items))
            {
                var errResult = new StatusCodeResult(OwHelper.GetLastError()) { };
                return errResult;
            }
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 删除单位换算的记录。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定实体的Id不存在。通常这是Bug.在极端情况下可能是并发问题。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpDelete]
        public ActionResult<RemoveUnitConversionReturnDto> RemoveUnitConversion(RemoveUnitConversionParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            if (!_AuthorizationManager.Demand(out err, "B.10")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new RemoveUnitConversionReturnDto();
            var id = model.Id;
            var dbSet = _DbContext.DD_UnitConversions;
            var item = dbSet.Find(id);
            if (item is null) return BadRequest();
            _EntityManager.Remove(item);
            _DbContext.SaveChanges();
            //if (item.DataDicType == 1) //若是简单字典
            //    _DbContext.Database.ExecuteSqlRaw($"delete from {nameof(_DbContext.SimpleDataDics)} where {nameof(SimpleDataDic.DataDicId)}='{id.ToString()}'");
            //else //其他字典待定
            //{

            //}
            return result;
        }

        /// <summary>
        /// 恢复指定的被删除单位换算记录。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定实体的Id不存在。通常这是Bug.在极端情况下可能是并发问题。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPost]
        public ActionResult<RestoreUnitConversionReturnDto> RestoreUnitConversion(RestoreUnitConversionParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            if (!_AuthorizationManager.Demand(out err, "B.10")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new RestoreUnitConversionReturnDto();
            if (!_EntityManager.Restore<UnitConversion>(model.Id))
            {
                var errResult = new StatusCodeResult(OwHelper.GetLastError()) { };
                return errResult;
            }
            _DbContext.SaveChanges();
            return result;
        }

        #endregion 单位换算相关

        #region 费用种类相关
        /// <summary>
        /// 获取费用种类。超管可以不受限制，其他用户最多仅能看到其所属公司/机构下的实体。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">支持通用查询的条件。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定类别Id无效。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllFeesTypeReturnDto> GetAllFeesType([FromQuery] PagingParamsDtoBase model, [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllFeesTypeReturnDto();
            var dbSet = _DbContext.DD_FeesTypes;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            if (_AccountManager.IsAdmin(context.User))  //若是超管
                coll = coll.Where(c => c.OrgId == null);
            else
            {
                // 获取用户管辖范围内的公司型组织机构ID
                var companyIds = _OrganizationManager.GetCompanyIds(context.User);
                coll = coll.Where(c => companyIds.Contains(c.OrgId));
            }
            coll = EfHelper.GenerateWhereAnd(coll, conditional);
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 增加费用种类记录。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">参数错误。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPost]
        public ActionResult<AddFeesTypeReturnDto> AddFeesType(AddFeesTypeParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            if (!_AuthorizationManager.Demand(out err, "B.8")) return StatusCode((int)HttpStatusCode.Forbidden, err);

            var result = new AddFeesTypeReturnDto();

            // 确保使用当前用户的组织机构ID
            model.Item.OrgId = context.User.OrgId;

            // 生成主记录ID
            model.Item.GenerateNewId();
            var id = model.Item.Id;

            // 添加主记录
            _DbContext.DD_FeesTypes.Add(model.Item);

            // 如果需要同步到子机构
            if (model.CopyToChildren)
            {
                // 获取用户管辖范围内的公司型组织机构ID
                var companyIds = _OrganizationManager.GetCompanyIds(context.User, true);

                foreach (var orgId in companyIds)
                {
                    // 检查是否已存在相同Code的记录
                    if (_DbContext.DD_FeesTypes.Any(f => f.OrgId == orgId && f.Code == model.Item.Code))
                        continue;

                    // 使用Clone方法创建深表副本
                    var newItem = (FeesType)model.Item.Clone();
                    newItem.OrgId = orgId;
                    newItem.GenerateNewId(); // 确保新记录有唯一ID

                    _DbContext.DD_FeesTypes.Add(newItem);
                }
            }

            _DbContext.SaveChanges();
            result.Id = id;
            return result;
        }

        /// <summary>
        /// 修改费用种类记录。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定实体的Id不存在。通常这是Bug.在极端情况下可能是并发问题。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPut]
        public ActionResult<ModifyFeesTypeReturnDto> ModifyFeesType(ModifyFeesTypeParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            if (!_AuthorizationManager.Demand(out err, "B.8")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new ModifyFeesTypeReturnDto();
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
        /// 删除费用种类的记录。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定实体的Id不存在。通常这是Bug.在极端情况下可能是并发问题。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpDelete]
        public ActionResult<RemoveFeesTypeReturnDto> RemoveFeesType(RemoveFeesTypeParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            if (!_AuthorizationManager.Demand(out err, "B.8")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new RemoveFeesTypeReturnDto();
            var id = model.Id;
            var dbSet = _DbContext.DD_FeesTypes;
            var item = dbSet.Find(id);
            if (item is null) return BadRequest();
            _EntityManager.Remove(item);
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 恢复指定的被删除费用种类记录。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定实体的Id不存在。通常这是Bug.在极端情况下可能是并发问题。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPost]
        public ActionResult<RestoreFeesTypeReturnDto> RestoreFeesType(RestoreFeesTypeParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            if (!_AuthorizationManager.Demand(out err, "B.8")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new RestoreFeesTypeReturnDto();
            if (!_EntityManager.Restore<FeesType>(model.Id))
            {
                var errResult = new StatusCodeResult(OwHelper.GetLastError()) { };
                return errResult;
            }
            _DbContext.SaveChanges();
            return result;
        }

        #endregion 费用种类相关

        #region 业务编码规则相关
        /// <summary>
        /// 获取业务编码规则。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">支持通用查询条件。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定类别Id无效。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllJobNumberRuleReturnDto> GetAllJobNumberRule([FromQuery] PagingParamsDtoBase model, [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllJobNumberRuleReturnDto();
            var dbSet = _DbContext.DD_JobNumberRules;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            if (_AccountManager.IsAdmin(context.User))  //若是超管
                coll = coll.Where(c => c.OrgId == null);
            else
            {
                if (!_MerchantManager.GetIdByUserId(context.User.Id, out var merchId)) return BadRequest("未知的商户Id");
                if (context.User.OrgId is null) //若没有指定机构
                {
                    coll = coll.Where(c => c.OrgId == merchId);
                }
                else
                {
                    coll = coll.Where(c => c.OrgId == context.User.OrgId);
                }
            }

            // 使用EfHelper.GenerateWhereAnd进行通用查询条件处理
            coll = EfHelper.GenerateWhereAnd(coll, conditional);

            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 增加业务编码规则记录。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">参数错误。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPost]
        public ActionResult<AddJobNumberRuleReturnDto> AddJobNumberRule(AddJobNumberRuleParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            if (!_AuthorizationManager.Demand(out err, "B.2")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            if (model.Item.BusinessTypeId == ProjectContent.AeId)    //若是空运出口业务
            {
                if (!_AuthorizationManager.Demand(out err, "D0.1.1.2")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (model.Item.BusinessTypeId == ProjectContent.AiId)    //若是空运进口业务
                if (!_AuthorizationManager.Demand(out err, "D1.1.1.2")) return StatusCode((int)HttpStatusCode.Forbidden, err);

            var result = new AddJobNumberRuleReturnDto();
            model.Item.GenerateNewId();
            var id = model.Item.Id;
            model.Item.OrgId = context.User.OrgId;
            _DbContext.DD_JobNumberRules.Add(model.Item);
            _DbContext.SaveChanges();
            result.Id = id;
            return result;
        }

        /// <summary>
        /// 修改业务编码规则记录。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定实体的Id不存在。通常这是Bug.在极端情况下可能是并发问题。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPut]
        public ActionResult<ModifyJobNumberRuleReturnDto> ModifyJobNumberRule(ModifyJobNumberRuleParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            if (model.Items == null || model.Items.Count == 0) return BadRequest($"{nameof(model.Items)} 为空集合。");

            string err;
            if (!_AuthorizationManager.Demand(out err, "B.2")) return StatusCode((int)HttpStatusCode.Forbidden, err);

            if (model.Items.Any(item => item.BusinessTypeId == ProjectContent.AeId))    //若是空运出口业务
                if (!_AuthorizationManager.Demand(out err, "D0.1.1.3")) return StatusCode((int)HttpStatusCode.Forbidden, err);

            var result = new ModifyJobNumberRuleReturnDto();

            try
            {
                // 在修改之前保存OrgId值
                var originalOrgIds = new Dictionary<Guid, Guid?>();
                foreach (var item in model.Items)
                {
                    if (item.Id != Guid.Empty)
                    {
                        var originalEntity = _DbContext.DD_JobNumberRules.AsNoTracking().FirstOrDefault(e => e.Id == item.Id);
                        if (originalEntity != null)
                        {
                            originalOrgIds[item.Id] = originalEntity.OrgId;
                        }
                    }
                }

                // 执行修改操作
                if (!_EntityManager.ModifyWithMarkDelete(model.Items))
                {
                    return new StatusCodeResult(OwHelper.GetLastError());
                }

                // 确保OrgId属性不被修改
                foreach (var item in model.Items)
                {
                    if (originalOrgIds.TryGetValue(item.Id, out var orgId))
                    {
                        var entry = _DbContext.Entry(item);
                        if (entry.State == EntityState.Modified)
                        {
                            entry.Property(e => e.OrgId).IsModified = false;
                            // 确保OrgId值保持不变
                            item.OrgId = orgId;
                        }
                    }
                }

                _DbContext.SaveChanges();
                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "修改业务编码规则记录时出错");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"修改业务编码规则记录时出错: {ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// 删除业务编码规则的记录。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定实体的Id不存在。通常这是Bug.在极端情况下可能是并发问题。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpDelete]
        public ActionResult<RemoveJobNumberRuleReturnDto> RemoveJobNumberRule(RemoveJobNumberRuleParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            if (!_AuthorizationManager.Demand(out err, "B.2")) return StatusCode((int)HttpStatusCode.Forbidden, err);

            var result = new RemoveJobNumberRuleReturnDto();
            var id = model.Id;
            var dbSet = _DbContext.DD_JobNumberRules;
            var item = dbSet.Find(id);
            if (item is null) return BadRequest();
            if (item.BusinessTypeId == ProjectContent.AeId)    //若是空运出口业务
            {
                if (!_AuthorizationManager.Demand(out err, "D0.1.1.4")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (item.BusinessTypeId == ProjectContent.AiId)    //若是空运进口业务
                if (!_AuthorizationManager.Demand(out err, "D1.1.1.4")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            _EntityManager.Remove(item);
            _DbContext.SaveChanges();
            //if (item.DataDicType == 1) //若是简单字典
            //    _DbContext.Database.ExecuteSqlRaw($"delete from {nameof(_DbContext.SimpleDataDics)} where {nameof(SimpleDataDic.DataDicId)}='{id.ToString()}'");
            //else //其他字典待定
            //{

            //}
            return result;
        }

        /// <summary>
        /// 恢复指定的被删除业务编码规则记录。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定实体的Id不存在。通常这是Bug.在极端情况下可能是并发问题。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPost]
        public ActionResult<RestoreJobNumberRuleReturnDto> RestoreJobNumberRule(RestoreJobNumberRuleParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            if (!_AuthorizationManager.Demand(out err, "B.2")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new RestoreJobNumberRuleReturnDto();
            if (!_EntityManager.Restore<JobNumberRule>(model.Id))
            {
                var errResult = new StatusCodeResult(OwHelper.GetLastError()) { };
                return errResult;
            }
            _DbContext.SaveChanges();
            return result;
        }

        #endregion 业务编码规则相关

        #region 其它编码规则相关
        /// <summary>
        /// 获取其它编码规则。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">支持通用查询条件。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定类别Id无效。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllOtherNumberRuleReturnDto> GetAllOtherNumberRule([FromQuery] PagingParamsDtoBase model, [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllOtherNumberRuleReturnDto();
            var dbSet = _DbContext.DD_OtherNumberRules;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            if (_AccountManager.IsAdmin(context.User))  //若是超管
                coll = coll.Where(c => c.OrgId == null);
            else
            {
                if (!_MerchantManager.GetIdByUserId(context.User.Id, out var merchId)) return BadRequest("未知的商户Id");
                if (context.User.OrgId is null) //若没有指定机构
                {
                    coll = coll.Where(c => c.OrgId == merchId);
                }
                else
                {
                    coll = coll.Where(c => c.OrgId == context.User.OrgId);
                }
            }

            // 使用EfHelper.GenerateWhereAnd进行通用查询条件处理
            coll = EfHelper.GenerateWhereAnd(coll, conditional);

            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 增加其它编码规则记录。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">参数错误。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpPost]
        public ActionResult<AddOtherNumberRuleReturnDto> AddOtherNumberRule(AddOtherNumberRuleParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new AddOtherNumberRuleReturnDto();
            model.Item.GenerateNewId();
            var id = model.Item.Id;
            model.Item.OrgId = context.User.OrgId;
            _DbContext.DD_OtherNumberRules.Add(model.Item);
            _DbContext.SaveChanges();
            result.Id = id;
            return result;
        }

        /// <summary>
        /// 修改其它编码规则记录。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定实体的Id不存在。通常这是Bug.在极端情况下可能是并发问题。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpPut]
        public ActionResult<ModifyOtherNumberRuleReturnDto> ModifyOtherNumberRule(ModifyOtherNumberRuleParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyOtherNumberRuleReturnDto();
            if (!_EntityManager.ModifyWithMarkDelete(model.Items))
            {
                var errResult = new StatusCodeResult(OwHelper.GetLastError()) { };
                return errResult;
            }
            model.Items.Select(c => c.Id).ToList().ForEach(c =>
            {
                var entity = _DbContext.DD_OtherNumberRules.Find(c);
                if (entity is null) return;
                _DbContext.Entry(entity).Property(c => c.OrgId).IsModified = false;
            });
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 删除其它编码规则的记录。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定实体的Id不存在。通常这是Bug.在极端情况下可能是并发问题。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpDelete]
        public ActionResult<RemoveOtherNumberRuleReturnDto> RemoveOtherNumberRule(RemoveOtherNumberRuleParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemoveOtherNumberRuleReturnDto();
            var id = model.Id;
            var dbSet = _DbContext.DD_OtherNumberRules;
            var item = dbSet.Find(id);
            if (item is null) return BadRequest();
            _EntityManager.Remove(item);
            _DbContext.SaveChanges();
            //if (item.DataDicType == 1) //若是简单字典
            //    _DbContext.Database.ExecuteSqlRaw($"delete from {nameof(_DbContext.SimpleDataDics)} where {nameof(SimpleDataDic.DataDicId)}='{id.ToString()}'");
            //else //其他字典待定
            //{

            //}
            return result;
        }

        /// <summary>
        /// 恢复指定的被删除其它编码规则记录。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定实体的Id不存在。通常这是Bug.在极端情况下可能是并发问题。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpPost]
        public ActionResult<RestoreOtherNumberRuleReturnDto> RestoreOtherNumberRule(RestoreOtherNumberRuleParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RestoreOtherNumberRuleReturnDto();
            if (!_EntityManager.Restore<OtherNumberRule>(model.Id))
            {
                var errResult = new StatusCodeResult(OwHelper.GetLastError()) { };
                return errResult;
            }
            _DbContext.SaveChanges();
            return result;
        }

        #endregion 其它编码规则相关

        #region 国家相关
        /// <summary>
        /// 获取国家。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">查询的条件。支持通用查询条件。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定类别Id无效。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllPlCountryReturnDto> GetAllPlCountry([FromQuery] PagingParamsDtoBase model, [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllPlCountryReturnDto();
            var dbSet = _DbContext.DD_PlCountrys;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            if (_AccountManager.IsAdmin(context.User))  //若是超管
                coll = coll.Where(c => c.OrgId == null);
            else
            {
                if (!_MerchantManager.GetIdByUserId(context.User.Id, out var merchId)) return BadRequest("未知的商户Id");
                if (context.User.OrgId is null) //若没有指定机构
                {
                    coll = coll.Where(c => c.OrgId == merchId);
                }
                else
                {
                    coll = coll.Where(c => c.OrgId == context.User.OrgId);
                }
            }

            // 使用EfHelper.GenerateWhereAnd进行通用查询条件处理
            coll = EfHelper.GenerateWhereAnd(coll, conditional);

            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 增加国家记录。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">参数错误。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPost]
        public ActionResult<AddPlCountryReturnDto> AddPlCountry(AddPlCountryParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            if (!_AuthorizationManager.Demand(out err, "B.5")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new AddPlCountryReturnDto();
            model.Item.GenerateNewId();
            var id = model.Item.Id;
            _DbContext.DD_PlCountrys.Add(model.Item);
            _DbContext.SaveChanges();
            result.Id = id;
            return result;
        }

        /// <summary>
        /// 修改国家记录。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定实体的Id不存在。通常这是Bug.在极端情况下可能是并发问题。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPut]
        public ActionResult<ModifyPlCountryReturnDto> ModifyPlCountry(ModifyPlCountryParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            if (!_AuthorizationManager.Demand(out err, "B.5")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new ModifyPlCountryReturnDto();
            if (!_EntityManager.ModifyWithMarkDelete(model.Items))
            {
                var errResult = new StatusCodeResult(OwHelper.GetLastError()) { };
                return errResult;
            }
            foreach (var item in model.Items)
            {
                _DbContext.Entry(item).Property(c => c.IsDelete).IsModified = false;
            }
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 删除国家的记录。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定实体的Id不存在。通常这是Bug.在极端情况下可能是并发问题。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpDelete]
        public ActionResult<RemovePlCountryReturnDto> RemovePlCountry(RemovePlCountryParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            if (!_AuthorizationManager.Demand(out err, "B.5")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new RemovePlCountryReturnDto();
            var id = model.Id;
            var dbSet = _DbContext.DD_PlCountrys;
            var item = dbSet.Find(id);
            if (item is null) return BadRequest();
            _EntityManager.Remove(item);
            _DbContext.SaveChanges();
            //if (item.DataDicType == 1) //若是简单字典
            //    _DbContext.Database.ExecuteSqlRaw($"delete from {nameof(_DbContext.SimpleDataDics)} where {nameof(SimpleDataDic.DataDicId)}='{id.ToString()}'");
            //else //其他字典待定
            //{

            //}
            return result;
        }

        /// <summary>
        /// 恢复指定的被删除国家记录。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定实体的Id不存在。通常这是Bug.在极端情况下可能是并发问题。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPost]
        public ActionResult<RestorePlCountryReturnDto> RestorePlCountry(RestorePlCountryParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            if (!_AuthorizationManager.Demand(out err, "B.5")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new RestorePlCountryReturnDto();
            if (!_EntityManager.Restore<PlCountry>(model.Id))
            {
                var errResult = new StatusCodeResult(OwHelper.GetLastError()) { };
                return errResult;
            }
            _DbContext.SaveChanges();
            return result;
        }

        #endregion 国家相关

        #region 币种相关
        /// <summary>
        /// 获取币种。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">支持通用查询条件。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定类别Id无效。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllPlCurrencyReturnDto> GetAllPlCurrency([FromQuery] PagingParamsDtoBase model, [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllPlCurrencyReturnDto();
            var dbSet = _DbContext.DD_PlCurrencys;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            if (_AccountManager.IsAdmin(context.User))  //若是超管
                coll = coll.Where(c => c.OrgId == null);
            else
            {
                if (!_MerchantManager.GetIdByUserId(context.User.Id, out var merchId)) return BadRequest("未知的商户Id");
                if (context.User.OrgId is null) //若没有指定机构
                {
                    coll = coll.Where(c => c.OrgId == merchId);
                }
                else
                {
                    coll = coll.Where(c => c.OrgId == context.User.OrgId);
                }
            }

            // 使用EfHelper.GenerateWhereAnd进行通用查询条件处理
            coll = EfHelper.GenerateWhereAnd(coll, conditional);

            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 增加币种记录。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">参数错误。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPost]
        public ActionResult<AddPlCurrencyReturnDto> AddPlCurrency(AddPlCurrencyParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            if (!_AuthorizationManager.Demand(out err, "B.3")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new AddPlCurrencyReturnDto();
            model.Item.GenerateNewId();
            var id = model.Item.Id;
            _DbContext.DD_PlCurrencys.Add(model.Item);
            _DbContext.SaveChanges();
            result.Id = id;
            return result;
        }

        /// <summary>
        /// 修改币种记录。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定实体的Id不存在。通常这是Bug.在极端情况下可能是并发问题。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPut]
        public ActionResult<ModifyPlCurrencyReturnDto> ModifyPlCurrency(ModifyPlCurrencyParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            if (!_AuthorizationManager.Demand(out err, "B.3")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new ModifyPlCurrencyReturnDto();
            if (!_EntityManager.ModifyWithMarkDelete(model.Items))
            {
                var errResult = new StatusCodeResult(OwHelper.GetLastError()) { };
                return errResult;
            }
            foreach (var item in model.Items)
            {
                _DbContext.Entry(item).Property(c => c.IsDelete).IsModified = false;
            }
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 删除币种的记录。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定实体的Id不存在。通常这是Bug.在极端情况下可能是并发问题。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpDelete]
        public ActionResult<RemovePlCurrencyReturnDto> RemovePlCurrency(RemovePlCurrencyParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            if (!_AuthorizationManager.Demand(out err, "B.3")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new RemovePlCurrencyReturnDto();
            var id = model.Id;
            var dbSet = _DbContext.DD_PlCurrencys;
            var item = dbSet.Find(id);
            if (item is null) return BadRequest();
            _EntityManager.Remove(item);
            _DbContext.SaveChanges();
            //if (item.DataDicType == 1) //若是简单字典
            //    _DbContext.Database.ExecuteSqlRaw($"delete from {nameof(_DbContext.SimpleDataDics)} where {nameof(SimpleDataDic.DataDicId)}='{id.ToString()}'");
            //else //其他字典待定
            //{

            //}
            return result;
        }

        /// <summary>
        /// 恢复指定的被删除币种记录。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定实体的Id不存在。通常这是Bug.在极端情况下可能是并发问题。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPost]
        public ActionResult<RestorePlCurrencyReturnDto> RestorePlCurrency(RestorePlCurrencyParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            if (!_AuthorizationManager.Demand(out err, "B.3")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new RestorePlCurrencyReturnDto();
            if (!_EntityManager.Restore<PlCurrency>(model.Id))
            {
                var errResult = new StatusCodeResult(OwHelper.GetLastError()) { };
                return errResult;
            }
            _DbContext.SaveChanges();
            return result;
        }
        
        #endregion 币种相关

        #region 箱型相关
        /// <summary>
        /// 获取箱型。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">支持通用查询条件。</param>
        /// 
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定类别Id无效。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllShippingContainersKindReturnDto> GetAllShippingContainersKind([FromQuery] PagingParamsDtoBase model, [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllShippingContainersKindReturnDto();
            var dbSet = _DbContext.DD_ShippingContainersKinds;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            if (_AccountManager.IsAdmin(context.User))  //若是超管
                coll = coll.Where(c => c.OrgId == null);
            else
            {
                if (!_MerchantManager.GetIdByUserId(context.User.Id, out var merchId)) return BadRequest("未知的商户Id");
                if (context.User.OrgId is null) //若没有指定机构
                {
                    coll = coll.Where(c => c.OrgId == merchId);
                }
                else
                {
                    coll = coll.Where(c => c.OrgId == context.User.OrgId);
                }
            }

            // 使用EfHelper.GenerateWhereAnd进行通用查询条件处理
            coll = EfHelper.GenerateWhereAnd(coll, conditional);

            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 增加箱型记录。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">参数错误。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPost]
        public ActionResult<AddShippingContainersKindReturnDto> AddShippingContainersKind(AddShippingContainersKindParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            if (!_AuthorizationManager.Demand(out err, "B.9")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new AddShippingContainersKindReturnDto();
            model.Item.GenerateNewId();
            var id = model.Item.Id;
            _DbContext.DD_ShippingContainersKinds.Add(model.Item);
            _DbContext.SaveChanges();
            result.Id = id;
            return result;
        }

        /// <summary>
        /// 修改箱型记录。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定实体的Id不存在。通常这是Bug.在极端情况下可能是并发问题。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPut]
        public ActionResult<ModifyShippingContainersKindReturnDto> ModifyShippingContainersKind(ModifyShippingContainersKindParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            if (!_AuthorizationManager.Demand(out err, "B.9")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new ModifyShippingContainersKindReturnDto();
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
        /// 删除箱型的记录。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定实体的Id不存在。通常这是Bug.在极端情况下可能是并发问题。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpDelete]
        public ActionResult<RemoveShippingContainersKindReturnDto> RemoveShippingContainersKind(RemoveShippingContainersKindParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            if (!_AuthorizationManager.Demand(out err, "B.9")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new RemoveShippingContainersKindReturnDto();
            var id = model.Id;
            var dbSet = _DbContext.DD_ShippingContainersKinds;
            var item = dbSet.Find(id);
            if (item is null) return BadRequest();
            _EntityManager.Remove(item);
            _DbContext.SaveChanges();
            //if (item.DataDicType == 1) //若是简单字典
            //    _DbContext.Database.ExecuteSqlRaw($"delete from {nameof(_DbContext.SimpleDataDics)} where {nameof(SimpleDataDic.DataDicId)}='{id.ToString()}'");
            //else //其他字典待定
            //{

            //}
            return result;
        }

        /// <summary>
        /// 恢复指定的被删除箱型记录。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定实体的Id不存在。通常这是Bug.在极端情况下可能是并发问题。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPost]
        public ActionResult<RestoreShippingContainersKindReturnDto> RestoreShippingContainersKind(RestoreShippingContainersKindParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            if (!_AuthorizationManager.Demand(out err, "B.9")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new RestoreShippingContainersKindReturnDto();
            if (!_EntityManager.Restore<ShippingContainersKind>(model.Id))
            {
                var errResult = new StatusCodeResult(OwHelper.GetLastError()) { };
                return errResult;
            }
            _DbContext.SaveChanges();
            return result;
        }

        #endregion 箱型相关

        #region 日志相关
        /// <summary>
        /// 获取系统日志。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">已支持通用查询——除个别涉及敏感信息字段外，所有实体字段都可作为条件。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllSystemLogReturnDto> GetAllSystemLog([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllSystemLogReturnDto();

            var dbSet = _DbContext.OwSystemLogs;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            coll = EfHelper.GenerateWhereAnd(coll, conditional);
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        #endregion 日志相关

    }

    /// <summary>
    /// 恢复指定的简单数据字典的功能参数封装类。
    /// </summary>
    public class RestoreSimpleDataDicParamsDto : RestoreParamsDtoBase
    {
    }

    /// <summary>
    /// 恢复指定的简单数据字典的功能返回值封装类。
    /// </summary>
    public class RestoreSimpleDataDicReturnDto : RestoreReturnDtoBase
    {
    }

    /// <summary>
    /// 恢复指定的被删除港口字典的功能参数封装类。
    /// </summary>
    public class RestorePlPortParamsDto : RestoreParamsDtoBase
    {
    }

    /// <summary>
    /// 恢复指定的被删除港口字典的功能返回值封装类。
    /// </summary>
    public class RestorePlPortReturnDto : RestoreReturnDtoBase
    {
    }

    /// <summary>
    /// 恢复航线对象功能的参数封装类。
    /// </summary>
    public class RestorePlCargoRouteParamsDto : RestoreParamsDtoBase
    {
    }

    /// <summary>
    /// 恢复航线对象功能的返回值封装类。
    /// </summary>
    public class RestorePlCargoRouteReturnDto : RestoreReturnDtoBase
    {
    }

    /// <summary>
    /// 删除航线字典中的一项的功能参数封装类。
    /// </summary>
    public class RemoveCargoPlRouteParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 删除航线字典中的一项的功能返回值封装类。
    /// </summary>
    public class RemovePlCargoRouteReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 修改航线数据字典项的功能参数封装类。
    /// </summary>
    public class ModifyPlCargoRouteParamsDto : ModifyParamsDtoBase<PlCargoRoute>
    {
    }

    /// <summary>
    /// 修改航线数据字典项的功能返回值封装类。
    /// </summary>
    public class ModifyPlCargoRouteReturnDto : ModifyReturnDtoBase
    {
    }

    /// <summary>
    /// 增加一个航线数据字典的功能参数封装类。
    /// </summary>
    public class AddPlCargoRouteParamsDto : AddParamsDtoBase<PlCargoRoute>
    {
    }

    /// <summary>
    /// 增加一个航线数据字典的功能返回值封装类。
    /// </summary>
    public class AddPlCargoRouteReturnDto : AddReturnDtoBase
    {
    }

    /// <summary>
    /// 获取航线功能的返回值封装类。
    /// </summary>
    public class GetAllPlCargoRouteReturnDto : PagingReturnDtoBase<PlCargoRoute>
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public GetAllPlCargoRouteReturnDto()
        {

        }
    }

    /// <summary>
    /// 删除港口字典中的一项的功能参数封装类。
    /// </summary>
    public class RemovePlPortParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 删除港口字典中的一项的功能返回值封装类。
    /// </summary>
    public class RemovePlPortReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 修改港口数据字典项的功能参数封装类。
    /// </summary>
    public class ModifyPlPortParamsDto : ModifyParamsDtoBase<PlPort>
    {
    }

    /// <summary>
    /// 修改港口数据字典项的功能返回值封装类。
    /// </summary>
    public class ModifyPlPortReturnDto : ModifyReturnDtoBase
    {
    }

    /// <summary>
    /// 增加港口功能的参数封装类。
    /// </summary>
    public class AddPlPortParamsDto : AddParamsDtoBase<PlPort>
    {
    }

    /// <summary>
    /// 增加港口功能的返回值封装类。
    /// </summary>
    public class AddPlPortReturnDto : AddReturnDtoBase
    {
    }

    /// <summary>
    /// 用数据字典目录码获取所有字典项功能的参数封装类。
    /// </summary>
    public class GetDataDicByCatalogCodeParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 简单字典目录的Code。
        /// </summary>
        public string SimpleDicCatalogCode { get; set; }
    }

    /// <summary>
    /// 用数据字典目录码获取所有字典项功能的返回值封装类。
    /// </summary>
    public class GetDataDicByCatalogCodeReturnDto : PagingReturnDtoBase<SimpleDataDic>
    {
    }


    /// <summary>
    /// 获取港口功能的返回值封装类。
    /// </summary>
    public class GetAllPortReturnDto : PagingReturnDtoBase<PlPort>
    {
    }

    /// <summary>
    /// 获取所有业务大类的数据的功能返回值封装类.
    /// </summary>
    public class GetAllBusinessTypeReturnDto : PagingReturnDtoBase<BusinessTypeDataDic>
    {
    }


}
