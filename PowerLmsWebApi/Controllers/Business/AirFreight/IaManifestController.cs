/*
* 项目：PowerLms | 模块：空运进口舱单控制器
* 功能：空运进口舱单（IaManifest，Ia=Import Air）的CRUD操作 - Manifest为行业标准术语
* 技术要点：依赖注入、权限验证、实体管理、主分单识别
* 作者：zc | 创建：2026-02-08 | 修改：2026-02-08 重命名为标准术语
*/

using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PowerLms.Data;
using PowerLmsServer;
using PowerLmsServer.EfData;
using PowerLmsServer.Helpers;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Dto;
using PowerLmsWebApi.Controllers;
using System.Net;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// 空运进口舱单相关控制器（Ia Manifest Controller，Ia=Import Air）。对应实体：IaManifest（主表）、IaManifestDetail（子表）。
    /// </summary>
    public partial class IaManifestController : PlControllerBase
    {
        private readonly AccountManager _AccountManager;
        private readonly IServiceProvider _ServiceProvider;
        private readonly PowerLmsUserDbContext _DbContext;
        private readonly EntityManager _EntityManager;
        private readonly IMapper _Mapper;
        private readonly AuthorizationManager _AuthorizationManager;
        private readonly ILogger<IaManifestController> _Logger;

        /// <summary>
        /// 构造函数。
        /// </summary>
        public IaManifestController(AccountManager accountManager, IServiceProvider serviceProvider,
            PowerLmsUserDbContext dbContext, EntityManager entityManager, IMapper mapper,
            AuthorizationManager authorizationManager, ILogger<IaManifestController> logger)
        {
            _AccountManager = accountManager;
            _ServiceProvider = serviceProvider;
            _DbContext = dbContext;
            _EntityManager = entityManager;
            _Mapper = mapper;
            _AuthorizationManager = authorizationManager;
            _Logger = logger;
        }

        #region 空运进口舱单主表CRUD

        /// <summary>
        /// 获取全部空运进口舱单（IaManifest）。
        /// </summary>
        /// <param name="model">分页查询参数</param>
        /// <param name="conditional">已支持通用查询——除个别涉及敏感信息字段外，所有实体字段都可作为条件。</param>
        /// <returns>空运进口仓单列表</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="401">无效令牌。</response>
        [HttpGet]
        public ActionResult<GetAllManifestReturnDto> GetAllManifest([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();
            var result = new GetAllManifestReturnDto();
            try
            {
                var dbSet = _DbContext.IaManifests;
                var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
                coll = QueryHelper.GenerateWhereAnd(coll, conditional);
                var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
                _Mapper.Map(prb, result);
                _Logger.LogDebug("查询空运进口舱单成功，返回{Count}条记录", result.Result?.Count ?? 0);
                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "查询空运进口舱单时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"查询空运进口舱单时发生错误: {ex.Message}";
                return StatusCode(StatusCodes.Status500InternalServerError, result);
            }
        }

        /// <summary>
        /// 获取单个空运进口舱单详情（含子表数据）。
        /// </summary>
        /// <param name="model">查询参数</param>
        /// <returns>仓单详情</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="404">指定Id的仓单不存在。</response>
        [HttpGet("detail")]
        public ActionResult<GetManifestReturnDto> GetManifest([FromQuery] GetManifestParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();
            var result = new GetManifestReturnDto();
            try
            {
                var manifest = _DbContext.IaManifests.Find(model.Id);
                if (manifest == null)
                    return NotFound($"未找到ID为{model.Id}的空运进口舱单");
                result.Manifest = manifest;
                result.Details = _DbContext.IaManifestDetails
                    .Where(c => c.ParentId == model.Id)
                    .OrderBy(c => c.HBLNO)
                    .ToList();
                _Logger.LogDebug("查询空运进口舱单详情成功，ID={ManifestId}, 明细数={DetailCount}",
                    model.Id, result.Details?.Count ?? 0);
                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "查询空运进口舱单详情时发生错误，ID={ManifestId}", model.Id);
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"查询空运进口舱单详情时发生错误: {ex.Message}";
                return StatusCode(StatusCodes.Status500InternalServerError, result);
            }
        }

        /// <summary>
        /// 增加新空运进口舱单（含主表和子表）。
        /// </summary>
        /// <param name="model">新增参数</param>
        /// <returns>新增结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="400">数据验证失败。</response>
        /// <response code="401">无效令牌。</response>
        [HttpPost]
        public ActionResult<AddManifestReturnDto> AddManifest(AddManifestParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();
            var result = new AddManifestReturnDto();
            try
            {
                var manifest = model.Manifest;
                if (string.IsNullOrWhiteSpace(manifest.MawbNo))
                    return BadRequest("主单号不能为空");
                manifest.GenerateNewId();
                manifest.OrgId = context.User.OrgId;
                _DbContext.IaManifests.Add(manifest);
                if (model.Details != null && model.Details.Count > 0)
                {
                    foreach (var detail in model.Details)
                    {
                        detail.GenerateNewId();
                        detail.ParentId = manifest.Id;
                        detail.MawbNo = manifest.MawbNo;
                        var (mawbId, errorMsg) = FindRelatedMawb(detail.MawbNo, detail.HBLNO);
                        if (!string.IsNullOrWhiteSpace(errorMsg))
                        {
                            return BadRequest($"明细行关联失败: {errorMsg}");
                        }
                        detail.MawbId = mawbId;
                        _DbContext.IaManifestDetails.Add(detail);
                    }
                }
                _DbContext.SaveChanges();
                result.Id = manifest.Id;
                _Logger.LogInformation("空运进口舱单创建成功：ID={ManifestId}, 主单号={MawbNo}, 明细数={DetailCount}, 用户={UserId}",
                    manifest.Id, manifest.MawbNo, model.Details?.Count ?? 0, context.User.Id);
                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "创建空运进口舱单时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"创建空运进口舱单时发生错误: {ex.Message}";
                return StatusCode(StatusCodes.Status500InternalServerError, result);
            }
        }

        /// <summary>
        /// 修改空运进口舱单信息（仅主表）。
        /// </summary>
        /// <param name="model">修改参数</param>
        /// <returns>修改结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="404">指定Id的仓单不存在。</response>
        [HttpPut]
        public ActionResult<ModifyManifestReturnDto> ModifyManifest(ModifyManifestParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();
            var result = new ModifyManifestReturnDto();
            try
            {
                if (!_EntityManager.Modify(new[] { model.Manifest }))
                    return NotFound($"未找到ID为{model.Manifest.Id}的空运进口舱单");
                _DbContext.SaveChanges();
                _Logger.LogInformation("空运进口舱单修改成功：ID={ManifestId}, 用户={UserId}",
                    model.Manifest.Id, context.User.Id);
                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "修改空运进口舱单时发生错误，ID={ManifestId}", model.Manifest.Id);
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"修改空运进口舱单时发生错误: {ex.Message}";
                return StatusCode(StatusCodes.Status500InternalServerError, result);
            }
        }

        /// <summary>
        /// 删除指定Id的空运进口舱单。慎用！
        /// </summary>
        /// <param name="model">删除参数</param>
        /// <returns>删除结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="400">仓单存在关联数据，无法删除。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="404">指定Id的仓单不存在。</response>
        [HttpDelete]
        public ActionResult<RemoveManifestReturnDto> RemoveManifest(RemoveManifestParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();
            var result = new RemoveManifestReturnDto();
            try
            {
                var id = model.Id;
                var item = _DbContext.IaManifests.Find(id);
                if (item is null)
                    return NotFound($"未找到ID为{id}的空运进口舱单");
                var hasDetails = _DbContext.IaManifestDetails.Any(c => c.ParentId == id);
                if (hasDetails)
                    return BadRequest("仓单存在关联的明细数据，无法删除。请先删除明细数据。");
                _EntityManager.Remove(item);
                _DbContext.SaveChanges();
                _Logger.LogInformation("空运进口舱单删除成功：ID={ManifestId}, 用户={UserId}",
                    id, context.User.Id);
                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "删除空运进口舱单时发生错误，ID={ManifestId}", model.Id);
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"删除空运进口舱单时发生错误: {ex.Message}";
                return StatusCode(StatusCodes.Status500InternalServerError, result);
            }
        }

        #endregion 空运进口舱单主表CRUD

        #region 空运进口舱单明细CRUD

        /// <summary>
        /// 获取指定空运进口舱单的所有明细（IaManifestDetail）。
        /// </summary>
        /// <param name="model">查询参数</param>
        /// <returns>明细列表</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="401">无效令牌。</response>
        [HttpGet("details")]
        public ActionResult<GetAllManifestDetailReturnDto> GetAllManifestDetail([FromQuery] GetAllManifestDetailParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();
            var result = new GetAllManifestDetailReturnDto();
            try
            {
                var query = _DbContext.IaManifestDetails.AsNoTracking();
                if (model.ParentId.HasValue)
                    query = query.Where(c => c.ParentId == model.ParentId.Value);
                if (!string.IsNullOrWhiteSpace(model.MawbNo))
                    query = query.Where(c => c.MawbNo == model.MawbNo);
                result.Result = query.OrderBy(c => c.HBLNO).ToList();
                result.Total = result.Result.Count;
                _Logger.LogDebug("查询仓单明细成功，返回{Count}条记录", result.Result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "查询仓单明细时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"查询仓单明细时发生错误: {ex.Message}";
                return StatusCode(StatusCodes.Status500InternalServerError, result);
            }
        }

        /// <summary>
        /// 增加空运进口舱单明细。
        /// </summary>
        /// <param name="model">新增参数</param>
        /// <returns>新增结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="400">数据验证失败。</response>
        /// <response code="401">无效令牌。</response>
        [HttpPost("detail")]
        public ActionResult<AddManifestDetailReturnDto> AddManifestDetail(AddManifestDetailParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();
            var result = new AddManifestDetailReturnDto();
            try
            {
                var detail = model.Detail;
                if (string.IsNullOrWhiteSpace(detail.MawbNo))
                    return BadRequest("主单号不能为空");
                detail.GenerateNewId();
                var (mawbId, errorMsg) = FindRelatedMawb(detail.MawbNo, detail.HBLNO);
                if (!string.IsNullOrWhiteSpace(errorMsg))
                {
                    return BadRequest($"关联失败: {errorMsg}");
                }
                detail.MawbId = mawbId;
                _DbContext.IaManifestDetails.Add(detail);
                _DbContext.SaveChanges();
                result.Id = detail.Id;
                _Logger.LogInformation("仓单明细创建成功：ID={DetailId}, 主单号={MawbNo}, 分单号={HBLNO}, MawbId={MawbId}, 用户={UserId}",
                    detail.Id, detail.MawbNo, detail.HBLNO, mawbId, context.User.Id);
                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "创建仓单明细时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"创建仓单明细时发生错误: {ex.Message}";
                return StatusCode(StatusCodes.Status500InternalServerError, result);
            }
        }

        /// <summary>
        /// 修改空运进口舱单明细信息。
        /// </summary>
        /// <param name="model">修改参数</param>
        /// <returns>修改结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="404">指定Id的明细不存在。</response>
        [HttpPut("detail")]
        public ActionResult<ModifyManifestDetailReturnDto> ModifyManifestDetail(ModifyManifestDetailParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();
            var result = new ModifyManifestDetailReturnDto();
            try
            {
                var detail = model.Detail;
                var (mawbId, errorMsg) = FindRelatedMawb(detail.MawbNo, detail.HBLNO);
                if (!string.IsNullOrWhiteSpace(errorMsg))
                {
                    return BadRequest($"关联失败: {errorMsg}");
                }
                detail.MawbId = mawbId;
                if (!_EntityManager.Modify(new[] { detail }))
                    return NotFound($"未找到ID为{model.Detail.Id}的仓单明细");
                _DbContext.SaveChanges();
                _Logger.LogInformation("仓单明细修改成功：ID={DetailId}, MawbId={MawbId}, 用户={UserId}",
                    model.Detail.Id, mawbId, context.User.Id);
                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "修改仓单明细时发生错误，ID={DetailId}", model.Detail.Id);
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"修改仓单明细时发生错误: {ex.Message}";
                return StatusCode(StatusCodes.Status500InternalServerError, result);
            }
        }

        /// <summary>
        /// 删除指定Id的空运进口舱单明细。
        /// </summary>
        /// <param name="model">删除参数</param>
        /// <returns>删除结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="404">指定Id的明细不存在。</response>
        [HttpDelete("detail")]
        public ActionResult<RemoveManifestDetailReturnDto> RemoveManifestDetail(RemoveManifestDetailParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();
            var result = new RemoveManifestDetailReturnDto();
            try
            {
                var id = model.Id;
                var item = _DbContext.IaManifestDetails.Find(id);
                if (item is null)
                    return NotFound($"未找到ID为{id}的仓单明细");
                _EntityManager.Remove(item);
                _DbContext.SaveChanges();
                _Logger.LogInformation("仓单明细删除成功：ID={DetailId}, 用户={UserId}",
                    id, context.User.Id);
                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "删除仓单明细时发生错误，ID={DetailId}", model.Id);
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"删除仓单明细时发生错误: {ex.Message}";
                return StatusCode(StatusCodes.Status500InternalServerError, result);
            }
        }

        #endregion 空运进口舱单明细CRUD

        #region 辅助方法（主单号/分单号标准化与查找）

        /// <summary>
        /// 标准化主单号（去除空格，保留连字符，标准格式：999-12345678）。
        /// </summary>
        /// <param name="mawbNo">原始主单号（可能有横杠、空格等）</param>
        /// <returns>标准化后的主单号（格式：999-12345678），如果格式不正确则返回原值</returns>
        private string NormalizeMawbNo(string mawbNo)
        {
            if (string.IsNullOrWhiteSpace(mawbNo))
                return string.Empty;
            var cleaned = mawbNo.Replace(" ", "");
            if (cleaned.Length == 11 && !cleaned.Contains("-"))
            {
                return $"{cleaned.Substring(0, 3)}-{cleaned.Substring(3, 8)}";
            }
            return cleaned;
        }

        /// <summary>
        /// 标准化分单号（去除空格和横杠）。
        /// </summary>
        /// <param name="hawbNo">原始分单号</param>
        /// <returns>标准化后的分单号</returns>
        private string NormalizeHawbNo(string hawbNo)
        {
            if (string.IsNullOrWhiteSpace(hawbNo))
                return string.Empty;
            return hawbNo.Replace(" ", "").Replace("-", "");
        }

        /// <summary>
        /// 根据主单号和分单号查找关联的主单或分单Id（分单优先策略）。
        /// 
        /// <para><strong>核心逻辑：</strong></para>
        /// <list type="bullet">
        ///   <item><description>有分单号（HBLNO不为空）：查找并返回<strong>分单ID</strong>（EaHawb.Id）</description></item>
        ///   <item><description>无分单号（HBLNO为空）：查找并返回<strong>主单ID</strong>（EaMawb.Id）</description></item>
        /// </list>
        /// 
        /// <para><strong>设计优势（为什么这样记录）：</strong></para>
        /// <list type="number">
        ///   <item><description><strong>避免重复查询</strong>：记录分单ID后，通过EaHawb.MawbNo字段可直接获取主单信息，无需重新标准化分单号再查找。</description></item>
        ///   <item><description><strong>提高查询效率</strong>：需要分单详细信息时，直接通过MawbId（分单ID）查询EaHawbs表即可，无需额外的标准化和匹配。</description></item>
        ///   <item><description><strong>数据关联清晰</strong>：通过MawbId可以明确知道关联的是主单还是分单（通过HBLNO是否为空判断）。</description></item>
        ///   <item><description><strong>扩展性好</strong>：分单包含主单号（EaHawb.MawbNo），可以追溯到主单；主单则直接关联。</description></item>
        /// </list>
        /// 
        /// <para><strong>验证机制：</strong></para>
        /// <list type="bullet">
        ///   <item><description>找不到对应的主单或分单时，返回错误信息（errorMsg），调用方必须返回BadRequest拒绝保存。</description></item>
        ///   <item><description>确保数据一致性：只有在空运出口系统中存在的主单/分单才能被舱单引用。</description></item>
        /// </list>
        /// </summary>
        /// <param name="mawbNo">主单号（11位纯数字或带横杠格式，如：99912345678或999-12345678）</param>
        /// <param name="hawbNo">分单号（可选，任意格式）</param>
        /// <returns>
        /// 元组：(MawbId, 错误信息)
        /// <list type="bullet">
        ///   <item><description>MawbId：成功时为分单ID或主单ID，失败时为null</description></item>
        ///   <item><description>errorMsg：成功时为空字符串，失败时包含具体错误信息</description></item>
        /// </list>
        /// </returns>
        private (Guid? mawbId, string errorMsg) FindRelatedMawb(string mawbNo, string hawbNo)
        {
            var normalizedMawb = NormalizeMawbNo(mawbNo);
            var normalizedHawb = NormalizeHawbNo(hawbNo);
            if (!string.IsNullOrWhiteSpace(normalizedHawb))
            {
                var hawb = _DbContext.EaHawbs
                    .AsNoTracking()
                    .FirstOrDefault(h => h.HBLNo == normalizedHawb);
                if (hawb == null)
                {
                    return (null, $"未找到分单号 [{hawbNo}] 对应的空运出口分单，请确认分单号是否正确或该分单是否已在系统中创建");
                }
                return (hawb.Id, string.Empty);
            }
            if (!string.IsNullOrWhiteSpace(normalizedMawb))
            {
                var mawb = _DbContext.EaMawbs
                    .AsNoTracking()
                    .FirstOrDefault(m => m.MawbNo == normalizedMawb);
                if (mawb == null)
                {
                    return (null, $"未找到主单号 [{mawbNo}] 对应的空运出口主单，请确认主单号是否正确或该主单是否已在系统中创建");
                }
                return (mawb.Id, string.Empty);
            }
            return (null, "主单号和分单号不能同时为空");
        }

        #endregion 辅助方法（主单号/分单号标准化与查找）
    }
}
