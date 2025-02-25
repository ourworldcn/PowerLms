﻿using Microsoft.EntityFrameworkCore;
using OW.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PowerLms.Data
{
    /// <summary>
    /// 客户资料。
    /// </summary>
    public class PlCustomer : GuidKeyObjectBase, ICreatorInfo
    {
        /// <summary>
        /// 所属组织机构的Id。
        /// </summary>
        [Comment("所属组织机构的Id。")]
        public Guid? OrgId { get; set; }

        /// <summary>
        /// 海关编码。
        /// </summary>
        [Comment("海关编码。")]
        [MaxLength(32)]
        public string CustomCode { get; set; }

        /// <summary>
        /// 客户编码。
        /// </summary>
        [Comment("客户编码")]
        [MaxLength(32)]
        public string Code { get; set; }

        /// <summary>
        /// 正式名称，拥有相对稳定性。
        /// </summary>
        [Comment("正式名称，拥有相对稳定性")]
        [MaxLength(64)]
        public string Name_Name { get; set; }

        /// <summary>
        /// 正式简称。对正式的组织机构通常简称也是规定的。
        /// </summary>
        [Comment("正式简称，对正式的组织机构通常简称也是规定的")]
        [MaxLength(32)]
        public string Name_ShortName { get; set; }

        /// <summary>
        /// 显示名，有时它是昵称或简称(系统内)的意思。
        /// </summary>
        [Comment("显示名，有时它是昵称或简称(系统内)的意思")]
        public string Name_DisplayName { get; set; }

        /// <summary>
        /// 纳税人识别号。
        /// </summary>
        [Comment("纳税人识别号")]
        public string CrideCode { get; set; }

        /// <summary>
        /// 编号。
        /// </summary>
        [Comment("编号")]
        public string Number { get; set; }

        /// <summary>
        /// 电话。
        /// </summary>
        [Comment("电话")]
        [MaxLength(32), Phone]
        public string Contact_Tel { get; set; }

        /// <summary>
        /// 传真。
        /// </summary>
        [Comment("传真")]
        [MaxLength(32), Phone]
        public string Contact_Fax { get; set; }

        /// <summary>
        /// 电子邮件。
        /// </summary>
        [Comment("电子邮件")]
        [MaxLength(128), EmailAddress]
        public string Contact_EMail { get; set; }

        /// <summary>
        /// 国家编码Id。
        /// </summary>
        [Comment("国家编码Id")]
        public Guid? Address_CountryId { get; set; }

        /// <summary>
        /// 省。
        /// </summary>
        [Comment("省")]
        [MaxLength(64)]
        public string Address_Province { get; set; }

        /// <summary>
        /// 地市。
        /// </summary>
        [Comment("地市")]
        [MaxLength(64)]
        public string Address_City { get; set; }

        /// <summary>
        /// 详细地址。
        /// </summary>
        [Comment("详细地址")]
        [MaxLength(64)]
        public string Address_Address { get; set; }

        /// <summary>
        /// 邮政编码。
        /// </summary>
        [Comment("邮政编码")]
        [MaxLength(8)]
        public string Address_ZipCode { get; set; }
        /// <summary>
        /// 网址。
        /// </summary>
        [Comment("网址")]
        [MaxLength(1024)]
        public string InternetAddress { get; set; }


        /// <summary>
        /// 搜索用的关键字。逗号分隔多个关键字。
        /// </summary>
        [Comment("搜索用的关键字。逗号分隔多个关键字。")]
        [MaxLength(128)]
        public string Keyword { get; set; }

        #region 账单信息

        /// <summary>
        /// 是否应收结算单位
        /// </summary>
        [Comment("是否应收结算单位")]
        public bool? BillingInfo_IsExesGather { get; set; }

        /// <summary>
        /// 是否应付结算单位
        /// </summary>
        [Comment("是否应付结算单位")]
        public bool? BillingInfo_IsExesPayer { get; set; }

        /// <summary>
        /// 信用期限天数
        /// </summary>
        [Comment("信用期限天数")]
        public int? BillingInfo_Dayslimited { get; set; }

        /// <summary>
        /// 拖欠限额币种Id
        /// </summary>
        [Comment("拖欠限额币种Id")]
        public Guid? BillingInfo_CurrtypeId { get; set; }

        /// <summary>
        /// 拖欠金额。
        /// </summary>
        [Comment("拖欠金额")]
        public decimal? BillingInfo_AmountLimited { get; set; }

        /// <summary>
        /// 付费方式Id。
        /// </summary>
        [Comment("付费方式Id")]
        public Guid? BillingInfo_AmountTypeId { get; set; }

        /// <summary>
        /// 是否超额黑名单
        /// </summary>
        [Comment("是否超额黑名单")]
        public bool? BillingInfo_IsCEBlack { get; set; }

        /// <summary>
        /// 是否超期黑名单
        /// </summary>
        [Comment("是否超期黑名单")]
        public bool? BillingInfo_IsBlack { get; set; }

        /// <summary>
        /// 是否特别注意
        /// </summary>
        [Comment("是否特别注意")]
        public bool? BillingInfo_IsNeedTrace { get; set; }

        #endregion 账单信息

        /// <summary>
        /// 货主性质Id。
        /// </summary>
        [Comment("货主性质Id")]
        public Guid? ShipperPropertyId { get; set; }

        /// <summary>
        /// 客户级别Id。
        /// </summary>
        [Comment("客户级别Id")]
        public Guid? CustomerLevelId { get; set; }

        /// <summary>
        /// 是否有效。
        /// </summary>
        [Comment("是否有效")]
        public bool IsValid { get; set; }

        /// <summary>
        /// 备注。
        /// </summary>
        [Comment("备注")]
        public string Remark { get; set; }

        #region Airlines 相关属性

        /// <summary>
        /// 航空公司2位代码（如国航为CA）。此项空则表示整个航空公司不生效。
        /// </summary>
        [MaxLength(2)]
        [Comment("航空公司2位代码（如国航为CA）")]
        public string Airlines_AirlineCode { get; set; }

        /// <summary>
        /// 3位，如国航999。
        /// </summary>
        [MaxLength(3)]
        [Comment("3位，如国航999")]
        public string Airlines_NumberCode { get; set; }

        /// <summary>
        /// 付款方式，关联简单字典BillPaymentMode。
        /// </summary>
        [Comment("付款方式Id，关联简单字典BillPaymentMode")]
        public Guid? Airlines_PayModeId { get; set; }

        /// <summary>
        /// 付款地点。
        /// </summary>
        [MaxLength(64)]
        [Comment("付款地点")]
        public string Airlines_PaymentPlace { get; set; }

        /// <summary>
        /// 交单地，简单字典DocumentsPlace。
        /// </summary>
        [Comment("交单地，简单字典DocumentsPlace")]
        public Guid? Airlines_DocumentsPlaceId { get; set; }

        /// <summary>
        /// 结算方式，cass=true/非Cass=false/空=null。
        /// </summary>
        [Comment("结算方式，cass=true/非Cass=false/空=null")]
        public bool? Airlines_SettlementModes { get; set; }

        #endregion Airlines 相关属性

        /// <summary>
        /// 财务编码。
        /// </summary>
        [MaxLength(32)]
        [Comment("财务编码")]
        public string TacCountNo { get; set; }

        /// <summary>
        /// 是否海关检疫。
        /// </summary>
        [Comment("是否海关检疫")]
        public bool IsCustomsQuarantine { get; set; }
        #region 客户性质

        /// <summary>
        /// 是否委托单位。
        /// </summary>
        [Comment("是否委托单位")]
        public bool IsShipper { get; set; }

        /// <summary>
        /// 是否结算单位。
        /// </summary>
        [Comment("是否结算单位")]
        public bool IsBalance { get; set; }

        /// <summary>
        /// 是否发货人。
        /// </summary>
        [Comment("是否发货人")]
        public bool IsConsignor { get; set; }

        /// <summary>
        /// 是否收货人。
        /// </summary>
        [Comment("是否收货人")]
        public bool IsConsignee { get; set; }

        /// <summary>
        /// 是否通知人。
        /// </summary>
        [Comment("是否通知人")]
        public bool IsNotify { get; set; }

        /// <summary>
        /// 是否航空公司。
        /// </summary>
        [Comment("是否航空公司")]
        public bool IsAirway { get; set; }

        /// <summary>
        /// 是否船公司
        /// </summary>
        [Comment("是否船公司")]
        public bool IsShipOwner { get; set; }

        /// <summary>
        /// 是否订舱代理
        /// </summary>
        [Comment("是否订舱代理")]
        public bool IsBookingAgent { get; set; }

        /// <summary>
        /// 是否目的港代理
        /// </summary>
        [Comment("是否目的港代理")]
        public bool IsDestAgent { get; set; }

        /// <summary>
        /// 是否卡车公司
        /// </summary>
        [Comment("是否卡车公司")]
        public bool IsLocal { get; set; }

        /// <summary>
        /// 是否报关行
        /// </summary>
        [Comment("是否报关行")]
        public bool IsCustom { get; set; }

        /// <summary>
        /// 是否保险公司
        /// </summary>
        [Comment("是否保险公司")]
        public bool IsInsure { get; set; }

        /// <summary>
        /// 是否供货商
        /// </summary>
        [Comment("是否供货商")]
        public bool IsProvide { get; set; }

        /// <summary>
        /// 是否仓储公司
        /// </summary>
        [Comment("是否仓储公司")]
        public bool IsStock { get; set; }

        /// <summary>
        /// 是否其他
        /// </summary>
        [Comment("是否其他")]
        public bool IsOthers { get; set; }

        /// <summary>
        /// 创建者的唯一标识。
        /// </summary>
        [Comment("创建者的唯一标识")]
        public Guid? CreateBy { get; set; }

        /// <summary>
        /// 创建的时间。
        /// </summary>
        [Comment("创建的时间")]
        public DateTime CreateDateTime { get; set; } = OwHelper.WorldNow;

        #endregion 客户性质
    }

    /// <summary>
    /// 航空公司内嵌类。
    /// </summary>
    [ComplexType]
    [Owned]
    public class OwnedAirlines
    {
        /*
         * AirlineCode	航空公司2位代码（如国航为CA）
         * Airline number code	3位，如国航999
         * paymode	付款方式，关联简单字典BillPaymentMode
         * paymentplace	付款地点
         * DocumentsPlace	交单地，简单字典DocumentsPlace
         * SettlementModes	结算方式，cass/非Cass/空
         */

        /// <summary>
        /// 航空公司2位代码（如国航为CA）。此项空则表示整个航空公司不生效。
        /// </summary>
        [MaxLength(2)]
        [Comment("航空公司2位代码（如国航为CA）")]
        public string AirlineCode { get; set; }

        /// <summary>
        /// 3位，如国航999。
        /// </summary>
        [MaxLength(3)]
        [Comment("3位，如国航999")]
        public string NumberCode { get; set; }

        /// <summary>
        /// 付款方式，关联简单字典BillPaymentMode。
        /// </summary>
        [Comment("付款方式Id，关联简单字典BillPaymentMode")]
        public Guid? PayModeId { get; set; }

        /// <summary>
        /// 付款地点。
        /// </summary>
        [MaxLength(64)]
        [Comment("付款地点")]
        public string PaymentPlace { get; set; }

        /// <summary>
        /// 交单地，简单字典DocumentsPlace。
        /// </summary>
        [Comment("交单地，简单字典DocumentsPlace")]
        public Guid? DocumentsPlaceId { get; set; }

        /// <summary>
        /// 结算方式，cass=true/非Cass=false/空=null。
        /// </summary>
        [Comment("结算方式，cass=true/非Cass=false/空=null")]
        public bool? SettlementModes { get; set; }
    }

    /// <summary>
    /// 客户资料的联系人。
    /// </summary>
    [Index(nameof(CustomerId), IsUnique = false)]
    public class PlCustomerContact : GuidKeyObjectBase
    {
        /// <summary>
        /// 所属客户的Id。
        /// </summary>
        public Guid CustomerId { get; set; }

        /// <summary>
        /// 姓名。
        /// </summary>
        [Comment("姓名。")]
        [MaxLength(32)]
        public string DisplayName { get; set; }

        /// <summary>
        /// 性别Id。
        /// </summary>
        [Comment("性别Id。")]
        public Guid? SexId { get; set; }

        /// <summary>
        /// 职务/行政级别。
        /// </summary>
        [Comment("职务/行政级别。")]
        [MaxLength(32)]
        public string Title { get; set; }

        /// <summary>
        /// 联系方式的封装。
        /// </summary>
        public PlOwnedContact Contact { get; set; }

        /// <summary>
        /// 移动电话。
        /// </summary>
        [Comment("移动电话。")]
        [MaxLength(32)]
        public string Mobile { get; set; }

        /// <summary>
        /// 开户行。
        /// </summary>
        [Comment("开户行。")]
        [MaxLength(64)]
        public string Bank { get; set; }

        /// <summary>
        /// 银行账号。
        /// </summary>
        [Comment("银行账号。")]
        [MaxLength(64)]
        public string Account { get; set; }

        /// <summary>
        /// 搜索用的关键字。逗号分隔多个关键字。
        /// </summary>
        [Comment("搜索用的关键字。逗号分隔多个关键字。")]
        [MaxLength(128)]
        public string Keyword { get; set; }

        /// <summary>
        /// 备注。
        /// </summary>
        [Comment("备注。")]
        [MaxLength(256)]
        public string Remark { get; set; }

    }

    /// <summary>
    /// 业务负责人表。
    /// </summary>
    [Index(nameof(CustomerId), IsUnique = false)]
    public class PlBusinessHeader
    {
        /// <summary>
        /// 所属客户Id。
        /// </summary>
        [Comment("客户Id")]
        public Guid CustomerId { get; set; }

        /// <summary>
        /// 用户Id。
        /// </summary>
        [Comment("用户Id")]
        public Guid UserId { get; set; }

        /// <summary>
        /// 负责的业务Id。连接业务种类字典。
        /// </summary>
        [Comment("负责的业务Id。连接业务种类字典。")]
        public Guid OrderTypeId { get; set; }
    }

    /// <summary>
    /// 客户提单内容表。
    /// </summary>
    [Index(nameof(CustomerId), IsUnique = false)]
    public class PlTidan : GuidKeyObjectBase
    {
        /// <summary>
        /// 所属客户Id。
        /// </summary>
        [Comment("所属客户Id")]
        public Guid CustomerId { get; set; }

        /// <summary>
        /// 提单内容。
        /// </summary>
        [Comment("提单内容")]
        public string Title { get; set; }

        /// <summary>
        /// 创建时间。默认值为创建对象的时间。
        /// </summary>
        [Comment("创建时间。默认值为创建对象的时间。")]
        public DateTime CreateDateTime { get; set; } = OwHelper.WorldNow;
    }

    /// <summary>
    /// 黑名单客户跟踪表。
    /// </summary>
    [Index(nameof(CustomerId), nameof(Datetime), IsUnique = false)]
    public class CustomerBlacklist : GuidKeyObjectBase
    {
        /// <summary>
        /// 所属客户Id。
        /// </summary>
        [Comment("所属客户Id")]
        public Guid CustomerId { get; set; }

        /// <summary>
        /// 类型，1=加入超额，2=加入超期，3=移除超额，4=移除超期
        /// </summary>
        [Comment("类型，1=加入超额，2=加入超期，3=移除超额，4=移除超期")]
        [Range(1, 4)]
        public byte Kind { get; set; }

        /// <summary>
        /// 来源，1=系统，0=人工。
        /// </summary>
        [Comment("来源，1=系统，0=人工。")]
        public bool IsSystem { get; set; }

        /// <summary>
        /// 操作员Id。空则表示系统操作。
        /// </summary>
        [Comment("操作员Id")]
        public Guid? OpertorId { get; set; }

        /// <summary>
        /// 执行时间
        /// </summary>
        [Comment("执行时间")]
        public DateTime Datetime { get; set; } = OwHelper.WorldNow;

        /// <summary>
        /// 备注。
        /// </summary>
        [Comment("备注")]
        public string Remark { get; set; }
    }

    /// <summary>
    /// 装货地址。
    /// </summary>
    [Index(nameof(CustomerId), IsUnique = false)]
    public class PlLoadingAddr : GuidKeyObjectBase
    {
        /// <summary>
        /// 所属客户Id。
        /// </summary>
        [Comment("所属客户Id")]
        public Guid CustomerId { get; set; }

        /// <summary>
        /// 联系人。
        /// </summary>
        [Comment("所属客户Id")]
        [MaxLength(32)]
        public string Contact { get; set; }

        /// <summary>
        /// 联系电话。
        /// </summary>
        [Comment("联系电话")]
        [MaxLength(32), Phone]
        public string Tel { get; set; }

        /// <summary>
        /// 详细地址。
        /// </summary>
        [Comment("详细地址")]
        [MaxLength(64)]
        public string Addr { get; set; }
    }
}
