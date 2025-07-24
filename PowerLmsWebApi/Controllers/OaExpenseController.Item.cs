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

                    coll = coll.Where(i => i.ParentId != null && accessibleRequisitionIds.Contains(i.ParentId.Value));
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
                var requisition = _DbContext.OaExpenseRequisitions.Find(model.Item.ParentId); // ����Ϊʹ�� Item ����
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

                // ����û�Ȩ�� - ����Ϊ�ɿ����ʹ���
                if ((requisition.ApplicantId.HasValue && requisition.ApplicantId.Value != context.User.Id) &&
                    (requisition.CreateBy.HasValue && requisition.CreateBy.Value != context.User.Id) &&
                    !context.User.IsSuperAdmin)
                {
                    result.HasError = true;
                    result.ErrorCode = 403;
                    result.DebugMessage = "Ȩ�޲��㣬�޷�Ϊ�����뵥�����ϸ";
                    return result;
                }

                var entity = model.Item; // ����Ϊʹ�� Item ����
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
                // ���������ϸ���Ƿ���ں�Ȩ��
                foreach (var item in model.Items)
                {
                    var existing = _DbContext.OaExpenseRequisitionItems.Find(item.Id);
                    if (existing == null)
                    {
                        result.HasError = true;
                        result.ErrorCode = 404;
                        result.DebugMessage = $"ָ����OA�������뵥��ϸ {item.Id} ������";
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

                    // ����û�Ȩ�� - ����Ϊ�ɿ����ʹ���
                    if ((requisition.ApplicantId.HasValue && requisition.ApplicantId.Value != context.User.Id) &&
                        (requisition.CreateBy.HasValue && requisition.CreateBy.Value != context.User.Id) &&
                        !context.User.IsSuperAdmin)
                    {
                        result.HasError = true;
                        result.ErrorCode = 403;
                        result.DebugMessage = "Ȩ�޲��㣬�޷��޸Ĵ���ϸ";
                        return result;
                    }
                }

                // ʹ��EntityManager���������޸�
                if (!_EntityManager.Modify(model.Items))
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
        /// <response code="200">δ����ϵͳ����������Ӧ�ô��󣬾���μ� HasError �� ErrorCode��</response>
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

                    // ����û�Ȩ�� - ����Ϊ�ɿ����ʹ���
                    if ((requisition.ApplicantId.HasValue && requisition.ApplicantId.Value != context.User.Id) &&
                        (requisition.CreateBy.HasValue && requisition.CreateBy.Value != context.User.Id) &&
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

        #region ƾ֤�����ɹ���

        /// <summary>
        /// ����ƾ֤�š�
        /// ���ݽ���ʱ��ͽ����˺����ɷ��ϲ���Ҫ���ƾ֤�š�
        /// </summary>
        /// <param name="model">ƾ֤�����ɲ���</param>
        /// <returns>���ɵ�ƾ֤����Ϣ</returns>
        /// <response code="200">δ����ϵͳ�����󡣵����ܳ���Ӧ�ô��󣬾���μ� HasError �� ErrorCode��</response>
        /// <response code="201">���ɳɹ��������غž��档</response>
        /// <response code="401">��Ч���ơ�</response>
        /// <response code="403">Ȩ�޲��㡣</response>
        /// <response code="404">ָ�������뵥������˺Ų����ڡ�</response>
        [HttpPost]
        public ActionResult<GenerateVoucherNumberReturnDto> GenerateVoucherNumber(GenerateVoucherNumberParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();

            var result = new GenerateVoucherNumberReturnDto();

            try
            {
                // ������뵥�Ƿ����
                var requisition = _DbContext.OaExpenseRequisitions.Find(model.RequisitionId);
                if (requisition == null)
                {
                    result.HasError = true;
                    result.ErrorCode = 404;
                    result.DebugMessage = "ָ����OA�������뵥������";
                    return result;
                }

                // ������뵥�Ƿ������
                if (!requisition.IsAudited())
                {
                    result.HasError = true;
                    result.ErrorCode = 400;
                    result.DebugMessage = "���뵥���������ͨ����������ƾ֤��";
                    return result;
                }

                // ���Ȩ�ޣ�������Ա�򳬹�
                if (!context.User.IsSuperAdmin && requisition.OrgId != context.User.OrgId)
                {
                    result.HasError = true;
                    result.ErrorCode = 403;
                    result.DebugMessage = "Ȩ�޲��㣬�޷�Ϊ�����뵥����ƾ֤��";
                    return result;
                }

                // �������˺��Ƿ����
                var settlementAccount = _DbContext.BankInfos.Find(model.SettlementAccountId);
                if (settlementAccount == null)
                {
                    result.HasError = true;
                    result.ErrorCode = 404;
                    result.DebugMessage = "ָ���Ľ����˺Ų�����";
                    return result;
                }

                // ���ƾ֤���Ƿ�����
                if (string.IsNullOrEmpty(settlementAccount.VoucherCharacter))
                {
                    result.HasError = true;
                    result.ErrorCode = 400;
                    result.DebugMessage = "�����˺�δ����ƾ֤�֣��޷�����ƾ֤��";
                    return result;
                }

                // ����ƾ֤��
                var period = model.SettlementDateTime.Month;
                var voucherCharacter = settlementAccount.VoucherCharacter;

                // ��ȡ���¸�ƾ֤�ֵ�������
                var currentMaxSequence = GetMaxVoucherSequence(period, voucherCharacter, model.SettlementDateTime.Year);
                var nextSequence = currentMaxSequence + 1;

                // ����ƾ֤�ţ���ʽΪ"�ڼ�-ƾ֤��-���"
                var voucherNumber = $"{period}-{voucherCharacter}-{nextSequence}";

                // ����Ƿ�����غ�
                var duplicateExists = CheckVoucherNumberDuplicate(voucherNumber, model.SettlementDateTime.Year);

                result.VoucherNumber = voucherNumber;
                result.VoucherCharacter = voucherCharacter;
                result.Period = period;
                result.SequenceNumber = nextSequence;
                result.HasDuplicateWarning = duplicateExists;

                if (duplicateExists)
                {
                    result.DuplicateWarningMessage = $"ƾ֤�� {voucherNumber} �Ѵ��ڣ���˲��Ƿ��ظ�";
                    _Logger.LogWarning("���ɵ�ƾ֤�Ŵ����ظ�: {VoucherNumber}", voucherNumber);
                    
                    // ����201״̬���ʾ�ɹ����о���
                    return StatusCode(201, result);
                }

                _Logger.LogInformation("�ɹ�����ƾ֤��: {VoucherNumber}�����뵥: {RequisitionId}", 
                    voucherNumber, model.RequisitionId);
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "����ƾ֤��ʱ��������");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"����ƾ֤��ʱ��������: {ex.Message}";
            }

            return result;
        }

        #endregion

        #region ˽�и�������

        /// <summary>
        /// ��ȡָ���ڼ��ƾ֤�ֵ������š�
        /// </summary>
        /// <param name="period">�ڼ䣨�·ݣ�</param>
        /// <param name="voucherCharacter">ƾ֤��</param>
        /// <param name="year">���</param>
        /// <returns>������</returns>
        private int GetMaxVoucherSequence(int period, string voucherCharacter, int year)
        {
            try
            {
                // ��ѯ��������ʹ�ø�ƾ֤�ֵ���ϸ��¼
                var voucherPattern = $"{period}-{voucherCharacter}-";
                
                var maxSequence = _DbContext.OaExpenseRequisitionItems
                    .Where(item => item.VoucherNumber != null && 
                                   item.VoucherNumber.StartsWith(voucherPattern) &&
                                   item.SettlementDateTime.Year == year)
                    .AsEnumerable() // �л����ͻ���������֧�ָ��ӵ��ַ�������
                    .Select(item => ExtractSequenceFromVoucherNumber(item.VoucherNumber, voucherPattern))
                    .Where(seq => seq.HasValue)
                    .DefaultIfEmpty(0)
                    .Max();

                return maxSequence ?? 0;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "��ȡ���ƾ֤���ʱ��������");
                return 0; // ����ʱ����0����1��ʼ
            }
        }

        /// <summary>
        /// ��ƾ֤������ȡ��š�
        /// </summary>
        /// <param name="voucherNumber">ƾ֤��</param>
        /// <param name="pattern">ģʽǰ׺</param>
        /// <returns>���</returns>
        private int? ExtractSequenceFromVoucherNumber(string voucherNumber, string pattern)
        {
            if (string.IsNullOrEmpty(voucherNumber) || !voucherNumber.StartsWith(pattern))
                return null;

            var sequencePart = voucherNumber.Substring(pattern.Length);
            return int.TryParse(sequencePart, out var sequence) ? sequence : (int?)null;
        }

        /// <summary>
        /// ���ƾ֤���Ƿ�����ظ���
        /// </summary>
        /// <param name="voucherNumber">ƾ֤��</param>
        /// <param name="year">���</param>
        /// <returns>�Ƿ�����ظ�</returns>
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
                _Logger.LogError(ex, "���ƾ֤���ظ�ʱ��������");
                return false; // ����ʱ����false��������������
            }
        }

        #endregion
    }
}