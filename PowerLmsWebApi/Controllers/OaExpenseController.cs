using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OW.Data;
using PowerLms.Data;
using PowerLms.Data.OA;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Dto;
using AutoMapper;
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
    public partial class OaExpenseController : PlControllerBase
    {
        private readonly PowerLmsUserDbContext _DbContext;
        private readonly IServiceProvider _ServiceProvider;
        private readonly AccountManager _AccountManager;
        private readonly ILogger<OaExpenseController> _Logger;
        private readonly EntityManager _EntityManager;
        private readonly OwWfManager _WfManager;
        private readonly IMapper _Mapper;

        /// <summary>
        /// ���캯����
        /// </summary>
        public OaExpenseController(PowerLmsUserDbContext dbContext, 
            IServiceProvider serviceProvider, 
            AccountManager accountManager,
            ILogger<OaExpenseController> logger,
            EntityManager entityManager,
            OwWfManager wfManager,
            IMapper mapper)
        {
            _DbContext = dbContext;
            _ServiceProvider = serviceProvider;
            _AccountManager = accountManager;
            _Logger = logger;
            _EntityManager = entityManager;
            _WfManager = wfManager;
            _Mapper = mapper;
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

                // �ǳ����û�Ȩ�޹���
                if (!context.User.IsSuperAdmin)
                {
                    // �ǳ���ֻ�ܿ��Լ�����Ļ��Լ��Ǽǵ�
                    dbSet = dbSet.Where(r => r.ApplicantId == context.User.Id || r.CreateBy == context.User.Id);
                }

                // ȷ�������ֵ䲻���ִ�Сд
                var normalizedConditional = conditional != null ?
                    new Dictionary<string, string>(conditional, StringComparer.OrdinalIgnoreCase) :
                    null;

                // Ӧ��ͨ��������ѯ
                var coll = EfHelper.GenerateWhereAnd(dbSet, normalizedConditional);

                // Ӧ�������ı��������ؿͻ��ͱ�ע��
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

                // ����Ӧ�����޸ĵĲ�ѯ
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
                var entity = model.Item; // ����Ϊʹ�� Item ����
                entity.GenerateNewId(); // ǿ������Id
                entity.OrgId = context.User.OrgId;
                entity.CreateBy = context.User.Id; // CreateBy���ǵǼ���
                entity.CreateDateTime = OwHelper.WorldNow;

                // ��������ģʽ�����˵Ǽ�
                if (model.IsRegisterForOthers)
                {
                    // ��Ϊ�Ǽ�ģʽ���Ǽ���Ϊ��ǰ�û���CreateBy�������������û�ѡ��
                    // ������IdӦ��ǰ�˴���
                }
                else
                {
                    // �Լ�����ģʽ�������˺͵Ǽ��˶��ǵ�ǰ�û�
                    entity.ApplicantId = context.User.Id;
                }

                // ��ʼ������ֶ�
                entity.AuditDateTime = null;
                entity.AuditOperatorId = null;

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
                // �������ʵ���Ƿ���ں�Ȩ��
                foreach (var item in model.Items)
                {
                    var existing = _DbContext.OaExpenseRequisitions.Find(item.Id);
                    if (existing == null)
                    {
                        result.HasError = true;
                        result.ErrorCode = 404;
                        result.DebugMessage = $"ָ����OA�������뵥 {item.Id} ������";
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

                    // ����û�Ȩ�ޣ�ֻ���޸��Լ���������뵥���Լ��Ǽǵ����뵥
                    if ((existing.ApplicantId.HasValue && existing.ApplicantId.Value != context.User.Id) && 
                        (existing.CreateBy.HasValue && existing.CreateBy.Value != context.User.Id) && 
                        !context.User.IsSuperAdmin)
                    {
                        result.HasError = true;
                        result.ErrorCode = 403;
                        result.DebugMessage = "Ȩ�޲��㣬�޷��޸Ĵ����뵥";
                        return result;
                    }
                }

                // ʹ��EntityManager�����޸�
                if (!_EntityManager.Modify(model.Items))
                {
                    result.HasError = true;
                    result.ErrorCode = 404;
                    result.DebugMessage = "�޸�ʧ�ܣ���������";
                    return result;
                }

                // ȷ�������ֶβ����޸�
                foreach (var item in model.Items)
                {
                    var entry = _DbContext.Entry(item);
                    entry.Property(e => e.OrgId).IsModified = false; // ����Id����ʱȷ���������޸�
                    entry.Property(e => e.CreateBy).IsModified = false;
                    entry.Property(e => e.CreateDateTime).IsModified = false;
                    entry.Property(e => e.AuditDateTime).IsModified = false;
                    entry.Property(e => e.AuditOperatorId).IsModified = false;
                }

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
                    if ((entity.ApplicantId.HasValue && entity.ApplicantId.Value != context.User.Id) && 
                        (entity.CreateBy.HasValue && entity.CreateBy.Value != context.User.Id) && 
                        !context.User.IsSuperAdmin)
                    {
                        result.HasError = true;
                        result.ErrorCode = 403;
                        result.DebugMessage = $"Ȩ�޲��㣬�޷�ɾ�����뵥";
                        return result;
                    }

                    // ɾ����������ϸ��¼
                    var items = _DbContext.OaExpenseRequisitionItems.Where(i => i.ParentId == entity.Id);
                    _DbContext.OaExpenseRequisitionItems.RemoveRange(items);

                    // ʹ��EntityManager����ɾ����֧�ּ���ɾ����
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

                // TODO: ������Ҫ��Ӹ����ӵ����Ȩ�޼��
                // ��ʱֻ�����ܺ����뵥������֯���û����
                if (!context.User.IsSuperAdmin && existing.OrgId != context.User.OrgId)
                {
                    result.HasError = true;
                    result.ErrorCode = 403;
                    result.DebugMessage = "Ȩ�޲��㣬�޷���˴����뵥";
                    return result;
                }

                if (model.IsAudit)
                {
                    // ���ͨ��ǰ���н��һ����У��
                    if (!existing.ValidateAmountConsistency(_DbContext))
                    {
                        var itemsSum = existing.GetItemsAmountSum(_DbContext);
                        result.HasError = true;
                        result.ErrorCode = 400;
                        result.DebugMessage = $"��ϸ���ϼ�({itemsSum:F2})���������({existing.Amount:F2})��һ�£�������ϸ������ύ���";
                        _Logger.LogWarning("���뵥{RequisitionId}���У��ʧ��: �������={MainAmount:F2}, ��ϸ�ϼ�={ItemsSum:F2}", 
                            model.RequisitionId, existing.Amount, itemsSum);
                        return result;
                    }

                    // ����Ƿ�����ϸ������Ҫ�Ļ���
                    var itemsCount = existing.GetItems(_DbContext).Count();
                    if (itemsCount == 0)
                    {
                        result.HasError = true;
                        result.ErrorCode = 400;
                        result.DebugMessage = "���뵥�����������һ����ϸ��������";
                        _Logger.LogWarning("���뵥{RequisitionId}���ʧ��: û����ϸ��", model.RequisitionId);
                        return result;
                    }

                    // ���ͨ��
                    existing.AuditDateTime = OwHelper.WorldNow;
                    existing.AuditOperatorId = context.User.Id;
                    
                    _Logger.LogInformation("���뵥���ͨ���������: {UserId}, ���뵥ID: {RequisitionId}, �������: {Amount:F2}, ��ϸ����: {ItemsCount}", 
                        context.User.Id, model.RequisitionId, existing.Amount, itemsCount);
                }
                else
                {
                    // ȡ�����
                    existing.AuditDateTime = null;
                    existing.AuditOperatorId = null;
                    _Logger.LogInformation("���뵥ȡ����ˣ�������: {UserId}, ���뵥ID: {RequisitionId}", 
                        context.User.Id, model.RequisitionId);
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

        /// <summary>
        /// ��ȡ��ǰ�û���ص�OA�������뵥��������״̬��
        /// �����׼�������̺����ˡ�
        /// </summary>
        /// <param name="model">��ҳ���������</param>
        /// <param name="conditional">��ѯ��������֧�����ָ�ʽ��������
        /// 1. ��ǰ׺��������ֱ����Ϊ���뵥(OaExpenseRequisition)��ɸѡ����
        /// 2. "OwWf.�ֶ���" ��ʽ������������ɸѡ�����Ĺ�����(OwWf)����
        /// ���м������ִ�Сд�����У�OwWf.State�����⴦����OwWfManager.GetWfNodeItemByOpertorId������state����ӳ���ϵ��
        /// 0(��ת��)��3, 1(�ɹ����)��4, 2(�ѱ���ֹ)��8
        /// ͨ������д��:�������������ַ������������д�����ö��ŷָ����ַ���������ʱ��֧�������Ҷ���ģ����ѯ����"2024-1-1,2024-1-2"��
        /// ��ǿ��ȡnull��Լ������д"null"��</param>
        /// <returns>�������뵥�Ͷ�Ӧ��������Ϣ�Ľ����</returns>
        /// <response code="200">δ����ϵͳ�����󡣵����ܳ���Ӧ�ô��󣬾���μ� HasError �� ErrorCode ��</response>  
        /// <response code="401">��Ч���ơ�</response>  
        [HttpGet]
        public ActionResult<GetAllOaExpenseRequisitionWithWfReturnDto> GetAllOaExpenseRequisitionWithWf([FromQuery] GetAllOaExpenseRequisitionWithWfParamsDto model,
            [FromQuery][ModelBinder(typeof(DotKeyDictionaryModelBinder))] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) 
                return Unauthorized();
            
            var result = new GetAllOaExpenseRequisitionWithWfReturnDto();

            try
            {
                // �������з������ͬǰ׺������
                Dictionary<string, string> wfConditions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                Dictionary<string, string> reqConditions = conditional != null
                    ? new Dictionary<string, string>(conditional, StringComparer.OrdinalIgnoreCase)
                    : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                byte wfState = 15; // Ĭ��ֵ����ζ�Ż�ȡָ����������ص����й������ڵ���

                if (reqConditions.Count > 0)
                {
                    List<string> keysToRemove = new List<string>();

                    foreach (var pair in reqConditions)
                    {
                        // ������������
                        if (pair.Key.StartsWith("OwWf.", StringComparison.OrdinalIgnoreCase))
                        {
                            string wfFieldName = pair.Key.Substring(5); // ȥ��"OwWf."ǰ׺

                            // ���� State ���������
                            if (string.Equals(wfFieldName, "State", StringComparison.OrdinalIgnoreCase))
                            {
                                if (byte.TryParse(pair.Value, out var state))
                                {
                                    switch (state)
                                    {
                                        case 0: // ��ת�� - �ȼ��ھɵ�"3"��1|2��
                                            wfState = 3; // ʹ��OwWfManager�е�ֵ3����ת�еĽڵ���
                                            break;
                                        case 1: // �ɹ���� - �ȼ��ھɵ�"4"
                                            wfState = 4; // ʹ��OwWfManager�е�ֵ4���ɹ�����������
                                            break;
                                        case 2: // �ѱ���ֹ - �ȼ��ھɵ�"8"
                                            wfState = 8; // ʹ��OwWfManager�е�ֵ8����ʧ�ܽ���������
                                            break;
                                        default:
                                            wfState = 15; // ʹ��Ĭ��ֵ15�����޶�״̬
                                            break;
                                    }
                                }
                            }
                            else // ��������������
                            {
                                wfConditions[wfFieldName] = pair.Value;
                            }
                            keysToRemove.Add(pair.Key);
                        }
                    }

                    // ��ԭʼ�������Ƴ�����ǰ׺������
                    foreach (var key in keysToRemove)
                    {
                        reqConditions.Remove(key);
                    }
                }

                // ��ѯ�����Ĺ�����
                var docIdsQuery = _WfManager.GetWfNodeItemByOpertorId(context.User.Id, wfState)
                    .Select(c => c.Parent.Parent);

                // �����������������������Ӧ������
                if (wfConditions.Count > 0)
                {
                    _Logger.LogDebug("Ӧ�ù�������������: {conditions}",
                        string.Join(", ", wfConditions.Select(kv => $"{kv.Key}={kv.Value}")));

                    // Ӧ�ù�����ɸѡ����
                    docIdsQuery = EfHelper.GenerateWhereAnd(docIdsQuery, wfConditions);
                }

                // ��ȡ�����������ĵ�ID
                var docIds = docIdsQuery.Select(wf => wf.DocId.Value).Distinct();

                // �������뵥��ѯ
                var dbSet = _DbContext.OaExpenseRequisitions.Where(r => docIds.Contains(r.Id));

                // Ȩ�޹��ˣ��ǳ����û�ֻ�ܿ����Լ���Ȩ�޵����뵥
                if (!context.User.IsSuperAdmin)
                {
                    dbSet = dbSet.Where(r => r.OrgId == context.User.OrgId &&
                                           (r.ApplicantId == context.User.Id || r.CreateBy == context.User.Id));
                }

                // Ӧ�����뵥����
                if (reqConditions.Count > 0)
                {
                    dbSet = EfHelper.GenerateWhereAnd(dbSet, reqConditions);
                }

                // Ӧ�÷�ҳ������
                var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
                var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);

                // ��ȡ���ID����
                var resultIds = prb.Result.Select(c => c.Id).ToList();

                // ֻ��ѯ�����صĹ�����
                var wfsArray = _DbContext.OwWfs
                    .Where(c => resultIds.Contains(c.DocId.Value))
                    .ToArray();

                // ��װ���
                foreach (var requisition in prb.Result)
                {
                    var wf = wfsArray.FirstOrDefault(d => d.DocId == requisition.Id);
                    result.Result.Add(new GetAllOaExpenseRequisitionWithWfItemDto()
                    {
                        Requisition = requisition,
                        Wf = _Mapper.Map<OwWfDto>(wf),
                    });
                }

                result.Total = prb.Total;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "��ȡOA�������뵥���������б�ʱ��������");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"��ȡOA�������뵥���������б�ʱ��������: {ex.Message}";
            }

            return result;
        }

        #endregion
    }
}