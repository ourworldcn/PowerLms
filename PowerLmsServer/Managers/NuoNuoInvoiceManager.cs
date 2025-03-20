﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using PowerLms.Data;
using Microsoft.Extensions.DependencyInjection;
using AutoMapper;
using PowerLmsServer.EfData;
using Microsoft.Extensions.Caching.Memory;
using System.Net;
using Microsoft.Extensions.ObjectPool;


namespace PowerLmsServer.Managers
{
    /// <summary>
    /// 诺诺发票服务类，包含获取访问令牌和创建发票的具体实现。
    /// </summary>
    [Guid("A08D3ECD-57CF-4E4E-B8F1-6B7C5B7F96E9")]
    public class NuoNuoManager
    {
        /// <summary>
        /// 访问令牌地址。
        /// </summary>
        private const string TokenUrl = "https://open.nuonuo.com/accessToken";
        /// <summary>
        /// 沙箱环境地址。
        /// </summary>
        private const string SanBoxTokenUrl = "https://sandbox.nuonuocs.cn/open/v1/services";
        /// <summary>
        /// 正式环境地址。
        /// </summary>
        private const string InvoiceUrl = "https://sdk.nuonuo.com/open/v1/services";

        private readonly HttpClient _httpClient;
        private readonly ILogger<NuoNuoManager> _logger;
        IMapper _mapper;
        PowerLmsUserDbContext _dbContext;
        IMemoryCache _cache;

        /// <summary>
        /// 构造函数，初始化HttpClient实例。
        /// </summary>
        /// <param name="httpClient">HttpClient实例。</param>
        /// <param name="mapper">AutoMapper实例</param>
        /// <param name="logger">日志记录器(可选)</param>
        /// <param name="dbContext">数据库上下文</param>
        /// <param name="cache">缓存对象</param>
        public NuoNuoManager(HttpClient httpClient, IMapper mapper, ILogger<NuoNuoManager> logger, PowerLmsUserDbContext dbContext, IMemoryCache cache)
        {
            _httpClient = httpClient;
            _logger = logger;
            _mapper = mapper;
            _dbContext = dbContext;
            _cache = cache;
        }

        #region 基础数据查询

        /// <summary>
        /// 根据发票ID查询发票信息对象
        /// </summary>
        /// <param name="taxInvoiceInfoId">发票信息ID</param>
        /// <returns>成功返回发票信息对象，失败返回null</returns>
        private TaxInvoiceInfo GetTaxInvoiceInfoById(Guid taxInvoiceInfoId)
        {
            try
            {
                var invoiceInfo = _dbContext.TaxInvoiceInfos.Find(taxInvoiceInfoId); // 查询发票信息
                if (invoiceInfo == null) // 发票信息不存在
                {
                    string errorMessage = $"未找到ID为 {taxInvoiceInfoId} 的发票信息";
                    _logger?.LogError(errorMessage);
                    OwHelper.SetLastErrorAndMessage((int)HttpStatusCode.NotFound, errorMessage);
                    return null;
                }
                return invoiceInfo; // 返回发票信息
            }
            catch (Exception ex) // 异常处理
            {
                _logger?.LogError(ex, "查询发票ID {TaxInvoiceInfoId} 的信息时发生错误", taxInvoiceInfoId);
                OwHelper.SetLastErrorAndMessage((int)HttpStatusCode.InternalServerError, $"查询发票ID {taxInvoiceInfoId} 的信息时发生错误: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 从发票对象获取对应的渠道账号配置实体。
        /// </summary>
        /// <param name="invoiceInfo">发票信息对象</param>
        /// <returns>成功返回渠道账号配置实体，失败返回null</returns>
        /// <remarks>此函数抽取了通用的获取TaxInvoiceChannelAccount的逻辑，以便其他方法重用</remarks>
        private TaxInvoiceChannelAccount GetChannelAccountFromInvoice(TaxInvoiceInfo invoiceInfo)
        {
            string errorMessage;    // 错误信息变量
            if (invoiceInfo == null) // 参数检查
            {
                errorMessage = "传入的发票信息对象为null";
                _logger?.LogError(errorMessage);
                OwHelper.SetLastErrorAndMessage((int)HttpStatusCode.BadRequest, errorMessage);
                return null;
            }

            errorMessage = null; // 错误信息变量
            int errorCode = 0; // 错误代码变量
            try
            {
                var channelAccountId = invoiceInfo.TaxInvoiceChannelAccountlId; // 获取渠道账号ID
                if (!channelAccountId.HasValue || channelAccountId.Value == Guid.Empty) // 检查渠道账号ID是否有效
                {
                    errorMessage = $"发票ID {invoiceInfo.Id} 未设置渠道账号ID";
                    errorCode = (int)HttpStatusCode.BadRequest;
                    goto ErrorHandler;
                }

                var channelAccount = _dbContext.TaxInvoiceChannelAccounts.Find(channelAccountId.Value); // 查询渠道账号配置
                if (channelAccount == null) // 检查配置是否存在
                {
                    errorMessage = $"未找到渠道账号ID {channelAccountId} 的配置信息";
                    errorCode = (int)HttpStatusCode.NotFound;
                    goto ErrorHandler;
                }

                _logger?.LogInformation("成功获取发票ID {TaxInvoiceInfoId} 对应的渠道账号配置", invoiceInfo.Id); // 使用结构化日志记录
                return channelAccount; // 返回成功结果

            ErrorHandler: // 统一错误处理标签
                _logger?.LogError(errorMessage); // 记录错误日志
                OwHelper.SetLastErrorAndMessage(errorCode, errorMessage);
                return null;
            }
            catch (Exception ex) // 异常处理
            {
                _logger?.LogError(ex, "获取发票ID {TaxInvoiceInfoId} 对应的渠道账号配置时发生错误", invoiceInfo.Id); // 使用结构化日志记录
                OwHelper.SetLastErrorAndMessage((int)HttpStatusCode.InternalServerError, $"获取发票ID {invoiceInfo.Id} 对应的渠道账号配置时发生错误: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 从渠道账号实体解析出诺诺渠道账号对象
        /// </summary>
        /// <param name="channelAccount">渠道账号实体</param>
        /// <returns>成功返回诺诺渠道账号对象，失败返回null</returns>
        private NNChannelAccountObject GetNNChannelAccountFromEntity(TaxInvoiceChannelAccount channelAccount)
        {
            // 参数验证
            if (channelAccount == null)
            {
                string errorMessage = "传入的渠道账号实体为null";
                _logger?.LogError(errorMessage);
                OwHelper.SetLastErrorAndMessage((int)HttpStatusCode.BadRequest, errorMessage);
                return null;
            }

            try
            {
                // 验证是否是诺诺渠道
                var nuoNuoManagerGuid = typeof(NuoNuoManager).GUID;
                if (channelAccount.ParentlId.GetValueOrDefault() != nuoNuoManagerGuid)
                {
                    string errorMessage = $"渠道账号ID {channelAccount.Id} 不是诺诺发票渠道";
                    _logger?.LogError(errorMessage);
                    OwHelper.SetLastErrorAndMessage((int)HttpStatusCode.BadRequest, errorMessage);
                    return null;
                }

                // 验证JSON字符串
                if (string.IsNullOrEmpty(channelAccount.JsonObjectString))
                {
                    string errorMessage = $"渠道账号ID {channelAccount.Id} 的配置信息为空";
                    _logger?.LogError(errorMessage);
                    OwHelper.SetLastErrorAndMessage((int)HttpStatusCode.BadRequest, errorMessage);
                    return null;
                }

                // 解析JSON字符串
                var nnChannelAccount = JsonSerializer.Deserialize<NNChannelAccountObject>(channelAccount.JsonObjectString);
                if (nnChannelAccount == null)
                {
                    string errorMessage = $"渠道账号ID {channelAccount.Id} 的配置信息无法解析";
                    _logger?.LogError(errorMessage);
                    OwHelper.SetLastErrorAndMessage((int)HttpStatusCode.BadRequest, errorMessage);
                    return null;
                }

                // 验证必要字段
                if (string.IsNullOrEmpty(nnChannelAccount.AppKey) || string.IsNullOrEmpty(nnChannelAccount.AppSecret))
                {
                    string errorMessage = $"渠道账号ID {channelAccount.Id} 的AppKey或AppSecret为空";
                    _logger?.LogError(errorMessage);
                    OwHelper.SetLastErrorAndMessage((int)HttpStatusCode.BadRequest, errorMessage);
                    return null;
                }

                _logger?.LogInformation("成功解析渠道账号ID {ChannelAccountId} 的诺诺账号配置", channelAccount.Id);
                return nnChannelAccount;
            }
            catch (JsonException ex)
            {
                _logger?.LogError(ex, "解析渠道账号ID {ChannelAccountId} 的JSON配置时发生错误", channelAccount.Id);
                OwHelper.SetLastErrorAndMessage((int)HttpStatusCode.BadRequest, $"渠道账号ID {channelAccount.Id} 的JSON配置无效: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "获取渠道账号ID {ChannelAccountId} 的诺诺账号配置时发生错误", channelAccount.Id);
                OwHelper.SetLastErrorAndMessage((int)HttpStatusCode.InternalServerError, $"获取渠道账号ID {channelAccount.Id} 的诺诺账号配置时发生错误: {ex.Message}");
                return null;
            }
        }
        #endregion 基础数据查询

        #region 令牌相关

        /// <summary>
        /// 从指定发票ID获取有效的访问令牌，如果令牌过期则自动刷新
        /// </summary>
        /// <param name="taxInvoiceInfoId">发票信息ID</param>
        /// <returns>有效的访问令牌，如果获取失败则返回null</returns>
        public string GetAccessToken(Guid taxInvoiceInfoId)
        {
            try
            {
                // 获取发票信息
                var invoiceInfo = GetTaxInvoiceInfoById(taxInvoiceInfoId);
                if (invoiceInfo == null)
                {
                    return null; // GetTaxInvoiceInfoById已记录错误信息
                }
                else if (!invoiceInfo.TaxInvoiceChannelAccountlId.HasValue)
                {
                    _logger?.LogError("发票ID {TaxInvoiceInfoId} 未设置渠道账号ID", taxInvoiceInfoId);
                    OwHelper.SetLastErrorAndMessage((int)HttpStatusCode.BadRequest, $"发票ID {taxInvoiceInfoId} 未设置渠道账号ID");
                    return null;
                }
                using var dw = DisposeHelper.CreateWithSingletonLocker(invoiceInfo.TaxInvoiceChannelAccountlId.Value.ToString(),
                    Timeout.InfiniteTimeSpan); // 锁定渠道账号ID

                // 获取渠道账号
                var channelAccount = GetChannelAccountFromInvoice(invoiceInfo);
                if (channelAccount == null)
                {
                    _logger?.LogError("未找到发票ID {TaxInvoiceInfoId} 对应的渠道账号配置", taxInvoiceInfoId);
                    return null;
                }

                // 解析获取诺诺渠道账号对象
                var nnChannelAccount = GetNNChannelAccountFromEntity(channelAccount);
                if (nnChannelAccount == null)
                {
                    return null; // GetNNChannelAccountFromEntity已记录错误信息
                }


                // 检查令牌是否存在且在有效期内
                var timeToExpiry = nnChannelAccount.GetTimeToExpiry(DateTime.Now);
                if (!string.IsNullOrEmpty(nnChannelAccount.Token) && timeToExpiry > TimeSpan.FromMinutes(1)) // 留出1分钟
                {
                    // 令牌有效，直接返回
                    _logger?.LogInformation("使用已有令牌，发票ID: {TaxInvoiceInfoId}, 剩余有效期: {TimeToExpiry}", taxInvoiceInfoId, timeToExpiry);
                    return nnChannelAccount.Token;
                }

                // 令牌不存在或已过期，刷新令牌
                _logger?.LogInformation("令牌已过期或不存在，发票ID: {TaxInvoiceInfoId}, 开始刷新令牌", taxInvoiceInfoId);

                // 刷新令牌并更新配置
                var refreshedToken = RefreshAccessToken(nnChannelAccount, channelAccount);
                if (string.IsNullOrEmpty(refreshedToken))
                {
                    _logger?.LogError("刷新令牌失败，发票ID: {TaxInvoiceInfoId}", taxInvoiceInfoId);
                    OwHelper.SetLastErrorAndMessage((int)HttpStatusCode.InternalServerError, $"刷新发票ID {taxInvoiceInfoId} 的访问令牌失败");
                    return null;
                }

                _logger?.LogInformation("成功刷新令牌，发票ID: {TaxInvoiceInfoId}", taxInvoiceInfoId);
                return refreshedToken;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "获取发票ID {TaxInvoiceInfoId} 的访问令牌时发生错误", taxInvoiceInfoId);
                OwHelper.SetLastErrorAndMessage((int)HttpStatusCode.InternalServerError, $"获取发票ID {taxInvoiceInfoId} 的访问令牌时发生错误: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 刷新访问令牌并更新到数据库
        /// </summary>
        /// <param name="channelAccountObject">诺诺渠道账号对象</param>
        /// <param name="channelAccount">渠道账号数据库实体</param>
        /// <returns>成功返回新令牌，失败返回null</returns>
        private string RefreshAccessToken(NNChannelAccountObject channelAccountObject, TaxInvoiceChannelAccount channelAccount)
        {
            try
            {
                // 调用诺诺接口获取新令牌
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("client_id", channelAccountObject.AppKey),
                    new KeyValuePair<string, string>("client_secret", channelAccountObject.AppSecret),
                    new KeyValuePair<string, string>("grant_type", "client_credentials")
                });

                // 同步方式发送请求
                var task = _httpClient.PostAsync(TokenUrl, content);
                task.Wait();
                var response = task.Result;
                response.EnsureSuccessStatusCode();

                // 同步读取响应内容
                var responseBodyTask = response.Content.ReadAsStringAsync();
                responseBodyTask.Wait();
                var responseBody = responseBodyTask.Result;

                // 解析响应
                var json = JsonSerializer.Deserialize<NNAccessTokenResponse>(responseBody);
                if (json == null || string.IsNullOrEmpty(json.AccessToken) || string.IsNullOrEmpty(json.ExpiresIn))
                {
                    _logger?.LogError("刷新访问令牌失败：响应数据无效或不完整");
                    return null;
                }

                // 解析过期时间（秒）
                if (!int.TryParse(json.ExpiresIn, out int expiresInSeconds))
                {
                    _logger?.LogWarning("无法解析令牌过期时间 '{ExpiresIn}'，将使用默认值", json.ExpiresIn);
                    expiresInSeconds = 7200; // 默认2小时
                }

                // 更新令牌信息
                channelAccountObject.Token = json.AccessToken;
                channelAccountObject.TokenRefreshTime = DateTime.Now;
                channelAccountObject.TokenExpiry = TimeSpan.FromSeconds(expiresInSeconds); // 使用服务器返回的过期时间

                // 更新到数据库
                channelAccount.JsonObjectString = JsonSerializer.Serialize(channelAccountObject);
                _dbContext.SaveChanges();

                _logger?.LogInformation(
                    "成功刷新访问令牌，渠道账号ID: {ChannelAccountId}, 令牌有效期: {ExpirySeconds}秒",
                    channelAccount.Id,
                    expiresInSeconds);

                return json.AccessToken;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "刷新访问令牌失败，渠道账号ID: {ChannelAccountId}", channelAccount.Id);
                return null;
            }
        }
        #endregion 令牌相关

        /// <summary>
        /// 根据发票ID发起开票请求
        /// </summary>
        /// <param name="taxInvoiceInfoId">发票信息ID</param>
        /// <returns>开票结果</returns>
        public NuoNuoInvoiceResult IssueInvoice(Guid taxInvoiceInfoId)
        {
            try
            {
                // 获取发票信息
                var invoiceInfo = GetTaxInvoiceInfoById(taxInvoiceInfoId);
                if (invoiceInfo == null)
                {
                    return new NuoNuoInvoiceResult
                    {
                        Success = false,
                        ErrorCode = "NotFound",
                        ErrorMessage = $"未找到ID为 {taxInvoiceInfoId} 的发票信息"
                    };
                }

                // 获取发票明细
                var items = _dbContext.TaxInvoiceInfoItems
                    .Where(item => item.ParentId == taxInvoiceInfoId)
                    .ToList();

                if (items == null || items.Count == 0)
                {
                    return new NuoNuoInvoiceResult
                    {
                        Success = false,
                        ErrorCode = "NoItems",
                        ErrorMessage = $"发票ID {taxInvoiceInfoId} 没有明细项"
                    };
                }

                // 获取渠道账号
                var channelAccount = GetChannelAccountFromInvoice(invoiceInfo);
                if (channelAccount == null)
                {
                    return new NuoNuoInvoiceResult
                    {
                        Success = false,
                        ErrorCode = "NoChannelAccount",
                        ErrorMessage = $"未找到发票ID {taxInvoiceInfoId} 对应的渠道账号配置"
                    };
                }

                // 获取诺诺渠道账号对象
                var nnChannelAccount = GetNNChannelAccountFromEntity(channelAccount);
                if (nnChannelAccount == null)
                {
                    return new NuoNuoInvoiceResult
                    {
                        Success = false,
                        ErrorCode = "InvalidChannelAccount",
                        ErrorMessage = $"渠道账号ID {channelAccount.Id} 不是有效的诺诺发票渠道账号"
                    };
                }

                // 获取令牌
                string accessToken = GetAccessToken(taxInvoiceInfoId);
                if (string.IsNullOrEmpty(accessToken))
                {
                    return new NuoNuoInvoiceResult
                    {
                        Success = false,
                        ErrorCode = "TokenError",
                        ErrorMessage = $"无法获取发票ID {taxInvoiceInfoId} 的访问令牌"
                    };
                }

                // 准备数据
                var request = BuildInvoiceRequest(invoiceInfo, items, nnChannelAccount);
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                };
                var jsonContent = JsonSerializer.Serialize(request, options);

                // 计算签名
                string signContent = nnChannelAccount.AppKey + nnChannelAccount.AppSecret + jsonContent + nnChannelAccount.AppSecret;
                string sign = ComputeMD5Hash(signContent);

                // 设置请求头
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Content-type", "application/json");
                _httpClient.DefaultRequestHeaders.Add("X-Nuonuo-Sign", sign);
                _httpClient.DefaultRequestHeaders.Add("accessToken", accessToken);
                _httpClient.DefaultRequestHeaders.Add("userTax", request.Order.SalerTaxNum);
                _httpClient.DefaultRequestHeaders.Add("method", "nuonuo.OpeMplatform.requestBillingNew");

                // 发送请求
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var responseTask = _httpClient.PostAsync(InvoiceUrl, content);
                responseTask.Wait(); // 同步等待请求
                var response = responseTask.Result;

                if (!response.IsSuccessStatusCode)
                {
                    return new NuoNuoInvoiceResult
                    {
                        Success = false,
                        ErrorCode = response.StatusCode.ToString(),
                        ErrorMessage = $"请求失败，状态码: {response.StatusCode}"
                    };
                }

                var responseContentTask = response.Content.ReadAsStringAsync();
                responseContentTask.Wait(); // 同步等待响应内容
                var responseContent = responseContentTask.Result;

                // 处理响应
                var result = JsonSerializer.Deserialize<NuoNuoInvoiceResponse>(responseContent, options);

                if (result != null && result.Code == "E0000")
                {
                    // 更新发票状态和流水号
                    if (!string.IsNullOrEmpty(result.Result?.InvoiceSerialNum))
                    {
                        invoiceInfo.InvoiceSerialNum = result.Result.InvoiceSerialNum;
                        invoiceInfo.State = 2; // 已开票状态
                        invoiceInfo.ReturnInvoiceTime = DateTime.Now;
                        _dbContext.SaveChanges();

                        _logger?.LogInformation($"开票成功并更新了发票状态，发票ID: {taxInvoiceInfoId}, 流水号: {result.Result.InvoiceSerialNum}");
                    }
                    else
                    {
                        _logger?.LogInformation($"开票成功但未返回流水号，发票ID: {taxInvoiceInfoId}");
                    }

                    return new NuoNuoInvoiceResult
                    {
                        Success = true,
                        InvoiceSerialNum = result.Result?.InvoiceSerialNum
                    };
                }
                else
                {
                    _logger?.LogError($"开票失败，发票ID: {taxInvoiceInfoId}, 错误码: {result?.Code}, 描述: {result?.Describe}");

                    return new NuoNuoInvoiceResult
                    {
                        Success = false,
                        ErrorCode = result?.Code,
                        ErrorMessage = result?.Describe
                    };
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "发票ID {TaxInvoiceInfoId} 调用诺诺开票接口时发生异常", taxInvoiceInfoId);

                return new NuoNuoInvoiceResult
                {
                    Success = false,
                    ErrorCode = "Exception",
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// 从发票信息确定推送方式
        /// </summary>
        /// <param name="invoiceInfo">发票信息对象</param>
        /// <returns>推送方式：-1不推送;0邮箱;1手机;2邮箱和手机</returns>
        private string DeterminePushMode(TaxInvoiceInfo invoiceInfo)
        {
            // 检查发票对象是否有效
            if (invoiceInfo == null)
            {
                _logger?.LogWarning("发票对象为null，将使用默认推送方式(手机)");
                return "1"; // 默认使用手机推送
            }

            try
            {
                // 检查是否有邮箱
                bool hasEmail = !string.IsNullOrWhiteSpace(invoiceInfo.Mail);

                // 检查是否有手机号
                bool hasPhone = !string.IsNullOrWhiteSpace(invoiceInfo.Mobile);

                string pushMode;

                if (hasEmail && hasPhone)
                {
                    pushMode = "2"; // 邮箱和手机
                    _logger?.LogInformation("发票ID {InvoiceId} 同时设置了邮箱和手机，将使用两者推送", invoiceInfo.Id);
                }
                else if (hasEmail)
                {
                    pushMode = "0"; // 仅邮箱
                    _logger?.LogInformation("发票ID {InvoiceId} 只设置了邮箱，将使用邮箱推送", invoiceInfo.Id);
                }
                else if (hasPhone)
                {
                    pushMode = "1"; // 仅手机
                    _logger?.LogInformation("发票ID {InvoiceId} 只设置了手机，将使用手机推送", invoiceInfo.Id);
                }
                else
                {
                    pushMode = "-1"; // 不推送
                    _logger?.LogWarning("发票ID {InvoiceId} 未设置邮箱和手机，将不进行推送", invoiceInfo.Id);
                }

                return pushMode;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "确定发票ID {InvoiceId} 的推送方式时出现异常，将使用默认推送方式(手机)",
                    invoiceInfo?.Id ?? Guid.Empty);
                return "1"; // 出现异常时默认使用手机推送
            }
        }

        /// <summary>
        /// 构建诺诺发票请求对象
        /// </summary>
        private NuoNuoRequest BuildInvoiceRequest(
            TaxInvoiceInfo invoiceInfo,
            List<TaxInvoiceInfoItem> items,
            NNChannelAccountObject channelAccount)
        {
            // 生成请求基本参数
            var senid = Guid.NewGuid().ToString("N");
            var nonce = new Random().Next(10000000, 99999999);
            var timestamp = DateTimeOffset.Now.ToUnixTimeSeconds();

            // 使用AutoMapper映射基本信息
            var order = _mapper.Map<NNOrder>(invoiceInfo);

            // 设置明细项
            order.InvoiceDetail = _mapper.Map<List<NNInvoiceDetail>>(items);

            // 设置额外的推送方式
            order.PushMode = DeterminePushMode(invoiceInfo);

            return new NuoNuoRequest
            {
                Senid = senid,
                Nonce = nonce,
                Timestamp = timestamp,
                AppKey = channelAccount.AppKey,
                Method = "nuonuo.OpeMplatform.requestBillingNew",
                Order = order
            };
        }

        /// <summary>
        /// 计算MD5哈希
        /// </summary>
        private string ComputeMD5Hash(string input)
        {
            using var md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);

            // 转换为十六进制字符串
            var sb = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("x2"));
            }
            return sb.ToString();
        }
    }

    /// <summary>扩展方法类，包含配置NuoNuoManager的依赖注入方法</summary>
    public static class NuoNuoManagerExtensions
    {
        /// <summary>扩展方法，配置NuoNuoManager的依赖注入</summary>
        /// <param name="services">IServiceCollection实例</param>
        public static void AddNuoNuoManager(this IServiceCollection services)
        {
            // 使用AddHttpClient注册NuoNuoManager，它会自动以Scoped生命周期注册服务
            services.AddHttpClient<NuoNuoManager>(client =>
            {
                // 可以在这里配置HttpClient的默认设置，例如超时时间
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            // 不需要再次添加AddScoped，因为AddHttpClient已经注册了服务
            // services.AddScoped<NuoNuoManager>(); // 这行应该被移除
        }
    }
}
