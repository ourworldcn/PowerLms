using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NPOI.SS.Formula.Functions;
using PowerLms.Data;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Dto;
using System.Net;
using System.Text.Json.Serialization;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// 工作流及模板相关操作的控制器类。
    /// </summary>
    public class WfTemplateController : PlControllerBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public WfTemplateController(AccountManager accountManager, IServiceProvider serviceProvider, PowerLmsUserDbContext dbContext, EntityManager entityManager, IMapper mapper, AuthorizationManager authorizationManager)
        {
            _AccountManager = accountManager;
            _ServiceProvider = serviceProvider;
            _DbContext = dbContext;
            _EntityManager = entityManager;
            _Mapper = mapper;
            _AuthorizationManager = authorizationManager;
        }

        private readonly AccountManager _AccountManager;
        private readonly IServiceProvider _ServiceProvider;
        private readonly PowerLmsUserDbContext _DbContext;
        private readonly EntityManager _EntityManager;
        private readonly IMapper _Mapper;
        private readonly AuthorizationManager _AuthorizationManager;

        #region 模板表相关

        /// <summary>
        /// 增加新工作流模板节点。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPost]
        public ActionResult<AddWorkflowTemplateReturnDto> AddWfTemplate(AddWorkflowTemplateParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            #region 权限判定
            if (!_AuthorizationManager.Demand(out string err, "E.1")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            #endregion 权限判定

            var result = new AddWorkflowTemplateReturnDto();
            model.Item.GenerateNewId();

            _DbContext.WfTemplates.Add(model.Item);
            model.Item.CreateDateTime = OwHelper.WorldNow;
            model.Item.CreateBy = context.User.Id;
            model.Item.OrgId = context.User.OrgId;
            _DbContext.SaveChanges();
            result.Id = model.Item.Id;
            return result;
        }

        /// <summary>
        /// 获取全部工作流模板节点。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">查询的条件。Id,DisplayName,KindCode。不区分大小写。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllWorkflowTemplateReturnDto> GetAllWfTemplate([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllWorkflowTemplateReturnDto();
            var dbSet = _DbContext.WfTemplates.Where(c => c.OrgId == context.User.OrgId);
            dbSet = _DbContext.WfTemplates;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsQueryable();
            foreach (var item in conditional)
                if (string.Equals(item.Key, nameof(OwWfTemplate.DisplayName), StringComparison.OrdinalIgnoreCase))
                {
                    coll = coll.Where(c => c.DisplayName.Contains(item.Value));
                }
                else if (string.Equals(item.Key, nameof(OwWfTemplate.KindCode), StringComparison.OrdinalIgnoreCase))
                {
                    coll = coll.Where(c => c.KindCode.Contains(item.Value));
                }
                else if (string.Equals(item.Key, "Id", StringComparison.OrdinalIgnoreCase))
                {
                    if (Guid.TryParse(item.Value, out var id))
                        coll = coll.Where(c => c.Id == id);
                }
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 修改工作流模板节点信息。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        /// <response code="404">指定Id的工作流模板节点不存在。</response>  
        [HttpPut]
        public ActionResult<ModifyWorkflowTemplateReturnDto> ModifyWfTemplate(ModifyWorkflowTemplateParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyWorkflowTemplateReturnDto();
            #region 权限判定
            if (!_AuthorizationManager.Demand(out string err, "E.1")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            #endregion 权限判定
            var modifiedEntities = new List<OwWfTemplate>();
            if (!_EntityManager.Modify(model.Items, modifiedEntities)) return NotFound();
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 批量删除工作流模板信息。(物理硬删除,请先移除子对象)
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        /// <response code="404">指定Id中，至少有一个不存在相应实体。</response>  
        [HttpDelete]
        public ActionResult<RemoveWorkflowTemplateReturnDto> RemoveWfTemplate(RemoveWorkflowTemplatePatamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemoveWorkflowTemplateReturnDto();

            #region 权限判定
            if (!_AuthorizationManager.Demand(out string err, "E.1")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            #endregion 权限判定

            var dbSet = _DbContext.WfTemplates;
            var items = dbSet.Where(c => model.Ids.Contains(c.Id)).ToArray();
            if (items.Length != model.Ids.Count) return BadRequest("指定Id中，至少有一个不存在相应实体。");
            _DbContext.RemoveRange(items);
            _DbContext.SaveChanges();
            return result;
        }
        #endregion 模板表相关

        #region 模板节点表相关

        /// <summary>
        /// 增加新工作流模板节点。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        /// <response code="404">未找到指定的前向节点或后向节点，NextId可以省略视同为null，但如果指定则必须是一个已存在节点的Id。</response>  
        [HttpPost]
        public ActionResult<AddWfTemplateNodeReturnDto> AddWfTemplateNode(AddWfTemplateNodeParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new AddWfTemplateNodeReturnDto();

            #region 权限判定
            if (!_AuthorizationManager.Demand(out string err, "E.1")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            #endregion 权限判定
            if (model.Item.NextId is not null)
            {
                var nextNode = _DbContext.WfTemplateNodes.Find(model.Item.NextId.Value);
                if (nextNode is null) return NotFound();
            }
            model.Item.GenerateNewId();

            _DbContext.WfTemplateNodes.Add(model.Item);
            if (model.PreNodeId != null) //若须增加前向节点
            {
                var prv = _DbContext.WfTemplateNodes.Find(model.PreNodeId);
                if (prv == null)
                {
                    return NotFound();
                }
                prv.NextId = model.Item.Id;
            }
            //model.Item.CreateDateTime = OwHelper.WorldNow;
            //model.Item.CreateBy = context.User.Id;
            //model.Item.OrgId = context.User.OrgId;
            _DbContext.SaveChanges();
            result.Id = model.Item.Id;
            return result;
        }

        /// <summary>
        /// 获取全部工作流模板节点。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">查询的条件。Id,DisplayName,NextId，ParentId，。不区分大小写。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllWfTemplateNodeReturnDto> GetAllWfTemplateNode([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllWfTemplateNodeReturnDto();
            var dbSet = _DbContext.WfTemplateNodes/*.Where(c => c.OrgId == context.User.OrgId)*/;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsQueryable();
            foreach (var item in conditional)
                if (string.Equals(item.Key, nameof(OwWfTemplateNode.DisplayName), StringComparison.OrdinalIgnoreCase))
                {
                    coll = coll.Where(c => c.DisplayName.Contains(item.Value));
                }
                else if (string.Equals(item.Key, nameof(OwWfTemplateNode.Id), StringComparison.OrdinalIgnoreCase))
                {
                    if (Guid.TryParse(item.Value, out var id))
                        coll = coll.Where(c => c.Id == id);
                }
                else if (string.Equals(item.Key, nameof(OwWfTemplateNode.ParentId), StringComparison.OrdinalIgnoreCase))
                {
                    if (Guid.TryParse(item.Value, out var id))
                        coll = coll.Where(c => c.ParentId == id);
                }
                else if (string.Equals(item.Key, nameof(OwWfTemplateNode.NextId), StringComparison.OrdinalIgnoreCase))
                {
                    if (Guid.TryParse(item.Value, out var id))
                        coll = coll.Where(c => c.NextId == id);
                }
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 修改工作流模板节点信息。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        /// <response code="404">指定Id的工作流模板节点或NextId指定节点不存在。NextId可以省略视同为null，但如果指定则必须是一个已存在节点的Id。</response>  
        [HttpPut]
        public ActionResult<ModifyWfTemplateNodeReturnDto> ModifyWfTemplateNode(ModifyWfTemplateNodeParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyWfTemplateNodeReturnDto();
            #region 权限判定
            if (!_AuthorizationManager.Demand(out string err, "E.1")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            #endregion 权限判定
            var ids = model.Items.Where(c => c.NextId is not null).Select(c => c.NextId);
            var nextNodeCount = _DbContext.WfTemplateNodes.Count(c => ids.Contains(c.Id));
            if (nextNodeCount != ids.Count()) return NotFound();
            var modifiedEntities = new List<OwWfTemplateNode>();
            if (!_EntityManager.Modify(model.Items, modifiedEntities)) return NotFound();
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 批量删除工作流模板节点信息。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        /// <response code="404">指定Id中，至少有一个不存在相应实体。</response>  
        [HttpDelete]
        public ActionResult<RemoveWfTemplateNodeReturnDto> RemoveWfTemplateNode(RemoveWfTemplateNodePatamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemoveWfTemplateNodeReturnDto();
            #region 权限判定
            if (!_AuthorizationManager.Demand(out string err, "E.1")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            #endregion 权限判定
            var dbSet = _DbContext.WfTemplateNodes;

            var items = dbSet.Where(c => model.Ids.Contains(c.Id)).ToArray();
            if (items.Length != model.Ids.Count) return BadRequest("指定Id中，至少有一个不存在相应实体。");
            var prvs = dbSet.Where(c => c.NextId != null && model.Ids.Contains(c.NextId.Value)).ToArray();

            if (model.IsRemoveChildren)  //若需要删除子项
            {
                foreach (var item in items)
                {
                    var tmp = item.Children.ToArray();
                    item.Children.Clear();
                    _DbContext.RemoveRange(tmp);
                }
            }
            _DbContext.RemoveRange(items);
            prvs.ForEach(c => c.NextId = null);
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 获取首次发送时可选操作人的信息。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定的模板不存在。</response>  
        [HttpGet]
        public ActionResult<GetNextNodeItemsByKindCodeReturnDto> GetNextNodeItemsByKindCode([FromQuery] GetNextNodeItemsByKindCodeParamsDto model)
        {
            var result = new GetNextNodeItemsByKindCodeReturnDto { };
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();

            var tt = _DbContext.WfTemplates.Include(c => c.Children).ThenInclude(c => c.Children).
                FirstOrDefault(c => c.KindCode == model.KindCode && c.OrgId == context.User.OrgId);
            if (tt is null) return NotFound();
            var nextNodeIds = tt.Children.Where(c => c.NextId != null).Select(c => c.NextId).ToArray(); //后续节点的Id集合
            var firstNodes = tt.Children.Where(c => !nextNodeIds.Contains(c.Id));   //最前面的节点
            firstNodes = firstNodes.Where(c => c.Children.Any(d => d.OpertorId == context.User.Id)).ToArray();    //含自身的节点
            var nodeIds = firstNodes.Select(c => c.NextId);    //含自身的下一个节点的Id集合
            var nextNodes = tt.Children.Where(c => nodeIds.Contains(c.Id));

            var r = nextNodes.SelectMany(c => c.Children).Where(c => c.OperationKind == 0);
            result.Template = tt;    //模板数据
            result.Result.AddRange(r.Select(c => _Mapper.Map<OwWfTemplateNodeItemDto>(c)));
            return result;
        }

        #endregion 模板节点表相关

        #region 模板节点详细表相关

        /// <summary>
        /// 增加新工作流模板节点详细。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPost]
        public ActionResult<AddWfTemplateNodeItemReturnDto> AddWfTemplateNodeItem(AddWfTemplateNodeItemParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new AddWfTemplateNodeItemReturnDto();
            #region 权限判定
            if (!_AuthorizationManager.Demand(out string err, "E.1")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            #endregion 权限判定
            model.Item.GenerateNewId();

            _DbContext.WfTemplateNodeItems.Add(model.Item);
            //model.Item.CreateDateTime = OwHelper.WorldNow;
            //model.Item.CreateBy = context.User.Id;
            //model.Item.OrgId = context.User.OrgId;
            _DbContext.SaveChanges();
            result.Id = model.Item.Id;
            return result;
        }

        /// <summary>
        /// 获取全部工作流模板节点详细。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">查询的条件。Id，ParentId，。不区分大小写。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllWfTemplateNodeItemReturnDto> GetAllWfTemplateNodeItem([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllWfTemplateNodeItemReturnDto();
            var dbSet = _DbContext.WfTemplateNodeItems/*.Where(c => c.OrgId == context.User.OrgId)*/;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            foreach (var item in conditional)
                if (string.Equals(item.Key, nameof(OwWfTemplateNodeItem.Id), StringComparison.OrdinalIgnoreCase))
                {
                    if (Guid.TryParse(item.Value, out var id))
                        coll = coll.Where(c => c.Id == id);
                }
                else if (string.Equals(item.Key, nameof(OwWfTemplateNodeItem.ParentId), StringComparison.OrdinalIgnoreCase))
                {
                    if (Guid.TryParse(item.Value, out var id))
                        coll = coll.Where(c => c.ParentId == id);
                }
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 修改工作流模板节点详细信息。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        /// <response code="404">指定Id的工作流模板节点详细不存在。</response>  
        [HttpPut]
        public ActionResult<ModifyWfTemplateNodeItemReturnDto> ModifyWfTemplateNodeItem(ModifyWfTemplateNodeItemParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyWfTemplateNodeItemReturnDto();
            #region 权限判定
            if (!_AuthorizationManager.Demand(out string err, "E.1")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            #endregion 权限判定
            var modifiedEntities = new List<OwWfTemplateNodeItem>();
            if (!_EntityManager.Modify(model.Items, modifiedEntities)) return NotFound();
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 批量删除工作流模板信息。(物理硬删除)
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        /// <response code="404">指定Id中，至少有一个不存在相应实体。</response>  
        [HttpDelete]
        public ActionResult<RemoveWfTemplateNodeItemReturnDto> RemoveWfTemplateNodeItem(RemoveWfTemplateNodeItemPatamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemoveWfTemplateNodeItemReturnDto();
            #region 权限判定
            if (!_AuthorizationManager.Demand(out string err, "E.1")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            #endregion 权限判定

            var dbSet = _DbContext.WfTemplateNodeItems;
            var items = dbSet.Where(c => model.Ids.Contains(c.Id)).ToArray();
            if (items.Length != model.Ids.Count) return BadRequest("指定Id中，至少有一个不存在相应实体。");
            _DbContext.RemoveRange(items);
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 设置指定的流程模板节点下所有明细。
        /// 指定存在id的明细则更新，Id全0或不存在的Id到自动添加，原有未指定的明细将被删除。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        /// <response code="404">指定Id的业务费用流程模板节点不存在。</response>  
        [HttpPut]
        public ActionResult<SetWfTemplateNodeItemReturnDto> SetWfTemplateNodeItem(SetWfTemplateNodeItemParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new SetWfTemplateNodeItemReturnDto();
            #region 权限判定
            if (!_AuthorizationManager.Demand(out string err, "E.1")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            #endregion 权限判定
            var parent = _DbContext.WfTemplateNodes.FirstOrDefault(c => c.Id == model.ParentId);
            if (parent is null) return NotFound();
            var aryIds = model.Items.Select(c => c.Id).ToArray();   //指定的Id
            var existsIds = _DbContext.WfTemplateNodeItems.Where(c => c.ParentId == parent.Id).Select(c => c.Id).ToArray();    //已经存在的Id
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
                var ss = _Mapper.Map<OwWfTemplateNodeItem>(c);
                ss.GenerateNewId();
                parent.Children.Add(ss);
            });
            //_DbContext.AddRange(adds);
            //删除
            var removeIds = existsIds.Except(aryIds).ToArray();
            _DbContext.RemoveRange(_DbContext.WfTemplateNodeItems.Where(c => removeIds.Contains(c.Id)));

            _DbContext.SaveChanges();
            //后处理
            result.Result.AddRange(model.Items);
            return result;
        }

        #endregion 模板节点详细表相关

        #region 流程模板类型码相关
        /// <summary>
        /// 获取全部工作流模板类型码详细。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">查询的条件。Id，DisplayName，。不区分大小写。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllWfTemplateKindCodeReturnDto> GetAllWfTemplateKindCode([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllWfTemplateKindCodeReturnDto();
            var dbSet = _DbContext.WfKindCodeDics/*.Where(c => c.OrgId == context.User.OrgId)*/;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            foreach (var item in conditional)
                if (string.Equals(item.Key, nameof(OwWfKindCodeDic.Id), StringComparison.OrdinalIgnoreCase))
                {
                    coll = coll.Where(c => c.Id.Contains(item.Value));
                }
                else if (string.Equals(item.Key, nameof(OwWfKindCodeDic.DisplayName), StringComparison.OrdinalIgnoreCase))
                {
                    if (Guid.TryParse(item.Value, out var id))
                        coll = coll.Where(c => c.DisplayName.Contains(item.Value));
                }
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        #endregion 流程模板类型码相关
    }
}
