﻿using AutoMapper;
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
        public WfController(IServiceProvider serviceProvider, AccountManager accountManager, PowerLmsUserDbContext dbContext, IMapper mapper)
        {
            _ServiceProvider = serviceProvider;
            _AccountManager = accountManager;
            _DbContext = dbContext;
            _Mapper = mapper;
        }

        private IServiceProvider _ServiceProvider;
        private AccountManager _AccountManager;
        private PowerLmsUserDbContext _DbContext;
        IMapper _Mapper;

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
                c.State = GetWfState(c);
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
            var collItems = _DbContext.OwWfs.SelectMany(c => c.Children).SelectMany(c => c.Children).Where(c => c.OpertorId == operatorId);   //所有相关的Items
            var wfs = collItems.Select(c => c.Parent.Parent).Distinct().ToArray();  //所有相关流程

            result.Result.AddRange(wfs.Select(c =>
            {
                var r = _Mapper.Map<OwWfDto>(c);
                r.CreateDateTime = c.Children.Min(d => d.ArrivalDateTime);
                r.State = GetWfState(c);
                return r;
            }));
            return result;
        }

        /// <summary>
        /// 发送工作流文档的功能。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定模板不存在。</response>  
        [HttpPost]
        public ActionResult<WfSendReturnDto> Send(WfSendParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new WfSendReturnDto();
            var template = _DbContext.WfTemplates.Include(c => c.Children).ThenInclude(c => c.Children).Single(c => c.Id == model.TemplateId);
            if (template is null) return NotFound();

            var wf = _DbContext.OwWfs.Where(c => c.DocId == model.DocId && !c.Children.Any(c => !c.Children.Any(d => d.IsSuccess == false))).FirstOrDefault();  //当前流程
            if (wf is null)  //若没有流程正在执行
            {
                //创建流程及首节点
                wf = new OwWf()
                {
                    DocId = model.DocId,
                    TemplateId = model.TemplateId,
                };
                var list = template.Children.ToList();
                var noneFirst = list.Where(c => list.Any(d => d.NextId == c.Id)).ToList();   //非首节点集合
                noneFirst.ForEach(c => list.Remove(c));  //首节点集合
                var node = list.FirstOrDefault(c => c.Children.Any(d => d.OpertorId == context.User.Id));   //使用的节点

                var firstNode = new OwWfNode
                {
                    ArrivalDateTime = OwHelper.WorldNow,
                    Parent = wf,
                    ParentId = wf.Id,
                    TemplateId = node.Id,
                };
                wf.Children.Add(firstNode);
                var firstItem = new OwWfNodeItem
                {
                    Comment = model.Comment,
                    IsSuccess = true,
                    OperationKind = 0,
                    OpertorId = context.User.Id,
                    ParentId = firstNode.Id,
                    Parent = firstNode,
                    OpertorDisplayName = context.User.DisplayName,
                };
                firstNode.Children.Add(firstItem);
                _DbContext.OwWfs.Add(wf);
            }
            if (model.NextOpertorId is not null)    //若需要流转
            {
                var currentNode = wf.Children.OrderBy(c => c.ArrivalDateTime).Last(c => c.Children.Select(d => d.OpertorId).Contains(context.User.Id));   //当前节点
                var tNode = template.Children.First(c => c.Id == currentNode.TemplateId);   //当前节点模板
                var nextTItem = tNode.Children.FirstOrDefault(c => c.OpertorId == model.NextOpertorId);    //下一个操作人的模板
                var nextTNode = nextTItem.Parent;    //下一个节点模板

                var nextNode = new OwWfNode
                {
                    ParentId = wf.Id,
                    Parent = wf,
                    ArrivalDateTime = OwHelper.WorldNow + TimeSpan.FromMilliseconds(1), //避免同时到达
                    TemplateId = nextTNode.Id,
                };

                var nextOpId = _DbContext.Accounts.FirstOrDefault(c => c.Id == model.NextOpertorId);  //下一个操作人
                var nextItem = new OwWfNodeItem
                {
                    Comment = null,
                    IsSuccess = true,
                    OperationKind = 0,
                    OpertorId = model.NextOpertorId,
                    OpertorDisplayName = nextOpId.DisplayName,
                    Parent = nextNode,
                    ParentId = nextNode.Id,
                };
                nextNode.Children.Add(nextItem);
                wf.Children.Add(nextNode);
            }
            return result;
        }
    }

    /// <summary>
    /// 获取人员相关流转信息的参数封装类。
    /// </summary>
    public class GetWfByOpertorIdParamsDto : TokenDtoBase
    {

    }

    /// <summary>
    /// 获取人员相关流转信息的返回值封装类。
    /// </summary>
    public class GetWfByOpertorIdReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 相关流程的集合。
        /// </summary>
        public List<OwWfDto> Result { get; set; } = new List<OwWfDto>();
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
        /// 该工作流所处状态。0=0流转中，1=成功完成，2=已被终止。未来可能有其它状态。
        /// </summary>
        public int State { get; set; }

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
