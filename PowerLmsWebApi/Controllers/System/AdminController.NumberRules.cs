using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PowerLms.Data;
using PowerLmsServer;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Dto;
using System.Net;

namespace PowerLmsWebApi.Controllers.System
{
    /// <summary>
    /// 管理员控制器 - 编号规则管理部分
    /// </summary>
    public partial class AdminController
    {
        #region 业务编码规则相关
        /// <summary>
        /// 获取业务编码规则。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">支持通用查询条件。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定类别Id无效。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllJobNumberRuleReturnDto> GetAllJobNumberRule([FromQuery] PagingParamsDtoBase model, [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllJobNumberRuleReturnDto();
            var dbSet = _DbContext.DD_JobNumberRules;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            if (_AccountManager.IsAdmin(context.User))  //若是超管
                coll = coll.Where(c => c.OrgId == null);
            else
            {
                var merchantId = _OrgManager.GetMerchantIdByUserId(context.User.Id);
                if (!merchantId.HasValue) return BadRequest("未知的商户Id");
                if (context.User.OrgId is null) //若没有指定机构
                {
                    coll = coll.Where(c => c.OrgId == merchantId);
                }
                else
                {
                    coll = coll.Where(c => c.OrgId == context.User.OrgId);
                }
            }

            // 使用EfHelper.GenerateWhereAnd进行通用查询条件处理
            coll = EfHelper.GenerateWhereAnd(coll, conditional);

            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 增加业务编码规则记录。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">参数错误。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPost]
        public ActionResult<AddJobNumberRuleReturnDto> AddJobNumberRule(AddJobNumberRuleParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            if (!_AuthorizationManager.Demand(out err, "B.2")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            if (model.Item.BusinessTypeId == ProjectContent.AeId)    //若是空运出口业务
            {
                if (!_AuthorizationManager.Demand(out err, "D0.1.1.2")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (model.Item.BusinessTypeId == ProjectContent.AiId)    //若是空运进口业务
                if (!_AuthorizationManager.Demand(out err, "D1.1.1.2")) return StatusCode((int)HttpStatusCode.Forbidden, err);

            var result = new AddJobNumberRuleReturnDto();
            model.Item.GenerateNewId();
            var id = model.Item.Id;
            model.Item.OrgId = context.User.OrgId;
            _DbContext.DD_JobNumberRules.Add(model.Item);
            _DbContext.SaveChanges();
            result.Id = id;
            return result;
        }

        /// <summary>
        /// 修改业务编码规则记录。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定实体的Id不存在。通常这是Bug.在极端情况下可能是并发问题。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPut]
        public ActionResult<ModifyJobNumberRuleReturnDto> ModifyJobNumberRule(ModifyJobNumberRuleParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            if (model.Items == null || model.Items.Count == 0) return BadRequest($"{nameof(model.Items)} 为空集合。");

            string err;
            if (!_AuthorizationManager.Demand(out err, "B.2")) return StatusCode((int)HttpStatusCode.Forbidden, err);

            if (model.Items.Any(item => item.BusinessTypeId == ProjectContent.AeId))    //若是空运出口业务
                if (!_AuthorizationManager.Demand(out err, "D0.1.1.3")) return StatusCode((int)HttpStatusCode.Forbidden, err);

            var result = new ModifyJobNumberRuleReturnDto();

            try
            {
                // 在修改之前保存OrgId值
                var originalOrgIds = new Dictionary<Guid, Guid?>();
                foreach (var item in model.Items)
                {
                    if (item.Id != Guid.Empty)
                    {
                        var originalEntity = _DbContext.DD_JobNumberRules.AsNoTracking().FirstOrDefault(e => e.Id == item.Id);
                        if (originalEntity != null)
                        {
                            originalOrgIds[item.Id] = originalEntity.OrgId;
                        }
                    }
                }

                // 执行修改操作
                if (!_EntityManager.ModifyWithMarkDelete(model.Items))
                {
                    return new StatusCodeResult(OwHelper.GetLastError());
                }

                // 确保OrgId属性不被修改
                foreach (var item in model.Items)
                {
                    if (originalOrgIds.TryGetValue(item.Id, out var orgId))
                    {
                        var entry = _DbContext.Entry(item);
                        if (entry.State == EntityState.Modified)
                        {
                            entry.Property(e => e.OrgId).IsModified = false;
                            // 确保OrgId值保持不变
                            item.OrgId = orgId;
                        }
                    }
                }

                _DbContext.SaveChanges();
                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "修改业务编码规则记录时出错");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"修改业务编码规则记录时出错: {ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// 删除业务编码规则的记录。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定实体的Id不存在。通常这是Bug.在极端情况下可能是并发问题。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpDelete]
        public ActionResult<RemoveJobNumberRuleReturnDto> RemoveJobNumberRule(RemoveJobNumberRuleParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            if (!_AuthorizationManager.Demand(out err, "B.2")) return StatusCode((int)HttpStatusCode.Forbidden, err);

            var result = new RemoveJobNumberRuleReturnDto();
            var id = model.Id;
            var dbSet = _DbContext.DD_JobNumberRules;
            var item = dbSet.Find(id);
            if (item is null) return BadRequest();
            if (item.BusinessTypeId == ProjectContent.AeId)    //若是空运出口业务
            {
                if (!_AuthorizationManager.Demand(out err, "D0.1.1.4")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (item.BusinessTypeId == ProjectContent.AiId)    //若是空运进口业务
                if (!_AuthorizationManager.Demand(out err, "D1.1.1.4")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            _EntityManager.Remove(item);
            _DbContext.SaveChanges();
            //if (item.DataDicType == 1) //若是简单字典
            //    _DbContext.Database.ExecuteSqlRaw($"delete from {nameof(_DbContext.SimpleDataDics)} where {nameof(SimpleDataDic.DataDicId)}='{id.ToString()}'");
            //else //其他字典待定
            //{

            //}
            return result;
        }

        /// <summary>
        /// 恢复指定的被删除业务编码规则记录。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定实体的Id不存在。通常这是Bug.在极端情况下可能是并发问题。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPost]
        public ActionResult<RestoreJobNumberRuleReturnDto> RestoreJobNumberRule(RestoreJobNumberRuleParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            if (!_AuthorizationManager.Demand(out err, "B.2")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new RestoreJobNumberRuleReturnDto();
            if (!_EntityManager.Restore<JobNumberRule>(model.Id))
            {
                var errResult = new StatusCodeResult(OwHelper.GetLastError()) { };
                return errResult;
            }
            _DbContext.SaveChanges();
            return result;
        }

        #endregion 业务编码规则相关

        #region 其它编码规则相关
        /// <summary>
        /// 获取其它编码规则。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">支持通用查询条件。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定类别Id无效。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllOtherNumberRuleReturnDto> GetAllOtherNumberRule([FromQuery] PagingParamsDtoBase model, [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllOtherNumberRuleReturnDto();
            var dbSet = _DbContext.DD_OtherNumberRules;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            if (_AccountManager.IsAdmin(context.User))  //若是超管
                coll = coll.Where(c => c.OrgId == null);
            else
            {
                var merchantId = _OrgManager.GetMerchantIdByUserId(context.User.Id);
                if (!merchantId.HasValue) return BadRequest("未知的商户Id");
                if (context.User.OrgId is null) //若没有指定机构
                {
                    coll = coll.Where(c => c.OrgId == merchantId);
                }
                else
                {
                    coll = coll.Where(c => c.OrgId == context.User.OrgId);
                }
            }

            // 使用EfHelper.GenerateWhereAnd进行通用查询条件处理
            coll = EfHelper.GenerateWhereAnd(coll, conditional);

            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 增加其它编码规则记录。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">参数错误。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpPost]
        public ActionResult<AddOtherNumberRuleReturnDto> AddOtherNumberRule(AddOtherNumberRuleParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new AddOtherNumberRuleReturnDto();
            model.Item.GenerateNewId();
            var id = model.Item.Id;
            model.Item.OrgId = context.User.OrgId;
            _DbContext.DD_OtherNumberRules.Add(model.Item);
            _DbContext.SaveChanges();
            result.Id = id;
            return result;
        }

        /// <summary>
        /// 修改其它编码规则记录。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定实体的Id不存在。通常这是Bug.在极端情况下可能是并发问题。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpPut]
        public ActionResult<ModifyOtherNumberRuleReturnDto> ModifyOtherNumberRule(ModifyOtherNumberRuleParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyOtherNumberRuleReturnDto();
            if (!_EntityManager.ModifyWithMarkDelete(model.Items))
            {
                var errResult = new StatusCodeResult(OwHelper.GetLastError()) { };
                return errResult;
            }
            model.Items.Select(c => c.Id).ToList().ForEach(c =>
            {
                var entity = _DbContext.DD_OtherNumberRules.Find(c);
                if (entity is null) return;
                _DbContext.Entry(entity).Property(c => c.OrgId).IsModified = false;
            });
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 删除其它编码规则的记录。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定实体的Id不存在。通常这是Bug.在极端情况下可能是并发问题。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpDelete]
        public ActionResult<RemoveOtherNumberRuleReturnDto> RemoveOtherNumberRule(RemoveOtherNumberRuleParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemoveOtherNumberRuleReturnDto();
            var id = model.Id;
            var dbSet = _DbContext.DD_OtherNumberRules;
            var item = dbSet.Find(id);
            if (item is null) return BadRequest();
            _EntityManager.Remove(item);
            _DbContext.SaveChanges();
            //if (item.DataDicType == 1) //若是简单字典
            //    _DbContext.Database.ExecuteSqlRaw($"delete from {nameof(_DbContext.SimpleDataDics)} where {nameof(SimpleDataDic.DataDicId)}='{id.ToString()}'");
            //else //其他字典待定
            //{

            //}
            return result;
        }

        /// <summary>
        /// 恢复指定的被删除其它编码规则记录。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定实体的Id不存在。通常这是Bug.在极端情况下可能是并发问题。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpPost]
        public ActionResult<RestoreOtherNumberRuleReturnDto> RestoreOtherNumberRule(RestoreOtherNumberRuleParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RestoreOtherNumberRuleReturnDto();
            if (!_EntityManager.Restore<OtherNumberRule>(model.Id))
            {
                var errResult = new StatusCodeResult(OwHelper.GetLastError()) { };
                return errResult;
            }
            _DbContext.SaveChanges();
            return result;
        }

        #endregion 其它编码规则相关

    }
}
