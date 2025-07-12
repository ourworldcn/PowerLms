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
    /// ˰����ع��ܿ����� - ˰��Ʊ������ع��ܡ�
    /// </summary>
    public partial class TaxController : PlControllerBase
    {
        #region ˰��Ʊ�������

        /// <summary>
        /// ��ȡָ��ID��˰��Ʊ������
        /// </summary>
        /// <param name="model">��ҳ����</param>
        /// <param name="conditional">��ѯ������֧��ͨ�ò�ѯ�ӿڡ�</param>
        /// <returns></returns>
        /// <response code="200">δ����ϵͳ�����󡣵����ܳ���Ӧ�ô��󣬾���μ� HasError �� ErrorCode ��</response>  
        /// <response code="401">��Ч���ơ�</response>  
        [HttpGet]
        public ActionResult<GetAllTaxInvoiceChannelReturnDto> GetAllTaxInvoiceChannel([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllTaxInvoiceChannelReturnDto();
            var dbSet = _DbContext.TaxInvoiceChannels;
            var coll = dbSet.AsNoTracking();
            // ʹ��ͨ�ò�ѯ��������ʽ
            coll = EfHelper.GenerateWhereAnd(coll, conditional);
            // Ӧ������
            coll = coll.OrderBy(model.OrderFieldName, model.IsDesc);
            // ��ȡ��ҳ���
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// �޸�˰��Ʊ������¼�������޸���ʾ����.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">δ����ϵͳ�����󡣵����ܳ���Ӧ�ô��󣬾���μ� HasError �� ErrorCode ��</response>  
        /// <response code="400">ָ��ʵ���Id�����ڡ�ͨ������Bug.�ڼ�������¿����ǲ������⡣</response>  
        /// <response code="401">��Ч���ơ�</response>  
        /// <response code="403">Ȩ�޲��㡣</response>  
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

        #endregion ˰��Ʊ�������
    }
}