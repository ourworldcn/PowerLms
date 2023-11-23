using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if  !NETCOREAPP && !NET40 && !NETSTANDARD
namespace System
{
    //
    // 摘要:
    //     Defines a provider for push-based notification.
    //
    // 类型参数:
    //   T:
    //     The object that provides notification information.
    public interface IObservable<out T>
    {
        //
        // 摘要:
        //     Notifies the provider that an observer is to receive notifications.
        //
        // 参数:
        //   observer:
        //     The object that is to receive notifications.
        //
        // 返回结果:
        //     A reference to an interface that allows observers to stop receiving notifications
        //     before the provider has finished sending them.
        IDisposable Subscribe(IObserver<T> observer);
    }

    //
    // 摘要:
    //     Provides a mechanism for receiving push-based notifications.
    //
    // 类型参数:
    //   T:
    //     The object that provides notification information.
    public interface IObserver<in T>
    {
        //
        // 摘要:
        //     Notifies the observer that the provider has finished sending push-based notifications.
        void OnCompleted();
        //
        // 摘要:
        //     Notifies the observer that the provider has experienced an error condition.
        //
        // 参数:
        //   error:
        //     An object that provides additional information about the error.
        void OnError(Exception error);
        //
        // 摘要:
        //     Provides the observer with new data.
        //
        // 参数:
        //   value:
        //     The current notification information.
        void OnNext(T value);
    }

}
#endif

namespace OW
{

    /// <summary>
    /// 定义该接口的初衷是打算配合NetCore的服务依赖注入功能一起使用，但如果直接使用<see cref="IObservable{T}"/>担心与其它库相冲突，所以定义了此接口。
    /// </summary>
    /// <typeparam name="T"><inheritdoc/></typeparam>
    public interface IOwObservable<out T> : IObservable<T>
    {

    }

    /// <summary>
    /// 定义该接口的初衷是打算配合NetCore的服务依赖注入功能一起使用，但如果直接使用<see cref="IObserver{T}{T}"/>担心与其它库相冲突，所以定义了此接口。
    /// </summary>
    /// <typeparam name="T"><inheritdoc/></typeparam>
    public interface IOwObserver<in T> : IObserver<T>
    {

    }

    public class OwObservableBase<T> : IOwObservable<T>
    {
        public OwObservableBase()
        {

        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            return default;
        }
    }

    public class IOwObserverBase<T> : IOwObserver<T>
    {
        public void OnCompleted()
        {
            throw new NotImplementedException();
        }

        public void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        public void OnNext(T value)
        {
            throw new NotImplementedException();
        }
    }
}
