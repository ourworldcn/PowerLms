/*
文件名称: ServiceProviderChecker.cs
作者: OW
创建日期: 2025年2月8日
修改日期: 2025年2月8日
描述: 这个文件包含一个 ServiceProviderChecker 类，它是一个单例服务，并提供方法来判断指定的服务提供者是否是根容器。
*/

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// 服务提供者检查器类，用于判断指定的服务提供者是否是根容器。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Singleton)] // 这个服务必须是单例
    public class ServiceProviderChecker : IDisposable
    {
        private readonly ILogger<ServiceProviderChecker> _Logger;
        private readonly IServiceProvider _RootServiceProvider;
        private bool _Disposed = false;

        #region 构造函数
        /// <summary>
        /// 构造函数，初始化日志记录器，并记录创建此类的根服务提供者。
        /// </summary>
        /// <param name="logger">用于记录日志的记录器。</param>
        /// <param name="serviceProvider">创建此类的服务提供者。</param>
        public ServiceProviderChecker(ILogger<ServiceProviderChecker> logger, IServiceProvider serviceProvider)
        {
            _Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _RootServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _Logger.LogDebug("ServiceProviderChecker initialized with root service provider.");
        }
        #endregion 构造函数

        #region IsRootContainer 方法
        /// <summary>
        /// 判断指定的服务提供者是否是根容器。
        /// </summary>
        /// <param name="serviceProvider">要检查的服务提供者。</param>
        /// <returns>如果是根容器返回 true，否则返回 false。</returns>
        public bool IsRootContainer(IServiceProvider serviceProvider)
        {
            return ReferenceEquals(serviceProvider, _RootServiceProvider);
        }
        #endregion IsRootContainer 方法

        #region IDisposable 实现
        /// <summary>
        /// 释放资源。
        /// </summary>
        /// <param name="disposing">指示是否释放托管资源。</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_Disposed)
            {
                if (disposing)
                {
                    // 释放托管资源。
                    _Logger.LogDebug("Releasing managed resources.");
                }

                // 释放非托管资源。

                _Disposed = true;
                _Logger.LogDebug("ServiceProviderChecker disposed.");
            }
        }

        /// <summary>
        /// 实现 IDisposable 接口的方法。
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion IDisposable 实现
    }
}
