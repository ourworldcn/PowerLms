/*
/*
 * 项目：PowerLms | 模块：报关
 * 功能：报关专用字典导入导出（Excel多Sheet）
 * 技术要点：复用ImportExportService，固定导出6张报关字典表，权限B.14
 * 作者：zc | 创建：2026-03
 */
using Microsoft.AspNetCore.Mvc;
using NPOI.SS.UserModel;
using OW.Data;
using PowerLms.Data;
using PowerLmsServer.Managers;
using PowerLmsServer.Services;
using PowerLmsWebApi.Dto;
using System.Net;

namespace PowerLmsWebApi.Controllers
{
    public partial class CustomsDictionaryController : PlControllerBase
    {
        static readonly List<string> _CustomsDictionaryTableNames = new()
        {
            nameof(CdHsCode),
            nameof(CdGoodsVsCiqCode),
            nameof(CdPlace),
            nameof(CdDomesticPort),
            nameof(CdInspectionPlace),
            nameof(CdPort)
        };

        #region 导入导出

        /// <summary>
        /// 导出报关专用字典到Excel（多Sheet结构）。
        /// 每张字典表对应一个Sheet，Sheet名称为实体类型名称。即使表无数据也会导出表头。
        /// </summary>
        /// <param name="token">登录令牌。</param>
        /// <returns>Excel文件（.xls），可直接下载。</returns>
        /// <response code="200">返回Excel文件二进制流。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="403">权限不足。</response>
        [HttpGet]
        public ActionResult ExportCustomsDictionaries([FromQuery] Guid token)
        {
            if (_AccountManager.GetOrLoadContextByToken(token, _ServiceProvider) is not OwContext context) return Unauthorized();
            if (!_AuthorizationManager.Demand(out string err, "B.14")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var merchantId = _OrgManager.GetMerchantIdByUserId(context.User.Id);
            var orgId = context.User.OrgId ?? merchantId;
            var fileBytes = _ImportExportService.ExportDictionaries(_CustomsDictionaryTableNames, orgId);
            var fileName = $"CustomsDictionaries_{DateTime.Now:yyyyMMdd_HHmmss}.xls";
            Response.Headers.Add("Content-Disposition", $"attachment; filename=\"{fileName}\"");
            return File(fileBytes, "application/octet-stream", fileName);
        }

        /// <summary>
        /// 从Excel导入报关专用字典（多Sheet结构）。
        /// 自动识别Excel中的Sheet，根据Sheet名称匹配对应字典表。
        /// Sheet名称须为实体类型名称（CdHsCode、CdGoodsVsCiqCode、CdPlace、CdDomesticPort、CdInspectionPlace、CdPort）。
        /// </summary>
        /// <param name="formFile">Excel文件（.xls 或 .xlsx）。</param>
        /// <param name="model">导入参数。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="400">文件为空或格式不支持。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="403">权限不足。</response>
        [HttpPost]
        public ActionResult<ImportCustomsDictionariesReturnDto> ImportCustomsDictionaries(IFormFile formFile,
            [FromForm] ImportCustomsDictionariesParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            if (!_AuthorizationManager.Demand(out string err, "B.14")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new ImportCustomsDictionariesReturnDto();
            if (formFile == null || formFile.Length == 0)
            {
                result.HasError = true;
                result.DebugMessage = "请选择要导入的Excel文件";
                return BadRequest(result);
            }
            var ext = Path.GetExtension(formFile.FileName)?.ToLowerInvariant();
            if (ext != ".xls" && ext != ".xlsx")
            {
                result.HasError = true;
                result.DebugMessage = "只支持.xls或.xlsx格式的Excel文件";
                return BadRequest(result);
            }
            var merchantId = _OrgManager.GetMerchantIdByUserId(context.User.Id);
            var orgId = context.User.OrgId ?? merchantId;
            using var workbook = WorkbookFactory.Create(formFile.OpenReadStream());
            var importResult = _ImportExportService.ImportDictionaries(workbook, orgId, !model.DeleteExisting);
            result.ImportedCount = importResult.TotalImportedCount;
            result.ProcessedSheets = importResult.ProcessedSheets;
            return result;
        }

        #endregion 导入导出
    }
}
