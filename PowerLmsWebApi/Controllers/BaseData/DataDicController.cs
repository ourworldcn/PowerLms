using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PowerLms.Data;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Dto;
using System.Net;
using OW.Data;
using AutoMapper;
using PowerLmsServer;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// �ճ����������ֵ��������
    /// </summary>
    public partial class DataDicController : PlControllerBase
    {
        /// <summary>
        /// ���캯����
        /// </summary>
        public DataDicController(PowerLmsUserDbContext context, AccountManager accountManager, 
            IServiceProvider serviceProvider, EntityManager entityManager, IMapper mapper, 
            OrgManager<PowerLmsUserDbContext> orgManager, AuthorizationManager authorizationManager,
            ILogger<DataDicController> logger)
        {
            _DbContext = context;
            _AccountManager = accountManager;
            _ServiceProvider = serviceProvider;
            _EntityManager = entityManager;
            _Mapper = mapper;
            _OrgManager = orgManager;
            _AuthorizationManager = authorizationManager;
            _Logger = logger;
        }

        readonly PowerLmsUserDbContext _DbContext;
        readonly AccountManager _AccountManager;
        readonly IServiceProvider _ServiceProvider;
        readonly EntityManager _EntityManager;
        readonly IMapper _Mapper;
        readonly OrgManager<PowerLmsUserDbContext> _OrgManager;
        readonly AuthorizationManager _AuthorizationManager;
        readonly ILogger<DataDicController> _Logger;

        #region �ճ������������

        /// <summary>
        /// ��ȡ�ճ��������͡����ܿ��Բ鿴ϵͳ���������û�ֻ�ܿ�����˾/��֯�µ�ʵ�塣
        /// </summary>
        /// <param name="model">��ҳ����</param>
        /// <param name="conditional">֧��ͨ�ò�ѯ����</param>
        /// <returns>�ճ����������б�</returns>
        /// <response code="200">δ����ϵͳ�����󡣵����ܳ���Ӧ�ô��󣬾���μ� HasError �� ErrorCode ��</response>  
        /// <response code="400">ָ�����Id��Ч��</response>  
        /// <response code="401">��Ч���ơ�</response>  
        [HttpGet]
        public ActionResult<GetAllDailyFeesTypeReturnDto> GetAllDailyFeesType([FromQuery] PagingParamsDtoBase model, [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllDailyFeesTypeReturnDto();
            var dbSet = _DbContext.DD_DailyFeesTypes;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            
            // ʹ��ͳһ����֯Ȩ�޿��Ʒ���
            var allowedOrgIds = GetOrgIds(context.User, _OrgManager);
            coll = coll.Where(c => allowedOrgIds.Contains(c.OrgId));
            
            coll = EfHelper.GenerateWhereAnd(coll, conditional);
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// �����ճ����������¼��
        /// </summary>
        /// <param name="model">���Ӳ���</param>
        /// <returns>�������</returns>
        /// <response code="200">δ����ϵͳ�����󡣵����ܳ���Ӧ�ô��󣬾���μ� HasError �� ErrorCode ��</response>  
        /// <response code="400">��������</response>  
        /// <response code="401">��Ч���ơ�</response>  
        /// <response code="403">Ȩ�޲��㡣</response>  
        [HttpPost]
        public ActionResult<AddDailyFeesTypeReturnDto> AddDailyFeesType(AddDailyFeesTypeParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            if (!_AuthorizationManager.Demand(out err, "B.8")) return StatusCode((int)HttpStatusCode.Forbidden, err);

            var result = new AddDailyFeesTypeReturnDto();

            // ȷ��ʹ�õ�ǰ�û�����֯����ID
            model.Item.OrgId = context.User.OrgId;

            // ��������¼ID
            model.Item.GenerateNewId();
            var id = model.Item.Id;

            // �������¼
            _DbContext.DD_DailyFeesTypes.Add(model.Item);

            // �����Ҫͬ�����ӻ���
            if (model.CopyToChildren)
            {
                // ��ȡ�û���Ͻ��Χ�ڵĹ�˾����֯����ID
                var merchantId = _OrgManager.GetMerchantIdByUserId(context.User.Id);
                if (merchantId.HasValue)
                {
                    var allOrgs = _OrgManager.GetOrLoadOrgCacheItem(merchantId.Value).Orgs.Values.ToArray();
                    var companyIds = allOrgs.Where(o => o.Otc == 2).Select(o => o.Id);

                    foreach (var orgId in companyIds)
                    {
                        // ����Ƿ��Ѵ�����ͬCode�ļ�¼
                        if (_DbContext.DD_DailyFeesTypes.Any(f => f.OrgId == orgId && f.Code == model.Item.Code))
                            continue;

                        // ʹ��Clone�������������
                        var newItem = (DailyFeesType)model.Item.Clone();
                        newItem.OrgId = orgId;
                        newItem.GenerateNewId(); // ȷ���¼�¼��ΨһID

                        _DbContext.DD_DailyFeesTypes.Add(newItem);
                    }
                }
            }

            _DbContext.SaveChanges();
            result.Id = id;
            return result;
        }

        /// <summary>
        /// �޸��ճ����������¼��
        /// </summary>
        /// <param name="model">�޸Ĳ���</param>
        /// <returns>�������</returns>
        /// <response code="200">δ����ϵͳ�����󡣵����ܳ���Ӧ�ô��󣬾���μ� HasError �� ErrorCode ��</response>  
        /// <response code="400">ָ��ʵ���Id�����ڡ�ͨ������Bug.�ڼ�������¿����ǲ������⡣</response>  
        /// <response code="401">��Ч���ơ�</response>  
        /// <response code="403">Ȩ�޲��㡣</response>  
        [HttpPut]
        public ActionResult<ModifyDailyFeesTypeReturnDto> ModifyDailyFeesType(ModifyDailyFeesTypeParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            if (!_AuthorizationManager.Demand(out err, "B.8")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new ModifyDailyFeesTypeReturnDto();
            if (!_EntityManager.ModifyWithMarkDelete(model.Items))
            {
                var errResult = new StatusCodeResult(OwHelper.GetLastError()) { };
                return errResult;
            }
            foreach (var item in model.Items)
            {
                _DbContext.Entry(item).Property(c => c.OrgId).IsModified = false;
            }
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// ɾ���ճ���������ļ�¼��
        /// </summary>
        /// <param name="model">ɾ������</param>
        /// <returns>�������</returns>
        /// <response code="200">δ����ϵͳ�����󡣵����ܳ���Ӧ�ô��󣬾���μ� HasError �� ErrorCode ��</response>  
        /// <response code="400">ָ��ʵ���Id�����ڡ�ͨ������Bug.�ڼ�������¿����ǲ������⡣</response>  
        /// <response code="401">��Ч���ơ�</response>  
        /// <response code="403">Ȩ�޲��㡣</response>  
        [HttpDelete]
        public ActionResult<RemoveDailyFeesTypeReturnDto> RemoveDailyFeesType(RemoveDailyFeesTypeParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            if (!_AuthorizationManager.Demand(out err, "B.8")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new RemoveDailyFeesTypeReturnDto();
            var id = model.Id;
            var dbSet = _DbContext.DD_DailyFeesTypes;
            var item = dbSet.Find(id);
            if (item is null) return BadRequest();
            _EntityManager.Remove(item);
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// �ָ�ָ���ı�ɾ���ճ����������¼��
        /// </summary>
        /// <param name="model">�ָ�����</param>
        /// <returns>�������</returns>
        /// <response code="200">δ����ϵͳ�����󡣵����ܳ���Ӧ�ô��󣬾���μ� HasError �� ErrorCode ��</response>  
        /// <response code="400">ָ��ʵ���Id�����ڡ�ͨ������Bug.�ڼ�������¿����ǲ������⡣</response>  
        /// <response code="401">��Ч���ơ�</response>  
        /// <response code="403">Ȩ�޲��㡣</response>  
        [HttpPost]
        public ActionResult<RestoreDailyFeesTypeReturnDto> RestoreDailyFeesType(RestoreDailyFeesTypeParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            if (!_AuthorizationManager.Demand(out err, "B.8")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new RestoreDailyFeesTypeReturnDto();
            if (!_EntityManager.Restore<DailyFeesType>(model.Id))
            {
                var errResult = new StatusCodeResult(OwHelper.GetLastError()) { };
                return errResult;
            }
            _DbContext.SaveChanges();
            return result;
        }

        #endregion �ճ������������
    }
}