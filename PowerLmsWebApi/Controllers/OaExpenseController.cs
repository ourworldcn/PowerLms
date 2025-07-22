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
    /// OA�������뵥��������
    /// ����OA�ճ��������뵥����ɾ�Ĳ������
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
        /// ���캯����
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

        #region OA�������뵥�������

        /// <summary>
        /// ��ȡ����OA�������뵥��
        /// </summary>
        /// <param name="model">��ҳ�Ͳ�ѯ����</param>
        /// <param name="conditional">��ѯ��������ʵ�������������ִ�Сд��
        /// ͨ������д��:�������������ַ������������д�����ö��ŷָ����ַ���������ʱ��֧�������Ҷ���ģ����ѯ����"2024-1-1,2024-1-2"��
        /// ��ǿ��ȡnull��Լ������д"null"��</param>
        /// <returns>OA�������뵥�б�</returns>
        /// <response code="200">δ����ϵͳ�����󡣵����ܳ���Ӧ�ô��󣬾���μ� HasError �� ErrorCode��</response>
        /// <response code="401">��Ч���ơ�</response>
        /// <response code="403">Ȩ�޲��㡣</response>
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

                // �����û�Ȩ�޹�������
                if (!context.User.IsSuperAdmin)
                {
                    // �ǳ���ֻ�ܿ����Լ�����Ļ��Լ��Ǽǵ�
                    dbSet = dbSet.Where(r => r.ApplicantId == context.User.Id || r.CreateBy == context.User.Id);
                }

                // ȷ�������ֵ䲻���ִ�Сд
                var normalizedConditional = conditional != null ?
                    new Dictionary<string, string>(conditional, StringComparer.OrdinalIgnoreCase) :
                    null;

                // Ӧ��ͨ��������ѯ
                var coll = EfHelper.GenerateWhereAnd(dbSet, normalizedConditional);

                // Ӧ��������������ͨ���������棩
                if (!string.IsNullOrEmpty(model.SearchText))
                {
                    coll = coll.Where(r => r.RelatedCustomer.Contains(model.SearchText) ||
                                         r.Remark.Contains(model.SearchText));
                }

                // ����ʱ�䷶Χ����
                if (model.StartDate.HasValue)
                {
                    coll = coll.Where(r => r.ApplyDateTime >= model.StartDate.Value);
                }
                if (model.EndDate.HasValue)
                {
                    coll = coll.Where(r => r.ApplyDateTime <= model.EndDate.Value);
                }

                // ����Ӧ���޸��ٲ�ѯ
                coll = coll.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();

                // ʹ��EntityManager���з�ҳ
                var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
                result.Total = prb.Total;
                result.Result.AddRange(prb.Result);
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "��ȡOA�������뵥�б�ʱ��������");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"��ȡOA�������뵥�б�ʱ��������: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// �����µ�OA�������뵥��
        /// </summary>
        /// <param name="model">���뵥��Ϣ</param>
        /// <returns>�������</returns>
        /// <response code="200">δ����ϵͳ�����󡣵����ܳ���Ӧ�ô��󣬾���μ� HasError �� ErrorCode��</response>
        /// <response code="401">��Ч���ơ�</response>
        /// <response code="403">Ȩ�޲��㡣</response>
        [HttpPost]
        public ActionResult<AddOaExpenseRequisitionReturnDto> AddOaExpenseRequisition(AddOaExpenseRequisitionParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();

            var result = new AddOaExpenseRequisitionReturnDto();

            try
            {
                var entity = model.OaExpenseRequisition;
                entity.GenerateNewId(); // ǿ������Id
                entity.OrgId = context.User.OrgId;
                entity.CreateBy = context.User.Id; // CreateBy���ǵǼ���
                entity.CreateDateTime = OwHelper.WorldNow;

                // ��������ģʽ����������
                if (model.IsRegisterForOthers)
                {
                    // ��Ϊ�Ǽ�ģʽ���Ǽ���Ϊ��ǰ�û���CreateBy�������������û�ѡ��
                    // ������IdӦ����ǰ������
                }
                else
                {
                    // ��������ģʽ�������˺͵Ǽ��˶��ǵ�ǰ�û�
                    entity.ApplicantId = context.User.Id;
                }

                // �������ֶκͽ�������ֶ�
                entity.AuditDateTime = null;
                entity.AuditOperatorId = null;
                entity.SettlementMethod = null; // ���㷽ʽֻ�������ʱָ��
                entity.BankAccountId = null; // �����˻�ֻ�������ʱָ��

                _DbContext.OaExpenseRequisitions.Add(entity);
                _DbContext.SaveChanges();

                result.Id = entity.Id;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "����OA�������뵥ʱ��������");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"����OA�������뵥ʱ��������: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// �޸�OA�������뵥��Ϣ��
        /// </summary>
        /// <param name="model">���뵥��Ϣ</param>
        /// <returns>�޸Ľ��</returns>
        /// <response code="200">δ����ϵͳ�����󡣵����ܳ���Ӧ�ô��󣬾���μ� HasError �� ErrorCode��</response>
        /// <response code="401">��Ч���ơ�</response>
        /// <response code="404">ָ��Id�����뵥�����ڡ�</response>
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
                    result.DebugMessage = "ָ����OA�������뵥������";
                    return result;
                }

                // ���Ȩ�޺�״̬
                if (!existing.CanEdit(_DbContext))
                {
                    result.HasError = true;
                    result.ErrorCode = 403;
                    result.DebugMessage = "���뵥����ˣ��޷��޸�";
                    return result;
                }

                // ����û�Ȩ�ޣ�ֻ���޸��Լ������뵥���Լ��Ǽǵ����뵥��
                if (existing.ApplicantId != context.User.Id && existing.CreateBy != context.User.Id && !context.User.IsSuperAdmin)
                {
                    result.HasError = true;
                    result.ErrorCode = 403;
                    result.DebugMessage = "Ȩ�޲��㣬�޷��޸Ĵ����뵥";
                    return result;
                }

                // ʹ��EntityManager�����޸�
                if (!_EntityManager.Modify(new[] { model.OaExpenseRequisition }))
                {
                    result.HasError = true;
                    result.ErrorCode = 404;
                    result.DebugMessage = "�޸�ʧ�ܣ���������";
                    return result;
                }

                // ȷ�������ֶβ����޸�
                var entry = _DbContext.Entry(model.OaExpenseRequisition);
                entry.Property(e => e.OrgId).IsModified = false; // ����Id����ʱȷ���������޸�
                entry.Property(e => e.CreateBy).IsModified = false;
                entry.Property(e => e.CreateDateTime).IsModified = false;
                entry.Property(e => e.AuditDateTime).IsModified = false;
                entry.Property(e => e.AuditOperatorId).IsModified = false;
                entry.Property(e => e.SettlementMethod).IsModified = false; // ���㷽ʽ��������ͨ�޸��и���
                entry.Property(e => e.BankAccountId).IsModified = false; // �����˻���������ͨ�޸��и���

                _DbContext.SaveChanges();
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "�޸�OA�������뵥ʱ��������");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"�޸�OA�������뵥ʱ��������: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// ɾ��OA�������뵥��
        /// </summary>
        /// <param name="model">ɾ������</param>
        /// <returns>ɾ�����</returns>
        /// <response code="200">δ����ϵͳ�����󡣵����ܳ���Ӧ�ô��󣬾���μ� HasError �� ErrorCode��</response>
        /// <response code="401">��Ч���ơ�</response>
        /// <response code="404">ָ��Id�����뵥�����ڡ�</response>
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
                    // ���Ȩ�޺�״̬
                    if (!entity.CanEdit(_DbContext))
                    {
                        result.HasError = true;
                        result.ErrorCode = 403;
                        result.DebugMessage = $"���뵥����ˣ��޷�ɾ��";
                        return result;
                    }

                    // ����û�Ȩ��
                    if (entity.ApplicantId != context.User.Id && entity.CreateBy != context.User.Id && !context.User.IsSuperAdmin)
                    {
                        result.HasError = true;
                        result.ErrorCode = 403;
                        result.DebugMessage = $"Ȩ�޲��㣬�޷�ɾ�����뵥";
                        return result;
                    }

                    // ɾ����ص���ϸ��¼
                    var items = _DbContext.OaExpenseRequisitionItems.Where(i => i.ParentId == entity.Id);
                    _DbContext.OaExpenseRequisitionItems.RemoveRange(items);

                    // ʹ��EntityManager����ɾ����֧����ɾ����
                    _EntityManager.Remove(entity);
                }

                _DbContext.SaveChanges();
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "ɾ��OA�������뵥ʱ��������");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"ɾ��OA�������뵥ʱ��������: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// ��˻�ȡ�����OA�������뵥��
        /// </summary>
        /// <param name="model">��˲���</param>
        /// <returns>��˽��</returns>
        /// <response code="200">δ����ϵͳ�����󡣵����ܳ���Ӧ�ô��󣬾���μ� HasError �� ErrorCode��</response>
        /// <response code="401">��Ч���ơ�</response>
        /// <response code="403">Ȩ�޲��㡣</response>
        /// <response code="404">ָ��Id�����뵥�����ڡ�</response>
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
                    result.DebugMessage = "ָ����OA�������뵥������";
                    return result;
                }

                // TODO: ���������Ӹ���������Ȩ�޼��
                // ��ʱ�����ܺ����뵥������֯���û����
                if (!context.User.IsSuperAdmin && existing.OrgId != context.User.OrgId)
                {
                    result.HasError = true;
                    result.ErrorCode = 403;
                    result.DebugMessage = "Ȩ�޲��㣬�޷���˴����뵥";
                    return result;
                }

                if (model.IsAudit)
                {
                    // ���ͨ��
                    existing.AuditDateTime = OwHelper.WorldNow;
                    existing.AuditOperatorId = context.User.Id;
                    
                    // ���ʱ�������ý��㷽ʽ�������˻�
                    if (model.SettlementMethod.HasValue)
                    {
                        existing.SettlementMethod = model.SettlementMethod.Value;
                    }
                    if (model.BankAccountId.HasValue)
                    {
                        existing.BankAccountId = model.BankAccountId.Value;
                    }
                    
                    _Logger.LogInformation("���뵥���ͨ���������: {UserId}", context.User.Id);
                }
                else
                {
                    // ȡ�����
                    existing.AuditDateTime = null;
                    existing.AuditOperatorId = null;
                    // ȡ�����ʱ��ս�������ֶ�
                    existing.SettlementMethod = null;
                    existing.BankAccountId = null;
                    _Logger.LogInformation("���뵥ȡ����ˣ�������: {UserId}", context.User.Id);
                }

                _DbContext.SaveChanges();
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "���OA�������뵥ʱ��������");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"���OA�������뵥ʱ��������: {ex.Message}";
            }

            return result;
        }

        #endregion
    }
}