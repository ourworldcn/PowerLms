using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OW.SyncCommand
{
    [OwAutoInjection(ServiceLifetime.Scoped)]
    public class SyncCommandManager : OwDisposableBase
    {
        public SyncCommandManager()
        {

        }

        public SyncCommandManager(IServiceProvider service)
        {
            _Service = service;

        }

        IServiceProvider _Service;
        /// <summary>
        /// 该服务使用的范围服务容器。
        /// </summary>
        public IServiceProvider Service => _Service;

        private Dictionary<string, object> _Items;
        /// <summary>
        /// 当前范围内的一些数据。
        /// </summary>
        public IDictionary<string, object> Items => _Items ??= AutoClearPool<Dictionary<string, object>>.Shared.Get();

        public void Handle<T>(T command) where T : ISyncCommand
        {
            List<Exception> exceptions = new List<Exception>();
            NestedCommand.Push(command);
            try
            {
                HandleCore(command, exceptions);
            }
            finally
            {
                NestedCommand.TryPop(out _);
            }
            if (NestedCommand.IsEmpty) //若已处理完所有任务
                while (Post.TryDequeue(out var postCommand))
                {
                    NestedCommand.Push(command);
                    try
                    {
                        HandleCore(postCommand, exceptions);
                    }
                    finally
                    {
                        NestedCommand.TryPop(out _);
                    }
                }
        }

        /// <summary>
        /// 处理单个命令。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="command"></param>
        /// <param name="exceptions">该函数不会抛出异常，将所有异常追加到此集合。省略或为null则忽略异常。</param>
        [DebuggerHidden]
        protected virtual void HandleCore<T>(T command, ICollection<Exception> exceptions = null) where T : ISyncCommand
        {
            var pre = _Service.GetServices<ISyncCommandHandling<T>>();
            var coll = _Service.GetServices<ISyncCommandHandler<T>>();
            var post = _Service.GetServices<ISyncCommandHandled<T>>();

            foreach (var item in pre)
            {
                try
                {
                    item.Handling(command);
                }
                catch (Exception excp)
                {
                    exceptions?.Add(excp);
                }
            }
            foreach (var item in coll)
            {
                try
                {
                    item.Handle(command);
                }
                catch (Exception excp)
                {
                    exceptions?.Add(excp);
                }
            }
            if (post.Any())
            {
                var excpTmp = exceptions?.Count > 0 ? new AggregateException(exceptions) : null;
                foreach (var c in post)
                {
                    try
                    {
                        c.Handled(command, excpTmp);
                    }
                    catch (Exception excp)
                    {
                        exceptions?.Add(excp);
                    }
                }
            }
        }

        /// <summary>
        /// 处理单个命令。
        /// </summary>
        /// <param name="command"></param>
        /// <param name="exceptions">该函数不会抛出异常，将所有异常追加到此集合。省略或为null则忽略异常。</param>
        protected virtual void HandleCore(ISyncCommand command, ICollection<Exception> exceptions = null)
        {
            var preType = typeof(ISyncCommandHandling<>).MakeGenericType(command.GetType());
            var pre = _Service.GetServices(preType);
            var type = typeof(ISyncCommandHandler<>).MakeGenericType(command.GetType());
            var coll = _Service.GetServices(type);
            var postType = typeof(ISyncCommandHandled<>).MakeGenericType(command.GetType());
            var post = _Service.GetServices(postType);

            foreach (dynamic c in pre)
            {
                try
                {
                    var typeService = c.GetType();
                    var mi = typeService.GetMethod("Handling");
                    var result = mi.Invoke(c, new object[] { command });
                }
                catch (Exception excp)
                {
                    exceptions?.Add(excp);
                }
            }
            foreach (var c in coll)
            {
                try
                {
                    var typeService = c.GetType();
                    var mi = typeService.GetMethod("Handle");
                    var result = mi.Invoke(c, new object[] { command });
                }
                catch (Exception excp)
                {
                    exceptions?.Add(excp);
                }
            }
            if (post.Any())
            {
                var excpTmp = exceptions?.Count > 0 ? new AggregateException(exceptions) : null;
                foreach (dynamic c in post)
                {
                    try
                    {
                        var typeService = c.GetType();
                        var mi = typeService.GetMethod("Handled");
                        var result = mi.Invoke(c, new object[] { command, excpTmp });
                    }
                    catch (Exception excp)
                    {
                        exceptions?.Add(excp);
                    }
                }
            }
        }

        ConcurrentQueue<ISyncCommand> _Post;
        /// <summary>
        /// 后续处理的命令。
        /// </summary>
        public ConcurrentQueue<ISyncCommand> Post => LazyInitializer.EnsureInitialized(ref _Post);

        ConcurrentStack<ISyncCommand> _NestedCommand;
        /// <summary>
        /// 命令栈，顶层是当前在处理的命令。
        /// </summary>
        public ConcurrentStack<ISyncCommand> NestedCommand => LazyInitializer.EnsureInitialized(ref _NestedCommand);

        /// <summary>
        /// 获取命令处理器的嵌套深度。没有处理命令时是0，1表示正在处理顶层命令。
        /// </summary>
        public int NestedCount => _NestedCommand?.Count ?? 0;

        private int _OrderNumber;
        /// <summary>
        /// 命令处理的顺序。
        /// </summary>
        public int OrderNumber { get => _OrderNumber; set => _OrderNumber = value; }

        #region IDisposable接口相关

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    // 释放托管状态(托管对象)
                }

                // 释放未托管的资源(未托管的对象)并重写终结器
                // 将大型字段设置为 null
                if (_Items != null)
                {
                    AutoClearPool<Dictionary<string, object>>.Shared.Return(_Items);
                    _Items = null;
                }
                base.Dispose(disposing);
            }
        }

        #endregion IDisposable接口相关
    }

    public static class SyncCommandManagerExtensions
    {
        public static IServiceCollection UseSyncCommand(this IServiceCollection services, IEnumerable<Assembly> assemblies)
        {
            var coll = from tmp in assemblies.SelectMany(c => c.GetTypes())
                       let i = tmp.FindInterfaces((c1, c2) => c1.IsGenericType && c1.GetGenericTypeDefinition() == typeof(ISyncCommandHandler<>), null).FirstOrDefault()
                       where i != null && tmp.IsClass && !tmp.IsAbstract
                       select (Type: tmp, @interface: i);
            foreach (var item in coll)
            {
                services.AddScoped(item.@interface, item.Type);
            }
            return services;
        }
    }

    /// <summary>
    /// 在同一个线程(net)中处理的命令对象的专用标记接口。
    /// </summary>
    public interface ISyncCommand
    {

    }

    /// <summary>
    /// 有通用返回值的命令。
    /// </summary>
    public interface IResultCommand : ISyncCommand
    {
        /// <summary>
        /// 错误码，参见 ErrorCodes。
        /// </summary>
        public int ErrorCode { get; set; }

        /// <summary>
        /// 是否有错误。不设置则使用<see cref="ErrorCode"/>来判定。
        /// </summary>
        /// <value>0没有错误，其它数值含义由应用定义。</value>
        public bool HasError { get; set; }

        /// <summary>
        /// 调试信息，如果发生错误，这里给出简要说明。
        /// </summary>
        public string DebugMessage { get; set; }
    }

    /// <summary>
    /// 游戏命令处理器的基础接口。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ISyncCommandHandler<T> where T : ISyncCommand
    {
        /// <summary>
        /// 命令处理函数。
        /// </summary>
        /// <param name="command"></param>
        public void Handle(T command);
    }

    /// <summary>
    /// 对命令预处理的接口。多个同命令的预处理接口被调用的顺序无法确定。
    /// </summary>
    /// <typeparam name="TCommand"></typeparam>
    public interface ISyncCommandHandling<TCommand> where TCommand : ISyncCommand
    {
        public void Handling(TCommand command);
    }

    /// <summary>
    /// 对命令进行后处理的接口。多个同命令的后处理接口被调用的顺序无法确定。
    /// </summary>
    /// <typeparam name="TCommand"></typeparam>
    public interface ISyncCommandHandled<TCommand> where TCommand : ISyncCommand
    {
        /// <summary>
        /// 对命令进行后处理。
        /// </summary>
        /// <param name="command">处理的命令，更改该对象内容，导致返回时的内容发生变化，不建议在这里更改命令内容。</param>
        /// <param name="exception">若命令的处理过程中(<see cref="ISyncCommandHandler{T}"/>)引发了异常则在此给出，否则为空引用。</param>
        public void Handled(TCommand command, Exception exception = null);

    }

    /// <summary>
    /// 
    /// </summary>
    public abstract class SyncCommandBase : IResultCommand, ISyncCommand
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public SyncCommandBase()
        {

        }

        #region IResultWorkData 接口相关

        private bool? _HasError;

        /// <summary>
        /// 是否有错误。不设置则使用<see cref="ErrorCode"/>来判定。
        /// </summary>
        /// <value>0没有错误，其它数值含义由应用定义。</value>
        public bool HasError { get => _HasError ??= ErrorCode != 0; set => _HasError = value; }

        /// <summary>
        /// 错误码，参见 ErrorCodes。
        /// </summary>
        public int ErrorCode { get; set; }

        /// <summary>
        /// 调试用的提示性信息。
        /// </summary>
        private string _ErrorMessage;

        /// <summary>
        /// 调试信息，如果发生错误，这里给出简要说明。
        /// </summary>
        public string DebugMessage
        {
            get => _ErrorMessage ??= new Win32Exception(ErrorCode).Message;
            set => _ErrorMessage = value;
        }

        #endregion IResultWorkData 接口相关
    }

    /// <summary>
    /// 命令处理类的基类，可以在构造函数中注入必须的对象。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class SyncCommandHandlerBase<T> : ISyncCommandHandler<T> where T : ISyncCommand
    {
        protected SyncCommandHandlerBase()
        {

        }



        public abstract void Handle(T command);
    }

    /// <summary>
    /// 
    /// </summary>
    public static class SyncCommandBaseExtensions
    {
        /// <summary>
        /// 从<see cref="VWorld"/>对象获取错误信息。
        /// </summary>
        /// <param name="obj"></param>
        public static void FillErrorFromWorld(this IResultCommand obj)
        {
            obj.ErrorCode = OwHelper.GetLastError();
            obj.DebugMessage = OwHelper.GetLastErrorMessage();
            obj.HasError = 0 != obj.ErrorCode;
        }

        /// <summary>
        /// 从另一个对象填充错误。
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="src"></param>
        public static void FillErrorFrom(this IResultCommand obj, IResultCommand src)
        {
            obj.ErrorCode = src.ErrorCode;
            obj.DebugMessage = src.DebugMessage;
            obj.HasError = src.HasError;
        }
    }
}
