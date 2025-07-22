using Microsoft.EntityFrameworkCore;
using OW.Data;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace PowerLms.Data.OA
{
    /// <summary>
    /// ���㷽ʽö�١�
    /// </summary>
    public enum SettlementMethodType : byte
    {
        /// <summary>
        /// �ֽ���㡣
        /// </summary>
        Cash = 0,

        /// <summary>
        /// ����ת�ˡ�
        /// </summary>
        Bank = 1
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
            ApplyDateTime = OwHelper.WorldNow;
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
        [Required]
        public Guid ApplicantId { get; set; }

        /// <summary>
        /// ����ʱ�䡣
        /// </summary>
        [Comment("����ʱ��")]
        [Precision(3)]
        public DateTime ApplyDateTime { get; set; }

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
        /// ���㷽ʽ���ֽ������ת�ˣ��������̳ɹ���ɺ�ͨ����������ӿڴ���
        /// </summary>
        [Comment("���㷽ʽ���ֽ������ת�ˣ��������̳ɹ���ɺ�ͨ����������ӿڴ���")]
        public SettlementMethodType? SettlementMethod { get; set; }

        /// <summary>
        /// �����˻�Id�������㷽ʽ������ʱ��ѡ�񱾹�˾��Ϣ�е������˻�id���������̳ɹ���ɺ�ͨ����������ӿڴ���
        /// </summary>
        [Comment("�����˻�Id�������㷽ʽ������ʱѡ�񱾹�˾��Ϣ�е������˻�id���������̳ɹ���ɺ�ͨ����������ӿڴ���")]
        public Guid? BankAccountId { get; set; }

        /// <summary>
        /// ���ʱ�䡣
        /// </summary>
        [Comment("���ʱ��")]
        [Precision(3)]
        public DateTime? AuditDateTime { get; set; }

        /// <summary>
        /// ��˲�����Id��
        /// </summary>
        [Comment("��˲�����Id")]
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
    /// �ļ���ϸ��id�����뵥id�µ��ļ�����
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
            ExpenseDate = DateTime.Today;
            ExchangeRate = 1.0m;
            Currency = "CNY";
        }

        /// <summary>
        /// ���뵥Id���������뵥Id�������� <see cref="OaExpenseRequisition"/> ��Id��
        /// </summary>
        [Comment("���뵥Id���������뵥Id��������OaExpenseRequisition��Id")]
        [Required]
        public Guid ParentId { get; set; }

        /// <summary>
        /// �ճ���������Id�������� <see cref="DailyFeesType"/> ��Id��
        /// </summary>
        [Comment("�ճ���������Id��������DailyFeesType��Id")]
        [Required]
        public Guid DailyFeesTypeId { get; set; }

        /// <summary>
        /// ���÷���ʱ�䡣
        /// </summary>
        [Comment("���÷���ʱ��")]
        public DateTime ExpenseDate { get; set; }

        /// <summary>
        /// ��Ʊ�š�
        /// </summary>
        [Comment("��Ʊ��")]
        [MaxLength(64)]
        public string InvoiceNumber { get; set; }

        /// <summary>
        /// ���֡���׼������д��
        /// </summary>
        [Comment("���֣���׼������д")]
        [MaxLength(4), Unicode(false)]
        public string Currency { get; set; } = "CNY";

        /// <summary>
        /// ���ʡ�
        /// </summary>
        [Comment("����")]
        [Precision(18, 6)]
        public decimal ExchangeRate { get; set; } = 1.0m;

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
            return context.Set<Account>().Find(requisition.ApplicantId);
        }

        /// <summary>
        /// ��ȡ�Ǽ�����Ϣ��CreateBy���ǵǼ���Id��
        /// </summary>
        /// <param name="requisition">���뵥</param>
        /// <param name="context">���ݿ�������</param>
        /// <returns>�Ǽ����˺Ŷ���</returns>
        public static Account GetRegistrar(this OaExpenseRequisition requisition, DbContext context)
        {
            return context.Set<Account>().Find(requisition.CreateBy);
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
        /// ��ȡ���㷽ʽ����ʾ���ơ�
        /// </summary>
        /// <param name="requisition">���뵥</param>
        /// <returns>���㷽ʽ��ʾ����</returns>
        public static string GetSettlementMethodDisplayName(this OaExpenseRequisition requisition)
        {
            return requisition.SettlementMethod switch
            {
                SettlementMethodType.Cash => "�ֽ�",
                SettlementMethodType.Bank => "����ת��",
                _ => "δ����"
            };
        }
    }
}