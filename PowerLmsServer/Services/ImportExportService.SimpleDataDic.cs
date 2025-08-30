/*
 * 项目：PowerLms物流管理系统 | 模块：简单字典导入导出服务
 * 功能：SimpleDataDic表的Excel导入导出专用功能，支持多Sheet批量处理
 * 技术要点：
 * - 基于DataDicCatalog.Code的Sheet命名机制
 * - 多租户数据隔离和权限控制
 * - 批量查询优化和性能提升
 * - Sheet级别错误隔离处理
 * 作者：zc | 创建：2025-01-27
 */

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using PowerLms.Data;
using System.Reflection;

namespace PowerLmsServer.Services
{
    /// <summary>
    /// 导入导出服务 - 简单字典专用功能
    /// 处理SimpleDataDic表的Excel导入导出，支持基于DataDicCatalog.Code的多Sheet结构
    /// </summary>
    public partial class ImportExportService
    {
        #region 简单字典专用功能

        /// <summary>
        /// 获取可用的简单字典Catalog Code列表
        /// 性能优化：使用AsNoTracking和投影查询
        /// 
        /// 多租户逻辑：
        /// - 有组织ID：返回当前组织 + 全局数据 (OrgId == orgId || OrgId == null)
        /// - 无组织ID：仅返回全局数据 (OrgId == null)
        /// 
        /// 返回格式：List{(Code, DisplayName)} 按Code排序
        /// </summary>
        /// <param name="orgId">组织ID，用于多租户数据隔离</param>
        /// <returns>可用的分类代码和显示名称，来源于ow_DataDicCatalogs表</returns>
        public List<(string Code, string DisplayName)> GetAvailableCatalogCodes(Guid? orgId)
        {
            try
            {
                // 性能优化：使用AsNoTracking和Select投影，只查询需要的字段
                var query = _DbContext.Set<DataDicCatalog>()
                    .AsNoTracking()
                    .Select(x => new { x.Code, x.DisplayName, x.OrgId });
                
                // 多租户数据隔离：获取当前组织或全局的字典目录
                if (orgId.HasValue)
                {
                    query = query.Where(x => x.OrgId == orgId || x.OrgId == null);
                }
                else
                {
                    query = query.Where(x => x.OrgId == null);
                }

                // 一次性获取结果并在内存中处理
                var result = query
                    .OrderBy(x => x.Code)
                    .ToList();

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
        /// 性能优化：批量查询和流式Excel生成
        /// 
        /// Excel文件结构：
        /// - 多Sheet：每个Catalog Code对应一个Sheet
        /// - Sheet命名：Sheet名称 = 传入的Catalog Code值（必须是DataDicCatalog.Code字段的有效值）
        /// - 列结构：SimpleDataDic的所有字段（排除DataDicId和Id）
        /// - 数据来源：通过DataDicId关联查询ow_SimpleDataDics表
        /// - 空数据支持：即使Catalog Code有效但无数据，也导出包含表头的Sheet
        /// 
        /// 批量处理逻辑：
        /// 1. 批量查询所有Catalog Code的映射关系 (避免N+1查询)
        /// 2. 为每个有效的Catalog Code创建对应Sheet
        /// 3. 自动跳过不存在的Catalog Code，记录警告日志
        /// 4. 流式写入Excel，减少内存占用
        /// 
        /// 性能特点：
        /// - AsNoTracking查询避免EF Change Tracking开销
        /// - 批量映射查询减少数据库往返次数
        /// - 预计算属性信息避免重复反射
        /// </summary>
        /// <param name="catalogCodes">
        /// 简单字典分类代码列表，必须是数据库表DD_DataDicCatalogs.Code字段的有效值。
        /// 这些是业务上的字典分类代码，比如：
        /// - "CARGOTYPE"（货物类型）
        /// - "PackType"（包装方式）  
        /// - "AddedTaxType"（增值税类型）
        /// 注意：不是实体名称，不是表名，也不是独立表字典（如PlCountry、PlPort、PlCurrency等）
        /// </param>
        /// <param name="orgId">组织ID，用于多租户数据隔离</param>
        /// <returns>Excel文件字节数组，文件名格式：SimpleDataDic_[Code1]_[Code2]_[DateTime].xls</returns>
        public byte[] ExportSimpleDictionaries(List<string> catalogCodes, Guid? orgId)
        {
            if (catalogCodes == null || !catalogCodes.Any())
                throw new ArgumentException("请至少指定一个Catalog Code");

            // 验证Catalog Code的合法性 - Excel Sheet名称不能包含非法字符
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
                // 性能优化：批量查询所有需要的DataDicCatalog，避免多次数据库查询
                var catalogMapping = GetCatalogMappingBatch(catalogCodes, orgId);
                
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

                // 如果没有创建任何Sheet，创建一个默认的空Sheet以确保Excel文件格式正确
                if (createdSheets == 0)
                {
                    _Logger.LogWarning("所有Catalog Code都无效，创建默认空Sheet");
                    var defaultSheet = workbook.CreateSheet("NoData");
                    var headerRow = defaultSheet.CreateRow(0);
                    headerRow.CreateCell(0).SetCellValue("提示");
                    var dataRow = defaultSheet.CreateRow(1);
                    dataRow.CreateCell(0).SetCellValue("未找到匹配的字典分类");
                }

                // 性能优化：流式写入内存流
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
        /// 性能优化：批量数据库操作和预先缓存映射关系
        /// 
        /// Excel识别逻辑：
        /// - 自动遍历Excel中的所有Sheet
        /// - Sheet名称作为Catalog Code进行匹配（如"COUNTRY"、"PORT"）
        /// - 只处理能匹配到有效DataDicCatalog的Sheet
        /// 
        /// 导入处理流程：
        /// 1. 预先批量查询所有Sheet名称对应的Catalog映射
        /// 2. 预计算SimpleDataDic属性映射，避免重复反射
        /// 3. 逐Sheet处理，单个Sheet失败不影响其他Sheet
        /// 4. 批量数据库操作，最后统一SaveChanges
        /// 
        /// 数据转换逻辑：
        /// - Sheet名称(Catalog Code) → 查询ow_DataDicCatalogs → 获取Id → 设置SimpleDataDic.DataDicId
        /// - Excel列标题与SimpleDataDic属性名称匹配（忽略大小写）
        /// - 自动设置Id = Guid.NewGuid(), CreateDateTime = DateTime.Now
        /// 
        /// 更新策略：
        /// - updateExisting=true：基于Code字段匹配现有记录进行更新
        /// - updateExisting=false：仅新增，不更新现有记录
        /// 
        /// 错误处理：
        /// - Sheet级别错误隔离，失败的Sheet不影响成功的Sheet
        /// - 详细的错误信息和成功统计
        /// - 完整的导入结果反馈（总记录数、成功Sheet数、详细结果）
        /// </summary>
        /// <param name="file">Excel文件，必须包含以Catalog Code命名的Sheet</param>
        /// <param name="orgId">组织ID，用于多租户数据隔离</param>
        /// <param name="updateExisting">是否更新已存在的记录，基于Code字段匹配</param>
        /// <returns>导入结果，包含总数、成功Sheet数和详细信息</returns>
        public SimpleDataDicImportResult ImportSimpleDictionaries(IFormFile file, Guid? orgId, bool updateExisting = true)
        {
            try
            {
                using var workbook = WorkbookFactory.Create(file.OpenReadStream());
                var result = new SimpleDataDicImportResult();

                // 性能优化：预先获取所有Sheet名称对应的Catalog映射
                var sheetNames = new List<string>();
                for (int i = 0; i < workbook.NumberOfSheets; i++)
                {
                    sheetNames.Add(workbook.GetSheetAt(i).SheetName);
                }
                var catalogMapping = GetCatalogMappingBatch(sheetNames, orgId);

                // 性能优化：获取属性映射信息，避免重复反射
                var propertyMappings = GetSimpleDataDicPropertyMappings();

                // 处理每个Sheet
                for (int i = 0; i < workbook.NumberOfSheets; i++)
                {
                    var sheet = workbook.GetSheetAt(i);
                    var sheetName = sheet.SheetName;
                    
                    try
                    {
                        if (catalogMapping.TryGetValue(sheetName, out var catalogId))
                        {
                            var importedCount = ImportSimpleDictionaryFromSheet(sheet, sheetName, catalogId, orgId, updateExisting, propertyMappings);
                            
                            result.SheetResults.Add(new SheetImportResult
                            {
                                SheetName = sheetName,
                                ImportedCount = importedCount,
                                Success = true
                            });
                            
                            result.TotalImportedCount += importedCount;
                            result.ProcessedSheets++;
                            
                            _Logger.LogInformation("导入简单字典Sheet {SheetName} 成功，共 {Count} 条记录", sheetName, importedCount);
                        }
                        else
                        {
                            result.SheetResults.Add(new SheetImportResult
                            {
                                SheetName = sheetName,
                                ImportedCount = 0,
                                Success = false,
                                ErrorMessage = $"未找到对应的Catalog Code: {sheetName}"
                            });
                            
                            _Logger.LogWarning("导入简单字典Sheet {SheetName} 失败：未找到对应的Catalog Code", sheetName);
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
                        
                        _Logger.LogWarning(ex, "导入简单字典Sheet {SheetName} 失败", sheetName);
                    }
                }

                // 性能优化：批量保存所有更改
                _DbContext.SaveChanges();
                
                _Logger.LogInformation("导入简单字典完成，共处理 {ProcessedSheets} 个Sheet，导入 {TotalCount} 条记录", 
                    result.ProcessedSheets, result.TotalImportedCount);

                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "导入简单字典时发生错误");
                throw;
            }
        }

        #endregion

        #region 简单字典性能优化的私有方法

        /// <summary>
        /// 批量获取Catalog Code到ID的映射
        /// 性能优化：一次查询获取所有需要的映射关系，避免N+1查询问题
        /// </summary>
        /// <param name="catalogCodes">Catalog Code列表</param>
        /// <param name="orgId">组织ID，用于多租户数据隔离</param>
        /// <returns>Catalog Code到DataDicCatalog.Id的映射字典</returns>
        private Dictionary<string, Guid> GetCatalogMappingBatch(List<string> catalogCodes, Guid? orgId)
        {
            var query = _DbContext.Set<DataDicCatalog>()
                .AsNoTracking()
                .Where(x => catalogCodes.Contains(x.Code));
            
            // 多租户数据隔离
            if (orgId.HasValue)
            {
                query = query.Where(x => x.OrgId == orgId || x.OrgId == null);
            }
            else
            {
                query = query.Where(x => x.OrgId == null);
            }

            // 性能优化：只查询需要的字段，避免加载整个实体
            return query
                .Select(x => new { x.Code, x.Id })
                .ToDictionary(x => x.Code, x => x.Id);
        }

        /// <summary>
        /// 获取简单字典属性映射信息，避免重复反射
        /// 性能优化：预先计算属性映射，避免每次导入时重复反射
        /// 排除字段：DataDicId（通过Sheet名称自动设置）、Id（自动生成）
        /// 修复：使用 StringComparer.OrdinalIgnoreCase 避免重复键错误
        /// </summary>
        /// <returns>属性名称到PropertyInfo的映射字典</returns>
        private Dictionary<string, PropertyInfo> GetSimpleDataDicPropertyMappings()
        {
            var properties = typeof(SimpleDataDic).GetProperties()
                .Where(p => p.CanWrite && 
                           !p.Name.Equals("DataDicId", StringComparison.OrdinalIgnoreCase) &&
                           !p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase));

            var result = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);
            
            foreach (var property in properties)
            {
                try
                {
                    // 检查是否已存在同名属性（忽略大小写），避免重复键错误
                    if (!result.ContainsKey(property.Name))
                    {
                        result.Add(property.Name, property);
                    }
                    else
                    {
                        _Logger.LogWarning("发现重复属性名称: {PropertyName}，已跳过", property.Name);
                    }
                }
                catch (ArgumentException ex)
                {
                    _Logger.LogError(ex, "添加属性映射时发生错误: {PropertyName}", property.Name);
                    throw new InvalidOperationException($"SimpleDataDic类中存在重复的属性名称: {property.Name}", ex);
                }
            }

            return result;
        }

        /// <summary>
        /// 导出简单字典数据到指定Sheet
        /// 性能优化：使用AsNoTracking和流式处理
        /// 即使没有数据也会创建表头，便于客户填写数据模板
        /// </summary>
        /// <param name="sheet">Excel工作表</param>
        /// <param name="catalogCode">分类代码，用于日志记录</param>
        /// <param name="catalogId">分类ID，用于查询ow_SimpleDataDics表</param>
        /// <param name="orgId">组织ID，用于多租户数据隔离</param>
        /// <returns>导出的记录数</returns>
        private int ExportSimpleDictionaryToSheet(ISheet sheet, string catalogCode, Guid catalogId, Guid? orgId)
        {
            // 性能优化：使用AsNoTracking查询，避免EF Change Tracking开销
            var data = _DbContext.Set<SimpleDataDic>()
                .AsNoTracking()
                .Where(x => x.DataDicId == catalogId)
                .ToList();

            // 性能优化：预先计算要导出的属性，避免重复反射
            var properties = typeof(SimpleDataDic).GetProperties()
                .Where(p => p.CanRead && 
                           (p.PropertyType.IsValueType || p.PropertyType == typeof(string)) &&
                           !p.Name.Equals("DataDicId", StringComparison.OrdinalIgnoreCase) &&
                           !p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
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
                _Logger.LogInformation("Catalog Code {CatalogCode} 没有数据，已导出表头模板", catalogCode);
                return 0;
            }

            // 性能优化：批量填充数据，减少单独的Cell操作
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

        /// <summary>
        /// 从Sheet导入简单字典数据
        /// 性能优化：批量数据库操作和预计算的属性映射
        /// </summary>
        /// <param name="sheet">Excel工作表</param>
        /// <param name="catalogCode">分类代码，用于日志记录</param>
        /// <param name="catalogId">分类ID，设置到SimpleDataDic.DataDicId</param>
        /// <param name="orgId">组织ID，用于多租户数据隔离</param>
        /// <param name="updateExisting">是否更新已存在的记录</param>
        /// <param name="propertyMappings">预计算的属性映射</param>
        /// <returns>导入的记录数</returns>
        private int ImportSimpleDictionaryFromSheet(ISheet sheet, string catalogCode, Guid catalogId, Guid? orgId, bool updateExisting, Dictionary<string, PropertyInfo> propertyMappings)
        {
            if (sheet.LastRowNum < 1) return 0; // 没有数据行

            var headerRow = sheet.GetRow(0);
            if (headerRow == null) return 0;

            // 性能优化：预先计算列映射，避免重复查找
            var columnMappings = new Dictionary<int, PropertyInfo>();
            for (int i = 0; i <= headerRow.LastCellNum; i++)
            {
                var cell = headerRow.GetCell(i);
                if (cell != null && !string.IsNullOrWhiteSpace(cell.StringCellValue))
                {
                    var columnName = cell.StringCellValue.Trim();
                    if (propertyMappings.TryGetValue(columnName, out var property))
                    {
                        columnMappings[i] = property;
                    }
                }
            }

            var importedCount = 0;
            var dbSet = _DbContext.Set<SimpleDataDic>();
            var entitiesToAdd = new List<SimpleDataDic>();
            
            // 性能优化：如果需要更新，预先查询现有记录并建立映射
            Dictionary<string, SimpleDataDic> existingEntities = null;
            if (updateExisting)
            {
                existingEntities = dbSet
                    .Where(x => x.DataDicId == catalogId)
                    .ToDictionary(x => x.Code ?? "", x => x);
            }

            for (int rowIndex = 1; rowIndex <= sheet.LastRowNum; rowIndex++)
            {
                var row = sheet.GetRow(rowIndex);
                if (row == null) continue;

                var entity = new SimpleDataDic();
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

                // 设置关联信息
                entity.DataDicId = catalogId;
                entity.Id = Guid.NewGuid();
                entity.CreateDateTime = DateTime.Now;

                // 性能优化：使用预先查询的映射检查是否需要更新
                if (updateExisting && !string.IsNullOrEmpty(entity.Code) && 
                    existingEntities?.TryGetValue(entity.Code, out var existing) == true)
                {
                    // 更新现有记录
                    foreach (var mapping in columnMappings)
                    {
                        var value = mapping.Value.GetValue(entity);
                        mapping.Value.SetValue(existing, value);
                    }
                    continue;
                }

                entitiesToAdd.Add(entity);
                importedCount++;
            }

            // 性能优化：批量添加实体，减少数据库操作次数
            if (entitiesToAdd.Any())
            {
                dbSet.AddRange(entitiesToAdd);
            }

            return importedCount;
        }

        #endregion
    }
}