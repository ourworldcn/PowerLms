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
    using System.Threading.Tasks;

    /// <summary>
    /// 诺诺发票服务类，包含获取访问令牌和创建发票的具体实现。
    /// </summary>
    public class NuoNuoInvoiceManager
    {
        private const string TokenUrl = "https://open.nuonuo.com/accessToken";
        private const string InvoiceUrl = "https://open.nuonuo.com/invoice";
        private readonly HttpClient _httpClient;

        /// <summary>
        /// 构造函数，初始化HttpClient实例。
        /// </summary>
        /// <param name="httpClient">HttpClient实例。</param>
        public NuoNuoInvoiceManager(HttpClient httpClient)
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
        public async Task<string> CreateInvoiceAsync(string accessToken, InvoiceData invoiceData)
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
    /// 发票项目模型类，定义发票项目的属性。
    /// </summary>
    public class InvoiceItem
    {
        /// <summary>
        /// 获取或设置货品品名。
        /// </summary>
        public string ItemName { get; set; }

        /// <summary>
        /// 获取或设置规格型号。
        /// </summary>
        public string ItemSpec { get; set; }

        /// <summary>
        /// 获取或设置单位。
        /// </summary>
        public string ItemUnit { get; set; }

        /// <summary>
        /// 获取或设置数量。
        /// </summary>
        public int ItemQuantity { get; set; }

        /// <summary>
        /// 获取或设置单价。
        /// </summary>
        public decimal ItemPrice { get; set; }

        /// <summary>
        /// 获取金额（数量 * 单价）。
        /// </summary>
        public decimal ItemAmount => ItemQuantity * ItemPrice;

        /// <summary>
        /// 获取或设置税率。
        /// </summary>
        public decimal ItemTaxRate { get; set; }

        /// <summary>
        /// 获取税额（金额 * 税率）。
        /// </summary>
        public decimal ItemTaxAmount => ItemAmount * ItemTaxRate;
    }

    /// <summary>
    /// 发票数据模型类，定义发票数据的属性。
    /// </summary>
    public class InvoiceData
    {
        /// <summary>
        /// 获取或设置客户名称。
        /// </summary>
        public string BuyerName { get; set; }

        /// <summary>
        /// 获取或设置客户税号。
        /// </summary>
        public string BuyerTaxNum { get; set; }

        /// <summary>
        /// 获取或设置客户地址。
        /// </summary>
        public string BuyerAddress { get; set; }

        /// <summary>
        /// 获取或设置客户电话。
        /// </summary>
        public string BuyerTel { get; set; }

        /// <summary>
        /// 获取或设置客户开户行。
        /// </summary>
        public string BuyerBankName { get; set; }

        /// <summary>
        /// 获取或设置客户银行账号。
        /// </summary>
        public string BuyerBankAccount { get; set; }

        /// <summary>
        /// 获取或设置发票类型。
        /// </summary>
        public string InvoiceType { get; set; }

        /// <summary>
        /// 获取或设置发票日期。
        /// </summary>
        public DateTime InvoiceDate { get; set; }

        /// <summary>
        /// 获取或设置发票项目列表。
        /// </summary>
        public List<InvoiceItem> Items { get; set; }
    }

    /// <summary>
    /// 扩展方法类，包含配置NuoNuoInvoiceManager的依赖注入方法。
    /// </summary>
    public static class ServiceExtensions
    {
        /// <summary>
        /// 扩展方法，配置NuoNuoInvoiceManager的依赖注入。
        /// </summary>
        /// <param name="services">IServiceCollection实例。</param>
        public static void AddNuoNuoInvoiceManager(this IServiceCollection services)
        {
            // 注册HttpClient，并为NuoNuoInvoiceManager配置基础地址
            //services.AddHttpClient<NuoNuoInvoiceManager>(client =>
            //{
            //    client.BaseAddress = new Uri("https://open.nuonuo.com");
            //});

            // 注册NuoNuoInvoiceManager服务
            services.AddSingleton<NuoNuoInvoiceManager>();
        }
    }
}
