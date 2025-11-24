/*
 * 项目：PowerLms物流管理系统
 * 模块：客户资料管理
 * 文件说明：
 * - 功能1：客户资料主表及相关子表定义
 * - 功能2：客户联系人、业务负责人、提单、黑名单、装货地址管理
 * 技术要点：
 * - 展开复杂类型为平铺字段以支持导入导出功能
 * - 保持数据库字段名与现有结构一致
 * - 多租户数据隔离和外键关联
 * 作者：zc
 * 创建：2023-12
 * 修改：2025-01-15 展开PlOwnedContact复杂类型为平铺字段
 * 修改：2025-02-06 添加RowVersion字段支持开放式并发控制
 */

using Microsoft.EntityFrameworkCore;
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
    /// 
    /// 财务系统对接说明：
    /// PowerLms与金蝶是两套独立的客户管理系统，存在数据对接问题：
    /// 1. 两边的客户ID/主键完全不同，无法直接关联
    /// 2. 唯一一致的是客户名称（建档时保持一致）  
    /// 3. 但金蝶在处理DBF凭证时只认财务编码，不认客户名称
    /// 4. 同一往来单位可能既是客户又是供应商，需要不同的财务编码
    /// 
    /// 解决方案：通过FinanceCodeAR/AP字段建立PowerLms与金蝶的精确编码映射，
    /// 确保生成的DBF凭证能被金蝶系统正确识别和处理。
    /// </summary>
    [Comment("客户资料")]
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

        #region 客户名称信息 - 展开PlOwnedName复杂类型

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

        #endregion 客户名称信息

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

        #region 联系方式信息 - 展开PlOwnedContact复杂类型

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

        #endregion 联系方式信息

        #region 地址信息 - 展开PlOwnedAddress复杂类型

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

        #endregion 地址信息

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

        #region 账单信息 - 展开PlBillingInfo复杂类型

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

        #region Airlines 相关属性 - 展开OwnedAirlines复杂类型

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
        /// 财务编码。原有字段，B账（外账）输出金蝶时的对接。
        /// </summary>
        [MaxLength(32)]
        [Comment("财务编码。B账（外账）输出金蝶时的对接。")]
        public string TacCountNo { get; set; }

        /// <summary>
        /// 财务编码(AR)。该客户在金蝶系统中的客户编码，用于应收类业务凭证生成。
        /// 
        /// 背景：PowerLms与金蝶是两套独立的客户资料系统，唯一能对应的是客户名称，但金蝶在导入凭证时只认编码不认名称。
        /// 解决方案：在PowerLms客户资料中预存该客户在金蝶系统中的客户编码，建立精确的数据映射关系。
        /// 应用场景：生成实收(RF)、应收账款等凭证时，通过此编码填写FTRANSID字段，确保金蝶能准确识别客户。
        /// </summary>
        [MaxLength(32)]
        [Comment("财务编码(AR)。该客户在金蝶系统中的客户编码，用于应收类业务凭证生成。")]
        public string FinanceCodeAR { get; set; }

        /// <summary>
        /// 财务编码(AP)。该客户在金蝶系统中的供应商编码，用于应付类业务凭证生成。
        /// 
        /// 背景：同一往来单位可能既是客户又是供应商，在金蝶中有不同的编码体系。
        /// 解决方案：分别存储该单位作为客户(AR)和供应商(AP)时在金蝶中的编码。
        /// 应用场景：生成实付(PF)、应付账款等凭证时，通过此编码填写FTRANSID字段，确保金蝶能准确识别供应商。
        /// </summary>
        [MaxLength(32)]
        [Comment("财务编码(AP)。该客户在金蝶系统中的供应商编码，用于应付类业务凭证生成。")]
        public string FinanceCodeAP { get; set; }

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

        /// <summary> 国内外字段，有时也叫国别。某些公司特有字段，用于输出凭证。 </summary>
        /// <value> true国内，false国外。 </value>
        [Comment("国内外字段")]
        public bool? IsDomestic { get; set; }

        /// <summary>
        /// 并发控制行版本号。用于检测并发修改冲突。
        /// SQL Server自动维护此字段，每次更新时自动递增。
        /// </summary>
        [Comment("并发控制行版本号")]
        [Timestamp]
        public byte[] RowVersion { get; set; }
    }

    /// <summary>
    /// 客户资料的联系人。
    /// </summary>
    [Comment("客户资料的联系人")]
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

        #region 联系方式信息 - 展开PlOwnedContact复杂类型

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

        #endregion 联系方式信息

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

        /// <summary>
        /// 并发控制行版本号。用于检测并发修改冲突。
        /// SQL Server自动维护此字段，每次更新时自动递增。
        /// </summary>
        [Comment("并发控制行版本号")]
        [Timestamp]
        public byte[] RowVersion { get; set; }
    }

    #region 客户子表实体

    /// <summary>
    /// 业务负责人表。
    /// </summary>
    [Comment("业务负责人表")]
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
    [Comment("客户提单内容表")]
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
    [Comment("黑名单客户跟踪表")]
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
    [Comment("装货地址")]
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
        [Comment("联系人")]
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

    #endregion 客户子表实体
}
