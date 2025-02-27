using Microsoft.EntityFrameworkCore;
using OW.Data;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PowerLms.Data
{
    /// <summary>
    /// 客户税务信息/开票信息。
    /// </summary>
    [Index(nameof(CustomerId), IsUnique = false)]
    public class PlTaxInfo : GuidKeyObjectBase
    {
        /// <summary>
        /// 所属客户Id。
        /// </summary>
        [Comment("所属客户Id")]
        public Guid CustomerId { get; set; }

        /// <summary>
        /// 抬头名称。
        /// </summary>
        [Comment("抬头名称")]
        public string Title { get; set; }

        /// <summary>
        /// 纳税人种类Id。简单字典AddedTaxType。
        /// </summary>
        [Comment("纳税人种类Id。简单字典AddedTaxType。")]
        public Guid? Type { get; set; }

        /// <summary>
        /// 纳税人识别号。
        /// </summary>
        [Comment("纳税人识别号")]
        [MaxLength(64)]
        public string Number { get; set; }

        /// <summary>
        /// 人民币账号。
        /// </summary>
        [Comment("人民币账号")]
        [MaxLength(64)]
        public string BankStdCoin { get; set; }

        /// <summary>
        /// 开户行。
        /// </summary>
        [Comment("开户行")]
        [MaxLength(64)]
        public string BankNoRMB { get; set; }

        /// <summary>
        /// 美金账户。
        /// </summary>
        [Comment("美金账户")]
        [MaxLength(64)]
        public string BankUSD { get; set; }

        /// <summary>
        /// 美金开户行。
        /// </summary>
        [Comment("美金开户行")]
        [MaxLength(64)]
        public string BankNoUSD { get; set; }

        /// <summary>
        /// 地址。
        /// </summary>
        [Comment("地址")]
        [MaxLength(256)]
        public string Addr { get; set; }

        /// <summary>
        /// 电话。
        /// </summary>
        [Comment("电话")]
        [MaxLength(32)]
        public string Tel { get; set; }

        /// <summary>
        /// 手机号。
        /// </summary>
        [Comment("手机号")]
        [MaxLength(32)]
        public string Mobile { get; set; }

        /// <summary>
        /// 电子邮件地址。
        /// </summary>
        [Comment("电子邮件地址")]
        [MaxLength(256), EmailAddress]
        public string EMail { get; set; }

        /// <summary>
        /// 税率。
        /// </summary>
        [Comment("税率")]
        public int TaxRate { get; set; }

        /// <summary>
        /// 发票邮寄地址。
        /// </summary>
        [Comment("发票邮寄地址")]
        [MaxLength(256)]
        public string InvoiceSignAddr { get; set; }

    }

}
