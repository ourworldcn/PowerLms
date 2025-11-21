using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NPOI.SS.Formula.Functions;
using NuGet.Packaging;
using OW.Data;
using PowerLms.Data;
using PowerLmsServer;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Dto;
using System.Linq;
using System.Net;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// 财务相关功能控制器。
    /// </summary>
    public partial class FinancialController : PlControllerBase
    {
        /// <summary>
        /// 构造函数。Debit 和credit。
        /// </summary>
        public FinancialController(AccountManager accountManager, IServiceProvider serviceProvider, EntityManager entityManager,
            PowerLmsUserDbContext dbContext, ILogger<FinancialController> logger, IMapper mapper, OwWfManager wfManager, AuthorizationManager authorizationManager, OwSqlAppLogger sqlAppLogger)
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
        readonly ILogger<FinancialController> _Logger;
        readonly IMapper _Mapper;
        readonly OwWfManager _WfManager;
        readonly AuthorizationManager _AuthorizationManager;
        readonly OwSqlAppLogger _SqlAppLogger;

        #region 结算单

        /// <summary>
        /// 获取全部结算单。
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
        public ActionResult<GetAllPlInvoicesReturnDto> GetAllPlInvoices([FromQuery] PagingParamsDtoBase model,
            [FromQuery][ModelBinder(typeof(DotKeyDictionaryModelBinder))] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllPlInvoicesReturnDto();

            try
            {
                conditional = conditional ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                // 使用nameof表达式获取DocFeeRequisition类名，并创建前缀
                string docFeeRequisitionPrefix = $"{nameof(DocFeeRequisition)}.";

                // 分离DocFeeRequisition相关条件和普通条件
                var reqConditions = conditional
                    .Where(pair => pair.Key.StartsWith(docFeeRequisitionPrefix, StringComparison.OrdinalIgnoreCase))
                    .ToDictionary(
                        pair => pair.Key[docFeeRequisitionPrefix.Length..], // 使用范围运算符去掉前缀
                        pair => pair.Value,
                        StringComparer.OrdinalIgnoreCase
                    );

                // 收集所有不包含点号的条件
                var invoiceConditions = conditional
                    .Where(pair => !pair.Key.Contains('.'))
                    .ToDictionary(
                        pair => pair.Key,
                        pair => pair.Value,
                        StringComparer.OrdinalIgnoreCase
                    );

                IQueryable<PlInvoices> dbSet = _DbContext.PlInvoicess;

                // 如果有DocFeeRequisition相关的条件，则需要联合查询
                if (reqConditions.Count > 0)
                {
                    _Logger.LogDebug("应用申请单过滤条件: {conditions}",
                        string.Join(", ", reqConditions.Select(kv => $"{kv.Key}={kv.Value}")));

                    // 先获取符合DocFeeRequisition条件的申请单
                    var requisitions = EfHelper.GenerateWhereAnd(_DbContext.DocFeeRequisitions.AsNoTracking(), reqConditions);

                    // 通过申请单明细和结算单明细的关联，找到相关的结算单
                    dbSet = (from invoice in _DbContext.PlInvoicess
                             join item in _DbContext.PlInvoicesItems on invoice.Id equals item.ParentId
                             join reqItem in _DbContext.DocFeeRequisitionItems on item.RequisitionItemId equals reqItem.Id
                             join req in requisitions on reqItem.ParentId equals req.Id
                             select invoice).Distinct();
                }

                // 应用结算单自身的过滤条件
                var coll = EfHelper.GenerateWhereAnd(dbSet, invoiceConditions);

                // 应用排序和分页
                coll = coll.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
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
            // 验证令牌和获取上下文
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
            {
                _Logger.LogWarning("添加结算单时提供了无效的令牌: {token}", model.Token);
                return Unauthorized();
            }

            var result = new AddPlInvoiceReturnDto();

            try
            {
                // 验证权限
                if (!_AuthorizationManager.Demand(out string err, "F.3.1"))
                    return StatusCode((int)HttpStatusCode.Forbidden, err);

                // 验证输入参数
                if (model.PlInvoices == null)
                {
                    result.HasError = true;
                    result.ErrorCode = 400;
                    result.DebugMessage = "结算单数据不能为空";
                    return result;
                }

                // 获取要保存的实体并进行基础设置
                var entity = model.PlInvoices;
                entity.GenerateIdIfEmpty(); // 生成新的GUID

                // 设置创建信息
                entity.CreateBy = context.User.Id;
                entity.CreateDateTime = OwHelper.WorldNow;

                // 添加实体到数据库上下文
                _DbContext.PlInvoicess.Add(entity);

                // 应用审计日志(可选)
                _SqlAppLogger.LogGeneralInfo($"用户 {context.User.Id} 创建了结算单ID:{entity.Id}，操作：AddPlInvoices");

                // 保存更改到数据库
                _DbContext.SaveChanges();

                // 设置返回结果
                result.Id = entity.Id;
                result.HasError = false;

                _Logger.LogDebug("成功创建结算单: {id}", entity.Id);
            }
            catch (Exception ex)
            {
                // 记录错误并设置返回错误信息
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
            if (!_EntityManager.Modify(new[] { model.PlInvoices })) return NotFound();
            //忽略不可更改字段
            var entity = _DbContext.Entry(model.PlInvoices);
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
        /// 结算单确认.
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
            // 确认结算单
            foreach (var invoice in coll)
            {
                if (model.IsConfirm)
                {
                    // 确认结算单
                    invoice.ConfirmDateTime = now;
                    invoice.ConfirmId = context.User.Id;
                }
                else
                {
                    // 取消确认结算单
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
        /// <param name="conditional">条件使用 [实体名.字段名] (带实体前三个别的需要方括号括住)格式,值格式参见通用格式。
        /// 支持的实体名有：PlJob,DocFeeRequisition,DocFeeRequisitionItem，PlInvoices ,PlInvoicesItem</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetPlInvoicesItemReturnDto> GetDocInvoicesItem([FromQuery] GetPlInvoicesItemParamsDto model,
            [FromQuery][ModelBinder(typeof(DotKeyDictionaryModelBinder))] Dictionary<string, string> conditional = null)
        {
            //查询 需要返回 申请单 job 费用实体 申请明细的余额（未结算）
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetPlInvoicesItemReturnDto();

            // 确保conditional不为null
            conditional = conditional ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var dbSet = _DbContext.PlInvoicesItems;

            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();

            // 处理PlInvoicesItem的条件（包括没有前缀的直接字段名条件）
            var tmpColl = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // 添加带PlInvoicesItem前缀的条件
            foreach (var item in conditional.Where(c => c.Key.StartsWith(nameof(PlInvoicesItem), StringComparison.OrdinalIgnoreCase)))
            {
                tmpColl[item.Key] = item.Value;
            }

            // 添加没有前缀的条件（直接作为PlInvoicesItem的字段）
            foreach (var item in conditional.Where(c => !c.Key.Contains('.')))
            {
                tmpColl[item.Key] = item.Value;
            }

            var collInvoicesItem = EfHelper.GenerateWhereAnd(coll, tmpColl);

            tmpColl = new Dictionary<string, string>(conditional.Where(c => c.Key.StartsWith(nameof(PlJob), StringComparison.OrdinalIgnoreCase)),
                StringComparer.OrdinalIgnoreCase);
            var collPlJob = EfHelper.GenerateWhereAnd(_DbContext.PlJobs, tmpColl);

            tmpColl = new Dictionary<string, string>(conditional.Where(c => c.Key.StartsWith(nameof(PlInvoices), StringComparison.OrdinalIgnoreCase)),
                StringComparer.OrdinalIgnoreCase);
            var collInvoice = EfHelper.GenerateWhereAnd(_DbContext.PlInvoicess, tmpColl);

            tmpColl = new Dictionary<string, string>(conditional.Where(c => c.Key.StartsWith(nameof(DocFeeRequisition), StringComparison.OrdinalIgnoreCase)),
                StringComparer.OrdinalIgnoreCase);
            var collDocFeeRequisition = EfHelper.GenerateWhereAnd(_DbContext.DocFeeRequisitions, tmpColl).Where(c => c.OrgId == context.User.OrgId);

            tmpColl = new Dictionary<string, string>(conditional.Where(c => c.Key.StartsWith(nameof(DocFeeRequisitionItem), StringComparison.OrdinalIgnoreCase)),
                StringComparer.OrdinalIgnoreCase);
            var collDocFeeRequisitionItem = EfHelper.GenerateWhereAnd(_DbContext.DocFeeRequisitionItems, tmpColl);

            var collBase = from fii in collInvoicesItem
                           join fi in collInvoice on fii.ParentId equals fi.Id
                           join fri in collDocFeeRequisitionItem on fii.RequisitionItemId equals fri.Id
                           join fr in collDocFeeRequisition on fri.ParentId equals fr.Id
                           join fee in _DbContext.DocFees on fri.FeeId equals fee.Id
                           join job in collPlJob on fee.JobId equals job.Id
                           select new { fii, fi, job, fr, fri };
            //获取总数
            var collCount = collBase.Distinct();
            result.Total = collCount.Count();
            //获取集合
            collBase = collBase.Skip(model.StartIndex);
            if (model.Count > 0)
                collBase = collBase.Take(model.Count);
            var aryResult = collBase.ToArray();

            foreach (var item in aryResult)
            {
                var tmp = new GetPlInvoicesItemItem
                {
                    InvoicesItem = item.fii,
                    Invoices = item.fi,
                    PlJob = item.job,
                    DocFeeRequisitionItem = item.fri,
                    DocFeeRequisition = item.fr,
                    Parent = item.fi,
                };
                //tmp.Remainder = item.Amount - _DbContext.PlInvoicesItems.Where(c => c.RequisitionItemId == item.Id).Sum(c => c.Amount);
                result.Result.Add(tmp);
            }
            return result;
        }

        #endregion 结算单

        #region 结算单明细

        /// <summary>
        /// 获取全部结算单明细。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">查询的条件。
        /// 通用条件写法:所有条件都是字符串，对区间的写法是用逗号分隔（字符串类型暂时不支持区间且都是模糊查询）如"2024-1-1,2024-1-2"。
        /// 对强制取null的约束，则写"null"。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllPlInvoicesItemReturnDto> GetAllPlInvoicesItem([FromQuery] PagingParamsDtoBase model,
            [FromQuery][ModelBinder(typeof(DotKeyDictionaryModelBinder))] Dictionary<string, string> conditional = null)
        {

            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllPlInvoicesItemReturnDto();
            var dbSet = _DbContext.PlInvoicesItems;

            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            coll = EfHelper.GenerateWhereAnd(coll, conditional);
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
            if (!_EntityManager.Modify(new[] { model.PlInvoicesItem })) return NotFound();
            //忽略不可更改字段
            var entity = _DbContext.Entry(model.PlInvoicesItem);
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
        /// 设置指定的申请单下所有明细。
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

            // ✅ 修复：使用PlInvoices而不是DocFeeRequisition
            if (_DbContext.PlInvoicess.Find(model.FrId) is not PlInvoices invoice) return NotFound();

            var aryIds = model.Items.Select(c => c.Id).ToArray();   //指定的Id

            // ✅ 修复：查询PlInvoicesItems关联的是结算单，不是申请单
            var existsIds = _DbContext.PlInvoicesItems
                .Where(c => c.ParentId == invoice.Id)
                .Select(c => c.Id)
                .ToArray();    //已经存在的Id

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
            _DbContext.RemoveRange(_DbContext.PlInvoicesItems.Where(c => removeIds.Contains(c.Id)));

            _DbContext.SaveChanges();
            //后处理
            result.Result.AddRange(model.Items);
            return result;
        }

        #endregion 结算单明细

        #region 费用方案

        /// <summary>
        /// 获取全部费用方案。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">查询的条件。
        /// 通用条件写法:所有条件都是字符串，对区间的写法是用逗号分隔（字符串类型暂时不支持区间且都是模糊查询）如"2024-1-1,2024-1-2"。
        /// 对强制取null的约束，则写"null"。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllDocFeeTemplateReturnDto> GetAllDocFeeTemplate([FromQuery] PagingParamsDtoBase model,
            [FromQuery][ModelBinder(typeof(DotKeyDictionaryModelBinder))] Dictionary<string, string> conditional = null)
        {

            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllDocFeeTemplateReturnDto();
            var dbSet = _DbContext.DocFeeTemplates;

            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            coll = EfHelper.GenerateWhereAnd(coll, conditional);
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 增加新费用方案。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPost]
        public ActionResult<AddDocFeeTemplateReturnDto> AddDocFeeTemplate(AddDocFeeTemplateParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
            {
                _Logger.LogWarning("无效的令牌: {token}", model.Token);
                return Unauthorized();
            }

            #region 权限判定
            var docFeeTT = model.DocFeeTemplate;
            if (docFeeTT.JobTypeId == ProjectContent.AeId)    //若是空运出口业务
            {
                if (!_AuthorizationManager.Demand(out string err, "D20.0")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (docFeeTT.JobTypeId == ProjectContent.AiId)    //若是空运进口业务
            {
                if (!_AuthorizationManager.Demand(out string err, "D20.1")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (docFeeTT.JobTypeId == ProjectContent.SeId)    //若是海运出口业务
            {
                if (!_AuthorizationManager.Demand(out string err, "D20.2")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (docFeeTT.JobTypeId == ProjectContent.SiId)    //若是海运进口业务
            {
                if (!_AuthorizationManager.Demand(out string err, "D20.3")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            #endregion 权限判定

            var result = new AddDocFeeTemplateReturnDto();
            var entity = model.DocFeeTemplate;
            entity.GenerateNewId();
            model.DocFeeTemplate.CreateBy = context.User.Id;
            model.DocFeeTemplate.CreateDateTime = OwHelper.WorldNow;
            _DbContext.DocFeeTemplates.Add(model.DocFeeTemplate);

            _DbContext.SaveChanges();

            result.Id = model.DocFeeTemplate.Id;
            return result;
        }

        /// <summary>
        /// 修改费用方案信息。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        /// <response code="404">指定Id的费用方案不存在。</response>  
        [HttpPut]
        public ActionResult<ModifyDocFeeTemplateReturnDto> ModifyDocFeeTemplate(ModifyDocFeeTemplateParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyDocFeeTemplateReturnDto();
            #region 权限判定
            var docFeeTT = model.DocFeeTemplate;
            if (docFeeTT.JobTypeId == ProjectContent.AeId)    //若是空运出口业务
            {
                if (!_AuthorizationManager.Demand(out string err, "D20.0")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (docFeeTT.JobTypeId == ProjectContent.AiId)    //若是空运进口业务
            {
                if (!_AuthorizationManager.Demand(out string err, "D20.1")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (docFeeTT.JobTypeId == ProjectContent.SeId)    //若是海运出口业务
            {
                if (!_AuthorizationManager.Demand(out string err, "D20.2")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (docFeeTT.JobTypeId == ProjectContent.SiId)    //若是海运进口业务
            {
                if (!_AuthorizationManager.Demand(out string err, "D20.3")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            #endregion 权限判定
            if (!_EntityManager.Modify(new[] { model.DocFeeTemplate })) return NotFound();
            //忽略不可更改字段
            var entity = _DbContext.Entry(model.DocFeeTemplate);
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 删除指定Id的费用方案。这会删除所有费用方案明细项。慎用！
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        /// <response code="404">指定Id的费用方案不存在。</response>  
        [HttpDelete]
        public ActionResult<RemoveDocFeeTemplateReturnDto> RemoveDocFeeTemplate(RemoveDocFeeTemplateParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemoveDocFeeTemplateReturnDto();
            var id = model.Id;
            if (_DbContext.DocFeeTemplates.Find(id) is not DocFeeTemplate item) return BadRequest();
            #region 权限判定
            var docFeeTT = item;
            if (docFeeTT.JobTypeId == ProjectContent.AeId)    //若是空运出口业务
            {
                if (!_AuthorizationManager.Demand(out string err, "D20.0")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (docFeeTT.JobTypeId == ProjectContent.AiId)    //若是空运进口业务
            {
                if (!_AuthorizationManager.Demand(out string err, "D20.1")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (docFeeTT.JobTypeId == ProjectContent.SeId)    //若是海运出口业务
            {
                if (!_AuthorizationManager.Demand(out string err, "D20.2")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (docFeeTT.JobTypeId == ProjectContent.SiId)    //若是海运进口业务
            {
                if (!_AuthorizationManager.Demand(out string err, "D20.3")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            #endregion 权限判定
            var children = _DbContext.DocFeeTemplateItems.Where(c => c.ParentId == item.Id).ToArray();
            _EntityManager.Remove(item);
            if (children.Length > 0) _DbContext.RemoveRange(children);
            _DbContext.OwSystemLogs.Add(new OwSystemLog
            {
                OrgId = context.User.OrgId,
                ActionId = $"Delete.{nameof(DocFeeTemplate)}.{item.Id}",
                ExtraGuid = context.User.Id,
                ExtraDecimal = children.Length,
            });
            _DbContext.SaveChanges();
            return result;
        }

        #endregion 费用方案

        #region 费用方案明细

        /// <summary>
        /// 获取全部费用方案明细。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">查询的条件。
        /// 通用条件写法:所有条件都是字符串，对区间的写法是用逗号分隔（字符串类型暂时不支持区间且都是模糊查询）如"2024-1-1,2024-1-2"。
        /// 对强制取null的约束，则写"null"。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的费用方案不存在。</response>  
        [HttpGet]
        public ActionResult<GetAllDocFeeTemplateItemReturnDto> GetAllDocFeeTemplateItem([FromQuery] PagingParamsDtoBase model,
            [FromQuery][ModelBinder(typeof(DotKeyDictionaryModelBinder))] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllDocFeeTemplateItemReturnDto();
            var dbSet = _DbContext.DocFeeTemplateItems;

            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            coll = EfHelper.GenerateWhereAnd(coll, conditional);
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 增加新费用方案明细。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="403">权限不足。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpPost]
        public ActionResult<AddDocFeeTemplateItemReturnDto> AddDocFeeTemplateItem(AddDocFeeTemplateItemParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
            {
                _Logger.LogWarning("无效的令牌: {token}", model.Token);
                return Unauthorized();
            }
            var result = new AddDocFeeTemplateItemReturnDto();
            var entity = model.DocFeeTemplateItem;

            var id = model.DocFeeTemplateItem.ParentId;
            if (id is null) return BadRequest();
            if (_DbContext.DocFeeTemplates.Find(id.Value) is not DocFeeTemplate item) return BadRequest();
            #region 权限判定
            var docFeeTT = item;
            if (docFeeTT.JobTypeId == ProjectContent.AeId)    //若是空运出口业务
            {
                if (!_AuthorizationManager.Demand(out string err, "D20.0")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (docFeeTT.JobTypeId == ProjectContent.AiId)    //若是空运进口业务
            {
                if (!_AuthorizationManager.Demand(out string err, "D20.1")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (docFeeTT.JobTypeId == ProjectContent.SeId)    //若是海运出口业务
            {
                if (!_AuthorizationManager.Demand(out string err, "D20.2")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (docFeeTT.JobTypeId == ProjectContent.SiId)    //若是海运进口业务
            {
                if (!_AuthorizationManager.Demand(out string err, "D20.3")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            #endregion 权限判定

            entity.GenerateNewId();
            _DbContext.DocFeeTemplateItems.Add(model.DocFeeTemplateItem);

            _DbContext.SaveChanges();

            result.Id = model.DocFeeTemplateItem.Id;
            return result;
        }

        /// <summary>
        /// 修改费用方案明细信息。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="403">权限不足。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的费用方案明细不存在。</response>
        [HttpPut]
        public ActionResult<ModifyDocFeeTemplateItemReturnDto> ModifyDocFeeTemplateItem(ModifyDocFeeTemplateItemParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyDocFeeTemplateItemReturnDto();
            var id = model.DocFeeTemplateItem.ParentId;
            if (id is null) return BadRequest();
            if (_DbContext.DocFeeTemplates.Find(id.Value) is not DocFeeTemplate item) return BadRequest();
            #region 权限判定
            var docFeeTT = item;
            if (docFeeTT.JobTypeId == ProjectContent.AeId)    //若是空运出口业务
            {
                if (!_AuthorizationManager.Demand(out string err, "D20.0")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (docFeeTT.JobTypeId == ProjectContent.AiId)    //若是空运进口业务
            {
                if (!_AuthorizationManager.Demand(out string err, "D20.1")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (docFeeTT.JobTypeId == ProjectContent.SeId)    //若是海运出口业务
            {
                if (!_AuthorizationManager.Demand(out string err, "D20.2")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (docFeeTT.JobTypeId == ProjectContent.SiId)    //若是海运进口业务
            {
                if (!_AuthorizationManager.Demand(out string err, "D20.3")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            #endregion 权限判定

            if (!_EntityManager.Modify(new[] { model.DocFeeTemplateItem })) return NotFound();
            //忽略不可更改字段
            var entity = _DbContext.Entry(model.DocFeeTemplateItem);
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 删除指定Id的费用方案明细。慎用！
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="403">权限不足。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的费用方案明细不存在。</response>  
        [HttpDelete]
        public ActionResult<RemoveDocFeeTemplateItemReturnDto> RemoveDocFeeTemplateItem(RemoveDocFeeTemplateItemParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemoveDocFeeTemplateItemReturnDto();
            var id = model.Id;
            if (_DbContext.DocFeeTemplateItems.Find(id) is not DocFeeTemplateItem item) return BadRequest();

            var idTT = item.ParentId;
            if (idTT is null) return BadRequest();

            if (_DbContext.DocFeeTemplates.Find(idTT.Value) is not DocFeeTemplate tt) return BadRequest();
            #region 权限判定
            var docFeeTT = tt;
            if (docFeeTT.JobTypeId == ProjectContent.AeId)    //若是空运出口业务
            {
                if (!_AuthorizationManager.Demand(out string err, "D20.0")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (docFeeTT.JobTypeId == ProjectContent.AiId)    //若是空运进口业务
            {
                if (!_AuthorizationManager.Demand(out string err, "D20.1")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (docFeeTT.JobTypeId == ProjectContent.SeId)    //若是海运出口业务
            {
                if (!_AuthorizationManager.Demand(out string err, "D20.2")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (docFeeTT.JobTypeId == ProjectContent.SiId)    //若是海运进口业务
            {
                if (!_AuthorizationManager.Demand(out string err, "D20.3")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            #endregion 权限判定

            _EntityManager.Remove(item);
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 设置指定的费用方案下所有明细。
        /// 指定存在id的明细则更新，Id全0或不存在的Id到自动添加，原有未指定的明细将被删除。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        /// <response code="404">指定Id的业务费用申请单不存在。</response>  
        [HttpPut]
        public ActionResult<SetDocFeeTemplateItemReturnDto> SetDocFeeTemplateItem(SetDocFeeTemplateItemParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new SetDocFeeTemplateItemReturnDto();
            if (_DbContext.DocFeeRequisitions.Find(model.FrId) is not DocFeeRequisition fr) return NotFound();

            var idTT = model.FrId;
            if (_DbContext.DocFeeTemplates.Find(idTT) is not DocFeeTemplate tt) return BadRequest();
            #region 权限判定
            var docFeeTT = tt;
            if (docFeeTT.JobTypeId == ProjectContent.AeId)    //若是空运出口业务
            {
                if (!_AuthorizationManager.Demand(out string err, "D20.0")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (docFeeTT.JobTypeId == ProjectContent.AiId)    //若是空运进口业务
            {
                if (!_AuthorizationManager.Demand(out string err, "D20.1")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (docFeeTT.JobTypeId == ProjectContent.SeId)    //若是海运出口业务
            {
                if (!_AuthorizationManager.Demand(out string err, "D20.2")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (docFeeTT.JobTypeId == ProjectContent.SiId)    //若是海运进口业务
            {
                if (!_AuthorizationManager.Demand(out string err, "D20.3")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            #endregion 权限判定
            var aryIds = model.Items.Select(c => c.Id).ToArray();   //指定的Id
            var existsIds = _DbContext.DocFeeTemplateItems.Where(c => c.ParentId == fr.Id).Select(c => c.Id).ToArray();    //已经存在的Id
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
            _DbContext.RemoveRange(_DbContext.DocFeeTemplateItems.Where(c => removeIds.Contains(c.Id)));

            _DbContext.SaveChanges();
            //后处理
            result.Result.AddRange(model.Items);
            return result;
        }

        #endregion 费用方案明细

    }
}
