using Microsoft.EntityFrameworkCore;
using PowerLms.Data;
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
    /// 费用种类。Code 是费用代码，DisplayName是费用名称，ShortName是英文名称,Remark是附加说明。
    /// </summary>
    [Comment("费用种类")]
    public class FeesType : NamedSpecialDataDicBase, IMarkDelete, ICloneable
    {
        /// <summary>
        /// 原币种Id字段，保留用于兼容旧版本。
        /// </summary>
        [Comment("币种Id"), Obsolete("请使用CurrencyCode属性")]
        public Guid? CurrencyTypeId { get; set; }
        /// <summary>
        /// 币种代码，关联PlCurrency.Code。
        /// </summary>
        [Comment("币种代码")]
        [Unicode(false), MaxLength(32)]
        public string CurrencyCode { get; set; }
        /// <summary>
        /// 默认单价。
        /// </summary>
        [Comment("默认单价"), Precision(18, 4)]
        public decimal Price { get; set; }
        /// <summary>
        /// 原费用组Id字段，保留用于兼容旧版本。
        /// </summary>
        [Comment("费用组Id"), Obsolete("请使用FeeGroupCode属性")]
        public Guid? FeeGroupId { get; set; }
        /// <summary>
        /// 费用组代码，关联SimpleDataDic.Code。
        /// </summary>
        [Comment("费用组代码")]
        [Unicode(false), MaxLength(32)]
        public string FeeGroupCode { get; set; }
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
    /// <summary>
    /// 费用种类扩展方法。
    /// </summary>
    public static class FeesTypeExtensions
    {
        /// <summary>
        /// 获取费用种类对应的币种
        /// </summary>
        /// <param name="feesType">费用种类</param>
        /// <param name="dbContext">数据库上下文</param>
        /// <returns>币种对象</returns>
        public static PlCurrency GetCurrency(this FeesType feesType, DbContext dbContext)
        {
            if (string.IsNullOrEmpty(feesType.CurrencyCode))
                return null;
            return dbContext.Set<PlCurrency>()
                .FirstOrDefault(c => c.Code == feesType.CurrencyCode);
        }
        /// <summary>
        /// 获取费用种类对应的费用组
        /// </summary>
        /// <param name="feesType">费用种类</param>
        /// <param name="dbContext">数据库上下文</param>
        /// <returns>费用组对象</returns>
        public static SimpleDataDic GetFeeGroup(this FeesType feesType, DbContext dbContext)
        {
            if (string.IsNullOrEmpty(feesType.FeeGroupCode))
                return null;
            // 获取费用组字典目录
            var feeGroupCatalog = dbContext.Set<DataDicCatalog>()
                .FirstOrDefault(c => c.Code == "FeeGroup");
            if (feeGroupCatalog == null)
                return null;
            // 查找对应费用组编码的字典项
            return dbContext.Set<SimpleDataDic>()
                .FirstOrDefault(c => c.DataDicId == feeGroupCatalog.Id && c.Code == feesType.FeeGroupCode);
        }
    }
}
