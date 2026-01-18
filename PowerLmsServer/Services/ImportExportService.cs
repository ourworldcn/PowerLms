/*
 * 项目：PowerLms物流管理系统 | 模块：通用导入导出服务
 * 功能：统一的Excel导入导出处理，支持独立表字典和客户资料表的批量多Sheet处理
 * 包含表类型：
 * - 独立字典表：pl_Countries、pl_Ports、pl_CargoRoutes、pl_Currencies、pl_FeesTypes、
 *               pl_ExchangeRates、pl_UnitConversions、pl_ShippingContainersKinds、
 *               JobNumberRule、OtherNumberRule、SubjectConfiguration、DailyFeesType等
 * - 客户资料主表：pl_Customers
 * - 客户资料子表：pl_CustomerContacts、pl_BusinessHeaders、pl_Tidans、pl_CustomerBlacklists、pl_LoadingAddrs
 * 技术要点：
 * - 基于OwDataUnit + OwNpoiUnit高性能Excel处理
 * - 多租户数据隔离和权限控制
 * - 重复数据覆盖策略，依赖关系验证
 * - 批量数据库操作和查询优化
 * - 多Sheet批量处理，Sheet名称直接使用数据库表名
 * 作者：zc | 创建：2025-01-27 | 修改：2025-01-27 添加四个新的基础数据表支持
 */
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using OW.Data;
using OwExtensions.NPOI;
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
        /// 包含：PlCountry、PlPort、PlCargoRoute、PlCurrency、FeesType、PlExchangeRate、UnitConversion、ShippingContainersKind、
        ///       JobNumberRule、OtherNumberRule、SubjectConfiguration、DailyFeesType
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
        /// 包含：PlCustomerContact、PlBusinessHeader、PlTidan、CustomerBlacklist、PlLoadingAddr
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
        /// 包含：PlCountry、PlPort、PlCargoRoute、PlCurrency、FeesType、PlExchangeRate、UnitConversion、ShippingContainersKind、
        ///       JobNumberRule（工作号编码规则）、OtherNumberRule（其他编码规则）、SubjectConfiguration（财务科目配置）、DailyFeesType（日常费用种类）
        /// 明确排除：PlCustomer及其子表（属于客户资料表）
        /// </summary>
        /// <param name="typeName">实体类型名称</param>
        /// <returns>是否为独立表字典实体</returns>
        private bool IsIndependentDictionaryEntity(string typeName)
        {
            // ✅ 明确排除PlCustomer及其子表（属于客户资料表，不是独立字典表）
            if (typeName.StartsWith("PlCustomer", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            // 新增的基础数据表类型：编码规则、科目配置、费用种类
            if (typeName.Equals("JobNumberRule", StringComparison.OrdinalIgnoreCase) ||
                typeName.Equals("OtherNumberRule", StringComparison.OrdinalIgnoreCase) ||
                typeName.Equals("SubjectConfiguration", StringComparison.OrdinalIgnoreCase) ||
                typeName.Equals("DailyFeesType", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            // 原有的独立表字典实体
            return typeName.StartsWith("Pl") &&
                   (typeName.Contains("Country") ||
                    typeName.Contains("Port") ||
                    typeName.Contains("Route") ||
                    typeName.Contains("Currency") ||
                    typeName.Contains("Exchange") ||
                    typeName.Contains("Unit") ||
                    typeName.Contains("Shipping")) ||
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
                if (!string.IsNullOrWhiteSpace(comment))
                {
                    return comment;
                }
                // 回退策略：使用实体类型名称作为显示名称
                var typeName = entityType.ClrType.Name;
                _Logger.LogDebug("实体 {EntityType} 的Comment注释为空，使用类型名作为显示名称", typeName);
                return typeName;
            }
            catch (Exception ex)
            {
                _Logger.LogWarning(ex, "获取实体 {EntityType} 的注释时发生错误", entityType.ClrType.Name);
                // 异常时也返回类型名称
                return entityType.ClrType.Name;
            }
        }
        #endregion
        #region 批量独立表字典导入导出
        /// <summary>
        /// 批量导出多个独立表字典类型到Excel（多Sheet结构）
        /// 支持：PlCountry、PlPort、PlCargoRoute、PlCurrency、FeesType、PlExchangeRate、UnitConversion、ShippingContainersKind、
        ///       JobNumberRule、OtherNumberRule、SubjectConfiguration、DailyFeesType
        /// 每个表类型对应一个Sheet，Sheet名称为实体类型名称
        /// 即使表无数据也会导出表头，便于客户填写数据模板
        /// </summary>
        /// <param name="dictionaryTypes">要导出的实体类型名称列表（如：PlCountry、PlPort等）</param>
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
                            "JobNumberRule" => ExportEntityData<JobNumberRule>(sheet, orgId),
                            "OtherNumberRule" => ExportEntityData<OtherNumberRule>(sheet, orgId),
                            "SubjectConfiguration" => ExportEntityData<SubjectConfiguration>(sheet, orgId),
                            "DailyFeesType" => ExportEntityData<DailyFeesType>(sheet, orgId),
                            _ => throw new InvalidOperationException($"不支持的独立字典类型: {dictionaryType}，这可能是配置错误")
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
        /// 自动识别Excel中的所有Sheet，根据Sheet名称匹配实体类型
        /// 支持：PlCountry、PlPort、PlCargoRoute、PlCurrency、FeesType、PlExchangeRate、UnitConversion、ShippingContainersKind、
        ///       JobNumberRule、OtherNumberRule、SubjectConfiguration、DailyFeesType
        /// Excel中Sheet名称必须使用实体类型名称（如：PlCountry、PlPort等）
        /// </summary>
        /// <param name="workbook">Excel工作簿对象</param>
        /// <param name="orgId">组织ID，用于多租户数据隔离</param>
        /// <param name="updateExisting">是否更新已存在的记录</param>
        /// <returns>导入结果</returns>
        public MultiTableImportResult ImportDictionaries(IWorkbook workbook, Guid? orgId, bool updateExisting = true)
        {
            ArgumentNullException.ThrowIfNull(workbook);
            try
            {
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
                                "JobNumberRule" => ImportEntityData<JobNumberRule>(sheet, orgId, updateExisting),
                                "OtherNumberRule" => ImportEntityData<OtherNumberRule>(sheet, orgId, updateExisting),
                                "SubjectConfiguration" => ImportEntityData<SubjectConfiguration>(sheet, orgId, updateExisting),
                                "DailyFeesType" => ImportEntityData<DailyFeesType>(sheet, orgId, updateExisting),
                                _ => throw new InvalidOperationException($"不支持的独立字典类型: {tableType}，这可能是配置错误")
                            };
                            result.SheetResults.Add(new TableImportResult
                            {
                                TableName = sheetName,
                                ImportedCount = importedCount,
                                Success = true
                            });
                            result.TotalImportedCount += importedCount;
                            result.ProcessedSheets++;
                        }
                        else
                        {
                            // ✅ 特殊提示：PlCustomer及其子表应使用客户资料导入方法
                            var errorMessage = sheetName.StartsWith("PlCustomer", StringComparison.OrdinalIgnoreCase)
                                ? $"表 {sheetName} 属于客户资料表，请使用客户资料导入功能，不应出现在独立字典导入中"
                                : $"不支持的表类型: {sheetName}";
                            result.SheetResults.Add(new TableImportResult
                            {
                                TableName = sheetName,
                                ImportedCount = 0,
                                Success = false,
                                ErrorMessage = errorMessage
                            });
                            _Logger.LogWarning("导入独立表字典Sheet {TableName} 失败：{ErrorMessage}", sheetName, errorMessage);
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
                // ✅ 统一保存所有更改（与ImportSimpleDictionaries一致）
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
        /// <param name="tableTypes">要导出的实体类型名称列表（如：PlCustomer、PlCustomerContact等）</param>
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
                            _ => throw new InvalidOperationException($"不支持的客户资料表类型: {tableType}，这可能是配置错误")
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
        /// 自动识别Excel中的所有Sheet，根据Sheet名称匹配实体类型
        /// 支持客户主表和所有客户子表
        /// Excel中Sheet名称必须使用实体类型名称（如：PlCustomer、PlCustomerContact等）
        /// </summary>
        /// <param name="workbook">Excel工作簿对象</param>
        /// <param name="orgId">组织ID，用于多租户数据隔离</param>
        /// <param name="updateExisting">是否更新已存在的记录</param>
        /// <returns>导入结果</returns>
        public MultiTableImportResult ImportCustomerTables(IWorkbook workbook, Guid? orgId, bool updateExisting = true)
        {
            ArgumentNullException.ThrowIfNull(workbook);
            try
            {
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
                                _ => throw new InvalidOperationException($"不支持的客户资料表类型: {tableType}，这可能是配置错误")
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
        /// 把指定数据源填充到数据库上下文中。
        /// 重要：假定数据库上下本文地已经加载了所有相关数据，排重仅在本地进行，以增强效率。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="source"></param>
        /// <param name="existingData">已存在数据的集合</param>
        /// <param name="keySelector">计算业务键的表达式</param>
        /// <param name="isUpdate">true覆盖模式(对可以软删除的软删除后新建，否则直接覆盖)，false忽略已有数据。
        /// 软删除定义 <see cref="IMarkDelete"/>。 </param>
        public FillResult FillToDbContext<T, TKey>(
            IEnumerable<T> source,
            IEnumerable<T> existingData,
            Func<T, TKey> keySelector,
            bool isUpdate = false)
            where T : class
            where TKey : notnull
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(existingData);
            ArgumentNullException.ThrowIfNull(keySelector);
            var result = new FillResult();
            var dbSet = _DbContext.Set<T>();
            var sourceList = source as IList<T> ?? source.ToList();
            if (!sourceList.Any()) return result;
            // ✅ 使用ILookup处理可能的重复键（更容错）
            ILookup<TKey, T> existingLookup;
            try
            {
                existingLookup = existingData.AsEnumerable()  // 避免Linq to SQL问题
                    .ToLookup(keySelector);
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "构建业务键索引时发生错误，表: {EntityType}", typeof(T).Name);
                throw new InvalidOperationException($"构建业务键索引失败: {ex.Message}", ex);
            }
            // ✅ 正确识别软删除：检查是否实现IMarkDelete接口
            var supportsMarkDelete = typeof(IMarkDelete).IsAssignableFrom(typeof(T));
            foreach (var entity in sourceList)
            {
                result.TotalCount++;
                TKey key;
                try
                {
                    key = keySelector(entity);
                }
                catch (Exception ex)
                {
                    _Logger.LogWarning(ex, "无法提取实体的业务键，跳过该实体，表: {EntityType}", typeof(T).Name);
                    result.SkippedCount++;
                    continue;
                }
                if (key == null)
                {
                    _Logger.LogWarning("实体的业务键为null，跳过该实体，表: {EntityType}", typeof(T).Name);
                    result.SkippedCount++;
                    continue;
                }
                // ✅ 查找所有匹配的现有记录（可能有多个重复）
                var existingRecords = existingLookup[key];
                if (existingRecords.Any())
                {
                    // ✅ 发现重复数据，记录警告
                    if (existingRecords.Count() > 1)
                    {
                        _Logger.LogWarning("发现重复业务键 {Key}，共 {Count} 条记录，表: {EntityType}",
                            key, existingRecords.Count(), typeof(T).Name);
                    }
                    if (isUpdate)
                    {
                        // ✅ 覆盖模式：处理所有重复记录
                        foreach (var existing in existingRecords)
                        {
                            if (supportsMarkDelete)
                            {
                                // ✅ 支持软删除：标记所有重复记录为删除
                                ((IMarkDelete)existing).IsDelete = true;
                                result.DeletedCount++;
                            }
                            else
                            {
                                // ✅ 不支持软删除：仅更新第一条记录，其余物理删除
                                if (existing == existingRecords.First())
                                {
                                    _DbContext.Entry(existing).CurrentValues.SetValues(entity);
                                }
                                else
                                {
                                    // 额外的重复记录直接物理删除
                                    _DbContext.Remove(existing);
                                    _Logger.LogWarning("物理删除重复记录，业务键: {Key}，表: {EntityType}", key, typeof(T).Name);
                                }
                                result.DeletedCount++;
                            }
                        }
                        // ✅ 添加新实体（仅添加一次）
                        if (supportsMarkDelete || existingRecords.Count() > 1)
                        {
                            dbSet.Add(entity);
                        }
                        result.AddedCount++;
                    }
                    else
                    {
                        // ✅ 追加模式：跳过已存在数据
                        result.SkippedCount++;
                    }
                }
                else
                {
                    // ✅ 新数据：直接添加
                    dbSet.Add(entity);
                    result.AddedCount++;
                }
            }
            _Logger.LogInformation(
                "填充完成：总数={Total}，新增={Added}，删除={Deleted}，跳过={Skipped}，表={EntityType}",
                result.TotalCount, result.AddedCount, result.DeletedCount, result.SkippedCount, typeof(T).Name);
            return result;
        }
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
                        var propertyType = properties[j].PropertyType;
                        // 枚举类型导出为数字值，提升兼容性（支持数据库直接存储值）
                        if (value != null && propertyType.IsEnum)
                        {
                            dataRow.CreateCell(j).SetCellValue(Convert.ToInt32(value).ToString());
                        }
                        else if (value != null && Nullable.GetUnderlyingType(propertyType)?.IsEnum == true)
                        {
                            dataRow.CreateCell(j).SetCellValue(Convert.ToInt32(value).ToString());
                        }
                        else
                        {
                            dataRow.CreateCell(j).SetCellValue(value?.ToString() ?? "");
                        }
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
        /// 导入实体数据（重构版 - 使用FillToDbContext）
        /// 新策略：按业务键判断重复，支持真正的覆盖导入
        /// - 有Code属性：使用Code作为业务键
        /// - UnitConversion：使用(Basic, Rim)复合键
        /// - PlExchangeRate：无业务键，直接添加（允许同一货币对不同时间段的多个汇率）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="sheet">Excel工作表</param>
        /// <param name="orgId">组织ID，用于多租户数据隔离</param>
        /// <param name="updateExisting">true=软删除重复数据后导入（对不能软删除的对象则直接更新），false=仅导入新数据</param>
        /// <returns>导入的记录数</returns>
        private int ImportEntityData<T>(ISheet sheet, Guid? orgId, bool updateExisting) where T : class, new()
        {
            if (sheet.LastRowNum < 1)
            {
                _Logger.LogDebug("表 {EntityType} 的Sheet {SheetName} 没有数据行", typeof(T).Name, sheet.SheetName);
                return 0;
            }
            var headerRow = sheet.GetRow(0);
            if (headerRow == null)
            {
                _Logger.LogWarning("表 {EntityType} 的Sheet {SheetName} 没有标题行", typeof(T).Name, sheet.SheetName);
                return 0;
            }
            try
            {
                // ✅ 步骤1: 读取Excel数据
                var entities = ReadEntitiesFromSheet<T>(sheet, orgId);
                if (entities.Count == 0)
                {
                    _Logger.LogInformation("表 {EntityType} 没有有效数据可导入", typeof(T).Name);
                    return 0;
                }
                var entityType = typeof(T);
                var typeName = entityType.Name;
                // ✅ 步骤2: 特殊处理 - PlExchangeRate（无业务键，直接添加）
                if (typeName == "PlExchangeRate")
                {
                    // PlExchangeRate允许同一货币对不同时间段的多个汇率，直接添加
                    _DbContext.Set<T>().AddRange(entities);
                    _Logger.LogInformation("表 {EntityType} 准备导入 {Count} 条记录（无业务键去重）", typeName, entities.Count);
                    return entities.Count;
                }
                // ✅ 步骤3: 特殊处理 - UnitConversion（使用Basic+Rim复合键）
                if (typeName == "UnitConversion")
                {
                    var basicProperty = entityType.GetProperty("Basic");
                    var rimProperty = entityType.GetProperty("Rim");
                    if (basicProperty != null && rimProperty != null)
                    {
                        // 预加载现有数据
                        var existingData = GetEntityDataByOrgId<T>(orgId);
                        // 使用复合键(Basic + Rim)
                        var result = FillToDbContext(
                            entities,
                            existingData,
                            e => $"{basicProperty.GetValue(e)}|{rimProperty.GetValue(e)}",
                            updateExisting);
                        _Logger.LogInformation("表 {EntityType} 导入完成：新增{Added}条，删除{Deleted}条，跳过{Skipped}条（使用Basic+Rim复合键）",
                            typeName, result.AddedCount, result.DeletedCount, result.SkippedCount);
                        return result.AddedCount;
                    }
                }
                // ✅ 步骤4: 标准处理 - 有Code属性的实体
                var codeProperty = entityType.GetProperty("Code");
                if (codeProperty != null)
                {
                    // 预加载现有数据（仅查询一次）
                    var existingData = GetEntityDataByOrgId<T>(orgId);
                    // 使用FillToDbContext统一处理
                    var result = FillToDbContext(
                        entities,
                        existingData,
                        e => codeProperty.GetValue(e) as string ?? string.Empty,
                        updateExisting);
                    _Logger.LogInformation("表 {EntityType} 导入完成：新增{Added}条，删除{Deleted}条，跳过{Skipped}条（使用Code）",
                        typeName, result.AddedCount, result.DeletedCount, result.SkippedCount);
                    return result.AddedCount;
                }
                // ✅ 步骤5: 其他无业务键的实体，直接添加
                _DbContext.Set<T>().AddRange(entities);
                _Logger.LogInformation("表 {EntityType} 准备导入 {Count} 条记录（无业务键）", typeName, entities.Count);
                return entities.Count;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "导入实体 {EntityType} 时发生严重错误", typeof(T).Name);
                throw new InvalidOperationException($"导入表 {typeof(T).Name} 失败: {ex.Message}", ex);
            }
        }
        /// <summary>
        /// 从Excel工作表读取实体数据（使用OwNpoiExtensions扩展方法）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="sheet">Excel工作表</param>
        /// <param name="orgId">组织ID</param>
        /// <returns>实体列表</returns>
        private List<T> ReadEntitiesFromSheet<T>(ISheet sheet, Guid? orgId) where T : class, new()
        {
            // ✅ 使用OwNpoiExtensions.ReadEntities扩展方法
            var entities = new List<T>();
            // 排除Id、OrgId和计算属性（IdString、Base64IdString）
            var excludedProperties = new[] { "Id", "OrgId", "IdString", "Base64IdString" };
            try
            {
                // ✅ 调用扩展方法（自动处理列映射、属性过滤、复杂类型排除）
                sheet.ReadEntities(entities, excludedProperties);
                // ✅ 手动设置OrgId（如果实体有OrgId属性）
                var orgIdProperty = typeof(T).GetProperty("OrgId");
                if (orgIdProperty != null && orgId.HasValue)
                {
                    foreach (var entity in entities)
                    {
                        orgIdProperty.SetValue(entity, orgId);
                    }
                }
                _Logger.LogDebug("从Sheet读取 {Count} 条数据，表: {EntityType}", entities.Count, typeof(T).Name);
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "读取Sheet数据时发生错误，表: {EntityType}", typeof(T).Name);
                throw;
            }
            return entities;
        }
        /// <summary>
        /// 获取指定类型的实体数据（排除软删除数据）
        /// 支持多租户数据隔离，启用 EF Core 变更跟踪以支持后续修改操作
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="orgId">组织ID</param>
        /// <returns>实体数据列表（不包含软删除数据，启用变更跟踪）</returns>
        private List<T> GetEntityDataByOrgId<T>(Guid? orgId) where T : class
        {
            try
            {
                var entityType = typeof(T);
                // ✅ 移除 AsNoTracking()，启用 EF Core 变更跟踪
                IQueryable<T> query = _DbContext.Set<T>();
                var parameter = Expression.Parameter(entityType, "x");
                Expression? whereExpression = null;
                // ✅ 过滤1：OrgId（如果实体有OrgId属性）
                if (entityType.GetProperty("OrgId") != null)
                {
                    var orgIdProperty = Expression.Property(parameter, "OrgId");
                    var orgIdConstant = Expression.Constant(orgId, typeof(Guid?));
                    whereExpression = Expression.Equal(orgIdProperty, orgIdConstant);
                }
                // ✅ 过滤2：软删除（如果实体实现IMarkDelete接口）
                if (typeof(IMarkDelete).IsAssignableFrom(entityType))
                {
                    var isDeleteProperty = Expression.Property(parameter, "IsDelete");
                    var falseConstant = Expression.Constant(false, typeof(bool));
                    var isDeleteCondition = Expression.Equal(isDeleteProperty, falseConstant);
                    whereExpression = whereExpression == null
                        ? isDeleteCondition
                        : Expression.AndAlso(whereExpression, isDeleteCondition);
                }
                // 应用过滤条件
                if (whereExpression != null)
                {
                    var lambda = Expression.Lambda<Func<T, bool>>(whereExpression, parameter);
                    query = query.Where(lambda);
                }
                var result = query.ToList();
                _Logger.LogDebug("获取实体数据 {EntityType}，OrgId: {OrgId}，记录数: {Count}（已排除软删除，启用变更跟踪）",
                    typeof(T).Name, orgId, result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "获取实体数据时发生错误，实体类型: {EntityType}, OrgId: {OrgId}",
                    typeof(T).Name, orgId);
                throw new InvalidOperationException($"查询实体 {typeof(T).Name} 数据失败: {ex.Message}", ex);
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
        #endregion
        #region 填充结果类型定义
        /// <summary>
        /// 数据填充结果统计
        /// </summary>
        public class FillResult
        {
            /// <summary>
            /// 总数据量
            /// </summary>
            public int TotalCount { get; set; }
            /// <summary>
            /// 新增数量
            /// </summary>
            public int AddedCount { get; set; }
            /// <summary>
            /// 删除数量（软删除+物理删除）
            /// </summary>
            public int DeletedCount { get; set; }
            /// <summary>
            /// 跳过数量（追加模式下已存在的实体）
            /// </summary>
            public int SkippedCount { get; set; }
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