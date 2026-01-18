/*
 * 项目：PowerLms物流管理系统 | 模块：简单字典导入导出服务
 * 功能：SimpleDataDic表的Excel导入导出专用功能，支持多Sheet批量处理
 * 技术要点：
 * - 基于DataDicCatalog.Code的Sheet命名机制
 * - 多租户数据隔离和权限控制
 * - 批量查询优化和性能提升
 * - Sheet级别错误隔离处理
 * 作者：zc | 创建：2025-01-27 | 修改：2025-02 简化调用链，优化Id生成
 */
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using OW.Data;
using OwExtensions.NPOI;
using PowerLms.Data;
using System.Linq.Expressions;
using System.Reflection;
namespace PowerLmsServer.Services
{
    public partial class ImportExportService
    {
        #region 简单字典专用功能
        /// <summary>
        /// 获取可用的简单字典Catalog Code列表
        /// </summary>
        /// <param name="orgId">组织ID，用于多租户数据隔离</param>
        /// <returns>可用的分类代码和显示名称列表</returns>
        public List<(string Code, string DisplayName)> GetAvailableCatalogCodes(Guid? orgId)
        {
            try
            {
                var query = _DbContext.Set<DataDicCatalog>().AsNoTracking().Select(x => new { x.Code, x.DisplayName, x.OrgId });
                if (orgId.HasValue)
                {
                    query = query.Where(x => x.OrgId == orgId || x.OrgId == null);
                }
                else
                {
                    query = query.Where(x => x.OrgId == null);
                }
                var result = query.OrderBy(x => x.Code).ToList();
                return result.Select(x => (x.Code, x.DisplayName ?? x.Code)).ToList();
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "获取Catalog Code列表时发生错误，OrgId: {OrgId}", orgId);
                throw;
            }
        }
        /// <summary>
        /// 导出简单字典到Excel
        /// </summary>
        /// <param name="catalogCodes">简单字典分类代码列表</param>
        /// <param name="orgId">组织ID，用于多租户数据隔离</param>
        /// <returns>Excel文件字节数组</returns>
        public byte[] ExportSimpleDictionaries(List<string> catalogCodes, Guid? orgId)
        {
            if (catalogCodes == null || !catalogCodes.Any())
                throw new ArgumentException("请至少指定一个Catalog Code");
            var invalidChars = new char[] { '\\', '/', '?', '*', '[', ']', ':', '.' };
            foreach (var catalogCode in catalogCodes)
            {
                if (string.IsNullOrWhiteSpace(catalogCode))
                {
                    throw new ArgumentException("Catalog Code不能为空或空白字符");
                }
                if (catalogCode.Length > 31)
                {
                    throw new ArgumentException($"Catalog Code '{catalogCode}' 长度超过31个字符，Excel Sheet名称不支持");
                }
                if (catalogCode.IndexOfAny(invalidChars) >= 0)
                {
                    throw new ArgumentException($"Catalog Code '{catalogCode}' 包含非法字符。Excel Sheet名称不能包含以下字符: \\ / ? * [ ] : .");
                }
                if (catalogCode.StartsWith("'") || catalogCode.EndsWith("'"))
                {
                    throw new ArgumentException($"Catalog Code '{catalogCode}' 不能以单引号开头或结尾");
                }
            }
            try
            {
                var catalogQuery = _DbContext.Set<DataDicCatalog>().AsNoTracking().Where(x => catalogCodes.Contains(x.Code));
                if (orgId.HasValue)
                {
                    catalogQuery = catalogQuery.Where(x => x.OrgId == orgId || x.OrgId == null);
                }
                else
                {
                    catalogQuery = catalogQuery.Where(x => x.OrgId == null);
                }
                var catalogList = catalogQuery.Select(x => new { x.Code, x.Id, x.OrgId }).ToList();
                var catalogMapping = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
                foreach (var catalog in catalogList)
                {
                    if (!catalogMapping.ContainsKey(catalog.Code))
                    {
                        catalogMapping.Add(catalog.Code, catalog.Id);
                    }
                    else if (catalog.OrgId == orgId)
                    {
                        catalogMapping[catalog.Code] = catalog.Id;
                    }
                }
                _Logger.LogInformation("开始导出，传入的CatalogCodes: {CatalogCodes}", string.Join(",", catalogCodes));
                _Logger.LogInformation("数据库映射结果: {MappingCount} 个匹配项", catalogMapping.Count);
                using var workbook = new HSSFWorkbook();
                int totalExported = 0;
                int createdSheets = 0;
                foreach (var catalogCode in catalogCodes)
                {
                    _Logger.LogInformation("处理CatalogCode: {CatalogCode}", catalogCode);
                    if (catalogMapping.TryGetValue(catalogCode, out var catalogId))
                    {
                        _Logger.LogInformation("找到映射 {CatalogCode} -> {CatalogId}，创建Sheet", catalogCode, catalogId);
                        try
                        {
                            var sheet = workbook.CreateSheet(catalogCode);
                            var exportedCount = ExportSimpleDictionaryToSheet(sheet, catalogCode, catalogId, orgId);
                            totalExported += exportedCount;
                            createdSheets++;
                            _Logger.LogInformation("成功创建Sheet {CatalogCode}，导出 {Count} 条记录", catalogCode, exportedCount);
                        }
                        catch (Exception ex)
                        {
                            _Logger.LogError(ex, "创建Sheet {CatalogCode} 时发生错误", catalogCode);
                            throw;
                        }
                    }
                    else
                    {
                        _Logger.LogWarning("未找到Catalog Code: {CatalogCode}，跳过导出", catalogCode);
                    }
                }
                if (createdSheets == 0)
                {
                    _Logger.LogWarning("所有Catalog Code都无效，创建默认空Sheet");
                    var defaultSheet = workbook.CreateSheet("NoData");
                    var headerRow = defaultSheet.CreateRow(0);
                    headerRow.CreateCell(0).SetCellValue("提示");
                    var dataRow = defaultSheet.CreateRow(1);
                    dataRow.CreateCell(0).SetCellValue("未找到匹配的字典分类");
                }
                using var stream = new MemoryStream();
                workbook.Write(stream, true);
                workbook.Close();
                _Logger.LogInformation("导出简单字典完成，共 {TotalCount} 条记录，{SheetCount} 个Sheet", totalExported, createdSheets);
                return stream.ToArray();
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "导出简单字典时发生错误，Catalog Codes: {CatalogCodes}", string.Join(",", catalogCodes));
                throw;
            }
        }
        /// <summary>
        /// 从Excel导入简单字典
        /// </summary>
        /// <param name="workbook">Excel工作簿对象</param>
        /// <param name="orgId">组织ID，用于多租户数据隔离</param>
        /// <param name="createAccountId">创建人账号ID</param>
        /// <param name="updateExisting">是否更新已存在的记录</param>
        /// <returns>导入结果</returns>
        public SimpleDataDicImportResult ImportSimpleDictionaries(IWorkbook workbook, Guid? orgId, Guid? createAccountId, bool updateExisting = true)
        {
            ArgumentNullException.ThrowIfNull(workbook);
            try
            {
                var result = new SimpleDataDicImportResult();
                // 循环处理每个Sheet，使用单Sheet导入方法
                for (int i = 0; i < workbook.NumberOfSheets; i++)
                {
                    var sheet = workbook.GetSheetAt(i);
                    var sheetName = sheet.SheetName;
                    try
                    {
                        // ✅ 调用单Sheet导入方法
                        var importCount = ImportSimpleDictionaries(sheet, orgId, createAccountId, updateExisting);
                        if (importCount >= 0)
                        {
                            result.SheetResults.Add(new SheetImportResult
                            {
                                SheetName = sheetName,
                                ImportedCount = importCount,
                                Success = true
                            });
                            result.ProcessedSheets++;
                        }
                        else
                        {
                            result.SheetResults.Add(new SheetImportResult
                            {
                                SheetName = sheetName,
                                ImportedCount = 0,
                                Success = false,
                                ErrorMessage = "导入失败"
                            });
                            _Logger.LogWarning("Sheet {SheetName} 导入失败", sheetName);
                        }
                    }
                    catch (Exception ex)
                    {
                        result.SheetResults.Add(new SheetImportResult
                        {
                            SheetName = sheetName,
                            ImportedCount = 0,
                            Success = false,
                            ErrorMessage = ex.Message
                        });
                        _Logger.LogWarning(ex, "处理Sheet {SheetName} 时发生错误", sheetName);
                    }
                }
                // ✅ 统一保存所有更改（与ImportDictionaries一致）
                _DbContext.SaveChanges();
                result.TotalImportedCount = result.SheetResults.Where(r => r.Success).Sum(r => r.ImportedCount);
                _Logger.LogInformation("批量导入完成，共处理 {ProcessedSheets} 个Sheet，导入 {TotalCount} 条记录",
                   result.ProcessedSheets, result.TotalImportedCount);
                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "批量导入简单字典时发生错误");
                throw;
            }
        }
        /// <summary>
        /// 从单个Sheet导入简单字典数据（重构版 - 使用FillToDbContext）
        /// 功能：根据Sheet名称自动创建或使用DataDicCatalog，支持软删除重复数据
        /// </summary>
        /// <param name="sheet">Excel工作表，Sheet名称即为Catalog Code</param>
        /// <param name="orgId">组织ID，用于多租户数据隔离</param>
        /// <param name="createAccountId">创建人账号ID</param>
        /// <param name="updateExisting">true=软删除已有重复数据后导入新数据，false=仅新增不重复的数据</param>
        /// <returns>实际导入的记录数，失败返回-1</returns>
        public int ImportSimpleDictionaries(ISheet sheet, Guid? orgId, Guid? createAccountId, bool updateExisting = true)
        {
            ArgumentNullException.ThrowIfNull(sheet);
            try
            {
                var now = OwHelper.WorldNow;
                var catalogCode = sheet.SheetName;
                if (string.IsNullOrWhiteSpace(catalogCode))
                {
                    _Logger.LogWarning("Sheet名称为空，无法导入");
                    return -1;
                }
                // ✅ 步骤1：查找或创建 DataDicCatalog
                var catalog = _DbContext.DD_DataDicCatalogs.FirstOrDefault(x => x.OrgId == orgId && x.Code == catalogCode);
                if (catalog == null)
                {
                    catalog = new DataDicCatalog
                    {
                        Code = catalogCode,
                        DisplayName = catalogCode,
                        OrgId = orgId
                    };
                    _DbContext.DD_DataDicCatalogs.Add(catalog);
                }
                var catalogId = catalog.Id;
                // ✅ 步骤2：读取Sheet中的实体数据
                var entities = new List<SimpleDataDic>();
                // 排除 Id、DataDicId 和计算属性（IdString、Base64IdString）
                var excludedProperties = new[] { "Id", "DataDicId", "IdString", "Base64IdString" };
                sheet.ReadEntities(entities, excludedProperties);
                if (!entities.Any())
                    return 0;
                // ✅ 步骤3：设置系统字段（DataDicId、创建时间、创建人）
                foreach (var entity in entities)
                {
                    entity.DataDicId = catalogId;
                    entity.CreateDateTime = now;
                    if (createAccountId.HasValue)
                    {
                        entity.CreateAccountId = createAccountId.Value;
                    }
                }
                // ✅ 步骤4：预加载现有数据（启用变更跟踪，以支持软删除）
                var existingData = _DbContext.DD_SimpleDataDics
                    .Where(x => x.DataDicId == catalogId && !x.IsDelete)  // ✅ 过滤软删除数据
                    .ToList();
                // ✅ 步骤5：使用FillToDbContext统一处理（替换重复逻辑）
                var result = FillToDbContext(
                    entities,
                    existingData,
                    e => e.Code,
                    updateExisting);
                _Logger.LogInformation("Catalog {CatalogCode} 导入完成：新增{Added}条，删除{Deleted}条，跳过{Skipped}条",
                    catalogCode, result.AddedCount, result.DeletedCount, result.SkippedCount);
                return result.AddedCount;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "导入Sheet {SheetName} 时发生错误", sheet?.SheetName);
                return -1;
            }
        }
        #endregion
        #region 简单字典性能优化的私有方法
        /// <summary>
        /// 导出简单字典数据到指定Sheet
        /// </summary>
        /// <param name="sheet">Excel工作表</param>
        /// <param name="catalogCode">分类代码</param>
        /// <param name="catalogId">分类ID</param>
        /// <param name="orgId">组织ID</param>
        /// <returns>导出的记录数</returns>
        private int ExportSimpleDictionaryToSheet(ISheet sheet, string catalogCode, Guid catalogId, Guid? orgId)
        {
            var data = _DbContext.Set<SimpleDataDic>().AsNoTracking().Where(x => x.DataDicId == catalogId).ToList();
            var properties = typeof(SimpleDataDic).GetProperties()
                 .Where(p => p.CanRead &&
        (p.PropertyType.IsValueType || p.PropertyType == typeof(string)) &&
         !p.Name.Equals("DataDicId", StringComparison.OrdinalIgnoreCase) &&
                          !p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
              .ToArray();
            var headerRow = sheet.CreateRow(0);
            for (int i = 0; i < properties.Length; i++)
            {
                headerRow.CreateCell(i).SetCellValue(properties[i].Name);
            }
            if (!data.Any())
            {
                _Logger.LogInformation("Catalog Code {CatalogCode} 没有数据，已导出表头模板", catalogCode);
                return 0;
            }
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
        #endregion
    }
}
