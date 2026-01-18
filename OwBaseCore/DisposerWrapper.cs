using Microsoft.Extensions.ObjectPool;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
namespace System
{
    /// <summary>帮助调用清理代码帮助器。应配合 C#8.0 using语法使用。对象本身就支持对象池，不要将此对象放在其他池中。</summary>
    //[DebuggerNonUserCode()]
    public sealed class DisposerWrapper : IDisposable
    {
        #region 内部类和静态成员
        /// <summary>对象池策略类。</summary>
        private class DisposerWrapperPolicy : PooledObjectPolicy<DisposerWrapper>
        {
            public DisposerWrapperPolicy() { }
            public override DisposerWrapper Create() => new DisposerWrapper();
            public override bool Return(DisposerWrapper obj)
            {
                obj.DisposeAction = null; // 清除处置动作
                obj._Disposed = false; // 重置处置标记
                obj._IsInPool = true; // 标记为在池中
                return true;
            }
        }
        /// <summary>
        /// 对象池。
        /// </summary>
        private static ObjectPool<DisposerWrapper> Pool { get; } = new DefaultObjectPool<DisposerWrapper>(new DisposerWrapperPolicy(), Math.Max(Environment.ProcessorCount * 4, 16)); // 创建对象池
        #endregion
        #region 创建方法
        /// <summary>使用指定的处置操作创建包装器。</summary>
        /// <param name="action">要在Dispose时执行的操作</param>
        /// <returns>处置包装器实例</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DisposerWrapper Create(Action action)
        {
            var result = Pool.Get(); // 从池中获取实例
            result._IsInPool = false; // 标记为不在池中
            result.DisposeAction = action; // 设置处置操作
            return result;
        }
        /// <summary>使用指定的带参数处置操作创建包装器。</summary>
        /// <typeparam name="T">状态参数类型</typeparam>
        /// <param name="action">要在Dispose时执行的带参数操作</param>
        /// <param name="state">操作使用的状态参数</param>
        /// <returns>处置包装器实例</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DisposerWrapper Create<T>(Action<T> action, T state) => Create(() => action(state));
        /// <summary>使用指定的可处置对象集合创建包装器。</summary>
        /// <param name="disposers">要在Dispose时处置的对象集合</param>
        /// <returns>处置包装器实例</returns>
        /// <remarks>方法逻辑较复杂，不适合内联；在处置过程中仅在出现异常时才按需分配异常列表并可选择聚合处理。</remarks>
        public static DisposerWrapper Create(IEnumerable<IDisposable> disposers) =>
 Create(c =>
 {
     //仅在确有异常时才分配列表
     List<Exception> exceptions = null;
     foreach (var item in c)
     {
         try { item.Dispose(); }
         catch (Exception err) { (exceptions ??= new List<Exception>()).Add(err); }
     }
     if (exceptions != null && exceptions.Count > 0)
     {
         var aggregate = new AggregateException(exceptions);
         // 可在此记录或抛出 aggregate
     }
 }, disposers);
        /// <summary>使用SingletonLocker锁定指定对象创建包装器。</summary>
        /// <typeparam name="T">要锁定的对象类型</typeparam>
        /// <param name="lockObject">要锁定的对象</param>
        /// <param name="timeout">锁定超时时间</param>
        /// <returns>处置包装器实例，Dispose时自动解锁，失败返回null</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DisposerWrapper Create<T>(T lockObject, TimeSpan timeout) where T : class
        {
            if (SingletonLocker.TryEnter(lockObject, timeout))
            {
                return Create(() => SingletonLocker.Exit(lockObject));
            }
            return null;
        }
        #endregion 创建方法
        #region 实例成员
        /// <summary>构造函数。</summary>
        private DisposerWrapper() { }
        /// <summary>获取或设置要执行的处置操作。</summary>
        public Action DisposeAction { get; set; }
        private bool _Disposed; // 标记是否已处置
        private bool _IsInPool; // 标记是否在对象池中
        /// <summary>执行处置操作并将此实例返回到对象池。</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void Dispose()
        {
            if (!_IsInPool && !_Disposed)
            {
                DisposeAction?.Invoke();
                _Disposed = true;
                Pool.Return(this);
            }
        }
        #endregion 实例成员
    }
    /// <summary>清理代码帮助器结构。实测比使用对象池要快20%左右。</summary>
    /// <typeparam name="T">清理时调用函数的参数。</typeparam>
    public readonly ref struct DisposeHelper<T>
    {
        #region 构造和属性
        /// <summary>构造函数。</summary>
        /// <param name="action">要运行的清理函数。</param>
        /// <param name="state">清理函数的参数。</param>
        public DisposeHelper(Action<T> action, T state)
        {
            Action = action; // 设置清理函数
            State = state; // 设置参数
        }
        /// <summary>获取清理的委托。</summary>
        public readonly Action<T> Action;
        /// <summary>获取清理委托使用的参数。</summary>
        public readonly T State;
        /// <summary>判断此结构是不是一个空结构。</summary>
        public readonly bool IsEmpty => Action is null;
        #endregion 构造和属性
        #region 处置方法
        /// <summary>处置函数。配合c#的using语法使用。</summary>
        /// <remarks>这个方法非常关键且简短，适合 AggressiveInlining。</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public readonly void Dispose()
        {
#if DEBUG
            try { Action?.Invoke(State); }
            catch (Exception err) { Debug.WriteLine(err.Message); }
#else
            Action?.Invoke(State);
#endif
        }
        #endregion 处置方法
    }
    /// <summary>提供创建DisposeHelper的静态方法。</summary>
    public static class DisposeHelper
    {
        #region 基本创建方法
        /// <summary>创建一个在using释放时自动调用的补偿操作。</summary>
        /// <typeparam name="T">状态参数类型</typeparam>
        /// <param name="action">清理操作</param>
        /// <param name="state">状态参数</param>
        /// <returns>DisposeHelper实例</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DisposeHelper<T> Create<T>(Action<T> action, T state) =>
        new DisposeHelper<T>(action, state);
        /// <summary>返回一个空的结构。</summary>
        /// <typeparam name="T">状态参数类型</typeparam>
        /// <returns>空的DisposeHelper结构</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DisposeHelper<T> Empty<T>() => default;
        #endregion 基本创建方法
        #region 锁定相关方法
        /// <summary>锁定对象创建一个可以释放的结构，在自动释放。</summary>
        /// <typeparam name="T">锁对象类型</typeparam>
        /// <param name="lockFunc">锁定的函数。</param>
        /// <param name="unlockFunc">解锁函数。</param>
        /// <param name="lockObject">锁定对象。</param>
        /// <param name="timeout">超时。</param>
        /// <returns><see cref="DisposeHelper{T}.IsEmpty"/>是true则说明锁定失败。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DisposeHelper<T> Create<T>(Func<T, TimeSpan, bool> lockFunc, Action<T> unlockFunc, T lockObject, TimeSpan timeout) =>
            lockFunc(lockObject, timeout) ? new DisposeHelper<T>(unlockFunc, lockObject) : default;
        /// <summary>使用SingletonLocker锁定对象并创建一个可释放结构</summary>
        /// <typeparam name="T">锁对象类型</typeparam>
        /// <param name="lockObject">要锁定的对象</param>
        /// <param name="timeout">锁定超时</param>
        /// <returns>可释放结构，IsEmpty为true表示锁定失败</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DisposeHelper<T> CreateWithSingletonLocker<T>(T lockObject, TimeSpan timeout) where T : class =>
        SingletonLocker.TryEnter(lockObject, timeout)
        ? new DisposeHelper<T>(static obj => SingletonLocker.Exit(obj), lockObject)
        : Empty<T>();
        /// <summary>按顺序锁定一组对象，并返回一个可以释放的结构。</summary>
        /// <typeparam name="T">锁对象类型</typeparam>
        /// <param name="lockFunc">锁定函数</param>
        /// <param name="unlockFunc">解锁函数</param>
        /// <param name="lockObject">要锁定的对象集合</param>
        /// <param name="timeout">超时</param>
        /// <returns><see cref="DisposeHelper{T}.IsEmpty"/>是true则说明锁定失败，此时没有任何对象被锁定。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DisposeHelper<IEnumerable<T>> CreateMulti<T>(Func<T, TimeSpan, bool> lockFunc, Action<T> unlockFunc, IEnumerable<T> lockObject, TimeSpan timeout) =>
        OwHelper.TryEnterAll(lockObject, lockFunc, unlockFunc, timeout)
        ? new DisposeHelper<IEnumerable<T>>(c => c.SafeForEach(c1 => unlockFunc(c1)), lockObject.Reverse())
        : Empty<IEnumerable<T>>();
        #endregion 锁定相关方法
    }
}
