/*
 * 项目：PowerLms货运物流业务管理系统
 * 模块：客户端工具控制器 - DTO定义
 * 文件说明：
 * - 功能1：定义费用核销金额计算接口DTO
 * - 功能2：支持多种客户端工具的参数封装
 * 技术要点：
 * - 严格的参数验证注解
 * - 符合系统精度规则（金额两位小数，汇率四位小数）
 * 作者：zc
 * 创建：2025-01
 * 修改：2025-01-27 定义费用核销计算接口
 */
using PowerLmsWebApi.Dto;
using System.ComponentModel.DataAnnotations;
namespace PowerLmsWebApi.Controllers.System
{
    #region 费用核销计算接口DTO
    /// <summary>
    /// 费用核销计算参数封装类。
    /// </summary>
    public class CalculateParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 主币种计算明细列表。必填。
        /// </summary>
        [Required(ErrorMessage = "主币种计算明细不能为空")]
        public IEnumerable<MainCurrencyDetailDto> MainCurrencyDetails { get; set; } = null!;
        /// <summary>
        /// 本位币计算明细列表。必填。
        /// </summary>
        [Required(ErrorMessage = "本位币计算明细不能为空")]
        public IEnumerable<BaseCurrencyDetailDto> BaseCurrencyDetails { get; set; } = null!;
    }
    /// <summary>
    /// 费用核销计算返回值封装类。
    /// </summary>
    public class CalculateReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 核销金额（主币种）。保留两位小数。
        /// </summary>
        public decimal WriteOffAmountMainCurrency { get; set; }
        /// <summary>
        /// 核销金额本位币。保留两位小数。
        /// </summary>
        public decimal WriteOffAmountBaseCurrency { get; set; }
    }
    #endregion
    #region 主币种计算明细DTO
    /// <summary>
    /// 主币种计算明细DTO。
    /// </summary>
    public class MainCurrencyDetailDto
    {
        /// <summary>
        /// 结算汇率。精度为四位小数。
        /// </summary>
        [Required(ErrorMessage = "结算汇率不能为空")]
        [Range(0.0001, double.MaxValue, ErrorMessage = "结算汇率必须大于0")]
        public decimal SettlementExchangeRate { get; set; }
        /// <summary>
        /// 本次结算金额。精度为两位小数。
        /// </summary>
        [Required(ErrorMessage = "本次结算金额不能为空")]
        [Range(0, double.MaxValue, ErrorMessage = "本次结算金额不能为负数")]
        public decimal SettlementAmount { get; set; }
    }
    #endregion
    #region 本位币计算明细DTO
    /// <summary>
    /// 本位币计算明细DTO。
    /// </summary>
    public class BaseCurrencyDetailDto
    {
        /// <summary>
        /// 本位币汇率。精度为四位小数。
        /// </summary>
        [Required(ErrorMessage = "本位币汇率不能为空")]
        [Range(0.0001, double.MaxValue, ErrorMessage = "本位币汇率必须大于0")]
        public decimal BaseCurrencyRate { get; set; }
        /// <summary>
        /// 本次结算金额。精度为两位小数。
        /// </summary>
        [Required(ErrorMessage = "本次结算金额不能为空")]
        [Range(0, double.MaxValue, ErrorMessage = "本次结算金额不能为负数")]
        public decimal SettlementAmount { get; set; }
    }
    #endregion
}