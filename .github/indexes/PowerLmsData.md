# PowerLmsData 数据层索引

## 📋 实体分类总览

| 业务域 | 文件夹 | 主要实体 | 数量 |
|--------|--------|----------|------|
| 账号权限 | 账号/、权限/ | Account, PlRole, PlPermission | 6个 |
| 组织架构 | 机构/ | PlMerchant, PlOrganization, PlOrganizationParameter | 3个 |
| 客户资料 | 客户资料/ | PlCustomer, PlCustomerContact, PlTaxInfo | 6个 |
| 业务单据 | 业务/ | PlJob, PlEaDoc, PlIaDoc, PlEsDoc, PlIsDoc, DocFee, DocBill | 7个 |
| 财务管理 | 财务/ | PlInvoices, DocFeeRequisition, ActualFinancialTransaction, TaxInvoiceInfo | 10+个 |
| 基础数据 | 基础数据/ | PlCountry, PlPort, PlCurrency, PlExchangeRate, FeesType | 15+个 |
| 工作流 | 流程/ | OwWorkflow, OwWfTemplate | 2个 |
| OA办公 | OA/ | OaExpenseRequisition, VoucherSequence | 2个 |
| 应用日志 | 应用日志/ | OwAppLogStore, OwAppLogItemStore, OwAppLogView | 3个 |
| 系统支持 | 基础支持/、消息系统/ | BusinessBase, DataDicBase, OwMessage | 3个 |
| 航线管理 | 航线管理/ | ShippingLane | 1个 |
| 多语言 | 多语言/ | Multilingual | 1个 |

---

## 🗂️ 实体详细索引

### 1. 账号与权限 (账号/、权限/)

#### Account (账号.cs)
- **主键**: Guid Id
- **关键字段**: LoginName(登录名), DisplayName(显示名), OrgId(组织ID), Token(令牌), State(状态位)
- **权限控制**: IsSuperAdmin(超管), IsMerchantAdmin(商管)
- **多租户**: OrgId(所属组织)
- **关联**: AccountRole(用户角色关联表)

#### PlRole (权限/PlRole.cs)
- **主键**: Guid Id
- **关键字段**: Name_Name, Name_DisplayName, OrgId(所属组织或商户)
- **关联**: AccountRole(用户角色), RolePermission(角色权限)
- **说明**: 角色可归属于机构或商户

#### PlPermission (权限/PlPermission.cs)
- **主键**: string Name(权限码,如"D0.1.1.2")
- **关键字段**: DisplayName, ShortName, ParentId(父权限)
- **结构**: 树形结构,支持权限层级
- **关联**: RolePermission(角色权限)

---

### 2. 组织架构 (机构/)

#### PlMerchant (机构/PlMerchant.cs)
- **主键**: Guid Id
- **关键字段**: Name_Name, Name_DisplayName, Name_ShortName
- **说明**: 商户,多租户顶层单位
- **子表**: PlOrganization(下属机构)

#### PlOrganization (机构/PlOrganization.cs)
- **主键**: Guid Id
- **关键字段**: Name_Name, Name_DisplayName, OrgTypeId(机构类型), ParentId(父机构), MerchantId(所属商户)
- **结构**: 树形结构,支持多级机构
- **类型**: 公司(Company)、部门(Department)等

#### PlOrganizationParameter (机构/PlOrganizationParameter.cs)
- **主键**: string FullPath(参数路径)
- **关键字段**: Value(参数值), OrgId(所属组织)
- **说明**: 组织级参数配置

---

### 3. 客户资料 (客户资料/)

#### PlCustomer (客户资料/PlCustomer.cs)
- **主键**: Guid Id
- **关键字段**: 
  - 名称: Name_Name, Name_DisplayName, EnglishName
  - 编码: Code, CustomCode, CrideCode(税号)
  - 地址: Address_CountryId, Address_Province, Address_City, EnglishAddress
  - 联系: Contact_Tel, Contact_Fax, Contact_EMail
  - 财务: FinanceCodeAR(应收编码), FinanceCodeAP(应付编码), TacCountNo(财务编码)
  - 客户性质: IsShipper, IsBalance, IsAirway等
  - 多租户: OrgId
- **子表**: PlCustomerContact(联系人), PlTaxInfo(开票信息), PlTidan(提单), PlLoadingAddr(装货地址), PlBusinessHeader(业务负责人), CustomerBlacklist(黑名单)
- **并发控制**: RowVersion(时间戳)

#### PlCustomerContact (客户资料/PlCustomer.cs)
- **主键**: Guid Id
- **关键字段**: DisplayName, SexId, Title, Contact_Tel, Mobile
- **外键**: CustomerId

#### PlTaxInfo (客户资料/PlTaxInfo.cs)
- **主键**: Guid Id
- **关键字段**: TaxpayerName(纳税人名称), TaxpayerNumber(税号), BankName, BankAccount
- **外键**: PlCustomerId

---

### 4. 业务单据 (业务/)

#### PlJob (业务/PlJob.cs)
- **主键**: Guid Id
- **关键字段**: 
  - JobNo(工作号), JobTypeId(业务类型)
  - CustomerId(客户ID), ShipperId(发货人ID)
  - Status(状态), OrgId(所属组织)
  - EtdDate(预计发货日期), EtaDate(预计到达日期)
- **业务类型**: 空运出口(AE), 空运进口(AI), 海运出口(SE), 海运进口(SI)等
- **状态**: 0=初始,1=进行中,2=已完成,3=已结算
- **子表**: DocFee(费用)
- **接口**: ICreatorInfo, IModifyInfo

#### PlEaDoc / PlIaDoc / PlEsDoc / PlIsDoc
- **说明**: 空运出口/进口、海运出口/进口业务单
- **主键**: Guid Id
- **关键字段**: JobId(关联工作号), HBL(提单号), MBL(主单号), Status(状态)
- **具体字段**: 根据业务类型包含发货人、收货人、港口、船公司等信息

#### DocFee (业务/DocFee.cs)
- **主键**: Guid Id
- **关键字段**: 
  - JobId(工作号), BillId(账单ID)
  - FeeName(费用名称), FeeTypeId(费用种类)
  - Amount(金额), Currency(币种), ExchangeRate(汇率)
  - IsAR(应收/应付), Status(状态)
  - TotalSettledAmount(已结算金额)
- **状态**: 0=初始,1=已审核,2=已申请,3=已结算
- **多租户**: OrgId

#### DocBill (业务/DocBill.cs)
- **主键**: Guid Id
- **关键字段**: DocNo(账单号)
- **关联**: DocFee.BillId(一对多)
- **说明**: 费用账单,关联多个费用

---

### 5. 财务管理 (财务/)

#### PlInvoices (财务/PlInvoices.cs)
- **主键**: Guid Id
- **关键字段**: InvoiceNo(发票号), InvoiceDate(开票日期), TotalAmount(总金额), TaxRate(税率), OrgId
- **状态**: Status
- **子表**: PlInvoicesItem(发票明细)

#### DocFeeRequisition (财务/DocFeeRequisition.cs)
- **主键**: Guid Id
- **关键字段**: RequisitionNo(申请单号), RequisitionType(类型), TotalAmount, OrgId, MakerId(制单人), Status
- **类型**: 0=应收,1=应付
- **状态**: 0=草稿,1=审批中,2=已审批,3=已核销
- **子表**: DocFeeRequisitionItem(申请明细)

#### ActualFinancialTransaction (财务/ActualFinancialTransaction.cs)
- **主键**: Guid Id
- **关键字段**: TransactionNo(交易号), Amount(金额), TransactionDate, BankAccountId, OrgId
- **说明**: 实际财务往来,支持费用分次收付

#### TaxInvoiceInfo (财务/TaxInvoiceInfo.cs)
- **主键**: Guid Id
- **关键字段**: 
  - InvoiceSerialNum(发票流水号), InvoiceType(发票类型)
  - BuyerTitle(购方名称), SellerTaxNum(销方税号)
  - TotalAmount(总金额), TaxAmount(税额)
  - Status(状态), ApplicantId(申请人), ApplyDateTime
- **子表**: TaxInvoiceInfoItem(发票明细)
- **集成**: 诺诺电子发票

#### TaxInvoiceChannel (财务/TaxInvoiceChannel.cs)
- **说明**: 发票渠道配置(如诺诺)

#### OrgChannelAccount (财务/OrgChannelAccount.cs)
- **说明**: 组织发票渠道账号映射

#### DocFeeTemplate (财务/DocFeeTemplate.cs)
- **说明**: 费用方案模板
- **子表**: DocFeeTemplateItem(方案明细)

#### SubjectConfiguration (财务/SubjectConfiguration.cs)
- **说明**: 科目配置,用于金蝶凭证生成

#### KingdeeVoucher (财务/KingdeeVoucher.cs)
- **说明**: 金蝶凭证生成记录

#### BankAccountInfo (财务/BankAccountInfo.cs)
- **说明**: 银行账户信息

---

### 6. 基础数据 (基础数据/)

#### PlCountry (基础数据/PlCountry.cs)
- **主键**: Guid Id
- **关键字段**: Code2(二字码), Code3(三字码), Name, DisplayName, OrgId
- **说明**: 国家代码字典

#### PlPort (基础数据/PlPort.cs)
- **主键**: Guid Id
- **关键字段**: PortCode(港口代码), PortName, CountryId, OrgId
- **说明**: 港口字典

#### PlCurrency (基础数据/PlCurrency.cs)
- **主键**: Guid Id
- **关键字段**: Code(币种代码,如CNY/USD), Name, Symbol(符号,如¥/$), OrgId
- **说明**: 币种字典

#### PlExchangeRate (基础数据/PlExchangeRate.cs)
- **主键**: Guid Id
- **关键字段**: CurrencyId(币种), Rate(汇率), StartDate(有效开始), EndDate(有效结束), BusinessTypeId(业务类型), OrgId
- **说明**: 汇率表,支持按业务类型和时间查询

#### FeesType (基础数据/FeesType.cs)
- **主键**: Guid Id
- **关键字段**: Code, Name, DisplayName, IsAR(应收/应付), OrgId
- **说明**: 费用种类字典

#### SimpleDataDic (基础数据/SimpleDataDic.cs)
- **主键**: Guid Id
- **关键字段**: Code, Name, DataDicId(所属分类), CreateAccountId, OrgId, IsDelete(软删除)
- **说明**: 简单数据字典,通用键值对配置

#### DataDicCatalog (基础数据/DataDicCatalog.cs)
- **主键**: Guid Id
- **关键字段**: Code(分类代码), DisplayName, OrgId
- **说明**: 数据字典分类

#### BusinessTypeDataDic (基础数据/BusinessTypeDataDic.cs)
- **说明**: 业务类型字典(空运/海运/陆运等)

#### JobNumberRule (基础数据/JobNumberRule.cs)
- **说明**: 工作号编码规则

#### OhterNumberRule (基础数据/OhterNumberRule.cs)
- **说明**: 其他编码规则

#### ShippingContainersKind (基础数据/ShippingContainersKind.cs)
- **说明**: 箱型字典

#### UnitConversion (基础数据/UnitConversion.cs)
- **说明**: 单位换算表

#### CaptchaInfo (基础数据/CaptchaInfo.cs)
- **说明**: 验证码信息

#### DailyFeesType (基础数据/DailyFeesType.cs)
- **说明**: 日常费用类型

---

### 7. 工作流 (流程/)

#### OwWorkflow (流程/OwWorkflow.cs)
- **主键**: Guid Id
- **关键字段**: TemplateId(流程模板), DocId(关联单据), Status(状态), CurrentNodeId(当前节点)
- **说明**: 工作流实例

#### OwWfTemplate (流程/OwWfTemplate.cs)
- **主键**: Guid Id
- **关键字段**: Name, KindCode(流程类型), OrgId
- **子表**: OwWfTemplateNode(模板节点)
- **说明**: 工作流模板

---

### 8. OA办公 (OA/)

#### OaExpenseRequisition (OA/OaExpenseRequisition.cs)
- **主键**: Guid Id
- **关键字段**: RequisitionNo(申请单号), TotalAmount, OrgId, CreateBy, Status
- **子表**: OaExpenseRequisitionItem(费用明细)
- **说明**: OA日常费用申请单

#### VoucherSequence (OA/VoucherSequence.cs)
- **说明**: 凭证序号管理

---

### 9. 应用日志 (应用日志/)

#### OwAppLogStore (应用日志/OwAppLogStore.cs)
- **主键**: Guid Id(TypeId)
- **关键字段**: FormatString(格式字符串), LogLevel(日志级别)
- **说明**: 日志源定义

#### OwAppLogItemStore
- **主键**: Guid Id
- **关键字段**: ParentId(日志源ID), CreateUtc(创建时间), ParamstersJson(参数JSON), MerchantId
- **说明**: 日志项记录

#### OwAppLogView (应用日志/OwAppLogView.cs)
- **说明**: 日志视图,用于查询展示

---

### 10. 系统支持 (基础支持/)

#### BusinessBase (基础支持/BusinessBase.cs)
- **说明**: 业务实体基类

#### DataDicBase (基础支持/DataDicBase.cs)
- **说明**: 数据字典基类

#### PlAddress (基础支持/PlAddress.cs)
- **说明**: 地址复杂类型(已展开为平铺字段)

---

### 11. 航线管理 (航线管理/)

#### ShippingLane (航线管理/ShippingLane.cs)
- **主键**: Guid Id
- **关键字段**: LaneName(航线名称), DeparturePortId, ArrivalPortId, ShippingCompanyId
- **说明**: 航线管理

---

### 12. 多语言 (多语言/)

#### Multilingual (多语言/Multilingual.cs)
- **主键**: string Key
- **关键字段**: LanguageCode(语言代码), Value(翻译值)
- **说明**: 多语言翻译表

---

### 13. 消息系统 (消息系统/)

#### OwMessage (消息系统/OwMessage.cs)
- **主键**: Guid Id
- **关键字段**: Title(标题), Content(内容), FromUserId, ToUserId, Status(状态)
- **说明**: 站内消息

---

### 14. 系统资源 (系统资源/)

#### SystemResource (系统资源/SystemResource.cs)
- **主键**: string Name
- **关键字段**: Version(版本), ResourceType(资源类型)
- **说明**: 系统资源清单

---

### 15. 基础数据 (基础数据/)

#### OwSystemLog (基础数据/OwSystemLog.cs)
- **说明**: 系统日志(EF日志)

---

## 🏗️ DbContext (PowerLmsUserDbContext.cs)

### 核心配置
```csharp
public class PowerLmsUserDbContext : DbContext
{
    // DbSet属性(省略,太多了)
    public DbSet<Account> Accounts { get; set; }
    public DbSet<PlJob> PlJobs { get; set; }
    public DbSet<PlCustomer> PlCustomers { get; set; }
    // ...
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 配置实体映射、索引、关系等
        // 配置软删除全局过滤器
        // 配置级联删除策略
    }
}
```

### 关键特性
- **延迟加载**: 禁用(UseLazyLoadingProxies配置但已禁用)
- **软删除**: 全局过滤器(IsDelete=false)
- **多租户**: 所有业务实体包含OrgId字段
- **并发控制**: 部分实体使用RowVersion(Timestamp)
- **审计字段**: CreateBy, CreateDateTime, ModifyBy, ModifyDateTime
- **触发器**: DeletedTriggerHandler.cs(软删除触发器)

---

## 📊 数据库迁移 (Migrations/)

### 迁移记录
- **总计**: 150+ 个迁移文件
- **命名规则**: YYMMDD+序号(如25031801)
- **最新迁移**: 25113001(2025-11-30)
- **快照**: PowerLmsUserDbContextModelSnapshot.cs

### 迁移策略
- **禁止自动迁移**: 生产环境手动执行
- **手动迁移**: 按发布节奏创建和应用
- **回滚机制**: Down方法(但基础库无回退机制)

---

## ⚠️ 重要约束

1. **禁止使用record类型定义实体**
2. **所有业务实体必须包含OrgId字段**
3. **软删除实体必须实现ISoftDelete接口**
4. **并发控制使用RowVersion(Timestamp)**
5. **外键关系必须显式配置**
6. **索引必须合理设计(考虑查询性能)**
7. **数据库表名/字段名保持稳定(影响导入导出)**

---

**索引更新时间**: 2025-01-31
**适用版本**: PowerLms v1.0+
**数据库**: SQL Server 2016+
