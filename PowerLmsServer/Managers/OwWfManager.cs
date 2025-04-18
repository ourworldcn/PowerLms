﻿using Microsoft.Extensions.DependencyInjection;
using PowerLms.Data;
using PowerLmsServer.EfData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public OwWfManager(PowerLmsUserDbContext dbContext)
        {
            _DbContext = dbContext;
        }

        private PowerLmsUserDbContext _DbContext;

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
}
