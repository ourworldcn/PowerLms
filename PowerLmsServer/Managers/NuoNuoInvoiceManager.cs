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


namespace PowerLmsServer.Managers
{
    using Microsoft.Extensions.DependencyInjection;
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;

    /// <summary>
    /// 诺诺发票服务类，包含获取访问令牌和创建发票的具体实现。
    /// </summary>
    public class NuoNuoManager
    {
        private const string TokenUrl = "https://open.nuonuo.com/accessToken";
        private const string InvoiceUrl = "https://sdk.nuonuo.com/open/v1/services";
        //沙箱环境https://sandbox.nuonuocs.cn/open/v1/services

        private readonly HttpClient _httpClient;

        /// <summary>
        /// 构造函数，初始化HttpClient实例。
        /// </summary>
        /// <param name="httpClient">HttpClient实例。</param>
        public NuoNuoManager(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// 异步获取访问令牌。
        /// </summary>
        /// <param name="appKey">应用程序密钥。</param>
        /// <param name="appSecret">应用程序密钥。</param>
        /// <returns>返回访问令牌。</returns>
        public async Task<string> GetAccessTokenAsync(string appKey, string appSecret)
        {
            var content = new FormUrlEncodedContent(new[]
            {
            new KeyValuePair<string, string>("appKey", appKey),
            new KeyValuePair<string, string>("appSecret", appSecret)
        });

            HttpResponseMessage response = await _httpClient.PostAsync(TokenUrl, content);
            response.EnsureSuccessStatusCode();

            string responseBody = await response.Content.ReadAsStringAsync();
            var json = JsonSerializer.Deserialize<Dictionary<string, string>>(responseBody);
            return json["access_token"];
        }

        /// <summary>
        /// 异步创建发票。
        /// </summary>
        /// <param name="accessToken">访问令牌。</param>
        /// <param name="invoiceData">发票数据。</param>
        /// <returns>返回发票创建结果。</returns>
        public async Task<string> CreateInvoiceAsync(string accessToken, NNOrder invoiceData)
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("access_token", accessToken);

            var jsonData = JsonSerializer.Serialize(invoiceData);
            var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.PostAsync(InvoiceUrl, content);
            response.EnsureSuccessStatusCode();

            string responseBody = await response.Content.ReadAsStringAsync();
            return responseBody;
        }
    }

    /// <summary>
    /// 诺税通saas请求开具发票接口请求类。
    /// 版本V2.0
    /// 具备诺税通saas资质的企业用户（集团总公司可拿下面公司的税号来开票，但需要先授权）填写发票销方、购方、明细等信息并发起开票请求。
    /// 请求地址：
    /// 正式环境：https://sdk.nuonuo.com/open/v1/services
    /// 沙箱环境：https://sandbox.nuonuocs.cn/open/v1/services
    /// 注：请下载SDK并完成报文组装后发送接口调用请求，accessToken获取方式请参考自用型应用创建和第三方应用创建。
    /// </summary>
    public class NuoNuoRequest
    {
        /// <summary>
        /// 唯一标识，由企业自己生成32位随机码。
        /// </summary>
        [JsonPropertyName("senid")]
        public string Senid { get; set; }

        /// <summary>
        /// 8位随机正整数。
        /// </summary>
        [JsonPropertyName("nonce")]
        public int Nonce { get; set; }

        /// <summary>
        /// 时间戳(当前时间的秒数)。
        /// </summary>
        [JsonPropertyName("timestamp")]
        public long Timestamp { get; set; }

        /// <summary>
        /// 平台分配给应用的appKey。
        /// </summary>
        [JsonPropertyName("appkey")]
        public string AppKey { get; set; }

        /// <summary>
        /// 请求api对应的方法名称。
        /// </summary>
        [JsonPropertyName("method")]
        public string Method { get; set; }

        /// <summary>
        /// 订单信息。
        /// </summary>
        [JsonPropertyName("order")]
        public NNOrder Order { get; set; }
    }

    /// <summary>
    /// 发票数据。
    /// </summary>
    public class NNOrder
    {
        /// <summary>
        /// 购方名称。
        /// </summary>
        [JsonPropertyName("buyerName")]
        public string BuyerName { get; set; }

        /// <summary>
        /// 销方税号。
        /// </summary>
        [JsonPropertyName("salerTaxNum")]
        public string SalerTaxNum { get; set; }

        /// <summary>
        /// 销方电话。
        /// </summary>
        [JsonPropertyName("salerTel")]
        public string SalerTel { get; set; }

        /// <summary>
        /// 销方地址。
        /// </summary>
        [JsonPropertyName("salerAddress")]
        public string SalerAddress { get; set; }

        /// <summary>
        /// 订单号（每个企业唯一）。
        /// </summary>
        [JsonPropertyName("orderNo")]
        public string OrderNo { get; set; }

        /// <summary>
        /// 订单时间。
        /// </summary>
        [JsonPropertyName("invoiceDate")]
        public DateTime InvoiceDate { get; set; }

        /// <summary>
        /// 开票类型：1: 蓝票; 2: 红票。
        /// </summary>
        [JsonPropertyName("invoiceType")]
        public int InvoiceType { get; set; }

        /// <summary>
        /// 发票明细。
        /// </summary>
        [JsonPropertyName("invoiceDetail")]
        public List<NNInvoiceDetail> InvoiceDetail { get; set; }

        /// <summary>
        /// 购方手机（pushMode为1或2时，此项为必填，同时受企业资质是否必填控制）。
        /// </summary>
        [JsonPropertyName("buyerPhone")]
        public string BuyerPhone { get; set; }

        /// <summary>
        /// 推送邮箱（pushMode为0或2时，此项为必填，同时受企业资质是否必填控制）。
        /// </summary>
        [JsonPropertyName("email")]
        public string Email { get; set; }

        /// <summary>
        /// 开票员（数电票时需要传入和开票登录账号对应的开票员姓名）。
        /// </summary>
        [JsonPropertyName("clerk")]
        public string Clerk { get; set; }
    }

    /// <summary>
    /// 发票明细。
    /// </summary>
    public class NNInvoiceDetail
    {
        /// <summary>
        /// 商品名称。
        /// </summary>
        [JsonPropertyName("goodsName")]
        public string GoodsName { get; set; }

        /// <summary>
        /// 单价含税标志：0: 不含税, 1: 含税。
        /// </summary>
        [JsonPropertyName("withTaxFlag")]
        public int WithTaxFlag { get; set; }

        /// <summary>
        /// 税率。
        /// </summary>
        [JsonPropertyName("taxRate")]
        public decimal TaxRate { get; set; }
    }

    /// <summary>
    /// 扩展方法类，包含配置NuoNuoManager的依赖注入方法。
    /// </summary>
    public static class NuoNuoManagerExtensions
    {
        /// <summary>
        /// 扩展方法，配置NuoNuoManager的依赖注入。
        /// </summary>
        /// <param name="services">IServiceCollection实例。</param>
        public static void AddNuoNuoManager(this IServiceCollection services)
        {
            // 注册HttpClient，并为NuoNuoManager配置基础地址
            //services.AddHttpClient<NuoNuoManager>(client =>
            //{
            //    client.BaseAddress = new Uri("https://open.nuonuo.com");
            //});

            // 注册NuoNuoManager服务
            services.AddSingleton<NuoNuoManager>();
        }
    }
}
