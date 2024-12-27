using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PowerLms.Data;
using PowerLmsServer;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Dto;
using System.Net;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// 生成序号控制器。
    /// </summary>
    public class JobNumberController : PlControllerBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public JobNumberController(IServiceProvider serviceProvider, AccountManager accountManager, PowerLmsUserDbContext dbContext, JobManager jobNumber, AuthorizationManager authorizationManager)
        {
            _ServiceProvider = serviceProvider;
            _AccountManager = accountManager;
            _DbContext = dbContext;
            _JobNumber = jobNumber;
            _AuthorizationManager = authorizationManager;
        }

        IServiceProvider _ServiceProvider;
        AccountManager _AccountManager;
        PowerLmsUserDbContext _DbContext;
        JobManager _JobNumber;
        AuthorizationManager _AuthorizationManager;

        /// <summary>
        /// 用指定的编码规则生成一个新的编码。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定类别Id无效。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPost]
        public ActionResult<GeneratedJobNumberReturnDto> GeneratedJobNumber(GeneratedJobNumberParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            if (_DbContext.DD_JobNumberRules.Find(model.RuleId) is not JobNumberRule jnr) return BadRequest($"指定的规则不存在，Id={model.RuleId}");
            string err;
            if (jnr.BusinessTypeId == ProjectContent.AeId)    //若是空运出口业务
            {
                if (!_AuthorizationManager.Demand(out err, "D0.1.1.2")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (jnr.BusinessTypeId == ProjectContent.AiId)    //若是空运进口业务
                if (!_AuthorizationManager.Demand(out err, "D1.1.1.2")) return StatusCode((int)HttpStatusCode.Forbidden, err);

            var result = new GeneratedJobNumberReturnDto();
            using var dw = DisposeHelper.Create((key, timeout) => SingletonLocker.TryEnter(key, timeout), key => SingletonLocker.Exit(key), model.RuleId.ToString(), TimeSpan.FromSeconds(2)); //锁定该规则
            result.Result = _JobNumber.Generated(jnr, context?.User, OwHelper.WorldNow);
            context.Nop();
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 用指定的其它编码规则生成一个新的编码。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定类别Id无效。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpPost]
        public ActionResult<GeneratedOtherNumberReturnDto> GeneratedOtherNumber(GeneratedOtherNumberParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GeneratedOtherNumberReturnDto();
            if (_DbContext.DD_OtherNumberRules.Find(model.RuleId) is not OtherNumberRule jnr) return BadRequest($"指定的规则不存在，Id={model.RuleId}");
            using var dw = DisposeHelper.Create((key, timeout) => SingletonLocker.TryEnter(key, timeout), key => SingletonLocker.Exit(key), model.RuleId.ToString(), TimeSpan.FromSeconds(2)); //锁定该规则
            result.Result = _JobNumber.Generated(jnr, context?.User, OwHelper.WorldNow);
            context.Nop();
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 复制编码规则。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">至少有一个指定代码的规则无法找到。</response> 
        /// <response code="401">无效令牌。</response> 
        [HttpPost]
        public ActionResult<CopyJobNumberRuleReturnDto> CopyJobNumberRule(CopyJobNumberRuleParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new CopyJobNumberRuleReturnDto();

            var srcs = _DbContext.DD_JobNumberRules.Where(c => model.Codes.Contains(c.Code)).ToArray();
            if (srcs.Length != model.Codes.Count) return BadRequest("至少有一个指定代码的规则无法找到。");

            var dests = srcs.Select(c =>
            {
                var r = new JobNumberRule
                {
                    Code = c.Code,
                    DisplayName = c.DisplayName,
                    IsDelete = c.IsDelete,
                    OrgId = model.DestOrgId,
                    Remark = c.Remark,
                    RepeatDate = c.RepeatDate,
                    ShortcutName = c.ShortcutName,
                    RuleString = c.RuleString,
                    RepeatMode = c.RepeatMode,
                    ShortName = c.ShortName,
                    StartValue = c.StartValue,
                    BusinessTypeId = c.BusinessTypeId,
                };
                r.CurrentNumber = r.StartValue;
                return r;
            });
            _DbContext.DD_JobNumberRules.AddRange(dests);
            _DbContext.SaveChanges();
            return result;
        }
    }

    /// <summary>
    /// 复制编码规则功能的参数封装类。
    /// </summary>
    public class CopyJobNumberRuleParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 指定要复制的规则的Code代码的集合。为空则没有字典会被复制。
        /// </summary>
        public List<string> Codes { get; set; } = new List<string>();

        /// <summary>
        /// 目标组织机构Id。
        /// </summary>
        public Guid DestOrgId { get; set; }
    }

    /// <summary>
    /// 复制编码规则功能的返回值封装类。
    /// </summary>
    public class CopyJobNumberRuleReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 新的编码规则的Id集合。
        /// </summary>
        public List<Guid> Result = new List<Guid>();
    }

    /// <summary>
    /// 用指定的编码规则生成一个新的其它编码的功能参数封装类。
    /// </summary>
    public class GeneratedOtherNumberParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 规则的Id.
        /// </summary>
        public Guid RuleId { get; set; }
    }

    /// <summary>
    /// 用指定的编码规则生成一个新的其它编码的功能返回值封装类。
    /// </summary>
    public class GeneratedOtherNumberReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 返回的业务码。
        /// </summary>
        public string Result { get; set; }
    }

    /// <summary>
    /// 用指定的编码规则生成一个新的编码的功能返回值封装类。
    /// </summary>
    public class GeneratedJobNumberReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 返回的业务码。
        /// </summary>
        public string Result { get; set; }
    }

    /// <summary>
    /// 用指定的编码规则生成一个新的编码的功能参数封装类。
    /// </summary>
    public class GeneratedJobNumberParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 规则的Id.
        /// </summary>
        public Guid RuleId { get; set; }
    }
}
