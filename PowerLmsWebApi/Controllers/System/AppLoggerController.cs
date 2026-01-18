using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NPOI.HPSF;
using NPOI.SS.Formula.Functions;
using PowerLms.Data;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Controllers;
using PowerLmsWebApi.Dto;
using System.Data.SqlTypes;
using System.Net;
using System.Text;
using System.Text.Json;
namespace GY02.Controllers
{
    /// <summary>
    /// 应用日志控制器。
    /// </summary>
    public class AppLoggerController : PlControllerBase
    {
        private readonly AccountManager _AccountManager;
        private readonly IServiceProvider _ServiceProvider;
        private readonly EntityManager _EntityManager;
        private readonly OrgManager<PowerLmsUserDbContext> _OrgManager;
        private readonly IMapper _Mapper;
        private readonly OwSqlAppLogger _AppLogger;
        private readonly PowerLmsUserDbContext _DbContext;
        /// <summary>
        /// 构造函数。
        /// </summary>
        public AppLoggerController(AccountManager accountManager, IServiceProvider serviceProvider, OwSqlAppLogger appLogger, PowerLmsUserDbContext dbContext, EntityManager entityManager, IMapper mapper, OrgManager<PowerLmsUserDbContext> orgManager)
        {
            _AccountManager = accountManager;
            _ServiceProvider = serviceProvider;
            _AppLogger = appLogger;
            _DbContext = dbContext;
            _EntityManager = entityManager;
            _Mapper = mapper;
            _OrgManager = orgManager;
        }
        /// <summary>
        /// 返回日志项。超管/商管可以调用，商管只能看自己商户的日志。
        /// </summary>
        /// <param name="model">不可以使用 Message 作为排序字段。</param>
        /// <param name="conditional">支持通用查询。但 Message 字段不可以作为条件。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定类别Id无效。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。只有超级管理员和商户管理员可以使用此功能。</response>  
        [HttpGet]
        public ActionResult<GetAllAppLogItemReturnDto> GetAllAppLogItem([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllAppLogItemReturnDto { };
            if (!context.User.IsAdmin()) return StatusCode((int)HttpStatusCode.Forbidden, "只有超级管理员和商户管理员可以使用此功能。");
            var dbSet = _DbContext.OwAppLogViews;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            if (context.User.IsMerchantAdmin)
            {
                var merchantId = _OrgManager.GetMerchantIdByUserId(context.User.Id);
                coll = coll.Where(c => c.MerchantId == merchantId);
            }
            var dic = new Dictionary<string, string>(conditional, StringComparer.OrdinalIgnoreCase);
            dic.Remove(nameof(OwAppLogView.Message), out var message);
            coll = EfHelper.GenerateWhereAnd(coll, dic);
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }
        /// <summary>
        /// 追加一个日志项。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<AddLoggerItemReturnDto> AddLoggerItem(AddLoggerItemParamsDto model)
        {
            var result = new AddLoggerItemReturnDto();
            return result;
        }
        /// <summary>
        /// 清除日志项。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定类别Id无效。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。只有超级管理员可以使用此功能。</response>  
        [HttpDelete]
        public ActionResult<RemoveAllLoggerItemReturnDto> RemoveAllLoggerItem(RemoveAllLoggerItemParamsDto model)
        {
            var result = new RemoveAllLoggerItemReturnDto();
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            if (!context.User.IsAdmin()) return StatusCode((int)HttpStatusCode.Forbidden, "只有超级管理员可以使用此功能。");
            _DbContext.TruncateTable(nameof(_DbContext.OwAppLogItemStores));
            _AppLogger.LogGeneralInfo("清除日志");
            _DbContext.SaveChanges();
            return result;
        }
        /// <summary>
        /// 导出日志功能。导出为csv文件。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult ExportLogger([FromQuery] ExportLoggerParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            if (!context.User.IsAdmin()) return StatusCode((int)HttpStatusCode.Forbidden, "只有超级管理员和商户管理员可以使用此功能。");
            var fileName = model.FileName ?? "应用日志导出.csv";
            // 查询日志视图数据，并按创建时间降序排序
            var query = _DbContext.OwAppLogViews.AsNoTracking();
            query = query.OrderByDescending(c => c.CreateUtc);
            // 如果是商户管理员，只能导出自己商户的日志
            if (context.User.IsMerchantAdmin)
            {
                var merchantId = _OrgManager.GetMerchantIdByUserId(context.User.Id);
                query = query.Where(c => c.MerchantId == merchantId);
            }
            var logs = query.ToArray();
            // 将视图内容转换为 CSV 格式
            var sb = new StringBuilder();
            // 添加标题行
            sb.AppendLine("ID,日志级别,创建时间,消息内容,操作人,操作IP,操作类型,公司名称,客户端类型");
            foreach (var log in logs)
            {
                // 格式化并转义CSV字段，确保特殊字符不会破坏CSV格式
                string message = log.Message?.Replace("\"", "\"\"");
                string loginName = log.LoginName?.Replace("\"", "\"\"");
                string operationIp = log.OperationIp?.Replace("\"", "\"\"");
                string operationType = log.OperationType?.Replace("\"", "\"\"");
                string companyName = log.CompanyName?.Replace("\"", "\"\"");
                string clientType = log.ClientType?.Replace("\"", "\"\"");
                // 将字段用双引号包裹，确保包含逗号的字段正确解析
                sb.AppendLine($"{log.Id},{log.LogLevel},{log.CreateUtc:yyyy-MM-dd HH:mm:ss},\"{message}\",\"{loginName}\",\"{operationIp}\",\"{operationType}\",\"{companyName}\",\"{clientType}\"");
            }
            var fileBytes = Encoding.UTF8.GetBytes(sb.ToString());
            var contentType = "text/csv";
            // 返回包含文件内容的 FileResult
            return File(fileBytes, contentType, fileName);
        }
    }
}
