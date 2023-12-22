using Microsoft.EntityFrameworkCore;
using PowerLms.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerLms.Data
{
    /// <summary>
    /// 费用种类。Code 是费用代码，DisplayName是费用名称，ShortName是英文名称,Remark是附加说明。
    /// </summary>
    [Comment("费用种类")]
    public class FeesType : NamedSpecialDataDicBase, IMarkDelete
    {
        /// <summary>
        /// 币种Id。
        /// </summary>
        [Comment("币种Id")]
        public Guid? CurrencyTypeId { get; set; }

        /// <summary>
        /// 默认单价。
        /// </summary>
        [Comment("默认单价"), Precision(18,4)]
        public decimal Price { get; set; }

        /// <summary>
        /// 费用组Id。
        /// </summary>
        [Comment("费用组Id")]
        public Guid? FeeGroupId { get; set; }

        /// <summary>
        /// 是否应付。true是应付。
        /// </summary>
        [Comment("是否应付。true是应付。")]
        public bool IsPay { get; set; }

        /// <summary>
        /// 是否应收。true是应收。
        /// </summary>
        [Comment("是否应收。true是应收。")]
        public bool IsGather { get; set; }

        /// <summary>
        /// 是否佣金,True是佣金。
        /// </summary>
        [Comment("是否佣金,True是佣金。")]
        public bool IsCommission { get; set; }

        /// <summary>
        /// 是否代垫费用,true垫付。
        /// </summary>
        [Comment("是否代垫费用,true垫付。")]
        public bool IsDaiDian { get; set; }
    }
}
