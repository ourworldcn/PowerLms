using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NPOI.HSSF.UserModel;
using PowerLms.Data;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Dto;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// 工作流实例相关控制器。
    /// </summary>
    public class WfController : PlControllerBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="accountManager"></param>
        /// <param name="dbContext"></param>
        /// <param name="mapper"></param>
        /// <param name="entityManager"></param>
        /// <param name="owWfManager"></param>
        public WfController(IServiceProvider serviceProvider, AccountManager accountManager, PowerLmsUserDbContext dbContext, IMapper mapper, EntityManager entityManager,
            OwWfManager owWfManager)
        {
            _ServiceProvider = serviceProvider;
            _AccountManager = accountManager;
            _DbContext = dbContext;
            _Mapper = mapper;
            _EntityManager = entityManager;
            _WfManager = owWfManager;
        }

        private IServiceProvider _ServiceProvider;
        private AccountManager _AccountManager;
        private PowerLmsUserDbContext _DbContext;
        EntityManager _EntityManager;
        IMapper _Mapper;
        OwWfManager _WfManager;

        /// <summary>
        /// 获取文档相关的流程信息。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetWfReturnDto> GetWfByDocId([FromQuery] GetWfParamsDto model)
        {
            var result = new GetWfReturnDto();
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var coll = _DbContext.OwWfs.Where(c => c.DocId == model.EntityId).AsEnumerable();
            result.Result.AddRange(coll.Select(c => _Mapper.Map<OwWfDto>(c)));
            result.Result.ForEach(c =>
            {
                c.CreateDateTime = c.Children.Min(d => d.ArrivalDateTime);
            });
            return result;
        }

        /// <summary>
        /// 获取流程当前状态。
        /// </summary>
        /// <param name="wf"></param>
        /// <returns>0=0流转中，1=成功完成，2=已被终止。未来可能有其它状态。</returns>
        int GetWfState(OwWf wf)
        {
            var lastApvNode = GetLastApprovalNode(wf);
            if (lastApvNode == null) return 0;
            if (!GetNodeState(lastApvNode, out var state)) return 0;
            if (!state.HasValue) return 0;
            return state.Value ? 1 : 2;
        }

        /// <summary>
        /// 获取节点的审批状态。
        /// </summary>
        /// <param name="node"></param>
        /// <param name="state">false=否决，true=通过,空=待审批。</param>
        /// <returns>false=不是审批节点，true=是审批节点。</returns>
        bool GetNodeState(OwWfNode node, out bool? state)
        {
            var tt = _DbContext.WfTemplateNodes.Find(node.TemplateId);
            var coll = from ttNode in tt.Children.Where(c => c.OperationKind == 0)    //模板节点审批人集合
                       join instNode in node.Children.Where(c => c.OperationKind == 0)    ////实例结点审批人集合
                       on ttNode.OpertorId equals instNode.OpertorId
                       select instNode;
            var result = false;
            state = true;
            foreach (var item in coll)
            {
                result = true;
                if (item.IsSuccess is null)  //若在审批中
                {
                    state = null;
                    break;
                }
                else if (item.IsSuccess == false)  //若已否决否决
                {
                    state = false;
                    break;
                }
            }
            return result;
        }

        //OwWfNode GetFirstNode(OwWf wf)
        //{
        //    var coll = wf.Children.AsEnumerable().OrderBy(c => c.ArrivalDateTime).ToArray();
        //    foreach (var node in coll)
        //    {
        //        var nodeTemplate = _DbContext.WfTemplateNodes.Find(node.TemplateId);    //获取模板
        //        if (!nodeTemplate.NextId.HasValue && nodeTemplate.Children.Any(c => c.OperationKind == 0))   //若找到了第一个节点
        //            return node;
        //    }
        //    return null;
        //}

        /// <summary>
        /// 获取流程当前状态下的最后一个审批的节点。该节点的 <see cref="OwWfNodeItem.IsSuccess"/> 决定了流程的状态。
        /// </summary>
        /// <param name="wf"></param>
        /// <returns></returns>
        private OwWfNode GetLastApprovalNode(OwWf wf)
        {
            var coll = wf.Children.OrderByDescending(c => c.ArrivalDateTime);
            foreach (var node in coll)
            {
                var b = GetNodeState(node, out var state);
                if (!b) continue;   //若不是审批节点
                return node;
            }
            return null;
        }

        /// <summary>
        /// 获取人员相关流转的信息。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetWfByOpertorIdReturnDto> GetWfByOpertorId([FromQuery] GetWfByOpertorIdParamsDto model)
        {
            var result = new GetWfByOpertorIdReturnDto();
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();

            var operatorId = context.User.Id;
            byte state = 0;
            switch (model.State)
            {
                case 2:
                    break;
                default:
                    break;
            };
            var collBase = _DbContext.OwWfNodeItems.Where(c => c.OpertorId == operatorId).Select(c => c.Parent.Parent).Where(c => c.State == model.State).Distinct()
                .Include(c => c.Children).ThenInclude(c => c.Children);   //获取相关流程
            var coll = collBase.OrderBy(model.OrderFieldName, model.IsDesc);

            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);

            result.Result.ForEach(c =>
            {
                c.CreateDateTime = c.Children.Min(d => d.ArrivalDateTime);
            });
            return result;
        }

        /// <summary>
        /// 获取指定文档下一个操作人集合的信息。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定文档不存在。</response>  
        [HttpGet]
        public ActionResult<GetNextNodeItemsByDocIdReturnDto> GetNextNodeItemsByDocId([FromQuery] GetNextNodeItemsByDocIdParamsDto model)
        {
            var result = new GetNextNodeItemsByDocIdReturnDto();
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var doc = _DbContext.OwWfs.Find(model.DocId);
            if (doc is null) return NotFound();

            var node = GetLastApprovalNode(doc);
            if (node is null) return result;

            var ttNode = _DbContext.WfTemplateNodes.Include(c => c.Children).FirstOrDefault(c => c.Id == node.TemplateId);
            if (ttNode?.NextId is null) return result;

            result.Template = ttNode.Parent; //模板信息

            var nextNode = _DbContext.WfTemplateNodes.Find(ttNode.NextId);

            var coll = nextNode.Children.Select(c => _Mapper.Map<OwWfTemplateNodeItemDto>(c));
            result.Result.AddRange(coll);
            return result;
        }

        /// <summary>
        /// 发送工作流文档的功能。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">参数错误。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定模板不存在。</response>  
        [HttpPost]
        public ActionResult<WfSendReturnDto> Send(WfSendParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new WfSendReturnDto();
            var now = OwHelper.WorldNow;

            var template = _DbContext.WfTemplates.Include(c => c.Children).ThenInclude(c => c.Children).Single(c => c.Id == model.TemplateId);
            if (template is null) return NotFound();

            var wfs = _DbContext.OwWfs.Where(c => c.DocId == model.DocId && c.TemplateId == model.TemplateId && c.State == 0);  //所有可能的流程
            if (wfs.Count() > 1) return BadRequest("一个文档针对一个模板只能有一个流程");
            var wf = wfs.FirstOrDefault();
            OwWfNode currentNode = default;   //当前节点
            OwWfTemplateNode ttCurrentNode = default; //当前节点的模板
            if (wf is null)  //若没有流程正在执行
            {
                //创建流程及首节点
                wf = new OwWf()
                {
                    DocId = model.DocId,
                    TemplateId = model.TemplateId,
                    State = 0,
                };
                ttCurrentNode = _WfManager.GetFirstNodes(template).SingleOrDefault(c => _WfManager.Contains(context.User.Id, c)); //首节点模板

                currentNode = new OwWfNode
                {
                    ArrivalDateTime = now,
                    Parent = wf,
                    ParentId = wf.Id,
                    TemplateId = ttCurrentNode.Id,
                };
                wf.FirstNodeId = currentNode.Id;
                wf.Children.Add(currentNode);

                var firstItem = new OwWfNodeItem
                {
                    Comment = model.Comment,
                    IsSuccess = true,
                    OperationKind = 0,
                    OpertorId = context.User.Id,
                    ParentId = currentNode.Id,
                    Parent = currentNode,
                    OpertorDisplayName = context.User.DisplayName,
                };
                currentNode.Children.Add(firstItem);
                _DbContext.OwWfs.Add(wf);
            }
            else if (wf.State != 0)
            {
                return BadRequest("文档所处流程已经结束。");
            }
            currentNode ??= wf.Children.OrderBy(c => c.ArrivalDateTime).Last();   //当前节点
            ttCurrentNode = _DbContext.WfTemplateNodes.Find(currentNode.TemplateId);  //当前节点的模板

            var currentNodeItem = currentNode.Children.FirstOrDefault(c => c.OperationKind == 0 && c.OpertorId == context.User.Id); //当前审批人
            if (currentNodeItem is null)
                return BadRequest("非法的投递目标");

            if (model.NextOpertorId is Guid nextOpertorId)    //若需要流转
            {
                currentNodeItem.IsSuccess = true;
                currentNodeItem.Comment = model.Comment;

                var nextTItem = _DbContext.WfTemplateNodeItems.FirstOrDefault(c => c.ParentId == ttCurrentNode.NextId &&
                    c.OpertorId == nextOpertorId);    //下一个操作人的模板
                if (nextTItem == null)
                {
                    return BadRequest($"指定下一个操作人Id={model.NextOpertorId},但它不是合法的下一个操作人。");
                }

                var nextTNode = nextTItem.Parent;    //下一个节点模板

                var nextNode = new OwWfNode
                {
                    ParentId = wf.Id,
                    Parent = wf,
                    ArrivalDateTime = now + TimeSpan.FromMilliseconds(1), //避免同时到达
                    TemplateId = nextTNode.Id,
                };

                var nextOpId = _DbContext.Accounts.FirstOrDefault(c => c.Id == model.NextOpertorId);  //下一个操作人
                var nextItem = new OwWfNodeItem
                {
                    Comment = null,
                    IsSuccess = null,
                    OperationKind = 0,
                    OpertorId = model.NextOpertorId,
                    OpertorDisplayName = nextOpId.DisplayName,
                    Parent = nextNode,
                    ParentId = nextNode.Id,
                };
                nextNode.Children.Add(nextItem);
                wf.Children.Add(nextNode);
            }
            else //流程结束
            {
                if (model.Approval == 0)   //若通过
                {
                    wf.State = 1;
                    currentNodeItem.IsSuccess = true;
                }
                else if (model.Approval == 1) //若拒绝
                {
                    wf.State = 2;
                    currentNodeItem.IsSuccess = false;
                }
                else
                    return BadRequest($"{nameof(model.Approval)} 参数值非法。");
            }
            _DbContext.SaveChanges();
            result.WfId = wf.Id;
            return result;
        }
    }

    /// <summary>
    /// 获取指定文档下一组操作人的信息功能的参数封装类。
    /// </summary>
    public class GetNextNodeItemsByDocIdParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 文档的Id。
        /// </summary>
        public Guid DocId { get; set; }
    }

    /// <summary>
    /// 获取指定文档下一组操作人的信息功能的返回值封装类。
    /// </summary>
    public class GetNextNodeItemsByDocIdReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 发送的下一个操作人的集合。可能为空，因为该模板仅有单一节点或已经到达最后一个节点，无法向下发送。
        /// </summary>
        public List<OwWfTemplateNodeItemDto> Result { get; set; } = new List<OwWfTemplateNodeItemDto>();

        /// <summary>
        /// 所属流程模板信息。
        /// </summary>
        public OwWfTemplate Template { get; set; }
    }

    /// <summary>
    /// 获取人员相关流转信息的参数封装类。
    /// </summary>
    public class GetWfByOpertorIdParamsDto : PagingParamsDtoBase
    {
        /// <summary>
        /// 过滤流文档状态的参数，0=待审批，1=已审批但仍在流转中，2=已结束的流程。
        /// </summary>
        public byte State { get; set; }
    }

    /// <summary>
    /// 获取人员相关流转信息的返回值封装类。
    /// </summary>
    public class GetWfByOpertorIdReturnDto : PagingReturnDtoBase<OwWfDto>
    {
    }

    /// <summary>
    /// 获取文档相关的流程信息的参数封装类。
    /// </summary>
    public class GetWfParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 要获取工作流实例的相关文档ID。
        /// </summary>
        public Guid EntityId { get; set; }
    }

    /// <summary>
    /// 工作流实例的封装类。
    /// </summary>
    [AutoMap(typeof(OwWf))]
    public class OwWfDto : OwWf
    {
        /// <summary>
        /// 该工作流的创建时间。
        /// </summary>
        public DateTime CreateDateTime { get; set; }

        //public List<Guid> FirstNodeIds { get; set; } = new List<Guid>();
    }

    /// <summary>
    /// 获取文档相关的流程信息的返回值封装类。
    /// </summary>
    public class GetWfReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 相关工作流实例的集合。
        /// </summary>
        public List<OwWfDto> Result { get; set; } = new List<OwWfDto>();
    }

    /// <summary>
    /// 发送工作流文档功能的参数封装类。
    /// </summary>
    public class WfSendParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 工作流模板的Id。
        /// </summary>
        public Guid TemplateId { get; set; }

        /// <summary>
        /// 流程文档Id。如申请单Id。
        /// </summary>
        public Guid DocId { get; set; }

        /// <summary>
        /// 审批结果，0通过，1终止。对起始节点只能是0.
        /// </summary>
        public int Approval { get; set; }

        /// <summary>
        /// 审核批示。
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// 指定发送的下一个操作人的Id。必须符合流程定义。
        /// 如果是null或省略，对起始节点是仅保存批示意见，不进行流转。对最后一个节点这个属性被忽视。
        /// </summary>
        public Guid? NextOpertorId { get; set; }
    }

    /// <summary>
    /// 发送工作流文档功能的返回值封装类。
    /// </summary>
    public class WfSendReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 工作流实例的Id。
        /// </summary>
        public Guid WfId { get; set; }
    }
}
