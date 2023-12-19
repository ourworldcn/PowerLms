using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PowerLms.Data;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Dto;
using System.ComponentModel.DataAnnotations;

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
        public CustomerController(IServiceProvider serviceProvider, AccountManager accountManager, PowerLmsUserDbContext dbContext, EntityManager entityManager, IMapper mapper)
        {
            _ServiceProvider = serviceProvider;
            _AccountManager = accountManager;
            _DbContext = dbContext;
            _EntityManager = entityManager;
            _Mapper = mapper;
        }

        IServiceProvider _ServiceProvider;
        AccountManager _AccountManager;

        readonly PowerLmsUserDbContext _DbContext;

        EntityManager _EntityManager;
        IMapper _Mapper;

        #region 客户资料本体的CRUD

        /// <summary>
        /// 获取全部客户。
        /// </summary>
        /// <param name="token">登录令牌。</param>
        /// <param name="startIndex">起始位置，从0开始。</param>
        /// <param name="count">最大返回数量。</param>
        /// <param name="conditional">查询的条件。支持 name，ShortName，displayname，Id。不区分大小写。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllCustomerReturnDto> GetAllCustomer(Guid token, [Range(0, int.MaxValue, ErrorMessage = "必须大于或等于0.")] int startIndex, [Range(-1, int.MaxValue)] int count = -1,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetAccountFromToken(token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllCustomerReturnDto();
            var coll = _DbContext.PlCustomers.AsNoTracking().OrderBy(c => c.Id).Skip(startIndex);
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
            var prb = _EntityManager.GetAll(coll, startIndex, count);
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
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new AddCustomerReturnDto();
            model.Customer.GenerateNewId();
            _DbContext.PlCustomers.Add(model.Customer);
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
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyCustomerReturnDto();
            if (_DbContext.PlCustomers.Find(model.Customer.Id) is not PlCustomer mcht) return NotFound();
            _DbContext.Entry(mcht).CurrentValues.SetValues(model.Customer);
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
        [HttpDelete]
        public ActionResult<RemoveCustomerReturnDto> RemoveCustomer(RemoveCustomerParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemoveCustomerReturnDto();
            var id = model.Id;
            var dbSet = _DbContext.PlCustomers;
            var item = dbSet.Find(id);
            if (item is null) return BadRequest();
            _DbContext.SaveChanges();
            return result;
        }
        #endregion 客户资料本体的CRUD

        #region 客户上的联系人操作

        /// <summary>
        /// 获取全部客户联系人。
        /// </summary>
        /// <param name="token">登录令牌。</param>
        /// <param name="startIndex">起始位置，从0开始。</param>
        /// <param name="count">最大返回数量。</param>
        /// <param name="conditional">查询的条件。支持 displayname，Id。不区分大小写。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllCustomerContactReturnDto> GetAllCustomerContact(Guid token, [Range(0, int.MaxValue, ErrorMessage = "必须大于或等于0.")] int startIndex, [Range(-1, int.MaxValue)] int count = -1,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetAccountFromToken(token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllCustomerContactReturnDto();
            var coll = _DbContext.PlCustomerContacts.AsNoTracking().OrderBy(c => c.Id).Skip(startIndex);
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
            var prb = _EntityManager.GetAll(coll, startIndex, count);
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
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyCustomerContactReturnDto();
            if (_DbContext.PlCustomerContacts.Find(model.CustomerContact.Id) is not PlCustomerContact mcht) return NotFound();
            _DbContext.Entry(mcht).CurrentValues.SetValues(model.CustomerContact);
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
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemoveCustomerContactReturnDto();
            var id = model.Id;
            var dbSet = _DbContext.PlCustomerContacts;
            var item = dbSet.Find(id);
            if (item is null) return BadRequest();
            _DbContext.SaveChanges();
            return result;
        }
        #endregion 客户上的联系人操作

        #region 业务负责人的所属关系的CRUD

        /// <summary>
        /// 获取业务负责人的所属关系。
        /// </summary>
        /// <param name="token">登录令牌。</param>
        /// <param name="startIndex">起始位置，从0开始。</param>
        /// <param name="count">最大返回数量。-1表示全返回。</param>
        /// <param name="conditional">查询的条件。支持 CustomerId,AccountId,OrderTypeId</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定类别Id无效。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllPlBusinessHeaderReturnDto> GetAllPlBusinessHeader(Guid token,
            [Range(0, int.MaxValue, ErrorMessage = "必须大于或等于0.")] int startIndex, [FromQuery][Range(-1, int.MaxValue)] int count = -1,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetAccountFromToken(token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllPlBusinessHeaderReturnDto();
            var coll = _DbContext.PlCustomerBusinessHeaders.AsNoTracking();
            foreach (var item in conditional)
                if (string.Equals(item.Key, "CustomerId", StringComparison.OrdinalIgnoreCase))
                {
                    if (Guid.TryParse(item.Value, out var customerId))
                        coll = coll.Where(c => c.CustomerId == customerId);
                }
                else if (string.Equals(item.Key, "accountId", StringComparison.OrdinalIgnoreCase))
                {
                    if (Guid.TryParse(item.Value, out var accountId))
                        coll = coll.Where(c => c.AccountId == accountId);
                }
                else if (string.Equals(item.Key, "OrderTypeId", StringComparison.OrdinalIgnoreCase))
                {
                    if (Guid.TryParse(item.Value, out var orderTypeId))
                        coll = coll.Where(c => c.OrderTypeId == orderTypeId);
                }
            var prb = _EntityManager.GetAll(coll, startIndex, count);
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
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemovePlBusinessHeaderReturnDto();
            DbSet<PlBusinessHeader> dbSet = _DbContext.PlCustomerBusinessHeaders;
            var item = dbSet.Find(model.UserId, model.OrgId);
            if (item is null) return BadRequest();
            _DbContext.Remove(item);
            _DbContext.SaveChanges();
            return result;
        }

        #endregion 业务负责人的所属关系的CRUD

        #region 客户上的开票信息操作

        /// <summary>
        /// 获取全部客户开票信息。
        /// </summary>
        /// <param name="token">登录令牌。</param>
        /// <param name="startIndex">起始位置，从0开始。</param>
        /// <param name="count">最大返回数量。</param>
        /// <param name="conditional">查询的条件。支持 CustomerId，Id,Number。不区分大小写。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllPlTaxInfoReturnDto> GetAllPlTaxInfo(Guid token, [Range(0, int.MaxValue, ErrorMessage = "必须大于或等于0.")] int startIndex, [Range(-1, int.MaxValue)] int count = -1,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetAccountFromToken(token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllPlTaxInfoReturnDto();
            var coll = _DbContext.PlCustomerTaxInfos.AsNoTracking().OrderBy(c => c.Id).Skip(startIndex);
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
            var prb = _EntityManager.GetAll(coll, startIndex, count);
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
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyPlTaxInfoReturnDto();
            if (_DbContext.PlCustomerTaxInfos.Find(model.PlTaxInfo.Id) is not PlTaxInfo mcht) return NotFound();
            _DbContext.Entry(mcht).CurrentValues.SetValues(model.PlTaxInfo);
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
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemovePlTaxInfoReturnDto();
            var id = model.Id;
            var dbSet = _DbContext.PlCustomerTaxInfos;
            var item = dbSet.Find(id);
            if (item is null) return BadRequest();
            _DbContext.SaveChanges();
            return result;
        }
        #endregion 客户上的开票信息操作

    }

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
        public Guid OrgId { get; set; }
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
    public class ModifyCustomerParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 客户数据。
        /// </summary>
        public PlCustomer Customer { get; set; }
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
