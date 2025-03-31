using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PowerLms.Data;
using PowerLmsServer.EfData;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// 诺诺发票系统回调处理控制器
    /// </summary>
    /// <remarks>
    /// 专门用于处理诺诺发票系统的各种回调请求，包括开票结果、发票作废结果、开票申请结果和红字信息表申请结果等。
    /// </remarks>
    public class NuoNuoCallbackController : PlControllerBase
    {
        private readonly PowerLmsUserDbContext _dbContext;
        private readonly ILogger<NuoNuoCallbackController> _logger;

        /// <summary>
        /// 初始化诺诺回调控制器
        /// </summary>
        /// <param name="dbContext">数据库上下文</param>
        /// <param name="logger">日志记录器</param>
        public NuoNuoCallbackController(PowerLmsUserDbContext dbContext, ILogger<NuoNuoCallbackController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        /// <summary>
        /// 处理诺诺发票系统的回调请求
        /// </summary>
        /// <remarks>
        /// 统一处理诺诺发票系统的各类回调请求，根据operater参数区分不同类型的回调。
        /// 目前支持的回调类型：
        /// - callback: 开票结果回调
        /// - invoiceInvalid: 发票作废结果回调
        /// - invoiceApply: 开票申请结果回调
        /// - invoiceRedCallback: 红字信息表申请结果回调
        /// - redConfirmCallback: 红字确认单申请结果回调
        /// </remarks>
        /// <returns>回调处理结果</returns>
        [HttpPost]
        [AllowAnonymous]
        public ActionResult HandleCallback()
        {
            try
            {
                _logger.LogInformation("收到诺诺发票回调请求");

                // 读取请求参数
                var form = Request.Form;
                string operater = form["operater"];

                // 验证操作类型参数
                if (string.IsNullOrEmpty(operater))
                {
                    _logger.LogWarning("诺诺发票回调缺少操作类型参数");
                    return BadRequest(new { status = "9999", message = "缺少操作类型参数" });
                }

                _logger.LogInformation($"处理诺诺发票回调, 操作类型: {operater}");

                // 根据操作类型处理不同的回调
                switch (operater)
                {
                    case "callback": // 开票结果回调
                        return HandleInvoiceCallback(form);

                    case "invoiceInvalid": // 发票作废结果回调
                        return HandleInvoiceInvalidCallback(form);

                    case "invoiceApply": // 开票申请结果回调
                        return HandleInvoiceApplyCallback(form);

                    case "invoiceRedCallback": // 红字信息表申请结果回调
                        return HandleRedInfoCallback(form);

                    case "redConfirmCallback": // 红字确认单申请结果回调
                        return HandleRedConfirmCallback(form);

                    default:
                        _logger.LogWarning($"未知的操作类型: {operater}");
                        return BadRequest(new { status = "9999", message = "未知的操作类型" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理诺诺发票回调请求时发生异常");
                // 返回成功以避免诺诺系统重试，但在日志中记录错误
                return Ok(new { status = "0000", message = "同步成功" });
            }
        }

        /// <summary>
        /// 处理开票结果回调
        /// </summary>
        /// <param name="form">表单数据</param>
        /// <returns>处理结果</returns>
        private ActionResult HandleInvoiceCallback(IFormCollection form)
        {
            try
            {
                string orderno = form["orderno"];
                string content = form["content"];

                // 验证必填参数
                if (string.IsNullOrEmpty(orderno) || string.IsNullOrEmpty(content))
                {
                    _logger.LogWarning("开票结果回调缺少必要参数");
                    return BadRequest(new { status = "9999", message = "缺少必要参数" });
                }

                _logger.LogInformation($"处理开票结果回调, 订单号: {orderno}");

                // 解析JSON内容
                var invoiceData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(content);
                if (invoiceData == null)
                {
                    _logger.LogWarning("无法解析开票结果数据");
                    return BadRequest(new { status = "9999", message = "无法解析开票结果数据" });
                }

                // 验证发票流水号
                if (!invoiceData.TryGetValue("c_fpqqlsh", out var fpqqlsh) || fpqqlsh.ValueKind == JsonValueKind.Undefined)
                {
                    _logger.LogWarning("开票结果数据中缺少发票流水号");
                    return BadRequest(new { status = "9999", message = "开票结果数据中缺少发票流水号" });
                }

                // 查找对应的发票记录
                var invoiceInfo = _dbContext.TaxInvoiceInfos
                    .FirstOrDefault(i => i.InvoiceSerialNum == fpqqlsh.GetString());

                if (invoiceInfo == null)
                {
                    _logger.LogWarning($"未找到匹配的发票记录, 发票流水号: {fpqqlsh.GetString()}");
                    // 记录回调数据但返回成功，避免诺诺系统重复回调
                    _dbContext.OwSystemLogs.Add(new OwSystemLog
                    {
                        ActionId = "NuoNuo.InvoiceCallback.NotFound",
                        JsonObjectString = content,
                        ExtraString = orderno,
                        WorldDateTime = DateTime.Now
                    });
                    _dbContext.SaveChanges();
                    return Ok(new { status = "0000", message = "同步成功" });
                }

                // 获取发票状态
                string status = null;
                if (invoiceData.TryGetValue("c_status", out var statusElement) && statusElement.ValueKind != JsonValueKind.Undefined)
                {
                    status = statusElement.GetString();
                }

                // 更新发票信息
                UpdateInvoiceInfo(invoiceInfo, invoiceData, status, content);
                _dbContext.SaveChanges();
                _logger.LogInformation($"成功更新发票信息, 发票ID: {invoiceInfo.Id}");

                // 记录回调日志
                _dbContext.OwSystemLogs.Add(new OwSystemLog
                {
                    ActionId = "NuoNuo.InvoiceCallback.Success",
                    JsonObjectString = content,
                    ExtraString = orderno,
                    WorldDateTime = DateTime.Now
                });
                _dbContext.SaveChanges();

                return Ok(new { status = "0000", message = "同步成功" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理开票结果回调时发生异常");
                // 仍然返回成功，避免诺诺系统重复回调
                return Ok(new { status = "0000", message = "同步成功" });
            }
        }

        /// <summary>
        /// 更新发票信息
        /// </summary>
        /// <param name="invoiceInfo">发票信息实体</param>
        /// <param name="invoiceData">回调的发票数据</param>
        /// <param name="status">发票状态</param>
        /// <param name="rawContent">原始回调内容</param>
        private void UpdateInvoiceInfo(TaxInvoiceInfo invoiceInfo, Dictionary<string, JsonElement> invoiceData, string status, string rawContent)
        {
            try
            {
                // 根据状态更新发票
                if (status == null || status == "1") // 开票完成
                {
                    // 已成功开票
                    invoiceInfo.State = 2;

                    // 更新发票基本信息
                    if (invoiceData.TryGetValue("c_fpdm", out var fpdm) && fpdm.ValueKind != JsonValueKind.Undefined)
                        invoiceInfo.InvoiceNumber = fpdm.GetString() + "-" + invoiceInfo.InvoiceNumber;

                    if (invoiceData.TryGetValue("c_fphm", out var fphm) && fphm.ValueKind != JsonValueKind.Undefined)
                        invoiceInfo.InvoiceNumber = invoiceInfo.InvoiceNumber ?? fphm.GetString();

                    // 将完整的回调数据存储在SellerInvoiceData字段中
                    invoiceInfo.SellerInvoiceData = rawContent;

                    // 设置返回发票时间
                    invoiceInfo.ReturnInvoiceTime = DateTime.Now;
                }
                else if (status == "2") // 开票失败
                {
                    invoiceInfo.State = 5; // 设置为开票失败状态

                    // 记录失败原因
                    if (invoiceData.TryGetValue("c_errorMessage", out var errorMsg) &&
                        errorMsg.ValueKind != JsonValueKind.Undefined)
                    {
                        // 将错误信息存储在Remark字段(如果模型中有此字段)或SellerInvoiceData中
                        invoiceInfo.SellerInvoiceData = $"{{\"errorMessage\": \"{errorMsg.GetString()}\"}}";
                    }
                    else
                    {
                        invoiceInfo.SellerInvoiceData = "{\"errorMessage\": \"开票失败: 未知原因\"}";
                    }

                    invoiceInfo.ReturnInvoiceTime = DateTime.Now;
                }
                else if (status == "3") // 开票成功签章失败
                {
                    invoiceInfo.State = 6; // 设置为签章失败状态

                    // 记录失败原因
                    if (invoiceData.TryGetValue("c_errorMessage", out var errorMsg) &&
                        errorMsg.ValueKind != JsonValueKind.Undefined)
                    {
                        invoiceInfo.SellerInvoiceData = $"{{\"errorMessage\": \"签章失败: {errorMsg.GetString()}\"}}";
                    }
                    else
                    {
                        invoiceInfo.SellerInvoiceData = "{\"errorMessage\": \"签章失败: 未知原因\"}";
                    }

                    invoiceInfo.ReturnInvoiceTime = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新发票信息时发生异常, 发票ID: {invoiceInfo.Id}");
                // 即使发生异常，也保存基本信息
                invoiceInfo.ReturnInvoiceTime = DateTime.Now;
                invoiceInfo.SellerInvoiceData = rawContent;
            }
        }

        /// <summary>
        /// 处理发票作废结果回调
        /// </summary>
        /// <param name="form">表单数据</param>
        /// <returns>处理结果</returns>
        private ActionResult HandleInvoiceInvalidCallback(IFormCollection form)
        {
            try
            {
                string fpqqlsh = form["fpqqlsh"];
                string content = form["content"];

                // 验证必填参数
                if (string.IsNullOrEmpty(fpqqlsh) || string.IsNullOrEmpty(content))
                {
                    _logger.LogWarning("发票作废回调缺少必要参数");
                    return BadRequest(new { status = "9999", message = "缺少必要参数" });
                }

                _logger.LogInformation($"处理发票作废回调, 发票流水号: {fpqqlsh}");

                // 解析JSON内容
                var invalidData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(content);
                if (invalidData == null)
                {
                    _logger.LogWarning("无法解析发票作废数据");
                    return BadRequest(new { status = "9999", message = "无法解析发票作废数据" });
                }

                // 查找对应的发票记录
                var invoiceInfo = _dbContext.TaxInvoiceInfos
                    .FirstOrDefault(i => i.InvoiceSerialNum == fpqqlsh);

                if (invoiceInfo != null)
                {
                    // 尝试获取作废状态
                    if (invalidData.TryGetValue("invalidStatus", out var invalidStatus) &&
                        invalidStatus.ValueKind != JsonValueKind.Undefined)
                    {
                        string status = invalidStatus.GetString();
                        if (status == "3") // 作废成功
                        {
                            invoiceInfo.State = 4; // 设置为已作废状态
                            invoiceInfo.SellerInvoiceData = content; // 存储作废信息
                            _dbContext.SaveChanges();
                            _logger.LogInformation($"发票作废成功, 发票ID: {invoiceInfo.Id}");
                        }
                        else if (status == "2") // 作废失败
                        {
                            // 记录失败原因
                            if (invalidData.TryGetValue("invalidErrorMessage", out var errorMsg) &&
                                errorMsg.ValueKind != JsonValueKind.Undefined)
                            {
                                _logger.LogWarning($"发票作废失败: {errorMsg.GetString()}, 发票ID: {invoiceInfo.Id}");
                                invoiceInfo.SellerInvoiceData = $"{{\"invalidErrorMessage\": \"{errorMsg.GetString()}\"}}";
                                _dbContext.SaveChanges();
                            }
                            else
                            {
                                _logger.LogWarning($"发票作废失败: 未知原因, 发票ID: {invoiceInfo.Id}");
                                invoiceInfo.SellerInvoiceData = "{\"invalidErrorMessage\": \"作废失败: 未知原因\"}";
                                _dbContext.SaveChanges();
                            }
                        }
                    }
                }

                // 记录回调日志
                _dbContext.OwSystemLogs.Add(new OwSystemLog
                {
                    ActionId = "NuoNuo.InvoiceInvalidCallback",
                    JsonObjectString = content,
                    ExtraString = fpqqlsh,
                    WorldDateTime = DateTime.Now
                });
                _dbContext.SaveChanges();

                return Ok(new { status = "0000", message = "同步成功" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理发票作废回调时发生异常");
                return Ok(new { status = "0000", message = "同步成功" });
            }
        }

        /// <summary>
        /// 处理开票申请结果回调
        /// </summary>
        /// <param name="form">表单数据</param>
        /// <returns>处理结果</returns>
        private ActionResult HandleInvoiceApplyCallback(IFormCollection form)
        {
            try
            {
                string orderno = form["orderno"];
                string taxNo = form["taxNo"];
                string isSuccess = form["isSuccess"];
                string invoiceId = form["invoiceId"];

                // 验证必填参数
                if (string.IsNullOrEmpty(orderno) || string.IsNullOrEmpty(isSuccess))
                {
                    _logger.LogWarning("开票申请结果回调缺少必要参数");
                    return BadRequest(new { status = "9999", message = "缺少必要参数" });
                }

                _logger.LogInformation($"处理开票申请结果回调, 订单号: {orderno}, 成功: {isSuccess}");

                // 记录回调日志
                _dbContext.OwSystemLogs.Add(new OwSystemLog
                {
                    ActionId = "NuoNuo.InvoiceApplyCallback",
                    JsonObjectString = JsonSerializer.Serialize(new
                    {
                        orderno,
                        taxNo,
                        isSuccess,
                        invoiceId
                    }),
                    ExtraString = orderno,
                    WorldDateTime = DateTime.Now
                });
                _dbContext.SaveChanges();

                return Ok(new { status = "0000", message = "同步成功" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理开票申请结果回调时发生异常");
                return Ok(new { status = "0000", message = "同步成功" });
            }
        }

        /// <summary>
        /// 处理红字信息表申请结果回调
        /// </summary>
        /// <param name="form">表单数据</param>
        /// <returns>处理结果</returns>
        private ActionResult HandleRedInfoCallback(IFormCollection form)
        {
            try
            {
                string billNo = form["billNo"];
                string content = form["content"];

                // 验证必填参数
                if (string.IsNullOrEmpty(billNo) || string.IsNullOrEmpty(content))
                {
                    _logger.LogWarning("红字信息表申请结果回调缺少必要参数");
                    return BadRequest(new { status = "9999", message = "缺少必要参数" });
                }

                _logger.LogInformation($"处理红字信息表申请结果回调, 申请单号: {billNo}");

                // 记录回调日志
                _dbContext.OwSystemLogs.Add(new OwSystemLog
                {
                    ActionId = "NuoNuo.RedInfoCallback",
                    JsonObjectString = content,
                    ExtraString = billNo,
                    WorldDateTime = DateTime.Now
                });
                _dbContext.SaveChanges();

                return Ok(new { status = "0000", message = "同步成功" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理红字信息表申请结果回调时发生异常");
                return Ok(new { status = "0000", message = "同步成功" });
            }
        }

        /// <summary>
        /// 处理红字确认单申请结果回调
        /// </summary>
        /// <param name="form">表单数据</param>
        /// <returns>处理结果</returns>
        private ActionResult HandleRedConfirmCallback(IFormCollection form)
        {
            try
            {
                string billId = form["billId"];
                string content = form["content"];

                // 验证必填参数
                if (string.IsNullOrEmpty(billId) || string.IsNullOrEmpty(content))
                {
                    _logger.LogWarning("红字确认单申请结果回调缺少必要参数");
                    return BadRequest(new { status = "9999", message = "缺少必要参数" });
                }

                _logger.LogInformation($"处理红字确认单申请结果回调, 申请单ID: {billId}");

                // 记录回调日志
                _dbContext.OwSystemLogs.Add(new OwSystemLog
                {
                    ActionId = "NuoNuo.RedConfirmCallback",
                    JsonObjectString = content,
                    ExtraString = billId,
                    WorldDateTime = DateTime.Now
                });
                _dbContext.SaveChanges();

                return Ok(new { status = "0000", message = "同步成功" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理红字确认单申请结果回调时发生异常");
                return Ok(new { status = "0000", message = "同步成功" });
            }
        }
    }
}
