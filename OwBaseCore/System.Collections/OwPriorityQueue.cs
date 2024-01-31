using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Collections.Generic
{
    /// <summary>
    /// 使用循环数组构建的优先级队列。
    /// 表示具有值和优先级的项的集合。 取消排队时，将删除优先级值最低的项。
    /// </summary>
    public class OwPriorityQueue
    {
        PriorityQueue<int, int> queue = new PriorityQueue<int, int>(Environment.ProcessorCount);
    }
}
