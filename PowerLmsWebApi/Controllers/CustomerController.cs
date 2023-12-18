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
    }

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
