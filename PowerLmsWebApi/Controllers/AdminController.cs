using Microsoft.AspNetCore.Mvc;
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

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// 管理员功能控制器。
    /// </summary>
    public class AdminController : PlControllerBase
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
        public AdminController(PowerLmsUserDbContext context, NpoiManager npoiManager, AccountManager accountManager, IServiceProvider scope, EntityManager entityManager, IMapper mapper)
        {
            _DbContext = context;
            _NpoiManager = npoiManager;
            _AccountManager = accountManager;
            _ServiceProvider = scope;
            _EntityManager = entityManager;
            _Mapper = mapper;
        }

        readonly PowerLmsUserDbContext _DbContext;
        private readonly NpoiManager _NpoiManager;
        readonly AccountManager _AccountManager;
        readonly IServiceProvider _ServiceProvider;
        readonly EntityManager _EntityManager;
        readonly IMapper _Mapper;

        #region 字典目录

        /// <summary>
        /// 获取所有数据字典的目录列表。
        /// </summary>
        /// <param name="token">登录令牌。</param>
        /// <param name="startIndex">起始位置，从0开始。</param>
        /// <param name="count">最大返回数量。</param>
        /// <param name="conditional">支持 code displayname DataDicType 关键字。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllDataDicCatalogReturnDto> GetAllDataDicCatalog(Guid token, [Range(0, int.MaxValue, ErrorMessage = "必须大于或等于0.")] int startIndex,
            [Range(-1, int.MaxValue)] int count = -1, [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetAccountFromToken(token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllDataDicCatalogReturnDto();
            var coll = _DbContext.DD_DataDicCatalogs.AsNoTracking();

            foreach (var item in conditional)
                if (string.Equals(item.Key, "id", StringComparison.OrdinalIgnoreCase))
                {
                    if (OwConvert.TryToGuid(item.Value, out var id))
                        coll = coll.Where(c => c.Id == id);
                }
                else if (string.Equals(item.Key, "code", StringComparison.OrdinalIgnoreCase))
                {
                    coll = coll.Where(c => c.Code.Contains(item.Value));
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
        /// 增加一个数据字典(目录)。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定的Code已经存在。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpPost]
        public ActionResult<AddDataDicCatalogReturnDto> AddDataDicCatalog(AddDataDicCatalogParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var ss = from tmp in _DbContext.DD_DataDicCatalogs
                     where tmp.OrgId == model.Item.OrgId && tmp.Code == model.Item.Code
                     select tmp;
            if (ss.FirstOrDefault(c => c.OrgId == model.Item.OrgId && c.Code == model.Item.Code) is not null)
            {
                return BadRequest();
            }
            var result = new AddDataDicCatalogReturnDto();
            model.Item.GenerateNewId();
            _DbContext.DD_DataDicCatalogs.Add(model.Item);
            _DbContext.SaveChanges();
            result.Id = model.Item.Id;
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
        [HttpPut]
        public ActionResult<ModifyDataDicCatalogReturnDto> ModifyDataDicCatalog(ModifyDataDicCatalogParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyDataDicCatalogReturnDto();
            if (!_EntityManager.ModifyEntities(model.Items))
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
        [HttpDelete]
        public ActionResult<RemoveDataDicCatalogReturnDto> RemoveDataDicCatalog(RemoveDataDicCatalogParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemoveDataDicCatalogReturnDto();
            var id = model.Id;
            var item = _DbContext.DD_DataDicCatalogs.Find(id);
            if (item is null) return BadRequest();
            _DbContext.DD_DataDicCatalogs.Remove(item);
            _DbContext.SaveChanges();
            return result;
        }

        #endregion 字典目录

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
        [HttpPost]
        [ProducesResponseType(typeof(ImportDataDicReturnDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public ActionResult<ImportDataDicReturnDto> ImportDataDic(IFormFile formFile, Guid token, Guid rId)
        {
            if (_AccountManager.GetAccountFromToken(token, _ServiceProvider) is not OwContext context) return Unauthorized(OwHelper.GetLastErrorMessage());
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
            if (_AccountManager.GetAccountFromToken(token, _ServiceProvider) is not OwContext context) return Unauthorized(OwHelper.GetLastErrorMessage());
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
            if (_AccountManager.GetAccountFromToken(token, _ServiceProvider) is not OwContext context) return Unauthorized(OwHelper.GetLastErrorMessage());
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
        /// <param name="token">登录令牌。</param>
        /// <param name="catalogId">数据字典类别的Id。</param>
        /// <param name="startIndex">起始位置，从0开始。</param>
        /// <param name="count">最大返回数量。-1表示全返回。</param>
        /// <param name="conditional">查询的条件。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定类别Id无效。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllDataDicReturnDto> GetAllDataDic(Guid token, Guid catalogId,
            [Range(0, int.MaxValue, ErrorMessage = "必须大于或等于0.")] int startIndex, [FromQuery][Range(-1, int.MaxValue)] int count = -1,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetAccountFromToken(token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllDataDicReturnDto();
            var coll = _DbContext.DD_SimpleDataDics.Where(c => c.DataDicId == catalogId).AsNoTracking();
            foreach (var item in conditional)
                if (string.Equals(item.Key, "id", StringComparison.OrdinalIgnoreCase))
                {
                    if (OwConvert.TryToGuid(item.Value, out var id))
                        coll = coll.Where(c => c.Id == id);
                }
                else if (string.Equals(item.Key, "code", StringComparison.OrdinalIgnoreCase))
                {
                    coll = coll.Where(c => c.Code == item.Value);
                }
                else if (string.Equals(item.Key, "displayname", StringComparison.OrdinalIgnoreCase))
                {
                    coll = coll.Where(c => c.DisplayName.Contains(item.Value));
                }
                else if (string.Equals(item.Key, "ShortcutName", StringComparison.OrdinalIgnoreCase))
                {
                    coll = coll.Where(c => c.ShortcutName.Contains(item.Value));
                }
            var prb = _EntityManager.GetAll(coll, startIndex, count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 给指定简单数据字典增加一项。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">在同一类别同一组织机构下指定了重复的Code。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpPost]
        public ActionResult<AddSimpleDataDicReturnDto> AddSimpleDataDic(AddSimpleDataDicParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
        [HttpPut]
        public ActionResult<ModifySimpleDataDicReturnDto> ModifySimpleDataDic(ModifySimpleDataDicParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifySimpleDataDicReturnDto();
            if (!_EntityManager.Modify(model.Items))
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
        [HttpDelete]
        public ActionResult<RemoveSimpleDataDicReturnDto> RemoveSimpleDataDic(RemoveSimpleDataDicParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemoveSimpleDataDicReturnDto();
            var id = model.Id;
            DbSet<SimpleDataDic> dbSet = _DbContext.DD_SimpleDataDics;
            var item = dbSet.Find(id);
            if (item is null) return BadRequest();
            item.IsDelete = true;
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
        [HttpPost]
        public ActionResult<RestoreSimpleDataDicReturnDto> RestoreSimpleDataDic(RestoreSimpleDataDicParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
            if (_AccountManager.GetAccountFromToken(token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
        /// <param name="token">登录令牌。</param>
        /// <param name="orgId">所属组织机构Id。null表示顶层系统。</param>
        /// <param name="startIndex">起始位置，从0开始。</param>
        /// <param name="count">最大返回数量。-1表示全返回。</param>
        /// <param name="conditional">查询的条件。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定类别Id无效。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllPortReturnDto> GetAllPlPort(Guid token, Guid? orgId, [Range(0, int.MaxValue, ErrorMessage = "必须大于或等于0.")] int startIndex,
            [FromQuery][Range(-1, int.MaxValue)] int count = -1, [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetAccountFromToken(token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllPortReturnDto();
            var coll = _DbContext.DD_PlPorts.AsNoTracking().Where(c => c.OrgId == orgId);
            foreach (var item in conditional)
                if (string.Equals(item.Key, "id", StringComparison.OrdinalIgnoreCase))
                {
                    if (OwConvert.TryToGuid(item.Value, out var id))
                        coll = coll.Where(c => c.Id == id);
                }
                else if (string.Equals(item.Key, "code", StringComparison.OrdinalIgnoreCase))
                {
                    coll = coll.Where(c => c.Code == item.Value);
                }
                else if (string.Equals(item.Key, "displayname", StringComparison.OrdinalIgnoreCase))
                {
                    coll = coll.Where(c => c.DisplayName.Contains(item.Value));
                }
                else if (string.Equals(item.Key, "ShortcutName", StringComparison.OrdinalIgnoreCase))
                {
                    coll = coll.Where(c => c.ShortcutName.Contains(item.Value));
                }
            var prb = _EntityManager.GetAll(coll, startIndex, count);
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
        [HttpPost]
        public ActionResult<AddPlPortReturnDto> AddPlPort(AddPlPortParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
        [HttpPut]
        public ActionResult<ModifyPlPortReturnDto> ModifyPlPort(ModifyPlPortParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyPlPortReturnDto();
            if (!_EntityManager.Modify(model.Items))
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
        [HttpDelete]
        public ActionResult<RemovePlPortReturnDto> RemovePlPort(RemovePlPortParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemovePlPortReturnDto();
            var id = model.Id;
            var dbSet = _DbContext.DD_PlPorts;
            var item = dbSet.Find(id);
            if (item is null) return BadRequest();
            item.IsDelete = true;
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
        [HttpPost]
        public ActionResult<RestorePlPortReturnDto> RestorePlPort(RestorePlPortParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
        /// <param name="token">登录令牌。</param>
        /// <param name="orgId">所属组织机构Id。null表示顶层系统。</param>
        /// <param name="startIndex">起始位置，从0开始。</param>
        /// <param name="count">最大返回数量。-1表示全返回。</param>
        /// <param name="conditional">查询的条件。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定类别Id无效。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllPlCargoRouteReturnDto> GetAllPlCargoRoute(Guid token, Guid? orgId, [Range(0, int.MaxValue, ErrorMessage = "必须大于或等于0.")] int startIndex,
            [FromQuery][Range(-1, int.MaxValue)] int count = -1, [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetAccountFromToken(token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllPlCargoRouteReturnDto();
            var coll = _DbContext.DD_PlCargoRoutes.AsNoTracking().Where(c => c.OrgId == orgId);
            foreach (var item in conditional)
                if (string.Equals(item.Key, "id", StringComparison.OrdinalIgnoreCase))
                {
                    if (OwConvert.TryToGuid(item.Value, out var id))
                        coll = coll.Where(c => c.Id == id);
                }
                else if (string.Equals(item.Key, "Id", StringComparison.OrdinalIgnoreCase))
                {
                    if (Guid.TryParse(item.Value, out var id))
                        coll = coll.Where(c => c.Id == id);
                }
                else if (string.Equals(item.Key, "code", StringComparison.OrdinalIgnoreCase))
                {
                    coll = coll.Where(c => c.Code == item.Value);
                }
                else if (string.Equals(item.Key, "displayname", StringComparison.OrdinalIgnoreCase))
                {
                    coll = coll.Where(c => c.DisplayName.Contains(item.Value));
                }
                else if (string.Equals(item.Key, "ShortcutName", StringComparison.OrdinalIgnoreCase))
                {
                    coll = coll.Where(c => c.ShortcutName.Contains(item.Value));
                }
            var prb = _EntityManager.GetAll(coll, startIndex, count);
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
        [HttpPost]
        public ActionResult<AddPlCargoRouteReturnDto> AddPlCargoRoute(AddPlCargoRouteParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
        [HttpPut]
        public ActionResult<ModifyPlCargoRouteReturnDto> ModifyPlCargoRoute(ModifyPlCargoRouteParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyPlCargoRouteReturnDto();
            if (!_EntityManager.Modify(model.Items))
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
        [HttpDelete]
        public ActionResult<RemovePlCargoRouteReturnDto> RemovePlCargoRoute(RemoveCargoPlRouteParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemovePlCargoRouteReturnDto();
            var id = model.Id;
            var dbSet = _DbContext.DD_PlCargoRoutes;
            var item = dbSet.Find(id);
            if (item is null) return BadRequest();
            item.IsDelete = true;
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
        [HttpPost]
        public ActionResult<RestorePlCargoRouteReturnDto> RestorePlCargoRoute(RestorePlCargoRouteParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
        /// <param name="token">登录令牌。</param>
        /// <param name="orgId">组织机构的Id。</param>
        /// <param name="startIndex">起始位置，从0开始。</param>
        /// <param name="count">最大返回数量。-1表示全返回。</param>
        /// <param name="conditional">查询的条件。支持Id，BeginDate，EndData三个字段</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定类别Id无效。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllPlExchangeRateReturnDto> GetAllPlExchangeRate(Guid token, Guid? orgId,
            [Range(0, int.MaxValue, ErrorMessage = "必须大于或等于0.")] int startIndex, [FromQuery][Range(-1, int.MaxValue)] int count = -1,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetAccountFromToken(token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllPlExchangeRateReturnDto();
            var coll = _DbContext.DD_PlExchangeRates.AsNoTracking().Where(c => c.OrgId == orgId);
            foreach (var item in conditional)
                if (string.Equals(item.Key, "id", StringComparison.OrdinalIgnoreCase))
                {
                    if (OwConvert.TryToGuid(item.Value, out var id))
                        coll = coll.Where(c => c.Id == id);
                }
                else if (string.Equals(item.Key, nameof(PlExchangeRate.BeginDate), StringComparison.OrdinalIgnoreCase) && OwConvert.TryGetDateTime(item.Value, out var bdt))
                {
                    coll = coll.Where(c => c.BeginDate >= bdt);
                }
                else if (string.Equals(item.Key, nameof(PlExchangeRate.EndData), StringComparison.OrdinalIgnoreCase) && OwConvert.TryGetDateTime(item.Value, out var edt))
                {
                    coll = coll.Where(c => c.EndData <= edt);
                }
                else if (string.Equals(item.Key, "ShortcutName", StringComparison.OrdinalIgnoreCase))
                {
                    coll = coll.Where(c => c.ShortcutName.Contains(item.Value));
                }
            var prb = _EntityManager.GetAll(coll, startIndex, count);
            _Mapper.Map(prb, result);
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
        [HttpPost]
        public ActionResult<AddPlExchangeRateReturnDto> AddPlExchangeRate(AddPlExchangeRateParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
        [HttpPut]
        public ActionResult<ModifyPlExchangeRateReturnDto> ModifyPlExchangeRate(ModifyPlExchangeRateParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyPlExchangeRateReturnDto();
            if (!_EntityManager.ModifyEntities(model.Items))
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
        /// <param name="token">登录令牌。</param>
        /// <param name="orgId">所属组织机构Id。null表示顶层系统。</param>
        /// <param name="startIndex">起始位置，从0开始。</param>
        /// <param name="count">最大返回数量。-1表示全返回。</param>
        /// <param name="conditional">查询的条件。支持basic 和 rim查询。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定类别Id无效。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllUnitConversionReturnDto> GetAllUnitConversion(Guid token, Guid? orgId,
            [Range(0, int.MaxValue, ErrorMessage = "必须大于或等于0.")] int startIndex, [FromQuery][Range(-1, int.MaxValue)] int count = -1,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetAccountFromToken(token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllUnitConversionReturnDto();
            var coll = _DbContext.DD_UnitConversions.AsNoTracking().Where(c => c.OrgId == orgId);
            foreach (var item in conditional)
                if (string.Equals(item.Key, "id", StringComparison.OrdinalIgnoreCase))
                {
                    if (OwConvert.TryToGuid(item.Value, out var id))
                        coll = coll.Where(c => c.Id == id);
                }
                else if (string.Equals(item.Key, "basic", StringComparison.OrdinalIgnoreCase))
                {
                    coll = coll.Where(c => c.Basic.Contains(item.Value));
                }
                else if (string.Equals(item.Key, "rim", StringComparison.OrdinalIgnoreCase))
                {
                    coll = coll.Where(c => c.Rim.Contains(item.Value));
                }
                else if (string.Equals(item.Key, "ShortcutName", StringComparison.OrdinalIgnoreCase))
                {
                    coll = coll.Where(c => c.ShortcutName.Contains(item.Value));
                }
            var prb = _EntityManager.GetAll(coll, startIndex, count);
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
        [HttpPost]
        public ActionResult<AddUnitConversionReturnDto> AddUnitConversion(AddUnitConversionParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
        [HttpPut]
        public ActionResult<ModifyUnitConversionReturnDto> ModifyUnitConversion(ModifyUnitConversionParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyUnitConversionReturnDto();
            if (!_EntityManager.Modify(model.Items))
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
        [HttpDelete]
        public ActionResult<RemoveUnitConversionReturnDto> RemoveUnitConversion(RemoveUnitConversionParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemoveUnitConversionReturnDto();
            var id = model.Id;
            var dbSet = _DbContext.DD_UnitConversions;
            var item = dbSet.Find(id);
            if (item is null) return BadRequest();
            //_DbContext.SimpleDataDics.Remove(item);
            item.IsDelete = true;
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
        [HttpPost]
        public ActionResult<RestoreUnitConversionReturnDto> RestoreUnitConversion(RestoreUnitConversionParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
        /// 获取费用种类。
        /// </summary>
        /// <param name="token">登录令牌。</param>
        /// <param name="orgId">所属组织机构Id。null表示顶层系统。</param>
        /// <param name="startIndex">起始位置，从0开始。</param>
        /// <param name="count">最大返回数量。-1表示全返回。</param>
        /// <param name="conditional">查询的条件。支持 DisplayName 和 ShortName 查询。</param>
        /// 
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定类别Id无效。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllFeesTypeReturnDto> GetAllFeesType(Guid token, Guid? orgId, [Range(0, int.MaxValue, ErrorMessage = "必须大于或等于0.")] int startIndex,
            [FromQuery][Range(-1, int.MaxValue)] int count = -1, [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetAccountFromToken(token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllFeesTypeReturnDto();
            var collBase = _DbContext.DD_FeesTypes.AsNoTracking().Where(c => c.OrgId == orgId);
            var coll = collBase;
            foreach (var item in conditional)
                if (string.Equals(item.Key, nameof(FeesType.Id), StringComparison.OrdinalIgnoreCase))
                {
                    if (OwConvert.TryToGuid(item.Value, out var id))
                        coll = coll.Where(c => c.Id == id);
                }
                else if (string.Equals(item.Key, nameof(FeesType.DisplayName), StringComparison.OrdinalIgnoreCase))
                {
                    coll = coll.Where(c => c.DisplayName.Contains(item.Value));
                }
                else if (string.Equals(item.Key, nameof(FeesType.ShortName), StringComparison.OrdinalIgnoreCase))
                {
                    coll = coll.Where(c => c.ShortcutName.Contains(item.Value));
                }
                else if (string.Equals(item.Key, "ShortcutName", StringComparison.OrdinalIgnoreCase))
                {
                    coll = coll.Where(c => c.ShortcutName.Contains(item.Value));
                }
            var prb = _EntityManager.GetAll(coll, startIndex, count);
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
        [HttpPost]
        public ActionResult<AddFeesTypeReturnDto> AddFeesType(AddFeesTypeParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new AddFeesTypeReturnDto();
            model.Item.GenerateNewId();
            var id = model.Item.Id;
            _DbContext.DD_FeesTypes.Add(model.Item);
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
        [HttpPut]
        public ActionResult<ModifyFeesTypeReturnDto> ModifyFeesType(ModifyFeesTypeParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyFeesTypeReturnDto();
            if (!_EntityManager.Modify(model.Items))
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
        [HttpDelete]
        public ActionResult<RemoveFeesTypeReturnDto> RemoveFeesType(RemoveFeesTypeParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemoveFeesTypeReturnDto();
            var id = model.Id;
            var dbSet = _DbContext.DD_FeesTypes;
            var item = dbSet.Find(id);
            if (item is null) return BadRequest();
            //_DbContext.SimpleDataDics.Remove(item);
            item.IsDelete = true;
            _DbContext.SaveChanges();
            //if (item.DataDicType == 1) //若是简单字典
            //    _DbContext.Database.ExecuteSqlRaw($"delete from {nameof(_DbContext.SimpleDataDics)} where {nameof(SimpleDataDic.DataDicId)}='{id.ToString()}'");
            //else //其他字典待定
            //{

            //}
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
        [HttpPost]
        public ActionResult<RestoreFeesTypeReturnDto> RestoreFeesType(RestoreFeesTypeParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
        /// <param name="token">登录令牌。</param>
        /// <param name="orgId">所属组织机构Id。null表示顶层系统。</param>
        /// <param name="startIndex">起始位置，从0开始。</param>
        /// <param name="count">最大返回数量。-1表示全返回。</param>
        /// <param name="conditional">查询的条件。支持 DisplayName 和 ShortName 查询。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定类别Id无效。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllJobNumberRuleReturnDto> GetAllJobNumberRule(Guid token, Guid? orgId, [Range(0, int.MaxValue, ErrorMessage = "必须大于或等于0.")] int startIndex,
            [FromQuery][Range(-1, int.MaxValue)] int count = -1, [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetAccountFromToken(token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllJobNumberRuleReturnDto();
            var coll = _DbContext.DD_JobNumberRules.AsNoTracking().Where(c => c.OrgId == orgId);
            foreach (var item in conditional)
                if (string.Equals(item.Key, nameof(JobNumberRule.Id), StringComparison.OrdinalIgnoreCase))
                {
                    if (OwConvert.TryToGuid(item.Value, out var id))
                        coll = coll.Where(c => c.Id == id);
                }
                else if (string.Equals(item.Key, nameof(JobNumberRule.DisplayName), StringComparison.OrdinalIgnoreCase))
                {
                    coll = coll.Where(c => c.DisplayName.Contains(item.Value));
                }
                else if (string.Equals(item.Key, nameof(JobNumberRule.ShortName), StringComparison.OrdinalIgnoreCase))
                {
                    coll = coll.Where(c => c.ShortcutName.Contains(item.Value));
                }
                else if (string.Equals(item.Key, "ShortcutName", StringComparison.OrdinalIgnoreCase))
                {
                    coll = coll.Where(c => c.ShortcutName.Contains(item.Value));
                }
            var prb = _EntityManager.GetAll(coll, startIndex, count);
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
        [HttpPost]
        public ActionResult<AddJobNumberRuleReturnDto> AddJobNumberRule(AddJobNumberRuleParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new AddJobNumberRuleReturnDto();
            model.Item.GenerateNewId();
            var id = model.Item.Id;
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
        [HttpPut]
        public ActionResult<ModifyJobNumberRuleReturnDto> ModifyJobNumberRule(ModifyJobNumberRuleParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyJobNumberRuleReturnDto();
            if (!_EntityManager.Modify(model.Items))
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
        /// 删除业务编码规则的记录。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定实体的Id不存在。通常这是Bug.在极端情况下可能是并发问题。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpDelete]
        public ActionResult<RemoveJobNumberRuleReturnDto> RemoveJobNumberRule(RemoveJobNumberRuleParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemoveJobNumberRuleReturnDto();
            var id = model.Id;
            var dbSet = _DbContext.DD_JobNumberRules;
            var item = dbSet.Find(id);
            if (item is null) return BadRequest();
            //_DbContext.SimpleDataDics.Remove(item);
            item.IsDelete = true;
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
        [HttpPost]
        public ActionResult<RestoreJobNumberRuleReturnDto> RestoreJobNumberRule(RestoreJobNumberRuleParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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

        #region 国家相关
        /// <summary>
        /// 获取国家。
        /// </summary>
        /// <param name="token">登录令牌。</param>
        /// <param name="orgId">所属组织机构Id。null表示顶层系统。</param>
        /// <param name="startIndex">起始位置，从0开始。</param>
        /// <param name="count">最大返回数量。-1表示全返回。</param>
        /// <param name="conditional">查询的条件。支持 DisplayName 和 ShortName 查询。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定类别Id无效。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllPlCountryReturnDto> GetAllPlCountry(Guid token, Guid? orgId, [Range(0, int.MaxValue, ErrorMessage = "必须大于或等于0.")] int startIndex,
            [FromQuery][Range(-1, int.MaxValue)] int count = -1, [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetAccountFromToken(token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllPlCountryReturnDto();
            var coll = _DbContext.DD_PlCountrys.AsNoTracking().Where(c => c.OrgId == orgId);
            foreach (var item in conditional)
                if (string.Equals(item.Key, nameof(PlCountry.Id), StringComparison.OrdinalIgnoreCase))
                {
                    if (OwConvert.TryToGuid(item.Value, out var id))
                        coll = coll.Where(c => c.Id == id);
                }
                else if (string.Equals(item.Key, nameof(PlCountry.DisplayName), StringComparison.OrdinalIgnoreCase))
                {
                    coll = coll.Where(c => c.DisplayName.Contains(item.Value));
                }
                else if (string.Equals(item.Key, nameof(PlCountry.ShortName), StringComparison.OrdinalIgnoreCase))
                {
                    coll = coll.Where(c => c.ShortcutName.Contains(item.Value));
                }
                else if (string.Equals(item.Key, "ShortcutName", StringComparison.OrdinalIgnoreCase))
                {
                    coll = coll.Where(c => c.ShortcutName.Contains(item.Value));
                }
            var prb = _EntityManager.GetAll(coll, startIndex, count);
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
        [HttpPost]
        public ActionResult<AddPlCountryReturnDto> AddPlCountry(AddPlCountryParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
        [HttpPut]
        public ActionResult<ModifyPlCountryReturnDto> ModifyPlCountry(ModifyPlCountryParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyPlCountryReturnDto();
            if (!_EntityManager.Modify(model.Items))
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
        [HttpDelete]
        public ActionResult<RemovePlCountryReturnDto> RemovePlCountry(RemovePlCountryParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemovePlCountryReturnDto();
            var id = model.Id;
            var dbSet = _DbContext.DD_PlCountrys;
            var item = dbSet.Find(id);
            if (item is null) return BadRequest();
            //_DbContext.SimpleDataDics.Remove(item);
            item.IsDelete = true;
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
        [HttpPost]
        public ActionResult<RestorePlCountryReturnDto> RestorePlCountry(RestorePlCountryParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
        /// <param name="token">登录令牌。</param>
        /// <param name="orgId">所属组织机构Id。null表示顶层系统。</param>
        /// <param name="startIndex">起始位置，从0开始。</param>
        /// <param name="count">最大返回数量。-1表示全返回。</param>
        /// <param name="conditional">查询的条件。支持 DisplayName 和 ShortName 查询。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定类别Id无效。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllPlCurrencyReturnDto> GetAllPlCurrency(Guid token, Guid? orgId, [Range(0, int.MaxValue, ErrorMessage = "必须大于或等于0.")] int startIndex,
            [FromQuery][Range(-1, int.MaxValue)] int count = -1, [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetAccountFromToken(token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllPlCurrencyReturnDto();
            var coll = _DbContext.DD_PlCurrencys.AsNoTracking().Where(c => c.OrgId == orgId);
            foreach (var item in conditional)
                if (string.Equals(item.Key, nameof(PlCurrency.Id), StringComparison.OrdinalIgnoreCase))
                {
                    if (OwConvert.TryToGuid(item.Value, out var id))
                        coll = coll.Where(c => c.Id == id);
                }
                else if (string.Equals(item.Key, nameof(PlCurrency.DisplayName), StringComparison.OrdinalIgnoreCase))
                {
                    coll = coll.Where(c => c.DisplayName.Contains(item.Value));
                }
                else if (string.Equals(item.Key, nameof(PlCurrency.ShortName), StringComparison.OrdinalIgnoreCase))
                {
                    coll = coll.Where(c => c.ShortcutName.Contains(item.Value));
                }
                else if (string.Equals(item.Key, "ShortcutName", StringComparison.OrdinalIgnoreCase))
                {
                    coll = coll.Where(c => c.ShortcutName.Contains(item.Value));
                }
            var prb = _EntityManager.GetAll(coll, startIndex, count);
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
        [HttpPost]
        public ActionResult<AddPlCurrencyReturnDto> AddPlCurrency(AddPlCurrencyParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
        [HttpPut]
        public ActionResult<ModifyPlCurrencyReturnDto> ModifyPlCurrency(ModifyPlCurrencyParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyPlCurrencyReturnDto();
            if (!_EntityManager.Modify(model.Items))
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
        [HttpDelete]
        public ActionResult<RemovePlCurrencyReturnDto> RemovePlCurrency(RemovePlCurrencyParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemovePlCurrencyReturnDto();
            var id = model.Id;
            var dbSet = _DbContext.DD_PlCurrencys;
            var item = dbSet.Find(id);
            if (item is null) return BadRequest();
            //_DbContext.SimpleDataDics.Remove(item);
            item.IsDelete = true;
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
        [HttpPost]
        public ActionResult<RestorePlCurrencyReturnDto> RestorePlCurrency(RestorePlCurrencyParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
    }

    /// <summary>
    /// 恢复币种条目参数封装类。
    /// </summary>
    public class RestorePlCurrencyParamsDto : RestoreParamsDtoBase
    {
    }

    /// <summary>
    /// 恢复币种条目返回值封装类。
    /// </summary>
    public class RestorePlCurrencyReturnDto : RestoreReturnDtoBase
    {
    }

    /// <summary>
    /// 删除币种条目参数封装类。
    /// </summary>
    public class RemovePlCurrencyParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 删除币种条目返回值封装类。
    /// </summary>
    public class RemovePlCurrencyReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 修改币种条目参数封装类。
    /// </summary>
    public class ModifyPlCurrencyParamsDto : ModifyParamsDtoBase<PlCurrency>
    {
    }

    /// <summary>
    /// 修改币种条目返回值封装类。
    /// </summary>
    public class ModifyPlCurrencyReturnDto : ModifyReturnDtoBase
    {
    }

    /// <summary>
    /// 增加币种条目参数封装类。
    /// </summary>
    public class AddPlCurrencyParamsDto : AddParamsDtoBase<PlCurrency>
    {
    }

    /// <summary>
    /// 增加币种条目返回值封装类。
    /// </summary>
    public class AddPlCurrencyReturnDto : AddReturnDtoBase
    {
    }

    /// <summary>
    /// 获取币种条目返回值封装类。
    /// </summary>
    public class GetAllPlCurrencyReturnDto : PagingReturnDtoBase<PlCurrency>
    {
    }

    /// <summary>
    /// 恢复国家条目参数封装类。
    /// </summary>
    public class RestorePlCountryParamsDto : RestoreParamsDtoBase
    {
    }

    /// <summary>
    /// 恢复国家条目返回值封装类。
    /// </summary>
    public class RestorePlCountryReturnDto : RestoreReturnDtoBase
    {
    }

    /// <summary>
    /// 删除国家条目参数封装类。
    /// </summary>
    public class RemovePlCountryParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 删除国家条目返回值封装类。
    /// </summary>
    public class RemovePlCountryReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 修改国家条目参数封装类。
    /// </summary>
    public class ModifyPlCountryParamsDto : ModifyParamsDtoBase<PlCountry>
    {
    }

    /// <summary>
    /// 修改国家条目返回值封装类。
    /// </summary>
    public class ModifyPlCountryReturnDto : ModifyReturnDtoBase
    {
    }

    /// <summary>
    /// 增加国家条目参数封装类。
    /// </summary>
    public class AddPlCountryParamsDto : AddParamsDtoBase<PlCountry>
    {
    }

    /// <summary>
    /// 增加国家条目返回值封装类。
    /// </summary>
    public class AddPlCountryReturnDto : AddReturnDtoBase
    {
    }

    /// <summary>
    /// 获取国家条目返回值封装类。
    /// </summary>
    public class GetAllPlCountryReturnDto : PagingReturnDtoBase<PlCountry>
    {
    }

    /// <summary>
    /// 恢复指定的被删除业务编码规则记录的功能参数封装类。
    /// </summary>
    public class RestoreJobNumberRuleParamsDto : RestoreParamsDtoBase
    {
    }

    /// <summary>
    /// 恢复指定的被删除业务编码规则记录的功能返回值封装类。
    /// </summary>
    public class RestoreJobNumberRuleReturnDto : RestoreReturnDtoBase
    {
    }

    /// <summary>
    /// 删除业务编码规则记录的功能参数封装类。
    /// </summary>
    public class RemoveJobNumberRuleParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 删除业务编码规则记录的功能返回值封装类。
    /// </summary>
    public class RemoveJobNumberRuleReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 修改业务编码规则记录的功能参数封装类。
    /// </summary>
    public class ModifyJobNumberRuleParamsDto : ModifyParamsDtoBase<JobNumberRule>
    {
    }

    /// <summary>
    /// 修改业务编码规则记录的功能返回值封装类。
    /// </summary>
    public class ModifyJobNumberRuleReturnDto : ModifyReturnDtoBase
    {
    }

    /// <summary>
    /// 增加业务编码规则记录的功能参数封装类。
    /// </summary>
    public class AddJobNumberRuleParamsDto : AddParamsDtoBase<JobNumberRule>
    {
    }

    /// <summary>
    ///增加业务编码规则记录的功能返回值封装类。
    /// </summary>
    public class AddJobNumberRuleReturnDto : AddReturnDtoBase
    {
    }

    /// <summary>
    /// 查询业务编码规则记录的功能返回值封装类。
    /// </summary>
    public class GetAllJobNumberRuleReturnDto : PagingReturnDtoBase<JobNumberRule>
    {
    }

    /// <summary>
    /// 恢复费用种类记录的功能参数封装类。
    /// </summary>
    public class RestoreFeesTypeParamsDto : RestoreParamsDtoBase
    {
    }

    /// <summary>
    /// 恢复费用种类记录的功能返回值封装类。
    /// </summary>
    public class RestoreFeesTypeReturnDto : RestoreReturnDtoBase
    {
    }

    /// <summary>
    /// 删除费用种类记录的功能参数封装类。
    /// </summary>
    public class RemoveFeesTypeParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 删除费用种类记录的功能返回值封装类。
    /// </summary>
    public class RemoveFeesTypeReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 修改费用种类记录的功能参数封装类。
    /// </summary>
    public class ModifyFeesTypeParamsDto : ModifyParamsDtoBase<FeesType>
    {
    }

    /// <summary>
    /// 修改费用种类记录的功能返回值封装类。
    /// </summary>
    public class ModifyFeesTypeReturnDto : ModifyReturnDtoBase
    {
    }

    /// <summary>
    /// 增加费用种类记录的功能参数封装类。
    /// </summary>
    public class AddFeesTypeParamsDto : AddParamsDtoBase<FeesType>
    {
    }

    /// <summary>
    /// 增加费用种类记录的功能返回值封装类。
    /// </summary>
    public class AddFeesTypeReturnDto : AddReturnDtoBase
    {
    }

    /// <summary>
    /// 获取费用种类的功能返回值封装类。
    /// </summary>
    public class GetAllFeesTypeReturnDto : PagingReturnDtoBase<FeesType>
    {
    }

    /// <summary>
    /// 恢复指定的被删除单位换算记录的功能参数封装类。
    /// </summary>
    public class RestoreUnitConversionParamsDto : RestoreParamsDtoBase
    {
    }

    /// <summary>
    /// 恢复指定的被删除单位换算记录的功能返回值封装类。
    /// </summary>
    public class RestoreUnitConversionReturnDto : RestoreReturnDtoBase
    {
    }

    /// <summary>
    /// 删除单位换算的记录的功能参数封装类。
    /// </summary>
    public class RemoveUnitConversionParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 删除单位换算的记录的功能返回值封装类。
    /// </summary>
    public class RemoveUnitConversionReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 修改单位换算记录的功能参数封装类。
    /// </summary>
    public class ModifyUnitConversionParamsDto : ModifyParamsDtoBase<UnitConversion>
    {
    }

    /// <summary>
    /// 修改单位换算记录的功能返回值封装类。
    /// </summary>
    public class ModifyUnitConversionReturnDto : ModifyReturnDtoBase
    {
    }

    /// <summary>
    /// 增加单位换算记录的功能参数封装类。
    /// </summary>
    public class AddUnitConversionParamsDto : AddParamsDtoBase<UnitConversion>
    {
    }

    /// <summary>
    /// 增加单位换算记录的功能返回值封装类。
    /// </summary>
    public class AddUnitConversionReturnDto : AddReturnDtoBase
    {
    }

    /// <summary>
    /// 获取单位换算的功能返回值封装类。
    /// </summary>
    public class GetAllUnitConversionReturnDto : PagingReturnDtoBase<UnitConversion>
    {
    }

    /// <summary>
    /// 修改汇率项的功能参数封装类。
    /// </summary>
    public class ModifyPlExchangeRateParamsDto : ModifyParamsDtoBase<PlExchangeRate>
    {
    }

    /// <summary>
    /// 修改汇率项的功能返回值封装类。
    /// </summary>
    public class ModifyPlExchangeRateReturnDto : ModifyReturnDtoBase
    {
    }

    /// <summary>
    /// 增加一个汇率记录的功能参数封装类。
    /// </summary>
    public class AddPlExchangeRateParamsDto : AddParamsDtoBase<PlExchangeRate>
    {
    }

    /// <summary>
    /// 增加一个汇率记录的功能返回值封装类。
    /// </summary>
    public class AddPlExchangeRateReturnDto : AddReturnDtoBase
    {
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
    /// 获取汇率功能的返回值封装类。
    /// </summary>
    public class GetAllPlExchangeRateReturnDto : PagingReturnDtoBase<PlExchangeRate>
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

    /// <summary>
    /// 获取指定类别数据字典的全部内容的功能返回值封装类。
    /// </summary>
    public class GetAllDataDicReturnDto : PagingReturnDtoBase<SimpleDataDic>
    {
    }

    /// <summary>
    /// 删除简单数据字典中的一项的功能参数封装类。
    /// </summary>
    public class RemoveSimpleDataDicParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 删除简单数据字典中的一项的功能返回值封装类。
    /// </summary>
    public class RemoveSimpleDataDicReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 修改简单字典的项的功能参数封装类。
    /// </summary>
    public class ModifySimpleDataDicParamsDto : ModifyParamsDtoBase<SimpleDataDic>
    {
    }

    /// <summary>
    /// 修改简单字典的项的功能返回值封装类。
    /// </summary>
    public class ModifySimpleDataDicReturnDto : ModifyReturnDtoBase
    {
    }

    /// <summary>
    /// 给指定简单数据字典增加一项的功能参数封装类，
    /// </summary>
    public class AddSimpleDataDicParamsDto : AddParamsDtoBase<SimpleDataDic>
    {
        /// <summary>
        /// 是否同步到子公司/组织机构。对于超管复制到所有字典中，对于商户管理员复制到商户所有字典中。
        /// </summary>
        public bool CopyToChildren { get; set; }
    }

    /// <summary>
    /// 给指定简单数据字典增加一项的功能返回值封装类。
    /// </summary>
    public class AddSimpleDataDicReturnDto : AddReturnDtoBase
    {
    }

    /// <summary>
    /// 删除一个数据字典目录功能的参数封装类。
    /// </summary>
    public class RemoveDataDicCatalogParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 删除一个数据字典目录功能的返回值封装类。
    /// </summary>
    public class RemoveDataDicCatalogReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 修改数据字典目录功能的参数封装类。
    /// </summary>
    public class ModifyDataDicCatalogParamsDto : ModifyParamsDtoBase<DataDicCatalog>
    {
    }

    /// <summary>
    /// 修改数据字典目录功能的返回值封装类。
    /// </summary>
    public class ModifyDataDicCatalogReturnDto : ModifyReturnDtoBase
    {
    }

    /// <summary>
    /// 增加数据字典目录功能的参数封装类。
    /// </summary>
    public class AddDataDicCatalogParamsDto : AddParamsDtoBase<DataDicCatalog>
    {
    }

    /// <summary>
    /// 增加数据字典目录功能的返回值封装类。
    /// </summary>
    public class AddDataDicCatalogReturnDto : AddReturnDtoBase
    {
    }

    /// <summary>
    /// 返回数据字典目录功能的返回值封装类。
    /// </summary>
    public class GetAllDataDicCatalogReturnDto : PagingReturnDtoBase<DataDicCatalog>
    {
    }
}
