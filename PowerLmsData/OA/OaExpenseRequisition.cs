using Microsoft.EntityFrameworkCore;
using OW.Data;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace PowerLms.Data.OA
{
    /// <summary>
    /// ��֧����ö�١�
    /// </summary>
    public enum IncomeExpenseType : byte
    {
        /// <summary>
        /// �տ
        /// </summary>
        Income = 0,

        /// <summary>
        /// ���
        /// </summary>
        Expense = 1
    }

    /// <summary>
    /// OA�ճ��������뵥����
    /// ���ڴ����ճ����ã�����á�����������������Ӫҵ��ķ������롣
    /// </summary>
    [Comment("OA�ճ��������뵥����")]
    [Index(nameof(OrgId), IsUnique = false)]
    public class OaExpenseRequisition : GuidKeyObjectBase, ISpecificOrg, ICreatorInfo
    {
        /// <summary>
        /// ���캯����
        /// </summary>
        public OaExpenseRequisition()
        {
            CreateDateTime = OwHelper.WorldNow;
            CurrencyCode = "CNY";
            ExchangeRate = 1.0000m;
        }

        /// <summary>
        /// ��������Id���õ��ݹ����Ļ���Id,���Ǽ��˵�ǰ��¼�Ļ���Id������ʱȷ���������޸ġ�
        /// </summary>
        [Comment("��������Id")]
        public Guid? OrgId { get; set; }

        /// <summary>
        /// ������Id��Ա���˺�Id��
        /// </summary>
        [Comment("������Id��Ա���˺�Id")]
        public Guid? ApplicantId { get; set; }

        /// <summary>
        /// ����ʱ�䡣
        /// </summary>
        [Comment("����ʱ��")]
        [Precision(3)]
        public DateTime? ApplyDateTime { get; set; }

        /// <summary>
        /// �Ƿ��true��ʾ������룬false��ʾ�������롣
        /// </summary>
        [Comment("�Ƿ��true��ʾ������룬false��ʾ��������")]
        public bool IsLoan { get; set; }

        /// <summary>
        /// �Ƿ�������������Ϊ���ڽ�������������������
        /// </summary>
        [Comment("�Ƿ�������������Ϊ���ڽ�����������������")]
        public bool IsImportFinancialSoftware { get; set; }

        /// <summary>
        /// ��ؿͻ����ַ������ɣ����ôӿͻ�������ѡ��
        /// </summary>
        [Comment("��ؿͻ����ַ������ɣ����ôӿͻ�������ѡ��")]
        [MaxLength(128)]
        public string RelatedCustomer { get; set; }

        /// <summary>
        /// �տ����С�
        /// </summary>
        [Comment("�տ�����")]
        [MaxLength(128)]
        public string ReceivingBank { get; set; }

        /// <summary>
        /// �տ����
        /// </summary>
        [Comment("�տ��")]
        [MaxLength(128)]
        public string ReceivingAccountName { get; set; }

        /// <summary>
        /// �տ��˻���
        /// </summary>
        [Comment("�տ��˻�")]
        [MaxLength(64)]
        public string ReceivingAccountNumber { get; set; }

        /// <summary>
        /// ��̸������ı���
        /// </summary>
        [Comment("��̸������ı�")]
        public string DiscussedMatters { get; set; }

        /// <summary>
        /// ��ע�����ı���
        /// </summary>
        [Comment("��ע�����ı�")]
        public string Remark { get; set; }

        /// <summary>
        /// ��֧���͡��տ�/���
        /// </summary>
        [Comment("��֧���ͣ��տ�/����")]
        public IncomeExpenseType? IncomeExpenseType { get; set; }

        /// <summary>
        /// �������ࡣѡ���ճ��������࣬����ķ���������������д����������Ŀ���롣
        /// </summary>
        [Comment("�������࣬ѡ���ճ��������࣬����ķ���������������д����������Ŀ����")]
        [MaxLength(128)]
        public string ExpenseCategory { get; set; }

        /// <summary>
        /// ���ִ��롣����code��
        /// </summary>
        [Comment("���ִ���")]
        [MaxLength(4)]
        [Unicode(false)]
        public string CurrencyCode { get; set; } = "CNY";

        /// <summary>
        /// ���ʡ���λС����
        /// </summary>
        [Comment("���ʣ���λС��")]
        [Precision(18, 4)]
        public decimal ExchangeRate { get; set; } = 1.0000m;

        /// <summary>
        /// ����λС����
        /// </summary>
        [Comment("����λС��")]
        [Precision(18, 2)]
        public decimal Amount { get; set; }

        /// <summary>
        /// ���ʱ�䡣Ϊ�����ʾδ��ˡ����ͨ������д���ʱ�䡣��������ת�в����޸ġ�
        /// </summary>
        [Comment("���ʱ�䡣Ϊ�����ʾδ��ˡ����ͨ������д���ʱ�䡣")]
        [Precision(3)]
        public DateTime? AuditDateTime { get; set; }

        /// <summary>
        /// ��˲�����Id��Ϊ�����ʾδ��ˡ���������ת�в����޸ġ�
        /// </summary>
        [Comment("��˲�����Id��Ϊ�����ʾδ��ˡ�")]
        public Guid? AuditOperatorId { get; set; }

        #region ICreatorInfo

        /// <summary>
        /// ������Id���Ǽ���Id����������ʱ���Ǽ���=�����ˣ������ˡ��Ǽ��˲���ѡ��Ϊ��¼�ˣ��������Ǽ�ʱѡ�������ˣ��Ǽ���Ϊ��¼�ˡ�
        /// </summary>
        [Comment("������Id�����Ǽ���Id")]
        public Guid? CreateBy { get; set; }

        /// <summary>
        /// ����ʱ�䡣
        /// </summary>
        [Comment("������ʱ��")]
        [Precision(3)]
        public DateTime CreateDateTime { get; set; }

        #endregion
    }

    /// <summary>
    /// OA�������뵥��ϸ��
    /// �˱��ɲ�����Ա��д������רҵ��ַ��á�֧�ֶ�����ϸ��¼��
    /// </summary>
    [Comment("OA�������뵥��ϸ��")]
    [Index(nameof(ParentId), IsUnique = false)]
    public class OaExpenseRequisitionItem : GuidKeyObjectBase
    {
        /// <summary>
        /// ���캯����
        /// </summary>
        public OaExpenseRequisitionItem()
        {
            SettlementDateTime = DateTime.Today;
        }

        /// <summary>
        /// ���뵥Id���������뵥Id�������� <see cref="OaExpenseRequisition"/> ��Id��
        /// </summary>
        [Comment("���뵥Id���������뵥Id��������OaExpenseRequisition��Id")]
        public Guid? ParentId { get; set; }

        /// <summary>
        /// ��š�������ϸ���������ʾ��
        /// </summary>
        [Comment("��ţ�������ϸ���������ʾ")]
        public int SequenceNumber { get; set; }

        /// <summary>
        /// ����ʱ�䡣������Ա����ʱ�Ľ���ʱ�䣬�ɿ���ƾ֤�ڼ䡣
        /// </summary>
        [Comment("����ʱ�䣬������Ա����ʱ�Ľ���ʱ�䣬�ɿ���ƾ֤�ڼ�")]
        public DateTime SettlementDateTime { get; set; }

        /// <summary>
        /// �����˺�Id�������� <see cref="BankInfo"/> ��Id��ͳһ���˺�ѡ��
        /// </summary>
        [Comment("�����˺�Id��������BankInfo��Id��ͳһ���˺�ѡ��")]
        public Guid? SettlementAccountId { get; set; }

        /// <summary>
        /// �ճ���������Id�������� <see cref="DailyFeesType"/> ��Id������ѡ����ȷ�ķ������ࡣ
        /// </summary>
        [Comment("�ճ���������Id��������DailyFeesType��Id������ѡ����ȷ�ķ�������")]
        public Guid? DailyFeesTypeId { get; set; }

        /// <summary>
        /// ������ϸ��Ľ���λС����
        /// </summary>
        [Comment("������ϸ��Ľ���λС��")]
        [Precision(18, 2)]
        public decimal Amount { get; set; }

        /// <summary>
        /// Ա��Id�����ÿ��ܺ��㵽��ͬԱ�����£��ǽ������ˣ��������� <see cref="Account"/> ��Id��
        /// </summary>
        [Comment("Ա��Id�����ÿ��ܺ��㵽��ͬԱ�����£�������Account��Id")]
        public Guid? EmployeeId { get; set; }

        /// <summary>
        /// ����Id��ѡ��ϵͳ�е���֯�ܹ����ţ������� <see cref="PlOrganization"/> ��Id��
        /// </summary>
        [Comment("����Id��ѡ��ϵͳ�е���֯�ܹ����ţ�������PlOrganization��Id")]
        public Guid? DepartmentId { get; set; }

        /// <summary>
        /// ƾ֤�š���̨�Զ����ɣ���ʽ���ڼ�-ƾ֤��-��ţ��磺7-��-1����
        /// </summary>
        [Comment("ƾ֤�ţ���̨�Զ����ɣ���ʽ���ڼ�-ƾ֤��-���")]
        [MaxLength(32)]
        public string VoucherNumber { get; set; }

        /// <summary>
        /// ժҪ��������д�ķ���ժҪ˵������"��ͳԷ�"��
        /// </summary>
        [Comment("ժҪ��������д�ķ���ժҪ˵��")]
        [MaxLength(256)]
        public string Summary { get; set; }

        /// <summary>
        /// ��Ʊ�š�
        /// </summary>
        [Comment("��Ʊ��")]
        [MaxLength(64)]
        public string InvoiceNumber { get; set; }

        /// <summary>
        /// ��ע��
        /// </summary>
        [Comment("��ע")]
        [MaxLength(512)]
        public string Remark { get; set; }
    }

    /// <summary>
    /// OA�������뵥��չ������
    /// </summary>
    public static class OaExpenseRequisitionExtensions
    {
        /// <summary>
        /// ��ȡ���뵥�ķ�����ϸ�
        /// </summary>
        /// <param name="requisition">���뵥</param>
        /// <param name="context">���ݿ�������</param>
        /// <returns>������ϸ���ѯ</returns>
        public static IQueryable<OaExpenseRequisitionItem> GetItems(this OaExpenseRequisition requisition, DbContext context)
        {
            return context.Set<OaExpenseRequisitionItem>().Where(x => x.ParentId == requisition.Id);
        }

        /// <summary>
        /// ��ȡ��������Ϣ��
        /// </summary>
        /// <param name="requisition">���뵥</param>
        /// <param name="context">���ݿ�������</param>
        /// <returns>�������˺Ŷ���</returns>
        public static Account GetApplicant(this OaExpenseRequisition requisition, DbContext context)
        {
            return requisition.ApplicantId.HasValue ? context.Set<Account>().Find(requisition.ApplicantId.Value) : null;
        }

        /// <summary>
        /// ��ȡ�Ǽ�����Ϣ��CreateBy���ǵǼ���Id��
        /// </summary>
        /// <param name="requisition">���뵥</param>
        /// <param name="context">���ݿ�������</param>
        /// <returns>�Ǽ����˺Ŷ���</returns>
        public static Account GetRegistrar(this OaExpenseRequisition requisition, DbContext context)
        {
            return requisition.CreateBy.HasValue ? context.Set<Account>().Find(requisition.CreateBy.Value) : null;
        }

        /// <summary>
        /// ��ȡ�������Ϣ��
        /// </summary>
        /// <param name="requisition">���뵥</param>
        /// <param name="context">���ݿ�������</param>
        /// <returns>������˺Ŷ���</returns>
        public static Account GetAuditor(this OaExpenseRequisition requisition, DbContext context)
        {
            return requisition.AuditOperatorId.HasValue ? context.Set<Account>().Find(requisition.AuditOperatorId.Value) : null;
        }

        /// <summary>
        /// �ж����뵥�Ƿ���Ա༭������˵����뵥���ɱ༭��
        /// </summary>
        /// <param name="requisition">���뵥</param>
        /// <param name="context">���ݿ������ģ���ѡ��</param>
        /// <returns>���Ա༭����true�����򷵻�false</returns>
        public static bool CanEdit(this OaExpenseRequisition requisition, DbContext context = null)
        {
            return !requisition.AuditDateTime.HasValue; // δ��˵Ŀ��Ա༭
        }

        /// <summary>
        /// �ж����뵥�Ƿ�����ˡ�
        /// </summary>
        /// <param name="requisition">���뵥</param>
        /// <returns>����˷���true�����򷵻�false</returns>
        public static bool IsAudited(this OaExpenseRequisition requisition)
        {
            return requisition.AuditDateTime.HasValue;
        }

        /// <summary>
        /// ��ȡ���뵥������״̬��
        /// </summary>
        /// <param name="requisition">���뵥</param>
        /// <param name="context">���ݿ�������</param>
        /// <returns>����״̬����</returns>
        public static string GetApprovalStatus(this OaExpenseRequisition requisition, DbContext context = null)
        {
            return requisition.AuditDateTime.HasValue ? "�����" : "�ݸ�";
        }

        /// <summary>
        /// ��ȡ��֧���͵���ʾ���ơ�
        /// </summary>
        /// <param name="requisition">���뵥</param>
        /// <returns>��֧������ʾ����</returns>
        public static string GetIncomeExpenseTypeDisplayName(this OaExpenseRequisition requisition)
        {
            return requisition.IncomeExpenseType switch
            {
                Data.OA.IncomeExpenseType.Income => "�տ�",
                Data.OA.IncomeExpenseType.Expense => "����",
                _ => "δ����"
            };
        }

        /// <summary>
        /// ��֤��ϸ���ϼ��Ƿ����������һ�¡�
        /// </summary>
        /// <param name="requisition">���뵥</param>
        /// <param name="context">���ݿ�������</param>
        /// <returns>���һ�·���true�����򷵻�false</returns>
        public static bool ValidateAmountConsistency(this OaExpenseRequisition requisition, DbContext context)
        {
            var itemsSum = requisition.GetItems(context).Sum(x => x.Amount);
            return Math.Abs(itemsSum - requisition.Amount) < 0.01m; // ����0.01�����
        }

        /// <summary>
        /// ��ȡ��ϸ���ϼơ�
        /// </summary>
        /// <param name="requisition">���뵥</param>
        /// <param name="context">���ݿ�������</param>
        /// <returns>��ϸ���ϼ�</returns>
        public static decimal GetItemsAmountSum(this OaExpenseRequisition requisition, DbContext context)
        {
            return requisition.GetItems(context).Sum(x => x.Amount);
        }
    }

    /// <summary>
    /// OA�������뵥��ϸ��չ������
    /// </summary>
    public static class OaExpenseRequisitionItemExtensions
    {
        /// <summary>
        /// ��ȡ��ϸ��Ľ����˺���Ϣ��
        /// </summary>
        /// <param name="item">��ϸ��</param>
        /// <param name="context">���ݿ�������</param>
        /// <returns>�����˺���Ϣ</returns>
        public static BankInfo GetSettlementAccount(this OaExpenseRequisitionItem item, DbContext context)
        {
            return item.SettlementAccountId.HasValue ? context.Set<BankInfo>().Find(item.SettlementAccountId.Value) : null;
        }

        /// <summary>
        /// ��ȡ��ϸ��ķ���������Ϣ��
        /// </summary>
        /// <param name="item">��ϸ��</param>
        /// <param name="context">���ݿ�������</param>
        /// <returns>����������Ϣ</returns>
        public static DailyFeesType GetDailyFeesType(this OaExpenseRequisitionItem item, DbContext context)
        {
            return item.DailyFeesTypeId.HasValue ? context.Set<DailyFeesType>().Find(item.DailyFeesTypeId.Value) : null;
        }

        /// <summary>
        /// ��ȡ��ϸ���Ա����Ϣ��
        /// </summary>
        /// <param name="item">��ϸ��</param>
        /// <param name="context">���ݿ�������</param>
        /// <returns>Ա����Ϣ</returns>
        public static Account GetEmployee(this OaExpenseRequisitionItem item, DbContext context)
        {
            return item.EmployeeId.HasValue ? context.Set<Account>().Find(item.EmployeeId.Value) : null;
        }

        /// <summary>
        /// ��ȡ��ϸ��Ĳ�����Ϣ��
        /// </summary>
        /// <param name="item">��ϸ��</param>
        /// <param name="context">���ݿ�������</param>
        /// <returns>������Ϣ</returns>
        public static PlOrganization GetDepartment(this OaExpenseRequisitionItem item, DbContext context)
        {
            return item.DepartmentId.HasValue ? context.Set<PlOrganization>().Find(item.DepartmentId.Value) : null;
        }
    }
}