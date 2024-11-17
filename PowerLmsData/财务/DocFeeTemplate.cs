using Microsoft.EntityFrameworkCore;
using OW.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerLms.Data
{
    /// <summary>
    /// 费用方案数据。
    /// </summary>
    [Table("DocFeeTemplates")]
    public class DocFeeTemplate : GuidKeyObjectBase, ICreatorInfo
    {
        /// <summary>
        /// 方案编号。
        /// </summary>
        [MaxLength(64)]
        public string Code { get; set; }

        /// <summary>
        /// 业务种类。
        /// </summary>
        [MaxLength(64)]
        public string Kind { get; set; }

        /// <summary>
        /// 业务说明。
        /// </summary>
        public string Remark { get; set; }

        #region ICreatorInfo

        /// <summary>
        /// 创建者Id。
        /// </summary>
        [Comment("创建者的唯一标识。")]
        public Guid? CreateBy { get; set; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        [Comment("创建的时间。")]
        public DateTime CreateDateTime { get; set; }
        #endregion ICreatorInfo

    }

    /// <summary>
    /// 费用方案数据详细项。
    /// </summary>
    [Table("DocFeeTemplateItems")]
    public class DocFeeTemplateItem : GuidKeyObjectBase
    {
        /// <summary>
        /// 费用方案 Id。
        /// </summary>
        [Comment("申请单Id")]
        public Guid? ParentId { get; set; }

        /// <summary>
        /// 收入或支出，false支出，true收入。
        /// </summary>
        [Comment("收付，false支出，true收入。")]
        public bool IO { get; set; }

        /// <summary>
        /// 结算单位类型。1=货主（业务中的客户）、2=收货人、4=发货人、8=承运人、16=代理人、32=固定客户
        /// </summary>
        [Comment("1=货主（业务中的客户）、2=收货人、4=发货人、8=承运人、16=代理人、32=固定客户")]
        public byte CoKind { get; set; }

        /// <summary>
        /// 固定结算单位,当前一项类型选固定客户时必填。
        /// </summary>
        [Comment("固定结算单位,当前一项类型选固定客户时必填。")]
        public Guid? CoId { get; set; }

        /// <summary>
        /// 结算方式,简单字典FeePayType。
        /// </summary>
        [Comment("结算方式,简单字典FeePayType。")]
        public Guid? FeePayTypeId { get; set; }

        /// <summary>
        /// 单位,简单字典ContainerType。
        /// </summary>
        [Comment("单位,简单字典ContainerType。")]
        public Guid? ContainerTypeId { get; set; }

        /// <summary>
        /// 费用种类,费用种类字典Id。
        /// </summary>
        [Comment(" 费用种类,费用种类字典Id。")]
        public Guid? FeesTypeId { get; set; }

        /// <summary>
        /// 币种。标准货币缩写。
        /// </summary>
        [MaxLength(4), Unicode(false)]
        [Comment("币种。标准货币缩写。")]
        public string Currency { get; set; }

        /// <summary>
        /// 单价。
        /// </summary>
        [Comment("单价"), Precision(18, 4)]
        public decimal Price { get; set; }

        /// <summary>
        /// 基价,默认为0.
        /// </summary>
        [Comment("基价,默认为0."), Precision(18, 4)]
        public decimal BasePrice { get; set; }

        /// <summary>
        /// 最低收费,默认为0.
        /// </summary>
        [Comment("最低收费,默认为0."), Precision(18, 4)]
        public decimal MinFee { get; set; }

        #region 空运独有

        /// <summary>
        /// 免费天数。
        /// </summary>
        [Comment("免费天数。")]
        public short FreeDayCount { get; set; }

        /// <summary>
        /// 10天以下单价。
        /// </summary>
        [Comment("10天以下单价"), Precision(18, 4)]
        public decimal PriceOfLessTen { get; set; }

        /// <summary>
        /// 10天(含)以上单价。
        /// </summary>
        [Comment("10天(含)以上单价。"), Precision(18, 4)]
        public decimal PriceOfGreateOrEqTen { get; set; }

        #endregion 空运独有

    }
}
