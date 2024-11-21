﻿using NPOI.SS.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerLmsServer
{
    /// <summary>
    /// 
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
}

