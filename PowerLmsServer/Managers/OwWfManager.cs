using Microsoft.Extensions.DependencyInjection;
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
        /// 获取指定人员相关的节点项。
        /// </summary>
        /// <param name="opertorId">人员Id。</param>
        /// <param name="state">1=正等待指定操作者审批，2=指定操作者已审批但仍在流转中，4=指定操作者参与的且已成功结束的流程,8=指定操作者参与的且已失败结束的流程。
        /// 12=指定操作者参与的且已结束的流程（包括成功/失败）,15=不限定状态</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="state"/> 数值错误。</exception>
        public IQueryable<OwWfNodeItem> GetWfNodeItemByOpertorId(Guid opertorId, byte state)
        {
            //Guid opertorId, byte state
            var collBase = _DbContext.OwWfNodeItems.Where(c => c.OpertorId == opertorId && c.OperationKind == 0);
            var result = state switch
            {
                1 => collBase.Where(c => c.Parent.Parent.State == 0 && c.IsSuccess == null),
                2 => collBase.Where(c => c.Parent.Parent.State == 0 && c.IsSuccess != null),
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
