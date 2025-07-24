# PowerLms �������ȸ���

## OA�ճ��������뵥ģ�鿪��״̬

### ? ����ɹ���

#### ����ģ��
- **�����ֶ�**: ��֧���͡����֡����ʡ�����������ʵ��
- **��ϸ�ֶ�**: ��š�����ʱ�䡢�����˺š��������ࡢ��Ա�������š�ƾ֤�š�ժҪ����Ʊ�š���ע
- **��˻���**: AuditDateTime��AuditOperatorId���״̬����

#### ҵ���߼�
- **����CRUD**: `OaExpenseController`�ṩ���뵥��ɾ�Ĳ�
- **��ϸ����**: `OaExpenseController.Item.cs`��ϸ�����
- **���У��**: `ValidateAmountConsistency()`ǿ��У����ϸ�ϼ����������һ��
- **Ȩ�޿���**: ����OrgId���û���ɫ�����ݸ���
- **�������**: ֧�����ͨ��/ȡ�������ɹ�����ϵͳ

#### ������ʩ����
- **�ļ�ϵͳ**: `OwFileService`�ļ��ϴ����أ�֧��Ȩ�޿���
- **������**: `OwWfManager`�������̣�״̬��������
- **ƾ֤����**: `VoucherNumberGeneratorController`�ڼ�-ƾ֤��-���
- **Ȩ�޹���**: ����PlPermission��ϸ����Ȩ�޿���

### ?? ������ɹ���

#### ���ݽṹ����
- **���Ƴ�**: ��ϸ��`Currency`��`ExchangeRate`�ֶ�
- **�����**: ���������˺�ͳһ�ֶ�
- **��ɾ��**: ����`SettlementMethod`��`BankAccountId`�����ֶ�

### ? ��ʵ�ֹ���

#### �ļ��ϴ�����
- ����׶��ϴ���Ʊ�ļ�����
- �ļ������뵥��ParentId����
- ǰ���ļ��ϴ��������

#### ��������ģ��
- ����"�ճ������տ�����"ģ������
- ����"�ճ����ø�������"ģ������
- ���̱���淶������

#### ǰ�˽����Ż�
- ����׶μ򻯽���(�ܽ��+��������+�ļ��ϴ�)
- �������׶���ϸ�������
- ���׶������û������Ż�

## ����ծ��

### ���ݿ�Ǩ��
```sql
-- ��Ҫ�ֹ�ִ�е�Ǩ��SQL
ALTER TABLE OaExpenseRequisitionItems DROP COLUMN Currency;
ALTER TABLE OaExpenseRequisitionItems DROP COLUMN ExchangeRate;
ALTER TABLE OaExpenseRequisitions DROP COLUMN SettlementMethod;
ALTER TABLE OaExpenseRequisitions DROP COLUMN BankAccountId;
ALTER TABLE OaExpenseRequisitions ADD SettlementAccountId uniqueidentifier;
```

### �ع�����
- ��ϸ���ֶ����Ʊ�׼��(ExpenseDate �� SettlementDateTime)
- ͳһ���������־��¼�淶
- API����ֵ�ṹ�Ż�

## ��ƾ��߼�¼

### ���У�����
- **ǿ��У��**: ���ʱ����ͨ�����һ���Լ��
- **�ݲ����**: ����0.01�ĸ��������
- **�û�����**: �ṩ��ϸ�Ĵ�����ʾ��Ϣ

### ���������ɷ���
- **��������**: �������OwWf���������
- **��������**: ͨ������ģ������ʵ��ҵ������
- **״̬ͳһ**: ʹ�ñ�׼�Ĺ�����״̬����

### �ļ��������
- **Ȩ�޼̳�**: �ļ�Ȩ�޼̳����뵥��ҵ��Ȩ��
- **�洢�Ż�**: ����OwFileService���ļ��洢����
- **�汾����**: ֧���ļ��汾�������ʷ׷��

---
*����ʱ��: 2025-01-XX*