/*
 * 项目：PowerLms物流管理系统 | 模块：通用导入导出服务
 * 功能：统一的Excel导入导出处理，支持字典、客户资料及其子表
 * 技术要点：
 * - 支持字典导入导出：简单字典和特殊字典
 * - 支持客户资料导入导出：主表和子表
 * - 基于OwDataUnit + OwNpoiUnit高性能Excel处理
 * - 多租户数据隔离和权限控制
 * - 重复数据覆盖策略，依赖关系验证
 * 作者：zc | 创建：2025-01-27
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
    /// 支持字典、客户资料及其子表的Excel导入导出
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Scoped)]
    public class ImportExportService
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
        /// 获取支持的字典类型列表
        /// </summary>
        /// <returns>字典类型名称和中文名称的元组集合</returns>
        public List<(string TypeName, string DisplayName)> GetSupportedDictionaryTypes()
        {
            var supportedTypes = new List<(string TypeName, string DisplayName)>();

            // 从DbContext中获取所有实体类型
            var entityTypes = _DbContext.Model.GetEntityTypes();

            foreach (var entityType in entityTypes)
            {
                var clrType = entityType.ClrType;
                var typeName = clrType.Name;
                
                // 过滤出字典类型的实体，并确保有Comment注释
                if (IsDictionaryEntity(typeName))
                {
                    var displayName = GetTableCommentFromEntityType(entityType);
                    // 只有当表有Comment注释时才加入支持列表
                    if (!string.IsNullOrEmpty(displayName))
                    {
                        supportedTypes.Add((typeName, displayName));
                    }
                    else
                    {
                        _Logger.LogWarning("字典实体 {TypeName} 缺少Comment注释，跳过导入导出支持", typeName);
                    }
                }
            }

            return supportedTypes.OrderBy(x => x.TypeName).ToList();
        }

        /// <summary>
        /// 获取支持的客户资料子表类型列表
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
        /// 获取支持的简单字典分类列表
        /// </summary>
        /// <returns>分类代码和中文名称的元组集合</returns>
        public List<(string CategoryCode, string DisplayName)> GetSimpleDictionaryCategories(Guid? orgId)
        {
            // 简单字典作为整体，返回固定项
            return new List<(string, string)>
            {
                ("SimpleDataDic", "简单数据字典")
            };
        }

        /// <summary>
        /// 验证导入导出类型是否支持
        /// </summary>
        public bool IsSupportedImportExportType(string typeName)
        {
            var supportedTypes = GetSupportedDictionaryTypes()
                .Concat(GetSupportedCustomerSubTableTypes())
                .Select(x => x.TypeName);
            
            return supportedTypes.Any(x => x.Equals(typeName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 验证简单字典分类是否存在
        /// </summary>
        public bool IsValidSimpleDictionaryCategory(string categoryCode, Guid? orgId)
        {
            return _DbContext.Set<DataDicCatalog>()
                .Any(x => x.Code == categoryCode && (x.OrgId == orgId || x.OrgId == null));
        }

        /// <summary>
        /// 判断是否为字典实体
        /// </summary>
        private bool IsDictionaryEntity(string typeName)
        {
            // 字典实体通常以Pl开头或包含Dictionary、DataDic等关键字
            return typeName.StartsWith("Pl") && 
                   (typeName.Contains("Country") || 
                    typeName.Contains("Port") || 
                    typeName.Contains("Route") || 
                    typeName.Contains("Currency") || 
                    typeName.Contains("Exchange") || 
                    typeName.Contains("Unit") ||
                    typeName.Contains("Shipping") ||
                    typeName.Equals("FeesType", StringComparison.OrdinalIgnoreCase) ||
                    typeName.Equals("PlCustomer", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 判断是否为客户资料子表实体
        /// </summary>
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

        /// <summary>
        /// 从EF Core实体类型获取表的Comment注释
        /// </summary>
        /// <param name="typeName">实体类型名</param>
        /// <returns>表的中文注释，如果未找到则返回null</returns>
        private string GetTableCommentFromDatabase(string typeName)
        {
            try
            {
                // 从DbContext的Model中查找对应的实体类型
                var entityType = _DbContext.Model.GetEntityTypes()
                    .FirstOrDefault(e => e.ClrType.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase));

                if (entityType == null)
                {
                    _Logger.LogWarning("未找到实体类型: {TypeName}", typeName);
                    return null;
                }

                // 从EF Core的元数据中获取表注释
                return entityType.GetComment();
            }
            catch (Exception ex)
            {
                _Logger.LogWarning(ex, "获取实体 {TypeName} 的注释时发生错误", typeName);
                return null;
            }
        }

        #endregion

        #region 字典导入导出

        /// <summary>
        /// 导出单一字典类型到Excel
        /// </summary>
        public byte[] ExportDictionary(string dictionaryType, Guid? orgId)
        {
            if (!GetSupportedDictionaryTypes().Any(x => x.TypeName.Equals(dictionaryType, StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException($"不支持的字典类型: {dictionaryType}");

            using var workbook = new HSSFWorkbook();
            var sheet = workbook.CreateSheet(dictionaryType);

            int exportedCount = dictionaryType switch
            {
                "PlCountry" => ExportEntityData<PlCountry>(sheet, orgId),
                "PlPort" => ExportEntityData<PlPort>(sheet, orgId),
                "PlCargoRoute" => ExportEntityData<PlCargoRoute>(sheet, orgId),
                "PlCurrency" => ExportEntityData<PlCurrency>(sheet, orgId),
                "FeesType" => ExportEntityData<FeesType>(sheet, orgId),
                "PlExchangeRate" => ExportEntityData<PlExchangeRate>(sheet, orgId),
                "UnitConversion" => ExportEntityData<UnitConversion>(sheet, orgId),
                "ShippingContainersKind" => ExportEntityData<ShippingContainersKind>(sheet, orgId),
                "PlCustomer" => ExportEntityData<PlCustomer>(sheet, orgId),
                _ => throw new ArgumentException($"字典类型处理逻辑缺失: {dictionaryType}")
            };

            using var stream = new MemoryStream();
            workbook.Write(stream, true);
            workbook.Close();
            
            _Logger.LogInformation("导出字典 {DictionaryType} 完成，共 {Count} 条记录", dictionaryType, exportedCount);
            return stream.ToArray();
        }

        /// <summary>
        /// 导出简单字典到Excel
        /// </summary>
        public byte[] ExportSimpleDictionary(Guid? orgId)
        {
            using var workbook = new HSSFWorkbook();
            var sheet = workbook.CreateSheet("SimpleDataDic");

            var exportedCount = ExportSimpleDictionaryData(sheet, orgId);

            using var stream = new MemoryStream();
            workbook.Write(stream, true);
            workbook.Close();

            _Logger.LogInformation("导出简单字典完成，共 {Count} 条记录", exportedCount);
            return stream.ToArray();
        }

        /// <summary>
        /// 导入单一字典类型
        /// </summary>
        public (int ImportedCount, List<string> Details) ImportDictionary(
            IFormFile file, string dictionaryType, Guid? orgId, bool updateExisting = true)
        {
            if (!GetSupportedDictionaryTypes().Any(x => x.TypeName.Equals(dictionaryType, StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException($"不支持的字典类型: {dictionaryType}");

            using var workbook = WorkbookFactory.Create(file.OpenReadStream());
            var sheet = workbook.GetSheetAt(0); // 取第一个工作表

            var details = new List<string>();
            int importedCount = 0;

            try
            {
                importedCount = dictionaryType switch
                {
                    "PlCountry" => ImportEntityData<PlCountry>(sheet, orgId, updateExisting),
                    "PlPort" => ImportEntityData<PlPort>(sheet, orgId, updateExisting),
                    "PlCargoRoute" => ImportEntityData<PlCargoRoute>(sheet, orgId, updateExisting),
                    "PlCurrency" => ImportEntityData<PlCurrency>(sheet, orgId, updateExisting),
                    "FeesType" => ImportEntityData<FeesType>(sheet, orgId, updateExisting),
                    "PlExchangeRate" => ImportEntityData<PlExchangeRate>(sheet, orgId, updateExisting),
                    "UnitConversion" => ImportEntityData<UnitConversion>(sheet, orgId, updateExisting),
                    "ShippingContainersKind" => ImportEntityData<ShippingContainersKind>(sheet, orgId, updateExisting),
                    "PlCustomer" => ImportEntityData<PlCustomer>(sheet, orgId, updateExisting),
                    _ => throw new ArgumentException($"字典类型处理逻辑缺失: {dictionaryType}")
                };

                _DbContext.SaveChanges();
                details.Add($"成功导入 {dictionaryType}：{importedCount} 条记录");
                
                _Logger.LogInformation("导入字典 {DictionaryType} 完成，共 {Count} 条记录", dictionaryType, importedCount);
            }
            catch (Exception ex)
            {
                details.Add($"导入 {dictionaryType} 失败：{ex.Message}");
                _Logger.LogError(ex, "导入字典 {DictionaryType} 时发生错误", dictionaryType);
                throw;
            }

            return (importedCount, details);
        }

        /// <summary>
        /// 导入简单字典
        /// </summary>
        public (int ImportedCount, List<string> Details) ImportSimpleDictionary(
            IFormFile file, Guid? orgId, bool updateExisting = true)
        {
            using var workbook = WorkbookFactory.Create(file.OpenReadStream());
            var sheet = workbook.GetSheetAt(0);

            var details = new List<string>();
            int importedCount = 0;

            try
            {
                importedCount = ImportSimpleDictionaryData(sheet, orgId, updateExisting);
                _DbContext.SaveChanges();
                details.Add($"成功导入简单字典：{importedCount} 条记录");
                
                _Logger.LogInformation("导入简单字典完成，共 {Count} 条记录", importedCount);
            }
            catch (Exception ex)
            {
                details.Add($"导入简单字典失败：{ex.Message}");
                _Logger.LogError(ex, "导入简单字典时发生错误");
                throw;
            }

            return (importedCount, details);
        }

        #endregion

        #region 客户资料导入导出

        /// <summary>
        /// 导出客户资料主表到Excel
        /// </summary>
        public byte[] ExportCustomers(Guid? orgId)
        {
            using var workbook = new HSSFWorkbook();
            var sheet = workbook.CreateSheet("PlCustomer");

            int exportedCount = ExportEntityData<PlCustomer>(sheet, orgId);

            using var stream = new MemoryStream();
            workbook.Write(stream, true);
            workbook.Close();
            
            _Logger.LogInformation("导出客户资料主表完成，共 {Count} 条记录", exportedCount);
            return stream.ToArray();
        }

        /// <summary>
        /// 导出客户资料子表到Excel
        /// </summary>
        public byte[] ExportCustomerSubTable(string subTableType, Guid? orgId)
        {
            if (!GetSupportedCustomerSubTableTypes().Any(x => x.TypeName.Equals(subTableType, StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException($"不支持的客户子表类型: {subTableType}");

            using var workbook = new HSSFWorkbook();
            var sheet = workbook.CreateSheet(subTableType);

            int exportedCount = subTableType switch
            {
                "PlCustomerContact" => ExportEntityData<PlCustomerContact>(sheet, orgId),
                "PlBusinessHeader" => ExportEntityData<PlBusinessHeader>(sheet, orgId),
                "PlTidan" => ExportEntityData<PlTidan>(sheet, orgId),
                "CustomerBlacklist" => ExportEntityData<CustomerBlacklist>(sheet, orgId),
                "PlLoadingAddr" => ExportEntityData<PlLoadingAddr>(sheet, orgId),
                _ => throw new ArgumentException($"客户子表类型处理逻辑缺失: {subTableType}")
            };

            using var stream = new MemoryStream();
            workbook.Write(stream, true);
            workbook.Close();
            
            _Logger.LogInformation("导出客户子表 {SubTableType} 完成，共 {Count} 条记录", subTableType, exportedCount);
            return stream.ToArray();
        }

        /// <summary>
        /// 导入客户资料主表
        /// </summary>
        public (int ImportedCount, List<string> Details) ImportCustomers(
            IFormFile file, Guid? orgId, bool updateExisting = true)
        {
            using var workbook = WorkbookFactory.Create(file.OpenReadStream());
            var sheet = workbook.GetSheetAt(0);

            var details = new List<string>();
            int importedCount = 0;

            try
            {
                importedCount = ImportEntityData<PlCustomer>(sheet, orgId, updateExisting);
                _DbContext.SaveChanges();
                details.Add($"成功导入客户资料主表：{importedCount} 条记录");
                
                _Logger.LogInformation("导入客户资料主表完成，共 {Count} 条记录", importedCount);
            }
            catch (Exception ex)
            {
                details.Add($"导入客户资料主表失败：{ex.Message}");
                _Logger.LogError(ex, "导入客户资料主表时发生错误");
                throw;
            }

            return (importedCount, details);
        }

        /// <summary>
        /// 导入客户资料子表
        /// </summary>
        public (int ImportedCount, List<string> Details) ImportCustomerSubTable(
            IFormFile file, string subTableType, Guid? orgId, bool updateExisting = true)
        {
            if (!GetSupportedCustomerSubTableTypes().Any(x => x.TypeName.Equals(subTableType, StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException($"不支持的客户子表类型: {subTableType}");

            using var workbook = WorkbookFactory.Create(file.OpenReadStream());
            var sheet = workbook.GetSheetAt(0);

            var details = new List<string>();
            int importedCount = 0;

            try
            {
                importedCount = subTableType switch
                {
                    "PlCustomerContact" => ImportEntityData<PlCustomerContact>(sheet, orgId, updateExisting),
                    "PlBusinessHeader" => ImportEntityData<PlBusinessHeader>(sheet, orgId, updateExisting),
                    "PlTidan" => ImportEntityData<PlTidan>(sheet, orgId, updateExisting),
                    "CustomerBlacklist" => ImportEntityData<CustomerBlacklist>(sheet, orgId, updateExisting),
                    "PlLoadingAddr" => ImportEntityData<PlLoadingAddr>(sheet, orgId, updateExisting),
                    _ => throw new ArgumentException($"客户子表类型处理逻辑缺失: {subTableType}")
                };

                _DbContext.SaveChanges();
                details.Add($"成功导入客户子表 {subTableType}：{importedCount} 条记录");
                
                _Logger.LogInformation("导入客户子表 {SubTableType} 完成，共 {Count} 条记录", subTableType, importedCount);
            }
            catch (Exception ex)
            {
                details.Add($"导入客户子表 {subTableType} 失败：{ex.Message}");
                _Logger.LogError(ex, "导入客户子表 {SubTableType} 时发生错误", subTableType);
                throw;
            }

            return (importedCount, details);
        }

        #endregion

        #region 私有辅助方法

        /// <summary>
        /// 导出实体数据到工作表
        /// </summary>
        private int ExportEntityData<T>(ISheet sheet, Guid? orgId) where T : class
        {
            try
            {
                var data = GetEntityDataByOrgId<T>(orgId);
                
                // 获取可导出的属性，排除OrgId列和复杂类型
                var properties = typeof(T).GetProperties()
                    .Where(p => p.CanRead && 
                               (p.PropertyType.IsValueType || p.PropertyType == typeof(string)) &&
                               !p.Name.Equals("OrgId", StringComparison.OrdinalIgnoreCase) &&
                               !IsComplexType(p.PropertyType))
                    .ToArray();

                // 创建表头
                var headerRow = sheet.CreateRow(0);
                for (int i = 0; i < properties.Length; i++)
                {
                    headerRow.CreateCell(i).SetCellValue(properties[i].Name);
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
        /// 导出简单字典数据到工作表
        /// </summary>
        private int ExportSimpleDictionaryData(ISheet sheet, Guid? orgId)
        {
            try
            {
                // 查询所有简单字典数据，通过DataDicCatalog关联过滤组织
                var data = _DbContext.Set<SimpleDataDic>()
                    .AsNoTracking()
                    .Join(_DbContext.Set<DataDicCatalog>().Where(c => c.OrgId == orgId || c.OrgId == null),
                          sdd => sdd.DataDicId,
                          catalog => catalog.Id,
                          (sdd, catalog) => new { SimpleDataDic = sdd, Catalog = catalog })
                    .Select(x => x.SimpleDataDic)
                    .ToList();

                // SimpleDataDic的属性，排除DataDicId
                var properties = typeof(SimpleDataDic).GetProperties()
                    .Where(p => p.CanRead && 
                               (p.PropertyType.IsValueType || p.PropertyType == typeof(string)) &&
                               !p.Name.Equals("DataDicId", StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                // 创建表头
                var headerRow = sheet.CreateRow(0);
                for (int i = 0; i < properties.Length; i++)
                {
                    headerRow.CreateCell(i).SetCellValue(properties[i].Name);
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
                _Logger.LogError(ex, "导出简单字典数据时发生错误");
                throw;
            }
        }

        /// <summary>
        /// 导入实体数据
        /// </summary>
        private int ImportEntityData<T>(ISheet sheet, Guid? orgId, bool updateExisting) where T : class, new()
        {
            if (sheet.LastRowNum < 1) return 0; // 没有数据行

            var headerRow = sheet.GetRow(0);
            if (headerRow == null) return 0;

            // 获取表头映射
            var properties = typeof(T).GetProperties()
                .Where(p => p.CanWrite && !IsComplexType(p.PropertyType))
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

                // 填充实体属性
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

                // 强制设置OrgId
                var orgIdProperty = typeof(T).GetProperty("OrgId");
                if (orgIdProperty != null)
                {
                    orgIdProperty.SetValue(entity, orgId);
                }

                // 设置Id
                var idProperty = typeof(T).GetProperty("Id");
                if (idProperty != null && idProperty.PropertyType == typeof(Guid))
                {
                    idProperty.SetValue(entity, Guid.NewGuid());
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
                                // 更新现有记录的属性
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
        /// 导入简单字典数据
        /// </summary>
        private int ImportSimpleDictionaryData(ISheet sheet, Guid? orgId, bool updateExisting)
        {
            if (sheet.LastRowNum < 1) return 0;

            var headerRow = sheet.GetRow(0);
            if (headerRow == null) return 0;

            var properties = typeof(SimpleDataDic).GetProperties()
                .Where(p => p.CanWrite && !p.Name.Equals("DataDicId", StringComparison.OrdinalIgnoreCase))
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
            var dbSet = _DbContext.Set<SimpleDataDic>();

            // 获取或创建默认的数据字典目录
            var defaultCatalog = _DbContext.Set<DataDicCatalog>()
                .FirstOrDefault(x => x.Code == "Default" && (x.OrgId == orgId || x.OrgId == null));
            
            if (defaultCatalog == null)
            {
                defaultCatalog = new DataDicCatalog
                {
                    Id = Guid.NewGuid(),
                    Code = "Default",
                    DisplayName = "默认字典分类",
                    OrgId = orgId
                };
                _DbContext.Set<DataDicCatalog>().Add(defaultCatalog);
                _DbContext.SaveChanges();
            }

            for (int rowIndex = 1; rowIndex <= sheet.LastRowNum; rowIndex++)
            {
                var row = sheet.GetRow(rowIndex);
                if (row == null) continue;

                var entity = new SimpleDataDic();
                var hasData = false;

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

                // 强制设置DataDicId为默认目录
                entity.DataDicId = defaultCatalog.Id;
                entity.Id = Guid.NewGuid();
                entity.CreateDateTime = DateTime.Now;

                // 检查是否需要更新现有记录
                if (updateExisting && !string.IsNullOrEmpty(entity.Code))
                {
                    var existing = dbSet
                        .FirstOrDefault(x => x.Code == entity.Code);
                    if (existing != null)
                    {
                        // 更新现有记录
                        foreach (var mapping in columnMappings)
                        {
                            var value = mapping.Value.GetValue(entity);
                            mapping.Value.SetValue(existing, value);
                        }
                        continue;
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

        #endregion 私有辅助方法
    }
}