using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OW.EntityFrameworkCore;
using PowerLms.Data;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Dto;
using System.ComponentModel;
using System.Net;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// 税务相关功能控制器 - 税务发票渠道相关功能。
    /// </summary>
    public partial class TaxController : PlControllerBase
    {
        #region 税务发票渠道相关

        /// <summary>
        /// 获取指定ID的税务发票渠道。
        /// </summary>
        /// <param name="model">分页参数</param>
        /// <param name="conditional">查询条件。支持通用查询接口。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllTaxInvoiceChannelReturnDto> GetAllTaxInvoiceChannel([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllTaxInvoiceChannelReturnDto();
            var dbSet = _DbContext.TaxInvoiceChannels;
            var coll = dbSet.AsNoTracking();
            // 使用通用查询条件处理方式
            coll = EfHelper.GenerateWhereAnd(coll, conditional);
            // 应用排序
            coll = coll.OrderBy(model.OrderFieldName, model.IsDesc);
            // 获取分页结果
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 修改税务发票渠道记录。仅能修改显示名称.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定实体的Id不存在。通常这是Bug.在极端情况下可能是并发问题。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPut]
        public ActionResult<ModifyTaxInvoiceChannelReturnDto> ModifyTaxInvoiceChannel(ModifyTaxInvoiceChannelParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyTaxInvoiceChannelReturnDto();
            if (!_EntityManager.Modify(model.Items))
            {
                var errResult = new StatusCodeResult(OwHelper.GetLastError()) { };
                return errResult;
            }
            foreach (var item in model.Items)
            {
                var tic = _DbContext.TaxInvoiceChannels.Find(item.Id);
                var entry = _DbContext.Entry(tic);
                entry.Property(c => c.InvoiceChannel).IsModified = false;
                entry.Property(c => c.InvoiceChannelParams).IsModified = false;
            }
            _DbContext.SaveChanges();
            return result;
        }

        #endregion 税务发票渠道相关
    }
}