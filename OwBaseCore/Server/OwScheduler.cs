using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Cache;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace OW.Server
{
    /// <summary>
    /// 任务类型。
    /// </summary>
    public enum OwTaskType
    {
        /// <summary>
        /// 未知类型。
        /// </summary>
        Unknow = 0,

        /// <summary>
        /// 幂等任务。调用多次与调用一次结果一样。成功执行一次就会自动清除。
        /// </summary>
        Idempotent = 1,

        /// <summary>
        /// 定时任务。
        /// </summary>
        Timer = 2,

        /// <summary>
        /// 长时间运行任务。
        /// </summary>
        LongRunning = 4,
    }

    /// <summary>
    /// 任务项的详细配置类。
    /// </summary>
    public class OwSchedulerEntry
    {
        public OwSchedulerEntry()
        {
        }

        public OwTaskType TaskType { get; set; }

        /// <summary>
        /// 任务的唯一标识对象。
        /// </summary>
        public object Key { get; set; }

        /// <summary>
        /// (键，state)。返回值如果是true则标识成功，否则将在下次扫描时继续执行该任务。
        /// </summary>
        public Func<object, object, bool> TaskCallback { get; set; }

        /// <summary>
        /// 任务回调的第二个参数。
        /// </summary>
        public object State { get; set; }

        /// <summary>
        /// 调用任务的周期。
        /// </summary>
        /// <value>可以是<see cref="Timeout.InfiniteTimeSpan"/>或任何大于0的间隔。</value>
        public TimeSpan Period { get; set; }

        /// <summary>
        /// 上次执行的时间点。null表示尚未运行过。
        /// </summary>
        public DateTime? LastUtc { get; internal set; }

        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public void IsValid()
        {
            if (Period == TimeSpan.Zero)
                throw new InvalidOperationException($"{nameof(Period)}应大于0。");
        }

        /// <summary>
        /// 是否到期。
        /// </summary>
        /// <param name="now"></param>
        /// <returns>true已经到期，false未到期。</returns>
        public bool IsExpired(DateTime now)
        {
            if (LastUtc is null)    //若尚未运行
                return true;
            if (Period == Timeout.InfiniteTimeSpan)  //若仅运行一次
                return false;
            return OwHelper.ComputeTimeout(LastUtc.Value, now) >= Period;
        }

        /// <summary>
        /// 增加最后运行时间。
        /// </summary>
        /// <param name="now">指定当前时间。</param>
        public void SetExpired(DateTime now)
        {
            if (LastUtc is null)    //若尚未运行过任务
            {
                LastUtc = now;
                return;
            }
            else
                while (LastUtc.Value < now)   //若时间未超过指定时间
                    LastUtc += Period;
        }
    }

    /// <summary>
    /// OwScheduler 服务的配置数据类。
    /// </summary>
    public class OwSchedulerOptions : IOptions<OwSchedulerOptions>
    {
        public OwSchedulerOptions()
        {

        }

        /// <summary>
        /// 默认的任务执行频度。
        /// </summary>
        /// <value>默认值：1分钟。</value>
        public TimeSpan Frequency { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// 设置或获取锁定键的回调。应支持递归与<see cref="UnlockCallback"/>配对使用。
        /// 默认值是<see cref="SingletonLocker.TryEnter(object, TimeSpan)"/>。
        /// </summary>
        public Func<object, TimeSpan, bool> LockCallback { get; set; } = SingletonLocker.TryEnter;

        /// <summary>
        /// 设置或获取释放键的回调。应支持递归与<see cref="LockCallback"/>配对使用。
        /// 默认值是<see cref="SingletonLocker.Exit(object)"/>。
        /// </summary>
        public Action<object> UnlockCallback { get; set; } = SingletonLocker.Exit;

        /// <summary>
        /// 确定当前线程是否保留指定键上的锁。
        /// 默认值是<see cref="SingletonLocker.IsEntered(object)"/>
        /// </summary>
        public Func<object, bool> IsEnteredCallback { get; set; } = SingletonLocker.IsEntered;

        #region IOptions 接口及相关

        public OwSchedulerOptions Value => this;
        #endregion IOptions 接口及相关
    }

    /// <summary>
    /// 一个用于处理长时间任务的任务计划器。
    /// 默认它仅用一个线程（不并行），因为假设的场景是一个服务器的IO线程。
    /// 每个任务会有一个唯一标识key，调用任务会首先锁定key,若不能锁定则会再下次扫描时调用任务。
    /// 每个任务都有一个object型的key,用于标识任务，但不同类型的任务即使key一样也被认为是不同的任务。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Singleton)]
    public class OwScheduler : BackgroundService
    {
        #region 构造函数及相关

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="options"></param>
        public OwScheduler(IOptions<OwSchedulerOptions> options, ILogger<OwScheduler> logger, IHostApplicationLifetime hostApplicationLifetime)
        {
            Options = options.Value;
            _Logger = logger;
            _HostApplicationLifetime = hostApplicationLifetime;
            Initializer();
        }

        /// <summary>
        /// 初始化函数。
        /// </summary>
        private void Initializer()
        {
            //执行幂等任务的初始化
            _IdempotentThread = new Thread(IdempotentWorker)
            {
                Priority = ThreadPriority.BelowNormal,
                IsBackground = false,
                Name = "幂等操作任务执行线程。",
            };
            _IdempotentThread.Start();
            _HostApplicationLifetime.ApplicationStopped.Register(() => _RequestCancel.Cancel());
        }

        #endregion 构造函数及相关

        ILogger<OwScheduler> _Logger;

        private OwSchedulerOptions _Options;

        IHostApplicationLifetime _HostApplicationLifetime;

        /// <summary>
        /// 配置对象。
        /// </summary>
        public OwSchedulerOptions Options { get => _Options ??= new OwSchedulerOptions(); init => _Options = value; }

        CancellationTokenSource _RequestCancel = new CancellationTokenSource();

        /// <summary>
        /// 任务项。
        /// </summary>
        ConcurrentDictionary<object, OwSchedulerEntry> _Items = new ConcurrentDictionary<object, OwSchedulerEntry>();

        /// <summary>
        /// 优先执行任务的列表。键是锁定项的键，使用此类可避免锁定。
        /// </summary>
        ConcurrentDictionary<object, object> _Plans = new ConcurrentDictionary<object, object>();

        /// <summary>
        /// 记录所有幂等任务的数据结构，(任务的键，任务条目).
        /// </summary>
        ConcurrentDictionary<object, OwSchedulerEntry> _IdempotentEntries = new ConcurrentDictionary<object, OwSchedulerEntry>();

        /// <summary>
        /// 幂等任务的工作线程。
        /// </summary>
        Thread _IdempotentThread;

        /// <summary>
        /// 执行幂等任务的工作函数。
        /// </summary>
        public void IdempotentWorker()
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(_RequestCancel.Token);
            var ct = cts.Token;
            var list = new List<OwSchedulerEntry>();    //没有成功完成的任务项放在此处
            while (!ct.IsCancellationRequested)
            {
                foreach (var kvp in _IdempotentEntries)   //扫描循环
                {
                    var key = kvp.Key;
                    if (!Options.LockCallback(key, TimeSpan.Zero)) continue;    //若无法锁定键
                    using var dw = DisposeHelper.Create(c => Options.UnlockCallback(c), kvp.Key); Debug.Assert(!dw.IsEmpty);
                    if (!_IdempotentEntries.TryRemove(key, out var entry)) continue;    //若已经并发处置
                    try
                    {
                        if (!entry.TaskCallback(entry.Key, entry.State))    //若任务不成功
                            list.Add(entry);
                    }
                    catch (Exception)
                    {
                        list.Add(entry);
                    }
                    Thread.Yield();
                }
                list.ForEach(c => _IdempotentEntries.TryAdd(c.Key, c)); //加入未正常完成的项
                ct.WaitHandle.WaitOne(1);
            }
        }

        /// <summary>
        /// 运行所有任务。
        /// </summary>
        /// <param name="now"></param>
        void RunAll(DateTime now)
        {
            foreach (var kvp in _Items)
            {
                using var dw = DisposeHelper.Create(Options.LockCallback, Options.UnlockCallback, kvp.Key, TimeSpan.Zero);
                if (dw.IsEmpty) //若无法锁定
                {
                    Run(kvp.Key);   //安排优先再次运行
                    continue;
                }
                if (!kvp.Value.IsExpired(now))   //若无需运行
                    continue;
                if (!RunCore(kvp.Value, out _))
                {
                    Run(kvp.Key);   //安排优先再次运行
                    continue;
                }
                //成功运行了任务
                _Plans.Remove(kvp.Key, out _);
                Thread.Yield();
            }
        }

        /// <summary>
        /// 运行优先执行任务。
        /// </summary>
        void RunPlans()
        {
            foreach (var kvp in _Plans)
            {
                using var dw = DisposeHelper.Create(Options.LockCallback, Options.UnlockCallback, kvp.Key, TimeSpan.Zero);
                if (dw.IsEmpty) //若无法锁定
                {
                    Run(kvp.Key);   //安排优先再次运行
                    continue;
                }
                if (!_Plans.Remove(kvp.Key, out _)) //若已经变化
                    continue;
                var entry = _Items.GetValueOrDefault(kvp.Key);
                if (entry is null)   //若已变化
                    continue;
                if (!RunCore(entry, out _))
                {
                    Run(kvp.Key);   //安排优先再次运行
                    continue;
                }
                //成功运行了任务
                _Plans.Remove(kvp.Key, out _);
                Thread.Yield();
            }
        }

        /// <summary>
        /// 执行指定键值的任务。
        /// 非公有函数不会自动对键加锁，若需要则调用者需负责加/解锁。
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="exception">任务函数抛出的异常，可能是null。</param>
        /// <returns>同步执行时返回任务的返回值。异步执行时返回true。
        /// 执行函数如果抛出异常，则也会返回false。
        /// </returns>
        protected virtual bool RunCore(OwSchedulerEntry entry, out Exception exception)
        {
            Debug.Assert(Options.IsEnteredCallback(entry.Key));
            var now = OwHelper.WorldNow;
            bool b;
            try
            {
                b = entry.TaskCallback?.Invoke(entry.Key, entry.State) ?? true;
                exception = null;
            }
            catch (Exception excp)
            {
                exception = excp;
                b = false;
            }
            if (b)
                entry.SetExpired(now);
            return b;
        }

        /// <summary>
        /// 执行指定键值的任务。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="sync">true同步执行，false(默认)计划优先执行。</param>
        /// <returns>同步执行时返回任务的返回值。异步执行时返回true标识成功增加优先任务，false表示优先队列中有一个同样的项尚未执行。</returns>
        public bool Run(object key, bool sync = false)
        {
            if (sync) //若同步执行
            {
                using var dw = DisposeHelper.Create(Options.LockCallback, Options.UnlockCallback, key, Timeout.InfiniteTimeSpan);
                var entry = _Items.GetValueOrDefault(key);
                var b = RunCore(entry, out _);
                if (b)
                    _Plans.Remove(key, out _);
                return b;
            }
            else //若计划优先执行
            {
                var b = _Plans.TryAdd(key, null);
                //发送脉冲
                using var dw = DisposeHelper.Create(Options.LockCallback, Options.UnlockCallback, _Plans, TimeSpan.Zero);
                if (!dw.IsEmpty) Monitor.Pulse(_Plans);
                return b;
            }
        }

        /// <summary>
        /// 增加一个幂等任务。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="task"></param>
        /// <param name="state"></param>
        public void TryAddIdempotent(object key, Func<object, object, bool> task, object state)
        {
            var entry = new OwSchedulerEntry
            {
                TaskType = OwTaskType.Idempotent,
                Key = key,
                State = state,
                TaskCallback = task,
            };
            _IdempotentEntries.AddOrUpdate(key, entry, (lkey, ov) => entry);
        }

        /// <summary>
        /// 确保指定键的幂等任务完成。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="timeout">锁定键等待的超时时间。</param>
        /// <returns>true成功执行并移除或不存在指定key的幂等操作任务；false无法锁定key或任务无法成功完成。
        /// </returns>
        public bool EnsureComplateIdempotent(object key, TimeSpan timeout)
        {
            using var dw = DisposeHelper.Create(Options.LockCallback, Options.UnlockCallback, key, timeout);
            if (dw.IsEmpty)
            {
                OwHelper.SetLastErrorAndMessage(258, $"锁定键时超时,Key={key} ,Timeout={timeout}");
                return false;
            }
            if (!_IdempotentEntries.TryRemove(key, out var entry)) return true;
            try
            {
                if (!entry.TaskCallback(entry.Key, entry.State))
                    _IdempotentEntries.TryAdd(entry.Key, entry);
            }
            catch (Exception)
            {
                _IdempotentEntries.TryAdd(entry.Key, entry);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 增加一个任务项。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns>true成功加入，false已经存在指定的 <paramref name="key"/>。</returns>
        public bool TryAdd(object key, OwSchedulerEntry value)
        {
            value.IsValid();
            using var dw = DisposeHelper.Create(Options.LockCallback, Options.UnlockCallback, key, Timeout.InfiniteTimeSpan);
            value.Key = key;
            return _Items.TryAdd(key, value);
        }

        /// <summary>
        /// 移除一个任务项。
        /// </summary>
        /// <param name="key"></param>
        /// <returns>true成功删除，否则返回false,可能是指定key不存在 -或- 作为优先执行任务却无法执行成功执行。</returns>
        public bool TryRemove(object key)
        {
            bool result;
            using var dw = DisposeHelper.Create(Options.LockCallback, Options.UnlockCallback, key, Timeout.InfiniteTimeSpan);
            if (_Plans.Remove(key, out _))    //若在优先执行任务列表
            {
                result = Run(key, true);    //强制运行任务
                if (!result)    //若任务失败
                    return result;
            }
            result = _Items.Remove(key, out _);
            return result;
        }

        #region BackgroundService及相关

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //注册终止函数。
            stoppingToken.Register(c =>
            {
                try
                {
                    ExecuteTask?.Wait(); //等待后台任务结束
                }
                catch (Exception) { }
                RunPlans();
                return;
            }, null);
            //后台任务
            return Task.Factory.StartNew(stoppingTokenObj =>
            {
                CancellationToken ctStop = (CancellationToken)stoppingTokenObj;
                DateTime now;  //此轮工作的开始时间
                var lastNow = DateTime.MinValue;    //上一轮运行时间点
                while (!stoppingToken.IsCancellationRequested)
                {
                    now = OwHelper.WorldNow;
                    if (!_Plans.IsEmpty)  //若有优先运行事务
                        RunPlans();
                    if (now - lastNow >= Options.Frequency)  //若需要运行完整的事务
                    {
                        RunAll(now);
                        lastNow = now;
                    }
                    //等待
                    var ts = OwHelper.ComputeTimeout(now, Options.Frequency);
                    var task = Task.Run(() =>
                    {
                        using var dw = DisposeHelper.Create(Monitor.TryEnter, Monitor.Exit, _Plans, ts);
                        if (!dw.IsEmpty)
                        {
                            ts = OwHelper.ComputeTimeout(OwHelper.WorldNow, now + Options.Frequency);
                            if (!_Plans.IsEmpty)   //若有优先执行任务
                                return;
                            Monitor.Wait(_Plans, ts, true);
                        }
                    });
                    try
                    {
                        task.Wait((int)ts.TotalMilliseconds, ctStop);
                    }
                    catch (AggregateException) { }
                    catch (OperationCanceledException) { break; }
                }
            }, stoppingToken, TaskCreationOptions.LongRunning);
        }

        #endregion BackgroundService及相关

        #region IDisposable接口及相关

        private bool _Disposed;

        /// <summary>
        /// 是否已经被处置。
        /// </summary>
        protected bool Disposed => _Disposed;

        protected virtual void Dispose(bool disposing)
        {
            if (!_Disposed)
            {
                if (disposing)
                {
                    // 释放托管状态(托管对象)
                }

                // 释放未托管的资源(未托管的对象)并重写终结器
                // 将大型字段设置为 null
                _Items = null;
                _Plans = null;
                _Logger = null;
                _Disposed = true;
                _Logger.LogInformation("{GetType().Name}已经处置。", GetType().Name);
            }
        }

        // 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~OwScheduler()
        // {
        //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        //     Dispose(disposing: false);
        // }

        //public void Dispose()
        //{
        //    // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        //    Dispose(disposing: true);
        //    GC.SuppressFinalize(this);
        //}

        #endregion IDisposable接口及相关

    }

    public static class OwSchedulerExtensions
    {
        public static IServiceCollection AddOwScheduler(this IServiceCollection services)
        {
            services.AddHostedService<OwScheduler>();
            services.AddSingleton(sp => (OwScheduler)sp.GetRequiredService<IEnumerable<IHostedService>>().First(c => c is OwScheduler));
            return services;
        }
    }

    public class OwTimerSchedulerEntry
    {

    }

    /// <summary>
    /// 一次性的定时任务
    /// </summary>
    public class OwDelayScheduler
    {
        public OwDelayScheduler()
        {

        }

        ConcurrentDictionary<object, OwTimerSchedulerEntry> keys;
        PriorityQueue<object, DateTime> values;

        public void Add()
        {
            
        }

        public void Remove()
        {

        }

        public void Delay()
        {
        }

        protected void Scan()
        {

        }

        public void EnsureRunAndRemove()
        {
        }
    }
}
