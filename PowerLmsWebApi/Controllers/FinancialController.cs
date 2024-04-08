using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PowerLms.Data;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Dto;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// 财务相关功能控制器。
    /// </summary>
    public class FinancialController : PlControllerBase
    {
        /// <summary>
        /// 构造函数。Debit 和credit。
        /// </summary>
        public FinancialController(AccountManager accountManager, ServiceProvider serviceProvider, EntityManager entityManager,
            PowerLmsUserDbContext dbContext, ILogger<FinancialController> logger, IMapper mapper)
        {
            _AccountManager = accountManager;
            _ServiceProvider = serviceProvider;
            _EntityManager = entityManager;
            _DbContext = dbContext;
            _Logger = logger;
            _Mapper = mapper;
        }

        private AccountManager _AccountManager;
        private ServiceProvider _ServiceProvider;
        private EntityManager _EntityManager;
        private PowerLmsUserDbContext _DbContext;
        readonly ILogger<FinancialController> _Logger;
        readonly IMapper _Mapper;

        #region 业务费用申请单

        /// <summary>
        /// 获取全部业务费用申请单。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">查询的条件。支持 Id。不区分大小写。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllDocFeeRequisitionReturnDto> GetAllDocFeeRequisition([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllDocFeeRequisitionReturnDto();

            var dbSet = _DbContext.DocFeeRequisitions;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            foreach (var item in conditional)
                if (string.Equals(item.Key, "Id", StringComparison.OrdinalIgnoreCase))
                {
                    if (Guid.TryParse(item.Value, out var id))
                        coll = coll.Where(c => c.Id == id);
                }
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 增加新业务费用申请单。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpPost]
        public ActionResult<AddDocFeeRequisitionReturnDto> AddDocFeeRequisition(AddDocFeeRequisitionParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context)
            {
                _Logger.LogWarning("无效的令牌{token}", model.Token);
                return Unauthorized();
            }
            var result = new AddDocFeeRequisitionReturnDto();
            var entity = model.DocFeeRequisition;
            entity.GenerateNewId();
            _DbContext.DocFeeRequisitions.Add(model.DocFeeRequisition);
            entity.MakerId = context.User.Id;
            entity.MakeDateTime = OwHelper.WorldNow;
            _DbContext.SaveChanges();
            result.Id = model.DocFeeRequisition.Id;
            return result;
        }

        /// <summary>
        /// 修改业务费用申请单信息。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的业务费用申请单不存在。</response>  
        [HttpPut]
        public ActionResult<ModifyDocFeeRequisitionReturnDto> ModifyDocFeeRequisition(ModifyDocFeeRequisitionParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyDocFeeRequisitionReturnDto();
            if (!_EntityManager.Modify(new[] { model.DocFeeRequisition })) return NotFound();
            //忽略不可更改字段
            var entity = _DbContext.Entry(model.DocFeeRequisition);
            entity.Property(c => c.MakeDateTime).IsModified = false;
            entity.Property(c => c.MakerId).IsModified = false;
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 删除指定Id的业务费用申请单。慎用！
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的业务费用申请单不存在。</response>  
        [HttpDelete]
        public ActionResult<RemoveDocFeeRequisitionReturnDto> RemoveDocFeeRequisition(RemoveDocFeeRequisitionParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemoveDocFeeRequisitionReturnDto();
            var id = model.Id;
            var dbSet = _DbContext.DocFeeRequisitions;
            var item = dbSet.Find(id);
            if (item is null) return BadRequest();
            _EntityManager.Remove(item);
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 获取指定费用的剩余未申请金额。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的业务费用至少有一个不存在。</response>  
        [HttpGet]
        public ActionResult<GetFeeRemainingReturnDto> GetFeeRemaining([FromQuery] GetFeeRemainingParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetFeeRemainingReturnDto();
            var fees = _DbContext.DocFees.Where(c => model.FeeIds.Contains(c.Id));
            if (fees.Count() !=model.FeeIds.Count) return NotFound();

            //var coll = _DbContext.DocFeeRequisitionItems.Where(c => c.FeeId == fees.Id);
            //var happened = coll.Sum(c => c.Amount);   //已申请的金额
            //result.Remaining = fees.Amount - happened;
            return result;
        }
        #endregion 业务费用申请单

    }

    #region 业务费用申请单

    /// <summary>
    /// 获取指定费用的剩余未申请金额参数封装类。
    /// </summary>
    public class GetFeeRemainingParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 费用的Id集合。
        /// </summary>
        public List<Guid> FeeIds { get; set; } = new List<Guid>();
    }

    /// <summary>
    /// 获取指定费用的剩余未申请金额功能返回值封装类。
    /// </summary>
    public class GetFeeRemainingReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 剩余未申请的费用。
        /// </summary>
        public decimal Remaining { get; set; }
    }

    /// <summary>
    /// 标记删除业务费用申请单功能的参数封装类。
    /// </summary>
    public class RemoveDocFeeRequisitionParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 标记删除业务费用申请单功能的返回值封装类。
    /// </summary>
    public class RemoveDocFeeRequisitionReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 获取所有业务费用申请单功能的返回值封装类。
    /// </summary>
    public class GetAllDocFeeRequisitionReturnDto : PagingReturnDtoBase<DocFeeRequisition>
    {
    }

    /// <summary>
    /// 增加新业务费用申请单功能参数封装类。
    /// </summary>
    public class AddDocFeeRequisitionParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 新业务费用申请单信息。其中Id可以是任何值，返回时会指定新值。
        /// </summary>
        public DocFeeRequisition DocFeeRequisition { get; set; }
    }

    /// <summary>
    /// 增加新业务费用申请单功能返回值封装类。
    /// </summary>
    public class AddDocFeeRequisitionReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 如果成功添加，这里返回新业务费用申请单的Id。
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 修改业务费用申请单信息功能参数封装类。
    /// </summary>
    public class ModifyDocFeeRequisitionParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 业务费用申请单数据。
        /// </summary>
        public DocFeeRequisition DocFeeRequisition { get; set; }
    }

    /// <summary>
    /// 修改业务费用申请单信息功能返回值封装类。
    /// </summary>
    public class ModifyDocFeeRequisitionReturnDto : ReturnDtoBase
    {
    }
    #endregion 业务费用申请单


}
