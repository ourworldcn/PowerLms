using Microsoft.EntityFrameworkCore;
using OW.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerLms.Data
{
    [Comment("海运进口单")]
    [Index(nameof(JobId), IsUnique = false)]
    public class PlIsDoc : GuidKeyObjectBase, ICreatorInfo, IPlBusinessDoc
    {
        #region IPlBusinessDoc接口相关

        /// <summary>
        /// 所属业务Id。
        /// </summary>
        [Comment("所属业务Id")]
        public Guid? JobId { get; set; }

        /// <summary>
        /// 操作状态。0=初始化单据但尚未操作，128=最后一个状态，此状态下将业务对象状态自动切换为下一个状态。
        /// 0=初始化单据但尚未操作，1=已换单,2=已申报,4=海关已放行,8=已提箱，128=已提货。
        /// </summary>
        [Comment("操作状态。0=初始化单据但尚未操作，1=已换单,2=已申报,4=海关已放行,8=已提箱，128=已提货。")]
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
        /// 船次。
        /// </summary>
        [MaxLength(64)]
        [Comment("船次。")]
        public string ShipSNumber { get; set; }

        /// <summary>
        /// 航线字典id。
        /// </summary>
        [Comment("航线字典id。")]
        public Guid? CargoRouteId { get; set; }

        /// <summary>
        /// 放货方式字典Id。
        /// </summary>
        [Comment("放货方式字典Id。")]
        public Guid? PlaceModeId { get; set; }

        /// <summary>
        /// 驳船船名。
        /// </summary>
        [Comment("驳船船名。")]
        [MaxLength(64)]
        public string BargeName { get; set; }

        /// <summary>
        /// 驳船班次。
        /// </summary>
        [MaxLength(64)]
        [Comment("驳船班次。")]
        public string BargeSNumber { get; set; }

        /// <summary>
        /// 驳船开航日期。
        /// </summary>
        [Comment("驳船开航日期。")]
        public DateTime? BargeStartDateTime { get; set; }

        /// <summary>
        /// 预计换单日期。
        /// </summary>
        [Comment("预计换单日期。")]
        public DateTime? AnticipateBillDateTime { get; set; }

        /// <summary>
        /// 实际换单日期。
        /// </summary>
        [Comment("实际换单日期。")]
        public DateTime? BillDateTime { get; set; }

        /// <summary>
        /// 进口日期。
        /// </summary>
        [Comment("进口日期。")]
        public DateTime? ArrivedDateTime { get; set; }

        /// <summary>
        /// 提货日期。
        /// </summary>
        [Comment("提货日期。")]
        public DateTime? DeliveryDateTime { get; set; }

        /// <summary>
        /// 截关日期。
        /// </summary>
        [Comment("提货日期。")]
        public DateTime? UpToDateTime { get; set; }

        /// <summary>
        /// 提单方式Id。
        /// </summary>
        [Comment("提单方式Id。")]
        public Guid? BillModeId { get; set; }

        /// <summary>
        /// 免箱期。
        /// </summary>
        [Comment("免箱期。")]
        public DateTime? ContainerFreeDateTime { get; set; }

        /// <summary>
        /// 箱号。
        /// </summary>
        [MaxLength(256)]
        [Comment("箱号。")]
        public string ContainerNumber { get; set; }

        /// <summary>
        /// 箱封号。
        /// </summary>
        [MaxLength(256)]
        [Comment("箱封号。")]
        public string SpeelContainerNumber { get; set; }

        /// <summary>
        /// 贸易条款Id。
        /// </summary>
        [Comment("贸易条款Id。")]
        public Guid? MerchantStyleId { get; set; }

        /// <summary>
        /// 目的港港区Id。
        /// </summary>
        [Comment("目的港港区Id。")]
        public Guid? DestPortId { get; set; }

        /// <summary>
        /// 委托类型。FCL=1、LCL=2、BULK=4。
        /// </summary>
        [Comment("委托类型。FCL=1、LCL=2、BULK=4。")]
        [Range(1, 7)]
        public byte DelegationKind { get; set; }

        /// <summary>
        /// 运输条款Id。
        /// </summary>
        [Comment("运输条款Id。")]
        public Guid? TransTermId { get; set; }

        /// <summary>
        /// 付款方式Id。
        /// </summary>
        [Comment("付款方式Id。")]
        public Guid? BillPaymentModeId { get; set; }

        /// <summary>
        /// 随船文件。服务器不解析，逗号分隔。
        /// </summary>
        [Comment("随船文件。服务器不解析，逗号分隔。")]
        public string FileStrings { get; set; }
    }

    /// <summary>
    /// 箱型箱量。
    /// </summary>
    [Index(nameof(ParentId), IsUnique = false)]
    public class ContainerKindCount : GuidKeyObjectBase,IOwSubtables
    {
        /// <summary>
        /// 所属业务单据Id。
        /// </summary>
        [Comment("所属业务单据Id。")]
        public Guid? ParentId { get; set; }

        /// <summary>
        /// 箱型。
        /// </summary>
        [Comment("箱型。")]
        [MaxLength(64)]
        public string Kind { get; set; }

        /// <summary>
        /// 数量。
        /// </summary>
        [Comment("数量。")]
        public int Count { get; set; }
    }
}
