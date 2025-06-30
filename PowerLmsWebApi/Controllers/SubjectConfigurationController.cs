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
            AuthorizationManager authorizationManager, ILogger<SubjectConfigurationController> logger)
        {
            _AccountManager = accountManager;
            _ServiceProvider = serviceProvider;
            _DbContext = dbContext;
            _EntityManager = entityManager;
            _Mapper = mapper;
            _AuthorizationManager = authorizationManager;
            _Logger = logger;
        }

        readonly AccountManager _AccountManager;
        readonly IServiceProvider _ServiceProvider;
        readonly PowerLmsUserDbContext _DbContext;
        readonly EntityManager _EntityManager;
        readonly IMapper _Mapper;
        readonly AuthorizationManager _AuthorizationManager;
        readonly ILogger<SubjectConfigurationController> _Logger;

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
                var filteredQuery = EfHelper.GenerateWhereAndWithEntityName(query, normalizedConditional);
                if (filteredQuery == null)
                {
                    // 如果实体名语法失败，尝试直接属性名
                    filteredQuery = EfHelper.GenerateWhereAnd(query, normalizedConditional);
                }

                if (filteredQuery == null)
                {
                    result.HasError = true;
                    result.ErrorCode = 400;
                    result.DebugMessage = OwHelper.GetLastErrorMessage();
                    return BadRequest(result);
                }
                query = filteredQuery;
            }

            // 权限控制：非超管只能查看自己组织机构的数据
            if (!context.User.IsSuperAdmin)
            {
                var userOrgId = context.User.OrgId;
                query = query.Where(c => c.OrgId == userOrgId);
            }

            var prb = _EntityManager.GetAll(query, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 增加财务科目设置
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
            string err;
            if (!_AuthorizationManager.Demand(out err, "B.11")) return StatusCode((int)HttpStatusCode.Forbidden, err);

            var result = new AddSubjectConfigurationReturnDto();

            // 数据验证
            if (model.Item == null)
                return BadRequest("财务科目设置信息不能为空");

            // 设置组织机构Id（非超管只能为自己的组织机构添加）
            if (!context.User.IsSuperAdmin)
            {
                model.Item.OrgId = context.User.OrgId;
            }

            // 设置创建信息
            model.Item.GenerateNewId();
            model.Item.CreateBy = context.User.Id;
            model.Item.CreateDateTime = OwHelper.WorldNow;
            model.Item.IsDelete = false;

            try
            {
                _DbContext.SubjectConfigurations.Add(model.Item);
                _DbContext.SaveChanges();
                result.Id = model.Item.Id;

                _Logger.LogInformation("用户 {UserId} 成功添加财务科目设置：{SubjectCode} - {DisplayName}",
                    context.User.Id, model.Item.Code, model.Item.DisplayName);
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message?.Contains("IX_SubjectConfigurations_OrgId_Code") == true ||
                                               ex.InnerException?.Message?.Contains("duplicate") == true ||
                                               ex.InnerException?.Message?.Contains("UNIQUE") == true)
            {
                // 拦截唯一索引违反错误
                _Logger.LogWarning("尝试添加重复的科目编码：OrgId={OrgId}, Code={Code}", model.Item.OrgId, model.Item.Code);
                return BadRequest($"组织机构中已存在科目编码 '{model.Item.Code}'，请使用不同的编码");
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "添加财务科目设置时发生异常");
                return BadRequest($"添加财务科目设置失败：{ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// 修改财务科目设置
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
            
            // 权限检查：需要B.11权限
            string err;
            if (!_AuthorizationManager.Demand(out err, "B.11")) return StatusCode((int)HttpStatusCode.Forbidden, err);

            var result = new ModifySubjectConfigurationReturnDto();

            if (model.Items == null || !model.Items.Any())
                return BadRequest("修改项不能为空");

            var modifiedCount = 0;
            foreach (var item in model.Items)
            {
                var existing = _DbContext.SubjectConfigurations.Find(item.Id);
                if (existing == null)
                {
                    _Logger.LogWarning("尝试修改不存在的财务科目设置：{Id}", item.Id);
                    continue;
                }

                // 权限检查：非超管只能修改自己组织机构的数据
                if (!context.User.IsSuperAdmin && existing.OrgId != context.User.OrgId)
                {
                    _Logger.LogWarning("用户 {UserId} 尝试修改其他组织机构的财务科目设置：{Id}", context.User.Id, item.Id);
                    continue;
                }

                // 更新字段（保护关键字段）
                existing.Code = item.Code;
                existing.SubjectNumber = item.SubjectNumber;
                existing.DisplayName = item.DisplayName;
                existing.Remark = item.Remark;
                // 不允许修改 OrgId、CreateBy、CreateDateTime

                modifiedCount++;
            }

            if (modifiedCount == 0)
                return NotFound("没有找到可修改的财务科目设置");

            try
            {
                _DbContext.SaveChanges();
                _Logger.LogInformation("用户 {UserId} 成功修改了 {Count} 个财务科目设置", context.User.Id, modifiedCount);
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message?.Contains("IX_SubjectConfigurations_OrgId_Code") == true ||
                                               ex.InnerException?.Message?.Contains("duplicate") == true ||
                                               ex.InnerException?.Message?.Contains("UNIQUE") == true)
            {
                // 拦截唯一索引违反错误
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
            if (!_AuthorizationManager.Demand(out err, "B.11")) return StatusCode((int)HttpStatusCode.Forbidden, err);

            var result = new RemoveSubjectConfigurationReturnDto();

            var item = _DbContext.SubjectConfigurations.Find(model.Id);
            if (item == null)
                return NotFound("找不到指定的财务科目设置");

            // 权限检查：非超管只能删除自己组织机构的数据
            if (!context.User.IsSuperAdmin && item.OrgId != context.User.OrgId)
                return StatusCode((int)HttpStatusCode.Forbidden, "只能删除本组织机构的财务科目设置");

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
            string err;
            if (!_AuthorizationManager.Demand(out err, "B.11")) return StatusCode((int)HttpStatusCode.Forbidden, err);

            var result = new RestoreSubjectConfigurationReturnDto();

            var item = _DbContext.SubjectConfigurations.Find(model.Id);
            if (item == null || !item.IsDelete)
                return NotFound("找不到指定的已删除财务科目设置");

            // 权限检查：非超管只能恢复自己组织机构的数据
            if (!context.User.IsSuperAdmin && item.OrgId != context.User.OrgId)
                return StatusCode((int)HttpStatusCode.Forbidden, "只能恢复本组织机构的财务科目设置");

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