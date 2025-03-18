using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PowerLmsServer.Managers
{
    /// <summary>
    /// 手工开票管理器。
    /// </summary>
    [Guid("3F2CD8B7-6A48-4D1E-95A4-F0FAFD2F7670")]
    public class ManualInvoicingManager
    {
    }

    /// <summary>扩展方法封装类。</summary>
    public static class ManualInvoicingManagerExtensions
    {
        /// <summary>将手工开票管理器加入服务容器。</summary>
        public static IServiceCollection AddManualInvoicingManager(this IServiceCollection services)
        {
            return services.AddSingleton<ManualInvoicingManager>();
        }
    }
}
