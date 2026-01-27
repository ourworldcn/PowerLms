using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OW.EntityFrameworkCore;
using PowerLms.Data;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsServer.Helpers;
using PowerLmsWebApi.Dto;
using System.ComponentModel;
using System.Net;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// 税务相关功能控制器 - 税务发票渠道账号相关功能。
    /// </summary>
    public partial class TaxController : PlControllerBase
    {
        #region 税务发票渠道账号相关操作

        /// <summary>
        /// 获取税务发票渠道账号列表。
        /// </summary>
        /// <param name="model">分页参数</param>
        /// <param name="conditional">查询条件</param>
        /// <returns>税务发票渠道账号列表</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        [Description("获取税务发票渠道账号列表")]
        public ActionResult<GetAllTaxInvoiceChannelAccountReturnDto> GetAllTaxInvoiceChannelAccount(
            [FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();
            var result = new GetAllTaxInvoiceChannelAccountReturnDto();
            var dbSet = _DbContext.Set<TaxInvoiceChannelAccount>();
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            // 使用通用查询条件处理
            coll = QueryHelper.GenerateWhereAnd(coll, conditional);
            // 获取分页结果
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 添加税务发票渠道账号。
        /// </summary>
        /// <param name="model">添加参数</param>
        /// <returns>操作结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpPost]
        [Description("添加税务发票渠道账号")]
        public ActionResult<AddTaxInvoiceChannelAccountReturnDto> AddTaxInvoiceChannelAccount(AddTaxInvoiceChannelAccountParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();
            var result = new AddTaxInvoiceChannelAccountReturnDto();
            try
            {
                // 确保要添加的项不为空
                if (model.Item == null)
                {
                    result.ErrorCode = 400;
                    result.DebugMessage = "要添加的税务发票渠道账号不能为空";
                    return result;
                }
                // 生成新的ID
                model.Item.GenerateNewId();
                // 添加到数据库
                _DbContext.Set<TaxInvoiceChannelAccount>().Add(model.Item);
                _DbContext.SaveChanges();
                // 返回添加的ID
                result.Id = model.Item.Id;
                return result;
            }
            catch (Exception ex)
            {
                result.ErrorCode = 500;
                result.DebugMessage = $"添加税务发票渠道账号失败：{ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// 修改税务发票渠道账号。
        /// </summary>
        /// <param name="model">修改参数</param>
        /// <returns>操作结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定实体的Id不存在。通常这是Bug.在极端情况下可能是并发问题。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpPut]
        [Description("修改税务发票渠道账号")]
        public ActionResult<ModifyTaxInvoiceChannelAccountReturnDto> ModifyTaxInvoiceChannelAccount(ModifyTaxInvoiceChannelAccountParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();
            var result = new ModifyTaxInvoiceChannelAccountReturnDto();
            try
            {
                if (model.Items == null || model.Items.Count == 0)
                {
                    result.ErrorCode = 400;
                    result.DebugMessage = "要修改的税务发票渠道账号不能为空";
                    return result;
                }
                var modifiedEntities = new List<TaxInvoiceChannelAccount>();
                if (!_EntityManager.Modify(model.Items, modifiedEntities))
                {
                    return new StatusCodeResult(OwHelper.GetLastError());
                }
                _DbContext.SaveChanges();
                return result;
            }
            catch (Exception ex)
            {
                result.ErrorCode = 500;
                result.DebugMessage = $"修改税务发票渠道账号失败：{ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// 删除税务发票渠道账号。
        /// </summary>
        /// <param name="model">删除参数</param>
        /// <returns>操作结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定实体的Id不存在。通常这是Bug.在极端情况下可能是并发问题。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpDelete]
        [Description("删除税务发票渠道账号")]
        public ActionResult<RemoveTaxInvoiceChannelAccountReturnDto> RemoveTaxInvoiceChannelAccount(RemoveTaxInvoiceChannelAccountParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();
            var result = new RemoveTaxInvoiceChannelAccountReturnDto();
            try
            {
                var id = model.Id;
                var item = _DbContext.Set<TaxInvoiceChannelAccount>().Find(id);
                if (item == null)
                {
                    result.ErrorCode = 404;
                    result.DebugMessage = "未找到指定的税务发票渠道账号";
                    return result;
                }
                // 直接物理删除
                _EntityManager.Remove(item);
                _DbContext.SaveChanges();
                return result;
            }
            catch (Exception ex)
            {
                result.ErrorCode = 500;
                result.DebugMessage = $"删除税务发票渠道账号失败：{ex.Message}";
                return result;
            }
        }

        #endregion
    }
}