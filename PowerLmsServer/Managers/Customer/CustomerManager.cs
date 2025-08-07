using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerLmsServer.Managers
{
    /// <summary>
    /// 客户资料及相关管理器。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Singleton)]
    public class CustomerManager
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public CustomerManager()
        {
            
        }
    }
}
