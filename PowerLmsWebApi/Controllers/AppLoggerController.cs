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
        MerchantManager _MerchantManager;
        private readonly IMapper _Mapper;
        OwSqlAppLogger _AppLogger;
        PowerLmsUserDbContext _DbContext;

        /// <summary>
        /// 构造函数。
        /// </summary>
        public AppLoggerController(AccountManager accountManager, IServiceProvider serviceProvider, OwSqlAppLogger appLogger, PowerLmsUserDbContext dbContext, EntityManager entityManager, IMapper mapper, MerchantManager merchantManager)
        {
            _AccountManager = accountManager;
            _ServiceProvider = serviceProvider;
            _AppLogger = appLogger;
            _DbContext = dbContext;
            _EntityManager = entityManager;
            _Mapper = mapper;
            _MerchantManager = merchantManager;
        }

        /// <summary>
        /// 返回日志项。超管/商管可以调用，商管只能看自己商户的日志。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">已支持通用查询——除个别涉及敏感信息字段外，所有实体字段都可作为条件。</param>
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

            var dbSet = _DbContext.OwAppLogVOs;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            if (context.User.IsMerchantAdmin)
            {
                var merchantId = _MerchantManager.GetOrLoadCacheItemByUser(context.User).Data.Id;
                coll = coll.Where(c => c.MerchantId == merchantId);
            }
            coll = EfHelper.GenerateWhereAnd(coll, conditional);
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

            var typeId = Guid.Parse("{2EC4DB41-C01C-40FC-86E2-8FE1A4F9DEF7}");
            _AppLogger.Define(typeId, "{CreateUtc} [Id:{UserId}] 清理了日志。");

            var dic = new Dictionary<string, string> { { "UserId", context.User.Id.ToString() },
            };
            var item = new OwAppLogItemStore(typeId)
            {
                ParamstersJson = JsonSerializer.Serialize(dic),
            };
            _DbContext.OwAppLogItemStores.Add(item);
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 导出日志功能。导出为csv文件。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult ExportLogger(ExportLoggerParamsDto model)
        {
            var result = new ExportLoggerReturnDto();
            var fileName = model.FileName;
            // 查询 OwAppLogVOs 视图的内容，并按创建时间降序排序
            var logs = _DbContext.OwAppLogVOs.OrderByDescending(c => c.CreateUtc).ToArray();

            // 将视图内容转换为 CSV 格式，排除 FormatString 和 ParamstersJson 字段，包含 Message 属性
            var sb = new StringBuilder();
            sb.AppendLine("Id,TypeId,Message,ExtraBytes,CreateUtc,MerchantId");

            foreach (var log in logs)
            {
                sb.AppendLine($"{log.Id},{log.TypeId},{log.Message},{Convert.ToBase64String(log.ExtraBytes)},{log.CreateUtc},{log.MerchantId}");
            }

            var fileBytes = Encoding.UTF8.GetBytes(sb.ToString());
            var contentType = "text/csv";

            // 返回包含文件内容的 FileResult
            return File(fileBytes, contentType, fileName);
        }

        /// <summary>
        /// 导出日志功能的参数封装类。
        /// </summary>
        public class ExportLoggerParamsDto
        {
            /// <summary>
            /// 指定下载文件的名字。不可以含路径。
            /// </summary>
            public string FileName { get; set; }
        }

        /// <summary>
        /// 导出日志功能的返回值封装类。
        /// </summary>
        public class ExportLoggerReturnDto
        {
        }

        /// <summary>
        /// 清除日志项功能的参数封装类。
        /// </summary>
        public class RemoveAllLoggerItemParamsDto : TokenDtoBase
        {
        }

        /// <summary>
        /// 清除日志项功能的返回值封装类。
        /// </summary>
        public class RemoveAllLoggerItemReturnDto : ReturnDtoBase
        {
        }

        /// <summary>
        /// 返回日志项功能的参数封装类。
        /// </summary>
        public class GetAllAppLogItemParamsDto
        {
        }

        /// <summary>
        /// 返回日志项功能的返回值封装类。
        /// </summary>
        public class GetAllAppLogItemReturnDto : PagingReturnDtoBase<OwAppLogVO>
        {
        }

        /// <summary>
        /// 追加一个日志项功能的参数封装类。
        /// </summary>
        public class AddLoggerItemParamsDto
        {
        }

        /// <summary>
        /// 追加一个日志项功能的返回值封装类。
        /// </summary>
        public class AddLoggerItemReturnDto
        {
        }
    }
}
