using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// 自动将类注册为服务。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class OwAutoInjectionAttribute : Attribute
    {
        // See the attribute guidelines at 
        //  http://go.microsoft.com/fwlink/?LinkId=85236

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="lifetime">服务的生存期。</param>
        public OwAutoInjectionAttribute(ServiceLifetime lifetime)
        {
            _Lifetime = lifetime;
        }

        readonly ServiceLifetime _Lifetime;
        /// <summary>
        /// 获取或设置服务的类型。
        /// </summary>
        public ServiceLifetime Lifetime
        {
            get { return _Lifetime; }
        }

        /// <summary>
        /// 服务的类型。可能返回null,表示使用实现类相同类型的服务类型。
        /// </summary>
        public Type ServiceType { get; set; }

        /// <summary>
        /// 创建服务对象的函数。
        /// 如果不设置则直接用 <see cref="IServiceCollection"/>.AddXXX&lt;T&gt; 加入。
        /// 函数签名是 static object XXX(IServiceProvider)
        /// </summary>
        public string CreateCallbackName { get; set; }

        /// <summary>
        /// 是否自动创建第一个实例。
        /// true将在后台线程自动创建一个实例以完成必要的初始化。
        /// </summary>
        public bool AutoCreateFirst { get; set; }

    }

    public class OwAutoInjection : BackgroundService
    {
        /// <summary>
        /// 要尽快创建第一个实例的服务类型。
        /// </summary>
        /// <param name="serviceTypes"></param>
        public OwAutoInjection(IServiceProvider service, IEnumerable<(Type, bool)> serviceTypes)
        {
            _ServiceTypes = serviceTypes;
            _Service = service;
        }

        IEnumerable<(Type, bool)> _ServiceTypes;
        IServiceProvider _Service;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (stoppingToken.IsCancellationRequested) return Task.CompletedTask;
            return Task.Run(() => AutoCreate(_Service, _ServiceTypes));
        }

        /// <summary>
        /// 按参数指定的，逐一创建一个实例。
        /// </summary>
        /// <param name="service"></param>
        /// <param name="serviceTypes"></param>
        static void AutoCreate(IServiceProvider service, IEnumerable<(Type, bool)> serviceTypes)
        {
            using var scope = service.CreateScope();
            foreach (var item in serviceTypes)
            {
                if (item.Item2)
                    scope.ServiceProvider.GetService(item.Item1);
                else
                    service.GetService(item.Item1);
            }
        }
    }

    public static class OwAutoInjectionExtensions
    {
        /// <summary>
        /// 自动注册指定程序集内的服务类型，这些类型必须是用<see cref="OwAutoInjectionAttribute"/>标记的可实例化类。
        /// </summary>
        /// <param name="services"></param>
        /// <param name="assemblies"></param>
        /// <returns></returns>
        public static IServiceCollection AutoRegister(this IServiceCollection services, IEnumerable<Assembly> assemblies = null)
        {
            assemblies ??= AppDomain.CurrentDomain.GetAssemblies();
            var coll = assemblies.SelectMany(c => c.GetTypes()).Where(c => c.GetCustomAttribute<OwAutoInjectionAttribute>() != null);
            var serviceTypes = new List<(Type, bool)>();
            foreach (var item in coll)
            {
                var att = item.GetCustomAttribute<OwAutoInjectionAttribute>();
                switch (att.Lifetime)
                {
                    case ServiceLifetime.Singleton:
                        if (string.IsNullOrWhiteSpace(att.CreateCallbackName))  //若没有创建函数
                            services.AddSingleton(att.ServiceType ?? item, item);
                        else //若有创建函数
                        {
                            var callback = att.ServiceType.GetMethod(att.CreateCallbackName);
                            services.AddSingleton(att.ServiceType ?? item, c => callback.Invoke(null, new object[] { c }));
                        }
                        if (att.AutoCreateFirst) serviceTypes.Add((att.ServiceType ?? item, false));
                        break;
                    case ServiceLifetime.Scoped:
                        if (string.IsNullOrWhiteSpace(att.CreateCallbackName))  //若没有创建函数
                            services.AddScoped(att.ServiceType ?? item, item);
                        else //若有创建函数
                        {
                            var callback = att.ServiceType.GetMethod(att.CreateCallbackName);
                            services.AddScoped(att.ServiceType ?? item, c => callback.Invoke(null, new object[] { c }));
                        }
                        if (att.AutoCreateFirst) serviceTypes.Add((att.ServiceType ?? item, true));
                        break;
                    case ServiceLifetime.Transient:
                        if (string.IsNullOrWhiteSpace(att.CreateCallbackName))  //若没有创建函数
                            services.AddTransient(att.ServiceType ?? item, item);
                        else //若有创建函数
                        {
                            var callback = att.ServiceType.GetMethod(att.CreateCallbackName);
                            services.AddTransient(att.ServiceType ?? item, c => callback.Invoke(null, new object[] { c }));
                        }
                        if (att.AutoCreateFirst) serviceTypes.Add((att.ServiceType ?? item, true));
                        break;
                    default:
                        break;
                }
            }
            services.AddHostedService(c => new OwAutoInjection(c, serviceTypes));
            return services;
        }
    }
}
