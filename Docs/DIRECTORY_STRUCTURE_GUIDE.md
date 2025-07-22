# PowerLms Ŀ¼�ṹ�Ż�����

���ڶ�����ǰ��Ŀ�ṹ�ķ������������.NET��׼���������������Ż����飺

## ? **��ǰ�ṹ�ŵ�**

### 1. �����ķֲ�ܹ�
- **PowerLmsData**: ���ݲ���ƺ���
- **PowerLmsServer**: ҵ���߼���ְ����ȷ  
- **PowerLmsWebApi**: API����֯����
- **Docs**: �ĵ��ṹרҵ

### 2. ���õ���֯ģʽ
- ��ҵ��ģ�����
- �ֲ������ʹ��
- DTO�����������

## ?? **������Ż���**

### 1. �ļ�������Ӣ�Ļ�
```diff
PowerLmsData/
- ������ ��������/
- ������ ����/
- ������ �ͻ�����/
- ������ ҵ��/
- ������ Ȩ��/
- ������ ����/
- ������ ��Ϣϵͳ/
- ������ Ӧ����־/
- ������ ����/
- ������ ������/
- ������ ���߹���/
- ������ ����֧��/
- ������ �˺�/
- ������ ϵͳ��Դ/
+ ������ BaseData/           # ��������
+ ������ Finance/            # ����
+ ������ Customer/           # �ͻ�����
+ ������ Business/           # ҵ��
+ ������ Authorization/      # Ȩ��
+ ������ Organization/       # ����
+ ������ Messaging/          # ��Ϣϵͳ
+ ������ Logging/            # Ӧ����־
+ ������ Workflow/           # ����
+ ������ Localization/       # ������
+ ������ ShippingRoute/      # ���߹���
+ ������ Infrastructure/     # ����֧��
+ ������ Identity/           # �˺�
+ ������ Resources/          # ϵͳ��Դ
+ ������ OfficeAutomation/   # OA�칫�Զ���
```

### 2. ��ӱ�׼.NET��Ŀ�ļ���
```
PowerLmsWebApi/
������ Controllers/
������ Dto/
������ Middleware/
������ AutoMapper/
+ ������ Extensions/         # ��չ����
+ ������ Constants/          # ��������
+ ������ Attributes/         # �Զ�������
+ ������ Filters/            # ������
+ ������ Validators/         # ��֤��
```

```
PowerLmsServer/
+ ������ Services/           # ҵ�����
+ ������ Interfaces/         # ����ӿ�
+ ������ Extensions/         # ��չ����
+ ������ Constants/          # ����
+ ������ Exceptions/         # �Զ����쳣
+ ������ Utilities/          # ������
```

### 3. ϸ��ҵ��ģ��ṹ
```
PowerLmsData/
������ BaseData/
��   ������ Geography/        # �������(�ۿڡ����ҵ�)
��   ������ Dictionary/       # �����ֵ�
��   ������ Configuration/    # �������
��   ������ Reference/        # �ο�����
������ Finance/
��   ������ Accounting/       # ��ƿ�Ŀ
��   ������ Invoice/          # ��Ʊ����
��   ������ Settlement/       # �������
��   ������ Voucher/          # ƾ֤���
������ Business/
��   ������ Jobs/             # ��������
��   ������ Documents/        # ҵ�񵥾�
��   ������ Fees/             # ���ù���
��   ������ Templates/        # ģ�����
```

### 4. ͳһ�����ռ�
```csharp
// ����������ռ�ṹ
PowerLms.Data.BaseData
PowerLms.Data.Finance  
PowerLms.Data.Business
PowerLms.Server.Services
PowerLms.Server.Interfaces
PowerLms.WebApi.Controllers
PowerLms.WebApi.Dto
```

## ?? **ʵʩ���ȼ�**

### �����ȼ� (����ʵʩ)
1. ? �ĵ��ṹ������
2. ?? ���ȱʧ�ı�׼�ļ���
3. ?? ͳһ�����淶

### �����ȼ� (��ʵʩ)  
1. ?? �����ļ���Ӣ�Ļ�
2. ?? ϸ��ҵ��ģ��
3. ??? �Ż������ռ�

### �����ȼ� (���ڹ滮)
1. ?? �ع����Ϳ�����
2. ?? ���Ƶ�Ԫ���Խṹ
3. ?? �����Ż���֯

## ?? **���ϵ�.NET��׼����**

? **����ʵ��**:
- �����ķֲ�ܹ�
- ְ�����ԭ��
- ģ�黯���
- �ĵ�������

? **��׼Ŀ¼�ṹ**:
- Controllers, Models, Services ����
- ����ע��ʹ��
- �м����֯
- AutoMapper ����

## ?? **�ܽ�**

������Ŀ�ṹ**�����Ϸ���.NET��׼����**����Ҫ�������ڣ�
- ������ҵ��ģ�黮��
- ����ķֲ�ܹ�
- ���õ��ĵ���֯

�������Ƚ���ļ�����������ӱ�׼Ŀ¼������������Ŀ����רҵ�͹��ʻ���

---

*���������.NET 6��׼ʵ������ҵ����Ŀ�����ƶ�*