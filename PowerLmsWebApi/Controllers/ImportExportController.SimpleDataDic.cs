/*
 * 项目：PowerLms物流管理系统 | 模块：简单字典导入导出控制器
 * 功能：ow_SimpleDataDics表的Excel导入导出专用API
 * 
 * API调用顺序：
 * 1. GetSimpleDictionaryCatalogCodes → 获取可用的Catalog Code列表（来源于ow_DataDicCatalogs表）
 * 2. ExportSimpleDictionary → 导出指定Catalog Code的简单字典到Excel (多Sheet结构)
 * 3. ImportSimpleDictionary → 从Excel导入简单字典数据 (基于Sheet名称自动识别Catalog Code)
 * 
 * Excel文件结构要求：
 * - 文件格式：.xls
 * - Sheet名称：使用ow_DataDicCatalogs.Code字段值 (如"COUNTRY"、"PORT"、"CURRENCY")
 * - 列标题：与ow_SimpleDataDics表字段名称完全匹配
 * - 排除字段：DataDicId（通过Sheet名称自动设置）、Id（自动生成）
 * - 多租户：自动应用当前用户组织权限
 * 
 * 数据关联逻辑：
 * - Sheet名称(Catalog Code) → 查询ow_DataDicCatalogs表 → 获取Id → 设置ow_SimpleDataDics.DataDicId
 * - 支持基于Code字段的重复数据覆盖策略
 * - 批量查询优化，避免N+1查询问题
 * 
 * 技术要点：
 * - 基于ImportExportService.SimpleDataDic.cs的专业化实现
 * - 多租户数据隔离和权限控制
 * - 批量数据库操作和查询优化
 * - Sheet级别错误隔离处理
 * 作者：zc | 创建：2025-01-27
 */

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OW.Data;
using PowerLms.Data;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Dto;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// 导入导出控制器 - 简单字典专用功能
    /// 处理ow_SimpleDataDics表的Excel导入导出，支持基于ow_DataDicCatalogs.Code的多Sheet结构
    /// 注意：通用表字典导入导出功能在主控制器中 (ImportExportController.cs)
    /// </summary>
    public partial class ImportExportController
    {
        #region 简单字典专用API

        /// <summary>
        /// 获取简单字典Catalog Code列表
        /// 查询ow_DataDicCatalogs表中的可用分类代码，用于确定可导入导出的简单字典类型
        /// 
        /// 多租户逻辑：
        /// - 有组织ID：返回当前组织 + 全局数据 (OrgId == orgId || OrgId == null)
        /// - 无组织ID：仅返回全局数据 (OrgId == null)
        /// 
        /// 返回数据结构：
        /// - Code：ow_DataDicCatalogs.Code字段，用作Excel的Sheet名称
        /// - DisplayName：ow_DataDicCatalogs.DisplayName字段，用于界面显示
        /// </summary>
        /// <param name="paramsDto">参数封装对象</param>
        /// <returns>可用的分类代码和显示名称列表</returns>
        [HttpGet]
        public ActionResult<GetSimpleDictionaryCatalogCodesReturnDto> GetSimpleDictionaryCatalogCodes([FromQuery] GetSimpleDictionaryCatalogCodesParamsDto paramsDto)
        {
            if (_AccountManager.GetOrLoadContextByToken(paramsDto.Token, _ServiceProvider) is not OwContext context) 
                return Unauthorized();

            var result = new GetSimpleDictionaryCatalogCodesReturnDto();
            
            try
            {
                var orgId = GetUserOrgId(context);
                
                var catalogCodes = _ImportExportService.GetAvailableCatalogCodes(orgId);
                result.CatalogCodes = catalogCodes.Select(x => new CatalogCodeInfo 
                { 
                    Code = x.Code, 
                    DisplayName = x.DisplayName 
                }).ToList();
                
                _Logger.LogInformation("获取简单字典Catalog Code列表成功，共返回 {Count} 个分类代码", result.CatalogCodes.Count);
                
                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "获取简单字典Catalog Code列表时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"获取失败: {ex.Message}";
                return StatusCode(500, result);
            }
        }

        /// <summary>
        /// 导出简单字典到Excel（多Sheet结构）
        /// 根据指定的Catalog Code列表，从ow_SimpleDataDics表导出数据
        /// 
        /// 导出逻辑：
        /// 1. 批量查询所有Catalog Code对应的ow_DataDicCatalogs记录
        /// 2. 通过DataDicId关联查询ow_SimpleDataDics表数据
        /// 3. 为每个Catalog Code创建一个Sheet，Sheet名称为Catalog Code值
        /// 4. 即使某个Catalog Code无数据，也导出包含表头的Sheet
        /// 
        /// Excel文件结构：
        /// - 文件名：SimpleDataDic_[Code1]_[Code2]_[DateTime].xls
        /// - Sheet名称：ow_DataDicCatalogs.Code字段值（如"COUNTRY"、"PORT"）
        /// - 列标题：ow_SimpleDataDics表的字段名称（排除DataDicId、Id）
        /// - 数据行：对应Catalog Code下的所有简单字典记录
        /// 
        /// 性能特点：
        /// - 批量查询避免N+1查询问题
        /// - AsNoTracking提升查询性能
        /// - 流式Excel生成减少内存占用
        /// </summary>
        /// <param name="paramsDto">参数封装对象，包含要导出的Catalog Code列表</param>
        /// <returns>Excel文件</returns>
        [HttpGet]
        public ActionResult ExportSimpleDictionary([FromQuery] ExportSimpleDictionaryParamsDto paramsDto)
        {
            if (_AccountManager.GetOrLoadContextByToken(paramsDto.Token, _ServiceProvider) is not OwContext context) 
                return Unauthorized();

            try
            {
                if (paramsDto.CatalogCodes == null || !paramsDto.CatalogCodes.Any())
                {
                    return BadRequest("请至少指定一个Catalog Code进行导出");
                }

                var orgId = GetUserOrgId(context);
                
                var fileBytes = _ImportExportService.ExportSimpleDictionaries(paramsDto.CatalogCodes, orgId);
                
                var fileName = $"SimpleDataDic_{string.Join("_", paramsDto.CatalogCodes.Take(3))}{(paramsDto.CatalogCodes.Count > 3 ? "_etc" : "")}_{DateTime.Now:yyyyMMdd_HHmmss}.xls";

                return File(fileBytes, "application/vnd.ms-excel", fileName);
            }
            catch (ArgumentException ex)
            {
                _Logger.LogWarning(ex, "导出简单字典参数错误: {CatalogCodes}", string.Join(",", paramsDto.CatalogCodes ?? new List<string>()));
                return BadRequest($"参数错误: {ex.Message}");
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "导出简单字典时发生错误，Catalog Codes: {CatalogCodes}", string.Join(",", paramsDto.CatalogCodes ?? new List<string>()));
                return StatusCode(500, $"导出失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 从Excel导入简单字典（多Sheet结构）
        /// 自动识别Excel中的所有Sheet，根据Sheet名称匹配ow_DataDicCatalogs.Code进行导入
        /// 
        /// 导入处理流程：
        /// 1. 遍历Excel中的所有Sheet
        /// 2. 将Sheet名称作为Catalog Code查询ow_DataDicCatalogs表
        /// 3. 找到匹配的DataDicCatalog记录后，获取其Id
        /// 4. 读取Sheet中的数据行，创建ow_SimpleDataDics记录
        /// 5. 设置SimpleDataDic.DataDicId = DataDicCatalog.Id
        /// 6. 批量保存到数据库
        /// 
        /// 字段处理规则：
        /// - Id字段：自动生成新的GUID
        /// - DataDicId字段：通过Sheet名称(Catalog Code)自动设置
        /// - CreateDateTime字段：设置为当前时间
        /// - 其他字段：根据Excel列标题与ow_SimpleDataDics表字段名称匹配
        /// 
        /// 更新策略：
        /// - updateExisting=true：基于Code字段匹配现有记录进行更新
        /// - updateExisting=false：仅新增，不更新现有记录
        /// 
        /// 错误处理：
        /// - Sheet级别错误隔离，单个Sheet失败不影响其他Sheet
        /// - 详细的错误信息和成功统计
        /// - 完整的导入结果反馈
        /// </summary>
        /// <param name="formFile">Excel文件，必须包含以Catalog Code命名的Sheet</param>
        /// <param name="paramsDto">参数封装对象</param>
        /// <returns>导入结果，包含总记录数、成功Sheet数和详细信息</returns>
        [HttpPost]
        public ActionResult<ImportSimpleDictionaryReturnDto> ImportSimpleDictionary(IFormFile formFile, [FromForm] ImportSimpleDictionaryParamsDto paramsDto)
        {
            if (_AccountManager.GetOrLoadContextByToken(paramsDto.Token, _ServiceProvider) is not OwContext context) 
                return Unauthorized();

            var result = new ImportSimpleDictionaryReturnDto();

            try
            {
                if (formFile == null || formFile.Length == 0)
                {
                    result.HasError = true;
                    result.ErrorCode = 400;
                    result.DebugMessage = "请选择要导入的Excel文件";
                    return BadRequest(result);
                }

                var orgId = GetUserOrgId(context);
                
                var importResult = _ImportExportService.ImportSimpleDictionaries(formFile, orgId, !paramsDto.DeleteExisting);
                
                result.ImportedCount = importResult.TotalImportedCount;
                result.ProcessedSheets = importResult.ProcessedSheets;
                
                // 组装详细信息
                result.Details = importResult.SheetResults.Select(x => 
                    x.Success 
                        ? $"Sheet '{x.SheetName}': 成功导入 {x.ImportedCount} 条记录"
                        : $"Sheet '{x.SheetName}': 导入失败 - {x.ErrorMessage}"
                ).ToList();

                if (importResult.TotalImportedCount > 0)
                {
                    result.DebugMessage = $"导入简单字典完成，共处理 {importResult.ProcessedSheets} 个Sheet，导入 {importResult.TotalImportedCount} 条记录";
                }
                else
                {
                    result.HasError = true;
                    result.ErrorCode = 400;
                    result.DebugMessage = "没有成功导入任何数据，请检查Excel文件格式和Sheet名称";
                }

                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "导入简单字典时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"导入失败: {ex.Message}";
                return StatusCode(500, result);
            }
        }

        #endregion
    }
}