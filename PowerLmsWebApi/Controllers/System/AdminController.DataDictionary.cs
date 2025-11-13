/*
 * PowerLms - 货运物流业务管理系统
 * 管理员控制器 - 数据字典管理部分
 * 
 * 功能说明：
 * - 数据字典目录管理（增删改查、复制到下级机构）
 * - 系统资源管理和元数据获取
 * - 数据字典导入导出（基于OwDataUnit + OwNpoiUnit高性能处理）
 * - 简单字典项的CRUD操作
 * - 支持多租户数据隔离和权限控制
 * 
 * 技术特点：
 * - 基于角色的权限验证
 * - 多租户OrgId数据隔离
 * - 高性能Excel数据处理
 * - 完整的错误处理和日志记录
 * - 支持复杂的机构层级复制逻辑
 * 
 * 作者：PowerLms开发团队
 * 创建时间：2024年
 * 最后修改：2024年 - Excel处理架构重构，使用OwDataUnit替代旧架构
 */

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using OwExtensions.NPOI; // 添加OwExtensions.NPOI命名空间
using PowerLms.Data;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Dto;
using System.Net;

namespace PowerLmsWebApi.Controllers.System
{
    /// <summary>
    /// 管理员功能控制器——数据字典部分
    /// </summary>
    public partial class AdminController : PlControllerBase
    {
        #region 数据字典管理
        #region 字典目录

        /// <summary>
        /// 获取所有的数据字典目录列表
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">支持通用查询条件</param>
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
            if (_AccountManager.IsAdmin(context.User))  //如果是超管
                coll = coll.Where(c => c.OrgId == null);
            else
            {
                var merchantId = _OrgManager.GetMerchantIdByUserId(context.User.Id);
                if (!merchantId.HasValue) return BadRequest("未知的商户Id");
                if (context.User.OrgId is null) //如果没有指定机构
                {
                    coll = coll.Where(c => c.OrgId == merchantId);
                }
                else
                {
                    coll = coll.Where(c => c.OrgId == context.User.OrgId);
                }
            }

            // 使用EfHelper.GenerateWhereAnd处理通用查询条件分析
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
        /// <response code="400">指定实体的Id不存在。通常这是Bug.在极端情况下是并发问题。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPut]
        public ActionResult<ModifyDataDicCatalogReturnDto> ModifyDataDicCatalog(ModifyDataDicCatalogParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            if (!_AuthorizationManager.Demand(out string err, "B.0")) return StatusCode((int)HttpStatusCode.Forbidden, err);
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
        /// 删除一个数据字典目录，并删除其数据。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定实体的Id不存在。通常这是Bug.在极端情况下是并发问题。</response>  
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

        #region 数据字典导入导出

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
            var baseCatalogs = _DbContext.DD_DataDicCatalogs.Where(c => c.OrgId == model.SrcOrgId).AsNoTracking();  //基础字典目录集合
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
        /// 获取系统资源列表
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
        /// 通用的导入数据字典。等同于批量导入。
        /// </summary>
        /// <param name="formFile"></param>
        /// <param name="token"></param>
        /// <param name="rId">系统资源列表中获取的指定资源的Id。例如:6AE3BBB3-BAC9-4509-BF82-C8578830CD24 。系统资源的Id是不会变化的。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPost]
        public ActionResult<ImportDataDicReturnDto> ImportDataDic(IFormFile formFile, Guid token, Guid rId)
        {
            if (_AccountManager.GetOrLoadContextByToken(token, _ServiceProvider) is not OwContext context) return Unauthorized(OwHelper.GetLastErrorMessage());
            string err;
            if (!_AuthorizationManager.Demand(out err, "B.0")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new ImportDataDicReturnDto();
            var srTask = _DbContext.DD_SystemResources.FindAsync(rId).AsTask();
            var workbook = WorkbookFactory.Create(formFile.OpenReadStream());
            var sheet = workbook.GetSheetAt(0);
            var sr = srTask.Result;
            switch (sr.Name)
            {
                case nameof(_DbContext.Multilinguals):
                    {
                        _DbContext.TruncateTable(nameof(_DbContext.Multilinguals));
                        // 使用 OwNpoiDataUnit.BulkInsertFromExcel 替代 OwDataUnit.BulkInsert
                        var count = OwNpoiDbUnit.BulkInsertFromExcel<Multilingual>(
                            sheet, _DbContext, ignoreExisting: true);
                        _Logger?.LogInformation("成功导入多语言数据：{count}条记录", count);
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
        /// 通用获取数据字典功能。
        /// </summary>
        /// <param name="token"></param>
        /// <param name="rId"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
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
                        OwNpoiUnit.WriteToExcel(_DbContext.Multilinguals.AsNoTracking(), typeof(Multilingual).GetProperties().Select(c => c.Name).ToArray(), sheet);
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
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
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
                        OwNpoiUnit.WriteToExcel(_DbContext.Multilinguals.Take(0), typeof(Multilingual).GetProperties().Select(c => c.Name).ToArray(), sheet);
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

        #endregion 数据字典导入导出

        #region 简单字典CRUD

        /// <summary>
        /// 获取指定类别的简单字典全部数据。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">支持通用查询条件。 </param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定的类Id无效。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllDataDicReturnDto> GetAllDataDic([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllDataDicReturnDto();
            var dbSet = _DbContext.DD_SimpleDataDics;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();

            // 使用EfHelper.GenerateWhereAnd处理通用查询条件分析
            coll = EfHelper.GenerateWhereAnd(coll, conditional);

            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 按数据字典目录获取简单字典——如果没有机构用户无法使用，如果有机构用户仍然可使用。
        /// 返回简约字典。
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
            if (_AccountManager.IsAdmin(context.User))  //如果是超管
                coll = coll.Where(c => c.OrgId == null);
            else
            {
                var merchantId = _OrgManager.GetMerchantIdByUserId(context.User.Id);
                if (!merchantId.HasValue) return BadRequest("未知的商户Id");
                if (context.User.OrgId is null) //如果没有指定机构
                {
                    coll = coll.Where(c => c.OrgId == merchantId);
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
        /// 增加一个数据字典(目录)，若有CopyToChildren值，可以在创建完后增加一个全局字典目录(OrgId为空)，商管创建一个商户级字典目录(OrgId为商户Id)。
        /// 只有当勾选了复制选项(CopyToChildren=true)时，才会将字典目录复制到公司客户下，超管可复制到所有公司客户，商管可复制到商戶下的所有公司客户，。
        /// 因为的机构是树状结构，可以循环多层的层级关系，具有复杂。
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

            if (!context.User.IsSuperAdmin) //不是超管
            {
                if (!_AuthorizationManager.Demand(out string err, "B.0")) return StatusCode((int)HttpStatusCode.Forbidden, err);
                if (!context.User.IsAdmin()) return StatusCode((int)HttpStatusCode.Forbidden, "需要管理员权限。");
            }

            // 根据用户身份确定正确的OrgId
            Guid? targetOrgId = null;
            if (context.User.IsSuperAdmin)
            {
                targetOrgId = null; // 超管使用全局目录
            }
            else if (context.User.IsMerchantAdmin)
            {
                var merchId = _OrgManager.GetMerchantIdByUserId(context.User.Id);
                if (!merchId.HasValue)
                    return BadRequest("无法获取商户ID");
                targetOrgId = merchId;
            }
            else if (context.User.OrgId.HasValue)
            {
                targetOrgId = context.User.OrgId;
            }

            // 检查同一机构是否已存在相同Code的目录
            if (_DbContext.DD_DataDicCatalogs.Any(c => c.OrgId == targetOrgId && c.Code == model.Item.Code))
                return BadRequest($"已存在相同Code的目录: {model.Item.Code}");

            // 创建数据字典目录，使用AutoMapper进行映射
            var mainCatalog = _Mapper.Map<DataDicCatalog>(model.Item);
            mainCatalog.OrgId = targetOrgId;
            mainCatalog.GenerateNewId();

            _DbContext.DD_DataDicCatalogs.Add(mainCatalog);

            // 只有勾选了复制选项时才复制到公司客户
            if (model.CopyToChildren)
            {
                try
                {
                    // 获取目标机构集合 - 使用 ToList() 确保查询执行
                    var targetOrgs = new List<PlOrganization>();

                    if (context.User.IsSuperAdmin)
                    {
                        // 超管：获取所有公司客户
                        targetOrgs = _DbContext.PlOrganizations
                            .Where(o => o.Otc == 2)
                            .AsNoTracking()
                            .ToList();
                    }
                    else if (context.User.IsMerchantAdmin && targetOrgId.HasValue)
                    {
                        // 商管：获取该商户下的所有公司客户
                        var merchantId = _OrgManager.GetMerchantIdByUserId(context.User.Id);
                        if (!merchantId.HasValue) return BadRequest("无法获取商户ID");

                        var dictOrgs = _OrgManager.GetOrLoadOrgCacheItem(merchantId.Value).Orgs;
                        var allowOrgObjs = dictOrgs.Values.ToArray();
                        if (allowOrgObjs != null)
                        {
                            targetOrgs = allowOrgObjs
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

                    // 遍历每个目标机构
                    foreach (var org in targetOrgs)
                    {
                        // 跳过已存在相同Code的机构
                        if (existingIdSet.Contains(org.Id))
                            continue;

                        // 创建新目录 - 只使用DataDicCatalog实体拥有的属性
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
                    // 记录错误并记录异常，但仍创建主目录
                    var log = new OwSystemLog
                    {
                        ExtraString = $"创建字典目录复制机构时出错: {ex.Message}",
                        ActionId = "DataDic.AddDataDicCatalog.CopyToChildren",
                        WorldDateTime = DateTime.Now,
                        OrgId = context.User.OrgId,
                    };

                    // 如果需要存储错误的堆栈跟踪，可以使用JsonObjectString字段
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
        /// 在指定的数据字典里增加一项
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">在同一类，同一机构下，指定了重复的Code。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPost]
        public ActionResult<AddSimpleDataDicReturnDto> AddSimpleDataDic(AddSimpleDataDicParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            if (!_AuthorizationManager.Demand(out err, "B.0")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new AddSimpleDataDicReturnDto();
            if (_DbContext.DD_SimpleDataDics.Any(c => c.DataDicId == model.Item.DataDicId && c.Code == model.Item.Code))   //如果重复
                return BadRequest("Id重复");
            if (model.Item.DataDicId is null)
                return BadRequest($"{nameof(model.Item.DataDicId)} 不能为空。");
            model.Item.GenerateNewId();
            var id = model.Item.Id;
            _DbContext.DD_SimpleDataDics.Add(model.Item);
            if (model.CopyToChildren)    //如果需要批量创建
            {
                if (_DbContext.DD_DataDicCatalogs.FirstOrDefault(c => c.Id == model.Item.DataDicId) is DataDicCatalog catalog)  //找到字典
                {
                    var allCatalog = _DbContext.DD_DataDicCatalogs.Where(c => c.Code == catalog.Code && c.Id != model.Item.DataDicId);  //排除主键的字典
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
        /// 修改简单字典项
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定实体的Id不存在。通常这是Bug.在极端情况下是并发问题。</response>  
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
        /// 删除简单数据字典中的一项
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

        #endregion 简单字典CRUD


        #endregion 数据字典管理
    }
}