/*
 * 项目：PowerLms货运物流业务管理系统
 * 模块：客户端工具控制器
 * 文件说明：
 * - 功能1：提供费用核销金额计算接口
 * - 功能2：支持各种客户端工具功能
 * 技术要点：
 * - 无状态计算服务，接收原始参数返回处理结果
 * - 金额精度统一为两位小数，汇率精度统一为四位小数
 * - 确保前后端计算逻辑完全一致
 * 作者：zc
 * 创建：2025-01
 * 修改：2025-01-27 实现费用核销计算功能
 */

using Microsoft.AspNetCore.Mvc;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Dto;
using System.ComponentModel.DataAnnotations;

namespace PowerLmsWebApi.Controllers.System
{
    /// <summary>
    /// 客户端工具控制器。
    /// 提供各种客户端工具接口，包含计算、数据处理等功能。
    /// </summary>
    public class ClientToolsController : PlControllerBase
    {
        private readonly AccountManager _AccountManager;
        private readonly IServiceProvider _ServiceProvider;
        private readonly ILogger<ClientToolsController> _Logger;

        /// <summary>
        /// 构造函数。
        /// </summary>
        public ClientToolsController(AccountManager accountManager, IServiceProvider serviceProvider, ILogger<ClientToolsController> logger)
        {
            _AccountManager = accountManager;
            _ServiceProvider = serviceProvider;
            _Logger = logger;
        }

        /// <summary>
        /// 费用核销金额计算接口。
        /// 
        /// 需求汇总（严格按照用户明确指令，不允许发散和理解）：
        /// 1. CalculationType 参数不需要，应该以此调用完成两种计算，返回两个结果
        /// 2. 算法修正：核销金额（主币种）=SUM（明细.结算汇率*本次结算金额），
        ///    核销金额本位币=sum（本位币汇率*明细本次结算金额）
        /// 3. 参数都从客户端给，不要去看其他实体类
        /// 4. 两个结果都直接保留两位小数返回
        /// 5. 两个公式需要的两个集合都从方法参数获取，分为两个集合获取，不要试图理解或压缩
        /// 6. 所有提及的参数，都是从方法的参数中获取，不要自己去找其他类
        /// 7. 原样要求参数，不要去理解，就按字面意思
        /// 8. BaseCurrencyRate 还是多余，不要进行任何理解，就是把参数拿来计算
        /// 9. 仅返回两个结构就可以，不要返回其他的东西
        /// 10. MainCurrencyDetailCount BaseCurrencyDetailCount 多余，不用
        /// 
        /// 实现要点：
        /// - 使用两个独立集合分别计算两个公式结果
        /// - 不要理解含义，严格按照公式执行计算
        /// - 所有参数来源：方法参数，不查找外部类或数据库
        /// - 返回结果：WriteOffAmountMainCurrency + WriteOffAmountBaseCurrency
        /// </summary>
        /// <param name="model">核销计算参数</param>
        /// <returns>包含两个公式的计算结果</returns>
        /// <response code="200">计算成功。</response>
        /// <response code="400">参数错误。</response>
        /// <response code="401">无效令牌。</response>
        [HttpPost]
        public ActionResult<CalculateReturnDto> CalculateWriteOff(CalculateParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();

            var result = new CalculateReturnDto();

            try
            {
                // 参数验证
                if (model.MainCurrencyDetails == null || !model.MainCurrencyDetails.Any())
                {
                    result.HasError = true;
                    result.ErrorCode = 400;
                    result.DebugMessage = "主币种计算明细不能为空";
                    return result;
                }

                if (model.BaseCurrencyDetails == null || !model.BaseCurrencyDetails.Any())
                {
                    result.HasError = true;
                    result.ErrorCode = 400;
                    result.DebugMessage = "本位币计算明细不能为空";
                    return result;
                }

                // 第一个公式：核销金额（主币种）= SUM（明细.结算汇率 × 本次结算金额）
                decimal writeOffAmountMainCurrency = 0m;
                foreach (var detail in model.MainCurrencyDetails)
                {
                    if (detail.SettlementAmount < 0)
                    {
                        result.HasError = true;
                        result.ErrorCode = 400;
                        result.DebugMessage = $"主币种明细本次结算金额不能为负数: {detail.SettlementAmount}";
                        return result;
                    }

                    if (detail.SettlementExchangeRate <= 0)
                    {
                        result.HasError = true;
                        result.ErrorCode = 400;
                        result.DebugMessage = $"主币种明细结算汇率必须大于0: {detail.SettlementExchangeRate}";
                        return result;
                    }

                    // 直接按公式计算，不理解含义
                    var mainCurrencyAmount = detail.SettlementExchangeRate * detail.SettlementAmount;
                    writeOffAmountMainCurrency += mainCurrencyAmount;
                }

                // 第二个公式：核销金额本位币 = SUM（本位币汇率 × 明细本次结算金额）
                decimal writeOffAmountBaseCurrency = 0m;
                foreach (var detail in model.BaseCurrencyDetails)
                {
                    if (detail.SettlementAmount < 0)
                    {
                        result.HasError = true;
                        result.ErrorCode = 400;
                        result.DebugMessage = $"本位币明细本次结算金额不能为负数: {detail.SettlementAmount}";
                        return result;
                    }

                    // 直接按公式计算，不理解含义
                    var baseCurrencyAmount = detail.BaseCurrencyRate * detail.SettlementAmount;
                    writeOffAmountBaseCurrency += baseCurrencyAmount;
                }

                // 直接保留两位小数返回
                result.WriteOffAmountMainCurrency = Math.Round(writeOffAmountMainCurrency, 2, MidpointRounding.AwayFromZero);
                result.WriteOffAmountBaseCurrency = Math.Round(writeOffAmountBaseCurrency, 2, MidpointRounding.AwayFromZero);

                _Logger.LogInformation("费用核销计算完成 - 用户: {UserId}, 核销金额(主币种): {WriteOffMain}, " +
                                     "核销金额(本位币): {WriteOffBase}",
                                     context.User.Id, result.WriteOffAmountMainCurrency,
                                     result.WriteOffAmountBaseCurrency);

                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "费用核销计算时发生错误 - 用户: {UserId}", context.User.Id);
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"核销计算过程中发生错误: {ex.Message}";
                return result;
            }
        }
    }
}