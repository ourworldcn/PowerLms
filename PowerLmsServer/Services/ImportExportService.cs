/*
 * 项目：PowerLms物流管理系统 | 模块：通用导入导出服务
 * 功能：统一的Excel导入导出处理，支持独立表字典和客户资料表的批量多Sheet处理
 * 包含表类型：
 * - 独立字典表：pl_Countries、pl_Ports、pl_CargoRoutes、pl_Currencies、pl_FeesTypes、
 *               pl_ExchangeRates、pl_UnitConversions、pl_ShippingContainersKinds等
 * - 客户资料主表：pl_Customers
 * - 客户资料子表：pl_CustomerContacts、pl_BusinessHeaders、pl_Tidans、pl_CustomerBlacklists、pl_LoadingAddrs
 * 技术要点：
 * - 基于OwDataUnit + OwNpoiUnit高性能Excel处理
 * - 多租户数据隔离和权限控制
 * - 重复数据覆盖策略，依赖关系验证
 * - 批量数据库操作和查询优化
 * - 多Sheet批量处理，Sheet名称直接使用数据库表名
 * 作者：zc | 创建：2025-01-27 | 修改：2025-01-27 简化为批量多表处理
 */

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using OW.Data;
using PowerLms.Data;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;

namespace PowerLmsServer.Services
{
    /// <summary>
    /// 通用导入导出服务类
    /// 支持独立表字典和客户资料表的批量多Sheet Excel导入导出
    /// 注意：简单字典(SimpleDataDic)功能在分部类ImportExportService.SimpleDataDic.cs中
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Scoped)]
    public partial class ImportExportService
    {
        private readonly PowerLmsUserDbContext _DbContext;
        private readonly AuthorizationManager _AuthManager;
        private readonly ILogger<ImportExportService> _Logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        public ImportExportService(
            PowerLmsUserDbContext dbContext,
            AuthorizationManager authManager,
            ILogger<ImportExportService> logger)
        {
            _DbContext = dbContext;
            _AuthManager = authManager;
            _Logger = logger;
        }

        #region 支持的导入导出类型管理

        /// <summary>
        /// 获取支持的独立表字典类型列表
        /// 包含：pl_Countries、pl_Ports、pl_CargoRoutes、pl_Currencies、pl_FeesTypes、pl_ExchangeRates、pl_UnitConversions、pl_ShippingContainersKinds
        /// </summary>
        /// <returns>独立表字典类型名称和中文名称的元组集合</returns>
        public List<(string TypeName, string DisplayName)> GetSupportedDictionaryTypes()
        {
            var supportedTypes = new List<(string TypeName, string DisplayName)>();

            // 从DbContext中获取所有实体类型
            var entityTypes = _DbContext.Model.GetEntityTypes();

            foreach (var entityType in entityTypes)
            {
                var clrType = entityType.ClrType;
                var typeName = clrType.Name;
                
                // 过滤出独立表字典类型的实体，并确保有Comment注释
                if (IsIndependentDictionaryEntity(typeName))
                {
                    var displayName = GetTableCommentFromEntityType(entityType);
                    // 只有当表有Comment注释时才加入支持列表
                    if (!string.IsNullOrEmpty(displayName))
                    {
                        supportedTypes.Add((typeName, displayName));
                    }
                    else
                    {
                        _Logger.LogWarning("独立字典实体 {TypeName} 缺少Comment注释，跳过导入导出支持", typeName);
                    }
                }
            }

            return supportedTypes.OrderBy(x => x.TypeName).ToList();
        }

        /// <summary>
        /// 获取支持的客户资料子表类型列表
        /// 包含：pl_CustomerContacts、pl_BusinessHeaders、pl_Tidans、pl_CustomerBlacklists、pl_LoadingAddrs
        /// </summary>
        /// <returns>子表类型名称和中文名称的元组集合</returns>
        public List<(string TypeName, string DisplayName)> GetSupportedCustomerSubTableTypes()
        {
            var supportedTypes = new List<(string TypeName, string DisplayName)>();

            // 从DbContext中获取所有实体类型
            var entityTypes = _DbContext.Model.GetEntityTypes();

            foreach (var entityType in entityTypes)
            {
                var clrType = entityType.ClrType;
                var typeName = clrType.Name;
                
                // 过滤出客户资料子表类型的实体，并确保有Comment注释
                if (IsCustomerSubTableEntity(typeName))
                {
                    var displayName = GetTableCommentFromEntityType(entityType);
                    // 只有当表有Comment注释时才加入支持列表
                    if (!string.IsNullOrEmpty(displayName))
                    {
                        supportedTypes.Add((typeName, displayName));
                    }
                    else
                    {
                        _Logger.LogWarning("客户子表实体 {TypeName} 缺少Comment注释，跳过导入导出支持", typeName);
                    }
                }
            }

            return supportedTypes.OrderBy(x => x.TypeName).ToList();
        }

        /// <summary>
        /// 验证导入导出类型是否支持
        /// 支持独立表字典和客户资料表
        /// </summary>
        /// <param name="typeName">实体类型名称</param>
        /// <returns>是否支持该类型的导入导出</returns>
        public bool IsSupportedImportExportType(string typeName)
        {
            var supportedTypes = GetSupportedDictionaryTypes()
                .Concat(GetSupportedCustomerSubTableTypes())
                .Select(x => x.TypeName);
            
            // 明确支持PlCustomer客户主表
            var allSupportedTypes = supportedTypes.Concat(new[] { "PlCustomer" });
            
            return allSupportedTypes.Any(x => x.Equals(typeName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 判断是否为独立表字典实体
        /// 包含：PlCountry、PlPort、PlCargoRoute、PlCurrency、FeesType、PlExchangeRate、UnitConversion、ShippingContainersKind
        /// </summary>
        /// <param name="typeName">实体类型名称</param>
        /// <returns>是否为独立表字典实体</returns>
        private bool IsIndependentDictionaryEntity(string typeName)
        {
            // 独立表字典实体通常以Pl开头或为FeesType等
            return typeName.StartsWith("Pl") && 
                   (typeName.Contains("Country") || 
                    typeName.Contains("Port") || 
                    typeName.Contains("Route") || 
                    typeName.Contains("Currency") || 
                    typeName.Contains("Exchange") || 
                    typeName.Contains("Unit") ||
                    typeName.Contains("Shipping") ||
                    typeName.Equals("PlCustomer", StringComparison.OrdinalIgnoreCase)) ||
                   typeName.Equals("FeesType", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 判断是否为客户资料子表实体
        /// 包含：PlCustomerContact、PlBusinessHeader、PlTidan、CustomerBlacklist、PlLoadingAddr
        /// </summary>
        /// <param name="typeName">实体类型名称</param>
        /// <returns>是否为客户资料子表实体</returns>
        private bool IsCustomerSubTableEntity(string typeName)
        {
            // 客户资料子表实体
            return typeName.StartsWith("PlCustomer") && !typeName.Equals("PlCustomer", StringComparison.OrdinalIgnoreCase) ||
                   typeName.Equals("PlBusinessHeader", StringComparison.OrdinalIgnoreCase) ||
                   typeName.Equals("PlTidan", StringComparison.OrdinalIgnoreCase) ||
                   typeName.Equals("CustomerBlacklist", StringComparison.OrdinalIgnoreCase) ||
                   typeName.Equals("PlLoadingAddr", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 从EntityType获取表注释
        /// </summary>
        /// <param name="entityType">EF Core实体类型</param>
        /// <returns>表注释字符串</returns>
        private string GetTableCommentFromEntityType(Microsoft.EntityFrameworkCore.Metadata.IEntityType entityType)
        {
            try
            {
                // 从EF Core的元数据中获取表注释
                var comment = entityType.GetComment();
                return comment;
            }
            catch (Exception ex)
            {
                _Logger.LogWarning(ex, "获取实体 {EntityType} 的注释时发生错误", entityType.ClrType.Name);
                return null;
            }
        }

        #endregion

        #region 批量独立表字典导入导出

        /// <summary>
        /// 批量导出多个独立表字典类型到Excel（多Sheet结构）
        /// 支持：pl_Countries、pl_Ports、pl_CargoRoutes、pl_Currencies、pl_FeesTypes、pl_ExchangeRates、pl_UnitConversions、pl_ShippingContainersKinds
        /// 每个表类型对应一个Sheet，Sheet名称为实体类型名称
        /// 即使表无数据也会导出表头，便于客户填写数据模板
        /// </summary>
        /// <param name="dictionaryTypes">要导出的表类型列表</param>
        /// <param name="orgId">组织ID，用于多租户数据隔离</param>
        /// <returns>Excel文件字节数组</returns>
        public byte[] ExportDictionaries(List<string> dictionaryTypes, Guid? orgId)
        {
            if (dictionaryTypes == null || !dictionaryTypes.Any())
                throw new ArgumentException("请至少指定一个表类型");

            try
            {
                var supportedTypes = GetSupportedDictionaryTypes().Select(x => x.TypeName).ToHashSet(StringComparer.OrdinalIgnoreCase);
                
                using var workbook = new HSSFWorkbook();
                int totalExported = 0;

                foreach (var dictionaryType in dictionaryTypes)
                {
                    if (supportedTypes.Contains(dictionaryType))
                    {
                        var sheet = workbook.CreateSheet(dictionaryType);
                        var exportedCount = dictionaryType switch
                        {
                            "PlCountry" => ExportEntityData<PlCountry>(sheet, orgId),
                            "PlPort" => ExportEntityData<PlPort>(sheet, orgId),
                            "PlCargoRoute" => ExportEntityData<PlCargoRoute>(sheet, orgId),
                            "PlCurrency" => ExportEntityData<PlCurrency>(sheet, orgId),
                            "FeesType" => ExportEntityData<FeesType>(sheet, orgId),
                            "PlExchangeRate" => ExportEntityData<PlExchangeRate>(sheet, orgId),
                            "UnitConversion" => ExportEntityData<UnitConversion>(sheet, orgId),
                            "ShippingContainersKind" => ExportEntityData<ShippingContainersKind>(sheet, orgId),
                            _ => 0
                        };
                        totalExported += exportedCount;
                        
                        _Logger.LogInformation("导出独立表字典 {DictionaryType} 到Sheet，共 {Count} 条记录", dictionaryType, exportedCount);
                    }
                    else
                    {
                        _Logger.LogWarning("不支持的独立表字典类型: {DictionaryType}，跳过导出", dictionaryType);
                    }
                }

                using var stream = new MemoryStream();
                workbook.Write(stream, true);
                workbook.Close();

                _Logger.LogInformation("批量导出独立表字典完成，共 {TotalCount} 条记录，{SheetCount} 个Sheet", totalExported, dictionaryTypes.Count);
                return stream.ToArray();
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "批量导出独立表字典时发生错误，表类型: {DictionaryTypes}", string.Join(",", dictionaryTypes));
                throw;
            }
        }

        /// <summary>
        /// 批量导入多个独立表字典类型（多Sheet结构）
        /// 自动识别Excel中的所有Sheet，根据Sheet名称匹配表类型
        /// 支持：pl_Countries、pl_Ports、pl_CargoRoutes、pl_Currencies、pl_FeesTypes、pl_ExchangeRates、pl_UnitConversions、pl_ShippingContainersKinds
        /// </summary>
        /// <param name="file">Excel文件</param>
        /// <param name="orgId">组织ID，用于多租户数据隔离</param>
        /// <param name="updateExisting">是否更新已存在的记录</param>
        /// <returns>导入结果</returns>
        public MultiTableImportResult ImportDictionaries(IFormFile file, Guid? orgId, bool updateExisting = true)
        {
            try
            {
                using var workbook = WorkbookFactory.Create(file.OpenReadStream());
                var result = new MultiTableImportResult();

                var supportedTypes = GetSupportedDictionaryTypes().ToDictionary(x => x.TypeName, x => x.TypeName, StringComparer.OrdinalIgnoreCase);

                // 处理每个Sheet
                for (int i = 0; i < workbook.NumberOfSheets; i++)
                {
                    var sheet = workbook.GetSheetAt(i);
                    var sheetName = sheet.SheetName;
                    
                    try
                    {
                        if (supportedTypes.TryGetValue(sheetName, out var tableType))
                        {
                            var importedCount = tableType switch
                            {
                                "PlCountry" => ImportEntityData<PlCountry>(sheet, orgId, updateExisting),
                                "PlPort" => ImportEntityData<PlPort>(sheet, orgId, updateExisting),
                                "PlCargoRoute" => ImportEntityData<PlCargoRoute>(sheet, orgId, updateExisting),
                                "PlCurrency" => ImportEntityData<PlCurrency>(sheet, orgId, updateExisting),
                                "FeesType" => ImportEntityData<FeesType>(sheet, orgId, updateExisting),
                                "PlExchangeRate" => ImportEntityData<PlExchangeRate>(sheet, orgId, updateExisting),
                                "UnitConversion" => ImportEntityData<UnitConversion>(sheet, orgId, updateExisting),
                                "ShippingContainersKind" => ImportEntityData<ShippingContainersKind>(sheet, orgId, updateExisting),
                                _ => 0
                            };
                            
                            result.SheetResults.Add(new TableImportResult
                            {
                                TableName = sheetName,
                                ImportedCount = importedCount,
                                Success = true
                            });
                            
                            result.TotalImportedCount += importedCount;
                            result.ProcessedSheets++;
                            
                            _Logger.LogInformation("导入独立表字典Sheet {TableName} 成功，共 {Count} 条记录", sheetName, importedCount);
                        }
                        else
                        {
                            result.SheetResults.Add(new TableImportResult
                            {
                                TableName = sheetName,
                                ImportedCount = 0,
                                Success = false,
                                ErrorMessage = $"不支持的表类型: {sheetName}"
                            });
                            
                            _Logger.LogWarning("导入独立表字典Sheet {TableName} 失败：不支持的表类型", sheetName);
                        }
                    }
                    catch (Exception ex)
                    {
                        result.SheetResults.Add(new TableImportResult
                        {
                            TableName = sheetName,
                            ImportedCount = 0,
                            Success = false,
                            ErrorMessage = ex.Message
                        });
                        
                        _Logger.LogWarning(ex, "导入独立表字典Sheet {TableName} 失败", sheetName);
                    }
                }

                // 批量保存所有更改
                _DbContext.SaveChanges();
                
                _Logger.LogInformation("批量导入独立表字典完成，共处理 {ProcessedSheets} 个Sheet，导入 {TotalCount} 条记录", 
                    result.ProcessedSheets, result.TotalImportedCount);

                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "批量导入独立表字典时发生错误");
                throw;
            }
        }

        #endregion

        #region 批量客户资料导入导出

        /// <summary>
        /// 批量导出客户资料表到Excel（多Sheet结构）
        /// 支持客户主表和所有客户子表
        /// 每个表类型对应一个Sheet，Sheet名称为实体类型名称
        /// 即使表无数据也会导出表头，便于客户填写数据模板
        /// </summary>
        /// <param name="tableTypes">要导出的表类型列表（可包含PlCustomer和所有客户子表）</param>
        /// <param name="orgId">组织ID，用于多租户数据隔离</param>
        /// <returns>Excel文件字节数组</returns>
        public byte[] ExportCustomerTables(List<string> tableTypes, Guid? orgId)
        {
            if (tableTypes == null || !tableTypes.Any())
                throw new ArgumentException("请至少指定一个表类型");

            try
            {
                var supportedSubTypes = GetSupportedCustomerSubTableTypes().Select(x => x.TypeName).ToHashSet(StringComparer.OrdinalIgnoreCase);
                supportedSubTypes.Add("PlCustomer"); // 添加客户主表支持
                
                using var workbook = new HSSFWorkbook();
                int totalExported = 0;

                foreach (var tableType in tableTypes)
                {
                    if (supportedSubTypes.Contains(tableType))
                    {
                        var sheet = workbook.CreateSheet(tableType);
                        var exportedCount = tableType switch
                        {
                            "PlCustomer" => ExportEntityData<PlCustomer>(sheet, orgId),
                            "PlCustomerContact" => ExportEntityData<PlCustomerContact>(sheet, orgId),
                            "PlBusinessHeader" => ExportEntityData<PlBusinessHeader>(sheet, orgId),
                            "PlTidan" => ExportEntityData<PlTidan>(sheet, orgId),
                            "CustomerBlacklist" => ExportEntityData<CustomerBlacklist>(sheet, orgId),
                            "PlLoadingAddr" => ExportEntityData<PlLoadingAddr>(sheet, orgId),
                            _ => 0
                        };
                        totalExported += exportedCount;
                        
                        _Logger.LogInformation("导出客户资料表 {TableType} 到Sheet，共 {Count} 条记录", tableType, exportedCount);
                    }
                    else
                    {
                        _Logger.LogWarning("不支持的客户资料表类型: {TableType}，跳过导出", tableType);
                    }
                }

                using var stream = new MemoryStream();
                workbook.Write(stream, true);
                workbook.Close();

                _Logger.LogInformation("批量导出客户资料表完成，共 {TotalCount} 条记录，{SheetCount} 个Sheet", totalExported, tableTypes.Count);
                return stream.ToArray();
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "批量导出客户资料表时发生错误，表类型: {TableTypes}", string.Join(",", tableTypes));
                throw;
            }
        }

        /// <summary>
        /// 批量导入客户资料表（多Sheet结构）
        /// 自动识别Excel中的所有Sheet，根据Sheet名称匹配表类型
        /// 支持客户主表和所有客户子表
        /// </summary>
        /// <param name="file">Excel文件</param>
        /// <param name="orgId">组织ID，用于多租户数据隔离</param>
        /// <param name="updateExisting">是否更新已存在的记录</param>
        /// <returns>导入结果</returns>
        public MultiTableImportResult ImportCustomerTables(IFormFile file, Guid? orgId, bool updateExisting = true)
        {
            try
            {
                using var workbook = WorkbookFactory.Create(file.OpenReadStream());
                var result = new MultiTableImportResult();

                var supportedTypes = GetSupportedCustomerSubTableTypes().ToDictionary(x => x.TypeName, x => x.TypeName, StringComparer.OrdinalIgnoreCase);
                supportedTypes.Add("PlCustomer", "PlCustomer"); // 添加客户主表支持

                // 处理每个Sheet
                for (int i = 0; i < workbook.NumberOfSheets; i++)
                {
                    var sheet = workbook.GetSheetAt(i);
                    var sheetName = sheet.SheetName;
                    
                    try
                    {
                        if (supportedTypes.TryGetValue(sheetName, out var tableType))
                        {
                            var importedCount = tableType switch
                            {
                                "PlCustomer" => ImportEntityData<PlCustomer>(sheet, orgId, updateExisting),
                                "PlCustomerContact" => ImportEntityData<PlCustomerContact>(sheet, orgId, updateExisting),
                                "PlBusinessHeader" => ImportEntityData<PlBusinessHeader>(sheet, orgId, updateExisting),
                                "PlTidan" => ImportEntityData<PlTidan>(sheet, orgId, updateExisting),
                                "CustomerBlacklist" => ImportEntityData<CustomerBlacklist>(sheet, orgId, updateExisting),
                                "PlLoadingAddr" => ImportEntityData<PlLoadingAddr>(sheet, orgId, updateExisting),
                                _ => 0
                            };
                            
                            result.SheetResults.Add(new TableImportResult
                            {
                                TableName = sheetName,
                                ImportedCount = importedCount,
                                Success = true
                            });
                            
                            result.TotalImportedCount += importedCount;
                            result.ProcessedSheets++;
                            
                            _Logger.LogInformation("导入客户资料表Sheet {TableName} 成功，共 {Count} 条记录", sheetName, importedCount);
                        }
                        else
                        {
                            result.SheetResults.Add(new TableImportResult
                            {
                                TableName = sheetName,
                                ImportedCount = 0,
                                Success = false,
                                ErrorMessage = $"不支持的表类型: {sheetName}"
                            });
                            
                            _Logger.LogWarning("导入客户资料表Sheet {TableName} 失败：不支持的表类型", sheetName);
                        }
                    }
                    catch (Exception ex)
                    {
                        result.SheetResults.Add(new TableImportResult
                        {
                            TableName = sheetName,
                            ImportedCount = 0,
                            Success = false,
                            ErrorMessage = ex.Message
                        });
                        
                        _Logger.LogWarning(ex, "导入客户资料表Sheet {TableName} 失败", sheetName);
                    }
                }

                // 批量保存所有更改
                _DbContext.SaveChanges();
                
                _Logger.LogInformation("批量导入客户资料表完成，共处理 {ProcessedSheets} 个Sheet，导入 {TotalCount} 条记录", 
                    result.ProcessedSheets, result.TotalImportedCount);

                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "批量导入客户资料表时发生错误");
                throw;
            }
        }

        #endregion

        #region 通用数据处理私有方法

        /// <summary>
        /// 导出实体数据到工作表
        /// 即使没有数据也会创建表头，便于客户填写数据模板
        /// 排除字段：Id（系统生成）、OrgId（自动设置）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="sheet">Excel工作表</param>
        /// <param name="orgId">组织ID，用于多租户数据隔离</param>
        /// <returns>导出的记录数</returns>
        private int ExportEntityData<T>(ISheet sheet, Guid? orgId) where T : class
        {
            try
            {
                var data = GetEntityDataByOrgId<T>(orgId);
                
                // 获取可导出的属性，排除Id、OrgId列和复杂类型
                var properties = typeof(T).GetProperties()
                    .Where(p => p.CanRead && 
                               (p.PropertyType.IsValueType || p.PropertyType == typeof(string)) &&
                               !p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase) &&
                               !p.Name.Equals("OrgId", StringComparison.OrdinalIgnoreCase) &&
                               !IsComplexType(p.PropertyType))
                    .ToArray();

                // 创建表头（即使没有数据也要创建表头）
                var headerRow = sheet.CreateRow(0);
                for (int i = 0; i < properties.Length; i++)
                {
                    headerRow.CreateCell(i).SetCellValue(properties[i].Name);
                }

                // 如果没有数据，记录日志但仍返回成功（已创建表头）
                if (!data.Any())
                {
                    _Logger.LogInformation("表 {EntityType} 没有数据，已导出表头模板", typeof(T).Name);
                    return 0;
                }

                // 填充数据
                for (int i = 0; i < data.Count; i++)
                {
                    var dataRow = sheet.CreateRow(i + 1);
                    var item = data[i];
                    
                    for (int j = 0; j < properties.Length; j++)
                    {
                        var value = properties[j].GetValue(item);
                        dataRow.CreateCell(j).SetCellValue(value?.ToString() ?? "");
                    }
                }

                return data.Count;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "导出实体 {EntityType} 时发生错误", typeof(T).Name);
                throw;
            }
        }

        /// <summary>
        /// 导入实体数据
        /// 自动处理Id和OrgId字段：Id生成新GUID，OrgId使用当前用户的机构ID
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="sheet">Excel工作表</param>
        /// <param name="orgId">组织ID，用于多租户数据隔离</param>
        /// <param name="updateExisting">是否更新已存在的记录</param>
        /// <returns>导入的记录数</returns>
        private int ImportEntityData<T>(ISheet sheet, Guid? orgId, bool updateExisting) where T : class, new()
        {
            if (sheet.LastRowNum < 1) return 0; // 没有数据行

            var headerRow = sheet.GetRow(0);
            if (headerRow == null) return 0;

            // 获取表头映射（排除Id、OrgId和复杂类型）
            var properties = typeof(T).GetProperties()
                .Where(p => p.CanWrite && 
                           !p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase) &&
                           !p.Name.Equals("OrgId", StringComparison.OrdinalIgnoreCase) &&
                           !IsComplexType(p.PropertyType))
                .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

            var columnMappings = new Dictionary<int, PropertyInfo>();
            for (int i = 0; i <= headerRow.LastCellNum; i++)
            {
                var cell = headerRow.GetCell(i);
                if (cell != null && !string.IsNullOrWhiteSpace(cell.StringCellValue))
                {
                    var columnName = cell.StringCellValue.Trim();
                    if (properties.ContainsKey(columnName))
                    {
                        columnMappings[i] = properties[columnName];
                    }
                }
            }

            var importedCount = 0;
            var dbSet = _DbContext.Set<T>();

            for (int rowIndex = 1; rowIndex <= sheet.LastRowNum; rowIndex++)
            {
                var row = sheet.GetRow(rowIndex);
                if (row == null) continue;

                var entity = new T();
                var hasData = false;

                // 填充实体属性（排除Id和OrgId）
                foreach (var mapping in columnMappings)
                {
                    var cell = row.GetCell(mapping.Key);
                    if (cell != null)
                    {
                        var value = GetCellValue(cell, mapping.Value.PropertyType);
                        if (value != null)
                        {
                            mapping.Value.SetValue(entity, value);
                            hasData = true;
                        }
                    }
                }

                if (!hasData) continue;

                // 自动设置Id字段：生成新的GUID
                var idProperty = typeof(T).GetProperty("Id");
                if (idProperty != null && idProperty.PropertyType == typeof(Guid))
                {
                    idProperty.SetValue(entity, Guid.NewGuid());
                }

                // 自动设置OrgId字段：使用当前登录用户的机构ID
                var orgIdProperty = typeof(T).GetProperty("OrgId");
                if (orgIdProperty != null)
                {
                    orgIdProperty.SetValue(entity, orgId);
                }

                // 检查是否需要更新现有记录
                if (updateExisting)
                {
                    var codeProperty = typeof(T).GetProperty("Code");
                    if (codeProperty != null)
                    {
                        var code = codeProperty.GetValue(entity)?.ToString();
                        if (!string.IsNullOrEmpty(code))
                        {
                            // 查找现有记录并更新
                            var existing = FindExistingEntity<T>(code, orgId);
                            if (existing != null)
                            {
                                // 更新现有记录的属性（排除Id和OrgId）
                                foreach (var mapping in columnMappings)
                                {
                                    var value = mapping.Value.GetValue(entity);
                                    mapping.Value.SetValue(existing, value);
                                }
                                continue;
                            }
                        }
                    }
                }

                dbSet.Add(entity);
                importedCount++;
            }

            return importedCount;
        }

        /// <summary>
        /// 根据Code查找现有实体
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="code">业务代码</param>
        /// <param name="orgId">组织ID</param>
        /// <returns>现有实体或null</returns>
        private T FindExistingEntity<T>(string code, Guid? orgId) where T : class
        {
            var dbSet = _DbContext.Set<T>();
            var entityType = typeof(T);

            // 构建查询条件
            var parameter = Expression.Parameter(entityType, "x");
            
            // Code条件
            var codeProperty = Expression.Property(parameter, "Code");
            var codeConstant = Expression.Constant(code);
            var codeEquals = Expression.Equal(codeProperty, codeConstant);

            Expression whereExpression = codeEquals;

            // OrgId条件（如果实体有OrgId属性）
            var orgIdProperty = entityType.GetProperty("OrgId");
            if (orgIdProperty != null)
            {
                var orgIdPropertyExpr = Expression.Property(parameter, "OrgId");
                var orgIdConstant = Expression.Constant(orgId, typeof(Guid?));
                var orgIdEquals = Expression.Equal(orgIdPropertyExpr, orgIdConstant);
                
                whereExpression = Expression.AndAlso(whereExpression, orgIdEquals);
            }

            var lambda = Expression.Lambda<Func<T, bool>>(whereExpression, parameter);
            return dbSet.FirstOrDefault(lambda);
        }

        /// <summary>
        /// 获取指定类型的实体数据
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="orgId">组织ID</param>
        /// <returns>实体数据列表</returns>
        private List<T> GetEntityDataByOrgId<T>(Guid? orgId) where T : class
        {
            try
            {
                var entityType = typeof(T);
                var query = _DbContext.Set<T>().AsNoTracking();

                // 如果实体有OrgId属性，则按OrgId过滤
                if (entityType.GetProperty("OrgId") != null)
                {
                    var parameter = Expression.Parameter(entityType, "x");
                    var property = Expression.Property(parameter, "OrgId");
                    var constant = Expression.Constant(orgId, typeof(Guid?));
                    var equals = Expression.Equal(property, constant);
                    var lambda = Expression.Lambda<Func<T, bool>>(equals, parameter);
                    
                    query = query.Where(lambda);
                }

                return query.ToList();
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "获取实体数据时发生错误，实体类型: {EntityType}", typeof(T).Name);
                throw;
            }
        }

        /// <summary>
        /// 判断是否为复杂类型（需要排除的类型）
        /// </summary>
        /// <param name="type">属性类型</param>
        /// <returns>是否为复杂类型</returns>
        private bool IsComplexType(Type type)
        {
            // 排除已过时的复杂类型
            if (type.IsClass && type != typeof(string))
            {
                var typeName = type.Name;
                return typeName.StartsWith("PlOwned") || typeName.StartsWith("Owned");
            }
            return false;
        }

        /// <summary>
        /// 获取单元格值并转换为指定类型
        /// </summary>
        /// <param name="cell">Excel单元格</param>
        /// <param name="targetType">目标类型</param>
        /// <returns>转换后的值</returns>
        private object GetCellValue(ICell cell, Type targetType)
        {
            if (cell == null) return null;

            try
            {
                var cellType = cell.CellType == CellType.Formula ? cell.CachedFormulaResultType : cell.CellType;
                
                return cellType switch
                {
                    CellType.String => ConvertValue(cell.StringCellValue, targetType),
                    CellType.Numeric => ConvertValue(cell.NumericCellValue, targetType),
                    CellType.Boolean => ConvertValue(cell.BooleanCellValue, targetType),
                    CellType.Blank => null,
                    _ => null
                };
            }
            catch (Exception ex)
            {
                _Logger.LogWarning(ex, "转换单元格值时发生错误，目标类型: {TargetType}", targetType.Name);
                return null;
            }
        }

        /// <summary>
        /// 值类型转换
        /// </summary>
        /// <param name="value">原始值</param>
        /// <param name="targetType">目标类型</param>
        /// <returns>转换后的值</returns>
        private object ConvertValue(object value, Type targetType)
        {
            if (value == null) return null;
            if (targetType == typeof(string)) return value.ToString();
            if (targetType == typeof(Guid) || targetType == typeof(Guid?))
            {
                if (Guid.TryParse(value.ToString(), out var guid))
                    return guid;
                return targetType == typeof(Guid?) ? null : Guid.NewGuid();
            }

            try
            {
                var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
                return Convert.ChangeType(value, underlyingType);
            }
            catch
            {
                return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
            }
        }

        #endregion
    }

    #region 多表导入结果类型定义

    /// <summary>
    /// 多表导入结果（通用表字典和客户资料表）
    /// </summary>
    public class MultiTableImportResult
    {
        /// <summary>
        /// 总导入记录数
        /// </summary>
        public int TotalImportedCount { get; set; }

        /// <summary>
        /// 处理的Sheet数量
        /// </summary>
        public int ProcessedSheets { get; set; }

        /// <summary>
        /// 各Sheet的导入结果
        /// </summary>
        public List<TableImportResult> SheetResults { get; set; } = new();
    }

    /// <summary>
    /// 表导入结果
    /// </summary>
    public class TableImportResult
    {
        /// <summary>
        /// 表名称（Sheet名称）
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// 导入记录数
        /// </summary>
        public int ImportedCount { get; set; }

        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// 简单字典导入结果
    /// </summary>
    public class SimpleDataDicImportResult
    {
        /// <summary>
        /// 总导入记录数
        /// </summary>
        public int TotalImportedCount { get; set; }

        /// <summary>
        /// 处理的Sheet数量
        /// </summary>
        public int ProcessedSheets { get; set; }

        /// <summary>
        /// 各Sheet的导入结果
        /// </summary>
        public List<SheetImportResult> SheetResults { get; set; } = new();
    }

    /// <summary>
    /// Sheet导入结果
    /// </summary>
    public class SheetImportResult
    {
        /// <summary>
        /// Sheet名称（Catalog Code）
        /// </summary>
        public string SheetName { get; set; }

        /// <summary>
        /// 导入记录数
        /// </summary>
        public int ImportedCount { get; set; }

        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorMessage { get; set; }
    }

    #endregion
}