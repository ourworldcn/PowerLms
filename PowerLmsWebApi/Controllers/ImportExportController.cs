/*
 * 项目：PowerLms物流管理系统 | 模块：通用导入导出控制器
 * 功能：Excel导入导出API，支持字典、客户资料及其子表
 * 技术要点：
 * - 多租户数据隔离，输入时忽略Excel中OrgId，输出时OrgId列不输出
 * - 重复数据覆盖策略，依赖关系验证
 * - 统一的权限验证和异常处理
 * 作者：zc | 创建：2025-01-27
 */

using Microsoft.AspNetCore.Mvc;
using OW.Data;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsServer.Services;
using PowerLmsWebApi.Dto;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// 通用导入导出控制器
    /// </summary>
    public class ImportExportController : PlControllerBase
    {
        readonly PowerLmsUserDbContext _DbContext;
        readonly AccountManager _AccountManager;
        readonly IServiceProvider _ServiceProvider;
        readonly EntityManager _EntityManager;
        readonly OrgManager<PowerLmsUserDbContext> _OrgManager;
        readonly AuthorizationManager _AuthorizationManager;
        readonly ILogger<ImportExportController> _Logger;
        readonly ImportExportService _ImportExportService;

        /// <summary>
        /// 构造函数
        /// </summary>
        public ImportExportController(
            PowerLmsUserDbContext context,
            AccountManager accountManager,
            IServiceProvider serviceProvider,
            EntityManager entityManager,
            OrgManager<PowerLmsUserDbContext> orgManager,
            AuthorizationManager authorizationManager,
            ILogger<ImportExportController> logger,
            ImportExportService importExportService)
        {
            _DbContext = context;
            _AccountManager = accountManager;
            _ServiceProvider = serviceProvider;
            _EntityManager = entityManager;
            _OrgManager = orgManager;
            _AuthorizationManager = authorizationManager;
            _Logger = logger;
            _ImportExportService = importExportService;
        }

        #region 类型查询接口

        /// <summary>
        /// 获取支持的表列表
        /// </summary>
        /// <param name="paramsDto">参数封装对象</param>
        /// <returns>所有支持的表和中文名称列表</returns>
        [HttpGet]
        public ActionResult<GetSupportedTablesReturnDto> GetSupportedTables([FromQuery] GetSupportedTablesParamsDto paramsDto)
        {
            if (_AccountManager.GetOrLoadContextByToken(paramsDto.Token, _ServiceProvider) is not OwContext context) 
                return Unauthorized();

            var result = new GetSupportedTablesReturnDto();
            
            try
            {
                var orgId = GetUserOrgId(context);
                var allTables = new List<TableInfo>();

                // 获取所有字典表
                var dictionaryTypes = _ImportExportService.GetSupportedDictionaryTypes();
                allTables.AddRange(dictionaryTypes.Select(x => new TableInfo 
                { 
                    TableName = x.TypeName, 
                    DisplayName = x.DisplayName 
                }));

                // 获取所有客户子表
                var customerSubTypes = _ImportExportService.GetSupportedCustomerSubTableTypes();
                allTables.AddRange(customerSubTypes.Select(x => new TableInfo 
                { 
                    TableName = x.TypeName, 
                    DisplayName = x.DisplayName 
                }));

                // 获取简单字典
                var simpleDictionaries = _ImportExportService.GetSimpleDictionaryCategories(orgId);
                allTables.AddRange(simpleDictionaries.Select(x => new TableInfo 
                { 
                    TableName = x.CategoryCode, 
                    DisplayName = x.DisplayName 
                }));

                // 添加客户主表
                allTables.Add(new TableInfo 
                { 
                    TableName = "PlCustomer", 
                    DisplayName = "客户资料" 
                });

                result.Tables = allTables.OrderBy(x => x.TableName).ToList();
                
                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "获取支持的表列表时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"获取失败: {ex.Message}";
                return StatusCode(500, result);
            }
        }

        #endregion

        #region 统一导入导出接口

        /// <summary>
        /// 通用导出功能
        /// </summary>
        /// <param name="paramsDto">参数封装对象</param>
        /// <returns>Excel文件</returns>
        [HttpGet]
        public ActionResult Export([FromQuery] ExportParamsDto paramsDto)
        {
            if (_AccountManager.GetOrLoadContextByToken(paramsDto.Token, _ServiceProvider) is not OwContext context) 
                return Unauthorized();

            try
            {
                var orgId = GetUserOrgId(context);
                byte[] fileBytes;
                string fileName;

                // 简化逻辑：简单字典作为整体处理
                if (paramsDto.TableName.Equals("SimpleDataDic", StringComparison.OrdinalIgnoreCase))
                {
                    // 简单字典整体导出
                    fileBytes = _ImportExportService.ExportSimpleDictionary(orgId);
                    fileName = $"SimpleDataDic_{DateTime.Now:yyyyMMdd_HHmmss}.xls";
                }
                else
                {
                    // 判断是字典类型还是客户资料类型
                    var dictionaryTypes = _ImportExportService.GetSupportedDictionaryTypes();
                    var customerSubTypes = _ImportExportService.GetSupportedCustomerSubTableTypes();
                    
                    if (dictionaryTypes.Any(x => x.TypeName.Equals(paramsDto.TableName, StringComparison.OrdinalIgnoreCase)))
                    {
                        // 字典导出
                        fileBytes = _ImportExportService.ExportDictionary(paramsDto.TableName, orgId);
                    }
                    else if (customerSubTypes.Any(x => x.TypeName.Equals(paramsDto.TableName, StringComparison.OrdinalIgnoreCase)))
                    {
                        // 客户子表导出
                        fileBytes = _ImportExportService.ExportCustomerSubTable(paramsDto.TableName, orgId);
                    }
                    else if (paramsDto.TableName.Equals("PlCustomer", StringComparison.OrdinalIgnoreCase))
                    {
                        // 客户主表导出
                        fileBytes = _ImportExportService.ExportCustomers(orgId);
                    }
                    else
                    {
                        return BadRequest($"不支持的表名称: {paramsDto.TableName}");
                    }
                    
                    fileName = $"{paramsDto.TableName}_{DateTime.Now:yyyyMMdd_HHmmss}.xls";
                }

                return File(fileBytes, "application/vnd.ms-excel", fileName);
            }
            catch (ArgumentException ex)
            {
                _Logger.LogWarning(ex, "导出参数错误: {TableName}", paramsDto.TableName);
                return BadRequest($"参数错误: {ex.Message}");
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "导出 {TableName} 时发生错误", paramsDto.TableName);
                return StatusCode(500, $"导出失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 通用导入功能
        /// </summary>
        /// <param name="formFile">Excel文件</param>
        /// <param name="paramsDto">参数封装对象</param>
        /// <returns>导入结果</returns>
        [HttpPost]
        public ActionResult<ImportReturnDto> Import(IFormFile formFile, [FromForm] ImportParamsDto paramsDto)
        {
            if (_AccountManager.GetOrLoadContextByToken(paramsDto.Token, _ServiceProvider) is not OwContext context) 
                return Unauthorized();

            var result = new ImportReturnDto();

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
                (int ImportedCount, List<string> Details) importResult;

                // 简化逻辑：简单字典作为整体处理，不再按分类
                if (paramsDto.TableName.Equals("SimpleDataDic", StringComparison.OrdinalIgnoreCase))
                {
                    // 简单字典整体导入
                    importResult = _ImportExportService.ImportSimpleDictionary(formFile, orgId, !paramsDto.DeleteExisting);
                }
                else
                {
                    // 判断是字典类型还是客户资料类型
                    var dictionaryTypes = _ImportExportService.GetSupportedDictionaryTypes();
                    var customerSubTypes = _ImportExportService.GetSupportedCustomerSubTableTypes();
                    
                    if (dictionaryTypes.Any(x => x.TypeName.Equals(paramsDto.TableName, StringComparison.OrdinalIgnoreCase)))
                    {
                        // 字典导入
                        importResult = _ImportExportService.ImportDictionary(
                            formFile, paramsDto.TableName, orgId, !paramsDto.DeleteExisting);
                    }
                    else if (customerSubTypes.Any(x => x.TypeName.Equals(paramsDto.TableName, StringComparison.OrdinalIgnoreCase)))
                    {
                        // 客户子表导入
                        importResult = _ImportExportService.ImportCustomerSubTable(
                            formFile, paramsDto.TableName, orgId, !paramsDto.DeleteExisting);
                    }
                    else if (paramsDto.TableName.Equals("PlCustomer", StringComparison.OrdinalIgnoreCase))
                    {
                        // 客户主表导入
                        importResult = _ImportExportService.ImportCustomers(
                            formFile, orgId, !paramsDto.DeleteExisting);
                    }
                    else
                    {
                        result.HasError = true;
                        result.ErrorCode = 400;
                        result.DebugMessage = $"不支持的表名称: {paramsDto.TableName}";
                        return BadRequest(result);
                    }
                }
                
                result.ImportedCount = importResult.ImportedCount;
                // 将处理详情和目标表信息压缩到DebugMessage中
                var detailsText = importResult.Details?.Count > 0 ? 
                    $"，详情: {string.Join("; ", importResult.Details.Take(3))}{(importResult.Details.Count > 3 ? "..." : "")}" : "";
                result.DebugMessage = $"导入{paramsDto.TableName}完成，共处理 {importResult.ImportedCount} 条记录{detailsText}";
                
                return result;
            }
            catch (ArgumentException ex)
            {
                _Logger.LogWarning(ex, "导入参数错误: {TableName}", paramsDto.TableName);
                result.HasError = true;
                result.ErrorCode = 400;
                result.DebugMessage = $"参数错误: {ex.Message}";
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "导入 {TableName} 时发生错误", paramsDto.TableName);
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"导入失败: {ex.Message}";
                return StatusCode(500, result);
            }
        }

        #endregion

        #region 私有辅助方法

        /// <summary>
        /// 获取用户的组织ID
        /// </summary>
        private Guid? GetUserOrgId(OwContext context)
        {
            var merchantId = _OrgManager.GetMerchantIdByUserId(context.User.Id);
            return context.User.OrgId ?? merchantId;
        }

        #endregion
    }
}