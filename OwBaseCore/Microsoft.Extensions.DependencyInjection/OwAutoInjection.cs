/*
 * 文件名：OwAutoInjection.cs
 * 作者：ow
 * 创建日期：2024年11月23日
 * 描述：该文件包含自动将类注册为服务的特性、后台服务及其扩展方法。
 * 当前文件内容概述：
 * - OwAutoInjectionAttribute：用于标记需要自动注册为服务的类。
 * - OwAutoInjection：后台服务类，用于按需创建服务实例。
 * - OwAutoInjectionExtensions：扩展方法类，用于自动注册指定程序集内的服务类型。
 */

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// 自动将类注册为服务的特性。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public sealed class OwAutoInjectionAttribute : Attribute
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="lifetime">服务的生存期。</param>
        public OwAutoInjectionAttribute(ServiceLifetime lifetime) => Lifetime = lifetime;

        /// <summary>
        /// 获取服务的生存期。
        /// </summary>
        public ServiceLifetime Lifetime { get; }

        /// <summary>
        /// 服务的类型。可能返回 null，表示使用实现类相同类型的服务类型。
        /// </summary>
        public Type ServiceType { get; set; }

        /// <summary>
        /// 创建服务对象的函数。
        /// 如果不设置则直接用 <see cref="IServiceCollection"/>.AddXXX&lt;T&gt; 加入。
        /// 函数签名是 static object XXX(IServiceProvider)。
        /// </summary>
        public string CreateCallbackName { get; set; }

        /// <summary>
        /// 是否自动创建第一个实例。
        /// 如果为 true，将在后台线程自动创建一个实例以完成必要的初始化。
        /// </summary>
        /// <value>默认值为 false，不会自动创建实例。</value>
        public bool AutoCreateFirst { get; set; }
    }

    /// <summary>
    /// 处理自动注入服务的后台服务。
    /// </summary>
    public class OwAutoInjection : BackgroundService
    {
        #region 私有字段

        private readonly IEnumerable<(Type, bool)> _ServiceTypes;
        private readonly IServiceProvider _Service;
        private readonly ILogger<OwAutoInjection> _Logger;

        #endregion 私有字段

        #region 构造函数

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="service">服务提供者。</param>
        /// <param name="serviceTypes">要尽快创建第一个实例的服务类型集合。</param>
        /// <param name="logger">日志记录器。</param>
        public OwAutoInjection(IServiceProvider service, IEnumerable<(Type, bool)> serviceTypes, ILogger<OwAutoInjection> logger)
        {
            _ServiceTypes = serviceTypes;
            _Service = service;
            _Logger = logger;
        }

        #endregion 构造函数

        #region 执行后台任务

        /// <summary>
        /// 执行后台任务的方法。
        /// </summary>
        /// <param name="stoppingToken">停止标记。</param>
        /// <returns>任务对象。</returns>
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.Run(() => AutoCreate(stoppingToken), stoppingToken);
        }

        #endregion 执行后台任务

        #region 创建实例

        /// <summary>
        /// 按参数指定的，逐一创建一个实例。
        /// </summary>
        /// <param name="stoppingToken">停止标记。</param>
        private void AutoCreate(CancellationToken stoppingToken)
        {
            using var scope = _Service.CreateScope();
            foreach (var (type, scoped) in _ServiceTypes)
            {
                if (stoppingToken.IsCancellationRequested)
                {
                    _Logger.LogDebug("取消创建服务实例：{ServiceType}", type.FullName);
                    break;
                }

                try
                {
                    if (scoped)
                    {
                        // 在作用域范围内创建服务实例。
                        scope.ServiceProvider.GetService(type);
                    }
                    else
                    {
                        // 在全局范围内创建服务实例。
                        _Service.GetService(type);
                    }

                    _Logger.LogDebug("成功创建服务实例：{ServiceType}", type.FullName);
                }
                catch (Exception ex)
                {
                    // 处理异常，记录日志或其他操作。
                    _Logger.LogError(ex, "创建服务实例 {ServiceType} 时出错", type.FullName);
                }
            }
        }

        #endregion 创建实例
    }

    public static class OwAutoInjectionExtensions
    {
        #region 扩展方法

        /// <summary>
        /// 自动注册指定程序集内的服务类型，这些类型必须是用 <see cref="OwAutoInjectionAttribute"/> 标记的可实例化类。
        /// </summary>
        /// <param name="services">服务集合。</param>
        /// <param name="assemblies">程序集集合。</param>
        /// <returns>服务集合。</returns>
        public static IServiceCollection AutoRegister(this IServiceCollection services, IEnumerable<Assembly> assemblies = null)
        {
            // 如果未指定程序集，则使用当前应用域中的所有程序集。
            assemblies ??= AppDomain.CurrentDomain.GetAssemblies();

            // 获取所有带有 OwAutoInjectionAttribute 特性的类及其特性。
            var coll = assemblies.SelectMany(c => c.GetTypes())
                                 .SelectMany(t => t.GetCustomAttributes<OwAutoInjectionAttribute>()
                                                   .Select(att => (type: t, attribute: att)));

            var serviceTypes = new List<(Type, bool)>();

            foreach (var (type, att) in coll)
            {
                Func<IServiceProvider, object> implementationFactory = null;
                var serviceType = att.ServiceType ?? type;

                if (!string.IsNullOrWhiteSpace(att.CreateCallbackName))
                {
                    var callback = serviceType.GetMethod(att.CreateCallbackName) ?? type.GetMethod(att.CreateCallbackName);
                    implementationFactory = c => callback.Invoke(null, new object[] { c });
                }

                switch (att.Lifetime)
                {
                    case ServiceLifetime.Singleton:
                        RegisterService(services, serviceType, type, implementationFactory, ServiceLifetime.Singleton);
                        break;
                    case ServiceLifetime.Scoped:
                        RegisterService(services, serviceType, type, implementationFactory, ServiceLifetime.Scoped);
                        break;
                    case ServiceLifetime.Transient:
                        RegisterService(services, serviceType, type, implementationFactory, ServiceLifetime.Transient);
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown ServiceLifetime: {att.Lifetime}");
                }

                if (att.AutoCreateFirst)
                {
                    serviceTypes.Add((serviceType, att.Lifetime == ServiceLifetime.Scoped || att.Lifetime == ServiceLifetime.Transient));
                }
            }

            // 添加 OwAutoInjection 后台服务。
            services.AddHostedService(c => new OwAutoInjection(c, serviceTypes, c.GetRequiredService<ILogger<OwAutoInjection>>()));
            return services;
        }

        private static void RegisterService(IServiceCollection services, Type serviceType, Type implementationType, Func<IServiceProvider, object> implementationFactory, ServiceLifetime lifetime)
        {
            if (implementationFactory == null)
            {
                services.Add(new ServiceDescriptor(serviceType, implementationType, lifetime));
            }
            else
            {
                services.Add(new ServiceDescriptor(serviceType, implementationFactory, lifetime));
            }
        }

        #endregion 扩展方法
    }
}
