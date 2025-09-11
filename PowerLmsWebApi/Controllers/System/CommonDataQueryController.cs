/*
 * 项目：PowerLms | 模块：通用数据查询
 * 功能：提供通用的数据查询接口，支持多种实体类型的字段值查询
 * 技术要点：表白名单机制、数据隔离、动态查询条件、安全过滤
 * 作者：zc | 创建：2025-01 | 修改：2025-01-27 创建通用数据查询接口
 */

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PowerLms.Data;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PowerLmsWebApi.Controllers.System
{
    /// <summary>
    /// 通用数据查询控制器。
    /// 提供安全的通用数据查询功能，支持多种实体类型的字段值查询。
    /// 当前支持的表（白名单）：OaExpenseRequisitions、DocFeeRequisitions
    /// 未来计划支持：PlCustomers、PlJobs、DocFees等
    /// </summary>
    public class CommonDataQueryController : PlControllerBase
    {
        private readonly PowerLmsUserDbContext _DbContext;
        private readonly AccountManager _AccountManager;
        private readonly IServiceProvider _ServiceProvider;
        private readonly ILogger<CommonDataQueryController> _Logger;

        /// <summary>
        /// 表白名单：允许查询的表名
        /// 当前支持：OaExpenseRequisitions、DocFeeRequisitions
        /// 未来扩展：PlCustomers、PlJobs、DocFees等
        /// </summary>
        private static readonly HashSet<string> TableWhitelist = new(StringComparer.OrdinalIgnoreCase)
        {
            "OaExpenseRequisitions",
            "DocFeeRequisitions"
        };

        /// <summary>
        /// 构造函数。
        /// 初始化通用数据查询控制器的依赖项。
        /// </summary>
        /// <param name="dbContext">数据库上下文</param>
        /// <param name="accountManager">账户管理器</param>
        /// <param name="serviceProvider">服务提供器</param>
        /// <param name="logger">日志记录器</param>
        public CommonDataQueryController(PowerLmsUserDbContext dbContext, 
            AccountManager accountManager, 
            IServiceProvider serviceProvider,
            ILogger<CommonDataQueryController> logger)
        {
            _DbContext = dbContext;
            _AccountManager = accountManager;
            _ServiceProvider = serviceProvider;
            _Logger = logger;
        }

        /// <summary>
        /// 获取指定表字段的所有值。
        /// 支持多种实体类型的字段值查询，根据表名和字段名动态查询数据。
        /// 使用表白名单机制确保数据安全，强制按机构ID隔离数据。
        /// </summary>
        /// <param name="token">用户访问令牌</param>
        /// <param name="tableName">表名（必须在白名单中）</param>
        /// <param name="fieldNames">字段名集合，可传递多个fieldNames参数</param>
        /// <param name="distinct">是否去重查询，默认true</param>
        /// <param name="maxResults">最大返回结果数量，不指定时返回所有结果</param>
        /// <returns>记录集合，每条记录包含指定字段的键值对</returns>
        /// <response code="200">查询成功，返回记录集合</response>
        /// <response code="400">参数错误，如表名不在白名单中</response>
        /// <response code="401">无效令牌</response>
        /// <response code="403">权限不足</response>
        /// <response code="500">服务器内部错误</response>
        [HttpGet]
        public ActionResult<GetCommonDataReturnDto> GetData(
            [FromQuery] Guid token,
            [FromQuery] string tableName,
            [FromQuery] List<string> fieldNames,
            [FromQuery] bool distinct = true,
            [FromQuery] int? maxResults = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(token, _ServiceProvider) is not OwContext context)
                return Unauthorized();
            var result = new GetCommonDataReturnDto();
            try
            {
                if (string.IsNullOrWhiteSpace(tableName))
                {
                    result.HasError = true;
                    result.ErrorCode = 400;
                    result.DebugMessage = "表名不能为空";
                    return result;
                }
                if (fieldNames == null || fieldNames.Count == 0)
                {
                    result.HasError = true;
                    result.ErrorCode = 400;
                    result.DebugMessage = "字段名集合不能为空";
                    return result;
                }
                var cleanFieldNames = fieldNames.Where(f => !string.IsNullOrWhiteSpace(f)).Select(f => f.Trim()).ToList();
                if (cleanFieldNames.Count == 0)
                {
                    result.HasError = true;
                    result.ErrorCode = 400;
                    result.DebugMessage = "有效字段名不能为空";
                    return result;
                }
                if (maxResults.HasValue && (maxResults.Value < 1 || maxResults.Value > 200))
                {
                    result.HasError = true;
                    result.ErrorCode = 400;
                    result.DebugMessage = "maxResults必须在1到200之间，或不指定该参数";
                    return result;
                }
                if (!TableWhitelist.Contains(tableName))
                {
                    result.HasError = true;
                    result.ErrorCode = 400;
                    result.DebugMessage = $"表名 '{tableName}' 不在允许查询的白名单中。当前支持的表：{string.Join(", ", TableWhitelist)}";
                    return result;
                }
                var records = tableName.ToLowerInvariant() switch
                {
                    "oaexpenserequisitions" => GetOaExpenseRequisitionData(cleanFieldNames, distinct, maxResults, context),
                    "docfeerequisitions" => GetDocFeeRequisitionData(cleanFieldNames, distinct, maxResults, context),
                    _ => new List<Dictionary<string, object>>()
                };
                result.Records = records;
                result.TableName = tableName;
                result.FieldNames = cleanFieldNames;
                result.IsDistinct = distinct;
                result.TotalCount = records.Count;
                _Logger.LogDebug("通用数据查询成功 - 表: {TableName}, 字段: {FieldNames}, 去重: {IsDistinct}, 结果数量: {Count}", 
                    tableName, string.Join(",", cleanFieldNames), distinct, records.Count);
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "通用数据查询时发生错误 - 表: {TableName}, 字段: {FieldNames}", 
                    tableName, fieldNames != null ? string.Join(",", fieldNames) : "null");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"查询数据时发生错误: {ex.Message}";
            }
            return result;
        }

        #region 查询辅助函数

        /// <summary>
        /// 查询OA费用申请单数据。
        /// 强制按当前用户的机构ID进行过滤，确保数据隔离。
        /// </summary>
        /// <param name="fieldNames">要查询的字段名列表</param>
        /// <param name="distinct">是否去重</param>
        /// <param name="maxResults">最大结果数量，null表示不限制</param>
        /// <param name="context">当前用户上下文</param>
        /// <returns>记录集合，每条记录包含指定字段的键值对</returns>
        private List<Dictionary<string, object>> GetOaExpenseRequisitionData(List<string> fieldNames, bool distinct, int? maxResults, OwContext context)
        {
            var query = _DbContext.OaExpenseRequisitions.Where(r => r.OrgId == context.User.OrgId).AsNoTracking();
            if (!context.User.IsSuperAdmin)
            {
                query = query.Where(r => r.CreateBy == context.User.Id);
            }
            var results = new List<Dictionary<string, object>>();
            var records = query.ToList();
            foreach (var record in records)
            {
                var recordDict = new Dictionary<string, object>();
                var recordType = record.GetType();
                foreach (var fieldName in fieldNames)
                {
                    try
                    {
                        var property = recordType.GetProperty(fieldName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                        if (property != null)
                        {
                            var value = property.GetValue(record);
                            recordDict[fieldName] = value != null && !string.IsNullOrWhiteSpace(value.ToString()) ? value : null;
                        }
                        else
                        {
                            recordDict[fieldName] = null;
                        }
                    }
                    catch (Exception ex)
                    {
                        _Logger.LogWarning("获取OA费用申请单字段 {FieldName} 值时发生错误: {Error}", fieldName, ex.Message);
                        recordDict[fieldName] = null;
                    }
                }
                if (recordDict.Values.Any(v => v != null))
                {
                    results.Add(recordDict);
                }
            }
            if (distinct)
            {
                var distinctResults = new List<Dictionary<string, object>>();
                var seenRecords = new HashSet<string>();
                foreach (var record in results)
                {
                    var recordKey = string.Join("|", record.Values.Select(v => v?.ToString() ?? "null"));
                    if (!seenRecords.Contains(recordKey))
                    {
                        seenRecords.Add(recordKey);
                        distinctResults.Add(record);
                    }
                }
                results = distinctResults;
            }
            if (maxResults.HasValue)
            {
                results = results.Take(maxResults.Value).ToList();
            }
            return results;
        }

        /// <summary>
        /// 查询业务费用申请单数据。
        /// 强制按当前用户的机构ID进行过滤，确保数据隔离。
        /// </summary>
        /// <param name="fieldNames">要查询的字段名列表</param>
        /// <param name="distinct">是否去重</param>
        /// <param name="maxResults">最大结果数量，null表示不限制</param>
        /// <param name="context">当前用户上下文</param>
        /// <returns>记录集合，每条记录包含指定字段的键值对</returns>
        private List<Dictionary<string, object>> GetDocFeeRequisitionData(List<string> fieldNames, bool distinct, int? maxResults, OwContext context)
        {
            var query = _DbContext.DocFeeRequisitions.Where(r => r.OrgId == context.User.OrgId).AsNoTracking();
            if (!context.User.IsSuperAdmin)
            {
                query = query.Where(r => r.MakerId == context.User.Id);
            }
            var results = new List<Dictionary<string, object>>();
            var records = query.ToList();
            foreach (var record in records)
            {
                var recordDict = new Dictionary<string, object>();
                var recordType = record.GetType();
                foreach (var fieldName in fieldNames)
                {
                    try
                    {
                        var property = recordType.GetProperty(fieldName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                        if (property != null)
                        {
                            var value = property.GetValue(record);
                            recordDict[fieldName] = value != null && !string.IsNullOrWhiteSpace(value.ToString()) ? value : null;
                        }
                        else
                        {
                            recordDict[fieldName] = null;
                        }
                    }
                    catch (Exception ex)
                    {
                        _Logger.LogWarning("获取业务费用申请单字段 {FieldName} 值时发生错误: {Error}", fieldName, ex.Message);
                        recordDict[fieldName] = null;
                    }
                }
                if (recordDict.Values.Any(v => v != null))
                {
                    results.Add(recordDict);
                }
            }
            if (distinct)
            {
                var distinctResults = new List<Dictionary<string, object>>();
                var seenRecords = new HashSet<string>();
                foreach (var record in results)
                {
                    var recordKey = string.Join("|", record.Values.Select(v => v?.ToString() ?? "null"));
                    if (!seenRecords.Contains(recordKey))
                    {
                        seenRecords.Add(recordKey);
                        distinctResults.Add(record);
                    }
                }
                results = distinctResults;
            }
            if (maxResults.HasValue)
            {
                results = results.Take(maxResults.Value).ToList();
            }
            return results;
        }

        #endregion 查询辅助函数
    }
}