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
    /// 工作流及模板相关操作的控制器类。
    /// </summary>
    public class WfTemplateController : PlControllerBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public WfTemplateController(AccountManager accountManager, IServiceProvider serviceProvider, PowerLmsUserDbContext dbContext, EntityManager entityManager, IMapper mapper)
        {
            _AccountManager = accountManager;
            _ServiceProvider = serviceProvider;
            _DbContext = dbContext;
            _EntityManager = entityManager;
            _Mapper = mapper;
        }

        private AccountManager _AccountManager;
        private IServiceProvider _ServiceProvider;
        private PowerLmsUserDbContext _DbContext;
        private EntityManager _EntityManager;
        private IMapper _Mapper;

        #region 模板表相关

        /// <summary>
        /// 增加新工作流模板节点。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpPost]
        public ActionResult<AddWorkflowTemplateReturnDto> AddWfTemplate(AddWorkflowTemplateParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllWorkflowTemplateReturnDto();
            var dbSet = _DbContext.WfTemplates.Where(c => c.OrgId == context.User.OrgId);
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
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
        /// <response code="404">指定Id的工作流模板节点不存在。</response>  
        [HttpPut]
        public ActionResult<ModifyWorkflowTemplateReturnDto> ModifyWfTemplate(ModifyWorkflowTemplateParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyWorkflowTemplateReturnDto();
            if (!_EntityManager.Modify(model.Items)) return NotFound();
            //foreach (var item in model.Items)
            //{
            //    item.UpdateBy = context.User.Id;
            //    item.UpdateDateTime = OwHelper.WorldNow;
            //}
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
        /// <response code="404">指定Id中，至少有一个不存在相应实体。</response>  
        [HttpDelete]
        public ActionResult<RemoveWorkflowTemplateReturnDto> RemoveWfTemplate(RemoveWorkflowTemplatePatamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemoveWorkflowTemplateReturnDto();

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
        /// <response code="404">未找到指定的前向节点。</response>  
        [HttpPost]
        public ActionResult<AddWfTemplateNodeReturnDto> AddWfTemplateNode(AddWfTemplateNodeParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new AddWfTemplateNodeReturnDto();
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
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllWfTemplateNodeReturnDto();
            var dbSet = _DbContext.WfTemplateNodes/*.Where(c => c.OrgId == context.User.OrgId)*/;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
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
        /// <response code="404">指定Id的工作流模板节点不存在。</response>  
        [HttpPut]
        public ActionResult<ModifyWfTemplateNodeReturnDto> ModifyWfTemplateNode(ModifyWfTemplateNodeParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyWfTemplateNodeReturnDto();
            if (!_EntityManager.Modify(model.Items)) return NotFound();
            //foreach (var item in model.Items)
            //{
            //    item.UpdateBy = context.User.Id;
            //    item.UpdateDateTime = OwHelper.WorldNow;
            //}
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
        /// <response code="404">指定Id中，至少有一个不存在相应实体。</response>  
        [HttpDelete]
        public ActionResult<RemoveWfTemplateNodeReturnDto> RemoveWfTemplateNode(RemoveWfTemplateNodePatamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemoveWfTemplateNodeReturnDto();

            var dbSet = _DbContext.WfTemplateNodes;
            var items = dbSet.Where(c => model.Ids.Contains(c.Id)).ToArray();
            if (items.Length != model.Ids.Count) return BadRequest("指定Id中，至少有一个不存在相应实体。");
            _DbContext.RemoveRange(items);
            _DbContext.SaveChanges();
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
        [HttpPost]
        public ActionResult<AddWfTemplateNodeItemReturnDto> AddWfTemplateNodeItem(AddWfTemplateNodeItemParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new AddWfTemplateNodeItemReturnDto();
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
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
        /// <response code="404">指定Id的工作流模板节点详细不存在。</response>  
        [HttpPut]
        public ActionResult<ModifyWfTemplateNodeItemReturnDto> ModifyWfTemplateNodeItem(ModifyWfTemplateNodeItemParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyWfTemplateNodeItemReturnDto();
            if (!_EntityManager.Modify(model.Items)) return NotFound();
            //foreach (var item in model.Items)
            //{
            //    item.UpdateBy = context.User.Id;
            //    item.UpdateDateTime = OwHelper.WorldNow;
            //}
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
        /// <response code="404">指定Id中，至少有一个不存在相应实体。</response>  
        [HttpDelete]
        public ActionResult<RemoveWfTemplateNodeItemReturnDto> RemoveWfTemplateNodeItem(RemoveWfTemplateNodeItemPatamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemoveWfTemplateNodeItemReturnDto();

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
        /// <response code="404">指定Id的业务费用流程模板节点不存在。</response>  
        [HttpPut]
        public ActionResult<SetWfTemplateNodeItemReturnDto> SetWfTemplateNodeItem(SetWfTemplateNodeItemParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new SetWfTemplateNodeItemReturnDto();
            var parent = _DbContext.DocFeeRequisitions.Find(model.ParentId);
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
            Array.ForEach(adds, c => c.GenerateNewId());
            _DbContext.AddRange(adds);
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
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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

    #region 流程模板类型码相关
    /// <summary>
    /// 查询工作流模板节点详细对象返回值封装类。
    /// </summary>
    public class GetAllWfTemplateKindCodeReturnDto : PagingReturnDtoBase<OwWfKindCodeDic>
    {
    }

    #endregion 流程模板类型码相关

    #region 模板节点详细表相关
    /// <summary>
    /// 设置指定的流程模板节点下所有明细功能的参数封装类。
    /// </summary>
    public class SetWfTemplateNodeItemParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 流程模板节点的Id。
        /// </summary>
        public Guid ParentId { get; set; }

        /// <summary>
        /// 流程模板节点明细表的集合。
        /// 指定存在id的明细则更新，Id全0或不存在的Id自动添加，原有未指定的明细将被删除。
        /// </summary>
        public List<OwWfTemplateNodeItem> Items { get; set; } = new List<OwWfTemplateNodeItem>();
    }

    /// <summary>
    /// 设置指定的流程模板节点下所有明细功能的返回值封装类。
    /// </summary>
    public class SetWfTemplateNodeItemReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 指定流程模板节点下，所有明细的对象。
        /// </summary>
        public List<OwWfTemplateNodeItem> Result { get; set; } = new List<OwWfTemplateNodeItem>();
    }

    /// <summary>
    /// 批量删除工作流模板信息功能参数封装类。
    /// </summary>
    public class RemoveWfTemplateNodeItemPatamsDto : RemoveItemsParamsDtoBase
    {

    }

    /// <summary>
    /// 批量删除工作流模板信息功能返回值封装类。
    /// </summary>
    public class RemoveWfTemplateNodeItemReturnDto : RemoveItemsReturnDtoBase
    {
    }

    /// <summary>
    /// 修改工作流模板节点详细功能的返回值封装类。
    /// </summary>
    public class ModifyWfTemplateNodeItemReturnDto : ModifyReturnDtoBase
    {
    }

    /// <summary>
    /// 修改工作流模板节点详细功能的参数封装类。
    /// </summary>
    public class ModifyWfTemplateNodeItemParamsDto : ModifyParamsDtoBase<OwWfTemplateNodeItem>
    {
    }

    /// <summary>
    /// 查询工作流模板节点详细对象返回值封装类。
    /// </summary>
    public class GetAllWfTemplateNodeItemReturnDto : PagingReturnDtoBase<OwWfTemplateNodeItem>
    {
    }

    /// <summary>
    /// 增加工作流模板节点详细对象功能参数封装类。
    /// </summary>
    public class AddWfTemplateNodeItemParamsDto : AddParamsDtoBase<OwWfTemplateNodeItem>
    {
    }

    /// <summary>
    /// 增加工作流模板节点详细对象功能返回值封装类。
    /// </summary>
    public class AddWfTemplateNodeItemReturnDto : AddReturnDtoBase
    {
    }
    #endregion 模板节点详细表相关

    #region 模板节点表相关
    /// <summary>
    /// 批量删除工作流模板信息功能参数封装类。
    /// </summary>
    public class RemoveWfTemplateNodePatamsDto : RemoveItemsParamsDtoBase
    {

    }

    /// <summary>
    /// 批量删除工作流模板信息功能返回值封装类。
    /// </summary>
    public class RemoveWfTemplateNodeReturnDto : RemoveItemsReturnDtoBase
    {
    }

    /// <summary>
    /// 修改工作流模板节点功能的返回值封装类。
    /// </summary>
    public class ModifyWfTemplateNodeReturnDto : ModifyReturnDtoBase
    {
    }

    /// <summary>
    /// 修改工作流模板节点功能的参数封装类。
    /// </summary>
    public class ModifyWfTemplateNodeParamsDto : ModifyParamsDtoBase<OwWfTemplateNode>
    {
    }

    /// <summary>
    /// 查询工作流模板节点对象返回值封装类。
    /// </summary>
    public class GetAllWfTemplateNodeReturnDto : PagingReturnDtoBase<OwWfTemplateNode>
    {
    }

    /// <summary>
    /// 增加工作流模板节点对象功能参数封装类。
    /// </summary>
    public class AddWfTemplateNodeParamsDto : AddParamsDtoBase<OwWfTemplateNode>
    {
        /// <summary>
        /// 前向节点的Id。省略或为空则不连接的前向节点。
        /// </summary>
        public Guid? PreNodeId { get; set; }
    }

    /// <summary>
    /// 增加工作流模板节点对象功能返回值封装类。
    /// </summary>
    public class AddWfTemplateNodeReturnDto : AddReturnDtoBase
    {
    }
    #endregion 模板节点表相关

    #region 模板表相关
    /// <summary>
    /// 批量删除工作流模板信息功能参数封装类。
    /// </summary>
    public class RemoveWorkflowTemplatePatamsDto : RemoveItemsParamsDtoBase
    {

    }

    /// <summary>
    /// 批量删除工作流模板信息功能返回值封装类。
    /// </summary>
    public class RemoveWorkflowTemplateReturnDto : RemoveItemsReturnDtoBase
    {
    }

    /// <summary>
    /// 修改工作流模板节点功能的返回值封装类。
    /// </summary>
    public class ModifyWorkflowTemplateReturnDto : ModifyReturnDtoBase
    {
    }

    /// <summary>
    /// 修改工作流模板节点功能的参数封装类。
    /// </summary>
    public class ModifyWorkflowTemplateParamsDto : ModifyParamsDtoBase<OwWfTemplate>
    {
    }

    /// <summary>
    /// 查询工作流模板节点对象返回值封装类。
    /// </summary>
    public class GetAllWorkflowTemplateReturnDto : PagingReturnDtoBase<OwWfTemplate>
    {
    }

    /// <summary>
    /// 增加工作流模板节点对象功能参数封装类。
    /// </summary>
    public class AddWorkflowTemplateParamsDto : AddParamsDtoBase<OwWfTemplate>
    {
    }

    /// <summary>
    /// 增加工作流模板节点对象功能返回值封装类。
    /// </summary>
    public class AddWorkflowTemplateReturnDto : AddReturnDtoBase
    {
    }
    #endregion 模板表相关
}
