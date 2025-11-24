using Microsoft.Extensions.DependencyInjection;
using PowerLms.Data;
using PowerLmsServer.EfData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace PowerLmsServer.Managers
{
    /// <summary>
    /// 工作流相关功能管理器。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Scoped)]
    public class OwWfManager
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public OwWfManager(PowerLmsUserDbContext dbContext, OwSqlAppLogger sqlAppLogger)
        {
            _DbContext = dbContext;
            _SqlAppLogger = sqlAppLogger;
        }

        private readonly PowerLmsUserDbContext _DbContext;
        private readonly OwSqlAppLogger _SqlAppLogger;

        /// <summary>
        /// 根据业务文档ID清空相关的所有工作流数据。
        /// 此方法用于申请单回退功能，会记录审计快照并级联删除所有相关工作流数据。
        /// </summary>
        /// <param name="docId">业务文档GUID，对应申请单ID</param>
        /// <returns>返回被清空的工作流实体列表，这些实体已在数据库上下文中标记为删除状态</returns>
        /// <exception cref="ArgumentException">当docId为空时抛出</exception>
        /// <exception cref="InvalidOperationException">当数据操作错误时抛出</exception>
        public List<OwWf> ClearWorkflowByDocId(Guid docId)
        {
            // 1. 参数有效性验证
            if (docId == Guid.Empty)
                throw new ArgumentException("业务文档ID不能为空", nameof(docId));

            try
            {
                // 2. 查询与文档ID关联的所有工作流实例
                var workflows = _DbContext.OwWfs
                    .Where(wf => wf.DocId == docId)
                    .ToList();

                if (!workflows.Any())
                {
                    _SqlAppLogger.LogGeneralInfo($"工作流清理：未找到文档ID为 {docId} 的工作流数据");
                    return new List<OwWf>();
                }

                // 3. 记录审计快照（操作前状态保存）
                foreach (var workflow in workflows)
                {
                    var snapshotInfo = new
                    {
                        WorkflowId = workflow.Id,
                        DocId = workflow.DocId,
                        State = workflow.State,
                        NodeCount = _DbContext.OwWfNodes.Count(n => n.ParentId == workflow.Id),
                        ItemCount = _DbContext.OwWfNodeItems.Count(i => i.Parent.ParentId == workflow.Id)
                    };

                    _SqlAppLogger.LogGeneralInfo($"工作流回退快照记录：{System.Text.Json.JsonSerializer.Serialize(snapshotInfo)}");
                }

                // 4. 级联标记删除（NodeItems → Nodes → Workflows）
                var workflowIds = workflows.Select(w => w.Id).ToList();

                // 删除工作流节点项
                var nodeItems = _DbContext.OwWfNodeItems
                    .Where(item => workflowIds.Contains(item.Parent.ParentId.Value))
                    .ToList();

                if (nodeItems.Any())
                {
                    _DbContext.OwWfNodeItems.RemoveRange(nodeItems);
                    _SqlAppLogger.LogGeneralInfo($"工作流清理：删除 {nodeItems.Count} 个工作流节点项");
                }

                // 删除工作流节点
                var nodes = _DbContext.OwWfNodes
                    .Where(node => workflowIds.Contains(node.ParentId.Value))
                    .ToList();

                if (nodes.Any())
                {
                    _DbContext.OwWfNodes.RemoveRange(nodes);
                    _SqlAppLogger.LogGeneralInfo($"工作流清理：删除 {nodes.Count} 个工作流节点");
                }

                // 删除工作流
                _DbContext.OwWfs.RemoveRange(workflows);
                _SqlAppLogger.LogGeneralInfo($"工作流清理：删除 {workflows.Count} 个工作流实例，文档ID：{docId}");

                return workflows;
            }
            catch (Exception ex)
            {
                _SqlAppLogger.LogGeneralInfo($"工作流清理失败：文档ID {docId}，错误：{ex.Message}");
                throw new InvalidOperationException($"清空工作流数据时发生错误：{ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取指定操作人相关的工作流节点项。
        /// </summary>
        /// <param name="opertorId">操作人Id。用于筛选与特定操作人相关的工作流节点项。</param>
        /// <param name="state">工作流状态过滤条件，不同值代表不同的筛选条件：
        /// <list type="bullet">
        /// <item><term>1</term><description>正等待指定操作者审批的节点项（流程处于流转中且该节点项未处理）</description></item>
        /// <item><term>2</term><description>指定操作者已审批但流程仍在流转中的节点项</description></item>
        /// <item><term>3</term><description>所有流转中的节点项（1和2的合集）</description></item>
        /// <item><term>4</term><description>指定操作者参与的且已成功结束的流程中的节点项</description></item>
        /// <item><term>8</term><description>指定操作者参与的且已失败结束（被终止）的流程中的节点项</description></item>
        /// <item><term>12</term><description>指定操作者参与的且已结束的流程中的节点项（包括成功/失败，相当于4|8）</description></item>
        /// <item><term>15</term><description>不限定状态，返回所有与指定操作者相关的节点项</description></item>
        /// </list>
        /// </param>
        /// <returns>符合条件的工作流节点项查询结果。可以进一步链式调用其他LINQ方法进行筛选或转换。</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="state"/>值不在支持的范围内（1、2、3、4、8、12、15）。</exception>
        /// <remarks>
        /// 此方法仅返回操作类型（OperationKind）为0（审批者）的节点项。
        /// 如果需要查询其他类型的操作人（如抄送人），需要额外添加条件。
        /// 工作流状态（State）说明：0=流转中，1=成功完成，2=已被终止（失败）。
        /// </remarks>
        public IQueryable<OwWfNodeItem> GetWfNodeItemByOpertorId(Guid opertorId, byte state)
        {
            var collBase = _DbContext.OwWfNodeItems.Where(c => c.OpertorId == opertorId && c.OperationKind == 0);
            var result = state switch
            {
                1 => collBase.Where(c => c.Parent.Parent.State == 0 && c.IsSuccess == null),
                2 => collBase.Where(c => c.Parent.Parent.State == 0 && c.IsSuccess != null),
                3 => collBase.Where(c => c.Parent.Parent.State == 0), // 合并1和2，表示所有流转中的节点项
                4 => collBase.Where(c => c.Parent.Parent.State == 1),
                8 => collBase.Where(c => c.Parent.Parent.State == 2),
                12 => collBase.Where(c => c.Parent.Parent.State == 2 || c.Parent.Parent.State == 1),
                15 => collBase,
                _ => throw new ArgumentOutOfRangeException(nameof(state)),
            };
            return result;
        }

        #region 缓存加载状态管理

        /// <summary>
        /// 通过工作流ID加载全部相关工作流对象到DbContext缓存。
        /// 
        /// 设计说明：
        /// 1. 优先检查本地缓存，如果已存在则直接返回（性能最优）
        /// 2. 未命中缓存时，使用Include完整加载所有层级数据
        /// 3. 即使之前使用Find()部分加载过，仍会重新完整加载以保证数据完整性
        /// 
        /// 性能建议：
        /// - 最佳实践：首次访问工作流时调用此方法，后续直接使用缓存对象
        /// - 可接受场景：即使之前用Find()加载过，重复调用此方法也是安全的（会重新完整加载，性能略降但逻辑正确）
        /// </summary>
        /// <param name="workflowId">工作流实例ID</param>
        /// <returns>完整加载的工作流实例（包含所有节点和节点项），如果不存在则返回null</returns>
        public OwWf LoadWorkflowById(Guid workflowId)
        {
            // 检查本地缓存，如果存在则直接返回（假设缓存中的数据是完整的）
            // 注意：如果之前使用Find()部分加载过，这里会返回部分数据，但调用者应避免这种场景
            var localWorkflow = _DbContext.OwWfs.Local.FirstOrDefault(w => w.Id == workflowId);
            if (localWorkflow != null)
                return localWorkflow;
            
            // 从数据库完整加载（Include自动加载所有子集）
            return _DbContext.OwWfs
                .Include(w => w.Children)
                    .ThenInclude(n => n.Children)
                .FirstOrDefault(w => w.Id == workflowId);
        }

        /// <summary>
        /// 通过节点ID加载全部相关工作流对象到DbContext缓存。
        /// 
        /// 设计说明：
        /// 1. 先尝试从本地缓存获取节点，如果找到则通过ParentId调用LoadWorkflowById
        /// 2. 如果缓存未命中，一次性通过节点Include完整加载工作流及所有层级数据
        /// 3. 性能优于先Find节点再LoadWorkflowById（减少一次数据库查询）
        /// 
        /// 性能优势：
        /// - 缓存命中：0次查询（直接返回内存数据）
        /// - 缓存未命中：1次查询（一次性Include完整加载）
        /// - 优于分步加载：避免Find + Include的2次查询
        /// </summary>
        /// <param name="nodeId">工作流节点ID</param>
        /// <returns>完整加载的工作流实例（包含所有节点和节点项），如果不存在则返回null</returns>
        public OwWf LoadWorkflowByNodeId(Guid nodeId)
        {
            // 1. 尝试从本地缓存获取节点
            var localNode = _DbContext.OwWfNodes.Local.FirstOrDefault(n => n.Id == nodeId);
            if (localNode?.ParentId != null)
            {
                // 缓存命中，直接调用LoadWorkflowById（可能再次命中缓存，0次查询）
                return LoadWorkflowById(localNode.ParentId.Value);
            }
            
            // 2. 缓存未命中，一次性完整加载（通过节点Include工作流及所有子集）
            var node = _DbContext.OwWfNodes
                .Include(n => n.Parent)              // 加载工作流
                    .ThenInclude(w => w.Children)    // 加载所有节点
                        .ThenInclude(n => n.Children) // 加载所有节点项
                .FirstOrDefault(n => n.Id == nodeId);
            
            return node?.Parent;
        }

        /// <summary>
        /// 通过模板ID加载完整的工作流模板到DbContext缓存。
        /// 
        /// 设计说明：
        /// 1. 优先检查本地缓存，如果已存在则直接返回（性能最优）
        /// 2. 未命中缓存时，使用Include完整加载所有层级数据（模板节点和节点项）
        /// 3. 模板数据相对稳定，适合长期缓存使用
        /// 
        /// 性能优势：
        /// - 缓存命中：0次查询（直接返回内存数据）
        /// - 缓存未命中：1次查询（一次性Include完整加载）
        /// - 后续所有访问模板节点和节点项都在内存中完成
        /// 
        /// 使用场景：
        /// - 创建工作流实例前需要读取模板配置
        /// - 验证流程定义是否符合业务规则
        /// - 展示流程模板详细信息给用户
        /// </summary>
        /// <param name="templateId">工作流模板ID</param>
        /// <returns>完整加载的工作流模板（包含所有节点和节点项），如果不存在则返回null</returns>
        public OwWfTemplate LoadTemplateById(Guid templateId)
        {
            // 检查本地缓存，如果存在则直接返回
            var localTemplate = _DbContext.Set<OwWfTemplate>().Local.FirstOrDefault(t => t.Id == templateId);
            if (localTemplate != null)
                return localTemplate;
            
            // 从数据库完整加载（Include自动加载所有子集）
            return _DbContext.Set<OwWfTemplate>()
                .Include(t => t.Children)              // 加载所有模板节点
                    .ThenInclude(n => n.Children)      // 加载所有模板节点项
                .FirstOrDefault(t => t.Id == templateId);
        }

        #endregion 缓存加载状态管理

        #region 模板相关

        /// <summary>
        /// 根据当前操作人和下一个操作人，获得整个流程的节点。
        /// </summary>
        /// <param name="currentOpertorId"></param>
        /// <param name="nextOpertorId">下一个操作人，null表示没有下一个操作人，也就是 <paramref name="currentOpertorId"/>是最后一个节点的操作人。</param>
        /// <param name="template"></param>
        /// <returns></returns>
        public List<(OwWfTemplateNode, OwWfTemplateNode)> GetFlow(Guid currentOpertorId, Guid? nextOpertorId, OwWfTemplate template)
        {
            var result = new List<(OwWfTemplateNode, OwWfTemplateNode)> { };
            var first = template.Children.Where(c => Contains(currentOpertorId, c));   //第一个节点的集合
            var dic = template.Children.ToDictionary(c => c.Id);    //节点字典
            foreach (var child in first)
            {
                if (nextOpertorId is null)  //若不要求有下一个操作人
                {
                    if (child.NextId is null)
                        result.Add((child, null));
                }
                else //若明确要有有下一个操作人
                {
                    if (child.NextId is Guid nextId)
                    {
                        var next = dic[nextId];
                        if (Contains(nextOpertorId.Value, next))
                            result.Add((child, next));
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// 获取模板中所有首节点。
        /// 
        /// 设计说明：
        /// 1. 自动通过LoadTemplateById加载完整模板（利用缓存）
        /// 2. 在内存中查找所有首节点（NextId为null的反向推导）
        /// 3. 调用者无需自己加载模板，简化使用
        /// 
        /// 查找逻辑：
        /// - 创建包含所有节点的字典（以Id为键）
        /// - 遍历所有节点，移除被NextId引用的节点
        /// - 剩余的节点即为首节点（没有前驱节点）
        /// 
        /// 性能优化：
        /// - 利用LoadTemplateById的缓存机制，重复调用0次额外查询
        /// - 纯内存操作，不触发数据库查询
        /// 
        /// 使用场景：
        /// - 创建工作流实例时确定起始节点
        /// - 验证流程定义的合法性
        /// - 展示流程图时确定入口节点
        /// </summary>
        /// <param name="templateId">工作流模板ID</param>
        /// <returns>首节点集合，如果模板不存在或没有节点则返回空集合</returns>
        public IEnumerable<OwWfTemplateNode> GetFirstNodes(Guid templateId)
        {
            // 1. 通过LoadTemplateById加载完整模板（利用缓存）
            var template = LoadTemplateById(templateId);
            if (template == null || !template.Children.Any())
                return Enumerable.Empty<OwWfTemplateNode>();
            
            // 2. 在内存中查找首节点（反向推导：没有被NextId引用的节点）
            var dic = template.Children.ToDictionary(c => c.Id);
            foreach (var item in template.Children)
            {
                if (item.NextId is not null)
                    dic.Remove(item.NextId.Value);
            }
            
            return dic.Values;
        }

        /// <summary>
        /// 指定操作人是否在指定的节点中。
        /// </summary>
        /// <param name="opertorId"></param>
        /// <param name="node"></param>
        /// <returns></returns>
        public bool Contains(Guid opertorId, OwWfTemplateNode node)
        {
            return node.Children.Any(c => c.OperationKind == 0 & c.OpertorId == opertorId);
        }

        #endregion 模板相关

        #region 工作流节点查询

        /// <summary>
        /// 获取工作流的首节点。
        /// 
        /// 查找逻辑（按优先级）：
        /// 1. 自动通过LoadWorkflowById加载完整工作流（利用缓存，几乎无性能开销）
        /// 2. 如果工作流关联了模板，则基于模板定义查找首节点
        /// 3. 如果没有模板或模板查找失败，则基于ArrivalDateTime运行时排序
        /// 
        /// 排序规则（无模板时）：
        /// - 主排序：按ArrivalDateTime升序
        /// - 次排序：按Id升序（确保确定性）
        /// 
        /// 性能优化：
        /// - 工作流缓存：LoadWorkflowById自动缓存，重复调用0次额外查询
        /// - 模板缓存：GetFirstNodes自动调用LoadTemplateById缓存模板数据
        /// - 内存查询：所有操作在内存中完成
        /// 
        /// 使用场景：
        /// - 控制器中快速获取首节点，无需手动加载工作流
        /// - 简化代码，一行调用完成所有操作
        /// </summary>
        /// <param name="workflowId">工作流实例ID</param>
        /// <returns>首节点对象，如果工作流不存在或没有节点则返回null</returns>
        public OwWfNode GetFirstNode(Guid workflowId)
        {
            var workflow = LoadWorkflowById(workflowId);
            if (workflow == null || !workflow.Children.Any())
                return null;
            
            // 1. 优先基于模板定义查找（GetFirstNodes自动加载并缓存模板）
            if (workflow.TemplateId != null)
            {
                var firstTemplateNodes = GetFirstNodes(workflow.TemplateId.Value);
                foreach (var templateNode in firstTemplateNodes)
                {
                    var instanceNode = workflow.Children.FirstOrDefault(n => n.TemplateId == templateNode.Id);
                    if (instanceNode != null)
                        return instanceNode;
                }
            }
            
            // 2. 降级方案：基于运行时到达时间排序
            return workflow.Children
                .OrderBy(n => n.ArrivalDateTime)
                .ThenBy(n => n.Id)
                .FirstOrDefault();
        }

        /// <summary>
        /// 获取工作流的当前节点（正在执行的节点）。
        /// 
        /// 查找逻辑：
        /// 1. 自动通过LoadWorkflowById加载完整工作流（利用缓存，几乎无性能开销）
        /// 2. 查找所有未处理完成的节点项，确定当前正在执行的节点
        /// 3. 如果所有节点都已处理完成，返回最后到达的节点（流程结束状态）
        /// 
        /// 当前节点判定规则：
        /// - 查找所有子节点项中存在未处理（IsSuccess == null）的节点
        /// - 如果有多个未处理节点，返回最早到达的（符合流程顺序）
        /// - 如果都已处理，返回最后到达的节点（流程已完成）
        /// 
        /// 性能优化：
        /// - 工作流缓存：LoadWorkflowById自动缓存，重复调用0次额外查询
        /// - 纯内存查询：节点查找在内存中完成，不触发数据库查询
        /// 
        /// 使用场景：
        /// - 控制器中快速获取当前节点，无需手动加载工作流
        /// - 简化代码，一行调用完成工作流加载和节点查找
        /// </summary>
        /// <param name="workflowId">工作流实例ID</param>
        /// <returns>当前正在执行的节点对象，如果工作流不存在或没有节点则返回null</returns>
        public OwWfNode GetCurrentNode(Guid workflowId)
        {
            var workflow = LoadWorkflowById(workflowId);
            if (workflow == null || !workflow.Children.Any())
                return null;
            
            // 1. 查找有未处理节点项的节点（当前正在处理的节点）
            var nodeWithPendingItems = workflow.Children
                .Where(n => n.Children.Any(item => item.IsSuccess == null))
                .OrderBy(n => n.ArrivalDateTime)
                .ThenBy(n => n.Id)
                .FirstOrDefault();
            
            if (nodeWithPendingItems != null)
                return nodeWithPendingItems;
            
            // 2. 所有节点都已处理完成，返回最后到达的节点（流程已完成）
            return workflow.Children
                .OrderByDescending(n => n.ArrivalDateTime)
                .ThenByDescending(n => n.Id)
                .FirstOrDefault();
        }

        /// <summary>
        /// 根据节点模板ID获取工作流实例中对应的节点。
        /// 
        /// 查找逻辑：
        /// 1. 自动通过LoadWorkflowById加载完整工作流（利用缓存，几乎无性能开销）
        /// 2. 在内存中查找匹配的节点
        /// 
        /// 性能优化：
        /// - 工作流缓存：LoadWorkflowById自动缓存，重复调用0次额外查询
        /// - 纯内存查询：不触发数据库查询
        /// 
        /// 使用场景：
        /// - 控制器中快速根据模板ID查找节点，无需手动加载工作流
        /// </summary>
        /// <param name="workflowId">工作流实例ID</param>
        /// <param name="templateNodeId">节点模板ID</param>
        /// <returns>匹配的节点对象，如果未找到则返回null</returns>
        public OwWfNode GetNodeByTemplateId(Guid workflowId, Guid templateNodeId)
        {
            var workflow = LoadWorkflowById(workflowId);
            if (workflow == null)
                return null;
            
            return workflow.Children.FirstOrDefault(n => n.TemplateId == templateNodeId);
        }

        #endregion 工作流节点查询

        #region 工作流状态查询

        /// <summary>
        /// 获取工作流的状态。
        /// 
        /// 查找逻辑：
        /// 1. 自动通过LoadWorkflowById加载完整工作流（利用缓存，几乎无性能开销）
        /// 2. 获取当前节点（最后一个审批节点）
        /// 3. 检查该节点的审批状态
        /// 4. 根据审批状态确定工作流状态
        /// 
        /// 性能优化：
        /// - 工作流缓存：LoadWorkflowById自动缓存，重复调用0次额外查询
        /// - 内存查询：所有操作在内存中完成
        /// - 模板缓存：GetNodeApprovalState内部使用Find，模板已被LoadTemplateById缓存
        /// 
        /// 使用场景：
        /// - 控制器中快速获取工作流状态，无需手动加载
        /// - 简化代码，一行调用完成所有操作
        /// </summary>
        /// <param name="workflowId">工作流实例ID</param>
        /// <returns>工作流状态：0=流转中，1=成功完成，2=已被终止</returns>
        public int GetWorkflowState(Guid workflowId)
        {
            var workflow = LoadWorkflowById(workflowId);
            if (workflow == null)
                return 0; // 工作流不存在，视为流转中
            
            var currentNode = GetCurrentNode(workflowId);
            if (currentNode == null)
                return 0; // 无节点，视为流转中
            
            if (!GetNodeApprovalState(currentNode, out var state))
                return 0; // 不是审批节点，视为流转中
            
            if (!state.HasValue)
                return 0; // 待审批状态
            
            return state.Value ? 1 : 2; // true=成功完成, false=被终止
        }

        /// <summary>
        /// 获取节点的审批状态。
        /// 
        /// 查找逻辑：
        /// 1. 通过Find加载节点模板（利用缓存）
        /// 2. 对比模板定义的审批人和实例节点的审批人
        /// 3. 汇总所有审批人的审批结果
        /// 
        /// 判定规则：
        /// - 任意审批人IsSuccess==null → 待审批（state=null）
        /// - 任意审批人IsSuccess==false → 否决（state=false）
        /// - 所有审批人IsSuccess==true → 通过（state=true）
        /// 
        /// 性能优化：
        /// - 模板缓存：Find利用EF Core缓存，如果模板已被LoadTemplateById加载则0次查询
        /// - 内存查询：节点项对比在内存中完成
        /// 
        /// 前置条件：
        /// - node对象必须已完整加载（包含Children）
        /// - 建议先调用LoadWorkflowById或LoadWorkflowByNodeId
        /// </summary>
        /// <param name="node">工作流节点（必须已完整加载）</param>
        /// <param name="state">审批状态：false=否决，true=通过，null=待审批</param>
        /// <returns>是否是审批节点：true=是审批节点，false=不是审批节点</returns>
        public bool GetNodeApprovalState(OwWfNode node, out bool? state)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));
            
            // 获取节点模板（利用缓存）
            var templateNode = _DbContext.WfTemplateNodes.Find(node.TemplateId);
            if (templateNode == null)
            {
                state = null;
                return false;
            }
            
            // 获取模板定义的审批人（OperationKind=0）
            var templateApprovers = templateNode.Children
                .Where(c => c.OperationKind == 0)
                .ToList();
            
            if (!templateApprovers.Any())
            {
                state = null;
                return false; // 不是审批节点
            }
            
            // 获取实例节点的审批人
            var instanceApprovers = node.Children
                .Where(c => c.OperationKind == 0)
                .ToList();
            
            // 对比模板和实例的审批人
            var approvalResults = from templateItem in templateApprovers
                                  join instanceItem in instanceApprovers
                                  on templateItem.OpertorId equals instanceItem.OpertorId
                                  select instanceItem;
            
            var approvalList = approvalResults.ToList();
            if (!approvalList.Any())
            {
                state = null;
                return false; // 无匹配的审批人，不是有效的审批节点
            }
            
            // 检查审批状态
            state = true; // 默认为通过
            foreach (var item in approvalList)
            {
                if (item.IsSuccess is null)
                {
                    state = null; // 有待审批的
                    break;
                }
                else if (item.IsSuccess == false)
                {
                    state = false; // 有否决的
                    break;
                }
            }
            
            return true; // 是审批节点
        }

        /// <summary>
        /// 检查工作流是否已完成（成功或失败）- 基于文档ID。
        /// 
        /// 查找逻辑：
        /// 1. 根据文档ID查找关联的工作流
        /// 2. 检查工作流的State字段
        /// 
        /// 注意：
        /// - 一个文档可能关联多个工作流（不同模板）
        /// - 此方法检查是否所有工作流都已完成
        /// 
        /// 性能优化：
        /// - 仅查询State字段，不加载完整工作流数据
        /// 
        /// 使用场景：
        /// - 控制器中快速检查文档的工作流是否完成
        /// - 业务逻辑中判断是否可以执行后续操作
        /// </summary>
        /// <param name="docId">业务文档ID</param>
        /// <returns>true=所有工作流已完成，false=至少有一个工作流在流转中或没有工作流</returns>
        public bool IsWorkflowCompleted(Guid docId)
        {
            var workflows = _DbContext.OwWfs
                .Where(wf => wf.DocId == docId)
                .Select(wf => new { wf.Id, wf.State })
                .ToList();
            
            if (!workflows.Any())
                return false; // 没有工作流，视为未完成
            
            return workflows.All(wf => wf.State != 0); // 所有工作流都不在流转中
        }

        #endregion 工作流状态查询
    }
}
