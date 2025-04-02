using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PowerLms.Data;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Dto;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// ϵͳ����Ϣ���ܿ�������
    /// </summary>
    public class OwMessageController : PlControllerBase
    {
        /// <summary>
        /// ���캯����
        /// </summary>
        public OwMessageController(AccountManager accountManager, IServiceProvider serviceProvider, EntityManager entityManager,
            PowerLmsUserDbContext dbContext, ILogger<OwMessageController> logger, IMapper mapper, AuthorizationManager authorizationManager, OwSqlAppLogger sqlAppLogger)
        {
            _AccountManager = accountManager;
            _ServiceProvider = serviceProvider;
            _EntityManager = entityManager;
            _DbContext = dbContext;
            _Logger = logger;
            _Mapper = mapper;
            _AuthorizationManager = authorizationManager;
            _SqlAppLogger = sqlAppLogger;
        }

        private readonly AccountManager _AccountManager;
        private readonly IServiceProvider _ServiceProvider;
        private readonly EntityManager _EntityManager;
        private readonly PowerLmsUserDbContext _DbContext;
        private readonly ILogger<OwMessageController> _Logger;
        private readonly IMapper _Mapper;
        private readonly AuthorizationManager _AuthorizationManager;
        private readonly OwSqlAppLogger _SqlAppLogger;

        #region ��Ϣ��ѯ

        /// <summary>
        /// ��ȡ��Ϣ�б�֧�ַ�ҳ��ɸѡ��
        /// </summary>
        /// <param name="model">��ҳ����</param>
        /// <param name="conditional">��ѯ������֧��ͨ�ò�ѯ�ӿڡ�</param>
        /// <returns>��Ϣ�б�������</returns>
        /// <response code="200">δ����ϵͳ�����󡣵����ܳ���Ӧ�ô��󣬾���μ� HasError �� ErrorCode ��</response>  
        /// <response code="401">��Ч���ơ�</response>  
        [HttpGet]
        public ActionResult<GetAllOwMessageReturnDto> GetAllOwMessage([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            // ��֤����
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();

            var result = new GetAllOwMessageReturnDto();

            try
            {
                // ��ȡ��ǰ�û�����Ϣ
                var dbSet = _DbContext.Set<OwMessage>();
                var coll = dbSet.Where(m => m.UserId == context.User.Id).AsNoTracking();

                // Ӧ�ò�ѯ����
                coll = EfHelper.GenerateWhereAnd(coll, conditional);

                // Ӧ������
                coll = coll.OrderBy(model.OrderFieldName, model.IsDesc);

                // ��ȡ��ҳ���
                var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
                _Mapper.Map(prb, result);
                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "��ȡ��Ϣ�б�ʱ�����쳣");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"��ȡ��Ϣ�б�ʱ�����쳣: {ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// ������Ϣ���û�������ͬһ�̻��ڵ������û�������Ϣ�������ǳ�������Ա��
        /// </summary>
        /// <param name="model">��Ϣ����</param>
        /// <returns>�������</returns>
        /// <response code="200">δ����ϵͳ�����󡣵����ܳ���Ӧ�ô��󣬾���μ� HasError �� ErrorCode ��</response>  
        /// <response code="400">�����������</response>  
        /// <response code="401">��Ч���ơ�</response>  
        /// <response code="403">������ͬ�̻����û�������Ϣ��</response>  
        [HttpPost]
        [Description("������Ϣ��ָ���û�")]
        public ActionResult<SendOwMessageReturnDto> SendOwMessage(SendOwMessageParamsDto model)
        {
            // ��֤����
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();

            var result = new SendOwMessageReturnDto();

            try
            {
                // ��֤����
                if (string.IsNullOrWhiteSpace(model.Title))
                {
                    result.HasError = true;
                    result.ErrorCode = 400;
                    result.DebugMessage = "��Ϣ���ⲻ��Ϊ��";
                    return BadRequest(result);
                }

                if (string.IsNullOrWhiteSpace(model.Content))
                {
                    result.HasError = true;
                    result.ErrorCode = 400;
                    result.DebugMessage = "��Ϣ���ݲ���Ϊ��";
                    return BadRequest(result);
                }

                if (model.ReceiverIds == null || model.ReceiverIds.Count == 0)
                {
                    result.HasError = true;
                    result.ErrorCode = 400;
                    result.DebugMessage = "�����߲���Ϊ��";
                    return BadRequest(result);
                }

                // �����ݿ��л�ȡ�����ߵ�������Ϣ�������̻�ID
                var currentUser = _DbContext.Accounts.Find(context.User.Id);
                if (currentUser == null)
                {
                    result.HasError = true;
                    result.ErrorCode = 500;
                    result.DebugMessage = "�޷���ȡ��ǰ�û���Ϣ";
                    return StatusCode((int)HttpStatusCode.InternalServerError, result);
                }

                // �����ݿ��л�ȡ��ǰ�û��������̻�ID
                var senderMerchantId = currentUser.MerchantId;

                // ����Ƿ�Ϊ��������Ա
                bool isSuperAdmin = _AccountManager.IsAdmin(currentUser);

                var merchantManager = _ServiceProvider.GetService<MerchantManager>();

                // ��֤���н����߶�����ͬһ�̻��������ǳ�������Ա
                if (!isSuperAdmin && senderMerchantId.HasValue)
                {
                    // ��ȡ���н������˺�
                    var receivers = _DbContext.Accounts
                        .Where(a => model.ReceiverIds.Contains(a.Id))
                        .ToList();

                    // ���ÿ��������
                    foreach (var receiver in receivers)
                    {
                        // ��������߲�����
                        if (receiver == null)
                        {
                            result.HasError = true;
                            result.ErrorCode = 400;
                            result.DebugMessage = "�����߲�����";
                            return BadRequest(result);
                        }
                        merchantManager.GetIdByUserId(receiver.Id, out var merchantId);
                        // ��������߲�����ͬһ�̻�
                        if (merchantId != senderMerchantId)
                        {
                            result.HasError = true;
                            result.ErrorCode = 403;
                            result.DebugMessage = "ֻ����ͬһ�̻��ڵ��û�������Ϣ";
                            return StatusCode((int)HttpStatusCode.Forbidden, result);
                        }
                    }
                }

                // ������Ϣʵ��
                var messages = new List<OwMessage>();
                var now = DateTime.UtcNow;

                foreach (var receiverId in model.ReceiverIds)
                {
                    var message = new OwMessage
                    {
                        UserId = receiverId,
                        Title = model.Title,
                        Content = model.Content,
                        CreateBy = context.User.Id,
                        CreateUtc = now,
                        IsSystemMessage = isSuperAdmin, // ֻ�г�������Ա���͵���Ϣ�ű��Ϊϵͳ��Ϣ
                    };
                    message.GenerateNewId();
                    messages.Add(message);
                }

                // ��ӵ����ݿ�
                _DbContext.Set<OwMessage>().AddRange(messages);
                _DbContext.SaveChanges();

                // ��¼��־
                _SqlAppLogger.LogGeneralInfo($"������Ϣ.{messages.Count}��");

                result.MessageIds = messages.Select(m => m.Id).ToList();
                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "������Ϣʱ�����쳣");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"������Ϣʱ�����쳣: {ex.Message}";
                return BadRequest(result);
            }
        }

        /// <summary>
        /// ���������ϢΪ�Ѷ���
        /// �� MarkAll=true ʱ��������û�����δ����ϢΪ�Ѷ���
        /// ������ָ�� MessageIds �е���ϢΪ�Ѷ���
        /// </summary>
        /// <param name="model">����</param>
        /// <returns>�������</returns>
        /// <response code="200">δ����ϵͳ�����󡣵����ܳ���Ӧ�ô��󣬾���μ� HasError �� ErrorCode ��</response>  
        /// <response code="401">��Ч���ơ�</response>  
        /// <response code="400">�� MarkAll=false �� MessageIds Ϊ��ʱ���ش˴���</response>  
        [HttpPut]
        public ActionResult<MarkMessagesAsReadReturnDto> MarkMessagesAsRead(MarkMessagesAsReadParamsDto model)
        {
            // ��֤����
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();

            var result = new MarkMessagesAsReadReturnDto();

            try
            {
                // ��֤����
                if (!model.MarkAll && (model.MessageIds == null || model.MessageIds.Count == 0))
                {
                    result.HasError = true;
                    result.ErrorCode = 400;
                    result.DebugMessage = "��ָ��Ҫ���Ϊ�Ѷ�����ϢID�б������� MarkAll=true �������δ����Ϣ";
                    return BadRequest(result);
                }

                // �����ϢΪ�Ѷ�
                var now = DateTime.UtcNow;
                IQueryable<OwMessage> query;

                if (model.MarkAll)
                {
                    // �������δ����Ϣ
                    query = _DbContext.Set<OwMessage>()
                        .Where(m => m.UserId == context.User.Id && m.ReadUtc == null);

                    // ��¼��־��������
                    _SqlAppLogger.LogGeneralInfo("���������Ϣ�Ѷ�");
                }
                else
                {
                    // ���ָ����Ϣ
                    query = _DbContext.Set<OwMessage>()
                        .Where(m => model.MessageIds.Contains(m.Id) && m.UserId == context.User.Id && m.ReadUtc == null);

                    // ��¼��־��������
                    _SqlAppLogger.LogGeneralInfo($"�����Ϣ�Ѷ�.{model.MessageIds.Count}��");
                }

                // ��ȡ��Ϣ�����Ϊ�Ѷ�
                var messages = query.ToList();
                foreach (var message in messages)
                {
                    message.ReadUtc = now;
                }

                _DbContext.SaveChanges();

                result.MarkedCount = messages.Count;
                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "�����Ϣ�Ѷ�ʱ�����쳣");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"�����Ϣ�Ѷ�ʱ�����쳣: {ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// ����ɾ����Ϣ���û�ֻ��ɾ���Լ�����Ϣ��
        /// </summary>
        /// <param name="model">����</param>
        /// <returns>�������</returns>
        /// <response code="200">δ����ϵͳ�����󡣵����ܳ���Ӧ�ô��󣬾���μ� HasError �� ErrorCode ��</response>  
        /// <response code="401">��Ч���ơ�</response>  
        [HttpDelete]
        public ActionResult<RemoveAllOwMessageReturnDto> RemoveAllOwMessage(RemoveAllOwMessageParamsDto model)
        {
            // ��֤����
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();

            var result = new RemoveAllOwMessageReturnDto();

            try
            {
                // ��֤����
                if (model.Ids == null || model.Ids.Count == 0)
                {
                    result.HasError = true;
                    result.ErrorCode = 400;
                    result.DebugMessage = "��ϢID�б���Ϊ��";
                    return BadRequest(result);
                }

                // �����û���Ȩɾ������Ϣ
                var messages = _DbContext.Set<OwMessage>()
                    .Where(m => model.Ids.Contains(m.Id) && m.UserId == context.User.Id)
                    .ToList();

                // ɾ����Ϣ
                _DbContext.Set<OwMessage>().RemoveRange(messages);
                _DbContext.SaveChanges();

                // ��¼��־
                _SqlAppLogger.LogGeneralInfo($"����ɾ����Ϣ.{messages.Count}��");

                result.RemovedCount = messages.Count;
                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "����ɾ����Ϣʱ�����쳣");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"����ɾ����Ϣʱ�����쳣: {ex.Message}";
                return result;
            }
        }

        #endregion
    }

    #region DTO��

    /// <summary>
    /// ��ȡ������Ϣ�б�ķ���ֵ��װ�ࡣ
    /// </summary>
    public class GetAllOwMessageReturnDto : PagingReturnDtoBase<OwMessage>
    {
    }

    /// <summary>
    /// ������Ϣ�Ĳ�����װ�ࡣ
    /// </summary>
    public class SendOwMessageParamsDto : TokenDtoBase
    {
        /// <summary>
        /// �������û�ID�б�
        /// </summary>
        [Required]
        public List<Guid> ReceiverIds { get; set; } = new List<Guid>();

        /// <summary>
        /// ��Ϣ���⡣
        /// </summary>
        [Required, MaxLength(64)]
        public string Title { get; set; }

        /// <summary>
        /// ��Ϣ���ݡ�HTML��ʽ��
        /// </summary>
        [Required]
        public string Content { get; set; }
    }

    /// <summary>
    /// ������Ϣ�ķ���ֵ��װ�ࡣ
    /// </summary>
    public class SendOwMessageReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// ���ͳɹ�����ϢID�б�
        /// </summary>
        public List<Guid> MessageIds { get; set; } = new List<Guid>();
    }

    /// <summary>
    /// �����ϢΪ�Ѷ��Ĳ�����װ�ࡣ
    /// </summary>
    public class MarkMessagesAsReadParamsDto : TokenDtoBase
    {
        /// <summary>
        /// Ҫ���Ϊ�Ѷ�����ϢID�б�
        /// �� MarkAll Ϊ true ʱ�����б��Ϊ�ա�
        /// </summary>
        public List<Guid> MessageIds { get; set; } = new List<Guid>();

        /// <summary>
        /// �Ƿ�������δ����ϢΪ�Ѷ���
        /// ����ֵΪ true ʱ�������� MessageIds �б���ǵ�ǰ�û�������δ����ϢΪ�Ѷ���
        /// </summary>
        public bool MarkAll { get; set; } = false;
    }

    /// <summary>
    /// �����ϢΪ�Ѷ��ķ���ֵ��װ�ࡣ
    /// </summary>
    public class MarkMessagesAsReadReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// �ɹ����Ϊ�Ѷ�����Ϣ������
        /// </summary>
        public int MarkedCount { get; set; }
    }

    /// <summary>
    /// ����ɾ����Ϣ�Ĳ�����װ�ࡣ
    /// </summary>
    public class RemoveAllOwMessageParamsDto : TokenDtoBase
    {
        /// <summary>
        /// Ҫɾ������ϢID�б�
        /// </summary>
        [Required]
        public List<Guid> Ids { get; set; } = new List<Guid>();
    }

    /// <summary>
    /// ����ɾ����Ϣ�ķ���ֵ��װ�ࡣ
    /// </summary>
    public class RemoveAllOwMessageReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// �ɹ�ɾ������Ϣ������
        /// </summary>
        public int RemovedCount { get; set; }
    }

    #endregion
}
