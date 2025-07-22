using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OW.Data;
using PowerLms.Data;
using PowerLms.Data.OA;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Dto;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// OA费用申请单控制器。
    /// 处理OA日常费用申请单的增删改查操作。
    /// </summary>
    [ApiController]
    [Route("api/[controller]/[action]")]
    public partial class OaExpenseController : ControllerBase
    {
        private readonly PowerLmsUserDbContext _DbContext;
        private readonly IServiceProvider _ServiceProvider;
        private readonly AccountManager _AccountManager;
        private readonly ILogger<OaExpenseController> _Logger;
        private readonly EntityManager _EntityManager;

        /// <summary>
        /// 构造函数。
        /// </summary>
        public OaExpenseController(PowerLmsUserDbContext dbContext, 
            IServiceProvider serviceProvider, 
            AccountManager accountManager,
            ILogger<OaExpenseController> logger,
            EntityManager entityManager)
        {
            _DbContext = dbContext;
            _ServiceProvider = serviceProvider;
            _AccountManager = accountManager;
            _Logger = logger;
            _EntityManager = entityManager;
        }

        #region OA费用申请单主表操作

        /// <summary>
        /// 获取所有OA费用申请单。
        /// </summary>
        /// <param name="model">分页和查询参数</param>
        /// <param name="conditional">查询的条件。实体属性名不区分大小写。
        /// 通用条件写法:所有条件都是字符串，对区间的写法是用逗号分隔（字符串类型暂时不支持区间且都是模糊查询）如"2024-1-1,2024-1-2"。
        /// 对强制取null的约束，则写"null"。</param>
        /// <returns>OA费用申请单列表</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="403">权限不足。</response>
        [HttpGet]
        public ActionResult<GetAllOaExpenseRequisitionReturnDto> GetAllOaExpenseRequisition([FromQuery] GetAllOaExpenseRequisitionParamsDto model,
            [FromQuery][ModelBinder(typeof(DotKeyDictionaryModelBinder))] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();

            var result = new GetAllOaExpenseRequisitionReturnDto();

            try
            {
                var dbSet = _DbContext.OaExpenseRequisitions.Where(c => c.OrgId == context.User.OrgId);

                // 根据用户权限过滤数据
                if (!context.User.IsSuperAdmin)
                {
                    // 非超管只能看到自己申请的或自己登记的
                    dbSet = dbSet.Where(r => r.ApplicantId == context.User.Id || r.CreateBy == context.User.Id);
                }

                // 确保条件字典不区分大小写
                var normalizedConditional = conditional != null ?
                    new Dictionary<string, string>(conditional, StringComparer.OrdinalIgnoreCase) :
                    null;

                // 应用通用条件查询
                var coll = EfHelper.GenerateWhereAnd(dbSet, normalizedConditional);

                // 应用搜索条件（与通用条件并存）
                if (!string.IsNullOrEmpty(model.SearchText))
                {
                    coll = coll.Where(r => r.RelatedCustomer.Contains(model.SearchText) ||
                                         r.Remark.Contains(model.SearchText));
                }

                // 申请时间范围过滤
                if (model.StartDate.HasValue)
                {
                    coll = coll.Where(r => r.ApplyDateTime >= model.StartDate.Value);
                }
                if (model.EndDate.HasValue)
                {
                    coll = coll.Where(r => r.ApplyDateTime <= model.EndDate.Value);
                }

                // 排序并应用无跟踪查询
                coll = coll.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();

                // 使用EntityManager进行分页
                var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
                result.Total = prb.Total;
                result.Result.AddRange(prb.Result);
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "获取OA费用申请单列表时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"获取OA费用申请单列表时发生错误: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// 创建新的OA费用申请单。
        /// </summary>
        /// <param name="model">申请单信息</param>
        /// <returns>创建结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="403">权限不足。</response>
        [HttpPost]
        public ActionResult<AddOaExpenseRequisitionReturnDto> AddOaExpenseRequisition(AddOaExpenseRequisitionParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();

            var result = new AddOaExpenseRequisitionReturnDto();

            try
            {
                var entity = model.OaExpenseRequisition;
                entity.GenerateNewId(); // 强制生成Id
                entity.OrgId = context.User.OrgId;
                entity.CreateBy = context.User.Id; // CreateBy就是登记人
                entity.CreateDateTime = OwHelper.WorldNow;

                // 根据申请模式设置申请人
                if (model.IsRegisterForOthers)
                {
                    // 代为登记模式：登记人为当前用户（CreateBy），申请人由用户选择
                    // 申请人Id应该在前端设置
                }
                else
                {
                    // 主动申请模式：申请人和登记人都是当前用户
                    entity.ApplicantId = context.User.Id;
                }

                // 清空审核字段和结算相关字段
                entity.AuditDateTime = null;
                entity.AuditOperatorId = null;
                entity.SettlementMethod = null; // 结算方式只能在审核时指定
                entity.BankAccountId = null; // 银行账户只能在审核时指定

                _DbContext.OaExpenseRequisitions.Add(entity);
                _DbContext.SaveChanges();

                result.Id = entity.Id;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "创建OA费用申请单时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"创建OA费用申请单时发生错误: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// 修改OA费用申请单信息。
        /// </summary>
        /// <param name="model">申请单信息</param>
        /// <returns>修改结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="404">指定Id的申请单不存在。</response>
        [HttpPut]
        public ActionResult<ModifyOaExpenseRequisitionReturnDto> ModifyOaExpenseRequisition(ModifyOaExpenseRequisitionParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();

            var result = new ModifyOaExpenseRequisitionReturnDto();

            try
            {
                var existing = _DbContext.OaExpenseRequisitions.Find(model.OaExpenseRequisition.Id);
                if (existing == null)
                {
                    result.HasError = true;
                    result.ErrorCode = 404;
                    result.DebugMessage = "指定的OA费用申请单不存在";
                    return result;
                }

                // 检查权限和状态
                if (!existing.CanEdit(_DbContext))
                {
                    result.HasError = true;
                    result.ErrorCode = 403;
                    result.DebugMessage = "申请单已审核，无法修改";
                    return result;
                }

                // 检查用户权限（只能修改自己的申请单或自己登记的申请单）
                if (existing.ApplicantId != context.User.Id && existing.CreateBy != context.User.Id && !context.User.IsSuperAdmin)
                {
                    result.HasError = true;
                    result.ErrorCode = 403;
                    result.DebugMessage = "权限不足，无法修改此申请单";
                    return result;
                }

                // 使用EntityManager进行修改
                if (!_EntityManager.Modify(new[] { model.OaExpenseRequisition }))
                {
                    result.HasError = true;
                    result.ErrorCode = 404;
                    result.DebugMessage = "修改失败，请检查数据";
                    return result;
                }

                // 确保保护字段不被修改
                var entry = _DbContext.Entry(model.OaExpenseRequisition);
                entry.Property(e => e.OrgId).IsModified = false; // 机构Id增加时确定，不可修改
                entry.Property(e => e.CreateBy).IsModified = false;
                entry.Property(e => e.CreateDateTime).IsModified = false;
                entry.Property(e => e.AuditDateTime).IsModified = false;
                entry.Property(e => e.AuditOperatorId).IsModified = false;
                entry.Property(e => e.SettlementMethod).IsModified = false; // 结算方式不能在普通修改中更改
                entry.Property(e => e.BankAccountId).IsModified = false; // 银行账户不能在普通修改中更改

                _DbContext.SaveChanges();
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "修改OA费用申请单时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"修改OA费用申请单时发生错误: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// 删除OA费用申请单。
        /// </summary>
        /// <param name="model">删除参数</param>
        /// <returns>删除结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="404">指定Id的申请单不存在。</response>
        [HttpDelete]
        public ActionResult<RemoveOaExpenseRequisitionReturnDto> RemoveOaExpenseRequisition(RemoveOaExpenseRequisitionParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();

            var result = new RemoveOaExpenseRequisitionReturnDto();

            try
            {
                var entities = _DbContext.OaExpenseRequisitions.Where(e => model.Ids.Contains(e.Id)).ToList();

                foreach (var entity in entities)
                {
                    // 检查权限和状态
                    if (!entity.CanEdit(_DbContext))
                    {
                        result.HasError = true;
                        result.ErrorCode = 403;
                        result.DebugMessage = $"申请单已审核，无法删除";
                        return result;
                    }

                    // 检查用户权限
                    if (entity.ApplicantId != context.User.Id && entity.CreateBy != context.User.Id && !context.User.IsSuperAdmin)
                    {
                        result.HasError = true;
                        result.ErrorCode = 403;
                        result.DebugMessage = $"权限不足，无法删除申请单";
                        return result;
                    }

                    // 删除相关的明细记录
                    var items = _DbContext.OaExpenseRequisitionItems.Where(i => i.ParentId == entity.Id);
                    _DbContext.OaExpenseRequisitionItems.RemoveRange(items);

                    // 使用EntityManager进行删除（支持软删除）
                    _EntityManager.Remove(entity);
                }

                _DbContext.SaveChanges();
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "删除OA费用申请单时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"删除OA费用申请单时发生错误: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// 审核或取消审核OA费用申请单。
        /// </summary>
        /// <param name="model">审核参数</param>
        /// <returns>审核结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="403">权限不足。</response>
        /// <response code="404">指定Id的申请单不存在。</response>
        [HttpPost]
        public ActionResult<AuditOaExpenseRequisitionReturnDto> AuditOaExpenseRequisition(AuditOaExpenseRequisitionParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();

            var result = new AuditOaExpenseRequisitionReturnDto();

            try
            {
                var existing = _DbContext.OaExpenseRequisitions.Find(model.RequisitionId);
                if (existing == null)
                {
                    result.HasError = true;
                    result.ErrorCode = 404;
                    result.DebugMessage = "指定的OA费用申请单不存在";
                    return result;
                }

                // TODO: 这里可以添加更具体的审核权限检查
                // 暂时允许超管和申请单所在组织的用户审核
                if (!context.User.IsSuperAdmin && existing.OrgId != context.User.OrgId)
                {
                    result.HasError = true;
                    result.ErrorCode = 403;
                    result.DebugMessage = "权限不足，无法审核此申请单";
                    return result;
                }

                if (model.IsAudit)
                {
                    // 审核通过
                    existing.AuditDateTime = OwHelper.WorldNow;
                    existing.AuditOperatorId = context.User.Id;
                    
                    // 审核时可以设置结算方式和银行账户
                    if (model.SettlementMethod.HasValue)
                    {
                        existing.SettlementMethod = model.SettlementMethod.Value;
                    }
                    if (model.BankAccountId.HasValue)
                    {
                        existing.BankAccountId = model.BankAccountId.Value;
                    }
                    
                    _Logger.LogInformation("申请单审核通过，审核人: {UserId}", context.User.Id);
                }
                else
                {
                    // 取消审核
                    existing.AuditDateTime = null;
                    existing.AuditOperatorId = null;
                    // 取消审核时清空结算相关字段
                    existing.SettlementMethod = null;
                    existing.BankAccountId = null;
                    _Logger.LogInformation("申请单取消审核，操作人: {UserId}", context.User.Id);
                }

                _DbContext.SaveChanges();
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "审核OA费用申请单时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"审核OA费用申请单时发生错误: {ex.Message}";
            }

            return result;
        }

        #endregion
    }
}