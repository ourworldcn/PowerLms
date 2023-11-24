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

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// 管理员功能控制器。
    /// </summary>
    public class AdminController : OwControllerBase
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="npoiManager"></param>
        /// <param name="accountManager"></param>
        /// <param name="scope"></param>
        public AdminController(PowerLmsUserDbContext context, NpoiManager npoiManager, AccountManager accountManager, IServiceProvider scope)
        {
            _DbContext = context;
            _NpoiManager = npoiManager;
            _AccountManager = accountManager;
            _ServiceProvider = scope;
        }

        PowerLmsUserDbContext _DbContext;
        NpoiManager _NpoiManager;
        AccountManager _AccountManager;
        IServiceProvider _ServiceProvider;

        #region 字典目录

        /// <summary>
        /// 获取所有数据字典的目录列表。
        /// </summary>
        /// <param name="token">登录令牌。</param>
        /// <param name="startIndex">起始位置，从0开始。</param>
        /// <param name="count">最大返回数量。</param>
        /// <param name="conditional"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllDataDicCatalogReturnDto> GetAllDataDicCatalog(Guid token, [Range(0, int.MaxValue, ErrorMessage = "必须大于或等于0.")] int startIndex,
            [Range(-1, int.MaxValue)] int count = -1, [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetAccountFromToken(token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllDataDicCatalogReturnDto();
            var coll = _DbContext.DD_DataDicCatalogs.AsNoTracking().OrderBy(c => c.Id).Skip(startIndex);
            StringDictionary stringDictionary = new StringDictionary();

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
            if (count > -1)
                coll = coll.Take(count);
            result.Total = _DbContext.DD_DataDicCatalogs.Count();
            result.Result.AddRange(coll);
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
            var dbSet = _DbContext.DD_DataDicCatalogs;
            foreach (var item in model.Items)
            {
                var tmp = dbSet.Find(item.Id);
                if (tmp is null) { return BadRequest($"找不到{item.Id}"); }
                _DbContext.Entry(tmp).CurrentValues.SetValues(item);
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
            if (item.DataDicType == 1) //若是简单字典
                _DbContext.Database.ExecuteSqlRaw($"delete from {nameof(_DbContext.DD_SimpleDataDics)} where {nameof(SimpleDataDic.DataDicId)}='{id}'");
            else //其他字典待定
            {

            }
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
            var collBase = _DbContext.DD_SimpleDataDics.AsNoTracking().Where(c => c.DataDicId == catalogId);
            var coll = collBase.OrderBy(c => c.Id).Skip(startIndex);
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
            if (count > -1)
                coll = coll.Take(count);
            result.Total = collBase.Count();
            result.Result.AddRange(coll);
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
                return BadRequest();
            model.Item.GenerateNewId();
            var id = model.Item.Id;
            _DbContext.DD_SimpleDataDics.Add(model.Item);
            if (model.CopyToChildren)    //若需要向下传播
            {
                if (_DbContext.DD_DataDicCatalogs.FirstOrDefault(c => c.Id == model.Item.DataDicId) is DataDicCatalog catalog)  //若有字典
                {
                    var allCatalog = _DbContext.DD_DataDicCatalogs.Where(c => c.Code == catalog.Code);
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
            var dbSet = _DbContext.DD_SimpleDataDics;
            foreach (var item in model.Items)
            {
                var tmp = dbSet.Find(item.Id);
                if (tmp is null) { return BadRequest($"找不到{item.Id}"); }
                _DbContext.Entry(tmp).CurrentValues.SetValues(item);
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
            var catalogId = item.DataDicId;
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
            var collBase = _DbContext.DD_BusinessTypeDataDics.AsNoTracking();
            result.Result.AddRange(collBase);
            result.Total = collBase.Count();
            return result;
        }

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
