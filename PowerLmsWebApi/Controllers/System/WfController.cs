/*
 * Web API层 - 工作流控制器
 * 工作流实例管理 - 核心业务逻辑
 * 
 * 功能说明：
 * - 工作流文档发送和状态管理
 * - 工作流节点和操作人查询
 * - 审批流程状态追踪
 * 
 * 技术特点：
 * - 集成OwWfManager工作流引擎
 * - 支持动态审批人分配
 * - 完整的权限验证和数据隔离
 * 
 * 作者：GitHub Copilot
 * 创建时间：2024
 * 最后修改：2025-02-06 - 增强客户端错误数据防御和诊断日志
 */

using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NPOI.HSSF.UserModel;
using PowerLms.Data;
using PowerLms.Data.OA;
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
        /// <param name="logger"></param>
        public WfController(IServiceProvider serviceProvider, AccountManager accountManager, PowerLmsUserDbContext dbContext, IMapper mapper, EntityManager entityManager,
               OwWfManager owWfManager, ILogger<WfController> logger)
        {
            _ServiceProvider = serviceProvider;
            _AccountManager = accountManager;
            _DbContext = dbContext;
            _Mapper = mapper;
            _EntityManager = entityManager;
            _WfManager = owWfManager;
            _Logger = logger;
        }

        private readonly IServiceProvider _ServiceProvider;
        private readonly AccountManager _AccountManager;
        private readonly PowerLmsUserDbContext _DbContext;
        private readonly EntityManager _EntityManager;
        private readonly IMapper _Mapper;
        private readonly OwWfManager _WfManager;
        private readonly ILogger<WfController> _Logger;

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
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();

            var operatorId = context.User.Id;
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
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var doc = _DbContext.OwWfs.Find(model.DocId);
            if (doc is null) return NotFound();

            var node = GetLastApprovalNode(doc);
            if (node is null) return result;

            var ttNode = _DbContext.WfTemplateNodes.Include(c => c.Children).FirstOrDefault(c => c.Id == node.TemplateId);

            // 🔧 增强诊断：记录模板节点信息
            if (ttNode?.NextId is null)
            {
                _Logger.LogWarning(
                        "工作流节点无下一节点：DocId={DocId}, 当前节点模板ID={TemplateId}, 当前节点显示名={NodeName}, NextId为null，流程将无法继续流转",
                  model.DocId, node.TemplateId, ttNode?.DisplayName ?? "未知");
                return result;
            }

            result.Template = ttNode.Parent; //模板信息

            var nextNode = _DbContext.WfTemplateNodes.Find(ttNode.NextId);

            // 🔧 增强诊断：验证下一个节点是否存在
            if (nextNode == null)
            {
                _Logger.LogError(
                    "工作流模板配置错误：NextId指向的节点不存在！DocId={DocId}, 当前节点={CurrentNode}, NextId={NextId}",
                 model.DocId, ttNode.Id, ttNode.NextId);
                return result;
            }

            // 🔧 增强诊断：检查下一个节点是否有审批人
            if (!nextNode.Children.Any(c => c.OperationKind == 0))
            {
                _Logger.LogWarning(
            "工作流节点未配置审批人：DocId={DocId}, 下一节点ID={NextNodeId}, 下一节点显示名={NextNodeName}，前端将收到空列表",
       model.DocId, nextNode.Id, nextNode.DisplayName);
            }

            var coll = nextNode.Children.Select(c => _Mapper.Map<OwWfTemplateNodeItemDto>(c));
            result.Result.AddRange(coll);

            _Logger.LogInformation(
           "获取下一节点审批人成功：DocId={DocId}, 当前节点={CurrentNode}, 下一节点={NextNode}, 审批人数量={ApproverCount}",
 model.DocId, ttNode.Id, nextNode.Id, result.Result.Count);

            return result;
        }

        /// <summary>
        /// 发送工作流文档的功能。
        /// </summary>
        [HttpPost]
        public ActionResult<WfSendReturnDto> Send(WfSendParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new WfSendReturnDto();
            var now = OwHelper.WorldNow;

            // 参数验证 - 参考项目其他Controller的验证模式
            if (model.DocId == Guid.Empty || model.TemplateId == Guid.Empty)
                return BadRequest("文档ID和模板ID不能为空");

            if (!string.IsNullOrEmpty(model.Comment) && model.Comment.Length > 1000)
                return BadRequest("审批意见不能超过1000字符");

            // 🔧 增强日志：记录请求参数
            _Logger.LogInformation(
       "工作流发送请求：DocId={DocId}, TemplateId={TemplateId}, Approval={Approval}, NextOpertorId={NextOpertorId}, User={UserId}",
           model.DocId, model.TemplateId, model.Approval, model.NextOpertorId, context.User.Id);

            // 改进模板查询 - 使用FirstOrDefault并添加AsSplitQuery优化
            var template = _DbContext.WfTemplates
                .Include(c => c.Children).ThenInclude(c => c.Children)
                .AsSplitQuery() // 避免笛卡尔积
                .FirstOrDefault(c => c.Id == model.TemplateId);
            if (template is null)
            {
                _Logger.LogWarning("工作流模板不存在：TemplateId={TemplateId}", model.TemplateId);
                return NotFound("指定的工作流模板不存在");
            }

            // 🔧 增强诊断：记录模板节点配置
            var templateNodeCount = template.Children.Count;
            var templateNodeInfo = string.Join(", ", template.Children.Select(n =>
           $"{n.DisplayName}(NextId={(n.NextId.HasValue ? n.NextId.ToString() : "null")})"));
            _Logger.LogInformation(
                 "工作流模板配置：TemplateId={TemplateId}, 节点数={NodeCount}, 节点链表=[{NodeInfo}]",
                       template.Id, templateNodeCount, templateNodeInfo);

            var wfs = _DbContext.OwWfs.Where(c => c.DocId == model.DocId && c.TemplateId == model.TemplateId && c.State == 0);  //所有可能的流程
            if (wfs.Count() > 1) return BadRequest("一个文档针对一个模板只能有一个流程");
            var wf = wfs.FirstOrDefault();
            OwWfNode currentNode = default;   //当前节点
            OwWfTemplateNode ttCurrentNode = default; //当前节点的模板
            bool isNewWorkflow = false; // 🔥 新增：标记是否为新创建的工作流

            if (wf is null)  //若没有流程正在执行
            {
                isNewWorkflow = true; // 🔥 标记为新工作流
                //创建流程及首节点
                wf = new OwWf()
                {
                    DocId = model.DocId,
                    TemplateId = model.TemplateId,
                    State = 0,
                };

                var firstNodes = _WfManager.GetFirstNodes(template).ToList();
                // 🔧 增强验证：检查首节点配置
                if (!firstNodes.Any())
                {
                    _Logger.LogError(
                     "工作流模板配置错误：未找到首节点！TemplateId={TemplateId}, 所有节点都被其他节点的NextId引用",
                      template.Id);
                    return BadRequest("工作流模板配置错误：未找到首节点，请联系系统管理员检查模板配置");
                }

                ttCurrentNode = firstNodes.SingleOrDefault(c => _WfManager.Contains(context.User.Id, c)); //首节点模板

                // 🔧 增强验证：检查当前用户是否在首节点审批人列表中
                if (ttCurrentNode == null)
                {
                    var firstNodeNames = string.Join(", ", firstNodes.Select(n => n.DisplayName));
                    _Logger.LogWarning(
                       "当前用户不在首节点审批人列表中：User={UserId}, FirstNodes=[{FirstNodeNames}]",
                             context.User.Id, firstNodeNames);
                    return BadRequest($"当前用户不在首节点审批人列表中，无法发起流程。首节点：{firstNodeNames}");
                }

                _Logger.LogInformation(
            "创建首节点：NodeTemplateId={NodeTemplateId}, NodeDisplayName={NodeDisplayName}",
            ttCurrentNode.Id, ttCurrentNode.DisplayName);

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
                _Logger.LogWarning(
                            "尝试操作已结束的工作流：WfId={WfId}, State={State}, DocId={DocId}",
              wf.Id, wf.State, model.DocId);
                return BadRequest("文档所处流程已经结束。");
            }
            
            currentNode ??= wf.Children.OrderBy(c => c.ArrivalDateTime).Last();   //当前节点
            ttCurrentNode = _DbContext.WfTemplateNodes.Find(currentNode.TemplateId);  //当前节点的模板

            // 🔧 增强验证：检查当前节点模板是否存在
            if (ttCurrentNode == null)
            {
                _Logger.LogError(
              "工作流节点模板不存在：NodeId={NodeId}, TemplateId={TemplateId}",
                   currentNode.Id, currentNode.TemplateId);
                return BadRequest("工作流节点模板不存在，流程数据可能已损坏");
            }

            _Logger.LogInformation(
           "当前节点信息：NodeId={NodeId}, NodeTemplateId={NodeTemplateId}, NodeDisplayName={NodeDisplayName}, NextId={NextId}",
               currentNode.Id, ttCurrentNode.Id, ttCurrentNode.DisplayName, ttCurrentNode.NextId);

            var currentNodeItem = currentNode.Children.FirstOrDefault(c => c.OperationKind == 0 && c.OpertorId == context.User.Id); //当前审批人
            if (currentNodeItem is null)
            {
                _Logger.LogWarning(
               "非法的投递目标：当前用户不在当前节点审批人列表中，NodeId={NodeId}, User={UserId}",
              currentNode.Id, context.User.Id);
                return BadRequest("非法的投递目标");
            }

            if (model.NextOpertorId is Guid nextOpertorId)    //若需要流转
            {
                // 🔧 增强验证：检查是否有下一个节点
                if (ttCurrentNode.NextId == null)
                {
                    _Logger.LogWarning(
                           "客户端传入了NextOpertorId但当前节点没有下一节点：DocId={DocId}, CurrentNode={CurrentNode}, NextOpertorId={NextOpertorId}",
                model.DocId, ttCurrentNode.Id, nextOpertorId);
                    return BadRequest("当前节点已是最后一个节点，无法流转。请使用Approval参数结束流程。");
                }

                currentNodeItem.IsSuccess = true;
                currentNodeItem.Comment = model.Comment;

                var nextTItem = _DbContext.WfTemplateNodeItems.FirstOrDefault(c => c.ParentId == ttCurrentNode.NextId &&
               c.OpertorId == nextOpertorId);    //下一个操作人的模板

                // 🔧 增强诊断：详细记录验证失败原因
                if (nextTItem == null)
                {
                    var nextNodeId = ttCurrentNode.NextId.Value;
                    var nextNode = _DbContext.WfTemplateNodes
                   .Include(n => n.Children)
                    .FirstOrDefault(n => n.Id == nextNodeId);

                    if (nextNode == null)
                    {
                        _Logger.LogError(
                           "工作流模板配置错误：NextId指向的节点不存在！CurrentNode={CurrentNode}, NextId={NextId}",
              ttCurrentNode.Id, nextNodeId);
                        return BadRequest($"工作流模板配置错误：下一个节点不存在（NextId={nextNodeId}），请联系系统管理员");
                    }

                    var validApprovers = nextNode.Children.Where(c => c.OperationKind == 0).Select(c => c.OpertorId).ToList();
                    var validApproverNames = _DbContext.Accounts
                    .Where(a => validApprovers.Contains(a.Id))
               .Select(a => a.DisplayName)
                   .ToList();

                    _Logger.LogWarning(
          "客户端传入的NextOpertorId不在下一节点审批人列表中：NextOpertorId={NextOpertorId}, NextNode={NextNode}, ValidApprovers=[{ValidApprovers}]",
           nextOpertorId, nextNode.DisplayName, string.Join(", ", validApproverNames));

                    return BadRequest($"指定下一个操作人Id={model.NextOpertorId},但它不是合法的下一个操作人。下一节点『{nextNode.DisplayName}』的有效审批人：{string.Join("、", validApproverNames)}");
                }

                var nextTNode = nextTItem.Parent;    //下一个节点模板

                _Logger.LogInformation(
                  "流转到下一节点：CurrentNode={CurrentNode}, NextNode={NextNode}, NextNodeName={NextNodeName}, NextOpertor={NextOpertor}",
                 ttCurrentNode.Id, nextTNode.Id, nextTNode.DisplayName, nextOpertorId);

                var nextNode2 = new OwWfNode
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
                    Parent = nextNode2,
                    ParentId = nextNode2.Id,
                };
                nextNode2.Children.Add(nextItem);
                wf.Children.Add(nextNode2);
            }
            else //流程结束
            {
                // 🔧 增强日志：记录流程结束
                _Logger.LogInformation(
              "工作流流程结束：WfId={WfId}, DocId={DocId}, CurrentNode={CurrentNode}, Approval={Approval}, TotalNodes={TotalNodes}",
                     wf.Id, model.DocId, ttCurrentNode.Id, model.Approval, wf.Children.Count);

                // 🔧 增强验证：检查是否还有下一个节点但客户端未传NextOpertorId
                if (ttCurrentNode.NextId != null)
                {
                    _Logger.LogWarning(
                "⚠️ 潜在的客户端错误：当前节点还有下一节点（NextId={NextId}），但客户端未传NextOpertorId，流程将提前结束。" +
           "这可能是因为：1) 下一节点未配置审批人；2) 前端调用GetNextNodeItemsByDocId返回空列表；3) 前端逻辑错误。",
                    ttCurrentNode.NextId);

                    // 检查下一节点是否有审批人
                    var nextNode = _DbContext.WfTemplateNodes
              .Include(n => n.Children)
                   .FirstOrDefault(n => n.Id == ttCurrentNode.NextId);
                    if (nextNode != null)
                    {
                        var hasApprovers = nextNode.Children.Any(c => c.OperationKind == 0);
                        if (!hasApprovers)
                        {
                            _Logger.LogWarning(
                                "⚠️ 工作流模板配置问题：下一节点『{NextNodeName}』(Id={NextNodeId})未配置审批人，导致流程无法继续",
                                    nextNode.DisplayName, nextNode.Id);
                        }
                        else
                        {
                            var approverCount = nextNode.Children.Count(c => c.OperationKind == 0);
                            _Logger.LogWarning(
                         "⚠️ 客户端错误：下一节点『{NextNodeName}』(Id={NextNodeId})有{ApproverCount}个审批人，但客户端未选择并传递NextOpertorId",
                             nextNode.DisplayName, nextNode.Id, approverCount);
                        }
                    }
                }

                // 修复审批意见丢失问题 - 保持与其他分支的一致性
                currentNodeItem.Comment = model.Comment;
                if (model.Approval == 0)   //若通过
                {
                    wf.State = 1;
                    currentNodeItem.IsSuccess = true;
                    _Logger.LogInformation("工作流成功完成：WfId={WfId}, TotalNodes={TotalNodes}", wf.Id, wf.Children.Count);
                }
                else if (model.Approval == 1) //若拒绝
                {
                    wf.State = 2;
                    currentNodeItem.IsSuccess = false;
                    _Logger.LogInformation("工作流被终止：WfId={WfId}, TotalNodes={TotalNodes}", wf.Id, wf.Children.Count);
                }
                else
                    return BadRequest($"{nameof(model.Approval)} 参数值非法。");
            }
            
            _DbContext.SaveChanges();

            // 🔥 新增：根据工作流状态自动同步业务单据状态
            try
            {
                if (isNewWorkflow)
                {
                    // 首次创建工作流：Draft → InApproval
                    SyncDocumentStatusOnWorkflowStart(model.DocId, template.KindCode);
                }
                else if (wf.State != 0)
                {
                    // 工作流结束：InApproval → ApprovedPendingSettlement 或 Draft
                    SyncDocumentStatusOnWorkflowComplete(model.DocId, template.KindCode, wf.State);
                }
                
                _DbContext.SaveChanges(); // 保存状态同步更改
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "同步业务单据状态失败，但工作流操作已成功：DocId={DocId}, KindCode={KindCode}",
                    model.DocId, template.KindCode);
                // 不抛出异常，避免影响工作流主流程
            }

            result.WfId = wf.Id;

            _Logger.LogInformation(
                "工作流操作成功：WfId={WfId}, State={State}, TotalNodes={TotalNodes}",
            wf.Id, wf.State, wf.Children.Count);

            return result;
        }

        #region 业务单据状态同步私有方法

        /// <summary>
        /// 工作流启动时同步业务单据状态（Draft → InApproval）
        /// </summary>
        private void SyncDocumentStatusOnWorkflowStart(Guid? docId, string kindCode)
        {
            if (!docId.HasValue) return;

            switch (kindCode)
            {
                case "OA_expense_reimb": // OA费用报销
                case "OA_expense_loan":  // OA费用借款
                case "OA_exchange_income": // OA外汇收入
                case "OA_exchange_expense": // OA外汇支出
                    SyncOaExpenseStatusOnStart(docId.Value);
                    break;

                case "Fee_requisition": // 主营业务费用申请
                    // 主营业务费用申请单没有独立的Status字段，状态由工作流管理
                    _Logger.LogDebug("主营业务费用申请单状态由工作流管理，无需同步Status字段");
                    break;

                default:
                    _Logger.LogDebug("未知的KindCode: {KindCode}，跳过状态同步", kindCode);
                    break;
            }
        }

        /// <summary>
        /// 工作流完成时同步业务单据状态（InApproval → ApprovedPendingSettlement 或 Draft）
        /// </summary>
        private void SyncDocumentStatusOnWorkflowComplete(Guid? docId, string kindCode, byte wfState)
        {
            if (!docId.HasValue) return;

            switch (kindCode)
            {
                case "OA_expense_reimb":
                case "OA_expense_loan":
                case "OA_exchange_income":
                case "OA_exchange_expense":
                    SyncOaExpenseStatusOnComplete(docId.Value, wfState);
                    break;

                case "Fee_requisition":
                    _Logger.LogDebug("主营业务费用申请单状态由工作流管理，无需同步Status字段");
                    break;

                default:
                    _Logger.LogDebug("未知的KindCode: {KindCode}，跳过状态同步", kindCode);
                    break;
            }
        }

        /// <summary>
        /// OA申请单工作流启动时的状态同步
        /// </summary>
        private void SyncOaExpenseStatusOnStart(Guid requisitionId)
        {
            var requisition = _DbContext.OaExpenseRequisitions.Find(requisitionId);
            if (requisition == null)
            {
                _Logger.LogWarning("OA申请单不存在，无法同步状态：RequisitionId={RequisitionId}", requisitionId);
                return;
            }

            if (requisition.Status == OaExpenseStatus.Draft)
            {
                var oldStatus = requisition.Status;
                requisition.Status = OaExpenseStatus.InApproval;
                
                _Logger.LogInformation(
                    "✅ 工作流启动，OA申请单状态同步：RequisitionId={RequisitionId}, {OldStatus} → {NewStatus}",
                    requisitionId, oldStatus, requisition.Status);
            }
            else
            {
                _Logger.LogWarning(
                    "⚠️ OA申请单状态异常：预期为Draft(0)，实际为{CurrentStatus}，跳过状态同步",
                    requisition.Status);
            }
        }

        /// <summary>
        /// OA申请单工作流完成时的状态同步
        /// </summary>
        private void SyncOaExpenseStatusOnComplete(Guid requisitionId, byte wfState)
        {
            var requisition = _DbContext.OaExpenseRequisitions.Find(requisitionId);
            if (requisition == null)
            {
                _Logger.LogWarning("OA申请单不存在，无法同步状态：RequisitionId={RequisitionId}", requisitionId);
                return;
            }

            var oldStatus = requisition.Status;

            switch (wfState)
            {
                case 1: // 工作流成功完成
                    if (requisition.Status == OaExpenseStatus.InApproval)
                    {
                        requisition.Status = OaExpenseStatus.ApprovedPendingSettlement;
                        requisition.AuditDateTime = OwHelper.WorldNow;
                        // 审批人ID由GetAllOaExpenseRequisitionWithWf自动同步时从工作流中提取
                        
                        _Logger.LogInformation(
                            "✅ 工作流审批通过，OA申请单状态同步：RequisitionId={RequisitionId}, {OldStatus} → {NewStatus}",
                            requisitionId, oldStatus, requisition.Status);
                    }
                    else
                    {
                        _Logger.LogWarning(
                            "⚠️ OA申请单状态异常：工作流已完成但申请单状态为{CurrentStatus}，预期为InApproval(1)",
                            requisition.Status);
                    }
                    break;

                case 2: // 工作流被终止（审批拒绝）
                    if (requisition.Status == OaExpenseStatus.InApproval)
                    {
                        requisition.Status = OaExpenseStatus.Rejected; // 🔥 修改：设置为被拒绝状态而非回退到草稿
                        requisition.AuditDateTime = null;
                        requisition.AuditOperatorId = null;
                        
                        _Logger.LogInformation(
                            "✅ 工作流被拒绝，OA申请单状态同步：RequisitionId={RequisitionId}, {OldStatus} → Rejected(32)",
                            requisitionId, oldStatus);
                    }
                    else
                    {
                        _Logger.LogWarning(
                            "⚠️ OA申请单状态异常：工作流被终止但申请单状态为{CurrentStatus}，预期为InApproval(1)",
                            requisition.Status);
                    }
                    break;

                default:
                    _Logger.LogWarning("未知的工作流状态：WfState={WfState}", wfState);
                    break;
            }
        }

        #endregion 业务单据状态同步私有方法
    }
}
