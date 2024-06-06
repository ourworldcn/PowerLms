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
        /// 12=指定操作者参与的且已结束的流程（包括成功/失败）</param>
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
                _ => throw new ArgumentOutOfRangeException(nameof(state)),
            };
            return result;
        }
    }
}
