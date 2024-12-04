using Microsoft.Extensions.Primitives;
using System;
using System.Linq;
using System.Threading;

namespace Microsoft.Extensions.Caching.Memory
{
    /// <summary>
    /// 缓存相关的帮助静态类。
    /// </summary>
    public static class OwCacheHelper
    {
        static OwCacheHelper()
        {
            IdKeyLength = Guid.Empty.ToString().Length;
        }

        /// <summary>
        /// Id在缓存键值中的长度。
        /// </summary>
        public readonly static int IdKeyLength;

        /// <summary>
        /// 根据缓存的键值和后缀获取其Id。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="suffix">为null则不会验证后缀是否合法。</param>
        /// <returns>获取id,如果不合法的后缀或格式则返回null。</returns>
        public static Guid? GetIdFromCacheKey(string key, string suffix = null)
        {
            if (suffix != null && key[^suffix.Length..] != suffix)    //若未找到后缀
            {
                OwHelper.SetLastErrorAndMessage(400, "格式错误");
                return null;
            }
            if (!Guid.TryParse(key[..IdKeyLength], out var id))
            {
                OwHelper.SetLastErrorAndMessage(400, "格式错误");
                return Guid.Empty;
            }
            return id;
        }

        /// <summary>
        /// 根据缓存的键值和后缀获取其Id。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="suffix">为null则不会验证后缀是否合法。</param>
        /// <returns>获取id,如果不合法的后缀或格式则返回null。</returns>
        public static Guid? GetIdFromCacheKey<T>(string key, string suffix = $".{nameof(T)}")
        {
            return GetIdFromCacheKey(key, suffix);
        }

        /// <summary>
        /// 根据指定Id和前缀获取其用于缓存的键值。
        /// </summary>
        /// <param name="id"></param>
        /// <param name="suffix">后缀</param>
        /// <returns></returns>
        public static string GetCacheKeyFromId(Guid id, string suffix = null)
        {
            return $"{id}{suffix}";
        }

        /// <summary>
        /// 根据指定Id和前缀获取其用于缓存的键值。
        /// </summary>
        /// <param name="id"></param>
        /// <param name="suffix"></param>
        /// <returns></returns>
        public static string GetCacheKeyFromId<T>(Guid id, string suffix = $".{nameof(T)}")
        {
            return GetCacheKeyFromId(id, suffix);
        }
    }

    /// <summary>
    /// 包装缓存项的类。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class OwCacheItem<T>
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public OwCacheItem()
        {

        }

        /// <summary>
        /// 数据项。
        /// </summary>
        public T Data { get; set; }

        /// <summary>
        /// 使该项被逐出的取消对象。
        /// </summary>
        public CancellationTokenSource CancellationTokenSource { get; set; }

        /// <summary>
        /// 该项的变化令牌。
        /// </summary>
        public IChangeToken ChangeToken { get; set; }

        /// <summary>
        /// 设置<see cref="ChangeToken"/>属性。
        /// </summary>
        /// <param name="cancellations"></param>
        public void SetCancellations(params CancellationTokenSource[] cancellations)
        {
            if(cancellations.Length==1)
            {
                ChangeToken = new CancellationChangeToken(cancellations[0].Token);
                return;
            }
            var changeToken = new CompositeChangeToken(cancellations.Select(c => new CancellationChangeToken(c.Token)).OfType<IChangeToken>().ToArray());
            ChangeToken = changeToken;
        }
    }
}

