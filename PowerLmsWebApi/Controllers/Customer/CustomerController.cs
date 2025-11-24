using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.ObjectPool;
using NPOI.OpenXmlFormats.Dml.Diagram;
using NPOI.SS.Formula.Functions;
using PowerLms.Data;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Dto;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Net;
using System.Text;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// 客户资料及相关类操作的控制器。
    /// </summary>
    public class CustomerController : PlControllerBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public CustomerController(IServiceProvider serviceProvider, AccountManager accountManager, PowerLmsUserDbContext dbContext, EntityManager entityManager,
            IMapper mapper, OrgManager<PowerLmsUserDbContext> orgManager, AuthorizationManager authorizationManager)
        {
            _ServiceProvider = serviceProvider;
            _AccountManager = accountManager;
            _DbContext = dbContext;
            _EntityManager = entityManager;
            _Mapper = mapper;
            _OrgManager = orgManager;
            _AuthorizationManager = authorizationManager;
        }

        readonly IServiceProvider _ServiceProvider;
        readonly AccountManager _AccountManager;

        readonly PowerLmsUserDbContext _DbContext;
        readonly OrgManager<PowerLmsUserDbContext> _OrgManager;

        readonly EntityManager _EntityManager;
        readonly IMapper _Mapper;
        readonly AuthorizationManager _AuthorizationManager;

        #region 客户资料本体的

        /// <summary>
        /// 获取全部客户。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">查询的条件。已支持通用查询——除个别涉及敏感信息字段外，所有实体字段都可作为条件。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpGet]
        public ActionResult<GetAllCustomerReturnDto> GetAllCustomer([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            //if (!_AuthorizationManager.Demand(out string err, "C.1.2")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new GetAllCustomerReturnDto();
            Guid[] allOrg = Array.Empty<Guid>();
            var merchantId = _OrgManager.GetMerchantIdByUserId(context.User.Id);
            if (merchantId.HasValue)
            {
                allOrg = _OrgManager.GetOrLoadOrgCacheItem(merchantId.Value).Orgs.Keys.ToArray();
            }
            //var dbSet = _DbContext.PlCustomers.Where(c => c.OrgId.HasValue && allOrg.Contains(c.OrgId.Value));
            var dbSet = _DbContext.PlCustomers.Where(c => c.OrgId == context.User.OrgId);
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            coll = EfHelper.GenerateWhereAnd(coll, conditional);
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 增加新客户。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPost]
        public ActionResult<AddCustomerReturnDto> AddCustomer(AddCustomerParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            if (!_AuthorizationManager.Demand(out string err, "C.1.1")) return StatusCode((int)HttpStatusCode.Forbidden, err);

            var result = new AddCustomerReturnDto();
            model.Customer.GenerateNewId();

            var entity = _DbContext.PlCustomers.Add(model.Customer);
            entity.Entity.OrgId = context.User.OrgId;   //根据登录用户首选组织机构确定客户资料绑定的机构id。20240109。
            entity.Entity.CreateBy = context.User.Id;

            _DbContext.SaveChanges();
            result.Id = model.Customer.Id;
            return result;
        }

        /// <summary>
        /// 修改客户信息。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误,具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的客户不存在。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPut]
        public ActionResult<ModifyCustomerReturnDto> ModifyCustomer(ModifyCustomerParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            if (!_AuthorizationManager.Demand(out string err, "C.1.3")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new ModifyCustomerReturnDto();
            var modifiedEntities = new List<PlCustomer>();
            if (!_EntityManager.Modify(model.Items, modifiedEntities)) return NotFound();
            foreach (var item in modifiedEntities)
            {
                var entry = _DbContext.Entry(item);
                entry.Property(c => c.OrgId).IsModified = false;
                entry.Property(c => c.IsValid).IsModified = false;
            }
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 删除指定Id的客户。慎用！
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的客户不存在。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpDelete]
        public ActionResult<RemoveCustomerReturnDto> RemoveCustomer(RemoveCustomerParamsDto model)
        {
            // 客户表子表的名字。
            string[] CustomerChildTableNames = new string[] { $"{nameof(PowerLmsUserDbContext.PlCustomerContacts)}",
            $"{nameof( PowerLmsUserDbContext.PlCustomerBusinessHeaders)}", $"{nameof(PowerLmsUserDbContext.PlCustomerTaxInfos)}",
            $"{nameof(PowerLmsUserDbContext.PlCustomerTidans)}", $"{nameof(PowerLmsUserDbContext.CustomerBlacklists)}",
            $"{nameof(PowerLmsUserDbContext.PlCustomerLoadingAddrs)}" };

            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            if (!_AuthorizationManager.Demand(out string err, "C.1.4")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new RemoveCustomerReturnDto();
            var id = model.Id;
            var dbSet = _DbContext.PlCustomers.Where(c => c.OrgId == context.User.OrgId);
            var item = dbSet.FirstOrDefault(c => c.Id == id);
            if (item is null) return BadRequest();

            //删除子表
            var sb = AutoClearPool<StringBuilder>.Shared.Get(); Trace.Assert(sb is not null);
            using var dwSb = DisposeHelper.Create(c => AutoClearPool<StringBuilder>.Shared.Return(c), sb);
            var idString = id.ToString();
            foreach (var name in CustomerChildTableNames)
            {
                sb.AppendLine($"delete from [{name}] where [CustomerId]='{idString}';");
            }
            _DbContext.Database.ExecuteSqlRaw(sb.ToString());
            //删除主表
            _EntityManager.Remove(item);
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 按指定条件获取客户本体。支持多个bool类型的或关系查询。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">支持多个bool类型的或查询。使用字段名作为key,true或false为值。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpGet]
        public ActionResult<GetAllCustomer2ReturnDto> GetAllCustomer2([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            if (!_AuthorizationManager.Demand(out string err, "C.1.2")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new GetAllCustomer2ReturnDto();
            Guid[] allOrg = Array.Empty<Guid>();
            var merchantId = _OrgManager.GetMerchantIdByUserId(context.User.Id);
            if (merchantId.HasValue)
            {
                allOrg = _OrgManager.GetOrLoadOrgCacheItem(merchantId.Value).Orgs.Keys.ToArray();
            }
            var dbSet = _DbContext.PlCustomers.Where(c => c.OrgId == context.User.OrgId);
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            coll = EfHelper.GenerateWhereOr(coll, conditional);
            var collR = coll.Where(c => c.OrgId == context.User.OrgId).AsQueryable();
            var prb = _EntityManager.GetAll(collR, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        #endregion 客户资料本体的

        #region 客户上的联系人操作

        /// <summary>
        /// 获取全部客户联系人。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">查询的条件。已支持通用查询——除个别涉及敏感信息字段外，所有实体字段都可作为条件。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllCustomerContactReturnDto> GetAllCustomerContact([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllCustomerContactReturnDto();
            var dbSet = _DbContext.PlCustomerContacts;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            coll = EfHelper.GenerateWhereAnd(coll, conditional);

            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 增加新客户联系人。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpPost]
        public ActionResult<AddCustomerContactReturnDto> AddCustomerContact(AddCustomerContactParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new AddCustomerContactReturnDto();
            model.CustomerContact.GenerateNewId();
            _DbContext.PlCustomerContacts.Add(model.CustomerContact);
            _DbContext.SaveChanges();
            result.Id = model.CustomerContact.Id;
            return result;
        }

        /// <summary>
        /// 修改客户联系人信息。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的客户联系人不存在。</response>  
        [HttpPut]
        public ActionResult<ModifyCustomerContactReturnDto> ModifyCustomerContact(ModifyCustomerContactParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyCustomerContactReturnDto();
            var modifiedEntities = new List<PlCustomerContact>();
            if (!_EntityManager.Modify(new[] { model.CustomerContact }, modifiedEntities)) return NotFound();
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 删除指定Id的客户联系人。慎用！
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的客户联系人不存在。</response>  
        [HttpDelete]
        public ActionResult<RemoveCustomerContactReturnDto> RemoveCustomerContact(RemoveCustomerContactParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemoveCustomerContactReturnDto();
            var id = model.Id;
            var dbSet = _DbContext.PlCustomerContacts;
            var item = dbSet.Find(id);
            if (item is null) return BadRequest();
            _EntityManager.Remove(item);
            _DbContext.SaveChanges();
            return result;
        }
        #endregion 客户上的联系人操作

        #region 业务负责人的所属关系的CRUD

        /// <summary>
        /// 获取业务负责人的所属关系。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">查询的条件。已支持通用查询——除个别涉及敏感信息字段外，所有实体字段都可作为条件。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定类别Id无效。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllPlBusinessHeaderReturnDto> GetAllPlBusinessHeader([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllPlBusinessHeaderReturnDto();
            if (model.OrderFieldName == "Id") model.OrderFieldName = "CustomerId";

            var dbSet = _DbContext.PlCustomerBusinessHeaders;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            coll = EfHelper.GenerateWhereAnd(coll, conditional);
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 增加业务负责人的所属关系。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">在同一类别同一组织机构下指定了重复的Code。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPost]
        public ActionResult<AddPlBusinessHeaderReturnDto> AddPlBusinessHeader(AddPlBusinessHeaderParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            if (!_AuthorizationManager.Demand(out string err, "C.1.5")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new AddPlBusinessHeaderReturnDto();
            _DbContext.PlCustomerBusinessHeaders.Add(model.Item);
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 删除业务负责人的所属关系。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定实体的Id不存在。通常这是Bug.在极端情况下可能是并发问题。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpDelete]
        public ActionResult<RemovePlBusinessHeaderReturnDto> RemovePlBusinessHeader(RemovePlBusinessHeaderParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            if (!_AuthorizationManager.Demand(out string err, "C.1.5")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new RemovePlBusinessHeaderReturnDto();
            DbSet<PlBusinessHeader> dbSet = _DbContext.PlCustomerBusinessHeaders;
            var item = dbSet.Find(model.CustomerId, model.UserId, model.OrderTypeId);
            if (item is null) return BadRequest();
            _EntityManager.Remove(item);
            _DbContext.SaveChanges();
            return result;
        }

        #endregion 业务负责人的所属关系的CRUD

        #region 客户上的开票信息操作

        /// <summary>
        /// 获取全部客户开票信息。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">查询的条件。已支持通用查询——除个别涉及敏感信息字段外，所有实体字段都可作为条件。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllPlTaxInfoReturnDto> GetAllPlTaxInfo([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllPlTaxInfoReturnDto();

            var dbSet = _DbContext.PlCustomerTaxInfos;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            coll = EfHelper.GenerateWhereAnd(coll, conditional);
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 增加新客户开票信息。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpPost]
        public ActionResult<AddPlTaxInfoReturnDto> AddPlTaxInfo(AddPlTaxInfoParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new AddPlTaxInfoReturnDto();
            model.PlTaxInfo.GenerateNewId();
            _DbContext.PlCustomerTaxInfos.Add(model.PlTaxInfo);
            _DbContext.SaveChanges();
            result.Id = model.PlTaxInfo.Id;
            return result;
        }

        /// <summary>
        /// 修改客户开票信息信息。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的客户开票信息不存在。</response>  
        [HttpPut]
        public ActionResult<ModifyPlTaxInfoReturnDto> ModifyPlTaxInfo(ModifyPlTaxInfoParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyPlTaxInfoReturnDto();
            var modifiedEntities = new List<PlTaxInfo>();
            if (!_EntityManager.Modify(new[] { model.PlTaxInfo }, modifiedEntities)) return NotFound();
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 删除指定Id的客户开票信息。慎用！
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的客户开票信息不存在。</response>  
        [HttpDelete]
        public ActionResult<RemovePlTaxInfoReturnDto> RemovePlTaxInfo(RemovePlTaxInfoParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemovePlTaxInfoReturnDto();
            var id = model.Id;
            var dbSet = _DbContext.PlCustomerTaxInfos;
            var item = dbSet.Find(id);
            if (item is null) return BadRequest();
            _EntityManager.Remove(item);
            _DbContext.SaveChanges();
            return result;
        }
        #endregion 客户上的开票信息操作

        #region 客户上的提单操作

        /// <summary>
        /// 获取全部客户提单。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">查询的条件。已支持通用查询——除个别涉及敏感信息字段外，所有实体字段都可作为条件。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpGet]
        public ActionResult<GetAllPlTidanReturnDto> GetAllPlTidan([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            //if (!_AuthorizationManager.Demand(out var err, "D0.1.5.2")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new GetAllPlTidanReturnDto();

            var dbSet = _DbContext.PlCustomerTidans;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            coll = EfHelper.GenerateWhereAnd(coll, conditional);
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 增加新客户提单。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPost]
        public ActionResult<AddPlTidanReturnDto> AddPlTidan(AddPlTidanParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            //if (!_AuthorizationManager.Demand(out var err, "D0.1.5.1")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new AddPlTidanReturnDto();
            model.PlTidan.GenerateNewId();
            _DbContext.PlCustomerTidans.Add(model.PlTidan);
            _DbContext.SaveChanges();
            result.Id = model.PlTidan.Id;
            return result;
        }

        /// <summary>
        /// 修改客户提单信息。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的客户提单不存在。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPut]
        public ActionResult<ModifyPlTidanReturnDto> ModifyPlTidan(ModifyPlTidanParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            //if (!_AuthorizationManager.Demand(out var err, "D0.1.5.3")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new ModifyPlTidanReturnDto();
            var modifiedEntities = new List<PlTidan>();
            if (!_EntityManager.Modify(new[] { model.PlTidan }, modifiedEntities)) return NotFound();
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 删除指定Id的客户提单。慎用！
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的客户提单不存在。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpDelete]
        public ActionResult<RemovePlTidanReturnDto> RemovePlTidan(RemovePlTidanParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            //if (!_AuthorizationManager.Demand(out var err, "D0.1.5.4")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new RemovePlTidanReturnDto();
            var id = model.Id;
            var dbSet = _DbContext.PlCustomerTidans; // ✅ 修复：使用正确的表
            var item = dbSet.Find(id);
            if (item is null) return NotFound(); // ✅ 修复：返回NotFound而不是BadRequest
            _EntityManager.Remove(item);
            _DbContext.SaveChanges();
            return result;
        }
        #endregion 客户上的提单操作

        #region 客户上的黑名单操作

        /// <summary>
        /// 获取全部客户黑名单。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">查询的条件。已支持通用查询——除个别涉及敏感信息字段外，所有实体字段都可作为条件。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllCustomerBlacklistReturnDto> GetAllCustomerBlacklist([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllCustomerBlacklistReturnDto();
            var dbSet = _DbContext.CustomerBlacklists;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();

            coll = EfHelper.GenerateWhereAnd(coll, conditional);
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 增加新客户黑名单。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">Kind必须是1或2。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpPost]
        public ActionResult<AddCustomerBlacklistReturnDto> AddCustomerBlacklist(AddCustomerBlacklistParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new AddCustomerBlacklistReturnDto();
            model.CustomerBlacklist.GenerateNewId();
            if (_DbContext.PlCustomers.Find(model.CustomerBlacklist.CustomerId) is not PlCustomer customer)
                return BadRequest($"指定的客户Id不存在。{model.CustomerBlacklist.CustomerId}");

            var entity = _DbContext.CustomerBlacklists.Add(model.CustomerBlacklist);
            if (entity.Entity.Kind != 1 && entity.Entity.Kind != 2) return BadRequest("Kind 应该是 1或2");
            if (entity.Entity.Kind == 1)
                customer.BillingInfo_IsCEBlack = true;
            else if (entity.Entity.Kind == 2)
                customer.BillingInfo_IsBlack = true;
            _DbContext.SaveChanges();
            result.Id = model.CustomerBlacklist.Id;
            return result;
        }

        /// <summary>
        /// 删除指定Id的客户黑名单。
        /// 特别地，不是删除指定Id的实体，而是建立一个新的实体，用于"冲红"指定实体的操作。指定Id的实体必须是添加黑名单的操作。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">不能手工删除系统添加的黑名单。- 或 - 不能对非添加项\"冲红\" - 或 - 已经不在黑名单中.</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定的客户Id不存在。</response>  
        /// <response code="500">其他错误，并发导致数据变化不能完成操作。</response>
        [HttpDelete]
        public ActionResult<RemoveCustomerBlacklistReturnDto> RemoveCustomerBlacklist(RemoveCustomerBlacklistParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemoveCustomerBlacklistReturnDto();
            var id = model.CustomerId;
            var customer = _DbContext.PlCustomers.Find(model.CustomerId);
            if (customer is null) return NotFound(model.CustomerId);
            var dbSet = _DbContext.CustomerBlacklists;
            var item = dbSet.Where(c => c.CustomerId == model.CustomerId && (c.Kind == model.Kind - 2 || c.Kind == model.Kind)).OrderByDescending(c => c.Datetime).FirstOrDefault();
            if (item is null) return BadRequest("指定客户不在黑名单中。");
            if (item.Kind == model.Kind) return BadRequest("指定客户不在黑名单中。");

            if (item.Kind != 1 && item.Kind != 2) return BadRequest("不能对非添加项\"冲红\"");
            var newItem = new CustomerBlacklist
            {
                CustomerId = item.CustomerId,
                IsSystem = false,
                Kind = model.Kind,
                OpertorId = context.User.Id,
                Remark = model.Remark,
            };
            result.Result = dbSet.Add(newItem).Entity;
            if (item.Kind == 3)
                customer.BillingInfo_IsCEBlack = false;
            else if (item.Kind == 4)
                customer.BillingInfo_IsBlack = false;
            _DbContext.SaveChanges();
            return result;
        }

        #endregion 客户上的黑名单操作

        #region 客户上的装货地址操作

        /// <summary>
        /// 获取全部客户装货地址。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">查询的条件。已支持通用查询——除个别涉及敏感信息字段外，所有实体字段都可作为条件。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllPlLoadingAddrReturnDto> GetAllPlLoadingAddr([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllPlLoadingAddrReturnDto();

            var dbSet = _DbContext.PlCustomerLoadingAddrs;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            coll = EfHelper.GenerateWhereAnd(coll, conditional);
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 增加新客户装货地址。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpPost]
        public ActionResult<AddPlLoadingAddrReturnDto> AddPlLoadingAddr(AddPlLoadingAddrParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new AddPlLoadingAddrReturnDto();
            model.PlLoadingAddr.GenerateNewId();
            _DbContext.PlCustomerLoadingAddrs.Add(model.PlLoadingAddr);
            _DbContext.SaveChanges();
            result.Id = model.PlLoadingAddr.Id;
            return result;
        }

        /// <summary>
        /// 修改客户装货地址信息。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的客户装货地址不存在。</response>  
        [HttpPut]
        public ActionResult<ModifyPlLoadingAddrReturnDto> ModifyPlLoadingAddr(ModifyPlLoadingAddrParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyPlLoadingAddrReturnDto();
            var modifiedEntities = new List<PlLoadingAddr>();
            if (!_EntityManager.Modify(new[] { model.PlLoadingAddr }, modifiedEntities)) return NotFound();
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 删除指定Id的客户装货地址。慎用！
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的客户装货地址不存在。</response>  
        [HttpDelete]
        public ActionResult<RemovePlLoadingAddrReturnDto> RemovePlLoadingAddr(RemovePlLoadingAddrParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemovePlLoadingAddrReturnDto();
            var id = model.Id;
            var dbSet = _DbContext.PlCustomerLoadingAddrs; // ✅ 修复：使用正确的表
            var item = dbSet.Find(id);
            if (item is null) return NotFound(); // ✅ 修复：返回NotFound而不是BadRequest
            _EntityManager.Remove(item);
            _DbContext.SaveChanges();
            return result;
        }
        #endregion 客户上的装货地址操作

        #region 客户有效性管理

        /// <summary>
        /// 设置客户有效性状态。专门用于启用或停用客户。
        /// </summary>
        /// <param name="model">客户有效性设置参数</param>
        /// <returns>设置结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        /// <response code="404">指定Id的客户不存在。</response>  
        [HttpPost]
        public ActionResult<SetCustomerValidityReturnDto> SetCustomerValidity(SetCustomerValidityParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            if (!_AuthorizationManager.Demand(out string err, "C.1.8")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new SetCustomerValidityReturnDto();
            var customer = _DbContext.PlCustomers.Where(c => c.OrgId == context.User.OrgId && c.Id == model.CustomerId).FirstOrDefault();
            if (customer == null) return NotFound($"未找到指定的客户，Id={model.CustomerId}");
            customer.IsValid = model.IsValid;
            _DbContext.SaveChanges();
            var logEntry = new OwSystemLog
            {
                OrgId = context.User.OrgId,
                ActionId = "Customer.SetValidity",
                ExtraGuid = model.CustomerId,
                ExtraString = $"{(model.IsValid ? "启用" : "停用")}客户",
                ExtraDecimal = context.User.Id.GetHashCode()
            };
            _DbContext.OwSystemLogs.Add(logEntry);
            _DbContext.SaveChanges();
            return result;
        }

        #endregion 客户有效性管理

    }
}
