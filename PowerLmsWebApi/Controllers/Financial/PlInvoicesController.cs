using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OW.Data;
using PowerLms.Data;
using PowerLmsServer;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsServer.Helpers;
using PowerLmsWebApi.Dto;
using System.Net;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// 结算单相关功能控制器。
    /// 包含结算单(PlInvoices)和结算单明细(PlInvoicesItem)的完整CRUD操作。
    /// </summary>
    public partial class PlInvoicesController : PlControllerBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public PlInvoicesController(AccountManager accountManager, IServiceProvider serviceProvider, EntityManager entityManager,
            PowerLmsUserDbContext dbContext, ILogger<PlInvoicesController> logger, IMapper mapper, OwWfManager wfManager, 
            AuthorizationManager authorizationManager, OwSqlAppLogger sqlAppLogger)
        {
            _AccountManager = accountManager;
            _ServiceProvider = serviceProvider;
            _EntityManager = entityManager;
            _DbContext = dbContext;
            _Logger = logger;
            _Mapper = mapper;
            _WfManager = wfManager;
            _AuthorizationManager = authorizationManager;
            _SqlAppLogger = sqlAppLogger;
        }

        readonly AccountManager _AccountManager;
        readonly IServiceProvider _ServiceProvider;
        readonly EntityManager _EntityManager;
        readonly PowerLmsUserDbContext _DbContext;
        readonly ILogger<PlInvoicesController> _Logger;
        readonly IMapper _Mapper;
        readonly OwWfManager _WfManager;
        readonly AuthorizationManager _AuthorizationManager;
        readonly OwSqlAppLogger _SqlAppLogger;

        #region 结算单

        /// <summary>
        /// 获取全部结算单。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">查询条件字典。
        /// 通用条件写法:所有条件都是字符串，对区间的写法是用逗号分隔（字符串类型暂时不支持区间且都是模糊查询）如"2024-1-1,2024-1-2"。
        /// 对强制取null的约束，则写"null"。
        /// 支持的跨表查询条件:
        /// - DocFeeRequisition.属性名: 按申请单表过滤
        /// - DocFeeRequisitionItem.属性名: 按申请单明细表过滤
        /// - DocFee.属性名: 按费用表过滤
        /// - PlJob.属性名: 按工作号表过滤
        /// 示例: 字典中添加 键="PlJob.JobNo" 值="XXX" 或 键="DocFee.FeeTypeId" 值="guid值"</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllPlInvoicesReturnDto> GetAllPlInvoices([FromQuery] PagingParamsDtoBase model,
            [FromQuery][ModelBinder(typeof(DotKeyDictionaryModelBinder))] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllPlInvoicesReturnDto();
            try
            {
                conditional = conditional ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                string docFeeRequisitionPrefix = $"{nameof(DocFeeRequisition)}.";
                string docFeeRequisitionItemPrefix = $"{nameof(DocFeeRequisitionItem)}.";
                string docFeePrefix = $"{nameof(DocFee)}.";
                string plJobPrefix = $"{nameof(PlJob)}.";
                var reqConditions = conditional
                    .Where(pair => pair.Key.StartsWith(docFeeRequisitionPrefix, StringComparison.OrdinalIgnoreCase))
                    .ToDictionary(
                        pair => pair.Key[docFeeRequisitionPrefix.Length..],
                        pair => pair.Value,
                        StringComparer.OrdinalIgnoreCase
                    );
                var reqItemConditions = conditional
                    .Where(pair => pair.Key.StartsWith(docFeeRequisitionItemPrefix, StringComparison.OrdinalIgnoreCase))
                    .ToDictionary(
                        pair => pair.Key[docFeeRequisitionItemPrefix.Length..],
                        pair => pair.Value,
                        StringComparer.OrdinalIgnoreCase
                    );
                var feeConditions = conditional
                    .Where(pair => pair.Key.StartsWith(docFeePrefix, StringComparison.OrdinalIgnoreCase))
                    .ToDictionary(
                        pair => pair.Key[docFeePrefix.Length..],
                        pair => pair.Value,
                        StringComparer.OrdinalIgnoreCase
                    );
                var jobConditions = conditional
                    .Where(pair => pair.Key.StartsWith(plJobPrefix, StringComparison.OrdinalIgnoreCase))
                    .ToDictionary(
                        pair => pair.Key[plJobPrefix.Length..],
                        pair => pair.Value,
                        StringComparer.OrdinalIgnoreCase
                    );
                var invoiceConditions = conditional
                    .Where(pair => !pair.Key.Contains('.'))
                    .ToDictionary(
                        pair => pair.Key,
                        pair => pair.Value,
                        StringComparer.OrdinalIgnoreCase
                    );
                var coll = _DbContext.PlInvoicess.AsNoTracking();
                coll = QueryHelper.GenerateWhereAnd(coll, invoiceConditions);
                bool needJoin = reqConditions.Count > 0 || reqItemConditions.Count > 0 || 
                                feeConditions.Count > 0 || jobConditions.Count > 0;
                if (needJoin)
                {
                    var collRequisitions = reqConditions.Count > 0
                        ? QueryHelper.GenerateWhereAnd(_DbContext.DocFeeRequisitions.AsNoTracking(), reqConditions)
                        : _DbContext.DocFeeRequisitions.AsNoTracking();
                    var collRequisitionItems = reqItemConditions.Count > 0
                        ? QueryHelper.GenerateWhereAnd(_DbContext.DocFeeRequisitionItems.AsNoTracking(), reqItemConditions)
                        : _DbContext.DocFeeRequisitionItems.AsNoTracking();
                    var collFees = feeConditions.Count > 0
                        ? QueryHelper.GenerateWhereAnd(_DbContext.DocFees.AsNoTracking(), feeConditions)
                        : _DbContext.DocFees.AsNoTracking();
                    var collJobs = jobConditions.Count > 0
                        ? QueryHelper.GenerateWhereAnd(_DbContext.PlJobs.AsNoTracking(), jobConditions)
                        : _DbContext.PlJobs.AsNoTracking();
                    coll = (from invoice in coll
                            join item in _DbContext.PlInvoicesItems on invoice.Id equals item.ParentId
                            join reqItem in collRequisitionItems on item.RequisitionItemId equals reqItem.Id
                            join req in collRequisitions on reqItem.ParentId equals req.Id
                            join fee in collFees on reqItem.FeeId equals fee.Id
                            join job in collJobs on fee.JobId equals job.Id
                            select invoice).Distinct();
                }
                coll = coll.OrderBy(model.OrderFieldName, model.IsDesc);
                var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
                _Mapper.Map(prb, result);
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "获取结算单时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"获取结算单时发生错误: {ex.Message}";
            }
            return result;
        }

        /// <summary>
        /// 增加新结算单。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPost]
        public ActionResult<AddPlInvoiceReturnDto> AddPlInvoice(AddPlInvoiceParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
            {
                _Logger.LogWarning("添加结算单时提供了无效的令牌: {token}", model.Token);
                return Unauthorized();
            }
            var result = new AddPlInvoiceReturnDto();
            try
            {
                if (!_AuthorizationManager.Demand(out string err, "F.3.1"))
                    return StatusCode((int)HttpStatusCode.Forbidden, err);
                if (model.PlInvoices == null)
                {
                    result.HasError = true;
                    result.ErrorCode = 400;
                    result.DebugMessage = "结算单数据不能为空";
                    return result;
                }
                var entity = model.PlInvoices;
                entity.GenerateIdIfEmpty();
                entity.CreateBy = context.User.Id;
                entity.CreateDateTime = OwHelper.WorldNow;
                _DbContext.PlInvoicess.Add(entity);
                _SqlAppLogger.LogGeneralInfo($"用户 {context.User.Id} 创建了结算单ID:{entity.Id}，操作：AddPlInvoices");
                _DbContext.SaveChanges();
                result.Id = entity.Id;
                result.HasError = false;
                _Logger.LogDebug("成功创建结算单: {id}", entity.Id);
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "创建结算单时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"创建结算单时发生错误: {ex.Message}";
            }
            return result;
        }

        /// <summary>
        /// 修改结算单信息。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        /// <response code="404">指定Id的结算单不存在。</response>  
        [HttpPut]
        public ActionResult<ModifyPlInvoicesReturnDto> ModifyPlInvoices(ModifyPlInvoicesParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            if (!_AuthorizationManager.Demand(out string err, "F.3.2")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new ModifyPlInvoicesReturnDto();
            var modifiedEntities = new List<PlInvoices>();
            if (!_EntityManager.Modify(new[] { model.PlInvoices }, modifiedEntities)) return NotFound();
            var entity = _DbContext.Entry(modifiedEntities[0]);
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 删除指定Id的结算单。这会删除所有结算单明细项。慎用！
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        /// <response code="404">指定Id的结算单不存在。</response>  
        [HttpDelete]
        public ActionResult<RemovePlInvoicesReturnDto> RemovePlInvoices(RemovePlInvoicesParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemovePlInvoicesReturnDto();
            if (!_AuthorizationManager.Demand(out string err, "F.3.3")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var id = model.Id;
            if (_DbContext.PlInvoicess.Find(id) is not PlInvoices item) return BadRequest();
            var children = _DbContext.PlInvoicesItems.Where(c => c.ParentId == item.Id).ToArray();
            _EntityManager.Remove(item);
            if (children.Length > 0) _DbContext.RemoveRange(children);
            _DbContext.OwSystemLogs.Add(new OwSystemLog
            {
                OrgId = context.User.OrgId,
                ActionId = $"Delete.{nameof(PlInvoices)}.{item.Id}",
                ExtraGuid = context.User.Id,
                ExtraDecimal = children.Length,
            });
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 结算单确认。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">至少有一个结算单已被确认或至少有一个结算单是自己创建的。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        /// <response code="404">指定Id的结算单不存在。</response>  
        [HttpPost]
        public ActionResult<ConfirmPlInvoicesReturnDto> ConfirmPlInvoices(ConfirmPlInvoicesParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ConfirmPlInvoicesReturnDto();
            if (!_AuthorizationManager.Demand(out string err, "F.3.4")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var coll = _DbContext.PlInvoicess.Where(c => model.Ids.Contains(c.Id)).ToArray();
            if (coll.Length != model.Ids.Count) return BadRequest("至少有一个id不存在对应的结算单");
            var now = OwHelper.WorldNow;
            foreach (var invoice in coll)
            {
                if (model.IsConfirm)
                {
                    invoice.ConfirmDateTime = now;
                    invoice.ConfirmId = context.User.Id;
                }
                else
                {
                    invoice.ConfirmDateTime = null;
                    invoice.ConfirmId = null;
                }
            }
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 获取结算单明细增强接口功能。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">查询条件字典,键格式: 实体名.字段名。
        /// 支持的实体名: PlInvoicesItem(本体), PlInvoices(结算单), DocFeeRequisitionItem(申请明细), DocFeeRequisition(申请单), DocFee(费用), PlJob(工作号)
        /// 示例: 字典中添加 键="PlJob.JobNo" 值="XXX" 或 键="DocFee.FeeTypeId" 值="guid值"</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>
        [HttpGet]
        [Obsolete("此接口已废弃,请使用GetAllPlInvoicesItem替代")]
        public ActionResult<GetPlInvoicesItemReturnDto> GetDocInvoicesItem([FromQuery] GetPlInvoicesItemParamsDto model,
            [FromQuery][ModelBinder(typeof(DotKeyDictionaryModelBinder))] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetPlInvoicesItemReturnDto();
            conditional = conditional ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var coll = _DbContext.PlInvoicesItems.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking(); // 初始化查询集合
            var invoiceJoined = false; // 结算单总单连接标记
            var requisitionItemJoined = false; // 申请明细连接标记
            var requisitionJoined = false; // 申请单连接标记
            var feeJoined = false; // 费用连接标记
            var jobJoined = false; // 工作号连接标记
            var itemConditions = conditional.Where(c => c.Key.StartsWith($"{nameof(PlInvoicesItem)}.", StringComparison.OrdinalIgnoreCase) || !c.Key.Contains('.')) // 提取本体条件
                .ToDictionary(
                    pair => pair.Key.Contains('.') ? pair.Key[($"{nameof(PlInvoicesItem)}.".Length)..] : pair.Key,
                    pair => pair.Value,
                    StringComparer.OrdinalIgnoreCase
                );
            coll = QueryHelper.GenerateWhereAnd(coll, itemConditions); // 应用本体条件过滤
            var hasInvoiceConditions = conditional.Any(c => c.Key.StartsWith($"{nameof(PlInvoices)}.", StringComparison.OrdinalIgnoreCase)); // 检查是否有结算单总单条件
            IQueryable<PlInvoices> collInvoices = null;
            if (hasInvoiceConditions)
            {
                var invoiceConditions = conditional // 提取结算单总单条件
                    .Where(c => c.Key.StartsWith($"{nameof(PlInvoices)}.", StringComparison.OrdinalIgnoreCase))
                    .ToDictionary(
                        pair => pair.Key[($"{nameof(PlInvoices)}.".Length)..],
                        pair => pair.Value,
                        StringComparer.OrdinalIgnoreCase
                    );
                collInvoices = QueryHelper.GenerateWhereAnd(_DbContext.PlInvoicess.AsNoTracking(), invoiceConditions); // 应用结算单条件
                coll = from item in coll
                       join invoice in collInvoices on item.ParentId equals invoice.Id
                       select item; // 连接结算单总单
                invoiceJoined = true;
            }
            var hasRequisitionItemConditions = conditional.Any(c => c.Key.StartsWith($"{nameof(DocFeeRequisitionItem)}.", StringComparison.OrdinalIgnoreCase)); // 检查是否有申请明细条件
            IQueryable<DocFeeRequisitionItem> collRequisitionItems = null;
            if (hasRequisitionItemConditions)
            {
                var requisitionItemConditions = conditional // 提取申请明细条件
                    .Where(c => c.Key.StartsWith($"{nameof(DocFeeRequisitionItem)}.", StringComparison.OrdinalIgnoreCase))
                    .ToDictionary(
                        pair => pair.Key[($"{nameof(DocFeeRequisitionItem)}.".Length)..],
                        pair => pair.Value,
                        StringComparer.OrdinalIgnoreCase
                    );
                collRequisitionItems = QueryHelper.GenerateWhereAnd(_DbContext.DocFeeRequisitionItems.AsNoTracking(), requisitionItemConditions); // 应用申请明细条件
                coll = from item in coll
                       join reqItem in collRequisitionItems on item.RequisitionItemId equals reqItem.Id
                       select item; // 连接申请明细
                requisitionItemJoined = true;
            }
            var hasRequisitionConditions = conditional.Any(c => c.Key.StartsWith($"{nameof(DocFeeRequisition)}.", StringComparison.OrdinalIgnoreCase)); // 检查是否有申请单条件
            IQueryable<DocFeeRequisition> collRequisitions = null;
            if (hasRequisitionConditions)
            {
                var requisitionConditions = conditional // 提取申请单条件
                    .Where(c => c.Key.StartsWith($"{nameof(DocFeeRequisition)}.", StringComparison.OrdinalIgnoreCase))
                    .ToDictionary(
                        pair => pair.Key[($"{nameof(DocFeeRequisition)}.".Length)..],
                        pair => pair.Value,
                        StringComparer.OrdinalIgnoreCase
                    );
                collRequisitions = QueryHelper.GenerateWhereAnd(_DbContext.DocFeeRequisitions.AsNoTracking(), requisitionConditions).Where(c => c.OrgId == context.User.OrgId); // 应用申请单条件+组织隔离
                if (!requisitionItemJoined) // 申请明细未连接则需先连接
                {
                    coll = from item in coll
                           join reqItem in _DbContext.DocFeeRequisitionItems on item.RequisitionItemId equals reqItem.Id
                           join req in collRequisitions on reqItem.ParentId equals req.Id
                           select item;
                    requisitionItemJoined = true;
                }
                else // 申请明细已连接,直接连接申请单
                {
                    coll = from item in coll
                           join reqItem in _DbContext.DocFeeRequisitionItems on item.RequisitionItemId equals reqItem.Id
                           join req in collRequisitions on reqItem.ParentId equals req.Id
                           select item;
                }
                requisitionJoined = true;
            }
            var hasFeeConditions = conditional.Any(c => c.Key.StartsWith($"{nameof(DocFee)}.", StringComparison.OrdinalIgnoreCase)); // 检查是否有费用条件
            IQueryable<DocFee> collFees = null;
            if (hasFeeConditions)
            {
                var feeConditions = conditional // 提取费用条件
                    .Where(c => c.Key.StartsWith($"{nameof(DocFee)}.", StringComparison.OrdinalIgnoreCase))
                    .ToDictionary(
                        pair => pair.Key[($"{nameof(DocFee)}.".Length)..],
                        pair => pair.Value,
                        StringComparer.OrdinalIgnoreCase
                    );
                collFees = QueryHelper.GenerateWhereAnd(_DbContext.DocFees.AsNoTracking(), feeConditions); // 应用费用条件
                if (!requisitionItemJoined) // 申请明细未连接则需先连接申请明细和费用
                {
                    coll = from item in coll
                           join reqItem in _DbContext.DocFeeRequisitionItems on item.RequisitionItemId equals reqItem.Id
                           join fee in collFees on reqItem.FeeId equals fee.Id
                           select item;
                    requisitionItemJoined = true;
                }
                else // 申请明细已连接,直接连接费用
                {
                    coll = from item in coll
                           join reqItem in _DbContext.DocFeeRequisitionItems on item.RequisitionItemId equals reqItem.Id
                           join fee in collFees on reqItem.FeeId equals fee.Id
                           select item;
                }
                feeJoined = true;
            }
            var hasJobConditions = conditional.Any(c => c.Key.StartsWith($"{nameof(PlJob)}.", StringComparison.OrdinalIgnoreCase)); // 检查是否有工作号条件
            IQueryable<PlJob> collJobs = null;
            if (hasJobConditions)
            {
                var jobConditions = conditional // 提取工作号条件
                    .Where(c => c.Key.StartsWith($"{nameof(PlJob)}.", StringComparison.OrdinalIgnoreCase))
                    .ToDictionary(
                        pair => pair.Key[($"{nameof(PlJob)}.".Length)..],
                        pair => pair.Value,
                        StringComparer.OrdinalIgnoreCase
                    );
                collJobs = QueryHelper.GenerateWhereAnd(_DbContext.PlJobs.AsNoTracking(), jobConditions); // 应用工作号条件
                if (!requisitionItemJoined) // 申请明细未连接则需先连接申请明细、费用、工作号
                {
                    coll = from item in coll
                           join reqItem in _DbContext.DocFeeRequisitionItems on item.RequisitionItemId equals reqItem.Id
                           join fee in _DbContext.DocFees on reqItem.FeeId equals fee.Id
                           join job in collJobs on fee.JobId equals job.Id
                           select item;
                    requisitionItemJoined = true;
                    feeJoined = true;
                }
                else if (!feeJoined) // 申请明细已连接但费用未连接,需连接费用和工作号
                {
                    coll = from item in coll
                           join reqItem in _DbContext.DocFeeRequisitionItems on item.RequisitionItemId equals reqItem.Id
                           join fee in _DbContext.DocFees on reqItem.FeeId equals fee.Id
                           join job in collJobs on fee.JobId equals job.Id
                           select item;
                    feeJoined = true;
                }
                else // 申请明细和费用都已连接,直接连接工作号
                {
                    coll = from item in coll
                           join reqItem in _DbContext.DocFeeRequisitionItems on item.RequisitionItemId equals reqItem.Id
                           join fee in _DbContext.DocFees on reqItem.FeeId equals fee.Id
                           join job in collJobs on fee.JobId equals job.Id
                           select item;
                }
                jobJoined = true;
            }
            coll = coll.Distinct(); // 去重
            result.Total = coll.Count(); // 统计总数
            coll = coll.Skip(model.StartIndex); // 跳过指定数量
            if (model.Count > 0)
                coll = coll.Take(model.Count); // 取指定数量
            var items = coll.ToArray(); // 执行查询
            var itemIds = items.Select(x => x.Id).ToList();
            var invoices = !invoiceJoined && items.Any() // 批量加载结算单总单(如未通过JOIN获取)
                ? _DbContext.PlInvoicess.Where(i => items.Select(x => x.ParentId).Contains(i.Id)).AsNoTracking().ToList().ToDictionary(i => i.Id)
                : new Dictionary<Guid, PlInvoices>();
            var requisitionItems = !requisitionItemJoined && items.Any() // 批量加载申请明细(如未通过JOIN获取)
                ? _DbContext.DocFeeRequisitionItems.Where(r => items.Select(x => x.RequisitionItemId).Contains(r.Id)).AsNoTracking().ToList().ToDictionary(r => r.Id)
                : new Dictionary<Guid, DocFeeRequisitionItem>();
            var requisitionIds = requisitionItems.Values.Select(r => r.ParentId).Where(x => x.HasValue).Select(x => x.Value).Distinct().ToList();
            var requisitions = !requisitionJoined && requisitionIds.Any() // 批量加载申请单(如未通过JOIN获取)
                ? _DbContext.DocFeeRequisitions.Where(r => requisitionIds.Contains(r.Id)).AsNoTracking().ToList().ToDictionary(r => r.Id)
                : new Dictionary<Guid, DocFeeRequisition>();
            var feeIds = requisitionItems.Values.Select(r => r.FeeId).Where(x => x.HasValue).Select(x => x.Value).Distinct().ToList();
            var fees = !feeJoined && feeIds.Any() // 批量加载费用(如未通过JOIN获取)
                ? _DbContext.DocFees.Where(f => feeIds.Contains(f.Id)).AsNoTracking().ToList().ToDictionary(f => f.Id)
                : new Dictionary<Guid, DocFee>();
            var jobIds = fees.Values.Select(f => f.JobId).Where(x => x.HasValue).Select(x => x.Value).Distinct().ToList();
            var jobs = !jobJoined && jobIds.Any() // 批量加载工作号(如未通过JOIN获取)
                ? _DbContext.PlJobs.Where(j => jobIds.Contains(j.Id)).AsNoTracking().ToList().ToDictionary(j => j.Id)
                : new Dictionary<Guid, PlJob>();
            foreach (var item in items) // 组装返回结果
            {
                var invoice = item.ParentId.HasValue && invoices.ContainsKey(item.ParentId.Value) ? invoices[item.ParentId.Value] : null;
                var requisitionItem = item.RequisitionItemId.HasValue && requisitionItems.ContainsKey(item.RequisitionItemId.Value) ? requisitionItems[item.RequisitionItemId.Value] : null;
                var requisition = requisitionItem?.ParentId.HasValue == true && requisitions.ContainsKey(requisitionItem.ParentId.Value) ? requisitions[requisitionItem.ParentId.Value] : null;
                var fee = requisitionItem?.FeeId.HasValue == true && fees.ContainsKey(requisitionItem.FeeId.Value) ? fees[requisitionItem.FeeId.Value] : null;
                var job = fee?.JobId.HasValue == true && jobs.ContainsKey(fee.JobId.Value) ? jobs[fee.JobId.Value] : null;
                result.Result.Add(new GetPlInvoicesItemItem
                {
                    InvoicesItem = item,
                    Invoices = invoice,
                    PlJob = job,
                    DocFeeRequisitionItem = requisitionItem,
                    DocFeeRequisition = requisition,
                    Parent = invoice,
                });
            }
            return result;
        }

        #endregion 结算单

        #region 结算单明细

        /// <summary>
        /// 获取全部结算单明细。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">查询条件字典,键格式: 实体名.字段名。
        /// 支持的实体名: PlInvoicesItem(本体), PlInvoices(结算单), DocFeeRequisitionItem(申请明细), DocFeeRequisition(申请单), DocFee(费用), PlJob(工作号)
        /// 通用条件写法:所有条件都是字符串，对区间的写法是用逗号分隔（字符串类型暂时不支持区间且都是模糊查询）如"2024-1-1,2024-1-2"。
        /// 对强制取null的约束，则写"null"。
        /// 示例: 字典中添加 键="PlJob.JobNo" 值="XXX" 或 键="DocFee.FeeTypeId" 值="guid值"</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllPlInvoicesItemReturnDto> GetAllPlInvoicesItem([FromQuery] PagingParamsDtoBase model,
            [FromQuery][ModelBinder(typeof(DotKeyDictionaryModelBinder))] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllPlInvoicesItemReturnDto();
            conditional = conditional ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var coll = _DbContext.PlInvoicesItems.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking(); // 初始化查询集合
            var invoiceJoined = false; // 结算单总单连接标记
            var requisitionItemJoined = false; // 申请明细连接标记
            var requisitionJoined = false; // 申请单连接标记
            var feeJoined = false; // 费用连接标记
            var jobJoined = false; // 工作号连接标记
            var itemConditions = conditional.Where(c => c.Key.StartsWith($"{nameof(PlInvoicesItem)}.", StringComparison.OrdinalIgnoreCase) || !c.Key.Contains('.')) // 提取本体条件
                .ToDictionary(
                    pair => pair.Key.Contains('.') ? pair.Key[($"{nameof(PlInvoicesItem)}.".Length)..] : pair.Key,
                    pair => pair.Value,
                    StringComparer.OrdinalIgnoreCase
                );
            coll = QueryHelper.GenerateWhereAnd(coll, itemConditions); // 应用本体条件过滤
            var hasInvoiceConditions = conditional.Any(c => c.Key.StartsWith($"{nameof(PlInvoices)}.", StringComparison.OrdinalIgnoreCase)); // 检查是否有结算单总单条件
            if (hasInvoiceConditions)
            {
                var invoiceConditions = conditional // 提取结算单总单条件
                    .Where(c => c.Key.StartsWith($"{nameof(PlInvoices)}.", StringComparison.OrdinalIgnoreCase))
                    .ToDictionary(
                        pair => pair.Key[($"{nameof(PlInvoices)}.".Length)..],
                        pair => pair.Value,
                        StringComparer.OrdinalIgnoreCase
                    );
                var collInvoices = QueryHelper.GenerateWhereAnd(_DbContext.PlInvoicess.AsNoTracking(), invoiceConditions); // 应用结算单条件
                coll = from item in coll
                       join invoice in collInvoices on item.ParentId equals invoice.Id
                       select item; // 连接结算单总单
                invoiceJoined = true;
            }
            var hasRequisitionItemConditions = conditional.Any(c => c.Key.StartsWith($"{nameof(DocFeeRequisitionItem)}.", StringComparison.OrdinalIgnoreCase)); // 检查是否有申请明细条件
            if (hasRequisitionItemConditions)
            {
                var requisitionItemConditions = conditional // 提取申请明细条件
                    .Where(c => c.Key.StartsWith($"{nameof(DocFeeRequisitionItem)}.", StringComparison.OrdinalIgnoreCase))
                    .ToDictionary(
                        pair => pair.Key[($"{nameof(DocFeeRequisitionItem)}.".Length)..],
                        pair => pair.Value,
                        StringComparer.OrdinalIgnoreCase
                    );
                var collRequisitionItems = QueryHelper.GenerateWhereAnd(_DbContext.DocFeeRequisitionItems.AsNoTracking(), requisitionItemConditions); // 应用申请明细条件
                coll = from item in coll
                       join reqItem in collRequisitionItems on item.RequisitionItemId equals reqItem.Id
                       select item; // 连接申请明细
                requisitionItemJoined = true;
            }
            var hasRequisitionConditions = conditional.Any(c => c.Key.StartsWith($"{nameof(DocFeeRequisition)}.", StringComparison.OrdinalIgnoreCase)); // 检查是否有申请单条件
            if (hasRequisitionConditions)
            {
                var requisitionConditions = conditional // 提取申请单条件
                    .Where(c => c.Key.StartsWith($"{nameof(DocFeeRequisition)}.", StringComparison.OrdinalIgnoreCase))
                    .ToDictionary(
                        pair => pair.Key[($"{nameof(DocFeeRequisition)}.".Length)..],
                        pair => pair.Value,
                        StringComparer.OrdinalIgnoreCase
                    );
                var collRequisitions = QueryHelper.GenerateWhereAnd(_DbContext.DocFeeRequisitions.AsNoTracking(), requisitionConditions).Where(c => c.OrgId == context.User.OrgId); // 应用申请单条件+组织隔离
                if (!requisitionItemJoined) // 申请明细未连接则需先连接
                {
                    coll = from item in coll
                           join reqItem in _DbContext.DocFeeRequisitionItems on item.RequisitionItemId equals reqItem.Id
                           join req in collRequisitions on reqItem.ParentId equals req.Id
                           select item;
                    requisitionItemJoined = true;
                }
                else // 申请明细已连接,直接连接申请单
                {
                    coll = from item in coll
                           join reqItem in _DbContext.DocFeeRequisitionItems on item.RequisitionItemId equals reqItem.Id
                           join req in collRequisitions on reqItem.ParentId equals req.Id
                           select item;
                }
                requisitionJoined = true;
            }
            var hasFeeConditions = conditional.Any(c => c.Key.StartsWith($"{nameof(DocFee)}.", StringComparison.OrdinalIgnoreCase)); // 检查是否有费用条件
            if (hasFeeConditions)
            {
                var feeConditions = conditional // 提取费用条件
                    .Where(c => c.Key.StartsWith($"{nameof(DocFee)}.", StringComparison.OrdinalIgnoreCase))
                    .ToDictionary(
                        pair => pair.Key[($"{nameof(DocFee)}.".Length)..],
                        pair => pair.Value,
                        StringComparer.OrdinalIgnoreCase
                    );
                var collFees = QueryHelper.GenerateWhereAnd(_DbContext.DocFees.AsNoTracking(), feeConditions); // 应用费用条件
                if (!requisitionItemJoined) // 申请明细未连接则需先连接申请明细和费用
                {
                    coll = from item in coll
                           join reqItem in _DbContext.DocFeeRequisitionItems on item.RequisitionItemId equals reqItem.Id
                           join fee in collFees on reqItem.FeeId equals fee.Id
                           select item;
                    requisitionItemJoined = true;
                }
                else // 申请明细已连接,直接连接费用
                {
                    coll = from item in coll
                           join reqItem in _DbContext.DocFeeRequisitionItems on item.RequisitionItemId equals reqItem.Id
                           join fee in collFees on reqItem.FeeId equals fee.Id
                           select item;
                }
                feeJoined = true;
            }
            var hasJobConditions = conditional.Any(c => c.Key.StartsWith($"{nameof(PlJob)}.", StringComparison.OrdinalIgnoreCase)); // 检查是否有工作号条件
            if (hasJobConditions)
            {
                var jobConditions = conditional // 提取工作号条件
                    .Where(c => c.Key.StartsWith($"{nameof(PlJob)}.", StringComparison.OrdinalIgnoreCase))
                    .ToDictionary(
                        pair => pair.Key[($"{nameof(PlJob)}.".Length)..],
                        pair => pair.Value,
                        StringComparer.OrdinalIgnoreCase
                    );
                var collJobs = QueryHelper.GenerateWhereAnd(_DbContext.PlJobs.AsNoTracking(), jobConditions); // 应用工作号条件
                if (!requisitionItemJoined) // 申请明细未连接则需先连接申请明细、费用、工作号
                {
                    coll = from item in coll
                           join reqItem in _DbContext.DocFeeRequisitionItems on item.RequisitionItemId equals reqItem.Id
                           join fee in _DbContext.DocFees on reqItem.FeeId equals fee.Id
                           join job in collJobs on fee.JobId equals job.Id
                           select item;
                    requisitionItemJoined = true;
                    feeJoined = true;
                }
                else if (!feeJoined) // 申请明细已连接但费用未连接,需连接费用和工作号
                {
                    coll = from item in coll
                           join reqItem in _DbContext.DocFeeRequisitionItems on item.RequisitionItemId equals reqItem.Id
                           join fee in _DbContext.DocFees on reqItem.FeeId equals fee.Id
                           join job in collJobs on fee.JobId equals job.Id
                           select item;
                    feeJoined = true;
                }
                else // 申请明细和费用都已连接,直接连接工作号
                {
                    coll = from item in coll
                           join reqItem in _DbContext.DocFeeRequisitionItems on item.RequisitionItemId equals reqItem.Id
                           join fee in _DbContext.DocFees on reqItem.FeeId equals fee.Id
                           join job in collJobs on fee.JobId equals job.Id
                           select item;
                }
                jobJoined = true;
            }
            coll = coll.Distinct(); // 去重
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 增加新结算单明细。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPost]
        public ActionResult<AddPlInvoicesItemReturnDto> AddPlInvoicesItem(AddPlInvoicesItemParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
            {
                _Logger.LogWarning("无效的令牌: {token}", model.Token);
                return Unauthorized();
            }
            if (!_AuthorizationManager.Demand(out string err, "F.3.1") && !_AuthorizationManager.Demand(out err, "F.3.2")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new AddPlInvoicesItemReturnDto();
            var entity = model.PlInvoicesItem;
            entity.GenerateNewId();
            _DbContext.PlInvoicesItems.Add(model.PlInvoicesItem);
            _DbContext.SaveChanges();
            result.Id = model.PlInvoicesItem.Id;
            return result;
        }

        /// <summary>
        /// 修改结算单明细信息。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        /// <response code="404">指定Id的结算单明细不存在。</response>  
        [HttpPut]
        public ActionResult<ModifyPlInvoicesItemReturnDto> ModifyPlInvoicesItem(ModifyPlInvoicesItemParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyPlInvoicesItemReturnDto();
            if (!_AuthorizationManager.Demand(out string err, "F.3.2")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var modifiedEntities = new List<PlInvoicesItem>();
            if (!_EntityManager.Modify(new[] { model.PlInvoicesItem }, modifiedEntities)) return NotFound();
            var entity = _DbContext.Entry(modifiedEntities[0]);
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 删除指定Id的结算单明细。慎用！
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        /// <response code="404">指定Id的结算单明细不存在。</response>  
        [HttpDelete]
        public ActionResult<RemovePlInvoicesItemReturnDto> RemovePlInvoicesItem(RemovePlInvoicesItemParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            if (!_AuthorizationManager.Demand(out string err, "F.3.2") && !_AuthorizationManager.Demand(out err, "F.3.3")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new RemovePlInvoicesItemReturnDto();
            var id = model.Id;
            if (_DbContext.PlInvoicesItems.Find(id) is not PlInvoicesItem item) return BadRequest();
            _EntityManager.Remove(item);
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 设置指定的结算单下所有明细。
        /// 指定存在id的明细则更新，Id全0或不存在的Id到自动添加，原有未指定的明细将被删除。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的结算单不存在。</response>  
        [HttpPut]
        public ActionResult<SetPlInvoicesItemReturnDto> SetPlInvoicesItem(SetPlInvoicesItemParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new SetPlInvoicesItemReturnDto();
            if (_DbContext.PlInvoicess.Find(model.FrId) is not PlInvoices invoice) return NotFound();
            var aryIds = model.Items.Select(c => c.Id).ToArray();
            var existsIds = _DbContext.PlInvoicesItems
                .Where(c => c.ParentId == invoice.Id)
                .Select(c => c.Id)
                .ToArray();
            var modifies = model.Items.Where(c => existsIds.Contains(c.Id));
            foreach (var item in modifies)
            {
                _DbContext.Entry(item).CurrentValues.SetValues(item);
                _DbContext.Entry(item).State = EntityState.Modified;
            }
            var addIds = aryIds.Except(existsIds).ToArray();
            var adds = model.Items.Where(c => addIds.Contains(c.Id)).ToArray();
            Array.ForEach(adds, c => c.GenerateNewId());
            _DbContext.AddRange(adds);
            var removeIds = existsIds.Except(aryIds).ToArray();
            _DbContext.RemoveRange(_DbContext.PlInvoicesItems.Where(c => removeIds.Contains(c.Id)));
            _DbContext.SaveChanges();
            result.Result.AddRange(model.Items);
            return result;
        }

        #endregion 结算单明细
    }
}
