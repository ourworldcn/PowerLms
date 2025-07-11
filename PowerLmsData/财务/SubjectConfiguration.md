# 财务系统接口与凭证生成需求说明

## 1. 过程（系统内不映射）

| 过程名 | CODE | 说明 |
|--------|------|------|
| 发票挂账（B账） | PBI | POST Bill Invoice |
| 实收 | RF | Receive Funds |
| 实付 | PF | Pay Funds |
| 计提应收账款（A账挂账） | ARA | Accrue Receivable A 帐 |
| 计提应付账款（A账挂账） | APA | Accrue Payable A 帐 |
| 计提税金及主营业务收入 | ATR | Accrue Tax & Revenue |
| 计提A账应收本位币挂账 | ARAB | Accrue Receivable A-account Base Currency |
| 计提A账应付本位币挂账 | APAB | Accrue Payable A-account Base Currency |

---

### 2. 科目配置（超管维护）

子项 CODE = 过程 CODE 前缀 + 业务缩写；  
`GEN_` 前缀表示公共（非专属过程）科目。

| 过程名 | DisplayName | CODE | Remark |
|--------|-------------|------|--------|
| GEN | 主营业务成本 | GEN_COGS | 主营业务成本 |
| GEN | 代垫项 | GEN_ADVANCE_PAYMENT | 代收代付科目 |
| GEN | 制单人 | GEN_PREPARER | 金蝶制单人名称 |
| GEN | 计提总应收 | GEN_TOTAL_RECEIVABLE | 计提总应收科目代码：531 |
| GEN | 计提总应付 | GEN_TOTAL_PAYABLE | 计提总应付科目代码：532 |
| PBI | 主营业务收入 | PBI_SALES_REVENUE | 主营业务收入 |
| PBI | 应交税金 | PBI_TAX_PAYABLE | 应交税金 |
| PBI | 应收账款 | PBI_ACC_RECEIVABLE | 应收账款 |
| RF | 银行存款 | RF_BANK_DEPOSIT | 收款银行存款 |
| RF | 应收账款 | RF_ACC_RECEIVABLE | 冲销应收 |
| PF | 银行存款 | PF_BANK_DEPOSIT | 付款银行存款 |
| PF | 应付账款 | PF_ACC_PAYABLE | 应付账款 |
| ARAB | 非代垫国内客户计提应收 | ARAB_DOMESTIC_NON_ADVANCE | 科目代码：113.001.01 |
| ARAB | 代垫国内客户计提应收 | ARAB_DOMESTIC_ADVANCE | 科目代码：113.001.02 |
| ARAB | 非代垫国外客户计提应收 | ARAB_FOREIGN_NON_ADVANCE | 科目代码：113.002 |
| ARAB | 代垫国外客户计提应收 | ARAB_FOREIGN_ADVANCE | 科目代码：{暂空预留} |
| APAB | 非代垫国内客户计提应付 | APAB_DOMESTIC_NON_ADVANCE | 科目代码：203.001.01 |
| APAB | 代垫国内客户计提应付 | APAB_DOMESTIC_ADVANCE | 科目代码：203.001.02 |
| APAB | 非代垫国外客户计提应付 | APAB_FOREIGN_NON_ADVANCE | 科目代码：203.002 |
| APAB | 代垫国外客户计提应付 | APAB_FOREIGN_ADVANCE | 科目代码：{暂空预留} |

---

## 3. DBF 文件生成规则

必须在同一凭证号内保持一致的字段：  
`FCLSNAME1`（核销类别）、`FOBJID1`（客户财务简称）、`FOBJNAME1`（客户名称）、`FTRANSID`（客户财务编码）

常用字段映射（示例）：

| 字段 | 取值示例 |
|------|----------|
| FAcctID | PBI_ACC_RECEIVABLE |
| FPREPARE | GEN_PREPARER |
| Fgroup | 转 |
| FDC | D / C |
| … | … |

---

## 4. 凭证生成流程

### 4.1 发票挂账（B账） — PBI

| 分录序号 | FAcctID | 借/贷 | 金额 | 摘要 |
|---------|---------|-------|------|------|
| 0 | PBI_ACC_RECEIVABLE | 借 | 价税合计 | 客户名＋开票明细＋客户财务代码 |
| 1 | PBI_SALES_REVENUE  | 贷 | 价额（价税合计−税额） | 同上 |
| 2 | PBI_TAX_PAYABLE    | 贷 | 税额 | 同上 |

共同字段：FDATE/FTRANSDATE/FPeriod 取发票开票日期；Fnum 当天连续编号；FPREPARE = GEN_PREPARER。

---

### 4.2 实收 — RF

| 分录序号 | FAcctID | 借/贷 | 外币金额 | 本位币金额 | 摘要 |
|---------|---------|-------|----------|------------|------|
| 0 | RF_BANK_DEPOSIT   | 借 | 结算总额 | 结算总额 × 汇率 | 结算单位名＋客户财务代码 |
| 1 | RF_ACC_RECEIVABLE | 贷 | 结算总额 | 结算总额 × 汇率 | 同上 |

共同字段：FDATE/FTRANSDATE 取结算单财务日期；FPeriod 取月份；Fnum 当天连续编号；FPREPARE = GEN_PREPARER。

---

### 4.3 实付 — PF

| 分录序号 | FAcctID | 借/贷 | 金额 | 摘要 |
|---------|---------|-------|------|------|
| 0 | PF_ACC_PAYABLE  | 借 | 付款金额 | 供应商名＋付款摘要 |
| 1 | PF_BANK_DEPOSIT | 贷 | 付款金额 | 同上 |

---

### 4.4 计提A账应收本位币挂账 — ARAB

**统计条件：** 默认工作号财务日期月初到操作日，可重复记账，IO=收入，  
按 `sum(Amount*ExchangeRate) as Totalamount group by 费用.结算单位，费用.结算单位.国内外，费用.费用种类.代垫` 进行汇总

**凭证结构：** 每个凭证至少2个分录，先生成各类应收明细分录，最后生成计提总应收分录

**共同字段：** FDATE/FTRANSDATE/FPeriod 取记账日期；Fnum 生成连续编号；FPREPARE = GEN_PREPARER；FGROUP = "转"

#### 4.4.1 计提总应收汇总分录（分录序号：0）

| 分录序号 | FAcctID | 借/贷 | 金额 | 摘要 | FCLSNAME1 | FOBJID1 | FOBJNAME1 | FTRANSID |
|---------|---------|-------|------|------|----------|---------|----------|----------|
| 0 | GEN_TOTAL_RECEIVABLE | 借 | Sum(Totalamount) | 计提YYYY年MM月总应收 XXX元 | 【空白】 | 【空白】 | 【空白】 | 【空白】 |

#### 4.4.2 应收账款明细分录（分录序号：1-4）

| 分录序号 | 条件 | FAcctID | 借/贷 | 金额 | 摘要格式 | FCLSNAME1 | FOBJID1 | FOBJNAME1 | FTRANSID |
|---------|------|---------|-------|------|----------|----------|---------|----------|----------|
| 1 | 结算单位=国内、费用.代垫=false | ARAB_DOMESTIC_NON_ADVANCE | 贷 | Totalamount | 计提YYYY年MM月总应收:国内应收账款-客户-{结算单位名称} {金额}元 | 客户 | 客户简称 | 客户名称 | 客户财务编码 |
| 2 | 结算单位=国内、费用.代垫=true | ARAB_DOMESTIC_ADVANCE | 贷 | Totalamount | 计提YYYY年MM月总应收:国内应收账款-关税-{结算单位名称} {金额}元 | 客户 | 客户简称 | 客户名称 | 客户财务编码 |
| 3 | 结算单位=国外、费用.代垫=false | ARAB_FOREIGN_NON_ADVANCE | 贷 | Totalamount | 计提YYYY年MM月总应收:国外应收账款-{结算单位名称} {金额}元 | 客户 | 客户简称 | 客户名称 | 客户财务编码 |
| 4 | 结算单位=国外、费用.代垫=true | ARAB_FOREIGN_ADVANCE | 贷 | Totalamount | 计提YYYY年MM月总应收:国外应收账款-关税-{结算单位名称} {金额}元 | 客户 | 客户简称 | 客户名称 | 客户财务编码 |

**说明：**
- 客户简称：docfee的结算单位在客户资料中的简称
- 客户名称：docfee的结算单位名称  
- 客户财务编码：docfee的结算单位财务编码
- 所有金额字段均使用本位币
- 汇率固定为1

---

### 4.5 计提A账应付本位币挂账 — APAB

**统计条件：** 默认工作号财务日期月初到操作日，可重复记账，IO=支出，  
按 `sum(Amount*ExchangeRate) as Totalamount group by 费用.结算单位，费用.结算单位.国内外，费用.费用种类.代垫` 进行汇总

**凭证结构：** 每个凭证至少2个分录，先生成各类应付明细分录，最后生成计提总应付分录

**共同字段：** FDATE/FTRANSDATE/FPeriod 取记账日期；Fnum 生成连续编号；FPREPARE = GEN_PREPARER；FGROUP = "转"

#### 4.5.1 计提总应付汇总分录（分录序号：0）

| 分录序号 | FAcctID | 借/贷 | 金额 | 摘要 | FCLSNAME1 | FOBJID1 | FOBJNAME1 | FTRANSID |
|---------|---------|-------|------|------|----------|---------|----------|----------|
| 0 | GEN_TOTAL_PAYABLE | 贷 | Sum(Totalamount) | 计提YYYY年MM月总应付 XXX元 | 【空白】 | 【空白】 | 【空白】 | 【空白】 |

#### 4.5.2 应付账款明细分录（分录序号：1-4）

| 分录序号 | 条件 | FAcctID | 借/贷 | 金额 | 摘要格式 | FCLSNAME1 | FOBJID1 | FOBJNAME1 | FTRANSID |
|---------|------|---------|-------|------|----------|----------|---------|----------|----------|
| 1 | 结算单位=国内、费用.代垫=false | APAB_DOMESTIC_NON_ADVANCE | 借 | Totalamount | 计提YYYY年MM月总应付:国内应付账款-供应商-{结算单位名称} {金额}元 | 供应商 | 客户简称 | 客户名称 | 客户财务编码 |
| 2 | 结算单位=国内、费用.代垫=true | APAB_DOMESTIC_ADVANCE | 借 | Totalamount | 计提YYYY年MM月总应付:国内应付账款-关税-{结算单位名称} {金额}元 | 供应商 | 客户简称 | 客户名称 | 客户财务编码 |
| 3 | 结算单位=国外、费用.代垫=false | APAB_FOREIGN_NON_ADVANCE | 借 | Totalamount | 计提YYYY年MM月总应付:国外应付账款-{结算单位名称} {金额}元 | 供应商 | 客户简称 | 客户名称 | 客户财务编码 |
| 4 | 结算单位=国外、费用.代垫=true | APAB_FOREIGN_ADVANCE | 借 | Totalamount | 计提YYYY年MM月总应付:国外应付账款-关税-{结算单位名称} {金额}元 | 供应商 | 客户简称 | 客户名称 | 客户财务编码 |

**说明：**
- 客户简称：docfee的结算单位在客户资料中的简称
- 客户名称：docfee的结算单位名称  
- 客户财务编码：docfee的结算单位财务编码
- 所有金额字段均使用本位币
- 汇率固定为1

---

## 5. 其他说明

1. 子项 CODE 新增时须使用对应过程 CODE 前缀或 `GEN_`，并保证在科目配置表内唯一。  
2. 汇率默认取结算明细首行，若无外币固定为 1。  
3. 任务需加锁，避免重复生成凭证。  
4. 出现异常时回滚已标记状态并记录日志。  
5. 计提类过程（ARAB、APAB）支持重复记账，需要在业务逻辑中处理重复性检查。
6. 计提类过程的分录生成顺序：先按条件生成各类明细分录，最后生成总计分录（分录序号为0）。

如需扩展科目或过程，请同步更新 **1. 过程（系统内不映射）** 与 **2. 科目配置（超管维护）**，并保持各自唯一性.

----