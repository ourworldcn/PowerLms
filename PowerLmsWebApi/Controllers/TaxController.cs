using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PowerLms.Data;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Dto;
using System.ComponentModel;
using System.Net;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// 税务相关功能控制器。
    /// </summary>
    public class TaxController : PlControllerBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public TaxController(AccountManager accountManager, IServiceProvider serviceProvider, EntityManager entityManager,
            PowerLmsUserDbContext dbContext, ILogger<TaxController> logger, IMapper mapper, AuthorizationManager authorizationManager, OwSqlAppLogger sqlAppLogger)
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
        private readonly ILogger<TaxController> _Logger;
        private readonly IMapper _Mapper;
        private readonly AuthorizationManager _AuthorizationManager;
        private readonly OwSqlAppLogger _SqlAppLogger;

        #region 税务发票渠道相关

        /// <summary>
        /// 获取指定ID的税务发票渠道。
        /// </summary>
        /// <param name="model">分页参数</param>
        /// <param name="conditional">查询条件。支持通用查询接口。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllTaxInvoiceChannelReturnDto> GetAllTaxInvoiceChannel([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllTaxInvoiceChannelReturnDto();

            var dbSet = _DbContext.TaxInvoiceChannels;
            var coll = dbSet.AsNoTracking();

            // 使用通用查询条件处理方式
            coll = EfHelper.GenerateWhereAnd(coll, conditional);

            // 应用排序
            coll = coll.OrderBy(model.OrderFieldName, model.IsDesc);

            // 获取分页结果
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 修改税务发票渠道记录。仅能修改显示名称.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定实体的Id不存在。通常这是Bug.在极端情况下可能是并发问题。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPut]
        public ActionResult<ModifyTaxInvoiceChannelReturnDto> ModifyTaxInvoiceChannel(ModifyTaxInvoiceChannelParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyTaxInvoiceChannelReturnDto();
            if (!_EntityManager.Modify(model.Items))
            {
                var errResult = new StatusCodeResult(OwHelper.GetLastError()) { };
                return errResult;
            }
            foreach (var item in model.Items)
            {
                _DbContext.Entry(item).Property(c => c.InvoiceChannel).IsModified = false;
                _DbContext.Entry(item).Property(c => c.InvoiceChannelParams).IsModified = false;
            }
            _DbContext.SaveChanges();
            return result;
        }

        #endregion 税务发票渠道相关

        #region 税务发票渠道账号相关操作

        /// <summary>
        /// 获取税务发票渠道账号列表。
        /// </summary>
        /// <param name="model">分页参数</param>
        /// <param name="conditional">查询条件</param>
        /// <returns>税务发票渠道账号列表</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        [Description("获取税务发票渠道账号列表")]
        public ActionResult<GetAllTaxInvoiceChannelAccountReturnDto> GetAllTaxInvoiceChannelAccount(
            [FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();

            var result = new GetAllTaxInvoiceChannelAccountReturnDto();

            var dbSet = _DbContext.Set<TaxInvoiceChannelAccount>();
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();

            // 使用通用查询条件处理
            coll = EfHelper.GenerateWhereAnd(coll, conditional);

            // 获取分页结果
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 添加税务发票渠道账号。
        /// </summary>
        /// <param name="model">添加参数</param>
        /// <returns>操作结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpPost]
        [Description("添加税务发票渠道账号")]
        public ActionResult<AddTaxInvoiceChannelAccountReturnDto> AddTaxInvoiceChannelAccount(AddTaxInvoiceChannelAccountParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();

            var result = new AddTaxInvoiceChannelAccountReturnDto();

            try
            {
                // 确保要添加的项不为空
                if (model.Item == null)
                {
                    result.ErrorCode = 400;
                    result.DebugMessage = "要添加的税务发票渠道账号不能为空";
                    return result;
                }

                // 生成新的ID
                model.Item.GenerateNewId();

                // 添加到数据库
                _DbContext.Set<TaxInvoiceChannelAccount>().Add(model.Item);
                _DbContext.SaveChanges();

                // 返回添加的ID
                result.Id = model.Item.Id;
                return result;
            }
            catch (Exception ex)
            {
                result.ErrorCode = 500;
                result.DebugMessage = $"添加税务发票渠道账号失败：{ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// 修改税务发票渠道账号。
        /// </summary>
        /// <param name="model">修改参数</param>
        /// <returns>操作结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定实体的Id不存在。通常这是Bug.在极端情况下可能是并发问题。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpPut]
        [Description("修改税务发票渠道账号")]
        public ActionResult<ModifyTaxInvoiceChannelAccountReturnDto> ModifyTaxInvoiceChannelAccount(ModifyTaxInvoiceChannelAccountParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();

            var result = new ModifyTaxInvoiceChannelAccountReturnDto();

            try
            {
                // 确保要修改的项不为空
                if (model.Items == null || model.Items.Count == 0)
                {
                    result.ErrorCode = 400;
                    result.DebugMessage = "要修改的税务发票渠道账号不能为空";
                    return result;
                }

                // 使用EntityManager进行修改
                if (!_EntityManager.Modify(model.Items))
                {
                    return new StatusCodeResult(OwHelper.GetLastError());
                }

                _DbContext.SaveChanges();
                return result;
            }
            catch (Exception ex)
            {
                result.ErrorCode = 500;
                result.DebugMessage = $"修改税务发票渠道账号失败：{ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// 删除税务发票渠道账号。
        /// </summary>
        /// <param name="model">删除参数</param>
        /// <returns>操作结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定实体的Id不存在。通常这是Bug.在极端情况下可能是并发问题。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpDelete]
        [Description("删除税务发票渠道账号")]
        public ActionResult<RemoveTaxInvoiceChannelAccountReturnDto> RemoveTaxInvoiceChannelAccount(RemoveTaxInvoiceChannelAccountParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();

            var result = new RemoveTaxInvoiceChannelAccountReturnDto();

            try
            {
                var id = model.Id;
                var item = _DbContext.Set<TaxInvoiceChannelAccount>().Find(id);

                if (item == null)
                {
                    result.ErrorCode = 404;
                    result.DebugMessage = "未找到指定的税务发票渠道账号";
                    return result;
                }

                // 直接物理删除
                _EntityManager.Remove(item);
                _DbContext.SaveChanges();

                return result;
            }
            catch (Exception ex)
            {
                result.ErrorCode = 500;
                result.DebugMessage = $"删除税务发票渠道账号失败：{ex.Message}";
                return result;
            }
        }
        #endregion

        #region 机构渠道账号管理

        /// <summary>获取指定条件的机构渠道账号列表，支持分页和排序。</summary>
        /// <param name="model">分页和排序参数</param>
        /// <param name="conditional">查询条件字典</param>
        /// <returns>机构渠道账号列表及总数</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllOrgTaxChannelAccountReturnDto> GetAllOrgTaxChannelAccount([FromQuery] PagingParamsDtoBase model, [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized(); // 验证令牌
            var result = new GetAllOrgTaxChannelAccountReturnDto();
            var dbSet = _DbContext.OrgTaxChannelAccounts;    //.Where(c => c.OrgId == context.User.OrgId); // 仅查询当前用户机构的数据
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking(); // 排序并禁用实体追踪
            coll = EfHelper.GenerateWhereAnd(coll, conditional); // 应用查询条件
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count); // 获取分页数据
            _Mapper.Map(prb, result); // 映射到返回对象
            return result;
        }

        /// <summary>新增机构渠道账号，自动设置创建信息，支持设置默认账号。</summary>
        /// <param name="model">包含新账号信息的参数对象</param>
        /// <returns>添加结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        /// <response code="409">已存在相同的机构和渠道账号关联。</response>  
        [HttpPost]
        public ActionResult<AddOrgTaxChannelAccountReturnDto> AddOrgTaxChannelAccount(AddOrgTaxChannelAccountParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized(); // 验证令牌
            if (!context.User.IsSuperAdmin) return StatusCode((int)HttpStatusCode.Forbidden, "只有超管可以使用此功能"); ; // 仅超级管理员可以增删改加机构渠道账号

            var result = new AddOrgTaxChannelAccountReturnDto();
            var entity = model.Item;

            // 检查是否已存在相同机构和渠道账号的记录
            var existingAccount = _DbContext.OrgTaxChannelAccounts.FirstOrDefault(a =>
                a.OrgId == entity.OrgId && a.ChannelAccountId == entity.ChannelAccountId);

            if (existingAccount != null)
            {
                result.HasError = true;
                result.DebugMessage = "已存在相同的机构和渠道账号关联";
                return Conflict(result.DebugMessage);
            }

            // 生成新ID
            entity.GenerateNewId();

            if (entity.IsDefault) // 如果设为默认账号
            {
                var defaultAccounts = _DbContext.OrgTaxChannelAccounts.Where(c => c.OrgId == entity.OrgId && c.IsDefault).ToArray();
                foreach (var acc in defaultAccounts) acc.IsDefault = false; // 取消其他默认账号
            }

            entity.CreateDateTime = DateTime.UtcNow; // 设置创建时间
            entity.CreateBy = context.User.Id; // 设置创建者
            _DbContext.OrgTaxChannelAccounts.Add(entity); // 添加新账号
            _DbContext.SaveChanges(); // 保存更改

            result.Id = entity.Id; // 返回新创建记录的ID
            return result;
        }

        /// <summary>修改机构渠道账号信息，支持批量修改，自动维护默认账号状态。</summary>
        /// <param name="model">包含要修改账号信息的参数对象</param>
        /// <returns>修改结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        /// <response code="409">已存在相同的机构和渠道账号关联。</response>  
        [HttpPut]
        public ActionResult<ModifyOrgTaxChannelAccountReturnDto> ModifyOrgTaxChannelAccount(ModifyOrgTaxChannelAccountParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized(); // 验证令牌
            if (!context.User.IsSuperAdmin) return StatusCode((int)HttpStatusCode.Forbidden, "只有超管可以使用此功能"); ; // 仅超级管理员可以增删改加机构渠道账号
            var result = new ModifyOrgTaxChannelAccountReturnDto();
            if (model.Items?.Count == 0) { result.HasError = true; return result; } // 验证输入
            int modifiedCount = 0;

            foreach (var item in model.Items)
            {
                // 确保只能修改当前用户机构的数据 - 可选附加安全措施
                var existingAccount = _DbContext.OrgTaxChannelAccounts.Find(item.Id);

                if (existingAccount == null) continue; // 跳过不存在或不属于当前用户机构的账号
                //新实体的OrgId和ChannelAccountId必须不和已有实体重复
                if (_DbContext.OrgTaxChannelAccounts.Any(a => a.OrgId == item.OrgId && a.ChannelAccountId == item.ChannelAccountId && a.Id != item.Id))
                {
                    result.HasError = true;
                    result.DebugMessage = "已存在相同的机构和渠道账号关联";
                    return Conflict(result.DebugMessage);
                }

                // 更新实体
                _DbContext.Entry(existingAccount).CurrentValues.SetValues(item);

                // 设置修改时间和修改者
                existingAccount.LastModifyUtc = OwHelper.WorldNow;
                existingAccount.LastModifyBy = context.User.Id;

                // 如果设为默认账号，取消其他账号的默认状态
                if (item.IsDefault && !existingAccount.IsDefault)
                {
                    var defaultAccounts = _DbContext.OrgTaxChannelAccounts
                        .Where(c => c.OrgId == item.OrgId && c.IsDefault && c.Id != item.Id).ToList();
                    foreach (var acc in defaultAccounts) acc.IsDefault = false;
                }

                _SqlAppLogger.LogGeneralInfo($"Modify.{nameof(OrgTaxChannelAccount)}.{item.Id}"); // 记录系统日志
                modifiedCount++;
            }

            if (modifiedCount == 0) { result.HasError = true; return NotFound(); } // 未找到要修改的账号
            _DbContext.SaveChanges(); // 保存更改
            return result;
        }

        /// <summary>删除指定的机构渠道账号。</summary>
        /// <param name="model">包含要删除账号ID的参数对象</param>
        /// <returns>删除结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpDelete]
        public ActionResult<RemoveOrgTaxChannelAccountReturnDto> RemoveOrgTaxChannelAccount(RemoveOrgTaxChannelAccountParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized(); // 验证令牌
            if (!context.User.IsSuperAdmin) return StatusCode((int)HttpStatusCode.Forbidden, "只有超管可以使用此功能"); ; // 仅超级管理员可以增删改加机构渠道账号
            var result = new RemoveOrgTaxChannelAccountReturnDto();

            // 查找要删除的账号，确保只能删除当前用户机构的数据
            var account = _DbContext.OrgTaxChannelAccounts.FirstOrDefault(a => a.Id == model.Id && a.OrgId == context.User.OrgId);

            if (account == null) return NotFound(); // 账号不存在或不属于当前用户机构

            _DbContext.OrgTaxChannelAccounts.Remove(account); // 删除账号
            _SqlAppLogger.LogGeneralInfo($"删除渠道账号 {nameof(OrgTaxChannelAccount)}.{account.Id}"); // 记录日志
            _DbContext.SaveChanges(); // 保存更改

            return result;
        }

        /// <summary>设置指定的机构渠道账号为默认账号，同时取消其他账号的默认状态。</summary>
        /// <param name="model">包含要设为默认的账号ID的参数对象</param>
        /// <returns>设置结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPost]
        public ActionResult<SetDefaultOrgTaxChannelAccountReturnDto> SetDefaultOrgTaxChannelAccount(SetDefaultOrgTaxChannelAccountParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized(); // 验证令牌
            if (!context.User.IsSuperAdmin) return StatusCode((int)HttpStatusCode.Forbidden, "只有超管可以使用此功能"); ; // 仅超级管理员可以增删改加机构渠道账号
            var result = new SetDefaultOrgTaxChannelAccountReturnDto();

            // 查找要设为默认的账号，确保只能操作当前用户机构的数据
            var account = _DbContext.OrgTaxChannelAccounts.FirstOrDefault(a => a.Id == model.Id);

            if (account == null) return NotFound(); // 账号不存在或不属于当前用户机构

            // 获取该机构的所有账号
            var allAccounts = _DbContext.OrgTaxChannelAccounts.Where(a => a.OrgId == account.OrgId).ToList();

            // 设置默认状态
            foreach (var acc in allAccounts)
            {
                acc.IsDefault = (acc.Id == model.Id);
            }

            _DbContext.SaveChanges(); // 保存更改
            return result;
        }

        #endregion

        #region 发票相关
        #region 税务发票信息

        /// <summary>
        /// 获取全部税务发票信息。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">查询的条件。
        /// 通用条件写法:所有条件都是字符串，对区间的写法是用逗号分隔（字符串类型暂时不支持区间且都是模糊查询）如"2024-1-1,2024-1-2"。
        /// 对强制取null的约束，则写"null"。
        /// 支持 DocFeeRequisition.属性名 格式的键，用于关联到申请单表进行过滤。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllTaxInvoiceInfoReturnDto> GetAllTaxInvoiceInfo([FromQuery] PagingParamsDtoBase model,
            [FromQuery][ModelBinder(typeof(DotKeyDictionaryModelBinder))] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllTaxInvoiceInfoReturnDto();

            try
            {
                // 从条件中分离出DocFeeRequisition开头的条件
                string docFeeRequisitionPrefix = $"{nameof(DocFeeRequisition)}.";

                // 初始化条件字典
                var reqConditions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var invoiceConditions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                // 如果有条件，需要分类处理
                if (conditional != null && conditional.Count > 0)
                {
                    foreach (var pair in conditional)
                    {
                        if (pair.Key.StartsWith(docFeeRequisitionPrefix, StringComparison.OrdinalIgnoreCase))
                        {
                            // 提取申请单条件，去掉前缀
                            string fieldName = pair.Key.Substring(docFeeRequisitionPrefix.Length);
                            reqConditions[fieldName] = pair.Value;
                        }
                        else
                        {
                            // 保存发票条件
                            invoiceConditions[pair.Key] = pair.Value;
                        }
                    }
                }

                IQueryable<TaxInvoiceInfo> dbSet;

                // 如果有DocFeeRequisition相关的条件，则需要联合查询
                if (reqConditions.Count > 0)
                {
                    _Logger.LogDebug("应用申请单过滤条件: {conditions}",
                        string.Join(", ", reqConditions.Select(kv => $"{kv.Key}={kv.Value}")));

                    // 先获取符合条件的申请单
                    var requisitions = EfHelper.GenerateWhereAnd(_DbContext.DocFeeRequisitions.AsNoTracking(), reqConditions);

                    // 通过申请单关联查询发票信息
                    dbSet = from invoice in _DbContext.TaxInvoiceInfos
                            join req in requisitions on invoice.DocFeeRequisitionId equals req.Id
                            select invoice;
                }
                else
                {
                    // 不需要关联查询
                    dbSet = _DbContext.TaxInvoiceInfos;
                }

                // 应用发票信息自身的过滤条件
                var coll = EfHelper.GenerateWhereAnd(dbSet, invoiceConditions);

                // 应用排序和分页
                coll = coll.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
                var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
                _Mapper.Map(prb, result);
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "获取税务发票信息时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"获取税务发票信息时发生错误: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// 增加新税务发票信息。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        /// <response code="409">同一申请单不能重复开票。</response>  
        [HttpPost]
        public ActionResult<AddTaxInvoiceInfoReturnDto> AddTaxInvoiceInfo(AddTaxInvoiceInfoParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
            {
                _Logger.LogWarning("无效的令牌{token}", model.Token);
                return Unauthorized();
            }

            string err;
            //if (!_AuthorizationManager.Demand(out err, "F.3.1")) return StatusCode((int)HttpStatusCode.Forbidden, err);

            var result = new AddTaxInvoiceInfoReturnDto();
            var entity = model.TaxInvoiceInfo;

            // 检查是否已存在该申请单的发票记录
            if (entity.DocFeeRequisitionId.HasValue && entity.DocFeeRequisitionId != Guid.Empty)
            {
                var existingInvoice = _DbContext.TaxInvoiceInfos
                    .FirstOrDefault(ti => ti.DocFeeRequisitionId == entity.DocFeeRequisitionId);

                if (existingInvoice != null)
                {
                    _Logger.LogWarning("尝试为申请单 {RequisitionId} 创建重复发票", entity.DocFeeRequisitionId);
                    result.HasError = true;
                    result.ErrorCode = (int)HttpStatusCode.Conflict;
                    result.DebugMessage = $"此申请单已存在发票记录(发票ID: {existingInvoice.Id})，不能重复开票";
                    return StatusCode((int)HttpStatusCode.Conflict, result);
                }
            }

            entity.GenerateNewId();
            //entry.CreateBy = context.User.Id;
            //entry.Entity.CreateDateTime = OwHelper.WorldNow;
            _DbContext.TaxInvoiceInfos.Add(model.TaxInvoiceInfo);
            var entry = _DbContext.Entry(entity);
            entry.Entity.State = 0;

            // 记录日志
            _SqlAppLogger.LogGeneralInfo($"创建发票信息.{nameof(TaxInvoiceInfo)}.{entity.Id}");

            _DbContext.SaveChanges();

            result.Id = model.TaxInvoiceInfo.Id;
            return result;
        }

        /// <summary>
        /// 修改税务发票信息。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">发票状态不是未审核状态，无法修改。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        /// <response code="404">指定Id的税务发票信息不存在。</response>  
        [HttpPut]
        public ActionResult<ModifyTaxInvoiceInfoReturnDto> ModifyTaxInvoiceInfo(ModifyTaxInvoiceInfoParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            //if (!_AuthorizationManager.Demand(out err, "F.3.2")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new ModifyTaxInvoiceInfoReturnDto();

            // 检查发票状态
            var existingInvoice = _DbContext.TaxInvoiceInfos.Find(model.TaxInvoiceInfo.Id);
            if (existingInvoice == null) return NotFound("指定ID的发票信息不存在");

            // 只有状态为0(未审核)的发票才能修改
            if (existingInvoice.State != 0)
            {
                result.HasError = true;
                result.ErrorCode = 400;
                result.DebugMessage = "只有未审核状态的发票才能修改";
                return BadRequest(result.DebugMessage);
            }

            if (!_EntityManager.Modify(new[] { model.TaxInvoiceInfo })) return NotFound();
            //忽略不可更改字段
            var entity = _DbContext.Entry(model.TaxInvoiceInfo);
            entity.Property(c => c.State).IsModified = false;
            _DbContext.SaveChanges();

            // 记录日志
            _SqlAppLogger.LogGeneralInfo($"修改发票信息.{nameof(TaxInvoiceInfo)}.{model.TaxInvoiceInfo.Id}");

            return result;
        }

        /// <summary>
        /// 删除指定Id的税务发票信息。这会删除所有税务发票信息明细项。慎用！
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">只有未审核状态的发票才能删除。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        /// <response code="404">指定Id的税务发票信息不存在。</response>  
        [HttpDelete]
        public ActionResult<RemoveTaxInvoiceInfoReturnDto> RemoveTaxInvoiceInfo(RemoveTaxInvoiceInfoParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemoveTaxInvoiceInfoReturnDto();
            string err;
            //if (!_AuthorizationManager.Demand(out err, "F.3.3")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var id = model.Id;
            var dbSet = _DbContext.TaxInvoiceInfos;
            var item = dbSet.Find(id);

            if (item is null) return NotFound("指定ID的发票信息不存在");

            // 只有状态为0(未审核)的发票才能删除
            if (item.State != 0)
            {
                result.HasError = true;
                result.ErrorCode = 400;
                result.DebugMessage = "只有未审核状态的发票才能删除";
                return BadRequest(result.DebugMessage);
            }

            var children = _DbContext.TaxInvoiceInfoItems.Where(c => c.ParentId == item.Id).ToArray();

            _EntityManager.Remove(item);
            if (children.Length > 0) _DbContext.RemoveRange(children);

            //记录日志
            _DbContext.OwSystemLogs.Add(new OwSystemLog
            {
                OrgId = context.User.OrgId,
                ActionId = $"Delete.{nameof(TaxInvoiceInfo)}.{item.Id}",
                ExtraGuid = context.User.Id,
                ExtraDecimal = children.Length,
                WorldDateTime = DateTime.Now
            });
            _DbContext.SaveChanges();

            return result;
        }

        /// <summary>
        /// 更改税务发票信息状态。当状态从0变更为1时，将记录审核人信息并调用诺诺开票接口。
        /// </summary>
        /// <param name="model">状态更改参数</param>
        /// <returns>状态更改结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode。</response>  
        /// <response code="400">状态不允许变更。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        /// <response code="404">税务发票信息不存在。</response>  
        [HttpPost]
        public ActionResult<ChangeStateOfTaxInvoiceInfoReturnDto> ChangeStateOfTaxInvoiceInfo(ChangeStateOfTaxInvoiceInfoParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ChangeStateOfTaxInvoiceInfoReturnDto();

            // 获取指定的发票信息
            var taxInvoiceInfo = _DbContext.TaxInvoiceInfos
                .FirstOrDefault(t => t.Id == model.Id);

            if (taxInvoiceInfo == null)
            {
                return NotFound("指定的税务发票信息不存在");
            }

            // 更新状态及相关字段
            var now = OwHelper.WorldNow;
            var oldState = taxInvoiceInfo.State;

            if (model.NewState == 1 && oldState == 0) // 从待审核状态变更为开票中状态
            {
                // 设置新状态
                taxInvoiceInfo.State = 1;

                // 记录审核人ID和审核时间
                taxInvoiceInfo.AuditorId = context.User.Id;
                taxInvoiceInfo.AuditDateTime = now;

                // 设置回调地址
                try
                {
                    // 获取HttpContext访问器
                    var httpContextAccessor = _ServiceProvider.GetRequiredService<IHttpContextAccessor>();
                    string baseUrl;

                    // 优先从当前请求上下文获取本网站的地址
                    if (httpContextAccessor.HttpContext != null)
                    {
                        var request = httpContextAccessor.HttpContext.Request;
                        baseUrl = $"{request.Scheme}://{request.Host}";
                        _Logger.LogInformation($"从当前HTTP上下文获取基础URL：{baseUrl}");
                    }
                    else
                    {
                        // 如果不在HTTP上下文中执行，则使用配置值
                        var configuration = _ServiceProvider.GetService<IConfiguration>();
                        baseUrl = configuration?.GetValue<string>("AppSettings:CallbackBaseUrl");

                        if (string.IsNullOrEmpty(baseUrl))
                        {
                            baseUrl = "https://api.example.com"; // 默认值
                            _Logger.LogWarning($"无法获取HTTP上下文，且未配置CallbackBaseUrl，使用默认值：{baseUrl}");
                        }
                        else
                        {
                            _Logger.LogInformation($"使用配置的CallbackBaseUrl：{baseUrl}");
                        }
                    }

                    // 设置回调地址
                    taxInvoiceInfo.CallbackUrl = $"{baseUrl.TrimEnd('/')}/api/NuoNuoCallback/HandleCallback";
                    _Logger.LogInformation($"设置发票回调地址：{taxInvoiceInfo.CallbackUrl}");
                }
                catch (Exception ex)
                {
                    _Logger.LogWarning(ex, "设置回调地址失败，使用默认地址");
                    // 设置一个默认的回调地址，防止开票失败
                    taxInvoiceInfo.CallbackUrl = "https://api.example.com/api/NuoNuoCallback/HandleCallback";
                }

                // 记录系统日志
                _SqlAppLogger.LogGeneralInfo($"变更发票状态.{nameof(TaxInvoiceInfo)}.{oldState}To{model.NewState}");

                // 保存状态变更
                _DbContext.SaveChanges();

                // 判断是否需要调用诺诺开票接口
                if (taxInvoiceInfo.TaxInvoiceChannelAccountlId == null ||
                    _DbContext.TaxInvoiceChannelAccounts.Find(taxInvoiceInfo.TaxInvoiceChannelAccountlId) is not TaxInvoiceChannelAccount tica ||
                    tica.ParentlId is null ||
                    _DbContext.TaxInvoiceChannels.Find(tica.ParentlId) is not TaxInvoiceChannel tic ||
                    tic.Id != typeof(NuoNuoManager).GUID)
                {
                    _Logger.LogInformation($"发票信息 {taxInvoiceInfo.Id} 没有指定诺诺渠道账号，无法调用开票接口");
                    return result;
                }
                // 在状态成功变更为审核通过(1)后，调用诺诺开票接口
                try
                {
                    // 尝试获取NuoNuoManager服务
                    var nuoNuoManager = _ServiceProvider.GetService<NuoNuoManager>();
                    if (nuoNuoManager != null)
                    {
                        // 记录是否使用沙箱模式
                        if (model.UseSandbox)
                        {
                            _Logger.LogInformation($"将使用沙箱模式开具发票，发票ID: {taxInvoiceInfo.Id}");
                        }

                        // 处理开票请求
                        try
                        {
                            _Logger.LogInformation($"开始调用诺诺开票接口，发票ID: {taxInvoiceInfo.Id}, 沙箱模式: {model.UseSandbox}, 回调地址: {taxInvoiceInfo.CallbackUrl}");

                            // 调用开票接口，传入沙箱模式参数
                            var issueResult = nuoNuoManager.IssueInvoice(taxInvoiceInfo.Id, model.UseSandbox);

                            if (issueResult.Success)
                            {
                                _Logger.LogInformation($"调用诺诺开票接口成功，发票ID: {taxInvoiceInfo.Id}");
                            }
                            else
                            {
                                _Logger.LogWarning($"调用诺诺开票接口失败，发票ID: {taxInvoiceInfo.Id}, 错误: {issueResult.ErrorMessage}, 错误代码: {issueResult.ErrorCode}");

                                // 如果开票失败，直接在当前上下文中更新错误信息
                                // 重新获取最新的发票信息
                                var invoice = _DbContext.TaxInvoiceInfos.Find(taxInvoiceInfo.Id);
                                if (invoice != null)
                                {
                                    // 记录错误信息
                                    invoice.SellerInvoiceData = $"{{\"errorCode\": \"{issueResult.ErrorCode}\", \"errorMessage\": \"{issueResult.ErrorMessage}\", \"sandbox\": {model.UseSandbox.ToString().ToLower()}}}";
                                    _DbContext.SaveChanges();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _Logger.LogError(ex, $"调用诺诺开票接口时发生异常，发票ID: {taxInvoiceInfo.Id}");

                            // 记录异常信息，直接在当前上下文中更新
                            // 重新获取最新的发票信息
                            var invoice = _DbContext.TaxInvoiceInfos.Find(taxInvoiceInfo.Id);
                            if (invoice != null)
                            {
                                invoice.SellerInvoiceData = $"{{\"errorMessage\": \"系统异常: {ex.Message.Replace("\"", "\\\"")}\", \"sandbox\": {model.UseSandbox.ToString().ToLower()}}}";
                                _DbContext.SaveChanges();
                            }
                        }
                    }
                    else
                    {
                        _Logger.LogWarning($"未能找到NuoNuoManager服务，无法调用开票接口，发票ID: {taxInvoiceInfo.Id}");
                    }
                }
                catch (Exception ex)
                {
                    _Logger.LogError(ex, $"尝试调用诺诺开票接口时发生异常，发票ID: {taxInvoiceInfo.Id}");
                }
            }
            else if (model.NewState == 2 && oldState == 1) // 从开票中状态变更为已开票状态
            {
                // 设置新状态
                taxInvoiceInfo.State = 2;

                // 记录返回发票号的时间
                taxInvoiceInfo.ReturnInvoiceTime = now;

                // 记录系统日志
                _SqlAppLogger.LogGeneralInfo($"变更发票状态.{nameof(TaxInvoiceInfo)}.{oldState}To{model.NewState}");

                // 保存状态变更
                _DbContext.SaveChanges();
            }
            else
            {
                return BadRequest("状态不允许变更");
            }

            result.Id = taxInvoiceInfo.Id;
            result.NewState = taxInvoiceInfo.State;
            return result;
        }

        #endregion 税务发票信息

        #region 税务发票信息明细

        /// <summary>
        /// 获取全部税务发票信息明细。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">查询的条件。
        /// 通用条件写法:所有条件都是字符串，对区间的写法是用逗号分隔（字符串类型暂时不支持区间且都是模糊查询）如"2024-1-1,2024-1-2"。
        /// 对强制取null的约束，则写"null"。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllTaxInvoiceInfoItemReturnDto> GetAllTaxInvoiceInfoItem([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllTaxInvoiceInfoItemReturnDto();
            var dbSet = _DbContext.TaxInvoiceInfoItems;

            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            coll = EfHelper.GenerateWhereAnd(coll, conditional);
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 增加新税务发票信息明细。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPost]
        public ActionResult<AddTaxInvoiceInfoItemReturnDto> AddTaxInvoiceInfoItem(AddTaxInvoiceInfoItemParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
            {
                _Logger.LogWarning("无效的令牌{token}", model.Token);
                return Unauthorized();
            }
            string err;
            //if (!_AuthorizationManager.Demand(out err, "F.3.1") && !_AuthorizationManager.Demand(out err, "F.3.2")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new AddTaxInvoiceInfoItemReturnDto();
            var entity = model.TaxInvoiceInfoItem;
            entity.GenerateNewId();
            _DbContext.TaxInvoiceInfoItems.Add(model.TaxInvoiceInfoItem);
            _DbContext.SaveChanges();

            result.Id = model.TaxInvoiceInfoItem.Id;
            return result;
        }

        /// <summary>
        /// 修改税务发票信息明细信息。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">关联的发票不是未审核状态，无法修改明细。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        /// <response code="404">指定Id的税务发票信息明细不存在或关联的发票不存在。</response>  
        [HttpPut]
        public ActionResult<ModifyTaxInvoiceInfoItemReturnDto> ModifyTaxInvoiceInfoItem(ModifyTaxInvoiceInfoItemParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyTaxInvoiceInfoItemReturnDto();
            string err;
            //if (!_AuthorizationManager.Demand(out err, "F.3.2")) return StatusCode((int)HttpStatusCode.Forbidden, err);

            // 获取明细记录
            var item = _DbContext.TaxInvoiceInfoItems.Find(model.TaxInvoiceInfoItem.Id);
            if (item == null) return NotFound("指定ID的发票明细信息不存在");

            // 获取关联的发票
            var parentInvoice = _DbContext.TaxInvoiceInfos.Find(item.ParentId);
            if (parentInvoice == null) return NotFound("关联的发票信息不存在");

            // 检查发票状态
            if (parentInvoice.State != 0)
            {
                result.HasError = true;
                result.ErrorCode = 400;
                result.DebugMessage = "只有未审核状态的发票才能修改其明细";
                return BadRequest(result.DebugMessage);
            }

            if (!_EntityManager.Modify(new[] { model.TaxInvoiceInfoItem })) return NotFound();
            _DbContext.SaveChanges();

            // 记录日志
            _SqlAppLogger.LogGeneralInfo($"修改发票明细.{nameof(TaxInvoiceInfoItem)}.{model.TaxInvoiceInfoItem.Id}");

            return result;
        }

        /// <summary>
        /// 删除指定Id的税务发票信息明细。慎用！
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">关联的发票不是未审核状态，无法删除明细。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        /// <response code="404">指定Id的税务发票信息明细不存在或关联的发票不存在。</response>  
        [HttpDelete]
        public ActionResult<RemoveTaxInvoiceInfoItemReturnDto> RemoveTaxInvoiceInfoItem(RemoveTaxInvoiceInfoItemParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            //if (!_AuthorizationManager.Demand(out err, "F.3.2") && !_AuthorizationManager.Demand(out err, "F.3.3")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new RemoveTaxInvoiceInfoItemReturnDto();

            // 获取明细记录
            var item = _DbContext.TaxInvoiceInfoItems.Find(model.Id);
            if (item is null) return NotFound("指定ID的发票明细信息不存在");

            // 获取关联的发票
            var parentInvoice = _DbContext.TaxInvoiceInfos.Find(item.ParentId);
            if (parentInvoice == null) return NotFound("关联的发票信息不存在");

            // 检查发票状态
            if (parentInvoice.State != 0)
            {
                result.HasError = true;
                result.ErrorCode = 400;
                result.DebugMessage = "只有未审核状态的发票才能删除其明细";
                return BadRequest(result.DebugMessage);
            }

            _EntityManager.Remove(item);

            // 记录日志
            _SqlAppLogger.LogGeneralInfo($"删除发票明细.{nameof(TaxInvoiceInfoItem)}.{item.Id}");

            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 设置指定的税务发票信息下所有明细。
        /// 指定存在id的明细则更新，Id全0或不存在的Id自动添加，原有未指定的明细将被删除。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">发票不是未审核状态，无法修改明细。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的税务发票信息不存在。</response>  
        [HttpPut]
        public ActionResult<SetTaxInvoiceInfoItemReturnDto> SetTaxInvoiceInfoItem(SetTaxInvoiceInfoItemParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new SetTaxInvoiceInfoItemReturnDto();

            // 获取发票信息
            var taxInvoiceInfo = _DbContext.TaxInvoiceInfos.Find(model.TaxInvoiceInfoId);
            if (taxInvoiceInfo is null) return NotFound("指定ID的发票信息不存在");

            // 检查发票状态
            if (taxInvoiceInfo.State != 0)
            {
                result.HasError = true;
                result.ErrorCode = 400;
                result.DebugMessage = "只有未审核状态的发票才能设置其明细";
                return BadRequest(result.DebugMessage);
            }

            var aryIds = model.Items.Select(c => c.Id).ToArray();   //指定的Id
            var existsIds = _DbContext.TaxInvoiceInfoItems.Where(c => c.ParentId == taxInvoiceInfo.Id).Select(c => c.Id).ToArray();    //已经存在的Id
                                                                                                                                       //更改
            var modifies = model.Items.Where(c => existsIds.Contains(c.Id));
            foreach (var item in modifies)
            {
                _DbContext.Entry(item).CurrentValues.SetValues(item);
                _DbContext.Entry(item).State = EntityState.Modified;
            }
            //增加
            var addIds = aryIds.Except(existsIds).ToArray();
            var adds = model.Items.Where(c => addIds.Contains(c.Id)).ToArray();
            Array.ForEach(adds, c =>
            {
                c.GenerateNewId();
                c.ParentId = taxInvoiceInfo.Id; // 确保设置父ID
            });
            _DbContext.AddRange(adds);
            //删除
            var removeIds = existsIds.Except(aryIds).ToArray();
            _DbContext.RemoveRange(_DbContext.TaxInvoiceInfoItems.Where(c => removeIds.Contains(c.Id)));

            // 记录日志
            _SqlAppLogger.LogGeneralInfo($"设置发票明细.{nameof(TaxInvoiceInfo)}.{taxInvoiceInfo.Id}");

            _DbContext.SaveChanges();
            //后处理
            result.Result.AddRange(model.Items);
            return result;
        }
        #endregion 税务发票信息明细

        #endregion 发票相关
    }
}
