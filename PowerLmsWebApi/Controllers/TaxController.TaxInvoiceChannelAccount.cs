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
    /// ˰����ع��ܿ����� - ˰��Ʊ�����˺���ع��ܡ�
    /// </summary>
    public partial class TaxController : PlControllerBase
    {
        #region ˰��Ʊ�����˺���ز���

        /// <summary>
        /// ��ȡ˰��Ʊ�����˺��б�
        /// </summary>
        /// <param name="model">��ҳ����</param>
        /// <param name="conditional">��ѯ����</param>
        /// <returns>˰��Ʊ�����˺��б�</returns>
        /// <response code="200">δ����ϵͳ�����󡣵����ܳ���Ӧ�ô��󣬾���μ� HasError �� ErrorCode ��</response>  
        /// <response code="401">��Ч���ơ�</response>  
        [HttpGet]
        [Description("��ȡ˰��Ʊ�����˺��б�")]
        public ActionResult<GetAllTaxInvoiceChannelAccountReturnDto> GetAllTaxInvoiceChannelAccount(
            [FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();
            var result = new GetAllTaxInvoiceChannelAccountReturnDto();
            var dbSet = _DbContext.Set<TaxInvoiceChannelAccount>();
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            // ʹ��ͨ�ò�ѯ��������
            coll = EfHelper.GenerateWhereAnd(coll, conditional);
            // ��ȡ��ҳ���
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// ���˰��Ʊ�����˺š�
        /// </summary>
        /// <param name="model">��Ӳ���</param>
        /// <returns>�������</returns>
        /// <response code="200">δ����ϵͳ�����󡣵����ܳ���Ӧ�ô��󣬾���μ� HasError �� ErrorCode ��</response>  
        /// <response code="401">��Ч���ơ�</response>  
        [HttpPost]
        [Description("���˰��Ʊ�����˺�")]
        public ActionResult<AddTaxInvoiceChannelAccountReturnDto> AddTaxInvoiceChannelAccount(AddTaxInvoiceChannelAccountParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();
            var result = new AddTaxInvoiceChannelAccountReturnDto();
            try
            {
                // ȷ��Ҫ��ӵ��Ϊ��
                if (model.Item == null)
                {
                    result.ErrorCode = 400;
                    result.DebugMessage = "Ҫ��ӵ�˰��Ʊ�����˺Ų���Ϊ��";
                    return result;
                }
                // �����µ�ID
                model.Item.GenerateNewId();
                // ��ӵ����ݿ�
                _DbContext.Set<TaxInvoiceChannelAccount>().Add(model.Item);
                _DbContext.SaveChanges();
                // ������ӵ�ID
                result.Id = model.Item.Id;
                return result;
            }
            catch (Exception ex)
            {
                result.ErrorCode = 500;
                result.DebugMessage = $"���˰��Ʊ�����˺�ʧ�ܣ�{ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// �޸�˰��Ʊ�����˺š�
        /// </summary>
        /// <param name="model">�޸Ĳ���</param>
        /// <returns>�������</returns>
        /// <response code="200">δ����ϵͳ�����󡣵����ܳ���Ӧ�ô��󣬾���μ� HasError �� ErrorCode ��</response>  
        /// <response code="400">ָ��ʵ���Id�����ڡ�ͨ������Bug.�ڼ�������¿����ǲ������⡣</response>  
        /// <response code="401">��Ч���ơ�</response>  
        [HttpPut]
        [Description("�޸�˰��Ʊ�����˺�")]
        public ActionResult<ModifyTaxInvoiceChannelAccountReturnDto> ModifyTaxInvoiceChannelAccount(ModifyTaxInvoiceChannelAccountParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();
            var result = new ModifyTaxInvoiceChannelAccountReturnDto();
            try
            {
                // ȷ��Ҫ�޸ĵ��Ϊ��
                if (model.Items == null || model.Items.Count == 0)
                {
                    result.ErrorCode = 400;
                    result.DebugMessage = "Ҫ�޸ĵ�˰��Ʊ�����˺Ų���Ϊ��";
                    return result;
                }
                // ʹ��EntityManager�����޸�
                if (!_EntityManager.Modify(model.Items))
                {
                    return new StatusCodeResult(OwHelper.GetLastError());
                }
                _DbContext.SaveChanges();
                return result;
            }
            catch (Exception ex)
            {
                result.ErrorCode = 500;
                result.DebugMessage = $"�޸�˰��Ʊ�����˺�ʧ�ܣ�{ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// ɾ��˰��Ʊ�����˺š�
        /// </summary>
        /// <param name="model">ɾ������</param>
        /// <returns>�������</returns>
        /// <response code="200">δ����ϵͳ�����󡣵����ܳ���Ӧ�ô��󣬾���μ� HasError �� ErrorCode ��</response>  
        /// <response code="400">ָ��ʵ���Id�����ڡ�ͨ������Bug.�ڼ�������¿����ǲ������⡣</response>  
        /// <response code="401">��Ч���ơ�</response>  
        [HttpDelete]
        [Description("ɾ��˰��Ʊ�����˺�")]
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
                    result.DebugMessage = "δ�ҵ�ָ����˰��Ʊ�����˺�";
                    return result;
                }
                // ֱ������ɾ��
                _EntityManager.Remove(item);
                _DbContext.SaveChanges();
                return result;
            }
            catch (Exception ex)
            {
                result.ErrorCode = 500;
                result.DebugMessage = $"ɾ��˰��Ʊ�����˺�ʧ�ܣ�{ex.Message}";
                return result;
            }
        }

        #endregion
    }
}