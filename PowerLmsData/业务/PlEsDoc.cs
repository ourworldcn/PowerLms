using Microsoft.EntityFrameworkCore;
using OW.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace PowerLms.Data
{
    /// <summary>
    /// 海运出口单。
    /// </summary>
    [Comment("海运出口单")]
    [Index(nameof(JobId), IsUnique = false)]
    public class PlEsDoc : GuidKeyObjectBase, ICreatorInfo, IPlBusinessDoc
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public PlEsDoc()
        {

        }

        #region IPlBusinessDoc接口相关

        /// <summary>
        /// 所属业务Id。
        /// </summary>
        [Comment("所属业务Id")]
        public Guid? JobId { get; set; }

        /// <summary>
        /// 操作状态。0=初始化单据但尚未操作，128=最后一个状态，此状态下将业务对象状态自动切换为下一个状态。
        /// 0=初始化单据但尚未操作，1=已报价,2=已订舱,4=已配舱,8=已装箱，16=已申报,32=已出提单,128=已放货。
        /// </summary>
        [Comment("操作状态。0=初始化单据但尚未操作，1=已报价,2=已订舱,4=已配舱,8=已装箱，16=已申报,32=已出提单,128=已放货。")]
        public byte Status { get; set; } = 0;

        #endregion IPlBusinessDoc接口相关

        #region ICreatorInfo接口相关
        /// <summary>
        /// 制单人，建立时系统默认，可以更改相当于工作号的所有者。
        /// </summary>
        [Comment("操作员，可以更改相当于工作号的所有者")]
        public Guid? CreateBy { get; set; }

        /// <summary>
        /// 制单时间,系统默认，不能更改
        /// </summary>
        [Comment("新建时间,系统默认，不能更改。")]
        public DateTime CreateDateTime { get; set; }

        #endregion ICreatorInfo接口相关

        /// <summary>
        /// 是否指装。
        /// </summary>
        [Comment("是否指装")]
        public bool IsSpecifyLoad { get; set; } = false;

        /// <summary>
        /// 是否分批。
        /// </summary>
        [Comment("是否分批")]
        public bool IsPartial { get; set; } = false;

        /// <summary>
        /// 是否熏蒸。
        /// </summary>
        [Comment("是否熏蒸")]
        public bool IsFumigation { get; set; } = false;

        /// <summary>
        /// 是否转运。
        /// </summary>
        [Comment("是否转运")]
        public bool IsTranshipment { get; set; } = false;

        /// <summary>
        /// 贸易条款Id。
        /// </summary>
        [Comment("贸易条款Id。")]
        public Guid? MerchantStyleId { get; set; }

        /// <summary>
        /// 装运条款。
        /// </summary>
        [Comment("装运条款。")]
        [MaxLength(128)]
        public string LoadStyle { get; set; }

        /// <summary>
        /// 装船港港区Id。
        /// </summary>
        [Comment("装船港港区Id。")]
        public Guid? SeaPortAreaId { get; set; }

        /// <summary>
        /// 委托类型。FCL=1、LCL=2、BULK=4。
        /// </summary>
        [Comment("委托类型。FCL=1、LCL=2、BULK=4。")]
        [Range(1, 7)]
        public byte DelegationKind { get; set; }

        /// <summary>
        /// 运输条款Id。
        /// </summary>
        [Comment("运输条款Id")]
        public Guid? TransTermId { get; set; }

        #region 订舱单

        /// <summary>
        /// 订舱要求。
        /// </summary>
        [Comment("订舱要求")]
        public string BookingsRemark { get; set; }

        /// <summary>
        /// 订舱日期。
        /// </summary>
        [Comment("订舱日期")]
        public DateTime BookingsDateTime { get; set; }

        /// <summary>
        /// 付款方式Id。
        /// </summary>
        [Comment("付款方式Id")]
        public Guid? BillPaymentModeId { get; set; }

        /// <summary>
        /// 付款地点。
        /// </summary>
        [Comment("付款地点")]
        public string BillPaymentPlace { get; set; }

        /// <summary>
        /// S/O编号。
        /// </summary>
        [Comment("S/O编号")]
        public string SoNumber { get; set; }

        /// <summary>
        /// 进仓编号。
        /// </summary>
        [Comment("进仓编号")]
        public string WarehousingNumber { get; set; }

        /// <summary>
        /// 放货方式。
        /// </summary>
        [Comment("放货方式")]
        public Guid? GoodsReleaseModeId { get; set; }

        /// <summary>
        /// 放舱日期。
        /// </summary>
        [Comment("放舱日期")]
        public DateTime WarehousingDateTime { get; set; }

        /// <summary>
        /// 航次。
        /// </summary>
        [Comment("航次")]
        public string Voyage { get; set; }

        /// <summary>
        /// 航线字典ID。
        /// </summary>
        [Comment("航线字典ID")]
        public Guid? CargoRouteId { get; set; }

        /// <summary>
        /// 中转港。
        /// </summary>
        [Comment("中转港")]
        public string TransitPort { get; set; }

        /// <summary>
        /// 截货日期
        /// </summary>
        [Comment("截货日期")]
        public DateTime? CutOffGoodsDateTime { get; set; }

        /// <summary>
        /// 截关日期
        /// </summary>
        [Comment("截关日期")]
        public DateTime? CutOffDateTime { get; set; }

        /// <summary>
        /// 海运说明
        /// </summary>
        [Comment("海运说明")]
        public string SeaborneRemark { get; set; }

        /// <summary>
        /// 驳船船名.
        /// </summary>
        [Comment("驳船船名")]
        [MaxLength(64)]
        public string BargeName { get; set; }

        /// <summary>
        /// 驳船船名.
        /// </summary>
        [Comment("驳船船名")]
        [MaxLength(64)]
        public string BargeVoyage { get; set; }

        /// <summary>
        /// 驳船开航日期.
        /// </summary>
        [Comment("驳船开航日期")]
        public DateTime? BargeSailDateTime { get; set; }

        /// <summary>
        /// 驳船到港日期.
        /// </summary>
        [Comment("驳船到港日期")]
        public DateTime? BargeArrivalDateTime { get; set; }

        /// <summary>
        /// 驳船装船港.
        /// </summary>
        [Comment("驳船装船港")]
        public string BargeLoadingHarbor { get; set; }

        /// <summary>
        /// 驳船目的港.
        /// </summary>
        [Comment("驳船目的港")]
        public string BargeDestinationHarbor { get; set; }

        /// <summary>
        /// 发货人.
        /// </summary>
        [Comment("发货人")]
        public string Consigner { get; set; }

        /// <summary>
        /// 发货人抬头.
        /// </summary>
        [Comment("发货人抬头")]
        public string ConsignerTitle { get; set; }

        /// <summary>
        /// 通知人.
        /// </summary>
        [Comment("通知人")]
        public string Informers { get; set; }

        /// <summary>
        /// 通知人抬头.
        /// </summary>
        [Comment("通知人抬头")]
        public string InformersTitle { get; set; }

        /// <summary>
        /// 唛头.
        /// </summary>
        [Comment("唛头")]
        public string MarkHeader { get; set; }

        /// <summary>
        /// 箱量。
        /// </summary>
        [Comment("箱量。")]
        public string ContainerKindCountString { get; set; }

        /// <summary>
        /// 品名。
        /// </summary>
        [Comment("品名。")]
        public string GoodsName { get; set; }

        /// <summary>
        /// 总计1,箱。
        /// </summary>
        [Comment("总计1,箱。")]
        public string Total1 { get; set; }

        /// <summary>
        /// 总计1,箱。
        /// </summary>
        [Comment("总计2,货。")]
        public string Total2 { get; set; }

        /// <summary>
        /// 总计1,箱。
        /// </summary>
        [Comment("总计3,合计。")]
        public string Total3 { get; set; }

        /// <summary>
        /// 货种Id。
        /// </summary>
        [Comment("货种Id。")]
        public Guid? BookingGoodsTypeId { get; set; }

        /// <summary>
        /// 危险级别。
        /// </summary>
        [Comment("危险级别。")]
        public string DangerousLevel { get; set; }

        /// <summary>
        /// 危规页码。
        /// </summary>
        [Comment("危规页码。")]
        public string DangerousPage { get; set; }

        /// <summary>
        /// 特征。
        /// </summary>
        [Comment("特征。")]
        public string Features { get; set; }

        /// <summary>
        /// UN No。
        /// </summary>
        [Comment("UN No。")]
        public string UnNumber { get; set; }

        /// <summary>
        /// 闪点。
        /// </summary>
        [Comment("闪点。")]
        public string FlashPoint { get; set; }

        /// <summary>
        /// 冷藏温度。
        /// </summary>
        [Comment("冷藏温度。")]
        public string RefrigerationTemperature { get; set; }


        #endregion 订舱单
    }
}
