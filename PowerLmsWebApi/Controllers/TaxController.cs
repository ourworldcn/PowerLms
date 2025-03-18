
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
        [HttpGet]
        public ActionResult<GetAllOrgTaxChannelAccountReturnDto> GetAllOrgTaxChannelAccount([FromQuery] PagingParamsDtoBase model, [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized(); // 验证令牌
            var result = new GetAllOrgTaxChannelAccountReturnDto();
            var dbSet = _DbContext.OrgTaxChannelAccounts.Where(c => c.OrgId == context.User.OrgId); // 仅查询当前用户机构的数据
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking(); // 排序并禁用实体追踪
            coll = EfHelper.GenerateWhereAnd(coll, conditional); // 应用查询条件
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count); // 获取分页数据
            _Mapper.Map(prb, result); // 映射到返回对象
            return result;
        }

        /// <summary>新增机构渠道账号，自动设置创建信息，支持设置默认账号。</summary>
        /// <param name="model">包含新账号信息的参数对象</param>
        /// <returns>添加结果</returns>
        [HttpPost]
        public ActionResult<AddOrgTaxChannelAccountReturnDto> AddOrgTaxChannelAccount(AddOrgTaxChannelAccountParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized(); // 验证令牌
            var result = new AddOrgTaxChannelAccountReturnDto();
            var entity = model.Item;
            entity.OrgId = context.User.OrgId.GetValueOrDefault(); // 设置机构Id
            entity.CreateDateTime = OwHelper.WorldNow; // 设置创建时间
            entity.CreateBy = context.User.Id; // 设置创建者
            if (entity.IsDefault) // 如果设为默认账号
            {
                var defaultAccounts = _DbContext.OrgTaxChannelAccounts.Where(c => c.OrgId == context.User.OrgId && c.IsDefault).ToList();
                foreach (var acc in defaultAccounts) acc.IsDefault = false; // 取消其他默认账号
            }
            _DbContext.OrgTaxChannelAccounts.Add(entity); // 添加新账号
            _DbContext.SaveChanges(); // 保存更改
            return result;
        }

        /// <summary>修改机构渠道账号信息，支持批量修改，自动维护默认账号状态。</summary>
        /// <param name="model">包含要修改账号信息的参数对象</param>
        /// <returns>修改结果</returns>
        [HttpPut]
        public ActionResult<ModifyOrgTaxChannelAccountReturnDto> ModifyOrgTaxChannelAccount(ModifyOrgTaxChannelAccountParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized(); // 验证令牌
            var result = new ModifyOrgTaxChannelAccountReturnDto();
            if (model.Items?.Count == 0) { result.HasError = true; return result; } // 验证输入
            int modifiedCount = 0;
            foreach (var item in model.Items)
            {
                item.OrgId = context.User.OrgId.GetValueOrDefault(); // 确保只能修改当前用户机构的数据
                var existingAccount = _DbContext.OrgTaxChannelAccounts
                    .Where(a => a.OrgId == context.User.OrgId && a.ChannelAccountId == item.ChannelAccountId)
                    .FirstOrDefault();
                if (existingAccount == null) continue; // 跳过不存在的账号
                var channelAccountId = existingAccount.ChannelAccountId;
                _DbContext.Entry(existingAccount).CurrentValues.SetValues(item); // 更新实体
                item.LastModifyUtc = OwHelper.WorldNow; // 设置修改时间
                item.LastModifyBy = context.User.Id; // 设置修改者
                if (item.IsDefault && !existingAccount.IsDefault) // 如果设为默认账号
                {
                    var defaultAccounts = _DbContext.OrgTaxChannelAccounts
                        .Where(c => c.OrgId == context.User.OrgId && c.IsDefault && c.ChannelAccountId != item.ChannelAccountId).ToList();
                    foreach (var acc in defaultAccounts) acc.IsDefault = false; // 取消其他默认账号
                }
                _SqlAppLogger.LogGeneralInfo($"Modify.{nameof(OrgTaxChannelAccount)}"); // 记录系统日志
                modifiedCount++;
            }
            if (modifiedCount == 0) { result.HasError = true; return NotFound(); } // 未找到要修改的账号
            _DbContext.SaveChanges(); // 保存更改
            return result;
        }

        /// <summary>删除指定的机构渠道账号。</summary>
        /// <param name="model">包含要删除账号ID的参数对象</param>
        /// <returns>删除结果</returns>
        [HttpDelete]
        public ActionResult<RemoveOrgTaxChannelAccountReturnDto> RemoveOrgTaxChannelAccount(RemoveOrgTaxChannelAccountParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized(); // 验证令牌
            var result = new RemoveOrgTaxChannelAccountReturnDto();
            var orgId = model.OrgId ?? context.User.OrgId.GetValueOrDefault(); // 获取要操作的机构Id
            var account = _DbContext.OrgTaxChannelAccounts.Where(a => a.OrgId == orgId && a.ChannelAccountId == model.ChannelAccountId).FirstOrDefault();
            if (account == null) return NotFound(); // 账号不存在
            _DbContext.OrgTaxChannelAccounts.Remove(account); // 删除账号
            _SqlAppLogger.LogGeneralInfo($"删除渠道账号 {nameof(OrgTaxChannelAccount)}.{account.ChannelAccountId}"); // 记录日志
            _DbContext.SaveChanges(); // 保存更改
            return result;
        }

        /// <summary>设置指定的机构渠道账号为默认账号，同时取消其他账号的默认状态。</summary>
        /// <param name="model">包含要设为默认的账号ID的参数对象</param>
        /// <returns>设置结果</returns>
        [HttpPost]
        public ActionResult<SetDefaultOrgTaxChannelAccountReturnDto> SetDefaultOrgTaxChannelAccount(SetDefaultOrgTaxChannelAccountParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized(); // 验证令牌
            var result = new SetDefaultOrgTaxChannelAccountReturnDto();
            var account = _DbContext.OrgTaxChannelAccounts.Where(a => a.OrgId == context.User.OrgId && a.ChannelAccountId == model.ChannelAccountId).FirstOrDefault();
            if (account == null) return NotFound(); // 账号不存在
            var allAccounts = _DbContext.OrgTaxChannelAccounts.Where(a => a.OrgId == context.User.OrgId).ToList(); // 获取所有账号
            foreach (var acc in allAccounts) acc.IsDefault = (acc.ChannelAccountId == model.ChannelAccountId); // 设置默认状态
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
        /// 对强制取null的约束，则写"null"。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllTaxInvoiceInfoReturnDto> GetAllTaxInvoiceInfo([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllTaxInvoiceInfoReturnDto();
            var dbSet = _DbContext.TaxInvoiceInfos;

            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            coll = EfHelper.GenerateWhereAnd(coll, conditional);
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
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
            entity.GenerateNewId();
            //entry.CreateBy = context.User.Id;
            //entry.Entity.CreateDateTime = OwHelper.WorldNow;
            _DbContext.TaxInvoiceInfos.Add(model.TaxInvoiceInfo);
            var entry = _DbContext.Entry(entity);
            entry.Entity.State = 0;

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
            if (!_EntityManager.Modify(new[] { model.TaxInvoiceInfo })) return NotFound();
            //忽略不可更改字段
            var entity = _DbContext.Entry(model.TaxInvoiceInfo);
            entity.Property(c => c.State).IsModified = false;
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 删除指定Id的税务发票信息。这会删除所有税务发票信息明细项。慎用！
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">若已经审核，则不能删除发票文件。</response>  
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
            if (item is null) return BadRequest();
            var children = _DbContext.TaxInvoiceInfoItems.Where(c => c.ParentId == item.Id).ToArray();

            _EntityManager.Remove(item);
            if (children.Length > 0) _DbContext.RemoveRange(children);
            //若已经审核，则不能删除发票文件
            if (item.State > 0)
            {
                return BadRequest("已经审核的发票不能删除！");
            }
            //记录日志
            _DbContext.OwSystemLogs.Add(new OwSystemLog
            {
                OrgId = context.User.OrgId,
                ActionId = $"Delete.{nameof(TaxInvoiceInfo)}.{item.Id}",
                ExtraGuid = context.User.Id,
                ExtraDecimal = children.Length,
            });
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 更改税务发票信息状态。目前仅支持从0切换到1。
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

            // 检查权限
            //string err;
            //if (!_AuthorizationManager.Demand(out err, "F.4")) return StatusCode((int)HttpStatusCode.Forbidden, err);

            // 获取指定的发票信息
            var taxInvoiceInfo = _DbContext.TaxInvoiceInfos.Find(model.Id);
            if (taxInvoiceInfo == null)
            {
                return NotFound("指定的税务发票信息不存在");
            }

            // 更新状态及相关字段
            var now = OwHelper.WorldNow;
            var oldState = taxInvoiceInfo.State;
            taxInvoiceInfo.State = model.NewState;

            switch (model.NewState)
            {
                case 1 when taxInvoiceInfo.State == 0: // 待审核
                                                       // 从驳回状态重新提交审核
                    taxInvoiceInfo.State = 1;
                    break;
                default:
                    return BadRequest("状态不允许变更");
            }

            // 记录系统日志
            _SqlAppLogger.LogGeneralInfo($"变更发票状态.{nameof(TaxInvoiceInfo)}.{oldState}To{model.NewState}");

            _DbContext.SaveChanges();

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
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        /// <response code="404">指定Id的税务发票信息明细不存在。</response>  
        [HttpPut]
        public ActionResult<ModifyTaxInvoiceInfoItemReturnDto> ModifyTaxInvoiceInfoItem(ModifyTaxInvoiceInfoItemParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyTaxInvoiceInfoItemReturnDto();
            string err;
            //if (!_AuthorizationManager.Demand(out err, "F.3.2")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            if (!_EntityManager.Modify(new[] { model.TaxInvoiceInfoItem })) return NotFound();
            //忽略不可更改字段
            var entity = _DbContext.Entry(model.TaxInvoiceInfoItem);
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 删除指定Id的税务发票信息明细。慎用！
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        /// <response code="404">指定Id的税务发票信息明细不存在。</response>  
        [HttpDelete]
        public ActionResult<RemoveTaxInvoiceInfoItemReturnDto> RemoveTaxInvoiceInfoItem(RemoveTaxInvoiceInfoItemParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            //if (!_AuthorizationManager.Demand(out err, "F.3.2") && !_AuthorizationManager.Demand(out err, "F.3.3")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new RemoveTaxInvoiceInfoItemReturnDto();
            var id = model.Id;
            var dbSet = _DbContext.TaxInvoiceInfoItems;
            var item = dbSet.Find(id);
            if (item is null) return BadRequest();
            _EntityManager.Remove(item);
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
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的税务发票信息不存在。</response>  
        [HttpPut]
        public ActionResult<SetTaxInvoiceInfoItemReturnDto> SetTaxInvoiceInfoItem(SetTaxInvoiceInfoItemParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new SetTaxInvoiceInfoItemReturnDto();
            var taxInvoiceInfo = _DbContext.TaxInvoiceInfos.Find(model.TaxInvoiceInfoId);
            if (taxInvoiceInfo is null) return NotFound();

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
            Array.ForEach(adds, c => c.GenerateNewId());
            _DbContext.AddRange(adds);
            //删除
            var removeIds = existsIds.Except(aryIds).ToArray();
            _DbContext.RemoveRange(_DbContext.TaxInvoiceInfoItems.Where(c => removeIds.Contains(c.Id)));

            _DbContext.SaveChanges();
            //后处理
            result.Result.AddRange(model.Items);
            return result;
        }
        #endregion 税务发票信息明细

        #endregion 发票相关
    }
}
