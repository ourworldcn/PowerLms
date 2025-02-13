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
    /// <summary>
    /// 诺诺发票服务类，包含获取访问令牌和创建发票的具体实现。
    /// </summary>
    public class NuoNuoInvoiceService
    {
        private readonly string _appKey;
        private readonly string _appSecret;
        private readonly string _tokenUrl;
        private readonly string _invoiceUrl;
        private readonly HttpClient _httpClient;

        /// <summary>
        /// 构造函数，初始化服务所需的参数。
        /// </summary>
        /// <param name="appKey">应用程序密钥。</param>
        /// <param name="appSecret">应用程序密钥。</param>
        /// <param name="tokenUrl">获取访问令牌的URL。</param>
        /// <param name="invoiceUrl">创建发票的URL。</param>
        /// <param name="httpClient">HttpClient实例。</param>
        public NuoNuoInvoiceService(string appKey, string appSecret, string tokenUrl, string invoiceUrl, HttpClient httpClient)
        {
            _appKey = appKey;
            _appSecret = appSecret;
            _tokenUrl = tokenUrl;
            _invoiceUrl = invoiceUrl;
            _httpClient = httpClient;
        }

        /// <summary>
        /// 异步获取访问令牌。
        /// </summary>
        /// <returns>返回访问令牌。</returns>
        public async Task<string> GetAccessTokenAsync()
        {
            // 创建表单内容，包含应用程序密钥和密钥
            var content = new FormUrlEncodedContent(new[]
            {
                    new KeyValuePair<string, string>("appKey", _appKey),
                    new KeyValuePair<string, string>("appSecret", _appSecret)
                });

            // 发送POST请求以获取访问令牌
            HttpResponseMessage response = await _httpClient.PostAsync(_tokenUrl, content);
            response.EnsureSuccessStatusCode();

            // 读取响应内容并解析JSON以获取访问令牌
            string responseBody = await response.Content.ReadAsStringAsync();
            var json = JsonSerializer.Deserialize<Dictionary<string, string>>(responseBody);
            return json["access_token"];
        }

        /// <summary>
        /// 异步创建发票。
        /// </summary>
        /// <param name="invoiceData">发票数据。</param>
        /// <returns>返回发票创建结果。</returns>
        public async Task<string> CreateInvoiceAsync(InvoiceData invoiceData)
        {
            // 获取访问令牌
            string accessToken = await GetAccessTokenAsync();

            // 设置请求头中的访问令牌
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("access_token", accessToken);

            // 序列化发票数据为JSON格式
            var jsonData = JsonSerializer.Serialize(invoiceData);
            var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

            // 发送POST请求以创建发票
            HttpResponseMessage response = await _httpClient.PostAsync(_invoiceUrl, content);
            response.EnsureSuccessStatusCode();

            // 读取响应内容并返回
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
        /// 项目名称
        /// </summary>
        public string ItemName { get; set; }

        /// <summary>
        /// 项目规格
        /// </summary>
        public string ItemSpec { get; set; }

        /// <summary>
        /// 项目单位
        /// </summary>
        public string ItemUnit { get; set; }

        /// <summary>
        /// 项目数量
        /// </summary>
        public int ItemQuantity { get; set; }

        /// <summary>
        /// 项目单价
        /// </summary>
        public decimal ItemPrice { get; set; }

        /// <summary>
        /// 项目金额（数量 * 单价）
        /// </summary>
        public decimal ItemAmount => ItemQuantity * ItemPrice;

        /// <summary>
        /// 项目税率
        /// </summary>
        public decimal ItemTaxRate { get; set; }

        /// <summary>
        /// 项目税额（金额 * 税率）
        /// </summary>
        public decimal ItemTaxAmount => ItemAmount * ItemTaxRate;
    }

    /// <summary>
    /// 发票数据模型类，定义发票数据的属性。
    /// </summary>
    public class InvoiceData
    {
        /// <summary>
        /// 购买方名称
        /// </summary>
        public string BuyerName { get; set; }

        /// <summary>
        /// 购买方税号
        /// </summary>
        public string BuyerTaxNum { get; set; }

        /// <summary>
        /// 购买方地址
        /// </summary>
        public string BuyerAddress { get; set; }

        /// <summary>
        /// 购买方电话
        /// </summary>
        public string BuyerTel { get; set; }

        /// <summary>
        /// 购买方开户行
        /// </summary>
        public string BuyerBankName { get; set; }

        /// <summary>
        /// 购买方银行账号
        /// </summary>
        public string BuyerBankAccount { get; set; }

        /// <summary>
        /// 发票类型
        /// </summary>
        public string InvoiceType { get; set; }

        /// <summary>
        /// 发票日期
        /// </summary>
        public DateTime InvoiceDate { get; set; }

        /// <summary>
        /// 发票项目列表
        /// </summary>
        public List<InvoiceItem> Items { get; set; }
    }

}
