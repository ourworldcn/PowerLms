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
        /// 完整加载指定的工作流实例及其所有子节点和节点项到DbContext缓存。
        /// </summary>
        /// <param name="workflowId">工作流实例ID</param>
        /// <returns>完整加载的工作流实例，如果不存在则返回null</returns>
        /// <remarks>
        /// 加载策略：
        /// - 一级：OwWf（工作流实例）
        /// - 二级：OwWfNode（工作流节点）
        /// - 三级：OwWfNodeItem（节点项）
        /// 加载后所有相关实体都会进入DbContext的本地缓存，后续访问不会触发数据库查询。
        /// </remarks>
        public OwWf LoadWorkflowWithAllChildren(Guid workflowId)
        {
            var workflow = _DbContext.OwWfs
                .Include(w => w.Children)
                    .ThenInclude(n => n.Children)
                .FirstOrDefault(w => w.Id == workflowId);
            if (workflow != null)
            {
                var entry = _DbContext.Entry(workflow);
                entry.Collection(w => w.Children).IsLoaded = true;
                foreach (var node in workflow.Children)
                {
                    _DbContext.Entry(node).Collection(n => n.Children).IsLoaded = true;
                }
                _SqlAppLogger.LogGeneralInfo($"工作流完整加载：ID={workflowId}, 节点数={workflow.Children.Count}, 总节点项数={workflow.Children.Sum(n => n.Children.Count)}");
            }
            return workflow;
        }

        /// <summary>
        /// 批量完整加载指定文档ID关联的所有工作流实例及其子节点。
        /// </summary>
        /// <param name="docId">业务文档ID</param>
        /// <returns>完整加载的工作流实例列表</returns>
        public List<OwWf> LoadWorkflowsByDocId(Guid docId)
        {
            var workflows = _DbContext.OwWfs
                .Where(w => w.DocId == docId)
                .Include(w => w.Children)
                    .ThenInclude(n => n.Children)
                .ToList();
            foreach (var workflow in workflows)
            {
                _DbContext.Entry(workflow).Collection(w => w.Children).IsLoaded = true;
                foreach (var node in workflow.Children)
                {
                    _DbContext.Entry(node).Collection(n => n.Children).IsLoaded = true;
                }
            }
            _SqlAppLogger.LogGeneralInfo($"批量工作流加载：文档ID={docId}, 工作流数={workflows.Count}");
            return workflows;
        }

        /// <summary>
        /// 检查指定工作流实例是否已完整加载到DbContext缓存。
        /// </summary>
        /// <param name="workflow">要检查的工作流实例</param>
        /// <returns>返回加载状态信息</returns>
        public WorkflowLoadStatus GetWorkflowLoadStatus(OwWf workflow)
        {
            if (workflow == null)
                throw new ArgumentNullException(nameof(workflow));
            var entry = _DbContext.Entry(workflow);
            var status = new WorkflowLoadStatus
            {
                WorkflowId = workflow.Id,
                IsWorkflowLoaded = entry.State != EntityState.Detached,
                IsNodesLoaded = entry.Collection(w => w.Children).IsLoaded,
                LoadedNodesCount = workflow.Children?.Count ?? 0
            };
            if (status.IsNodesLoaded && workflow.Children != null)
            {
                status.NodeItemsLoadStatus = workflow.Children.Select(node => new NodeItemLoadStatus
                {
                    NodeId = node.Id,
                    IsLoaded = _DbContext.Entry(node).Collection(n => n.Children).IsLoaded,
                    ItemsCount = node.Children?.Count ?? 0
                }).ToList();
                status.AllNodeItemsLoaded = status.NodeItemsLoadStatus.All(n => n.IsLoaded);
            }
            return status;
        }

        /// <summary>
        /// 强制标记工作流及其子对象已完整加载（适用于已确认完整加载的场景）。
        /// </summary>
        /// <param name="workflow">工作流实例</param>
        public void MarkWorkflowAsFullyLoaded(OwWf workflow)
        {
            if (workflow == null)
                throw new ArgumentNullException(nameof(workflow));
            var entry = _DbContext.Entry(workflow);
            entry.Collection(w => w.Children).IsLoaded = true;
            if (workflow.Children != null)
            {
                foreach (var node in workflow.Children)
                {
                    _DbContext.Entry(node).Collection(n => n.Children).IsLoaded = true;
                }
            }
        }

        /// <summary>
        /// 获取DbContext中所有已加载的工作流实例。
        /// </summary>
        /// <returns>本地缓存中的工作流列表</returns>
        public List<OwWf> GetLocalWorkflows()
        {
            return _DbContext.OwWfs.Local.ToList();
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
        /// </summary>
        /// <param name="template"></param>
        /// <returns></returns>
        public IEnumerable<OwWfTemplateNode> GetFirstNodes(OwWfTemplate template)
        {
            var dic = template.Children.ToDictionary(c => c.Id);
            foreach (var item in template.Children)
                if (item.NextId is not null) dic.Remove(item.NextId.Value);
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
    }

    /// <summary>
    /// 工作流加载状态信息。
    /// </summary>
    public class WorkflowLoadStatus
    {
        /// <summary>
        /// 工作流ID。
        /// </summary>
        public Guid WorkflowId { get; set; }

        /// <summary>
        /// 工作流实例是否已加载到DbContext。
        /// </summary>
        public bool IsWorkflowLoaded { get; set; }

        /// <summary>
        /// 工作流的子节点集合是否已加载。
        /// </summary>
        public bool IsNodesLoaded { get; set; }

        /// <summary>
        /// 已加载的节点数量。
        /// </summary>
        public int LoadedNodesCount { get; set; }

        /// <summary>
        /// 所有节点项是否都已加载。
        /// </summary>
        public bool AllNodeItemsLoaded { get; set; }

        /// <summary>
        /// 各节点的节点项加载状态详情。
        /// </summary>
        public List<NodeItemLoadStatus> NodeItemsLoadStatus { get; set; }

        /// <summary>
        /// 是否完整加载（工作流、节点、节点项全部加载）。
        /// </summary>
        public bool IsFullyLoaded => IsWorkflowLoaded && IsNodesLoaded && AllNodeItemsLoaded;
    }

    /// <summary>
    /// 节点项加载状态信息。
    /// </summary>
    public class NodeItemLoadStatus
    {
        /// <summary>
        /// 节点ID。
        /// </summary>
        public Guid NodeId { get; set; }

        /// <summary>
        /// 该节点的子项集合是否已加载。
        /// </summary>
        public bool IsLoaded { get; set; }

        /// <summary>
        /// 已加载的节点项数量。
        /// </summary>
        public int ItemsCount { get; set; }
    }
}
