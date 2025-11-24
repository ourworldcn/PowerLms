using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OW.Data;
using PowerLms.Data;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Dto;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// 财务科目设置控制器
    /// </summary>
    public class SubjectConfigurationController : PlControllerBase
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public SubjectConfigurationController(AccountManager accountManager, IServiceProvider serviceProvider,
            PowerLmsUserDbContext dbContext, EntityManager entityManager, IMapper mapper,
            AuthorizationManager authorizationManager, ILogger<SubjectConfigurationController> logger,
            OrgManager<PowerLmsUserDbContext> orgManager)
        {
            _AccountManager = accountManager;
            _ServiceProvider = serviceProvider;
            _DbContext = dbContext;
            _EntityManager = entityManager;
            _Mapper = mapper;
            _AuthorizationManager = authorizationManager;
            _Logger = logger;
            _OrgManager = orgManager;
        }

        readonly AccountManager _AccountManager;
        readonly IServiceProvider _ServiceProvider;
        readonly PowerLmsUserDbContext _DbContext;
        readonly EntityManager _EntityManager;
        readonly IMapper _Mapper;
        readonly AuthorizationManager _AuthorizationManager;
        readonly ILogger<SubjectConfigurationController> _Logger;
        readonly OrgManager<PowerLmsUserDbContext> _OrgManager;

        #region 静态数据

        /// <summary>
        /// 预定义的科目编码字典
        /// </summary>
        private static readonly Dictionary<string, string> SubjectCodeDictionary = new()
        {
            { "Code样例", "简要说明样例，可以是说明\"操作\"的名字" },
        };

        #endregion 静态数据

        #region 财务科目设置CRUD

        /// <summary>
        /// 获取全部财务科目设置
        /// </summary>
        /// <param name="model">分页参数</param>
        /// <param name="conditional">查询条件。支持实体属性和"实体名.属性名"语法，如 SubjectConfiguration.Code、OrgId、Code、SubjectNumber、DisplayName、IsDelete。键不区分大小写。</param>
        /// <returns>财务科目设置列表</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllSubjectConfigurationReturnDto> GetAllSubjectConfiguration([FromQuery] GetAllSubjectConfigurationParamsDto model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllSubjectConfigurationReturnDto();

            var dbSet = _DbContext.SubjectConfigurations;
            var query = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();

            // 使用EfHelper.GenerateWhereAnd简化条件过滤
            if (conditional != null && conditional.Count > 0)
            {
                // 创建不区分大小写的字典，支持实体名.属性名语法
                var normalizedConditional = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var kvp in conditional)
                {
                    normalizedConditional[kvp.Key] = kvp.Value;
                }

                // 尝试使用实体名.属性名语法
                var filteredQuery = EfHelper.GenerateWhereAnd(query, normalizedConditional);

                if (filteredQuery == null)
                {
                    result.HasError = true;
                    result.ErrorCode = 400;
                    result.DebugMessage = OwHelper.GetLastErrorMessage();
                    return BadRequest(result);
                }
                query = filteredQuery;
            }

            // 权限控制：应用组织权限过滤
            query = ApplyOrganizationFilter(query, context.User);

            var prb = _EntityManager.GetAll(query, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 增加财务科目设置（参考现有控制器的标准模式）
        /// </summary>
        /// <param name="model">财务科目设置信息</param>
        /// <returns>增加结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">参数错误或数据冲突。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPost]
        public ActionResult<AddSubjectConfigurationReturnDto> AddSubjectConfiguration(AddSubjectConfigurationParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();

            // 权限检查：需要B.11权限
            if (!context.User.IsAdmin())
                if (!_AuthorizationManager.Demand(out string err, "B.11")) return StatusCode((int)HttpStatusCode.Forbidden, err);

            var result = new AddSubjectConfigurationReturnDto();

            // 数据验证
            if (model.Item == null)
                return BadRequest("财务科目设置信息不能为空");

            // 使用传入的实体并设置系统管理字段（参考PlJobController模式）
            var entity = model.Item;
            entity.GenerateNewId();
            entity.CreateBy = context.User.Id;
            entity.CreateDateTime = OwHelper.WorldNow;
            entity.IsDelete = false;
            
            // 设置组织机构Id（应用权限控制）
            if (context.User.IsSuperAdmin)
            {
                // 超管创建的科目配置OrgId设置为null（全局科目配置）
                entity.OrgId = null;
            }
            else
            {
                // 非超管用户创建的科目配置使用当前用户的组织机构Id
                entity.OrgId = context.User.OrgId;
            }

            try
            {
                _DbContext.SubjectConfigurations.Add(entity);
                _DbContext.SaveChanges();
                result.Id = entity.Id;

                _Logger.LogInformation("用户 {UserId} 成功添加财务科目设置：{SubjectCode} - {DisplayName} (OrgId: {OrgId})",
                    context.User.Id, entity.Code, entity.DisplayName, entity.OrgId);
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message?.Contains("IX_SubjectConfigurations_OrgId_Code") == true ||
                                               ex.InnerException?.Message?.Contains("duplicate") == true ||
                                               ex.InnerException?.Message?.Contains("UNIQUE") == true)
            {
                // 拦截唯一索引违反错误
                _Logger.LogWarning("尝试添加重复的科目编码：OrgId={OrgId}, Code={Code}", entity.OrgId, entity.Code);
                var scopeMessage = entity.OrgId.HasValue ? "组织机构中" : "全局范围内";
                return BadRequest($"{scopeMessage}已存在科目编码 '{entity.Code}'，请使用不同的编码");
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "添加财务科目设置时发生异常");
                return BadRequest($"添加财务科目设置失败：{ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// 修改财务科目设置（支持商户管理员权限）
        /// 超管只能修改OrgId为null的全局科目配置，商户管理员可以修改同一商户下所有科目配置，普通用户只能修改自己组织机构的科目配置
        /// </summary>
        /// <param name="model">财务科目设置修改信息</param>
        /// <returns>修改结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">参数错误或数据冲突。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        /// <response code="404">找不到指定的财务科目设置。</response>  
        [HttpPut]
        public ActionResult<ModifySubjectConfigurationReturnDto> ModifySubjectConfiguration(ModifySubjectConfigurationParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            if (!context.User.IsAdmin())
                if (!_AuthorizationManager.Demand(out string err, "B.11")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new ModifySubjectConfigurationReturnDto();
            if (model.Items == null || !model.Items.Any())
                return BadRequest("修改项不能为空");
            var itemsToUpdate = new List<SubjectConfiguration>();
            var rejectedItems = new List<string>();
            foreach (var item in model.Items)
            {
                var existing = _DbContext.SubjectConfigurations.Find(item.Id);
                if (existing == null)
                {
                    _Logger.LogWarning("尝试修改不存在的财务科目设置：{Id}", item.Id);
                    rejectedItems.Add($"ID {item.Id} (不存在)");
                    continue;
                }
                if (!HasPermissionToModify(context.User, existing))
                {
                    _Logger.LogWarning("用户 {UserId} 尝试修改无权限的财务科目设置：{Id} (OrgId: {OrgId})", 
                        context.User.Id, item.Id, existing.OrgId);
                    rejectedItems.Add($"ID {item.Id} (无权限)");
                    continue;
                }
                itemsToUpdate.Add(item);
            }
            if (!itemsToUpdate.Any())
            {
                var errorMessage = rejectedItems.Any() 
                    ? $"没有找到可修改的财务科目设置。被拒绝的项目：{string.Join(", ", rejectedItems)}"
                    : "没有找到可修改的财务科目设置";
                return NotFound(errorMessage);
            }
            var modifiedEntities = new List<SubjectConfiguration>();
            if (!_EntityManager.Modify(itemsToUpdate, modifiedEntities))
            {
                var errorMsg = OwHelper.GetLastErrorMessage();
                _Logger.LogError("修改财务科目设置失败：{Error}", errorMsg);
                return BadRequest($"修改财务科目设置失败：{errorMsg}");
            }
            foreach (var item in modifiedEntities)
            {
                var entry = _DbContext.Entry(item);
                entry.Property(c => c.OrgId).IsModified = false;
                entry.Property(c => c.CreateBy).IsModified = false;
                entry.Property(c => c.CreateDateTime).IsModified = false;
                entry.Property(c => c.IsDelete).IsModified = false;
            }
            try
            {
                _DbContext.SaveChanges();
                var logMessage = rejectedItems.Any() 
                    ? $"用户 {context.User.Id} 成功修改了 {itemsToUpdate.Count} 个财务科目设置，拒绝了 {rejectedItems.Count} 个项目"
                    : $"用户 {context.User.Id} 成功修改了 {itemsToUpdate.Count} 个财务科目设置";
                _Logger.LogInformation(logMessage);
                if (rejectedItems.Any())
                {
                    _Logger.LogInformation("被拒绝的项目详情：{RejectedItems}", string.Join(", ", rejectedItems));
                }
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message?.Contains("IX_SubjectConfigurations_OrgId_Code") == true ||
                                               ex.InnerException?.Message?.Contains("duplicate") == true ||
                                               ex.InnerException?.Message?.Contains("UNIQUE") == true)
            {
                _Logger.LogWarning("尝试修改为重复的科目编码");
                return BadRequest("修改失败：科目编码在组织机构中已存在");
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "修改财务科目设置时发生异常");
                return BadRequest($"修改财务科目设置失败：{ex.Message}");
            }
            return result;
        }

        /// <summary>
        /// 删除财务科目设置（软删除）
        /// </summary>
        /// <param name="model">删除参数</param>
        /// <returns>删除结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">参数错误。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        /// <response code="404">找不到指定的财务科目设置。</response>  
        [HttpDelete]
        public ActionResult<RemoveSubjectConfigurationReturnDto> RemoveSubjectConfiguration([FromBody] RemoveSubjectConfigurationParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();

            // 权限检查：需要B.11权限
            string err;
            if (!context.User.IsAdmin())
                if (!_AuthorizationManager.Demand(out err, "B.11")) return StatusCode((int)HttpStatusCode.Forbidden, err);

            var result = new RemoveSubjectConfigurationReturnDto();

            var item = _DbContext.SubjectConfigurations.Find(model.Id);
            if (item == null)
                return NotFound("找不到指定的财务科目设置");

            // 权限检查：应用组织权限控制
            if (!HasPermissionToModify(context.User, item))
                return StatusCode((int)HttpStatusCode.Forbidden, "权限不足，无法删除该财务科目设置");

            try
            {
                // 软删除
                item.IsDelete = true;
                _DbContext.SaveChanges();

                _Logger.LogInformation("用户 {UserId} 删除了财务科目设置：{SubjectCode} - {DisplayName}",
                    context.User.Id, item.Code, item.DisplayName);
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "删除财务科目设置时发生异常");
                return BadRequest($"删除财务科目设置失败：{ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// 恢复被删除的财务科目设置
        /// </summary>
        /// <param name="model">恢复参数</param>
        /// <returns>恢复结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        /// <response code="404">找不到指定的财务科目设置。</response>  
        [HttpPost]
        public ActionResult<RestoreSubjectConfigurationReturnDto> RestoreSubjectConfiguration(RestoreSubjectConfigurationParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();

            // 权限检查：需要B.11权限
            if (!context.User.IsAdmin())
                if (!_AuthorizationManager.Demand(out string err, "B.11")) return StatusCode((int)HttpStatusCode.Forbidden, err);

            var result = new RestoreSubjectConfigurationReturnDto();

            var item = _DbContext.SubjectConfigurations.Find(model.Id);
            if (item == null || !item.IsDelete)
                return NotFound("找不到指定的已删除财务科目设置");

            // 权限检查：应用组织权限控制
            if (!HasPermissionToModify(context.User, item))
                return StatusCode((int)HttpStatusCode.Forbidden, "权限不足，无法恢复该财务科目设置");

            try
            {
                item.IsDelete = false;
                _DbContext.SaveChanges();

                _Logger.LogInformation("用户 {UserId} 恢复了财务科目设置：{SubjectCode} - {DisplayName}",
                    context.User.Id, item.Code, item.DisplayName);
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message?.Contains("IX_SubjectConfigurations_OrgId_Code") == true ||
                                               ex.InnerException?.Message?.Contains("duplicate") == true ||
                                               ex.InnerException?.Message?.Contains("UNIQUE") == true)
            {
                // 拦截唯一索引违反错误
                _Logger.LogWarning("尝试恢复重复的科目编码：{Code}", item.Code);
                return BadRequest($"恢复失败：科目编码 '{item.Code}' 在组织机构中已存在");
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "恢复财务科目设置时发生异常");
                return BadRequest($"恢复财务科目设置失败：{ex.Message}");
            }

            return result;
        }

        #endregion 财务科目设置CRUD

        #region 权限控制辅助方法

        /// <summary>
        /// 检查用户是否有权限修改指定的财务科目设置
        /// 超管：只能修改OrgId为null的全局科目配置
        /// 商户管理员：可以修改同一商户下所有科目配置
        /// 普通用户：只能修改自己组织机构的科目配置
        /// </summary>
        /// <param name="user">当前用户</param>
        /// <param name="subjectConfig">要修改的科目配置</param>
        /// <returns>是否有权限</returns>
        private bool HasPermissionToModify(Account user, SubjectConfiguration subjectConfig)
        {
            if (user.IsSuperAdmin)
            {
                // 超管只能修改OrgId为null的全局科目配置，不能修改其他机构的科目
                return subjectConfig.OrgId == null;
            }

            if (user.IsMerchantAdmin)
            {
                // 商户管理员可以修改同一商户下所有科目配置
                var userMerchantId = _OrgManager.GetMerchantIdByUserId(user.Id);
                if (!userMerchantId.HasValue) return false;

                if (!subjectConfig.OrgId.HasValue) return false; // 科目配置没有组织机构ID时拒绝访问

                var configMerchantId = _OrgManager.GetMerchantIdByOrgId(subjectConfig.OrgId.Value);
                return configMerchantId.HasValue && userMerchantId.Value == configMerchantId.Value;
            }

            // 普通用户只能修改自己组织机构的科目配置
            return subjectConfig.OrgId == user.OrgId;
        }

        /// <summary>
        /// 应用组织权限过滤查询
        /// 超管：只能查看OrgId为null的全局科目配置
        /// 商户管理员：可以查看同一商户下所有科目配置
        /// 普通用户：只能查看自己组织机构的科目配置
        /// </summary>
        /// <param name="query">科目配置查询</param>
        /// <param name="user">当前用户</param>
        /// <returns>过滤后的查询</returns>
        private IQueryable<SubjectConfiguration> ApplyOrganizationFilter(IQueryable<SubjectConfiguration> query, Account user)
        {
            if (user.IsSuperAdmin)
            {
                // 超管只能查看OrgId为null的全局科目配置
                return query.Where(c => c.OrgId == null);
            }

            if (user.IsMerchantAdmin)
            {
                // 商户管理员可以查看同一商户下所有科目配置
                var merchantId = _OrgManager.GetMerchantIdByUserId(user.Id);
                if (!merchantId.HasValue)
                {
                    return query.Where(c => false); // 找不到商户ID时返回空结果
                }

                // 获取商户下所有组织机构ID
                var merchantOrgIds = _OrgManager.GetOrLoadOrgCacheItem(merchantId.Value).Orgs.Keys.ToList();
                merchantOrgIds.Add(merchantId.Value); // 添加商户ID本身

                return query.Where(c => c.OrgId.HasValue && merchantOrgIds.Contains(c.OrgId.Value));
            }

            // 普通用户只能查看自己组织机构的科目配置
            return query.Where(c => c.OrgId == user.OrgId);
        }

        #endregion 权限控制辅助方法

        #region 辅助功能

        /// <summary>
        /// 获取科目编码字典
        /// </summary>
        /// <param name="model">请求参数</param>
        /// <returns>科目编码字典</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetSubjectCodeDictionaryReturnDto> GetSubjectCodeDictionary([FromQuery] GetSubjectCodeDictionaryParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();

            var result = new GetSubjectCodeDictionaryReturnDto
            {
                Result = new Dictionary<string, string>(SubjectCodeDictionary) // 返回静态字典的副本
            };

            return result;
        }

        #endregion 辅助功能
    }
}