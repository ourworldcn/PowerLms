using System.Runtime.CompilerServices;
using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.ObjectPool;

namespace OW
{
    /// <summary>
    /// 帮助实现 <see cref="IDisposable"/> 和 <see cref="IAsyncDisposable"/> 接口的抽象类。
    /// </summary>
    public abstract partial class OwDisposableBase : IDisposable
    {
        protected OwDisposableBase() { }

        #region IDisposable接口相关

        /// <summary>
        /// 如果对象已经被处置则抛出<see cref="ObjectDisposedException"/>异常。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //[DoesNotReturn]
        protected void ThrowIfDisposed()
        {
            if (_IsDisposed)
                throw new ObjectDisposedException(GetType().FullName);
        }

        private bool _IsDisposed;

        /// <summary>
        /// 获取或设置对象是否已经处置的属性，派生类需要自己切换该属性。
        /// </summary>
        public bool IsDisposed { get => _IsDisposed; protected set => _IsDisposed = value; }

        /// <summary>
        /// 调用此实现以切换 <see cref="IsDisposed"/> 属性。
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing) => IsDisposed = true;
        //{
        //    if (!IsDisposed)
        //    {
        //        if (disposing)
        //        {
        //            //释放托管状态(托管对象)
        //        }

        //        // 释放未托管的资源(未托管的对象)并重写终结器
        //        // 将大型字段设置为 null
        //        base.Dispose(disposing);  //        IsDisposed = true;
        //    }
        //}

        // 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~LeafMemoryCache()
        // {
        //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        //     Dispose(disposing: false);
        // }

        /// <summary>
        /// 处置对象。
        /// </summary>
        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable接口相关

    }

#if NETCOREAPP3_0_OR_GREATER
    //public abstract partial class DisposableBase : IAsyncDisposable
    //{
    //    #region IDisposable接口相关
    //    public async ValueTask DisposeAsync()
    //    {
    //        await DisposeAsyncCore().ConfigureAwait(false);
    //        GC.SuppressFinalize(this);
    //    }

    //    protected virtual async ValueTask DisposeAsyncCore()
    //    {
    //        //if (_example is not null)
    //        //{
    //        //    await _example.DisposeAsync().ConfigureAwait(false);
    //        //}

    //        //_example = null;
    //        await ValueTask.CompletedTask.ConfigureAwait(false);
    //    }

    //    #endregion IDisposable接口相关

    //}
#endif  //NETCOREAPP3_0_OR_GREATER

    /// <summary>
    /// 服务的基类。
    /// </summary>
    /// <typeparam name="TOptions"></typeparam>
    /// <typeparam name="TService"></typeparam>
    public abstract class OwServiceBase<TOptions, TService> : OwDisposableBase
        where TOptions : class
    {
        protected OwServiceBase(IOptions<TOptions> options, ILogger<TService> logger)
        {
            Options = options.Value;
            Logger = logger;
        }

        public TOptions Options { get; protected set; }

        public ILogger<TService> Logger { get; protected set; }

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    //释放托管状态(托管对象)
                }

                // 释放未托管的资源(未托管的对象)并重写终结器
                // 将大型字段设置为 null
                Options = null;
                Logger = null;
                IsDisposed = true;
            }
        }
    }

    /// <summary>
    /// 提供池化对象的基类，派生类重载<seealso cref="Dispose(bool)"/>在参数为True时，不是真的处置对象，而是将对象状态清理，并试图返回池。
    /// </summary>
    /// <typeparam name="T">池化对象的类型。</typeparam>
    public abstract class OwPooledObjectBase<T> : OwDisposableBase, IPooledObjectPolicy<T> where T : class, new()
    {
        public virtual T Create()
        {
            return new T();
        }

        public abstract bool Return(T obj);

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    //试图将对象返回池中
                }
                else
                {
                    // 释放未托管的资源(未托管的对象)并重写终结器
                    // 将大型字段设置为 null
                    //base.Dispose(disposing);  //        IsDisposed = true;
                }
            }
        }
    }
}