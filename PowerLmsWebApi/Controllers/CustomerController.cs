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
        public CustomerController(IServiceProvider serviceProvider, AccountManager accountManager, PowerLmsUserDbContext dbContext, EntityManager entityManager, IMapper mapper, OrganizationManager organizationManager)
        {
            _ServiceProvider = serviceProvider;
            _AccountManager = accountManager;
            _DbContext = dbContext;
            _EntityManager = entityManager;
            _Mapper = mapper;
            _OrganizationManager = organizationManager;
        }

        IServiceProvider _ServiceProvider;
        AccountManager _AccountManager;

        readonly PowerLmsUserDbContext _DbContext;
        OrganizationManager _OrganizationManager;

        EntityManager _EntityManager;
        IMapper _Mapper;

        #region 客户资料本体的

        /// <summary>
        /// 获取全部客户。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">查询的条件。支持 name，ShortName，displayname，Id，Keyword。不区分大小写。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllCustomerReturnDto> GetAllCustomer([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllCustomerReturnDto();
            Guid[] allOrg = Array.Empty<Guid>();
            if (_OrganizationManager.GetMerchantId(context.User.Id, out var merId))
            {
                allOrg = _OrganizationManager.GetAllOrgInRoot(merId.Value).Select(c => c.Id).ToArray();
            }
            //var dbSet = _DbContext.PlCustomers.Where(c => c.OrgId.HasValue && allOrg.Contains(c.OrgId.Value));
            var dbSet = _DbContext.PlCustomers.Where(c => c.OrgId == context.User.OrgId);
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            foreach (var item in conditional)
                if (string.Equals(item.Key, "name", StringComparison.OrdinalIgnoreCase))
                {
                    coll = coll.Where(c => c.Name.Name.Contains(item.Value));
                }
                else if (string.Equals(item.Key, "Id", StringComparison.OrdinalIgnoreCase))
                {
                    if (Guid.TryParse(item.Value, out var id))
                        coll = coll.Where(c => c.Id == id);
                }
                else if (string.Equals(item.Key, "ShortName", StringComparison.OrdinalIgnoreCase))
                {
                    coll = coll.Where(c => c.Name.ShortName.Contains(item.Value));
                }
                else if (string.Equals(item.Key, "displayname", StringComparison.OrdinalIgnoreCase))
                {
                    coll = coll.Where(c => c.Name.DisplayName.Contains(item.Value));
                }
                else if (string.Equals(item.Key, "Keyword", StringComparison.OrdinalIgnoreCase))
                {
                    coll = coll.Where(c => c.Keyword.Contains(item.Value));
                }
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
        [HttpPost]
        public ActionResult<AddCustomerReturnDto> AddCustomer(AddCustomerParamsDto model)
        {
            if (_AccountManager.GetOrLoadAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的客户不存在。</response>  
        [HttpPut]
        public ActionResult<ModifyCustomerReturnDto> ModifyCustomer(ModifyCustomerParamsDto model)
        {
            if (_AccountManager.GetOrLoadAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyCustomerReturnDto();
            if (!_EntityManager.Modify(model.Items)) return NotFound();
            foreach (var item in model.Items)
            {
                _DbContext.Entry(item).Property(c => c.OrgId).IsModified = false;
            }
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 客户表子表的名字。
        /// </summary>
        static string[] CustomerChildTableNames = new string[] { "PlCustomerContact", "PlBusinessHeader", "PlTaxInfo", "PlTidan", "CustomerBlacklist", "PlLoadingAddr" };

        /// <summary>
        /// 删除指定Id的客户。慎用！
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的客户不存在。</response>  
        [HttpDelete]
        public ActionResult<RemoveCustomerReturnDto> RemoveCustomer(RemoveCustomerParamsDto model)
        {
            if (_AccountManager.GetOrLoadAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
        [HttpGet]
        public ActionResult<GetAllCustomer2ReturnDto> GetAllCustomer2([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllCustomer2ReturnDto();
            Guid[] allOrg = Array.Empty<Guid>();
            if (_OrganizationManager.GetMerchantId(context.User.Id, out var merId))
            {
                allOrg = _OrganizationManager.GetAllOrgInRoot(merId.Value).Select(c => c.Id).ToArray();
            }
            //var dbSet = _DbContext.PlCustomers.Where(c => c.OrgId.HasValue && allOrg.Contains(c.OrgId.Value));
            var dbSet = _DbContext.PlCustomers.Where(c => c.OrgId == context.User.OrgId);
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();

            StringBuilder sb = new StringBuilder("select * from PlCustomers where   ");
            foreach (var item in conditional.Where(c => c.Key != "IsDesc"))
            {
                if (!bool.TryParse(item.Value, out var b)) continue;
                if (b)
                    sb.Append($"{item.Key}=1 or ");
                else
                    sb.Append($"{item.Key}=0 or ");
            }
            sb.Remove(sb.Length - 3, 3);    //获得条件
            var collBase = _DbContext.PlCustomers.FromSqlRaw(sb.ToString()).ToArray();
            //var collR = collBase.Where(c => c.OrgId.HasValue && allOrg.Contains(c.OrgId.Value)).AsQueryable();
            var collR = collBase.Where(c => c.OrgId == context.User.OrgId).AsQueryable();

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
        /// <param name="conditional">查询的条件。支持 displayname，Id,CustomerId。不区分大小写。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllCustomerContactReturnDto> GetAllCustomerContact([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllCustomerContactReturnDto();
            var dbSet = _DbContext.PlCustomerContacts;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            foreach (var item in conditional)
                if (string.Equals(item.Key, "Id", StringComparison.OrdinalIgnoreCase))
                {
                    if (Guid.TryParse(item.Value, out var id))
                        coll = coll.Where(c => c.Id == id);
                }
                else if (string.Equals(item.Key, "displayname", StringComparison.OrdinalIgnoreCase))
                {
                    coll = coll.Where(c => c.DisplayName.Contains(item.Value));
                }
                else if (string.Equals(item.Key, "CustomerId", StringComparison.OrdinalIgnoreCase))
                {
                    if (Guid.TryParse(item.Value, out var id))
                        coll = coll.Where(c => c.CustomerId == id);
                }
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
            if (_AccountManager.GetOrLoadAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
            if (_AccountManager.GetOrLoadAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyCustomerContactReturnDto();
            if (!_EntityManager.Modify(new[] { model.CustomerContact })) return NotFound();
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
            if (_AccountManager.GetOrLoadAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
        /// <param name="conditional">查询的条件。支持 CustomerId,AccountId,OrderTypeId</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定类别Id无效。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllPlBusinessHeaderReturnDto> GetAllPlBusinessHeader([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllPlBusinessHeaderReturnDto();
            if (model.OrderFieldName == "Id") model.OrderFieldName = "CustomerId";

            var dbSet = _DbContext.PlCustomerBusinessHeaders;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            foreach (var item in conditional)
                if (string.Equals(item.Key, "CustomerId", StringComparison.OrdinalIgnoreCase))
                {
                    if (Guid.TryParse(item.Value, out var customerId))
                        coll = coll.Where(c => c.CustomerId == customerId);
                }
                else if (string.Equals(item.Key, "accountId", StringComparison.OrdinalIgnoreCase))
                {
                    if (Guid.TryParse(item.Value, out var accountId))
                        coll = coll.Where(c => c.UserId == accountId);
                }
                else if (string.Equals(item.Key, "OrderTypeId", StringComparison.OrdinalIgnoreCase))
                {
                    if (Guid.TryParse(item.Value, out var orderTypeId))
                        coll = coll.Where(c => c.OrderTypeId == orderTypeId);
                }
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
        [HttpPost]
        public ActionResult<AddPlBusinessHeaderReturnDto> AddPlBusinessHeader(AddPlBusinessHeaderParamsDto model)
        {
            if (_AccountManager.GetOrLoadAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
        [HttpDelete]
        public ActionResult<RemovePlBusinessHeaderReturnDto> RemovePlBusinessHeader(RemovePlBusinessHeaderParamsDto model)
        {
            if (_AccountManager.GetOrLoadAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
        /// <param name="conditional">查询的条件。支持 CustomerId，Id,Number。不区分大小写。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllPlTaxInfoReturnDto> GetAllPlTaxInfo([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllPlTaxInfoReturnDto();

            var dbSet = _DbContext.PlCustomerTaxInfos;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            foreach (var item in conditional)
                if (string.Equals(item.Key, "Id", StringComparison.OrdinalIgnoreCase))
                {
                    if (Guid.TryParse(item.Value, out var id))
                        coll = coll.Where(c => c.Id == id);
                }
                else if (string.Equals(item.Key, "CustomerId", StringComparison.OrdinalIgnoreCase))
                {
                    if (Guid.TryParse(item.Value, out var id))
                        coll = coll.Where(c => c.CustomerId == id);
                }
                else if (string.Equals(item.Key, "Number", StringComparison.OrdinalIgnoreCase))
                {
                    coll = coll.Where(c => c.Number.Contains(item.Value));
                }
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
            if (_AccountManager.GetOrLoadAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
            if (_AccountManager.GetOrLoadAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyPlTaxInfoReturnDto();
            if (!_EntityManager.Modify(new[] { model.PlTaxInfo })) return NotFound();
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
            if (_AccountManager.GetOrLoadAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
        /// <param name="conditional">查询的条件。支持 CustomerId，Id。不区分大小写。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllPlTidanReturnDto> GetAllPlTidan([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllPlTidanReturnDto();

            var dbSet = _DbContext.PlCustomerTidans;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            foreach (var item in conditional)
                if (string.Equals(item.Key, "Id", StringComparison.OrdinalIgnoreCase))
                {
                    if (Guid.TryParse(item.Value, out var id))
                        coll = coll.Where(c => c.Id == id);
                }
                else if (string.Equals(item.Key, "CustomerId", StringComparison.OrdinalIgnoreCase))
                {
                    if (Guid.TryParse(item.Value, out var id))
                        coll = coll.Where(c => c.CustomerId == id);
                }
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
        [HttpPost]
        public ActionResult<AddPlTidanReturnDto> AddPlTidan(AddPlTidanParamsDto model)
        {
            if (_AccountManager.GetOrLoadAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
        [HttpPut]
        public ActionResult<ModifyPlTidanReturnDto> ModifyPlTidan(ModifyPlTidanParamsDto model)
        {
            if (_AccountManager.GetOrLoadAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyPlTidanReturnDto();
            if (!_EntityManager.Modify(new[] { model.PlTidan })) return NotFound();
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
        [HttpDelete]
        public ActionResult<RemovePlTidanReturnDto> RemovePlTidan(RemovePlTidanParamsDto model)
        {
            if (_AccountManager.GetOrLoadAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemovePlTidanReturnDto();
            var id = model.Id;
            var dbSet = _DbContext.PlCustomerTaxInfos;
            var item = dbSet.Find(id);
            if (item is null) return BadRequest();
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
        /// <param name="conditional">查询的条件。支持 CustomerId，Id,Kind,IsSystem("true" "false" 字符串)。不区分大小写。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllCustomerBlacklistReturnDto> GetAllCustomerBlacklist([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllCustomerBlacklistReturnDto();
            var dbSet = _DbContext.CustomerBlacklists;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            foreach (var item in conditional)
                if (string.Equals(item.Key, "Id", StringComparison.OrdinalIgnoreCase))
                {
                    if (Guid.TryParse(item.Value, out var id))
                        coll = coll.Where(c => c.Id == id);
                }
                else if (string.Equals(item.Key, "CustomerId", StringComparison.OrdinalIgnoreCase))
                {
                    if (Guid.TryParse(item.Value, out var id))
                        coll = coll.Where(c => c.CustomerId == id);
                }
                else if (string.Equals(item.Key, "Kind", StringComparison.OrdinalIgnoreCase))
                {
                    coll = coll.Where(c => c.Kind == int.Parse(item.Value));
                }
                else if (string.Equals(item.Key, "IsSystem", StringComparison.OrdinalIgnoreCase))
                {
                    coll = coll.Where(c => c.IsSystem == bool.Parse(item.Value));
                }
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
            if (_AccountManager.GetOrLoadAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new AddCustomerBlacklistReturnDto();
            model.CustomerBlacklist.GenerateNewId();
            if (_DbContext.PlCustomers.Find(model.CustomerBlacklist.CustomerId) is not PlCustomer customer)
                return BadRequest($"指定的客户Id不存在。{model.CustomerBlacklist.CustomerId}");

            var entity = _DbContext.CustomerBlacklists.Add(model.CustomerBlacklist);
            if (entity.Entity.Kind != 1 && entity.Entity.Kind != 2) return BadRequest("Kind 应该是 1或2");
            if (entity.Entity.Kind == 1)
                customer.BillingInfo.IsCEBlack = true;
            else if (entity.Entity.Kind == 2)
                customer.BillingInfo.IsBlack = true;
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
            if (_AccountManager.GetOrLoadAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
                customer.BillingInfo.IsCEBlack = false;
            else if (item.Kind == 4)
                customer.BillingInfo.IsBlack = false;
            _DbContext.SaveChanges();
            return result;
        }

        #endregion 客户上的黑名单操作

        #region 客户上的装货地址操作

        /// <summary>
        /// 获取全部客户装货地址。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">查询的条件。支持 CustomerId，Id，Tel。不区分大小写。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllPlLoadingAddrReturnDto> GetAllPlLoadingAddr([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllPlLoadingAddrReturnDto();

            var dbSet = _DbContext.PlCustomerLoadingAddrs;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            foreach (var item in conditional)
                if (string.Equals(item.Key, "Id", StringComparison.OrdinalIgnoreCase))
                {
                    if (Guid.TryParse(item.Value, out var id))
                        coll = coll.Where(c => c.Id == id);
                }
                else if (string.Equals(item.Key, "CustomerId", StringComparison.OrdinalIgnoreCase))
                {
                    if (Guid.TryParse(item.Value, out var id))
                        coll = coll.Where(c => c.CustomerId == id);
                }
                else if (string.Equals(item.Key, "Number", StringComparison.OrdinalIgnoreCase))
                {
                    coll = coll.Where(c => c.Tel.Contains(item.Value));
                }
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
            if (_AccountManager.GetOrLoadAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
            if (_AccountManager.GetOrLoadAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyPlLoadingAddrReturnDto();
            if (!_EntityManager.Modify(new[] { model.PlLoadingAddr })) return NotFound();
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
            if (_AccountManager.GetOrLoadAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemovePlLoadingAddrReturnDto();
            var id = model.Id;
            var dbSet = _DbContext.PlCustomerTaxInfos;
            var item = dbSet.Find(id);
            if (item is null) return BadRequest();
            _EntityManager.Remove(item);
            _DbContext.SaveChanges();
            return result;
        }
        #endregion 客户上的装货地址操作

    }

    #region 装货地址
    /// <summary>
    /// 标记删除装货地址功能的参数封装类。
    /// </summary>
    public class RemovePlLoadingAddrParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 标记删除装货地址功能的返回值封装类。
    /// </summary>
    public class RemovePlLoadingAddrReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 获取所有装货地址功能的返回值封装类。
    /// </summary>
    public class GetAllPlLoadingAddrReturnDto : PagingReturnDtoBase<PlLoadingAddr>
    {
    }

    /// <summary>
    /// 增加新装货地址功能参数封装类。
    /// </summary>
    public class AddPlLoadingAddrParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 新装货地址信息。其中Id可以是任何值，返回时会指定新值。
        /// </summary>
        public PlLoadingAddr PlLoadingAddr { get; set; }
    }

    /// <summary>
    /// 增加新装货地址功能返回值封装类。
    /// </summary>
    public class AddPlLoadingAddrReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 如果成功添加，这里返回新装货地址的Id。
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 修改装货地址信息功能参数封装类。
    /// </summary>
    public class ModifyPlLoadingAddrParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 装货地址数据。
        /// </summary>
        public PlLoadingAddr PlLoadingAddr { get; set; }
    }

    /// <summary>
    /// 修改装货地址信息功能返回值封装类。
    /// </summary>
    public class ModifyPlLoadingAddrReturnDto : ReturnDtoBase
    {
    }
    #endregion 装货地址

    #region 黑名单
    /// <summary>
    /// 获取所有黑名单功能的返回值封装类。
    /// </summary>
    public class GetAllCustomerBlacklistReturnDto : PagingReturnDtoBase<CustomerBlacklist>
    {
    }

    /// <summary>
    /// 增加新黑名单功能参数封装类。
    /// </summary>
    public class AddCustomerBlacklistParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 新黑名单信息。其中Id可以是任何值，返回时会指定新值。
        /// </summary>
        public CustomerBlacklist CustomerBlacklist { get; set; }
    }

    /// <summary>
    /// 增加新黑名单功能返回值封装类。
    /// </summary>
    public class AddCustomerBlacklistReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 如果成功添加，这里返回新黑名单的Id。
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 删除黑名单功能参数封装类。
    /// </summary>
    public class RemoveCustomerBlacklistParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 指定的是客户Id(CustomerId)。
        /// </summary>
        public Guid CustomerId { get; set; }

        /// <summary>
        /// 删除的类型。3=移除超额，4=移除超期。
        /// </summary>
        [Range(3, 4)]
        public byte Kind { get; set; }

        /// <summary>
        /// 删除实体的注释。
        /// </summary>
        public string Remark { get; set; }

    }

    /// <summary>
    /// 删除新黑名单功能返回值封装类。
    /// </summary>
    public class RemoveCustomerBlacklistReturnDto
    {
        /// <summary>
        /// 新增的"冲红"实体。
        /// </summary>
        public CustomerBlacklist Result { get; set; }
    }

    #endregion 黑名单

    #region 提单
    /// <summary>
    /// 标记删除提单功能的参数封装类。
    /// </summary>
    public class RemovePlTidanParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 标记删除提单功能的返回值封装类。
    /// </summary>
    public class RemovePlTidanReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 获取所有提单功能的返回值封装类。
    /// </summary>
    public class GetAllPlTidanReturnDto : PagingReturnDtoBase<PlTidan>
    {
    }

    /// <summary>
    /// 增加新提单功能参数封装类。
    /// </summary>
    public class AddPlTidanParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 新提单信息。其中Id可以是任何值，返回时会指定新值。
        /// </summary>
        public PlTidan PlTidan { get; set; }
    }

    /// <summary>
    /// 增加新提单功能返回值封装类。
    /// </summary>
    public class AddPlTidanReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 如果成功添加，这里返回新提单的Id。
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 修改提单信息功能参数封装类。
    /// </summary>
    public class ModifyPlTidanParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 提单数据。
        /// </summary>
        public PlTidan PlTidan { get; set; }
    }

    /// <summary>
    /// 修改提单信息功能返回值封装类。
    /// </summary>
    public class ModifyPlTidanReturnDto : ReturnDtoBase
    {
    }
    #endregion 提单

    #region 开票信息
    /// <summary>
    /// 标记删除开票信息功能的参数封装类。
    /// </summary>
    public class RemovePlTaxInfoParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 标记删除开票信息功能的返回值封装类。
    /// </summary>
    public class RemovePlTaxInfoReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 获取所有开票信息功能的返回值封装类。
    /// </summary>
    public class GetAllPlTaxInfoReturnDto : PagingReturnDtoBase<PlTaxInfo>
    {
    }

    /// <summary>
    /// 增加新开票信息功能参数封装类。
    /// </summary>
    public class AddPlTaxInfoParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 新开票信息信息。其中Id可以是任何值，返回时会指定新值。
        /// </summary>
        public PlTaxInfo PlTaxInfo { get; set; }
    }

    /// <summary>
    /// 增加新开票信息功能返回值封装类。
    /// </summary>
    public class AddPlTaxInfoReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 如果成功添加，这里返回新开票信息的Id。
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 修改开票信息信息功能参数封装类。
    /// </summary>
    public class ModifyPlTaxInfoParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 开票信息数据。
        /// </summary>
        public PlTaxInfo PlTaxInfo { get; set; }
    }

    /// <summary>
    /// 修改开票信息信息功能返回值封装类。
    /// </summary>
    public class ModifyPlTaxInfoReturnDto : ReturnDtoBase
    {
    }
    #endregion 开票信息

    #region 业务负责人的所属关系的CRUD
    /// <summary>
    /// 获取业务负责人的所属关系返回值封装类。
    /// </summary>
    public class GetAllPlBusinessHeaderReturnDto : PagingReturnDtoBase<PlBusinessHeader>
    {
    }

    /// <summary>
    /// 删除业务负责人的所属关系的功能参数封装类。
    /// </summary>
    public class RemovePlBusinessHeaderParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 用户的Id。
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// 商户/组织机构的Id。
        /// </summary>
        public Guid CustomerId { get; set; }

        /// <summary>
        /// 负责的业务Id。连接业务种类字典。
        /// </summary>
        public Guid OrderTypeId { get; set; }

    }

    /// <summary>
    /// 删除业务负责人的所属关系的功能返回值封装类。
    /// </summary>
    public class RemovePlBusinessHeaderReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 增加业务负责人的所属关系的功能参数封装类，
    /// </summary>
    public class AddPlBusinessHeaderParamsDto : AddParamsDtoBase<PlBusinessHeader>
    {
    }

    /// <summary>
    /// 增加业务负责人的所属关系的功能返回值封装类。
    /// </summary>
    public class AddPlBusinessHeaderReturnDto : ReturnDtoBase
    {
    }

    #endregion 业务负责人的所属关系的CRUD

    #region 客户本体

    /// <summary>
    /// 查询的参数封装类。
    /// </summary>
    public class GetAllCustomer2ParamsDto : PagingParamsDtoBase
    {
    }

    /// <summary>
    /// 查询的返回值封装类。
    /// </summary>
    public class GetAllCustomer2ReturnDto : PagingReturnDtoBase<PlCustomer>
    {
    }

    /// <summary>
    /// 标记删除客户功能的参数封装类。
    /// </summary>
    public class RemoveCustomerParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 标记删除客户功能的返回值封装类。
    /// </summary>
    public class RemoveCustomerReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 获取所有客户功能的返回值封装类。
    /// </summary>
    public class GetAllCustomerReturnDto : PagingReturnDtoBase<PlCustomer>
    {
    }

    /// <summary>
    /// 增加新客户功能参数封装类。
    /// </summary>
    public class AddCustomerParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 新客户信息。其中Id可以是任何值，返回时会指定新值。
        /// </summary>
        public PlCustomer Customer { get; set; }
    }

    /// <summary>
    /// 增加新客户功能返回值封装类。
    /// </summary>
    public class AddCustomerReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 如果成功添加，这里返回新客户的Id。
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 修改客户信息功能参数封装类。
    /// </summary>
    public class ModifyCustomerParamsDto : ModifyParamsDtoBase<PlCustomer>
    {

    }

    /// <summary>
    /// 修改客户信息功能返回值封装类。
    /// </summary>
    public class ModifyCustomerReturnDto : ReturnDtoBase
    {
    }
    #endregion 客户本体

    #region 联系人
    /// <summary>
    /// 标记删除联系人功能的参数封装类。
    /// </summary>
    public class RemoveCustomerContactParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 标记删除联系人功能的返回值封装类。
    /// </summary>
    public class RemoveCustomerContactReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 获取所有联系人功能的返回值封装类。
    /// </summary>
    public class GetAllCustomerContactReturnDto : PagingReturnDtoBase<PlCustomerContact>
    {
    }

    /// <summary>
    /// 增加新联系人功能参数封装类。
    /// </summary>
    public class AddCustomerContactParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 新联系人信息。其中Id可以是任何值，返回时会指定新值。
        /// </summary>
        public PlCustomerContact CustomerContact { get; set; }
    }

    /// <summary>
    /// 增加新联系人功能返回值封装类。
    /// </summary>
    public class AddCustomerContactReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 如果成功添加，这里返回新联系人的Id。
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 修改联系人信息功能参数封装类。
    /// </summary>
    public class ModifyCustomerContactParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 联系人数据。
        /// </summary>
        public PlCustomerContact CustomerContact { get; set; }
    }

    /// <summary>
    /// 修改联系人信息功能返回值封装类。
    /// </summary>
    public class ModifyCustomerContactReturnDto : ReturnDtoBase
    {
    }
    #endregion 联系人

}
