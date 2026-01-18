using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PowerLms.Data;
using System.Text.Json.Serialization;
namespace PowerLmsServer.Managers
{
    /// <summary>诺诺开票渠道账户数据类，此类不是数据库类，仅宿主到TaxInvoiceChannelAccount.JsonObjectString中。 </summary>
    public class NNChannelAccountObject
    {
        /// <summary>应用程序密钥</summary>
        public string AppKey { get; set; }
        /// <summary>应用程序密钥</summary>
        public string AppSecret { get; set; }
        /// <summary>令牌刷新获取时间点</summary>
        public DateTime TokenRefreshTime { get; set; }
        /// <summary>令牌有效期.可能永不过期。"TokenExpiry":"-00:00:00.0010000"</summary>
        /// <value>默认设置为永不过期,NuoNuo技术支持如此要求。</value>
        public TimeSpan TokenExpiry { get; set; } = Timeout.InfiniteTimeSpan;
        /// <summary>访问令牌</summary>
        public string Token { get; set; }
        /// <summary>
        /// 计算指定时间点到令牌失效的剩余时间
        /// </summary>
        /// <param name="currentTime">指定的当前时间点</param>
        /// <returns>返回剩余有效时间；如果令牌已过期或不存在，则返回TimeSpan.Zero；如果令牌永不过期，则返回TimeSpan.MaxValue</returns>
        public TimeSpan GetTimeToExpiry(DateTime currentTime)
        {
            // 如果令牌为空，则视为已过期
            if (string.IsNullOrEmpty(Token))
            {
                return TimeSpan.Zero;
            }
            // 检查是否为永不过期的特殊值
            if (TokenExpiry.Ticks < 0)
            {
                return TimeSpan.MaxValue; // 返回最大时间间隔表示永不过期
            }
            // 计算令牌的过期时间点
            DateTime expiryTime = TokenRefreshTime.Add(TokenExpiry);
            // 如果当前时间已经超过过期时间，则返回零
            if (currentTime >= expiryTime)
            {
                return TimeSpan.Zero;
            }
            // 返回从当前时间到过期时间点的时间间隔
            return expiryTime - currentTime;
        }
    }
    #region 令牌相关类
    /// <summary>访问令牌请求类</summary>
    public class NNAccessTokenRequest
    {
        /// <summary>应用程序密钥</summary>
        public string ClientId { get; set; }
        /// <summary>应用程序密钥</summary>
        public string ClientSecret { get; set; }
        /// <summary>授权类型，固定为"client_credentials"</summary>
        public string GrantType { get; set; } = "client_credentials";
    }
    /// <summary>访问令牌响应类</summary>
    public class NNAccessTokenResponse
    {
        /// <summary>访问令牌</summary>
        public string AccessToken { get; set; }
        /// <summary>访问令牌的过期时长(秒)</summary>
        public string ExpiresIn { get; set; }
    }
    /// <summary>访问令牌错误响应类</summary>
    public class NNAccessTokenErrorResponse
    {
        /// <summary>错误代码</summary>
        public string Error { get; set; }
        /// <summary>错误描述</summary>
        public string ErrorDescription { get; set; }
    }
    #endregion
}
