using Microsoft.AspNetCore.Mvc;
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
    /// OA�������뵥������ - ��ϸ��������֡�
    /// </summary>
    public partial class OaExpenseController
    {
        #region OA�������뵥��ϸ����

        /// <summary>
        /// ��ȡ����OA�������뵥��ϸ��
        /// </summary>
        /// <param name="model">��ҳ�Ͳ�ѯ����</param>
        /// <param name="conditional">��ѯ��������ʵ�������������ִ�Сд��
        /// ͨ������д��:�������������ַ������������д�����ö��ŷָ����ַ���������ʱ��֧�������Ҷ���ģ����ѯ����"2024-1-1,2024-1-2"��
        /// ��ǿ��ȡnull��Լ������д"null"��</param>
        /// <returns>OA�������뵥��ϸ�б�</returns>
        /// <response code="200">δ����ϵͳ�����󡣵����ܳ���Ӧ�ô��󣬾���μ� HasError �� ErrorCode��</response>
        /// <response code="401">��Ч���ơ�</response>
        /// <response code="403">Ȩ�޲��㡣</response>
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

                // ȷ�������ֵ䲻���ִ�Сд
                var normalizedConditional = conditional != null ?
                    new Dictionary<string, string>(conditional, StringComparer.OrdinalIgnoreCase) :
                    null;

                // Ӧ��ͨ��������ѯ
                var coll = EfHelper.GenerateWhereAnd(dbSet, normalizedConditional);

                // Ȩ�޹��ˣ�ֻ��ʾ�û���Ȩ�޲鿴�����뵥����ϸ
                if (!context.User.IsSuperAdmin)
                {
                    var accessibleRequisitionIds = _DbContext.OaExpenseRequisitions
                        .Where(r => r.OrgId == context.User.OrgId && 
                                   (r.ApplicantId == context.User.Id || r.CreateBy == context.User.Id))
                        .Select(r => r.Id)
                        .ToList();

                    coll = coll.Where(i => accessibleRequisitionIds.Contains(i.ParentId));
                }

                // ����
                coll = coll.OrderBy(model.OrderFieldName, model.IsDesc);

                // ʹ��EntityManager���з�ҳ
                var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
                result.Total = prb.Total;
                result.Result.AddRange(prb.Result);
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "��ȡOA�������뵥��ϸ�б�ʱ��������");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"��ȡOA�������뵥��ϸ�б�ʱ��������: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// �����µ�OA�������뵥��ϸ��
        /// </summary>
        /// <param name="model">��ϸ��Ϣ</param>
        /// <returns>�������</returns>
        /// <response code="200">δ����ϵͳ�����󡣵����ܳ���Ӧ�ô��󣬾���μ� HasError �� ErrorCode��</response>
        /// <response code="401">��Ч���ơ�</response>
        /// <response code="403">Ȩ�޲��㡣</response>
        [HttpPost]
        public ActionResult<AddOaExpenseRequisitionItemReturnDto> AddOaExpenseRequisitionItem(AddOaExpenseRequisitionItemParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();

            var result = new AddOaExpenseRequisitionItemReturnDto();

            try
            {
                // ������뵥�Ƿ���ں�Ȩ��
                var requisition = _DbContext.OaExpenseRequisitions.Find(model.OaExpenseRequisitionItem.ParentId);
                if (requisition == null)
                {
                    result.HasError = true;
                    result.ErrorCode = 404;
                    result.DebugMessage = "ָ����OA�������뵥������";
                    return result;
                }

                // ������뵥״̬
                if (!requisition.CanEdit(_DbContext))
                {
                    result.HasError = true;
                    result.ErrorCode = 403;
                    result.DebugMessage = "���뵥��ǰ״̬�����������ϸ";
                    return result;
                }

                // ����û�Ȩ��
                if (requisition.ApplicantId != context.User.Id && 
                    requisition.CreateBy != context.User.Id && 
                    !context.User.IsSuperAdmin)
                {
                    result.HasError = true;
                    result.ErrorCode = 403;
                    result.DebugMessage = "Ȩ�޲��㣬�޷�Ϊ�����뵥�����ϸ";
                    return result;
                }

                var entity = model.OaExpenseRequisitionItem;
                entity.GenerateNewId(); // ǿ������Id

                _DbContext.OaExpenseRequisitionItems.Add(entity);
                _DbContext.SaveChanges();

                result.Id = entity.Id;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "����OA�������뵥��ϸʱ��������");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"����OA�������뵥��ϸʱ��������: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// �޸�OA�������뵥��ϸ��Ϣ��
        /// </summary>
        /// <param name="model">��ϸ��Ϣ</param>
        /// <returns>�޸Ľ��</returns>
        /// <response code="200">δ����ϵͳ�����󡣵����ܳ���Ӧ�ô��󣬾���μ� HasError �� ErrorCode��</response>
        /// <response code="401">��Ч���ơ�</response>
        /// <response code="404">ָ��Id����ϸ�����ڡ�</response>
        [HttpPut]
        public ActionResult<ModifyOaExpenseRequisitionItemReturnDto> ModifyOaExpenseRequisitionItem(ModifyOaExpenseRequisitionItemParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();

            var result = new ModifyOaExpenseRequisitionItemReturnDto();

            try
            {
                var existing = _DbContext.OaExpenseRequisitionItems.Find(model.OaExpenseRequisitionItem.Id);
                if (existing == null)
                {
                    result.HasError = true;
                    result.ErrorCode = 404;
                    result.DebugMessage = "ָ����OA�������뵥��ϸ������";
                    return result;
                }

                // ������뵥״̬��Ȩ��
                var requisition = _DbContext.OaExpenseRequisitions.Find(existing.ParentId);
                if (requisition == null || !requisition.CanEdit(_DbContext))
                {
                    result.HasError = true;
                    result.ErrorCode = 403;
                    result.DebugMessage = "���뵥��ǰ״̬�������޸���ϸ";
                    return result;
                }

                // ����û�Ȩ��
                if (requisition.ApplicantId != context.User.Id && 
                    requisition.CreateBy != context.User.Id && 
                    !context.User.IsSuperAdmin)
                {
                    result.HasError = true;
                    result.ErrorCode = 403;
                    result.DebugMessage = "Ȩ�޲��㣬�޷��޸Ĵ���ϸ";
                    return result;
                }

                // ʹ��EntityManager�����޸�
                if (!_EntityManager.Modify(new[] { model.OaExpenseRequisitionItem }))
                {
                    result.HasError = true;
                    result.ErrorCode = 404;
                    result.DebugMessage = "�޸�ʧ�ܣ���������";
                    return result;
                }

                _DbContext.SaveChanges();
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "�޸�OA�������뵥��ϸʱ��������");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"�޸�OA�������뵥��ϸʱ��������: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// ɾ��OA�������뵥��ϸ��
        /// </summary>
        /// <param name="model">ɾ������</param>
        /// <returns>ɾ�����</returns>
        /// <response code="200">δ����ϵͳ�����󡣵����ܳ���Ӧ�ô��󣬾���μ� HasError �� ErrorCode��</response>
        /// <response code="401">��Ч���ơ�</response>
        /// <response code="404">ָ��Id����ϸ�����ڡ�</response>
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
                    // ������뵥״̬��Ȩ��
                    var requisition = _DbContext.OaExpenseRequisitions.Find(entity.ParentId);
                    if (requisition == null || !requisition.CanEdit(_DbContext))
                    {
                        result.HasError = true;
                        result.ErrorCode = 403;
                        result.DebugMessage = "���뵥��ǰ״̬������ɾ����ϸ";
                        return result;
                    }

                    // ����û�Ȩ��
                    if (requisition.ApplicantId != context.User.Id && 
                        requisition.CreateBy != context.User.Id && 
                        !context.User.IsSuperAdmin)
                    {
                        result.HasError = true;
                        result.ErrorCode = 403;
                        result.DebugMessage = "Ȩ�޲��㣬�޷�ɾ������ϸ";
                        return result;
                    }

                    _EntityManager.Remove(entity);
                }

                _DbContext.SaveChanges();
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "ɾ��OA�������뵥��ϸʱ��������");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"ɾ��OA�������뵥��ϸʱ��������: {ex.Message}";
            }

            return result;
        }

        #endregion
    }
}