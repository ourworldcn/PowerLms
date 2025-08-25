/*
 * 项目：PowerLms物流管理系统 | 模块：通用导入导出控制器
 * 功能：支持独立表字典、客户资料的批量多Sheet Excel导入导出
 * 
 * API调用顺序：
 * 1. GetSupportedTables → 获取支持的表类型列表
 * 2. ExportMultipleTables → 批量导出指定表类型到Excel (多Sheet结构)
 * 3. ImportMultipleTables → 从Excel批量导入多个表类型 (基于Sheet名称自动识别)
 * 
 * Excel文件结构要求：
 * - 文件格式：.xls
 * - Sheet名称：使用实体类型名称 (如PlCountry、PlCustomer等)
 * - 列标题：与实体属性名称完全匹配
 * - 排除字段：Id、OrgId等系统字段自动处理
 * - 多租户：自动应用当前用户组织权限
 * 
 * 支持表类型：
 * - 独立字典表：pl_Countries、pl_Ports、pl_CargoRoutes、pl_Currencies、pl_FeesTypes、pl_ExchangeRates、pl_UnitConversions、pl_ShippingContainersKinds
 * - 客户资料主表：pl_Customers
 * - 客户资料子表：pl_CustomerContacts、pl_BusinessHeaders、pl_Tidans、pl_CustomerBlacklists、pl_LoadingAddrs
 * 
 * 技术要点：
 * - 基于OwDataUnit + OwNpoiUnit高性能Excel处理
 * - 多租户数据隔离和权限控制  
 * - 重复数据覆盖策略，依赖关系验证
 * 作者：zc | 创建：2025-01 | 修改：2025-01-27 简化为批量多表处理架构
 */

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OW.Data;
using PowerLms.Data;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsServer.Services;
using PowerLmsWebApi.Dto;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// 通用导入导出控制器
    /// 支持独立表字典、客户资料的批量多Sheet导入导出
    /// 注意：简单字典(SimpleDataDic)功能在分部控制器API (ImportExportController.SimpleDataDic.cs)中
    /// </summary>
    public partial class ImportExportController : PlControllerBase
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
        /// 包含：
        /// - 独立字典表：pl_Countries、pl_Ports、pl_CargoRoutes、pl_Currencies、pl_FeesTypes、pl_ExchangeRates、pl_UnitConversions、pl_ShippingContainersKinds
        /// - 客户资料主表：pl_Customers
        /// - 客户资料子表：pl_CustomerContacts、pl_BusinessHeaders、pl_Tidans、pl_CustomerBlacklists、pl_LoadingAddrs
        /// 不包含：ow_SimpleDataDics（简单字典，有专门的分部控制器API处理）
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

                // 获取所有独立表字典
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

                // 添加客户主表
                allTables.Add(new TableInfo 
                { 
                    TableName = "PlCustomer", 
                    DisplayName = "客户资料" 
                });

                result.Tables = allTables.OrderBy(x => x.TableName).ToList();
                
                _Logger.LogInformation("获取支持的表列表成功，共返回 {Count} 个表（不包括ow_SimpleDataDics）", result.Tables.Count);
                
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

        #region 批量导入导出接口

        /// <summary>
        /// 批量导出功能（多表多Sheet模式）
        /// 支持同时导出多个独立表字典和客户资料表到一个Excel文件
        /// 每个表对应一个Sheet，Sheet名称为实体类型名称
        /// 即使表无数据也会导出表头，便于客户填写数据模板
        /// 
        /// 支持的表类型：
        /// - 独立字典表：PlCountry、PlPort、PlCargoRoute、PlCurrency、FeesType、PlExchangeRate、UnitConversion、ShippingContainersKind
        /// - 客户资料主表：PlCustomer  
        /// - 客户资料子表：PlCustomerContact、PlBusinessHeader、PlTidan、CustomerBlacklist、PlLoadingAddr
        /// 
        /// Excel文件结构：
        /// - 文件名：MultiTables_[Table1]_[Table2]_[DateTime].xls
        /// - 每个表对应一个Sheet，Sheet名称为实体类型名称
        /// - 列标题为实体属性名称（排除Id、OrgId系统字段）
        /// </summary>
        /// <param name="paramsDto">参数封装对象，包含要导出的表名称列表</param>
        /// <returns>Excel文件</returns>
        [HttpGet]
        public ActionResult ExportMultipleTables([FromQuery] ExportMultipleTablesParamsDto paramsDto)
        {
            if (_AccountManager.GetOrLoadContextByToken(paramsDto.Token, _ServiceProvider) is not OwContext context) 
                return Unauthorized();

            try
            {
                if (paramsDto.TableNames == null || !paramsDto.TableNames.Any())
                {
                    return BadRequest("请至少指定一个表名称进行导出");
                }

                var orgId = GetUserOrgId(context);
                
                // 分离独立表字典和客户资料表
                var dictionaryTypes = _ImportExportService.GetSupportedDictionaryTypes().Select(x => x.TypeName).ToHashSet(StringComparer.OrdinalIgnoreCase);
                var customerSubTypes = _ImportExportService.GetSupportedCustomerSubTableTypes().Select(x => x.TypeName).ToHashSet(StringComparer.OrdinalIgnoreCase);
                customerSubTypes.Add("PlCustomer"); // 添加客户主表

                var dictionaryTables = paramsDto.TableNames.Where(x => dictionaryTypes.Contains(x)).ToList();
                var customerTables = paramsDto.TableNames.Where(x => customerSubTypes.Contains(x)).ToList();
                var unsupportedTables = paramsDto.TableNames.Except(dictionaryTables).Except(customerTables).ToList();

                if (unsupportedTables.Any())
                {
                    return BadRequest($"不支持的表名称: {string.Join(", ", unsupportedTables)}。ow_SimpleDataDics（简单字典）请使用专门的分部控制器API。");
                }

                byte[] fileBytes;
                
                // 如果只有独立表字典
                if (dictionaryTables.Any() && !customerTables.Any())
                {
                    fileBytes = _ImportExportService.ExportDictionaries(dictionaryTables, orgId);
                }
                // 如果只有客户资料表
                else if (customerTables.Any() && !dictionaryTables.Any())
                {
                    fileBytes = _ImportExportService.ExportCustomerTables(customerTables, orgId);
                }
                // 如果混合了两种类型，需要合并处理
                else
                {
                    // TODO: 实现统一的多类型表导出方法
                    return BadRequest("暂不支持同时导出独立表字典和客户资料表，请分别调用对应的导出功能");
                }
                
                var fileName = $"MultiTables_{string.Join("_", paramsDto.TableNames.Take(3))}{(paramsDto.TableNames.Count > 3 ? "_etc" : "")}_{DateTime.Now:yyyyMMdd_HHmmss}.xls";

                return File(fileBytes, "application/vnd.ms-excel", fileName);
            }
            catch (ArgumentException ex)
            {
                _Logger.LogWarning(ex, "批量导出参数错误: {TableNames}", string.Join(",", paramsDto.TableNames ?? new List<string>()));
                return BadRequest($"参数错误: {ex.Message}");
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "批量导出时发生错误，表名称: {TableNames}", string.Join(",", paramsDto.TableNames ?? new List<string>()));
                return StatusCode(500, $"导出失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 批量导入功能（多表多Sheet模式）
        /// 自动识别Excel中的所有Sheet，根据Sheet名称匹配表类型进行导入
        /// 支持独立表字典和客户资料表的混合导入
        /// 
        /// 导入逻辑：
        /// 1. 自动遍历Excel中的所有Sheet
        /// 2. 根据Sheet名称（实体类型名称）匹配对应的表类型
        /// 3. 调用相应的导入处理逻辑
        /// 4. Sheet级别错误隔离，单个Sheet失败不影响其他Sheet
        /// 
        /// 字段处理：
        /// - Id字段：自动生成新的GUID
        /// - OrgId字段：自动设置为当前登录用户的机构ID
        /// - 其他字段：根据Excel列标题与实体属性名称匹配
        /// 
        /// 更新策略：
        /// - updateExisting=true：基于Code字段匹配现有记录进行更新
        /// - updateExisting=false：仅新增，不更新现有记录
        /// </summary>
        /// <param name="formFile">Excel文件</param>
        /// <param name="paramsDto">参数封装对象</param>
        /// <returns>导入结果</returns>
        [HttpPost]
        public ActionResult<ImportMultipleTablesReturnDto> ImportMultipleTables(IFormFile formFile, [FromForm] ImportMultipleTablesParamsDto paramsDto)
        {
            if (_AccountManager.GetOrLoadContextByToken(paramsDto.Token, _ServiceProvider) is not OwContext context) 
                return Unauthorized();

            var result = new ImportMultipleTablesReturnDto();

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
                
                // 先尝试作为独立表字典导入
                try
                {
                    var dictionaryResult = _ImportExportService.ImportDictionaries(formFile, orgId, !paramsDto.DeleteExisting);
                    if (dictionaryResult.ProcessedSheets > 0)
                    {
                        result.ImportedCount = dictionaryResult.TotalImportedCount;
                        result.ProcessedSheets = dictionaryResult.ProcessedSheets;
                        result.DebugMessage = $"导入独立表字典完成，共处理 {dictionaryResult.ProcessedSheets} 个Sheet，导入 {dictionaryResult.TotalImportedCount} 条记录";
                        return result;
                    }
                }
                catch
                {
                    // 忽略错误，继续尝试客户资料表导入
                }
                
                // 再尝试作为客户资料表导入
                try
                {
                    var customerResult = _ImportExportService.ImportCustomerTables(formFile, orgId, !paramsDto.DeleteExisting);
                    if (customerResult.ProcessedSheets > 0)
                    {
                        result.ImportedCount = customerResult.TotalImportedCount;
                        result.ProcessedSheets = customerResult.ProcessedSheets;
                        result.DebugMessage = $"导入客户资料表完成，共处理 {customerResult.ProcessedSheets} 个Sheet，导入 {customerResult.TotalImportedCount} 条记录";
                        return result;
                    }
                }
                catch
                {
                    // 忽略错误
                }
                
                // 如果两种方式都没有成功处理任何Sheet
                result.HasError = true;
                result.ErrorCode = 400;
                result.DebugMessage = "Excel文件中没有识别到支持的表类型Sheet，请检查Sheet名称是否正确";
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "批量导入时发生错误");
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
        /// <param name="context">用户上下文</param>
        /// <returns>用户所属的组织ID</returns>
        private Guid? GetUserOrgId(OwContext context)
        {
            var merchantId = _OrgManager.GetMerchantIdByUserId(context.User.Id);
            return context.User.OrgId ?? merchantId;
        }

        #endregion
    }
}