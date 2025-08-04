﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OW.Data;
using PowerLms.Data.OA;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Dto;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// OA费用申请单控制器 - 明细项操作部分。
    /// </summary>
    public partial class OaExpenseController
    {
        #region OA费用申请单明细操作

        /// <summary>
        /// 获取所有OA费用申请单明细。
        /// </summary>
        /// <param name="model">分页和查询参数</param>
        /// <param name="conditional">查询的条件。实体属性名不区分大小写。
        /// 通用条件写法:所有条件都是字符串，对区间的写法是用逗号分隔（字符串类型暂时不支持区间且都是模糊查询）如"2024-1-1,2024-1-2"。
        /// 对强制取null的约束，则写"null"。</param>
        /// <returns>OA费用申请单明细列表</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="403">权限不足。</response>
        [HttpGet]
        public ActionResult<GetAllOaExpenseRequisitionItemReturnDto> GetAllOaExpenseRequisitionItem([FromQuery] PagingParamsDtoBase model,
            [FromQuery][ModelBinder(typeof(DotKeyDictionaryModelBinder))] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();

            var result = new GetAllOaExpenseRequisitionItemReturnDto();

            try
            {
                var dbSet = _DbContext.OaExpenseRequisitionItems;

                // 确保条件字典不区分大小写
                var normalizedConditional = conditional != null ?
                    new Dictionary<string, string>(conditional, StringComparer.OrdinalIgnoreCase) :
                    null;

                // 应用通用条件查询
                var coll = EfHelper.GenerateWhereAnd(dbSet, normalizedConditional);

                // 权限过滤：只显示用户有权限查看的申请单的明细（废弃ApplicantId，统一使用CreateBy）
                if (!context.User.IsSuperAdmin)
                {
                    var accessibleRequisitionIds = _DbContext.OaExpenseRequisitions
                        .Where(r => r.OrgId == context.User.OrgId && r.CreateBy == context.User.Id)
                        .Select(r => r.Id)
                        .ToList();

                    coll = coll.Where(i => i.ParentId != null && accessibleRequisitionIds.Contains(i.ParentId.Value));
                }

                // 排序
                coll = coll.OrderBy(model.OrderFieldName, model.IsDesc);

                // 使用EntityManager进行分页
                var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
                result.Total = prb.Total;
                result.Result.AddRange(prb.Result);
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "获取OA费用申请单明细列表时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"获取OA费用申请单明细列表时发生错误: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// 创建新的OA费用申请单明细。
        /// </summary>
        /// <param name="model">明细信息</param>
        /// <returns>创建结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="403">权限不足。</response>
        [HttpPost]
        public ActionResult<AddOaExpenseRequisitionItemReturnDto> AddOaExpenseRequisitionItem(AddOaExpenseRequisitionItemParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();

            var result = new AddOaExpenseRequisitionItemReturnDto();

            try
            {
                // 检查申请单是否存在和权限
                var requisition = _DbContext.OaExpenseRequisitions.Find(model.Item.ParentId);
                if (requisition == null)
                {
                    result.HasError = true;
                    result.ErrorCode = 404;
                    result.DebugMessage = "指定的OA费用申请单不存在";
                    return result;
                }

                // 检查申请单状态：结算后不能修改明细项
                if (!requisition.CanEditItems(_DbContext))
                {
                    result.HasError = true;
                    result.ErrorCode = 403;
                    result.DebugMessage = "申请单当前状态不允许添加明细";
                    return result;
                }

                // 检查用户权限：只能为自己创建/登记的申请单添加明细（废弃ApplicantId，统一使用CreateBy）
                if (requisition.CreateBy.HasValue && requisition.CreateBy.Value != context.User.Id && !context.User.IsSuperAdmin)
                {
                    result.HasError = true;
                    result.ErrorCode = 403;
                    result.DebugMessage = "权限不足，无法为此申请单添加明细";
                    return result;
                }

                var entity = model.Item;
                entity.GenerateNewId();

                _DbContext.OaExpenseRequisitionItems.Add(entity);
                _DbContext.SaveChanges();

                result.Id = entity.Id;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "创建OA费用申请单明细时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"创建OA费用申请单明细时发生错误: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// 修改OA费用申请单明细信息。
        /// </summary>
        /// <param name="model">明细信息</param>
        /// <returns>修改结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="404">指定Id的明细不存在。</response>
        [HttpPut]
        public ActionResult<ModifyOaExpenseRequisitionItemReturnDto> ModifyOaExpenseRequisitionItem(ModifyOaExpenseRequisitionItemParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();

            var result = new ModifyOaExpenseRequisitionItemReturnDto();

            try
            {
                // 检查所有明细项是否存在和权限
                foreach (var item in model.Items)
                {
                    var existing = _DbContext.OaExpenseRequisitionItems.Find(item.Id);
                    if (existing == null)
                    {
                        result.HasError = true;
                        result.ErrorCode = 404;
                        result.DebugMessage = $"指定的OA费用申请单明细 {item.Id} 不存在";
                        return result;
                    }

                    // 检查申请单状态和权限
                    var requisition = _DbContext.OaExpenseRequisitions.Find(existing.ParentId);
                    if (requisition == null || !requisition.CanEditItems(_DbContext))
                    {
                        result.HasError = true;
                        result.ErrorCode = 403;
                        result.DebugMessage = "申请单当前状态不允许修改明细";
                        return result;
                    }

                    // 检查用户权限：只能修改自己创建/登记申请单的明细（废弃ApplicantId，统一使用CreateBy）
                    if (requisition.CreateBy.HasValue && requisition.CreateBy.Value != context.User.Id && !context.User.IsSuperAdmin)
                    {
                        result.HasError = true;
                        result.ErrorCode = 403;
                        result.DebugMessage = "权限不足，无法修改此明细";
                        return result;
                    }
                }

                // 使用EntityManager进行批量修改
                if (!_EntityManager.Modify(model.Items))
                {
                    result.HasError = true;
                    result.ErrorCode = 404;
                    result.DebugMessage = "修改失败，请检查数据";
                    return result;
                }

                _DbContext.SaveChanges();
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "修改OA费用申请单明细时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"修改OA费用申请单明细时发生错误: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// 删除OA费用申请单明细。
        /// </summary>
        /// <param name="model">删除参数</param>
        /// <returns>删除结果</returns>
        /// <response code="200">未发生系统级错误。如有应用错误，具体参见 HasError 和 ErrorCode。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="404">指定Id的明细不存在。</response>
        [HttpDelete]
        public ActionResult<RemoveOaExpenseRequisitionItemReturnDto> RemoveOaExpenseRequisitionItem(RemoveOaExpenseRequisitionItemParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();

            var result = new RemoveOaExpenseRequisitionItemReturnDto();

            try
            {
                var entities = _DbContext.OaExpenseRequisitionItems.Where(e => model.Ids.Contains(e.Id)).ToList();

                foreach (var entity in entities)
                {
                    // 检查申请单状态和权限
                    var requisition = _DbContext.OaExpenseRequisitions.Find(entity.ParentId);
                    if (requisition == null || !requisition.CanEditItems(_DbContext))
                    {
                        result.HasError = true;
                        result.ErrorCode = 403;
                        result.DebugMessage = "申请单当前状态不允许删除明细";
                        return result;
                    }

                    // 检查用户权限：只能删除自己创建/登记申请单的明细（废弃ApplicantId，统一使用CreateBy）
                    if (requisition.CreateBy.HasValue && requisition.CreateBy.Value != context.User.Id && !context.User.IsSuperAdmin)
                    {
                        result.HasError = true;
                        result.ErrorCode = 403;
                        result.DebugMessage = "权限不足，无法删除此明细";
                        return result;
                    }

                    _EntityManager.Remove(entity);
                }

                _DbContext.SaveChanges();
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "删除OA费用申请单明细时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"删除OA费用申请单明细时发生错误: {ex.Message}";
            }

            return result;
        }

        #endregion

        #region 私有辅助方法

        /// <summary>
        /// 获取指定期间和凭证字的最大序号。
        /// </summary>
        /// <param name="period">期间（月份）</param>
        /// <param name="voucherCharacter">凭证字</param>
        /// <param name="year">年份</param>
        /// <returns>最大序号</returns>
        private int GetMaxVoucherSequence(int period, string voucherCharacter, int year)
        {
            try
            {
                // 查询当月所有使用该凭证字的明细记录
                var voucherPattern = $"{period}-{voucherCharacter}-";
                
                var maxSequence = _DbContext.OaExpenseRequisitionItems
                    .Where(item => item.VoucherNumber != null && 
                                   item.VoucherNumber.StartsWith(voucherPattern) &&
                                   item.SettlementDateTime.Year == year)
                    .AsEnumerable() // 切换到客户端评估以支持复杂的字符串操作
                    .Select(item => ExtractSequenceFromVoucherNumber(item.VoucherNumber, voucherPattern))
                    .Where(seq => seq.HasValue)
                    .DefaultIfEmpty(0)
                    .Max();

                return maxSequence ?? 0;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "获取最大凭证序号时发生错误");
                return 0; // 出错时返回0，从1开始
            }
        }

        /// <summary>
        /// 从凭证号中提取序号。
        /// </summary>
        /// <param name="voucherNumber">凭证号</param>
        /// <param name="pattern">模式前缀</param>
        /// <returns>序号</returns>
        private int? ExtractSequenceFromVoucherNumber(string voucherNumber, string pattern)
        {
            if (string.IsNullOrEmpty(voucherNumber) || !voucherNumber.StartsWith(pattern))
                return null;

            var sequencePart = voucherNumber.Substring(pattern.Length);
            return int.TryParse(sequencePart, out var sequence) ? sequence : (int?)null;
        }

        /// <summary>
        /// 检查凭证号是否存在重复。
        /// </summary>
        /// <param name="voucherNumber">凭证号</param>
        /// <param name="year">年份</param>
        /// <returns>是否存在重复</returns>
        private bool CheckVoucherNumberDuplicate(string voucherNumber, int year)
        {
            try
            {
                return _DbContext.OaExpenseRequisitionItems
                    .Any(item => item.VoucherNumber == voucherNumber && 
                                item.SettlementDateTime.Year == year);
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "检查凭证号重复时发生错误");
                return false; // 出错时返回false，避免阻塞流程
            }
        }

        #endregion
    }
}