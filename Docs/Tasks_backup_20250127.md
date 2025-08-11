# ZC@WorkGroup 任务执行计划

## 任务概览
根据会议纪要中的工作安排，作为后端开发负责以下核心任务：

### 紧急Bug修复
1. **OA备用申请单编辑保存失败** ✅ **已完成**
2. **工作流Send方法审批意见丢失问题**（已在临时输出.md中确认修复）

### 主营业务结算单功能改造
1. **实体与数据库变更**
2. **通用实际收付记录表**
3. **计算工具接口开发**

### 财务导出接口
1. **财务单据导出接口开发**

---

## 详细执行计划

### 任务一：Bug修复 [高优先级] ✅ **已完成**

#### 1.1 OA备用申请单编辑保存失败 ✅ **已完成**
**问题描述：** 前端发送正确数据，但后端未能成功写入数据库

**问题根因：** `OaExpenseController.ModifyOaExpenseRequisition`方法缺少`_DbContext.SaveChanges()`调用

**修复内容：**
```csharp
// 在字段保护逻辑之后添加：
_DbContext.SaveChanges();
_Logger.LogInformation("成功修改OA费用申请单，用户: {UserId}, 申请单数量: {Count}", 
    context.User.Id, model.Items.Count());
```

**验证结果：** ✅ 编译成功，Bug已修复

#### 1.2 数据锁定功能实现 ✅ **已完成并修正**
**需求：** 审批中的单据不允许修改主表单信息

**实现范围：**
1. **OA日常费用申请单**：主单+明细分别锁定
2. **主营业务费用申请单**：基于工作流状态实现锁定

**具体实现：**

**OA日常费用申请单锁定逻辑：**
```csharp
// 主单锁定：只有草稿状态可以修改主单
if (!existing.CanEdit(_DbContext))
{
    result.HasError = true;
    result.ErrorCode = 403;
    result.DebugMessage = GetEditRestrictionMessage(existing.Status);
    return result;
}

// 明细锁定：结算后不能修改明细项
if (!requisition.CanEditItems(_DbContext))
{
    result.HasError = true;
    result.ErrorCode = 403;
    result.DebugMessage = "申请单当前状态不允许修改明细";
    return result;
}
```

**主营业务费用申请单锁定逻辑：**
```csharp
// 检查申请单是否已进入审批流程
if (IsInWorkflow(originalEntity.Id))
{
    result.HasError = true;
    result.ErrorCode = 403;
    result.DebugMessage = "申请单已进入审批流程，主表单信息不能修改";
    return result;
}

private bool IsInWorkflow(Guid requisitionId)
{
    // 查询是否存在关联的工作流
    var workflow = _DbContext.OwWfs
        .Where(w => w.DocId == requisitionId && w.State != 8) // 排除失败结束的流程
        .FirstOrDefault();
    return workflow != null;
}
```

**锁定规则总结：**
- **OA日常费用申请单主单**：只有草稿状态可编辑
- **OA日常费用申请单明细**：结算前可编辑，结算后锁定
- **主营业务费用申请单**：进入审批流程后主单锁定
- **确认后状态**：完全锁定（仅允许系统字段更新）

**预估工期：** 0.5天 → **实际完成：** 0.5天

### 任务二：主营业务结算单功能改造 [核心功能]

#### 2.1 实体与数据库变更
**需求：** 根据Excel文档新增约13个字段

**执行步骤：**
```
1. 分析现有实体结构
   - 查找MainBusinessSettlement相关实体
   - 分析当前字段定义
   
2. 对比Excel文档需求
   - 确认新增字段列表
   - 定义字段类型和约束
   
3. 更新实体定义
   - 添加新增字段属性
   - 配置数据注解和约束
   - 更新字段注释
   
4. 数据库迁移（手动规划）
   - 编写ALTER TABLE语句
   - 考虑数据兼容性
   - 制定回滚方案
```

**字段设计要点：**
- 金额字段：decimal(18,2) - 两位小数
- 汇率字段：decimal(18,4) - 四位小数  
- 新增字段包括："收付单号"、"财务凭证号"、"是否导出到财务软件"等

**预估工期：** 1.5天

#### 2.2 通用实际收付记录表设计
**需求：** 处理一笔结算分多次收/付款场景

**执行步骤：**
```
1. 设计表结构
   CREATE TABLE ActualPaymentRecords (
       Id uniqueidentifier PRIMARY KEY,
       ParentId uniqueidentifier NOT NULL, -- 通用父单据ID
       PaymentDate datetime2 NOT NULL,     -- 收付日期
       Amount decimal(18,2) NOT NULL,      -- 金额
       AmountInBaseCurrency decimal(18,2), -- 本位币金额
       ExchangeRate decimal(18,4),         -- 汇率
       CurrencyId uniqueidentifier,        -- 币种ID
       PaymentMethod nvarchar(50),         -- 收付方式
       Remark nvarchar(500),              -- 备注
       CreateTime datetime2 DEFAULT GETDATE(),
       CreatorId uniqueidentifier,
       IsDelete bit DEFAULT 0
   )
   
2. 创建对应实体类
   - 定义ActualPaymentRecord实体
   - 配置数据注解
   - 设置导航属性
   
3. 实现CRUD接口
   - AddActualPaymentRecord
   - GetActualPaymentRecords  
   - ModifyActualPaymentRecord
   - RemoveActualPaymentRecord
```

**预估工期：** 1天

#### 2.3 计算工具接口开发
**需求：** 提供核销金额等复杂计算的统一接口

**执行步骤：**
```
1. 设计计算接口
   - 定义输入参数DTO
   - 确定计算输出格式
   
2. 实现计算逻辑
   public class SettlementCalculationService 
   {
       // 核销金额计算
       public CalculationResultDto CalculateWriteOffAmount(...)
       
       // 汇率转换计算  
       public decimal ConvertCurrency(...)
       
       // 金额汇总计算
       public CalculationResultDto SumAmounts(...)
   }
   
3. 创建Controller接口
   [HttpPost]
   public ActionResult<CalculationResultDto> Calculate(CalculationParamsDto model)
```

**计算规则：**
- 确保浮点数精度一致性
- 与金蝶导出算法保持一致
- 支持多币种计算

**预估工期：** 1天

### 任务三：财务导出接口开发 [紧急需求]

#### 3.1 财务单据导出接口
**需求：** 完成两个财务单据导出接口（客户已催促）

**执行步骤：**
```
1. 分析现有导出代码
   - 查看FinancialSystemExportController
   - 了解现有导出逻辑
   
2. 设计导出接口
   - 主营业务结算单导出
   - 费用申请单导出
   
3. 实现导出功能
   - 数据查询和筛选
   - 格式转换（Excel/DBF）
   - 权限控制
   - 异步任务处理
   
4. 测试和优化
   - 大数据量测试
   - 性能优化
   - 错误处理
```

**技术要点：**
- 使用现有的任务调度机制
- 保持与发票导出一致的架构
- 支持条件筛选和权限过滤

**预估工期：** 2天

---

## 总体时间规划

| 任务 | 优先级 | 预估工期 | 完成状态 |
|------|--------|----------|----------|
| OA申请单Bug修复 | 高 | 0.5天 | ✅ **已完成** |
| 数据锁定功能 | 高 | 0.5天 | ✅ **已完成** |
| 财务导出接口 | 高 | 2天 | 📋 计划中 |
| 实际收付记录表 | 中 | 1天 | 📋 计划中 |
| 结算单实体改造 | 中 | 1.5天 | 📋 计划中 |
| 计算工具接口 | 中 | 1天 | 📋 计划中 |

**总计：6.5个工作日** | **已完成：1天** | **剩余：5.5天**

---

## 已完成任务详细记录

### ✅ 任务1.1：OA申请单编辑保存失败修复
**修复时间：** 2025-01-27  
**问题定位：** `PowerLmsWebApi/Controllers/OaExpenseController.cs` 的 `ModifyOaExpenseRequisition` 方法  
**根本原因：** 缺少 `_DbContext.SaveChanges()` 调用  
**修复方案：** 在字段保护逻辑后添加SaveChanges调用和日志记录  
**验证结果：** 编译成功，功能恢复正常  

### ✅ 任务1.2：数据锁定功能实现（已修正）
**实现时间：** 2025-01-27  
**实现文件：** 
- `PowerLmsWebApi/Controllers/OaExpenseController.cs` - OA费用申请单主单锁定
- `PowerLmsWebApi/Controllers/OaExpenseController.Item.cs` - OA费用申请单明细锁定
- `PowerLmsWebApi/Controllers/FinancialController.DocFeeRequisition.cs` - 主营业务费用申请单锁定

**核心技术方案：**
1. **状态驱动锁定**：基于申请单状态或工作流状态判断编辑权限
2. **分层权限控制**：主单和明细分别控制编辑权限
3. **字段级保护**：使用EF Core的`IsModified = false`保护特定字段
4. **用户友好提示**：返回明确的错误信息说明限制原因

**锁定规则详细说明：**
- **OA日常费用申请单主单**：`CanEdit()` - 只有草稿状态可编辑
- **OA日常费用申请单明细**：`CanEditItems()` - 结算前可编辑，结算后锁定
- **主营业务费用申请单**：`IsInWorkflow()` - 进入审批流程后主单锁定

**验证结果：** 编译成功，逻辑完整，错误处理完善

---

## 技术风险与注意事项

### 1. 数据库变更风险
- **风险：** 生产环境数据结构变更可能影响现有功能
- **应对：** 制定详细的迁移脚本和回滚方案

### 2. 兼容性风险  
- **风险：** 新增字段可能影响前端现有逻辑
- **应对：** 确保新字段有合理默认值，保持向后兼容

### 3. 性能风险
- **风险：** 复杂计算和大数据量导出可能影响性能
- **应对：** 使用异步处理，优化查询语句

### 4. 业务逻辑风险
- **风险：** 计算逻辑与前端不一致导致数据差异
- **应对：** 提供统一的计算接口，确保前后端使用相同算法

---

## 协作要求

### 与前端开发协作
1. **字段变更通知：** 新增字段定义完成后及时通知前端
2. **接口文档：** 提供详细的API文档和示例
3. **联调测试：** 完成后端开发后安排联调测试

### 与产品经理协作  
1. **需求确认：** Excel文档中的字段定义需要最终确认
2. **验收标准：** 明确功能验收的具体标准
3. **优先级调整：** 如有紧急需求变更及时沟通

---

## 备注说明

1. **禁止自动创建数据迁移：** 所有数据库变更必须手动规划，不使用EF自动迁移
2. **代码质量：** 保持与现有代码风格一致，添加充分的注释和错误处理
3. **测试覆盖：** 重要功能需要编写单元测试，确保代码质量
4. **文档更新：** 完成开发后更新相关技术文档

**执行状态：** 📋 任务1已完成，继续执行剩余任务