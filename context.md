# PowerLms �������ϵͳ - �����������ĵ�

## ?? ��Ŀ����

### ������Ϣ
- **��Ŀ����**: PowerLms �������ϵͳ
- **�������**: .NET 6
- **��������**: C# 10
- **�����ռ�**: `C:\Users\zc-home\source\ourworldcn\PowerLms\`

### ���ļ���ջ�ܹ�
```
������ .NET 6                          # �����ƽ̨
������ Entity Framework Core           # ORM���ݷ��ʲ�
������ DotNetDBF                      # DBF�ļ�����⣨���ƾ֤������
������ AutoMapper                     # ����ӳ��
������ Microsoft.Extensions.DI        # ����ע������
������ ASP.NET Core Web API           # RESTful API���
������ OwTaskService                  # �첽���������
```

## ??? ��Ŀ�ṹ

### ����������
```
PowerLms/
������ PowerLmsData/                  # ����ģ�Ͳ�
��   ������ �ͻ�����/                  # �ͻ����ʵ�� (PlCustomer)
��   ������ ����/                     # ��֯����ʵ�� (PlOrganization, BankInfo)
��   ������ ����/                     # �������ʵ�� (SubjectConfiguration, KingdeeVoucher)
��   ������ ��������/                  # ���������ֵ�
��   ������ ҵ��/                     # ҵ�񵥾�ʵ�� (DocFee, TaxInvoiceInfo)
������ PowerLmsServer/                # ҵ���߼���
��   ������ Managers/                 # ҵ������� (OrgManager, AccountManager)
��   ������ EfData/                   # EF����������
��   ������ Services/                 # ҵ�����
������ PowerLmsWebApi/                # Web API��
��   ������ Controllers/              # API������
��   ��   ������ FinancialSystemExportController.cs        # ��������
��   ��   ������ FinancialSystemExportController.Arab.cs   # ARAB�ֲ���
��   ��   ������ FinancialSystemExportController.Apab.cs   # APAB�ֲ���
��   ��   ������ FinancialSystemExportController.Dto.cs    # DTO����
��   ��   ������ SubjectConfigurationController.cs         # �����Ŀ���ÿ�����
��   ������ Dto/                      # ���ݴ������
������ Bak/                          # �������
    ������ OwDbBase/                 # ���ݿ������� (OwTaskService)
    ������ OwBaseCore/               # ���Ļ������
```

## ?? ����ҵ��ģ��

### 1. �����Ŀ����ϵͳ (SubjectConfiguration)

#### ʵ��ṹ
```csharp
public class SubjectConfiguration : GuidKeyObjectBase, ISpecificOrg, IMarkDelete, ICreatorInfo
{
    public Guid? OrgId { get; set; }                    // ������֯����Id
    public string Code { get; set; }                    // ��Ŀ���� [MaxLength(32), Unicode(false)]
    public string SubjectNumber { get; set; }           // ��ƿ�Ŀ��� [Required]
    public string DisplayName { get; set; }             // ��ʾ���� [MaxLength(128)]
    public string VoucherGroup { get; set; }            // ƾ֤����� [MaxLength(10)] ?�����ֶ�
    public string AccountingCategory { get; set; }      // ������� [MaxLength(50)] ?�����ֶ�
    public string Preparer { get; set; }                // �Ƶ��ˣ�����Ƶ������ƣ�[MaxLength(64)] ?�����ֶ�
    public string Remark { get; set; }                  // ��ע
    public bool IsDelete { get; set; }                  // ��ɾ�����
    public Guid? CreateBy { get; set; }                 // ������ID
    public DateTime CreateDateTime { get; set; }        // ����ʱ��
}
```

#### ��Ŀ����淶��ϵ
```
ͨ�ÿ�Ŀ (GEN):
- GEN_PREPARER          # �Ƶ��ˣ�����Ƶ������ƣ�
- GEN_VOUCHER_GROUP     # ƾ֤����֣��磺ת���ա������ǣ�

��Ʊ���˿�Ŀ (PBI):
- PBI_ACC_RECEIVABLE    # Ӧ���˿�
- PBI_SALES_REVENUE     # ��Ӫҵ������
- PBI_TAX_PAYABLE       # Ӧ��˰��

ʵ�տ�Ŀ (RF):
- RF_BANK_DEPOSIT       # ���д��տ����д�
- RF_ACC_RECEIVABLE     # Ӧ���˿����Ӧ�գ�

ʵ����Ŀ (PF):
- PF_BANK_DEPOSIT       # ���д��������д�
- PF_ACC_PAYABLE        # Ӧ���˿�

A��Ӧ�ռ����Ŀ (ARAB):
- ARAB_TOTAL           # ������Ӧ�� (531)
- ARAB_IN_CUS          # ����Ӧ�չ���-�ͻ� (113.001.01)
- ARAB_IN_TAR          # ����Ӧ�չ���-��˰ (113.001.02)
- ARAB_OUT_CUS         # ����Ӧ�չ���-�ͻ� (113.002)
- ARAB_OUT_TAR         # ����Ӧ�չ���-��˰ (������)

A��Ӧ�������Ŀ (APAB):
- APAB_TOTAL           # ������Ӧ�� (532)
- APAB_IN_SUP          # ����Ӧ������-��Ӧ�� (203.001.01)
- APAB_IN_TAR          # ����Ӧ������-��˰ (203.001.02)
- APAB_OUT_SUP         # ����Ӧ������-��Ӧ�� (203.002)
- APAB_OUT_TAR         # ����Ӧ������-��˰ (������)
```

### 2. �������ϵͳ����ģ��

#### 2.1 ����ƾ֤��������ܹ�

**�ֲ������ģʽ:**
- `FinancialSystemExportController.cs` - ������������������ע���ͨ�����ԣ�
- `FinancialSystemExportController.Arab.cs` - ARABģ�飨����A��Ӧ�գ�
- `FinancialSystemExportController.Apab.cs` - APABģ�飨����A��Ӧ����
- `FinancialSystemExportController.Dto.cs` - DTO����

#### 2.2 ƾ֤����������ϵ
```
����ƾ֤����:
������ ��Ʊ���ˣ�B�ˣ�- PBI
��   ������ Ӧ���˿� (�跽) - ��˰�ϼ�
��   ������ ��Ӫҵ������ (����) - �۶�
��   ������ Ӧ��˰�� (����) - ˰��
������ ʵ�� - RF
��   ������ ���д�� (�跽) - �����ܶ�
��   ������ Ӧ���˿� (����) - �����ܶ�
������ ʵ�� - PF
��   ������ Ӧ���˿� (�跽) - ������
��   ������ ���д�� (����) - ������
������ ����A��Ӧ�ձ�λ�ҹ��� - ARAB
��   ������ Ӧ���˿���ϸ (�跽) - ���ͻ�/����/�������
��   ������ ������Ӧ�� (����) - Sum(Totalamount)
������ ����A��Ӧ����λ�ҹ��� - APAB
    ������ Ӧ���˿���ϸ (�跽) - ����Ӧ��/����/�������
    ������ ������Ӧ�� (����) - Sum(Totalamount)
```

#### 2.3 ����ҵ���߼�

**ARAB������A��Ӧ�գ�ҵ�����:**
- ����Դ: `DocFees` where `IO == true` (����)
- ��������: ����.���㵥λ + ���㵥λ.������ + ��������.����
- ������: `sum(Amount * ExchangeRate)`
- ƾ֤�ṹ: ��ϸ��¼���跽�� + �ܼƷ�¼��������
- �������: "�ͻ�"

**APAB������A��Ӧ����ҵ�����:**
- ����Դ: `DocFees` where `IO == false` (֧��)
- ��������: ����.���㵥λ + ���㵥λ.������ + ��������.����
- ������: `sum(Amount * ExchangeRate)`
- ƾ֤�ṹ: ��ϸ��¼���跽�� + �ܼƷ�¼��������
- �������: "��Ӧ��"

#### 2.4 DBF�ļ������淶

**���ƾ֤�ֶ�ӳ��:**
```
// �����ֶ�
FDATE/FTRANSDATE    # ƾ֤����
FPERIOD             # ����ڼ�
FNUM                # ƾ֤��
FENTRYID            # ��¼���
FEXP                # ժҪ
FACCTID             # ��Ŀ����
FCLSNAME1           # ������𣨿ͻ�/��Ӧ�̣�
FOBJID1             # �ͻ�/��Ӧ�̼��
FOBJNAME1           # �ͻ�/��Ӧ������
FTRANSID            # �������
FDC                 # ������� (0=�跽, 1=����)
FDEBIT/FCREDIT      # �跽/�������
FPREPARE            # �Ƶ���
```

**�첽���������:**
- ʹ�� `OwTaskService` ͳһ�������
- ֧��������ȸ��ٺ�״̬��ѯ
- �ֲ�����������־��¼
- �ļ�������ɺ��Զ����浽 `FinancialExports` Ŀ¼

### 3. �ͻ����Ϲ���ϵͳ (PlCustomer)

#### �������Է���
```csharp
public class PlCustomer : GuidKeyObjectBase, ICreatorInfo
{
    // ������Ϣ
    public Guid? OrgId { get; set; }                    // ������֯����Id
    public string Name_DisplayName { get; set; }        // ��ʾ��
    public string Name_ShortName { get; set; }          // ��ʽ���
    public string TacCountNo { get; set; }              // �������
    
    // �ͻ����ʱ�ʶ
    public bool IsBalance { get; set; }                 // �Ƿ���㵥λ
    
    // �������ʶ�����ڲ���ƾ֤�����
    public bool? IsDomestic { get; set; }               // true=���ڣ�false=����
}
```
### 4. Ȩ���밲ȫ��ϵ

#### 4.1 Ȩ����֤����
```csharp
// Token��֤
if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
    return Unauthorized();

// ��֯Ȩ�޹���
- ��������Ա: ������������
- �̻�����Ա: ���ʱ��̻���������
- ��ͨ�û�: ������������˾��������������
```

#### 4.2 ����Ȩ�޿���
```csharp
// ��̬Ȩ�޹��˷���
private static IQueryable<DocFee> ApplyOrganizationFilterForFeesStatic(
    IQueryable<DocFee> feesQuery, Account user, 
    PowerLmsUserDbContext dbContext, IServiceProvider serviceProvider)
```

## ?? ����ʵ��ϸ��

### 1. �첽������

#### OwTaskService ����
```csharp
// ���񴴽�
var taskId = taskService.CreateTask(
    typeof(FinancialSystemExportController),
    nameof(ProcessArabDbfExportTask),
    taskParameters,
    context.User.Id,
    context.User.OrgId);

// ��̬��������
public static object ProcessArabDbfExportTask(
    Guid taskId, 
    Dictionary<string, string> parameters, 
    IServiceProvider serviceProvider)
```

### 2. �������ݴ���

#### ARAB�������ݽṹ
```csharp
public class ArabGroupDataItem
{
    public Guid? BalanceId { get; set; }            // ���㵥λID
    public string CustomerName { get; set; }        // �ͻ�����
    public string CustomerShortName { get; set; }   // �ͻ����
    public string CustomerFinanceCode { get; set; } // �ͻ��������
    public bool IsDomestic { get; set; }            // �Ƿ����
    public bool IsAdvance { get; set; }             // �Ƿ����
    public decimal TotalAmount { get; set; }        // �ܽ��
}
```

#### APAB�������ݽṹ
```csharp
public class ApabGroupDataItem
{
    public Guid? BalanceId { get; set; }             // ���㵥λID
    public string SupplierName { get; set; }         // ��Ӧ������
    public string SupplierShortName { get; set; }    // ��Ӧ�̼��
    public string SupplierFinanceCode { get; set; }  // ��Ӧ�̲������
    public bool IsDomestic { get; set; }             // �Ƿ����
    public bool IsAdvance { get; set; }              // �Ƿ����
    public decimal TotalAmount { get; set; }         // �ܽ��
}
```

### 3. ��Ŀ���ü���

#### ������֤����
```csharp
// ARAB��Ŀ����Ҫ��
var requiredCodes = new List<string>
{
    "ARAB_TOTAL",      // ������Ӧ��
    "ARAB_IN_CUS",     // ����Ӧ�չ���-�ͻ�
    "ARAB_IN_TAR",     // ����Ӧ�չ���-��˰
    "ARAB_OUT_CUS",    // ����Ӧ�չ���-�ͻ�
    "ARAB_OUT_TAR",    // ����Ӧ�չ���-��˰
    "GEN_PREPARER",    // �Ƶ���
    "GEN_VOUCHER_GROUP" // ƾ֤�����
};

// APAB��Ŀ����Ҫ��
var requiredCodes = new List<string>
{
    "APAB_TOTAL",      // ������Ӧ��
    "APAB_IN_SUP",     // ����Ӧ������-��Ӧ��
    "APAB_IN_TAR",     // ����Ӧ������-��˰
    "APAB_OUT_SUP",    // ����Ӧ������-��Ӧ��
    "APAB_OUT_TAR",    // ����Ӧ������-��˰
    "GEN_PREPARER",    // �Ƶ���
    "GEN_VOUCHER_GROUP" // ƾ֤�����
};
```

### 4. ƾ֤�����㷨

#### ��Ŀѡ���߼�
```csharp
// ARAB/APAB��Ŀѡ��
if (group.IsDomestic)  // ����
{
    if (group.IsAdvance)  // ����
        subjectCode = "ARAB_IN_TAR";  // �� "APAB_IN_TAR"
    else
        subjectCode = "ARAB_IN_CUS";  // �� "APAB_IN_SUP"
}
else  // ����
{
    if (group.IsAdvance)  // ����
        subjectCode = "ARAB_OUT_TAR";  // �� "APAB_OUT_TAR"
    else
        subjectCode = "ARAB_OUT_CUS";  // �� "APAB_OUT_SUP"
}
```

#### ժҪ���ɹ淶
```csharp
// ARABժҪ��ʽ
description = $"����Ӧ�չ���-�ͻ�-{group.CustomerName} {group.TotalAmount:F2}Ԫ";

// APABժҪ��ʽ
description = $"����Ӧ������-��Ӧ��-{group.SupplierName} {group.TotalAmount:F2}Ԫ";

// �ܼƷ�¼ժҪ
description = $"����{accountingDate:yyyy��MM��}��Ӧ�� {totalAmount:F2}Ԫ";
```

## ?? API�ӿڹ淶

### 1. ARAB�����ӿ�
```http
POST /FinancialSystemExport/ExportArabToDbf
Content-Type: application/json

{
  "Token": "�û���������",
  "ExportConditions": {
    "StartDate": "2025-01-01",
    "EndDate": "2025-01-31",
    "AccountingDate": "2025-01-31"
  },
  "DisplayName": "�Զ����ļ���ʾ����",
  "Remark": "�Զ����ļ���ע"
}
```

### 2. APAB�����ӿ�
```http
POST /FinancialSystemExport/ExportApabToDbf
Content-Type: application/json

{
  "Token": "�û���������",
  "ExportConditions": {
    "StartDate": "2025-01-01",
    "EndDate": "2025-01-31",
    "AccountingDate": "2025-01-31"
  },
  "DisplayName": "�Զ����ļ���ʾ����",
  "Remark": "�Զ����ļ���ע"
}
```

### 3. ���ؽ����ʽ
```json
{
  "TaskId": "����Ψһ��ʶID",
  "Message": "���񴴽��ɹ�����ʾ��Ϣ",
  "ExpectedFeeCount": 100,
  "HasError": false,
  "ErrorCode": 0,
  "DebugMessage": "�����ɹ�"
}
```

## ?? ��̹淶����

### C# �����׼
```csharp
// 1. .NET 6 �� C# 10 �﷨����
using System;  // ȫ��using���

// 2. ����ע����β���
public string Code { get; set; }  // ��Ŀ����
public string DisplayName { get; set; }  // ��ʾ����

// 3. #region ������֯
#region HTTP�ӿ� - ARAB(����A��Ӧ�ձ�λ�ҹ���)
#endregion

#region ��̬�������� - ARAB
#endregion

// 4. ��ϸXML�ĵ�ע��
/// <summary>
/// ����A��Ӧ�ձ�λ�ҹ���(ARAB)����Ϊ���DBF��ʽ�ļ���
/// </summary>
[HttpPost]
public ActionResult<ExportArabToDbfReturnDto> ExportArabToDbf(ExportArabToDbfParamsDto model)
```

### ���������
```csharp
// �ֲ��������
string currentStep = "������֤";
try
{
    currentStep = "������������";
    // ҵ���߼�...
    
    currentStep = "�������ݿ�������";
    // ҵ���߼�...
}
catch (Exception ex)
{
    var contextualError = $"ARAB DBF��������ʧ�ܣ���ǰ����: {currentStep}, ����ID: {taskId}";
    throw new InvalidOperationException(contextualError, ex);
}
```

### ��Դ����ģʽ
```csharp
// �ڴ�����ȫ����
var memoryStream = new MemoryStream(1024 * 1024 * 1024);
try
{
    DotNetDbfUtil.WriteToStream(kingdeeVouchers, memoryStream, kingdeeFieldMappings, customFieldTypes);
    // �ļ������߼�...
}
finally
{
    OwHelper.DisposeAndRelease(ref memoryStream);
}
```

## ??? ���ݿ����ԭ��

### Ψһ����Լ��
```csharp
[Index(nameof(OrgId), nameof(Code), IsUnique = true)]
public class SubjectConfiguration
```

### �ֶ�ע��������
```csharp
[Comment("��Ŀ����")]
[MaxLength(32), Unicode(false)]
[Required(AllowEmptyStrings = false)]
public string Code { get; set; }
```

### ��ɾ���ӿ�
```csharp
public class SubjectConfiguration : IMarkDelete
{
    public bool IsDelete { get; set; }  // ��ɾ�����
}
```

## ? �����Ż�����

### ���ݿ��ѯ�Ż�
```csharp
// ����ͳ�Ʋ�ѯ�Ż�
var arabGroupData = (from fee in feesQuery
                   join customer in dbContext.PlCustomers on fee.BalanceId equals customer.Id into customerGroup
                   from cust in customerGroup.DefaultIfEmpty()
                   join feeType in dbContext.DD_SimpleDataDics on fee.FeeTypeId equals feeType.Id into feeTypeGroup
                   from feeTypeDict in feeTypeGroup.DefaultIfEmpty()
                   group new { fee, cust, feeTypeDict } by new
                   {
                       BalanceId = fee.BalanceId,
                       CustomerName = cust != null ? cust.Name_DisplayName : "δ֪�ͻ�",
                       IsDomestic = cust != null ? (cust.IsDomestic ?? true) : true,
                       IsAdvance = feeTypeDict != null && feeTypeDict.Remark != null && feeTypeDict.Remark.Contains("����")
                   } into g
                   select new ArabGroupDataItem
                   {
                       TotalAmount = g.Sum(x => x.fee.Amount * x.fee.ExchangeRate)
                   }).ToList();
```

### ���ļ������Ż�
```csharp
// ʹ�ô����ڴ���������ʱ�ļ�
var memoryStream = new MemoryStream(1024 * 1024 * 1024);
```

## ?? ��Ҫ�������߼�¼

### 1. �ֲ���ܹ�����
- **����**: ���÷ֲ���ģʽ��֯���񵼳�����
- **ԭ��**: ���ִ�����֯�ԣ�ÿ�������������ά��
- **Ӱ��**: �����Ŷ�Э�������͹�����չ

### 2. �첽���������
- **����**: ʹ��OwTaskServiceͳһ�������
- **ԭ��**: ���������ݴ�����Ҫ�첽ִ�У�����HTTP��ʱ
- **Ӱ��**: �����û����飬֧��������ȸ���

### 3. DBF�ļ���ʽ����
- **����**: ����DotNetDBF�����ɽ��DBF��ʽ�ļ�
- **ԭ��**: ��������ϵͳ��������
- **Ӱ��**: ȷ����������׼ȷ����

### 4. Ȩ�޿��ƾ���
- **����**: ����Ȩ����ϵ����������Ա/�̻�����Ա/��ͨ�û���
- **ԭ��**: �������֯���������ݰ�ȫҪ��
- **Ӱ��**: ȷ�����ݷ���Ȩ�޵ĺϹ���

## ?? ���������

### ��־��¼�淶
```csharp
// ��Ϣ��־
_Logger.LogInformation("�û� {UserId} ������ARAB��������", context.User.Id);

// ������־
_Logger.LogError(ex, "ARAB DBF��������ʧ�ܣ�����ID: {TaskId}", taskId);
```

### ��֤����
```csharp
// �ļ�������֤
if (fileSize == 0)
    throw new InvalidOperationException("DBF�ļ�����ʧ�ܣ��ļ�Ϊ��");

// ������������֤
if (!subjectConfigs.Any())
    throw new InvalidOperationException("ARAB��Ŀ����δ�ҵ����޷�����ƾ֤");
```

## ?? ��Ҫ�޸���¼ (��ǰ�Ự���۵ĺ�������)

### 1. SubjectConfiguration�����ֶ������޸�

#### ��������
�û���ӳ"�������������ֶ�Modifyʱ����ɹ�������û����"���漰SubjectConfigurationʵ������������ֶΣ�
- `VoucherGroup` - ƾ֤����֣���Ǩ�� 20250703083639_25070301.cs ����ӣ�
- `AccountingCategory` - ���������Ǩ�� 20250703083639_25070301.cs ����ӣ�  
- `Preparer` - �Ƶ��ˣ���Ǩ�� 20250715072810_25071501.cs ����ӣ�

#### �޸�����
**ԭ�����**: SubjectConfigurationController��ModifySubjectConfiguration������ȱ�ٶ������������ֶεĸ����߼���

**�޸�����**: ʹ�ù��������еİ�ȫģʽ�ع����ķ�����

#### ��ȫ�Ŀ�����ģʽ (�ο�����������)

**���ӷ���ģʽ (�ο�PlJobController)**:
```csharp
// ֱ��ʹ�ô����ʵ�壬����ϵͳ�����ֶ�
var entity = model.Item;
entity.GenerateNewId();
entity.CreateBy = context.User.Id;
entity.CreateDateTime = OwHelper.WorldNow;
entity.IsDelete = false;
```

**�޸ķ���ģʽ (�ο�AdminController)**:
```csharp
// ʹ��EntityManager.ModifyWithMarkDelete
if (!_EntityManager.ModifyWithMarkDelete(itemsToUpdate))
{
    var errorMsg = OwHelper.GetLastErrorMessage();
    return BadRequest($"�޸Ĳ����Ŀ����ʧ�ܣ�{errorMsg}");
}

// �ֶ������ؼ��ֶ�
foreach (var item in itemsToUpdate)
{
    var entry = _DbContext.Entry(item);
    entry.Property(c => c.OrgId).IsModified = false;
    entry.Property(c => c.CreateBy).IsModified = false;
    entry.Property(c => c.CreateDateTime).IsModified = false;
}
```

#### �ؼ��ܱ����ֶζ���
```csharp
private static readonly string[] ProtectedFields = new[]
{
    nameof(SubjectConfiguration.Id),           // ����ID�����ɸ���
    nameof(SubjectConfiguration.OrgId),        // ��֯����ID����ҪȨ�޿���
    nameof(SubjectConfiguration.CreateBy),     // ������ID��ϵͳ����
    nameof(SubjectConfiguration.CreateDateTime), // ����ʱ�䣬ϵͳ����
    nameof(SubjectConfiguration.IsDelete)      // ɾ����ǣ�ϵͳ����
};
```

### 2. VS2022 17.14.9 ���ܸĽ�����

#### �۲�����
�����߷�ӳVS2022 17.14.9���º�༭�ļ��ٶȱ��

#### ��������ԭ�����
**��� .NET 6 ��Ŀ���Ż�**:
- **C# 10���Է����Ż�**: ����Ч����ȫ��using���ļ���Χ�����ռ䡢��¼���͵�����
- **������Ŀ�ṹ�Ż�**: PowerLms��5����Ҫ��Ŀ�����ܹ����������
- **IntelliSense��Ӧ�ٶ�**: ʵ���ܡ�����ע�롢AutoMapper���ɵ����ܸ�֪����
- **��������޸�����**: ʵʱ������ʹ����������ִ�и���Ч

### 3. EntityManager����ʹ�ð�ȫ�Է���

#### ����ʶ��
ͨ����������`CopyIgnoreCase`��`ModifyWithMarkDelete`�ڹ�������ʹ�ý��٣����ܴ���Ǳ�ڷ��գ�

**ModifyWithMarkDelete����**:
- ����Լ�������ϸ���Ҫ`IEntityWithSingleKey<Guid>`�ӿ�
- �����������ڲ�����`Modify`��������AutoMapper����

**CopyIgnoreCase����**:
- AutoMapper����������ӳ������������
- �쳣����������δ�����쳣

#### �������
���ù���������֤�İ�ȫģʽ������ʹ�ò�ȷ����EntityManager�����������ֶ����Ը��ƺͱ�׼��EF Core������

## ?? ���ʵ���ܽ�

1. **ģ�黯���**: �ֲ�����֯���Ϳ���������
2. **�첽����**: ��Ҫҵ�����ʹ���첽����
3. **Ȩ�޿���**: �ϸ�����ݷ���Ȩ����֤
4. **������**: �ֲ������ϸ����λ
5. **��Դ����**: ��ʱ�ͷ��ڴ���ļ���Դ
6. **��־��¼**: �����Ĳ�����־���쳣��¼
7. **������֤**: ��Ŀ���ú�ҵ�����������Լ��
8. **�����Ż�**: ��������Ͳ�ѯ�Ż�
9. **?? ��ȫģʽ**: �ο����п�����ģʽ������ʹ�ò�ȷ���ķ���
10. **?? ����һ����**: �����빤����������������ͬ�ı�̷���ģʽ

## ?? ���������빤��

### �������߰汾
- **Visual Studio 2022 17.14.9**: �༭�����Ż����ر��Ƕ�.NET 6������Ŀ
- **Entity Framework Core**: ORM���ݷ��ʲ�
- **AutoMapper**: ����ӳ�䣨����ʹ�ø������ã�

### ����������֤
- **����ʱ���**: ����C# 10���Ͱ�ȫ����
- **��̬����**: VS2022���ô����������Ч
- **����ָ�**: ����Ĵ���״̬�ָ�����

---

**�ĵ��汾**: v4.0  
**������**: 2025-01-16  
**���÷�Χ**: PowerLms�������ϵͳ�����Ŷ�  
**ά������**: �����ܹ���  
**��Ҫ����**: 
- ����SubjectConfigurationʵ�������ֶ��޸���¼
- ����VS2022�����Ż�����
- ����EntityManager��ȫʹ��ָ��
- ������������ȫģʽ���ʵ��
- ���ƿ��������͹��߰汾��Ϣ