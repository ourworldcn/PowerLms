using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PowerLms.Data.Finance
{
    /// <summary>
    /// 金蝶数据交换凭证分录模型
    /// </summary>
    [Table("KingdeeVouchers")]
    [Comment("金蝶数据交换凭证分录")]
    public class KingdeeVoucher
    {
        /// <summary>
        /// 主键ID（系统内部使用）
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        /// <summary>
        /// 制单日期
        /// </summary>
        [Column("FDATE")]
        [Comment("制单日期")]
        public DateTime FDATE { get; set; }

        /// <summary>
        /// 凭证日期
        /// </summary>
        [Column("FTRANSDATE")]
        [Comment("凭证日期")]
        public DateTime FTRANSDATE { get; set; }

        /// <summary>
        /// 期间，财务期间序号或月份
        /// </summary>
        [Column("FPERIOD")]
        [Comment("期间，财务期间序号或月份")]
        [Precision(10, 5)]
        public decimal FPERIOD { get; set; }

        /// <summary>
        /// 凭证类别字
        /// </summary>
        [Column("FGROUP")]
        [Comment("凭证类别字")]
        [StringLength(10)]
        public string FGROUP { get; set; }

        /// <summary>
        /// 凭证号
        /// </summary>
        [Column("FNUM")]
        [Comment("凭证号")]
        [Precision(10, 5)]
        public decimal FNUM { get; set; }

        /// <summary>
        /// 分录号，一个凭证号内不重复
        /// </summary>
        [Column("FENTRYID")]
        [Comment("分录号，一个凭证号内不重复")]
        [Precision(10, 5)]
        public decimal FENTRYID { get; set; }

        /// <summary>
        /// 摘要，客户名+开票明细+客户财务代码
        /// </summary>
        [Column("FEXP")]
        [Comment("摘要，客户名+开票明细+客户财务代码")]
        [StringLength(500)]
        public string FEXP { get; set; }

        /// <summary>
        /// 科目代码
        /// </summary>
        [Column("FACCTID")]
        [Comment("科目代码")]
        [StringLength(50)]
        public string FACCTID { get; set; }

        /// <summary>
        /// 核算类别
        /// </summary>
        [Column("FCLSNAME1")]
        [Comment("核算类别")]
        [StringLength(50)]
        public string FCLSNAME1 { get; set; }

        /// <summary>
        /// 客户财务简称
        /// </summary>
        [Column("FOBJID1")]
        [Comment("客户财务简称")]
        [StringLength(50)]
        public string FOBJID1 { get; set; }

        /// <summary>
        /// 客户名称
        /// </summary>
        [Column("FOBJNAME1")]
        [Comment("客户名称")]
        [StringLength(200)]
        public string FOBJNAME1 { get; set; }

        /// <summary>
        /// 核算类别2
        /// </summary>
        [Column("FCLSNAME2")]
        [StringLength(50)]
        public string FCLSNAME2 { get; set; }

        /// <summary>
        /// 核算对象编码2
        /// </summary>
        [Column("FOBJID2")]
        [StringLength(50)]
        public string FOBJID2 { get; set; }

        /// <summary>
        /// 核算对象名称2
        /// </summary>
        [Column("FOBJNAME2")]
        [StringLength(200)]
        public string FOBJNAME2 { get; set; }

        /// <summary>
        /// 核算类别3
        /// </summary>
        [Column("FCLSNAME3")]
        [StringLength(50)]
        public string FCLSNAME3 { get; set; }

        /// <summary>
        /// 核算对象编码3
        /// </summary>
        [Column("FOBJID3")]
        [StringLength(50)]
        public string FOBJID3 { get; set; }

        /// <summary>
        /// 核算对象名称3
        /// </summary>
        [Column("FOBJNAME3")]
        [StringLength(200)]
        public string FOBJNAME3 { get; set; }

        /// <summary>
        /// 核算类别4
        /// </summary>
        [Column("FCLSNAME4")]
        [StringLength(50)]
        public string FCLSNAME4 { get; set; }

        /// <summary>
        /// 核算对象编码4
        /// </summary>
        [Column("FOBJID4")]
        [StringLength(50)]
        public string FOBJID4 { get; set; }

        /// <summary>
        /// 核算对象名称4
        /// </summary>
        [Column("FOBJNAME4")]
        [StringLength(200)]
        public string FOBJNAME4 { get; set; }

        /// <summary>
        /// 核算类别5
        /// </summary>
        [Column("FCLSNAME5")]
        [StringLength(50)]
        public string FCLSNAME5 { get; set; }

        /// <summary>
        /// 核算对象编码5
        /// </summary>
        [Column("FOBJID5")]
        [StringLength(50)]
        public string FOBJID5 { get; set; }

        /// <summary>
        /// 核算对象名称5
        /// </summary>
        [Column("FOBJNAME5")]
        [StringLength(200)]
        public string FOBJNAME5 { get; set; }

        /// <summary>
        /// 核算类别6
        /// </summary>
        [Column("FCLSNAME6")]
        [StringLength(50)]
        public string FCLSNAME6 { get; set; }

        /// <summary>
        /// 核算对象编码6
        /// </summary>
        [Column("FOBJID6")]
        [StringLength(50)]
        public string FOBJID6 { get; set; }

        /// <summary>
        /// 核算对象名称6
        /// </summary>
        [Column("FOBJNAME6")]
        [StringLength(200)]
        public string FOBJNAME6 { get; set; }

        /// <summary>
        /// 核算类别7
        /// </summary>
        [Column("FCLSNAME7")]
        [StringLength(50)]
        public string FCLSNAME7 { get; set; }

        /// <summary>
        /// 核算对象编码7
        /// </summary>
        [Column("FOBJID7")]
        [StringLength(50)]
        public string FOBJID7 { get; set; }

        /// <summary>
        /// 核算对象名称7
        /// </summary>
        [Column("FOBJNAME7")]
        [StringLength(200)]
        public string FOBJNAME7 { get; set; }

        /// <summary>
        /// 核算类别8
        /// </summary>
        [Column("FCLSNAME8")]
        [StringLength(50)]
        public string FCLSNAME8 { get; set; }

        /// <summary>
        /// 核算对象编码8
        /// </summary>
        [Column("FOBJID8")]
        [StringLength(50)]
        public string FOBJID8 { get; set; }

        /// <summary>
        /// 核算对象名称8
        /// </summary>
        [Column("FOBJNAME8")]
        [StringLength(200)]
        public string FOBJNAME8 { get; set; }

        /// <summary>
        /// 核算类别9
        /// </summary>
        [Column("FCLSNAME9")]
        [StringLength(50)]
        public string FCLSNAME9 { get; set; }

        /// <summary>
        /// 核算对象编码9
        /// </summary>
        [Column("FOBJID9")]
        [StringLength(50)]
        public string FOBJID9 { get; set; }

        /// <summary>
        /// 核算对象名称9
        /// </summary>
        [Column("FOBJNAME9")]
        [StringLength(200)]
        public string FOBJNAME9 { get; set; }

        /// <summary>
        /// 核算类别10
        /// </summary>
        [Column("FCLSNAME10")]
        [StringLength(50)]
        public string FCLSNAME10 { get; set; }

        /// <summary>
        /// 核算对象编码10
        /// </summary>
        [Column("FOBJID10")]
        [StringLength(50)]
        public string FOBJID10 { get; set; }

        /// <summary>
        /// 核算对象名称10
        /// </summary>
        [Column("FOBJNAME10")]
        [StringLength(200)]
        public string FOBJNAME10 { get; set; }

        /// <summary>
        /// 核算类别11
        /// </summary>
        [Column("FCLSNAME11")]
        [StringLength(50)]
        public string FCLSNAME11 { get; set; }

        /// <summary>
        /// 核算对象编码11
        /// </summary>
        [Column("FOBJID11")]
        [StringLength(50)]
        public string FOBJID11 { get; set; }

        /// <summary>
        /// 核算对象名称11
        /// </summary>
        [Column("FOBJNAME11")]
        [StringLength(200)]
        public string FOBJNAME11 { get; set; }

        /// <summary>
        /// 核算类别12
        /// </summary>
        [Column("FCLSNAME12")]
        [StringLength(50)]
        public string FCLSNAME12 { get; set; }

        /// <summary>
        /// 核算对象编码12
        /// </summary>
        [Column("FOBJID12")]
        [StringLength(50)]
        public string FOBJID12 { get; set; }

        /// <summary>
        /// 核算对象名称12
        /// </summary>
        [Column("FOBJNAME12")]
        [StringLength(200)]
        public string FOBJNAME12 { get; set; }

        /// <summary>
        /// 核算类别13
        /// </summary>
        [Column("FCLSNAME13")]
        [StringLength(50)]
        public string FCLSNAME13 { get; set; }

        /// <summary>
        /// 核算对象编码13
        /// </summary>
        [Column("FOBJID13")]
        [StringLength(50)]
        public string FOBJID13 { get; set; }

        /// <summary>
        /// 核算对象名称13
        /// </summary>
        [Column("FOBJNAME13")]
        [StringLength(200)]
        public string FOBJNAME13 { get; set; }

        /// <summary>
        /// 核算类别14
        /// </summary>
        [Column("FCLSNAME14")]
        [StringLength(50)]
        public string FCLSNAME14 { get; set; }

        /// <summary>
        /// 核算对象编码14
        /// </summary>
        [Column("FOBJID14")]
        [StringLength(50)]
        public string FOBJID14 { get; set; }

        /// <summary>
        /// 核算对象名称14
        /// </summary>
        [Column("FOBJNAME14")]
        [StringLength(200)]
        public string FOBJNAME14 { get; set; }

        /// <summary>
        /// 核算类别15
        /// </summary>
        [Column("FCLSNAME15")]
        [StringLength(50)]
        public string FCLSNAME15 { get; set; }

        /// <summary>
        /// 核算对象编码15
        /// </summary>
        [Column("FOBJID15")]
        [StringLength(50)]
        public string FOBJID15 { get; set; }

        /// <summary>
        /// 核算对象名称15
        /// </summary>
        [Column("FOBJNAME15")]
        [StringLength(200)]
        public string FOBJNAME15 { get; set; }

        /// <summary>
        /// 客户财务编码
        /// </summary>
        [Column("FTRANSID")]
        [Comment("客户财务编码")]
        [StringLength(50)]
        public string FTRANSID { get; set; }

        /// <summary>
        /// 币别代码
        /// </summary>
        [Column("FCYID")]
        [Comment("币别代码")]
        [StringLength(10)]
        public string FCYID { get; set; }

        /// <summary>
        /// 汇率
        /// </summary>
        [Column("FEXCHRATE")]
        [Comment("汇率")]
        [Precision(18, 7)]
        public decimal FEXCHRATE { get; set; }

        /// <summary>
        /// 借贷方向：0-借方，1-贷方
        /// </summary>
        [Column("FDC")]
        [Comment("借贷方向：0-借方，1-贷方")]
        public int FDC { get; set; }

        /// <summary>
        /// 外币金额
        /// </summary>
        [Column("FFCYAMT")]
        [Comment("外币金额")]
        [Precision(18, 5)]
        public decimal? FFCYAMT { get; set; }

        /// <summary>
        /// 数量
        /// </summary>
        [Column("FQTY")]
        [Comment("数量")]
        [Precision(18, 5)]
        public decimal? FQTY { get; set; }

        /// <summary>
        /// 单价
        /// </summary>
        [Column("FPRICE")]
        [Comment("单价")]
        [Precision(18, 5)]
        public decimal? FPRICE { get; set; }

        /// <summary>
        /// 金额借方
        /// </summary>
        [Column("FDEBIT")]
        [Comment("金额借方")]
        [Precision(18, 5)]
        public decimal? FDEBIT { get; set; }

        /// <summary>
        /// 金额贷方
        /// </summary>
        [Column("FCREDIT")]
        [Comment("金额贷方")]
        [Precision(18, 5)]
        public decimal? FCREDIT { get; set; }

        /// <summary>
        /// 结算方式代码
        /// </summary>
        [Column("FSETTLCODE")]
        [Comment("结算方式代码")]
        [StringLength(20)]
        public string FSETTLCODE { get; set; }

        /// <summary>
        /// 结算号
        /// </summary>
        [Column("FSETTLENO")]
        [Comment("结算号")]
        [StringLength(50)]
        public string FSETTLENO { get; set; }

        /// <summary>
        /// 制单人姓名
        /// </summary>
        [Column("FPREPARE")]
        [Comment("制单人姓名")]
        [StringLength(50)]
        public string FPREPARE { get; set; }

        /// <summary>
        /// 支付
        /// </summary>
        [Column("FPAY")]
        [Comment("支付")]
        [StringLength(50)]
        public string FPAY { get; set; }

        /// <summary>
        /// 出纳人姓名
        /// </summary>
        [Column("FCASH")]
        [Comment("出纳人姓名")]
        [StringLength(50)]
        public string FCASH { get; set; }

        /// <summary>
        /// 过帐人姓名
        /// </summary>
        [Column("FPOSTER")]
        [Comment("过帐人姓名")]
        [StringLength(50)]
        public string FPOSTER { get; set; }

        /// <summary>
        /// 审核人姓名
        /// </summary>
        [Column("FCHECKER")]
        [Comment("审核人姓名")]
        [StringLength(50)]
        public string FCHECKER { get; set; }

        /// <summary>
        /// 附单据数
        /// </summary>
        [Column("FATTCHMENT")]
        [Comment("附单据数")]
        public int? FATTCHMENT { get; set; }

        /// <summary>
        /// 过帐状态
        /// </summary>
        [Column("FPOSTED")]
        [Comment("过帐状态")]
        public int? FPOSTED { get; set; }

        /// <summary>
        /// 模块
        /// </summary>
        [Column("FMODULE")]
        [Comment("模块")]
        [StringLength(50)]
        public string FMODULE { get; set; }

        /// <summary>
        /// 删除标记
        /// </summary>
        [Column("FDELETED")]
        [Comment("删除标记")]
        public bool? FDELETED { get; set; }

        /// <summary>
        /// 序列号
        /// </summary>
        [Column("FSERIALNO")]
        [Comment("序列号")]
        [StringLength(50)]
        public string FSERIALNO { get; set; }

        /// <summary>
        /// 单位名称
        /// </summary>
        [Column("FUNITNAME")]
        [Comment("单位名称")]
        [StringLength(100)]
        public string FUNITNAME { get; set; }

        /// <summary>
        /// 参考
        /// </summary>
        [Column("FREFERENCE")]
        [Comment("参考")]
        [StringLength(200)]
        public string FREFERENCE { get; set; }

        /// <summary>
        /// 现金流
        /// </summary>
        [Column("FCASHFLOW")]
        [Comment("现金流")]
        [StringLength(50)]
        public string FCASHFLOW { get; set; }

        /// <summary>
        /// 处理者
        /// </summary>
        [Column("FHANDLER")]
        [Comment("处理者")]
        [StringLength(50)]
        public string FHANDLER { get; set; }
    }
}
