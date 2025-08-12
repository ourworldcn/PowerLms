/*
 * 项目：PowerLms物流管理系统 | 模块：数据字典控制器 - 导入导出功能
 * 功能：所有字典的Excel导入导出
 * 技术要点：
 * - 导出所有字典：系统既定的字典类型，无需用户选择参数
 * - 导入所有字典：自动识别Excel工作表名称，严格验证表名范围
 * - 多租户数据隔离，输入时忽略Excel中OrgId，输出时OrgId列不输出
 * 作者：zc | 创建：2025-01-27
 */

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using PowerLms.Data;
using PowerLmsWebApi.Dto;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using System.Linq.Expressions;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// 数据字典控制器 - 导入导出功能分部类
    /// </summary>
    public partial class DataDicController : PlControllerBase
    {
        /// <summary>
        /// 导出所有字典数据到Excel文件。
        /// 输出时：仅输出当前登录用户OrgId的数据，但OrgId列不输出。
        /// </summary>
        /// <param name="token">用户令牌</param>
        /// <returns>Excel文件</returns>
        [HttpGet]
        public ActionResult ExportAllDictionaries(Guid token)
        {
            if (_AccountManager.GetOrLoadContextByToken(token, _ServiceProvider) is not OwContext context) 
                return Unauthorized();

            // 权限验证：搜索权限文件，如果有叶子权限符合要求就使用，如果没有就不进行权限验证
            // TODO: 实现具体权限查找逻辑

            try
            {
                var orgId = GetUserOrgId(context);
                using var workbook = new HSSFWorkbook();
                
                // 系统既定的所有字典类型（排除JobNumberRule、OtherNumberRule）
                var allDictionaryTypes = new[]
                {
                    ("PlCountry", typeof(PlCountry)),
                    ("PlPort", typeof(PlPort)),
                    ("PlCargoRoute", typeof(PlCargoRoute)),
                    ("PlCurrency", typeof(PlCurrency)),
                    ("FeesType", typeof(FeesType)),
                    ("PlExchangeRate", typeof(PlExchangeRate)),
                    ("UnitConversion", typeof(UnitConversion)),
                    ("ShippingContainersKind", typeof(ShippingContainersKind))
                };
                
                int totalExported = 0;
                
                foreach (var (sheetName, entityType) in allDictionaryTypes)
                {
                    var sheet = workbook.CreateSheet(sheetName);
                    int exportedCount = sheetName switch
                    {
                        "PlCountry" => ExportDictionaryData<PlCountry>(sheet, orgId),
                        "PlPort" => ExportDictionaryData<PlPort>(sheet, orgId),
                        "PlCargoRoute" => ExportDictionaryData<PlCargoRoute>(sheet, orgId),
                        "PlCurrency" => ExportDictionaryData<PlCurrency>(sheet, orgId),
                        "FeesType" => ExportDictionaryData<FeesType>(sheet, orgId),
                        "PlExchangeRate" => ExportDictionaryData<PlExchangeRate>(sheet, orgId),
                        "UnitConversion" => ExportDictionaryData<UnitConversion>(sheet, orgId),
                        "ShippingContainersKind" => ExportDictionaryData<ShippingContainersKind>(sheet, orgId),
                        _ => 0
                    };
                    totalExported += exportedCount;
                }

                var fileName = $"AllDictionaries_{DateTime.Now:yyyyMMdd_HHmmss}.xls";
                var stream = new MemoryStream();
                workbook.Write(stream, true);
                workbook.Close();
                stream.Seek(0, SeekOrigin.Begin);
                
                return File(stream, "application/vnd.ms-excel", fileName);
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "导出所有字典时发生错误");
                return StatusCode(500, $"导出失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 导入所有字典数据。
        /// 自动识别Excel工作表名称，严格验证表名在合理范围内。
        /// 输入时：忽略Excel表中的OrgId数据，只用当前登录用户的OrgId。
        /// </summary>
        /// <param name="formFile">Excel文件</param>
        /// <param name="token">用户令牌</param>
        /// <param name="ignoreExisting">是否忽略已存在记录</param>
        /// <returns>导入结果</returns>
        [HttpPost]
        public ActionResult<ImportAllDictionariesReturnDto> ImportAllDictionaries(
            IFormFile formFile, 
            Guid token, 
            bool ignoreExisting = true)
        {
            if (_AccountManager.GetOrLoadContextByToken(token, _ServiceProvider) is not OwContext context) 
                return Unauthorized();

            // 权限验证：搜索权限文件，如果有叶子权限符合要求就使用，如果没有就不进行权限验证
            // TODO: 实现具体权限查找逻辑

            var result = new ImportAllDictionariesReturnDto();
            var processingDetails = new List<string>();
            int totalImported = 0;

            try
            {
                var orgId = GetUserOrgId(context);
                using var workbook = WorkbookFactory.Create(formFile.OpenReadStream());
                
                // 系统既定的所有字典类型（排除JobNumberRule、OtherNumberRule）
                var supportedDictionaryTypes = new HashSet<string>
                {
                    "PlCountry", "PlPort", "PlCargoRoute", "PlCurrency", "FeesType", 
                    "PlExchangeRate", "UnitConversion", "ShippingContainersKind"
                };
                
                for (int i = 0; i < workbook.NumberOfSheets; i++)
                {
                    var sheet = workbook.GetSheetAt(i);
                    var sheetName = sheet.SheetName;
                    
                    if (!supportedDictionaryTypes.Contains(sheetName))
                    {
                        processingDetails.Add($"{sheetName}：不在支持范围内，已忽略");
                        continue;
                    }
                    
                    try
                    {
                        int importedCount = sheetName switch
                        {
                            "PlCountry" => ImportDictionaryData<PlCountry>(sheet, orgId, ignoreExisting),
                            "PlPort" => ImportDictionaryData<PlPort>(sheet, orgId, ignoreExisting),
                            "PlCargoRoute" => ImportDictionaryData<PlCargoRoute>(sheet, orgId, ignoreExisting),
                            "PlCurrency" => ImportDictionaryData<PlCurrency>(sheet, orgId, ignoreExisting),
                            "FeesType" => ImportDictionaryData<FeesType>(sheet, orgId, ignoreExisting),
                            "PlExchangeRate" => ImportDictionaryData<PlExchangeRate>(sheet, orgId, ignoreExisting),
                            "UnitConversion" => ImportDictionaryData<UnitConversion>(sheet, orgId, ignoreExisting),
                            "ShippingContainersKind" => ImportDictionaryData<ShippingContainersKind>(sheet, orgId, ignoreExisting),
                            _ => 0
                        };

                        totalImported += importedCount;
                        processingDetails.Add($"{sheetName}：{importedCount}条记录");
                    }
                    catch (Exception ex)
                    {
                        processingDetails.Add($"{sheetName}：导入失败 - {ex.Message}");
                        result.HasError = true;
                        result.ErrorCode = 400;
                        result.DebugMessage = $"工作表 {sheetName} 导入失败：{ex.Message}";
                    }
                }

                if (totalImported == 0 && !result.HasError)
                {
                    result.HasError = true;
                    result.ErrorCode = 400;
                    result.DebugMessage = "Excel文件中没有找到可识别的字典工作表";
                    return BadRequest(result);
                }

                _DbContext.SaveChanges();
                
                // 设置返回值
                result.TotalImported = totalImported;
                result.ProcessingDetails = processingDetails;
                result.DebugMessage = $"导入完成，总计：{totalImported}条记录";
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "导入所有字典时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"导入失败：{ex.Message}";
                return StatusCode(500, result);
            }

            return result;
        }

        #region 私有辅助方法

        /// <summary>
        /// 获取用户的组织ID
        /// </summary>
        private Guid? GetUserOrgId(OwContext context)
        {
            var merchantId = _OrgManager.GetMerchantIdByUserId(context.User.Id);
            return context.User.OrgId ?? merchantId;
        }

        /// <summary>
        /// 导出字典数据方法。
        /// 输出时：仅输出当前登录用户OrgId的数据，但OrgId列不输出。
        /// </summary>
        private int ExportDictionaryData<T>(ISheet sheet, Guid? orgId) where T : class
        {
            try
            {
                var data = GetDictionaryDataByOrgId<T>(orgId);
                
                // 获取可导出的属性名数组，排除OrgId列
                var properties = typeof(T).GetProperties()
                    .Where(p => p.CanRead && 
                               (p.PropertyType.IsValueType || p.PropertyType == typeof(string)) &&
                               !p.Name.Equals("OrgId", StringComparison.OrdinalIgnoreCase))
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
                _Logger.LogError(ex, "导出字典 {EntityType} 时发生错误", typeof(T).Name);
                throw;
            }
        }

        /// <summary>
        /// 导入字典数据方法。
        /// 输入时：忽略Excel表中的OrgId数据，只用当前用户的OrgId。
        /// </summary>
        private int ImportDictionaryData<T>(ISheet sheet, Guid? orgId, bool ignoreExisting) where T : class, new()
        {
            try
            {
                // TODO: 实现具体的导入逻辑
                // 忽略Excel表中的OrgId数据，强制使用当前用户的OrgId
                _Logger.LogWarning("导入功能尚未完全实现：{EntityType}", typeof(T).Name);
                return 0;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "导入字典 {EntityType} 时发生错误", typeof(T).Name);
                throw;
            }
        }

        /// <summary>
        /// 获取指定类型的字典数据，严格按照组织权限过滤。
        /// 仅返回当前登录用户OrgId的数据。
        /// </summary>
        private List<T> GetDictionaryDataByOrgId<T>(Guid? orgId) where T : class
        {
            try
            {
                var entityType = typeof(T);
                var query = _DbContext.Set<T>().AsNoTracking();

                // 严格按照OrgId过滤数据
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
                _Logger.LogError(ex, "获取字典数据时发生错误，实体类型: {EntityType}", typeof(T).Name);
                throw;
            }
        }

        #endregion 私有辅助方法
    }
}