using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PowerLms.Data;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using System;
using System.Collections.Generic;
using System.Text.Json;

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
        private readonly OwMessageManager _messageManager; // 注入消息管理器

        /// <summary>
        /// 初始化诺诺回调控制器
        /// </summary>
        /// <param name="dbContext">数据库上下文</param>
        /// <param name="logger">日志记录器</param>
        /// <param name="messageManager">消息管理器</param>
        public NuoNuoCallbackController(
            PowerLmsUserDbContext dbContext,
            ILogger<NuoNuoCallbackController> logger,
            OwMessageManager messageManager) // 添加消息管理器参数
        {
            _dbContext = dbContext;
            _logger = logger;
            _messageManager = messageManager; // 初始化消息管理器
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

                _logger.LogInformation("处理诺诺发票回调, 操作类型: {Operater}", operater);

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

                _logger.LogInformation("处理开票结果回调, 订单号: {OrderNo}", orderno);

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
                    _logger.LogWarning("未找到匹配的发票记录, 发票流水号: {InvoiceSerialNum}", fpqqlsh.GetString());
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
                _logger.LogInformation("成功更新发票信息, 发票ID: {InvoiceId}", invoiceInfo.Id);

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
                _logger.LogInformation("开始更新发票信息, 发票ID: {InvoiceId}, 状态: {Status}", invoiceInfo.Id, status);

                // 根据状态更新发票
                if (status == null || status == "1") // 开票完成
                {
                    // 已成功开票 - 状态2表示已开票
                    invoiceInfo.State = 2; // 保持状态值为2，表示已开票

                    // 更新发票号
                    if (invoiceData.TryGetValue("c_fphm", out var fphm) && fphm.ValueKind != JsonValueKind.Undefined)
                    {
                        string fphmValue = fphm.GetString();
                        invoiceInfo.InvoiceNumber = fphmValue;
                    }

                    // 更新发票代码 - 注释掉，因为TaxInvoiceInfo没有InvoiceCode属性
                    // if (invoiceData.TryGetValue("c_fpdm", out var fpdm) && fpdm.ValueKind != JsonValueKind.Undefined)
                    // {
                    //     string fpdmValue = fpdm.GetString();
                    //     invoiceInfo.InvoiceCode = fpdmValue;
                    //     _logger.LogInformation($"发票ID: {invoiceInfo.Id} 更新发票代码: {fpdmValue}");
                    // }

                    // 将发票代码等信息记录在日志中或SellerInvoiceData中
                    if (invoiceData.TryGetValue("c_fpdm", out var fpdm) && fpdm.ValueKind != JsonValueKind.Undefined)
                    {
                        string fpdmValue = fpdm.GetString();
                        _logger.LogInformation("发票ID: {InvoiceId} 收到发票代码: {InvoiceCode}", invoiceInfo.Id, fpdmValue);
                        // 发票代码将存储在SellerInvoiceData的完整回调数据中
                    }

                    // 更新开票金额，若回调中包含金额信息
                    if (invoiceData.TryGetValue("c_jshj", out var jshj) && jshj.ValueKind != JsonValueKind.Undefined)
                    {
                        if (decimal.TryParse(jshj.GetString(), out decimal amount))
                        {
                            invoiceInfo.TaxInclusiveAmount = amount;
                        }
                    }

                    // 更新开票日期 - 正确的修复版本
                    if (invoiceData.TryGetValue("c_kprq", out var kprq) && kprq.ValueKind != JsonValueKind.Undefined)
                    {
                        string dateString = kprq.GetString();
                        if (!string.IsNullOrEmpty(dateString))
                        {
                            // 尝试解析为时间戳（毫秒）
                            if (long.TryParse(dateString, out long timestamp))
                            {
                                invoiceInfo.InvoiceDate = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).DateTime;
                                _logger.LogInformation("发票ID: {InvoiceId} 成功解析开票日期时间戳: {DateString} -> {InvoiceDate}", 
                                    invoiceInfo.Id, dateString, invoiceInfo.InvoiceDate.Value.ToString("yyyy-MM-dd HH:mm:ss"));
                            }
                            // 如果不是时间戳，尝试直接解析日期字符串
                            else if (DateTime.TryParse(dateString, out DateTime invoiceDate))
                            {
                                invoiceInfo.InvoiceDate = invoiceDate;
                                _logger.LogInformation("发票ID: {InvoiceId} 成功解析开票日期字符串: {DateString} -> {InvoiceDate}", 
                                    invoiceInfo.Id, dateString, invoiceInfo.InvoiceDate.Value.ToString("yyyy-MM-dd HH:mm:ss"));
                            }
                            else
                            {
                                _logger.LogWarning("发票ID: {InvoiceId} 无法解析开票日期: {DateString}，使用当前时间", invoiceInfo.Id, dateString);
                                invoiceInfo.InvoiceDate = DateTime.Now;
                            }
                        }
                        else
                        {
                            _logger.LogWarning("发票ID: {InvoiceId} 开票日期字段为空，使用当前时间", invoiceInfo.Id);
                            invoiceInfo.InvoiceDate = DateTime.Now;
                        }
                    }
                    else
                    {
                        _logger.LogWarning("发票ID: {InvoiceId} 未找到开票日期字段 c_kprq，使用当前时间", invoiceInfo.Id);
                        invoiceInfo.InvoiceDate = DateTime.Now;
                    }

                    // 更新发票类型信息
                    if (invoiceData.TryGetValue("c_fpzl_dm", out var fpzlDm) && fpzlDm.ValueKind != JsonValueKind.Undefined)
                    {
                        // 更新发票类型代码，根据实际情况可能需要转换
                        invoiceInfo.InvoiceTypeCode = fpzlDm.GetString();
                    }

                    // 更新销方信息（如果回调中包含）
                    if (invoiceData.TryGetValue("c_xfmc", out var xfmc) && xfmc.ValueKind != JsonValueKind.Undefined)
                    {
                        invoiceInfo.SellerTitle = xfmc.GetString();
                    }

                    if (invoiceData.TryGetValue("c_xfsbh", out var xfsbh) && xfsbh.ValueKind != JsonValueKind.Undefined)
                    {
                        invoiceInfo.SellerTaxNum = xfsbh.GetString();
                    }

                    if (invoiceData.TryGetValue("c_xfdh", out var xfdh) && xfdh.ValueKind != JsonValueKind.Undefined)
                    {
                        invoiceInfo.SellerTel = xfdh.GetString();
                    }

                    if (invoiceData.TryGetValue("c_xfdz", out var xfdz) && xfdz.ValueKind != JsonValueKind.Undefined)
                    {
                        invoiceInfo.SellerAddress = xfdz.GetString();
                    }

                    if (invoiceData.TryGetValue("c_xfyhzh", out var xfyhzh) && xfyhzh.ValueKind != JsonValueKind.Undefined)
                    {
                        invoiceInfo.SellerAccount = xfyhzh.GetString();
                    }

                    // 更新购方信息（如果回调中包含）
                    if (invoiceData.TryGetValue("c_gfmc", out var gfmc) && gfmc.ValueKind != JsonValueKind.Undefined)
                    {
                        invoiceInfo.BuyerTitle = gfmc.GetString();
                    }

                    if (invoiceData.TryGetValue("c_gfsbh", out var gfsbh) && gfsbh.ValueKind != JsonValueKind.Undefined)
                    {
                        invoiceInfo.BuyerTaxNum = gfsbh.GetString();
                    }

                    // 更新备注信息
                    if (invoiceData.TryGetValue("c_bz", out var bz) && bz.ValueKind != JsonValueKind.Undefined)
                    {
                        invoiceInfo.Remark = bz.GetString();
                    }

                    // 记录校验码信息到日志中
                    if (invoiceData.TryGetValue("c_jym", out var jym) && jym.ValueKind != JsonValueKind.Undefined)
                    {
                        string jymValue = jym.GetString();
                        _logger.LogInformation("发票ID: {InvoiceId} 收到校验码: {VerificationCode}", invoiceInfo.Id, jymValue);
                    }

                    // 记录数电票号码信息到日志中
                    if (invoiceData.TryGetValue("c_ddh", out var ddh) && ddh.ValueKind != JsonValueKind.Undefined)
                    {
                        string ddhValue = ddh.GetString();
                        _logger.LogInformation("发票ID: {InvoiceId} 收到数电票号码: {DigitalInvoiceNumber}", invoiceInfo.Id, ddhValue);
                    }

                    // 更新PDF下载地址
                    if (invoiceData.TryGetValue("c_pdf_url", out var pdfUrl) && pdfUrl.ValueKind != JsonValueKind.Undefined)
                    {
                        invoiceInfo.PdfUrl = pdfUrl.GetString();
                    }

                    // 将完整的回调数据存储在SellerInvoiceData字段中
                    invoiceInfo.SellerInvoiceData = rawContent;

                    // 设置返回发票时间
                    invoiceInfo.ReturnInvoiceTime = DateTime.Now;

                    _logger.LogInformation("发票开具成功，已更新发票信息，发票ID: {InvoiceId}, 发票号: {InvoiceNumber}", 
                        invoiceInfo.Id, invoiceInfo.InvoiceNumber);
                }
                else if (status == "2") // 开票失败
                {
                    // 保存原审核人ID以便发送通知
                    var auditorId = invoiceInfo.AuditorId;

                    // 将状态重置为待审核状态 - 状态0表示创建后待审核
                    invoiceInfo.State = 0; // 确保状态值为0，表示创建后待审核

                    // 获取错误信息
                    string errorMessage = "开票失败: 未知原因";
                    if (invoiceData.TryGetValue("c_errorMessage", out var errorMsg) &&
                        errorMsg.ValueKind != JsonValueKind.Undefined)
                    {
                        errorMessage = errorMsg.GetString();
                    }

                    // 将错误信息存储在SellerInvoiceData字段和FailReason字段中
                    invoiceInfo.SellerInvoiceData = $"{{\"errorMessage\": \"{errorMessage}\"}}";
                    invoiceInfo.FailReason = errorMessage; // 明确记录失败原因
                    invoiceInfo.ReturnInvoiceTime = DateTime.Now;

                    _logger.LogWarning("发票开具失败，已重置状态，发票ID: {InvoiceId}, 失败原因: {ErrorMessage}", 
                        invoiceInfo.Id, errorMessage);

                    // 如果有原审核人ID，向审核人发送消息通知
                    if (auditorId.HasValue)
                    {
                        try
                        {
                            // 使用纯文本格式构建通知内容
                            string title = $"发票开具失败通知 - {invoiceInfo.InvoiceSerialNum}";
                            string content = $"发票开具失败\n" +
                                $"流水号: {invoiceInfo.InvoiceSerialNum}\n" +
                                $"购方名称: {invoiceInfo.BuyerTitle}\n" +
                                $"购方税号: {invoiceInfo.BuyerTaxNum}\n" +
                                $"销方名称: {invoiceInfo.SellerTitle}\n" +
                                $"销方税号: {invoiceInfo.SellerTaxNum}\n" +
                                $"失败原因: {errorMessage}\n\n";

                            // 发送系统消息给原审核人
                            _messageManager.SendMessage(
                                null,                   // 发送者ID，系统消息为null
                                new[] { auditorId.Value }, // 接收者ID数组，这里只有原审核人
                                title,                  // 消息标题
                                content,                // 消息内容，纯文本格式
                                true                    // 是系统消息
                            );

                            _logger.LogInformation("已向原审核人 {AuditorId} 发送开票失败通知，发票已重置为待审核状态", auditorId.Value);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "向原审核人 {AuditorId} 发送开票失败通知时出错", auditorId.Value);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("发票 {InvoiceId} 开票失败，但未找到原审核人ID，无法发送通知", invoiceInfo.Id);
                    }
                }
                else if (status == "3") // 开票成功签章失败
                {
                    // 状态值6为签章失败状态
                    invoiceInfo.State = 6; // 这是一个特殊状态，表示签章失败

                    // 获取错误信息
                    string errorMessage = "签章失败: 未知原因";
                    if (invoiceData.TryGetValue("c_errorMessage", out var errorMsg) &&
                        errorMsg.ValueKind != JsonValueKind.Undefined)
                    {
                        errorMessage = $"签章失败: {errorMsg.GetString()}";
                    }

                    // 将错误信息存储在SellerInvoiceData字段和FailReason字段中
                    invoiceInfo.SellerInvoiceData = $"{{\"errorMessage\": \"{errorMessage}\"}}";
                    invoiceInfo.FailReason = errorMessage; // 明确记录失败原因
                    invoiceInfo.ReturnInvoiceTime = DateTime.Now;

                    _logger.LogWarning("发票签章失败，已更新状态，发票ID: {InvoiceId}, 失败原因: {ErrorMessage}", 
                        invoiceInfo.Id, errorMessage);

                    // 如果有审核人ID，向审核人发送消息通知
                    if (invoiceInfo.AuditorId.HasValue)
                    {
                        try
                        {
                            // 使用纯文本格式构建通知内容
                            string title = $"发票签章失败通知 - {invoiceInfo.InvoiceSerialNum}";
                            string content = $"发票签章失败\n" +
                                $"流水号: {invoiceInfo.InvoiceSerialNum}\n" +
                                $"购方名称: {invoiceInfo.BuyerTitle}\n" +
                                $"购方税号: {invoiceInfo.BuyerTaxNum}\n" +
                                $"销方名称: {invoiceInfo.SellerTitle}\n" +
                                $"销方税号: {invoiceInfo.SellerTaxNum}\n" +
                                $"失败原因: {errorMessage}\n\n" +
                                $"发票已开具成功，但签章操作失败，请联系系统管理员。";

                            // 发送系统消息给审核人
                            _messageManager.SendMessage(
                                null,                       // 发送者ID，系统消息为null
                                new[] { invoiceInfo.AuditorId.Value }, // 接收者ID数组，这里只有审核人
                                title,                      // 消息标题
                                content,                    // 消息内容，纯文本格式
                                true                        // 是系统消息
                            );

                            _logger.LogInformation("已向审核人 {AuditorId} 发送签章失败通知", invoiceInfo.AuditorId.Value);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "向审核人 {AuditorId} 发送签章失败通知时出错", invoiceInfo.AuditorId.Value);
                        }
                    }
                }

                // 检查是否有关联的申请单，如果有则更新关联信息
                if (invoiceInfo.DocFeeRequisitionId.HasValue)
                {
                    try
                    {
                        var requisition = _dbContext.DocFeeRequisitions.Find(invoiceInfo.DocFeeRequisitionId.Value);
                        if (requisition != null)
                        {
                            requisition.TaxInvoiceId = invoiceInfo.Id;  // 更新申请单关联的发票ID
                            requisition.InvoiceNumber = invoiceInfo.InvoiceNumber;  // 回写发票号到申请单
                            _logger?.LogInformation("已更新费用申请单与发票的关联，申请单ID: {RequisitionId}, 发票ID: {InvoiceId}, 发票号: {InvoiceNumber}", 
                                requisition.Id, invoiceInfo.Id, invoiceInfo.InvoiceNumber);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "更新费用申请单与发票关联时出错，申请单ID: {RequisitionId}, 发票ID: {InvoiceId}", 
                            invoiceInfo.DocFeeRequisitionId, invoiceInfo.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新发票信息时发生异常, 发票ID: {InvoiceId}", invoiceInfo.Id);
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

                _logger.LogInformation("处理发票作废回调, 发票流水号: {InvoiceSerialNum}", fpqqlsh);

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
                            _logger.LogInformation("发票作废成功, 发票ID: {InvoiceId}", invoiceInfo.Id);
                        }
                        else if (status == "2") // 作废失败
                        {
                            // 记录失败原因
                            if (invalidData.TryGetValue("invalidErrorMessage", out var errorMsg) &&
                                errorMsg.ValueKind != JsonValueKind.Undefined)
                            {
                                _logger.LogWarning("发票作废失败: {ErrorMessage}, 发票ID: {InvoiceId}", errorMsg.GetString(), invoiceInfo.Id);
                                invoiceInfo.SellerInvoiceData = $"{{\"invalidErrorMessage\": \"{errorMsg.GetString()}\"}}";
                                _dbContext.SaveChanges();
                            }
                            else
                            {
                                _logger.LogWarning("发票作废失败: 未知原因, 发票ID: {InvoiceId}", invoiceInfo.Id);
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

                _logger.LogInformation("处理开票申请结果回调, 订单号: {OrderNo}, 成功: {IsSuccess}", orderno, isSuccess);

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

                _logger.LogInformation("处理红字信息表申请结果回调, 申请单号: {BillNo}", billNo);

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

                _logger.LogInformation("处理红字确认单申请结果回调, 申请单ID: {BillId}", billId);

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
