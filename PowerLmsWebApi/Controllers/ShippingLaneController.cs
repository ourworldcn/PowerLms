using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NPOI; // 引入NpoiUnit.GetStringList
using PowerLms.Data;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Dto;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// 航线管理控制器
    /// </summary>
    public class ShippingLaneController : PlControllerBase
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public ShippingLaneController(IServiceProvider serviceProvider, AccountManager accountManager, 
            PowerLmsUserDbContext dbContext, OrgManager<PowerLmsUserDbContext> orgManager, 
            EntityManager entityManager, IMapper mapper, AuthorizationManager authorizationManager)
        {
            _ServiceProvider = serviceProvider;
            _AccountManager = accountManager;
            _DbContext = dbContext;
            _OrgManager = orgManager;
            _EntityManager = entityManager;
            _Mapper = mapper;
            _AuthorizationManager = authorizationManager;
        }
        
        IServiceProvider _ServiceProvider;
        AccountManager _AccountManager;
        readonly PowerLmsUserDbContext _DbContext;
        OrgManager<PowerLmsUserDbContext> _OrgManager;
        EntityManager _EntityManager;
        IMapper _Mapper;
        readonly AuthorizationManager _AuthorizationManager;
        
        /// <summary>
        /// 添加新航线方案
        /// </summary>
        [HttpPost]
        public ActionResult<AddShippingLaneReturnDto> AddShippingLane(AddShippingLaneParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            if (!_AuthorizationManager.Demand(out err, "A.1.1")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new AddShippingLaneReturnDto();
            model.Item.GenerateNewId();

            _DbContext.ShippingLanes.Add(model.Item);
            model.Item.CreateDateTime = OwHelper.WorldNow;
            model.Item.CreateBy = context.User.Id;
            model.Item.OrgId = context.User.OrgId;
            model.Item.UpdateBy = context.User.Id;
            model.Item.UpdateDateTime = OwHelper.WorldNow;
            _DbContext.SaveChanges();
            result.Id = model.Item.Id;
            return result;
        }

        /// <summary>
        /// 获取全部航线方案
        /// </summary>
        [HttpGet]
        public ActionResult<GetAllShippingLaneReturnDto> GetAllShippingLane([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllShippingLaneReturnDto();
            var dbSet = _DbContext.ShippingLanes.Where(c => c.OrgId == context.User.OrgId);
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            coll = EfHelper.GenerateWhereAnd(coll, conditional);
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 修改航线方案信息。
        /// </summary>
        [HttpPut]
        public ActionResult<ModifyShippingLaneReturnDto> ModifyShippingLane(ModifyShippingLaneParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            if (!_AuthorizationManager.Demand(out err, "A.1.3")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new ModifyShippingLaneReturnDto();
            if (!_EntityManager.Modify(model.Items)) return NotFound();
            foreach (var item in model.Items)
            {
                item.UpdateBy = context.User.Id;
                item.UpdateDateTime = OwHelper.WorldNow;
            }
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 标记删除航线信息。(软删除)
        /// </summary>
        [HttpDelete]
        public ActionResult<RemoveShippingLaneReturnDto> RemoveShippingLane(RemoveShippingLanePatamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            if (!_AuthorizationManager.Demand(out err, "A.1.2")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new RemoveShippingLaneReturnDto();

            var dbSet = _DbContext.ShippingLanes;
            var items = dbSet.Where(c => model.Ids.Contains(c.Id)).ToArray();
            if (items.Length != model.Ids.Count) return BadRequest("指定Id中，至少有一个不对应实体。");
            _DbContext.RemoveRange(items);
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 导入航线数据 - 使用DataSeedHelper高性能批量插入优化版本
        /// </summary>
        [HttpPost]
        public ActionResult<ImportShippingLaneReturnDto> ImportShippingLane(IFormFile file, Guid token)
        {
            if (_AccountManager.GetOrLoadContextByToken(token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            if (!_AuthorizationManager.Demand(out err, "A.1.4")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new ImportShippingLaneReturnDto();

            try
            {
                // 🚀 使用NpoiUnit.GetStringList替代NpoiManager
                using var workbook = new XSSFWorkbook(file.OpenReadStream());
                var sheet = workbook.GetSheetAt(0);
                
                // 使用GetStringList获取所有数据，跳过前2行
                using var allRows = NpoiUnit.GetStringList(sheet, out var columnHeaders);
                
                if (columnHeaders.Count == 0)
                {
                    return BadRequest("Excel文件格式错误：未找到有效的列标题");
                }

                if (allRows.Count <= 2) // 考虑跳过的行数
                {
                    return BadRequest("Excel文件格式错误：没有有效的数据行");
                }

                // 创建列名到属性的映射 - 基于ShippingLaneEto的JsonPropertyName
                var propertyMappings = CreateShippingLanePropertyMappings();
                var columnMappings = CreateColumnMappings(columnHeaders, propertyMappings);

                if (columnMappings.Count == 0)
                {
                    return BadRequest("Excel文件格式错误：未找到匹配的列标题");
                }

                // 使用PooledList存储转换后的实体
                using var shippingLanes = new PooledList<ShippingLane>(allRows.Count - 2);
                
                // 跳过前2行，从第3行开始处理数据（索引从0开始，所以是从索引2开始）
                for (int rowIndex = 2; rowIndex < allRows.Count; rowIndex++)
                {
                    using var currentRow = allRows[rowIndex];
                    
                    try
                    {
                        var shippingLane = CreateShippingLaneFromRow(currentRow, columnMappings, context);
                        if (shippingLane != null)
                        {
                            shippingLanes.Add(shippingLane);
                        }
                    }
                    catch (Exception ex)
                    {
                        // 记录具体行的错误，但继续处理其他行
                        var logger = _ServiceProvider.GetService<ILogger<ShippingLaneController>>();
                        logger?.LogWarning("处理第{RowNumber}行数据时发生错误: {Error}", rowIndex + 1, ex.Message);
                    }
                }

                if (shippingLanes.Count > 0)
                {
                    // 🚀 使用DataSeedHelper进行高性能批量插入，替代传统的AddRange+SaveChanges
                    var logger = _ServiceProvider.GetService<ILogger<ShippingLaneController>>();
                    var insertedCount = DataSeedHelper.TryBulkInsertOptimized(
                        _DbContext, shippingLanes, false, logger, "航线数据导入");
                    
                    if (insertedCount > 0)
                    {
                        logger?.LogInformation("航线数据导入成功：共插入{Count}条记录", insertedCount);
                    }
                    else
                    {
                        logger?.LogWarning("航线数据导入失败：没有记录被插入");
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                var logger = _ServiceProvider.GetService<ILogger<ShippingLaneController>>();
                logger?.LogError(ex, "导入航线数据时发生错误");
                return StatusCode(500, "导入数据时发生错误，请检查文件格式");
            }
        }

        #region 私有辅助方法

        /// <summary>
        /// 创建航线属性映射字典
        /// </summary>
        private static Dictionary<string, string> CreateShippingLanePropertyMappings()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // 基于ShippingLaneEto的JsonPropertyName映射到ShippingLane属性
                { "启运港", nameof(ShippingLane.StartCode) },
                { "目的港", nameof(ShippingLane.EndCode) },
                { "航空公司", nameof(ShippingLane.Shipper) },
                { "航班信息", nameof(ShippingLane.VesslRate) },
                { "到达天数", nameof(ShippingLane.ArrivalTimeInDay) },
                { "包装规范", nameof(ShippingLane.Packing) },
                { "KGSm", nameof(ShippingLane.KgsM) },
                { "KGSN", nameof(ShippingLane.KgsN) },
                { "KGS45", nameof(ShippingLane.A45) },
                { "KGS100", nameof(ShippingLane.A100) },
                { "KGS300", nameof(ShippingLane.A300) },
                { "KGS500", nameof(ShippingLane.A500) },
                { "KGS1000", nameof(ShippingLane.A1000) },
                { "KGS2000", nameof(ShippingLane.A2000) },
                { "有效日期", nameof(ShippingLane.StartDateTime) },
                { "失效日期", nameof(ShippingLane.EndDateTime) },
                { "备注", nameof(ShippingLane.Remark) },
                { "联系联系方式", nameof(ShippingLane.Contact) }
            };
        }

        /// <summary>
        /// 创建列索引到属性名的映射
        /// </summary>
        private static Dictionary<int, string> CreateColumnMappings(PooledList<string> columnHeaders, Dictionary<string, string> propertyMappings)
        {
            var columnMappings = new Dictionary<int, string>();
            
            for (int i = 0; i < columnHeaders.Count; i++)
            {
                var columnName = columnHeaders[i]?.Trim();
                if (!string.IsNullOrEmpty(columnName) && propertyMappings.TryGetValue(columnName, out var propertyName))
                {
                    columnMappings[i] = propertyName;
                }
            }
            
            return columnMappings;
        }

        /// <summary>
        /// 从Excel行数据创建ShippingLane实体
        /// </summary>
        private static ShippingLane CreateShippingLaneFromRow(PooledList<string> rowData, 
            Dictionary<int, string> columnMappings, OwContext context)
        {
            var shippingLane = new ShippingLane();
            bool hasValidData = false;

            // 设置基础信息
            shippingLane.GenerateNewId();
            shippingLane.CreateDateTime = OwHelper.WorldNow;
            shippingLane.CreateBy = context.User.Id;
            shippingLane.OrgId = context.User.OrgId;
            shippingLane.UpdateBy = context.User.Id;
            shippingLane.UpdateDateTime = OwHelper.WorldNow;

            // 根据列映射设置属性值
            foreach (var mapping in columnMappings)
            {
                var columnIndex = mapping.Key;
                var propertyName = mapping.Value;
                
                if (columnIndex < rowData.Count)
                {
                    var cellValue = rowData[columnIndex];
                    if (!string.IsNullOrWhiteSpace(cellValue))
                    {
                        if (SetPropertyValue(shippingLane, propertyName, cellValue))
                        {
                            hasValidData = true;
                        }
                    }
                }
            }

            return hasValidData ? shippingLane : null;
        }

        /// <summary>
        /// 设置实体属性值
        /// </summary>
        private static bool SetPropertyValue(ShippingLane entity, string propertyName, string value)
        {
            try
            {
                var property = typeof(ShippingLane).GetProperty(propertyName);
                if (property == null || !property.CanWrite) return false;

                var targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

                object convertedValue = targetType.Name switch
                {
                    nameof(String) => value.Trim(),
                    nameof(Decimal) => decimal.TryParse(value, out var decVal) ? decVal : (decimal?)null,
                    nameof(DateTime) => DateTime.TryParse(value, out var dtVal) ? dtVal : (DateTime?)null,
                    nameof(Int32) => int.TryParse(value, out var intVal) ? intVal : (int?)null,
                    nameof(Boolean) => bool.TryParse(value, out var boolVal) ? boolVal : (bool?)null,
                    _ => value.Trim()
                };

                if (convertedValue != null || property.PropertyType.IsGenericType && 
                    property.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    property.SetValue(entity, convertedValue);
                    return convertedValue != null;
                }
            }
            catch
            {
                // 转换失败时忽略该字段
            }

            return false;
        }

        #endregion
    }
}