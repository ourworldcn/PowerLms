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
using AutoMapper;
using AutoMapper.Configuration.Annotations;


namespace PowerLmsServer.Managers
{
    /// <summary>诺税通saas请求开具发票接口请求类(V2.0)</summary>
    public class NuoNuoRequest
    {
        #region 基本信息
        /// <summary>唯一标识，由企业自己生成32位随机码</summary>
        [JsonPropertyName("senid")]
        public string Senid { get; set; }

        /// <summary>8位随机正整数</summary>
        [JsonPropertyName("nonce")]
        public int Nonce { get; set; }

        /// <summary>时间戳(当前时间的秒数)</summary>
        [JsonPropertyName("timestamp")]
        public long Timestamp { get; set; }

        /// <summary>平台分配给应用的appKey</summary>
        [JsonPropertyName("appkey")]
        public string AppKey { get; set; }

        /// <summary>请求api对应的方法名称</summary>
        [JsonPropertyName("method")]
        public string Method { get; set; }

        /// <summary>订单信息</summary>
        [JsonPropertyName("order")]
        public NNOrder Order { get; set; }
        #endregion
    }

    /// <summary>发票数据</summary>
    public class NNOrder
    {
        #region 购方信息
        /// <summary>购方名称</summary>
        [JsonPropertyName("buyerName")]
        public string BuyerName { get; set; }

        /// <summary>购方税号</summary>
        [JsonPropertyName("buyerTaxNum")]
        public string BuyerTaxNum { get; set; }

        /// <summary>购方电话</summary>
        [JsonPropertyName("buyerTel")]
        public string BuyerTel { get; set; }

        /// <summary>购方手机(用于发票推送)</summary>
        [JsonPropertyName("buyerPhone")]
        public string BuyerPhone { get; set; }

        /// <summary>购方地址</summary>
        [JsonPropertyName("buyerAddress")]
        public string BuyerAddress { get; set; }

        /// <summary>购方银行账号</summary>
        [JsonPropertyName("buyerAccount")]
        public string BuyerAccount { get; set; }

        /// <summary>推送邮箱</summary>
        [JsonPropertyName("email")]
        public string Email { get; set; }
        #endregion

        #region 销方信息
        /// <summary>销方税号</summary>
        [JsonPropertyName("salerTaxNum")]
        public string SalerTaxNum { get; set; }

        /// <summary>销方电话</summary>
        [JsonPropertyName("salerTel")]
        public string SalerTel { get; set; }

        /// <summary>销方地址</summary>
        [JsonPropertyName("salerAddress")]
        public string SalerAddress { get; set; }

        /// <summary>销方银行账号</summary>
        [JsonPropertyName("salerAccount")]
        public string SalerAccount { get; set; }
        #endregion

        #region 订单信息
        /// <summary>订单号(每个企业唯一)</summary>
        [JsonPropertyName("orderNo")]
        public string OrderNo { get; set; }

        /// <summary>订单时间</summary>
        [JsonPropertyName("invoiceDate")]
        public DateTime InvoiceDate { get; set; }
        #endregion

        #region 发票信息
        /// <summary>开票类型：1:蓝票; 2:红票</summary>
        [JsonPropertyName("invoiceType")]
        public int InvoiceType { get; set; }

        /// <summary>发票种类</summary>
        /// <value>发票种类：p,普通发票(电票)(默认);c,普通发票(纸票); s,专用发票;e,收购发票(电票); f,收购发票(纸质); r,普通发票(卷式); b,增值税电子专用发票; 
        /// j,机动车销售统一发票;u,二手车销售统一发票; bs:电子发票(增值税专用发票)-即数电专票(电子),pc:电子发票(普通发票)-即数电普票(电子),
        /// es:数电纸质发票(增值税专用发票)-即数电专票(纸质); ec:数电纸质发票(普通发票)-即数电普票(纸质)</value>
        [JsonPropertyName("invoiceLine")]
        public string InvoiceLine { get; set; }

        /// <summary>发票明细</summary>
        [JsonPropertyName("invoiceDetail")]
        public List<NNInvoiceDetail> InvoiceDetail { get; set; }
        #endregion

        #region 其他信息
        /// <summary>开票员</summary>
        [JsonPropertyName("clerk")]
        public string Clerk { get; set; }

        /// <summary>推送方式：-1不推送;0邮箱;1手机;2邮箱和手机</summary>
        [JsonPropertyName("pushMode")]
        public string PushMode { get; set; }

        /// <summary>备注信息</summary>
        [JsonPropertyName("remark")]
        public string Remark { get; set; }
        #endregion
    }

    /// <summary>发票明细</summary>
    public class NNInvoiceDetail
    {
        #region 商品信息
        /// <summary>商品名称</summary>
        [JsonPropertyName("goodsName")]
        public string GoodsName { get; set; }

        /// <summary>单位</summary>
        [JsonPropertyName("unit")]
        public string Unit { get; set; }

        /// <summary>规格型号</summary>
        [JsonPropertyName("specType")]
        public string SpecType { get; set; }
        #endregion

        #region 金额信息
        /// <summary>单价含税标志：0不含税;1含税</summary>
        [JsonPropertyName("withTaxFlag")]
        public int WithTaxFlag { get; set; }

        /// <summary>单价</summary>
        [JsonPropertyName("price")]
        public string Price { get; set; }

        /// <summary>数量</summary>
        [JsonPropertyName("num")]
        public string Num { get; set; }

        /// <summary>税率</summary>
        [JsonPropertyName("taxRate")]
        public decimal TaxRate { get; set; }

        /// <summary>税额</summary>
        [JsonPropertyName("tax")]
        public string Tax { get; set; }

        /// <summary>不含税金额</summary>
        [JsonPropertyName("taxExcludedAmount")]
        public string TaxExcludedAmount { get; set; }

        /// <summary>含税金额</summary>
        [JsonPropertyName("taxIncludedAmount")]
        public string TaxIncludedAmount { get; set; }
        #endregion

        #region 发票行与政策信息
        /// <summary>发票行性质：0正常行;1折扣行;2被折扣行</summary>
        [JsonPropertyName("invoiceLineProperty")]
        public string InvoiceLineProperty { get; set; } = "0";

        /// <summary>优惠政策标识：0不使用;1使用</summary>
        [JsonPropertyName("favouredPolicyFlag")]
        public string FavouredPolicyFlag { get; set; } = "0";

        /// <summary>优惠政策名称</summary>
        [JsonPropertyName("favouredPolicyName")]
        public string FavouredPolicyName { get; set; } = "";

        /// <summary>扣除额，差额征收时填写</summary>
        [JsonPropertyName("deduction")]
        public string Deduction { get; set; } = "0";

        /// <summary>零税率标识</summary>
        [JsonPropertyName("zeroRateFlag")]
        public string ZeroRateFlag { get; set; } = "0";
        #endregion
    }

    #region 开票结果类
    /// <summary>诺诺开票结果类</summary>
    public class NuoNuoInvoiceResult
    {
        /// <summary>是否成功</summary>
        public bool Success { get; set; }

        /// <summary>发票流水号</summary>
        public string InvoiceSerialNum { get; set; }

        /// <summary>错误代码</summary>
        public string ErrorCode { get; set; }

        /// <summary>错误信息</summary>
        public string ErrorMessage { get; set; }
    }

    /// <summary>诺诺开票响应类</summary>
    public class NuoNuoInvoiceResponse
    {
        /// <summary>响应代码</summary>
        [JsonPropertyName("code")]
        public string Code { get; set; }

        /// <summary>响应描述</summary>
        [JsonPropertyName("describe")]
        public string Describe { get; set; }

        /// <summary>响应结果</summary>
        [JsonPropertyName("result")]
        public NuoNuoInvoiceResponseResult Result { get; set; }
    }

    /// <summary>诺诺开票响应结果类</summary>
    public class NuoNuoInvoiceResponseResult
    {
        /// <summary>发票流水号</summary>
        [JsonPropertyName("invoiceSerialNum")]
        public string InvoiceSerialNum { get; set; }
    }

    #endregion 开票结果类
}
