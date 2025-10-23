# 金蝶凭证生成逻辑重构 - 从7分录升级为12分录模型

## 0. 编码规范与约束 🔧

### 0.1 强制性编码要求
- **每个分录规则独立函数**：如GenerateReceivableForeignVoucher()，返回List<KingdeeVoucher>
- **批量加载策略**：分录生成前一次性批量加载所有关联数据到内存，禁止循环中查询数据库
- **性能目标**：1000个结算单导出 < 2分钟，使用Dictionary加速查询
- **数据源优先级**：ActualFinancialTransaction表 > PlInvoicesItem明细（收款）/ 结算单金额（付款）
- **零值分录处理**：金额为0的分录不生成（默认规则，无需额外判断）

### 0.2 金额与精度
- **汇率精度**：4位小数（decimal(18,4)）
- **金额精度**：2位小数（decimal(18,2)）
- **借贷平衡容差**：≤0.01元
- **本位币计算**：明细金额 × 原费用汇率（DocFee.ExchangeRate）

### 0.3 异常处理
- **数据缺失**：跳过记录并记录警告
- **配置缺失**：终止导出并返回明确错误信息
- **借贷不平**：调试环境警告，生产环境报错

### 0.4 凭证号与分录号生成规则（基于Excel规则）
- **凭证号（FNUM）**：同一结算单的所有分录共用一个凭证号（递增序号如1、2、3...）
- **分录号（FENTRYID）**：同一凭证号内从0开始递增，不重复
- **凭证字（FGROUP）**：从SubjectConfiguration.VoucherGroup获取（如"转"、"银"）

## 1. 业务规则核心

### 1.1 科目拆分逻辑
**将往来账款按"地域"和"业务性质"拆分为3类细分科目**：

| 分类 | 判断条件 | 科目代码后缀 |
|------|---------|------------|
| 国外客户 | PlCustomer.IsDomestic = false | _OUT_CUS |
| 国内客户（非代垫） | IsDomestic = true 且 FeesType.IsDaiDian = false | _IN_CUS |
| 国内关税（代垫） | IsDomestic = true 且 FeesType.IsDaiDian = true | _IN_TAR |

**注意**：IsDomestic为null时按国内处理（customer?.IsDomestic ?? true）

### 1.2 混合业务判断
- **判断依据**：通过DocFee.IO统计收入（true）和支出（false）项目数量
- **触发条件**：同一结算单中既有收入又有支出
- **业务场景**：客户付款时扣除费用、付供应商款时扣除收入（实际存在但罕见）

## 2. 收款单12分录规则

### 规则1：银行收款（借方，1~N条必生成）
- **数据源**：优先查ActualFinancialTransaction表（ParentId匹配且IsDelete=false），无记录则用PlInvoicesItem
- **科目代码来源**：**从每笔收款记录关联的BankInfo动态获取**（ActualFinancialTransaction.BankAccountId → BankInfo.AAccountSubjectCode）
- **回退逻辑**：如无ActualFinancialTransaction记录，从PlInvoices.BankId获取银行科目
- **摘要**：往来单位名+【收入】+收款单号
- **分录号**：从0开始（多笔收款时为0、1、2...）

### 规则2：应收账款冲抵（贷方，1~3条必生成）
- **2A-国外**：国外客户所有收入明细本位币金额汇总，科目SR_RECEIVABLE_CREDIT_OUT_CUS
- **2B-国内客户**：国内非代垫收入明细汇总，科目SR_RECEIVABLE_CREDIT_IN_CUS
- **2C-国内关税**：国内代垫收入明细汇总，科目SR_RECEIVABLE_CREDIT_IN_TAR
- **分录号**：紧接规则1递增（如规则1占用0-2，则此处从3开始）
- **计算公式**：sum(if 明细.费用种类.代垫=false/true，明细.收支=收入，明细本次结算金额×明细原费用本位币汇率)

### 规则3：应付账款冲抵（借方，0~3条混合业务生成）
- **3A-国外**：国外客户所有支出明细本位币金额汇总，科目SR_PAYABLE_DEBIT_OUT_CUS
- **3B-国内客户**：国内非代垫支出明细汇总，科目SR_PAYABLE_DEBIT_IN_CUS
- **3C-国内关税**：国内代垫支出明细汇总，科目SR_PAYABLE_DEBIT_IN_TAR
- **分录号**：紧接规则2递增
- **计算公式**：sum(if 明细.费用种类.代垫=false/true，明细.收支=支出，明细本次结算金额×明细原费用本位币汇率)

### 规则4-7：其他科目（条件生成）
- **规则4**：预收款（贷方），条件：AdvancePaymentAmount > 0，科目SR_ADVANCE_CREDIT
- **规则5**：汇兑损益（借/贷），条件：ExchangeLoss ≠ 0，科目SR_EXCHANGE_LOSS
- **规则6**：手续费（借方），条件：ServiceFeeAmount > 0，科目SR_SERVICE_FEE_DEBIT
- **规则7**：预收冲应收（借方），条件：AdvanceOffsetReceivableAmount > 0，科目SR_ADVANCE_OFFSET_DEBIT
- **分录号**：依次递增，保持连续

## 3. 付款单12分录规则

### 规则1：银行付款（贷方，1~N条必生成）
- **数据源**：优先查ActualFinancialTransaction表，无记录则用结算单金额
- **科目代码来源**：**从每笔付款记录关联的BankInfo动态获取**（ActualFinancialTransaction.BankAccountId → BankInfo.AAccountSubjectCode）
- **回退逻辑**：如无ActualFinancialTransaction记录，从PlInvoices.BankId获取银行科目，或使用SP_BANK_CREDIT配置兜底
- **摘要**：往来单位名+【支出】+付款单号
- **分录号**：从0开始（多笔付款时为0、1、2...）

### 规则2：应付账款冲销（借方，1~3条必生成）
- **2A-国外**：国外供应商所有支出明细本位币金额汇总，科目SP_PAYABLE_DEBIT_OUT_CUS
- **2B-国内客户**：国内非代垫支出明细汇总，科目SP_PAYABLE_DEBIT_IN_CUS
- **2C-国内关税**：国内代垫支出明细汇总，科目SP_PAYABLE_DEBIT_IN_TAR
- **分录号**：紧接规则1递增
- **计算公式**：sum(if 明细.费用种类.代垫=false/true，明细.收支=支出，明细本次结算金额×明细原费用本位币汇率)

### 规则3：应收账款增加（贷方，0~3条混合业务生成）
- **3A-国外**：国外客户所有收入明细本位币金额汇总，科目SP_RECEIVABLE_CREDIT_OUT_CUS
- **3B-国内客户**：国内非代垫收入明细汇总，科目SP_RECEIVABLE_CREDIT_IN_CUS
- **3C-国内关税**：国内代垫收入明细汇总，科目SP_RECEIVABLE_CREDIT_IN_TAR
- **分录号**：紧接规则2递增
- **计算公式**：sum(if 明细.费用种类.代垫=false/true，明细.收支=收入，明细本次结算金额×明细原费用本位币汇率)

### 规则4-7：其他科目（条件生成）
- **规则4**：汇兑损益（借/贷），条件：ExchangeLoss ≠ 0，科目SP_EXCHANGE_LOSS
- **规则5**：手续费支出（借方），条件：ServiceFeeAmount > 0，科目SP_SERVICE_FEE_DEBIT
- **规则6**：手续费银行扣款（贷方），与规则5配对，科目SP_SERVICE_FEE_CREDIT，形成自平衡
- **规则7**：预付款（借方），条件：AdvancePaymentAmount > 0，科目SP_ADVANCE_CREDIT
- **分录号**：依次递增，保持连续

## 4. 科目配置管理

### 4.1 科目代码清单
**收款单（SR_前缀）**：
- 应收：SR_RECEIVABLE_CREDIT（旧兜底）、SR_RECEIVABLE_CREDIT_IN_CUS、SR_RECEIVABLE_CREDIT_IN_TAR、SR_RECEIVABLE_CREDIT_OUT_CUS
- 应付：SR_PAYABLE_DEBIT（旧兜底）、SR_PAYABLE_DEBIT_IN_CUS、SR_PAYABLE_DEBIT_IN_TAR、SR_PAYABLE_DEBIT_OUT_CUS
- 其他：SR_ADVANCE_CREDIT、SR_EXCHANGE_LOSS、SR_SERVICE_FEE_DEBIT、SR_ADVANCE_OFFSET_DEBIT
- **制单人**：SR_PREPARER（从SubjectConfiguration.DisplayName获取）
- **凭证字**：SR_VOUCHER_GROUP（从SubjectConfiguration.VoucherGroup获取，支持下拉快捷输入）

**付款单（SP_前缀）**：
- 银行：SP_BANK_CREDIT（兜底科目，优先使用BankInfo动态获取）
- 应付：SP_PAYABLE_DEBIT（旧兜底）、SP_PAYABLE_DEBIT_IN_CUS、SP_PAYABLE_DEBIT_IN_TAR、SP_PAYABLE_DEBIT_OUT_CUS
- 应收：SP_RECEIVABLE_CREDIT（旧兜底）、SP_RECEIVABLE_CREDIT_IN_CUS、SP_RECEIVABLE_CREDIT_IN_TAR、SP_RECEIVABLE_CREDIT_OUT_CUS
- 其他：SP_EXCHANGE_LOSS、SP_SERVICE_FEE_DEBIT、SP_SERVICE_FEE_CREDIT、SP_ADVANCE_CREDIT
- **制单人**：SP_PREPARER
- **凭证字**：SP_VOUCHER_GROUP（支持下拉快捷输入）

### 4.2 三级回退策略
- **第一优先**：细分配置（如SR_RECEIVABLE_CREDIT_IN_CUS）
- **第二优先**：旧配置（如SR_RECEIVABLE_CREDIT）
- **第三优先**：硬编码默认值
- **实现**：GetValidSubjectCode(细分科目, 旧科目, 默认值)

### 4.3 动态科目获取（银行科目特殊处理）
- **多笔收付款场景**：每笔从ActualFinancialTransaction.BankAccountId → BankInfo.AAccountSubjectCode动态获取
- **单笔或无实际记录**：从PlInvoices.BankId → BankInfo.AAccountSubjectCode获取
- **最终兜底**：使用配置的SP_BANK_CREDIT或硬编码默认值

## 5. 数据关联路径

### 5.1 核心实体关系
```
PlInvoices（结算单）
  ├─ IO（true=收款，false=付款）
  ├─ JiesuanDanweiId → PlCustomer.IsDomestic（国内外判断，整个结算单统一往来单位）
  └─ PlInvoicesItem（结算单明细）
       └─ RequisitionItemId → DocFeeRequisitionItem
            └─ FeeId → DocFee
                 ├─ IO（true=收入，false=支出）
                 ├─ ExchangeRate（原费用汇率）
                 └─ FeeTypeId → FeesType.IsDaiDian（代垫判断，bool类型）

ActualFinancialTransaction（实际收付记录，多笔收付款）
  ├─ ParentId → PlInvoices.Id
  ├─ Amount（实收付金额）
  ├─ BankAccountId → BankInfo.AAccountSubjectCode（动态获取银行科目）
  ├─ TransactionDate（收付款日期，用于排序）
  └─ IsDelete（软删除标记）
```

### 5.2 批量加载模式
```csharp
// 步骤1：批量查询所有关联数据
var settlementIds = settlements.Select(s => s.Id).ToList();
var allItems = dbContext.PlInvoicesItems
    .Where(i => settlementIds.Contains(i.ParentId.Value))
    .Include(i => i.RequisitionItem).ThenInclude(ri => ri.Fee).ThenInclude(f => f.FeeType)
    .ToList();
var allCustomers = dbContext.PlCustomers.Where(c => settlements.Select(s => s.JiesuanDanweiId).Contains(c.Id)).ToList();
var allTransactions = dbContext.ActualFinancialTransactions
    .Where(t => settlementIds.Contains(t.ParentId) && !t.IsDelete)
    .Include(t => t.BankAccount)  // 预加载银行信息以获取科目代码
    .OrderBy(t => t.TransactionDate)  // 按时间排序
    .ToList();

// 步骤2：构建内存字典
var customerDict = allCustomers.ToDictionary(c => c.Id);
var itemsGrouped = allItems.GroupBy(i => i.ParentId.Value).ToDictionary(g => g.Key, g => g.ToList());
var transactionsGrouped = allTransactions.GroupBy(t => t.ParentId).ToDictionary(g => g.Key, g => g.ToList());
```

## 6. 实施清单

### 6.1 改造范围
**收款单（11个独立函数）**：
- 规则1：GenerateBankReceiptVouchers()（增强：优先查ActualFinancialTransaction，动态获取银行科目）
- 规则2：GenerateReceivableForeignVoucher()、GenerateReceivableDomesticCustomerVoucher()、GenerateReceivableDomesticTariffVoucher()
- 规则3：GeneratePayableForeignVoucher()、GeneratePayableDomesticCustomerVoucher()、GeneratePayableDomesticTariffVoucher()
- 规则4-7：GenerateAdvanceReceiptVoucher()、GenerateExchangeLossVoucher()、GenerateServiceFeeVoucher()、GenerateAdvanceOffsetVoucher()

**付款单（11个独立函数）**：
- 规则1：GeneratePaymentBankVouchers()（动态获取银行科目）
- 规则2：GeneratePayableDebitForeignVoucher()、GeneratePayableDebitDomesticCustomerVoucher()、GeneratePayableDebitDomesticTariffVoucher()
- 规则3：GenerateReceivableCreditForeignVoucher()、GenerateReceivableCreditDomesticCustomerVoucher()、GenerateReceivableCreditDomesticTariffVoucher()
- 规则4-7：GeneratePaymentExchangeLossVoucher()、GeneratePaymentServiceFeeDebitVoucher()、GeneratePaymentServiceFeeCreditVoucher()、GenerateAdvancePaymentVoucher()

### 6.2 现有代码位置
- **收款单**：`PowerLmsWebApi/Controllers/Financial/FinancialSystemExportController.SettlementReceipt.cs`
- **付款单**：`PowerLmsWebApi/Controllers/Financial/FinancialSystemExportController.SettlementPayment.cs`
- **科目配置**：`PowerLms.Data.Finance.SubjectConfiguration`
- **凭证实体**：`PowerLms.Data.Finance.KingdeeVoucher`（21个DBF字段）

### 6.3 金额计算算法
**步骤1**：获取往来单位国别（customer?.IsDomestic ?? true）
**步骤2**：分类汇总
- 国外：所有金额汇总
- 国内：按代垫属性拆分（IsDaiDian=true→关税，false→客户）
**步骤3**：零值过滤（金额>0才生成分录，自动忽略0值）

---

## ✅ 所有疑问已解决

### 基于Excel规则文档确认的关键信息：

1. **✅ 凭证号（FNUM）生成策略**：
   - **规则明确**："同一任务中递增凭证号，同一分录组的相同"
   - **实现方案**：同一结算单的所有分录共用一个凭证号
   - **示例**：一个收款单分3次收款，生成1张凭证包含所有分录（银行×3 + 应收×1-3 + 其他）

2. **✅ 分录号（FENTRYID）生成规则**：
   - **规则明确**："每个凭证号内从0开始，不重复，同一凭证号中递增序号"
   - **实现方案**：同一凭证号内按业务规则顺序递增（0、1、2、3...）
   - **排序要求**：无特殊要求，只需保持连续递增

3. **✅ 本位币金额计算公式**：
   - **规则明确**："明细本次结算金额×明细原费用本位币汇率"
   - **关键字段**：PlInvoicesItem.Amount × DocFee.ExchangeRate
   - **与结算汇率区分**：不使用PlInvoices.PaymentExchangeRate

4. **✅ 摘要格式**：
   - **收款单**：往来单位名+【收入】+收款单号
   - **付款单**：往来单位名+【支出】+付款单号
   - **特殊场景**：部分规则可能需要补充金额信息

---

**版本**：v3.0（最终版）
**最后更新**：2025-01-31
**状态**：✅ 所有疑问已解决，准备开始实施
