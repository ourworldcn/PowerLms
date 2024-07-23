using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using OW.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerLms.Data
{
    /// <summary>
    /// 总业务数据类。
    /// </summary>
    [Index(nameof(OrgId), nameof(JobNo), IsUnique = true)]
    public class PlJob : GuidKeyObjectBase, ICreatorInfo
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public PlJob()
        {
            
        }

        /// <summary>
        /// 所属机构Id。
        /// </summary>
        [Comment("所属机构Id")]
        public Guid? OrgId { get; set; }

        /// <summary>
        /// 工作号.
        /// </summary>
        [Comment("工作号")]
        public string JobNo { get; set; }

        /// <summary>
        /// 业务种类id
        /// </summary>
        [Comment("业务种类id")]
        public Guid? JobTypeId { get; set; }

        /// <summary>
        /// 客户Id。
        /// </summary>
        [Comment("客户Id")]
        public Guid? CustomId { get; set; }

        /// <summary>
        /// 客户联系人。客户联系人可以从客户资料中维护的联系人中选择，也可以临时输入，所以这里不是关联联系人的id，是字符串
        /// </summary>
        [MaxLength(32)]
        [Comment("客户联系人")]
        public string LinkMan { get; set; }

        /// <summary>
        /// 联系人电话。客户联系人可以从客户资料中维护的联系人中选择，也可以临时输入，所以这里不是关联联系人的id，是字符串
        /// </summary>
        [MaxLength(24), DataType("varchar(24)"), Phone]
        [Comment("联系人电话")]
        public string LinkTel { get; set; }

        /// <summary>
        /// 联系人传真。客户联系人可以从客户资料中维护的联系人中选择，也可以临时输入，所以这里不是关联联系人的id，是字符串
        /// </summary>
        [MaxLength(24), DataType("varchar(24)"), Phone]
        [Comment("联系人传真")]
        public string LinkFax { get; set; }

        /// <summary>
        /// 发货人。
        /// </summary>
        [MaxLength(128)]
        [Comment("发货人")]
        public string Consignor { get; set; }

        /// <summary>
        /// 收货人。
        /// </summary>
        [MaxLength(128)]
        [Comment("收货人")]
        public string Consignee { get; set; }

        /// <summary>
        /// 通知人。
        /// </summary>
        [MaxLength(128)]
        [Comment("通知人")]
        public string Notify { get; set; }

        /// <summary>
        /// 代理人。
        /// </summary>
        [MaxLength(128)]
        [Comment("代理人")]
        public string Agent { get; set; }

        /// <summary>
        /// 主单号.
        /// </summary>
        [Comment("主单号")]
        [MaxLength(128)]
        public string MblNo { get; set; }

        /// <summary>
        /// 分单号字符串，/分隔多个分单号.
        /// </summary>
        [Comment("分单号字符串，/分隔多个分单号")]
        public string HblNoString { get; set; }

        /// <summary>
        /// 分单号分隔符。
        /// </summary>
        const string HblSeparator = "/";

        /// <summary>
        /// 揽货类型,简单字典HoldType
        /// </summary>
        [Comment("揽货类型,简单字典HoldType")]
        public Guid? HoldtypeId { get; set; }

        /// <summary>
        /// 操作员，建立时系统默认，可以更改相当于工作号的所有者。
        /// </summary>
        [Comment("操作员，可以更改相当于工作号的所有者")]
        public Guid? CreateBy { get; set; }

        /// <summary>
        /// 新建时间,系统默认，不能更改
        /// </summary>
        [Comment("新建时间,系统默认，不能更改。")]
        public DateTime CreateDateTime { get; set; }

        /// <summary>
        /// 操作人Id。
        /// </summary>
        [Comment("操作人Id。")]
        public Guid? OperatorId { get; set; }

        /// <summary>
        /// 操作时间
        /// </summary>
        [Comment("操作时间。")]
        public DateTime? OperatingDateTime { get; set; }

        /// <summary>
        /// 工作状态。Operating正操作=2，Operated操作完成=4，Checked已审核=8，Closed已关闭=16.
        /// </summary>
        [Comment("工作状态。Operating正操作=2，Operated操作完成=4，Checked已审核=8，Closed已关闭=16.")]
        public byte JobState { get; set; }

        /// <summary>
        /// 操作状态。NewJob初始=0,Arrived 已到货=2,Declared 已申报=4,Delivered 已配送=8,Submitted 已交单=16,Notified 已通知=32
        /// 对空运进口单使用空运进口单的相关定义。
        /// </summary>
        [Comment("操作状态。NewJob初始=0,Arrived 已到货=2,Declared 已申报=4,Delivered 已配送=8,Submitted 已交单=16,Notified 已通知=32")]
        public byte OperateState { get; set; }

        /// <summary>
        ///  财务日期。出口默认出港日期，进口默认出库日期。
        /// </summary>
        [Comment("出口默认出港日期，进口默认出库日期。")]
        public DateTime AccountDate { get; set; }

        /// <summary>
        /// 开航日期。
        /// </summary>
        [Comment("开航日期。")]
        public DateTime? Etd { get; set; }

        /// <summary>
        /// 到港日期。
        /// </summary>
        [Comment("到港日期")]
        public DateTime? ETA { get; set; }

        /// <summary>
        /// 提送货日期
        /// </summary>
        [Comment("提送货日期")]
        public DateTime? DeliveryDate { get; set; }
        /*salesman	业务员	用户id
serviceman	客服	用户id
Businessmanager	业务负责人	用户id
AirlineMan	航线负责人	用户id
*/
        /// <summary>
        /// 业务员Id。
        /// </summary>
        [Comment("业务员Id")]
        public Guid? SalesId { get; set; }

        /// <summary>
        /// 客服Id。
        /// </summary>
        [Comment("客服Id")]
        public Guid? CustomerServiceId { get; set; }

        /// <summary>
        /// 业务负责人Id
        /// </summary>
        [Comment("业务负责人Id")]
        public Guid? BusinessManagerId { get; set; }

        /// <summary>
        /// 航线负责人Id
        /// </summary>
        [Comment("航线负责人Id")]
        public Guid? ShippingLineManagerId { get; set; }

        /// <summary>
        /// 合同号。
        /// </summary>
        [Comment("合同号")]
        [MaxLength(32)]
        public string ContractNo { get; set; }

        /*verifyDate	审核日期	
CloseDate	关闭日期	
*/
        /// <summary>
        /// 审核日期。
        /// </summary>
        [Comment("审核日期")]
        public DateTime? VerifyDate { get; set; }

        /// <summary>
        /// 关闭日期。
        /// </summary>
        [Comment("关闭日期")]
        public DateTime? CloseDate { get; set; }

        /*SpecialAgent	订舱代理	string100	选择客户资料是订舱代理的客户
Opcompany	操作公司	string100	选择所有客户资料的客户
*/

        /// <summary>
        /// 订舱代理。选择客户资料是订舱代理的客户
        /// </summary>
        [Comment("订舱代理。选择客户资料是订舱代理的客户")]
        [MaxLength(128)]
        public string SpecialAgent { get; set; }

        /// <summary>
        /// 操作公司。选择客户资料是订舱代理的客户
        /// </summary>
        [Comment("操作公司。选择客户资料是订舱代理的客户")]
        [MaxLength(128)]
        public string OpCompany { get; set; }

        /*LoadingCode	起始港	港口id	显示三字码即可
DestinationCode	目的港	港口id	显示三字码即可
Carrie	承运人	船公司或航空公司或	二字码
*/

        /// <summary>
        /// 起始港 港口代码	显示三字码即可。
        /// </summary>
        [Comment("起始港，港口代码，显示三字码即可。")]
        [MaxLength(4), Unicode(false)]
        public string LoadingCode { get; set; }

        /// <summary>
        /// 目的港 港口代码	显示三字码即可
        /// </summary>
        [Comment("目的港，港口代码，显示三字码即可")]
        [MaxLength(4), Unicode(false)]
        public string DestinationCode { get; set; }

        /// <summary>
        /// 承运人，船公司或航空公司或，二字码
        /// </summary>
        [Comment("承运人，船公司或航空公司或，二字码")]
        [MaxLength(4), Unicode(false)]
        public string CarrieCode { get; set; }

        /*Carriernumber	运输工具号		空运显示为航班号，海运显示为船名、陆运显示为卡车号
GoodsName	货物名称	string200	
CARGOTYPE	货物类型	简单字典CARGOTYPE	
PackType	包装方式	简单字典PackType	
*/
        /// <summary>
        /// 运输工具号，空运显示为航班号，海运显示为船名、陆运显示为卡车号.
        /// </summary>
        [Comment("运输工具号，空运显示为航班号，海运显示为船名、陆运显示为卡车号")]
        [MaxLength(64)]
        public string CarrierNo { get; set; }

        /// <summary>
        /// 货物名称.
        /// </summary>
        [Comment("货物名称")]
        [MaxLength(256)]
        public string GoodsName { get; set; }

        /// <summary>
        /// 货物类型.简单字典CARGOTYPE	
        /// </summary>
        [Comment("货物类型.简单字典CARGOTYPE")]
        public Guid? CargoType { get; set; }

        /// <summary>
        /// 包装方式,简单字典PackType
        /// </summary>
        [Comment("包装方式,简单字典PackType")]
        public Guid? PackType { get; set; }

        /*PkgsNum	包装件数	整数	委托件数
weight	毛重	三位小数	委托重量KG数，海运显示为毛重
Netweigh	计费重量	三位小数	委托计费重量，海运显示为净重
MeasureMent	体积	三位小数	委托体积立方
goodssize	尺寸	string100	字符串表达
*/

        /// <summary>
        /// 包装件数
        /// </summary>
        [Comment("包装件数")]
        public int? PkgsCount { get; set; }

        /// <summary>
        /// 毛重,单位Kg,三位小数。委托重量KG数，海运显示为毛重
        /// </summary>
        [Comment("毛重,单位Kg,三位小数。委托重量KG数，海运显示为毛重")]
        [Precision(18, 3)]
        public decimal? Weight { get; set; }

        /// <summary>
        /// 体积,三位小数,委托体积立方
        /// </summary>
        [Comment("体积,三位小数,委托体积立方")]
        [Precision(18, 3)]
        public decimal? MeasureMent { get; set; }

        /// <summary>
        /// 尺寸,字符串表达.
        /// </summary>
        [Comment("尺寸,字符串表达.")]
        [MaxLength(128)]
        public string GoodsSize { get; set; }
    }
}
