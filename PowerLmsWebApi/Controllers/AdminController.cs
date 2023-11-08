using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NPOI.HSSF.UserModel;
using NPOI.SS.Formula.Functions;
using NPOI.SS.UserModel;
using PowerLms.Data;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Dto;
using System.Net;
using System.Reflection;

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
        public AdminController(PowerLmsUserDbContext context, NpoiManager npoiManager)
        {
            _Context = context;
            _NpoiManager = npoiManager;
        }

        PowerLmsUserDbContext _Context;
        NpoiManager _NpoiManager;

        /// <summary>
        /// 获取系统资源列表。
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult<GetSystemResourceReturnDto> GetSystemResource()
        {
            var result = new GetSystemResourceReturnDto();
            result.Resources.AddRange(_Context.SystemResources);
            return result;
        }

        /// <summary>
        /// 通用的获取数据字典。
        /// </summary>
        /// <param name="token"></param>
        /// <param name="rId">从资源列表中获取，指定资源的Id。如:6AE3BBB3-BAC9-4509-BF82-C8578830CD24 表示 多语言资源表。Id是不会变化的。</param>
        /// <param name="startIndex">获取的起始索引。可用于分页。</param>
        /// <param name="count">获取的最大数量，-1表示全获取。</param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult<GetDataDicReturnDto> GetDataDic(Guid token, Guid rId, int startIndex, int count = -1)
        {
            var result = new GetDataDicReturnDto();
            var sr = _Context.SystemResources.Find(rId);
            switch (sr.Name)
            {
                case nameof(_Context.Multilinguals):
                    {
                        var coll = _Context.Multilinguals.OrderBy(c => c.Id).Skip(startIndex);
                        coll = count == -1 ? coll : coll.Take(count);
                        result.Result.AddRange(coll);
                    }
                    break;
                case nameof(_Context.LanguageDataDics):
                    {
                        var coll = _Context.LanguageDataDics.OrderBy(c => c.LanguageTag).Skip(startIndex);
                        coll = count == -1 ? coll : coll.Take(count);
                        result.Result.AddRange(coll);
                    }
                    break;
                default:
                    break;
            }
            return result;
        }

        /// <summary>
        /// 通用的导入数据字典。相当于清理表后再导入。
        /// </summary>
        /// <param name="formFile"></param>
        /// <param name="token"></param>
        /// <param name="rId">从资源列表中获取，指定资源的Id。如:6AE3BBB3-BAC9-4509-BF82-C8578830CD24 表示 多语言资源表。Id是不会变化的。</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<ImportDataDicReturnDto> ImportDataDic(IFormFile formFile, Guid token, Guid rId)
        {
            var result = new ImportDataDicReturnDto();
            var srTask = _Context.SystemResources.FindAsync(rId).AsTask();
            var workbook = _NpoiManager.GetWorkbookFromStream(formFile.OpenReadStream());
            var sheet = workbook.GetSheetAt(0);
            var sr = srTask.Result;
            switch (sr.Name)
            {
                case nameof(_Context.Multilinguals):
                    {
                        _Context.TruncateTable(nameof(_Context.Multilinguals));
                        _NpoiManager.WriteToDb(sheet, _Context, _Context.Multilinguals);
                        _Context.SaveChanges();
                    }
                    break;
                case nameof(_Context.LanguageDataDics):
                    {
                        _Context.TruncateTable(nameof(_Context.LanguageDataDics));
                        _NpoiManager.WriteToDb(sheet, _Context, _Context.LanguageDataDics);
                        _Context.SaveChanges();
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
        [HttpGet]
        [ProducesResponseType(typeof(FileResult), (int)HttpStatusCode.OK)]
        public FileStreamResult ExportDataDic(Guid token, Guid rId)
        {
            var srTask = _Context.SystemResources.FindAsync(rId).AsTask();
            var sr = srTask.Result;
            using var workbook = new HSSFWorkbook();
            var sheet = workbook.CreateSheet("Sheet0");//创建一个名称为Sheet0的表  
            var fileName = $"{sr.Name}.xls";
            switch (sr.Name)
            {
                case nameof(_Context.Multilinguals):
                    {
                        _NpoiManager.WriteToExcel(_Context.Multilinguals.AsNoTracking(), typeof(Multilingual).GetProperties().Select(c => c.Name).ToArray(), sheet);
                    }
                    break;
                case nameof(_Context.LanguageDataDics):
                    {
                        _NpoiManager.WriteToExcel(_Context.LanguageDataDics.AsNoTracking(), typeof(LanguageDataDic).GetProperties().Select(c => c.Name).ToArray(), sheet);
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
        [HttpGet]
        [ProducesResponseType(typeof(FileResult), (int)HttpStatusCode.OK)]
        public FileStreamResult ExportDataDicTemplate(Guid token, Guid rId)
        {
            var srTask = _Context.SystemResources.FindAsync(rId).AsTask();
            var sr = srTask.Result;
            using var workbook = new HSSFWorkbook();
            var sheet = workbook.CreateSheet("Sheet0");//创建一个名称为Sheet0的表  
            var fileName = $"{sr.Name}.xls";
            switch (sr.Name)
            {
                case nameof(_Context.Multilinguals):
                    {
                        _NpoiManager.WriteToExcel(_Context.Multilinguals.Take(0), typeof(Multilingual).GetProperties().Select(c => c.Name).ToArray(), sheet);
                    }
                    break;
                case nameof(_Context.LanguageDataDics):
                    {
                        _NpoiManager.WriteToExcel(_Context.LanguageDataDics.Take(0), typeof(LanguageDataDic).GetProperties().Select(c => c.Name).ToArray(), sheet);
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
    }

}
