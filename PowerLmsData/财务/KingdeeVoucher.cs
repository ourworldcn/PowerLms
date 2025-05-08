using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PowerLms.Data.Finance
{
    /// <summary>
    /// ������ݽ���ƾ֤��¼ģ��
    /// </summary>
    [Table("KingdeeVouchers")]
    [Comment("������ݽ���ƾ֤��¼")]
    public class KingdeeVoucher
    {
        /// <summary>
        /// ����ID��ϵͳ�ڲ�ʹ�ã�
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        /// <summary>
        /// �Ƶ�����
        /// </summary>
        [Column("FDATE")]
        [Comment("�Ƶ�����")]
        public DateTime FDATE { get; set; }

        /// <summary>
        /// ƾ֤����
        /// </summary>
        [Column("FTRANSDATE")]
        [Comment("ƾ֤����")]
        public DateTime FTRANSDATE { get; set; }

        /// <summary>
        /// �ڼ䣬�����ڼ���Ż��·�
        /// </summary>
        [Column("FPERIOD")]
        [Comment("�ڼ䣬�����ڼ���Ż��·�")]
        [Precision(10, 5)]
        public decimal FPERIOD { get; set; }

        /// <summary>
        /// ƾ֤�����
        /// </summary>
        [Column("FGROUP")]
        [Comment("ƾ֤�����")]
        [StringLength(10)]
        public string FGROUP { get; set; }

        /// <summary>
        /// ƾ֤��
        /// </summary>
        [Column("FNUM")]
        [Comment("ƾ֤��")]
        [Precision(10, 5)]
        public decimal FNUM { get; set; }

        /// <summary>
        /// ��¼�ţ�һ��ƾ֤���ڲ��ظ�
        /// </summary>
        [Column("FENTRYID")]
        [Comment("��¼�ţ�һ��ƾ֤���ڲ��ظ�")]
        [Precision(10, 5)]
        public decimal FENTRYID { get; set; }

        /// <summary>
        /// ժҪ���ͻ���+��Ʊ��ϸ+�ͻ��������
        /// </summary>
        [Column("FEXP")]
        [Comment("ժҪ���ͻ���+��Ʊ��ϸ+�ͻ��������")]
        [StringLength(500)]
        public string FEXP { get; set; }

        /// <summary>
        /// ��Ŀ����
        /// </summary>
        [Column("FACCTID")]
        [Comment("��Ŀ����")]
        [StringLength(50)]
        public string FACCTID { get; set; }

        /// <summary>
        /// �������
        /// </summary>
        [Column("FCLSNAME1")]
        [Comment("�������")]
        [StringLength(50)]
        public string FCLSNAME1 { get; set; }

        /// <summary>
        /// �ͻ�������
        /// </summary>
        [Column("FOBJID1")]
        [Comment("�ͻ�������")]
        [StringLength(50)]
        public string FOBJID1 { get; set; }

        /// <summary>
        /// �ͻ�����
        /// </summary>
        [Column("FOBJNAME1")]
        [Comment("�ͻ�����")]
        [StringLength(200)]
        public string FOBJNAME1 { get; set; }

        /// <summary>
        /// �������2
        /// </summary>
        [Column("FCLSNAME2")]
        [StringLength(50)]
        public string FCLSNAME2 { get; set; }

        /// <summary>
        /// ����������2
        /// </summary>
        [Column("FOBJID2")]
        [StringLength(50)]
        public string FOBJID2 { get; set; }

        /// <summary>
        /// �����������2
        /// </summary>
        [Column("FOBJNAME2")]
        [StringLength(200)]
        public string FOBJNAME2 { get; set; }

        /// <summary>
        /// �������3
        /// </summary>
        [Column("FCLSNAME3")]
        [StringLength(50)]
        public string FCLSNAME3 { get; set; }

        /// <summary>
        /// ����������3
        /// </summary>
        [Column("FOBJID3")]
        [StringLength(50)]
        public string FOBJID3 { get; set; }

        /// <summary>
        /// �����������3
        /// </summary>
        [Column("FOBJNAME3")]
        [StringLength(200)]
        public string FOBJNAME3 { get; set; }

        /// <summary>
        /// �������4
        /// </summary>
        [Column("FCLSNAME4")]
        [StringLength(50)]
        public string FCLSNAME4 { get; set; }

        /// <summary>
        /// ����������4
        /// </summary>
        [Column("FOBJID4")]
        [StringLength(50)]
        public string FOBJID4 { get; set; }

        /// <summary>
        /// �����������4
        /// </summary>
        [Column("FOBJNAME4")]
        [StringLength(200)]
        public string FOBJNAME4 { get; set; }

        /// <summary>
        /// �������5
        /// </summary>
        [Column("FCLSNAME5")]
        [StringLength(50)]
        public string FCLSNAME5 { get; set; }

        /// <summary>
        /// ����������5
        /// </summary>
        [Column("FOBJID5")]
        [StringLength(50)]
        public string FOBJID5 { get; set; }

        /// <summary>
        /// �����������5
        /// </summary>
        [Column("FOBJNAME5")]
        [StringLength(200)]
        public string FOBJNAME5 { get; set; }

        /// <summary>
        /// �������6
        /// </summary>
        [Column("FCLSNAME6")]
        [StringLength(50)]
        public string FCLSNAME6 { get; set; }

        /// <summary>
        /// ����������6
        /// </summary>
        [Column("FOBJID6")]
        [StringLength(50)]
        public string FOBJID6 { get; set; }

        /// <summary>
        /// �����������6
        /// </summary>
        [Column("FOBJNAME6")]
        [StringLength(200)]
        public string FOBJNAME6 { get; set; }

        /// <summary>
        /// �������7
        /// </summary>
        [Column("FCLSNAME7")]
        [StringLength(50)]
        public string FCLSNAME7 { get; set; }

        /// <summary>
        /// ����������7
        /// </summary>
        [Column("FOBJID7")]
        [StringLength(50)]
        public string FOBJID7 { get; set; }

        /// <summary>
        /// �����������7
        /// </summary>
        [Column("FOBJNAME7")]
        [StringLength(200)]
        public string FOBJNAME7 { get; set; }

        /// <summary>
        /// �������8
        /// </summary>
        [Column("FCLSNAME8")]
        [StringLength(50)]
        public string FCLSNAME8 { get; set; }

        /// <summary>
        /// ����������8
        /// </summary>
        [Column("FOBJID8")]
        [StringLength(50)]
        public string FOBJID8 { get; set; }

        /// <summary>
        /// �����������8
        /// </summary>
        [Column("FOBJNAME8")]
        [StringLength(200)]
        public string FOBJNAME8 { get; set; }

        /// <summary>
        /// �������9
        /// </summary>
        [Column("FCLSNAME9")]
        [StringLength(50)]
        public string FCLSNAME9 { get; set; }

        /// <summary>
        /// ����������9
        /// </summary>
        [Column("FOBJID9")]
        [StringLength(50)]
        public string FOBJID9 { get; set; }

        /// <summary>
        /// �����������9
        /// </summary>
        [Column("FOBJNAME9")]
        [StringLength(200)]
        public string FOBJNAME9 { get; set; }

        /// <summary>
        /// �������10
        /// </summary>
        [Column("FCLSNAME10")]
        [StringLength(50)]
        public string FCLSNAME10 { get; set; }

        /// <summary>
        /// ����������10
        /// </summary>
        [Column("FOBJID10")]
        [StringLength(50)]
        public string FOBJID10 { get; set; }

        /// <summary>
        /// �����������10
        /// </summary>
        [Column("FOBJNAME10")]
        [StringLength(200)]
        public string FOBJNAME10 { get; set; }

        /// <summary>
        /// �������11
        /// </summary>
        [Column("FCLSNAME11")]
        [StringLength(50)]
        public string FCLSNAME11 { get; set; }

        /// <summary>
        /// ����������11
        /// </summary>
        [Column("FOBJID11")]
        [StringLength(50)]
        public string FOBJID11 { get; set; }

        /// <summary>
        /// �����������11
        /// </summary>
        [Column("FOBJNAME11")]
        [StringLength(200)]
        public string FOBJNAME11 { get; set; }

        /// <summary>
        /// �������12
        /// </summary>
        [Column("FCLSNAME12")]
        [StringLength(50)]
        public string FCLSNAME12 { get; set; }

        /// <summary>
        /// ����������12
        /// </summary>
        [Column("FOBJID12")]
        [StringLength(50)]
        public string FOBJID12 { get; set; }

        /// <summary>
        /// �����������12
        /// </summary>
        [Column("FOBJNAME12")]
        [StringLength(200)]
        public string FOBJNAME12 { get; set; }

        /// <summary>
        /// �������13
        /// </summary>
        [Column("FCLSNAME13")]
        [StringLength(50)]
        public string FCLSNAME13 { get; set; }

        /// <summary>
        /// ����������13
        /// </summary>
        [Column("FOBJID13")]
        [StringLength(50)]
        public string FOBJID13 { get; set; }

        /// <summary>
        /// �����������13
        /// </summary>
        [Column("FOBJNAME13")]
        [StringLength(200)]
        public string FOBJNAME13 { get; set; }

        /// <summary>
        /// �������14
        /// </summary>
        [Column("FCLSNAME14")]
        [StringLength(50)]
        public string FCLSNAME14 { get; set; }

        /// <summary>
        /// ����������14
        /// </summary>
        [Column("FOBJID14")]
        [StringLength(50)]
        public string FOBJID14 { get; set; }

        /// <summary>
        /// �����������14
        /// </summary>
        [Column("FOBJNAME14")]
        [StringLength(200)]
        public string FOBJNAME14 { get; set; }

        /// <summary>
        /// �������15
        /// </summary>
        [Column("FCLSNAME15")]
        [StringLength(50)]
        public string FCLSNAME15 { get; set; }

        /// <summary>
        /// ����������15
        /// </summary>
        [Column("FOBJID15")]
        [StringLength(50)]
        public string FOBJID15 { get; set; }

        /// <summary>
        /// �����������15
        /// </summary>
        [Column("FOBJNAME15")]
        [StringLength(200)]
        public string FOBJNAME15 { get; set; }

        /// <summary>
        /// �ͻ��������
        /// </summary>
        [Column("FTRANSID")]
        [Comment("�ͻ��������")]
        [StringLength(50)]
        public string FTRANSID { get; set; }

        /// <summary>
        /// �ұ����
        /// </summary>
        [Column("FCYID")]
        [Comment("�ұ����")]
        [StringLength(10)]
        public string FCYID { get; set; }

        /// <summary>
        /// ����
        /// </summary>
        [Column("FEXCHRATE")]
        [Comment("����")]
        [Precision(18, 7)]
        public decimal FEXCHRATE { get; set; }

        /// <summary>
        /// �������0-�跽��1-����
        /// </summary>
        [Column("FDC")]
        [Comment("�������0-�跽��1-����")]
        public int FDC { get; set; }

        /// <summary>
        /// ��ҽ��
        /// </summary>
        [Column("FFCYAMT")]
        [Comment("��ҽ��")]
        [Precision(18, 5)]
        public decimal? FFCYAMT { get; set; }

        /// <summary>
        /// ����
        /// </summary>
        [Column("FQTY")]
        [Comment("����")]
        [Precision(18, 5)]
        public decimal? FQTY { get; set; }

        /// <summary>
        /// ����
        /// </summary>
        [Column("FPRICE")]
        [Comment("����")]
        [Precision(18, 5)]
        public decimal? FPRICE { get; set; }

        /// <summary>
        /// ���跽
        /// </summary>
        [Column("FDEBIT")]
        [Comment("���跽")]
        [Precision(18, 5)]
        public decimal? FDEBIT { get; set; }

        /// <summary>
        /// ������
        /// </summary>
        [Column("FCREDIT")]
        [Comment("������")]
        [Precision(18, 5)]
        public decimal? FCREDIT { get; set; }

        /// <summary>
        /// ���㷽ʽ����
        /// </summary>
        [Column("FSETTLCODE")]
        [Comment("���㷽ʽ����")]
        [StringLength(20)]
        public string FSETTLCODE { get; set; }

        /// <summary>
        /// �����
        /// </summary>
        [Column("FSETTLENO")]
        [Comment("�����")]
        [StringLength(50)]
        public string FSETTLENO { get; set; }

        /// <summary>
        /// �Ƶ�������
        /// </summary>
        [Column("FPREPARE")]
        [Comment("�Ƶ�������")]
        [StringLength(50)]
        public string FPREPARE { get; set; }

        /// <summary>
        /// ֧��
        /// </summary>
        [Column("FPAY")]
        [Comment("֧��")]
        [StringLength(50)]
        public string FPAY { get; set; }

        /// <summary>
        /// ����������
        /// </summary>
        [Column("FCASH")]
        [Comment("����������")]
        [StringLength(50)]
        public string FCASH { get; set; }

        /// <summary>
        /// ����������
        /// </summary>
        [Column("FPOSTER")]
        [Comment("����������")]
        [StringLength(50)]
        public string FPOSTER { get; set; }

        /// <summary>
        /// ���������
        /// </summary>
        [Column("FCHECKER")]
        [Comment("���������")]
        [StringLength(50)]
        public string FCHECKER { get; set; }

        /// <summary>
        /// ��������
        /// </summary>
        [Column("FATTCHMENT")]
        [Comment("��������")]
        public int? FATTCHMENT { get; set; }

        /// <summary>
        /// ����״̬
        /// </summary>
        [Column("FPOSTED")]
        [Comment("����״̬")]
        public int? FPOSTED { get; set; }

        /// <summary>
        /// ģ��
        /// </summary>
        [Column("FMODULE")]
        [Comment("ģ��")]
        [StringLength(50)]
        public string FMODULE { get; set; }

        /// <summary>
        /// ɾ�����
        /// </summary>
        [Column("FDELETED")]
        [Comment("ɾ�����")]
        public bool? FDELETED { get; set; }

        /// <summary>
        /// ���к�
        /// </summary>
        [Column("FSERIALNO")]
        [Comment("���к�")]
        [StringLength(50)]
        public string FSERIALNO { get; set; }

        /// <summary>
        /// ��λ����
        /// </summary>
        [Column("FUNITNAME")]
        [Comment("��λ����")]
        [StringLength(100)]
        public string FUNITNAME { get; set; }

        /// <summary>
        /// �ο�
        /// </summary>
        [Column("FREFERENCE")]
        [Comment("�ο�")]
        [StringLength(200)]
        public string FREFERENCE { get; set; }

        /// <summary>
        /// �ֽ���
        /// </summary>
        [Column("FCASHFLOW")]
        [Comment("�ֽ���")]
        [StringLength(50)]
        public string FCASHFLOW { get; set; }

        /// <summary>
        /// ������
        /// </summary>
        [Column("FHANDLER")]
        [Comment("������")]
        [StringLength(50)]
        public string FHANDLER { get; set; }

        /// <summary>
        /// ���ɽ��ƾ֤��¼����������
        /// </summary>
        /// <param name="count">Ҫ���ɵļ�¼����</param>
        /// <returns>���ƾ֤��¼�ļ���</returns>
        public static IEnumerable<KingdeeVoucher> GenerateSeedData(int count)
        {
            if (count <= 0)
                throw new ArgumentException("���ɵļ�¼�������������", nameof(count));

            var random = new Random();
            var result = new List<KingdeeVoucher>(count);

            // ����һЩ�������ݣ��������ѡ��
            string[] accountIds = { "1001", "1002", "2001", "2002", "5001", "6001" };
            string[] groups = { "��", "��", "��", "ת" };
            string[] classNames = { "�ͻ�", "����", "Ա��", "��Ŀ", "����", "�����˻�", "��Ӧ��" };
            string[] objIds = { "C001", "C002", "D001", "D002", "E001", "E002", "P001", "P002" };
            string[] objNames = { "��Ϊ�Ƽ�", "��Ѷ��˾", "�з���", "�г���", "����", "�Ż�", "ERP��Ŀ", "CRM��Ŀ" };
            string[] currencies = { "RMB", "USD", "EUR", "GBP", "JPY" };
            string[] handlers = { "������", "�����", "���ܼ�", "������" };

            var baseDate = DateTime.Now.Date.AddDays(-30); // ��׼����Ϊ��ǰ������ǰ30��

            for (int i = 0; i < count; i++)
            {
                var voucherDate = baseDate.AddDays(random.Next(30)); // ���ƾ֤����(���30����)
                var transDate = voucherDate.AddDays(-random.Next(5)); // ��������ͨ����ƾ֤����֮ǰ
                var period = Math.Floor((decimal)voucherDate.Month) + (decimal)voucherDate.Day / 100; // �ڼ䣬����5��10��Ϊ5.10

                // �����������λС��
                var amount = Math.Round((decimal)(random.NextDouble() * 10000 + 100), 2);
                // ��������ǽ跽���Ǵ���
                var isDC = random.Next(2);
                // ��ѡ�����
                var currency = currencies[random.Next(currencies.Length)];
                // ���ݱ���ȷ������
                var exchangeRate = currency == "RMB" ? 1.0m : Math.Round((decimal)(random.NextDouble() * 2 + 5), 4);

                var voucher = new KingdeeVoucher
                {
                    // ϵͳ���Զ�����Id
                    FDATE = voucherDate, // ƾ������
                    FTRANSDATE = transDate, // ƾ֤����
                    FPERIOD = period, // �ڼ�
                    FGROUP = groups[random.Next(groups.Length)], // ���ƾ֤��
                    FNUM = i + 1, // ƾ֤�ţ���1��ʼ����
                    FENTRYID = i + 1, // ��¼��
                    FEXP = $"ҵ��ժҪ{i + 1} - {objNames[random.Next(objNames.Length)]}��{random.Next(1000, 9999)}��ҵ��", // ժҪ
                    FACCTID = accountIds[random.Next(accountIds.Length)], // �����Ŀ����

                    // ����ά��1 - ͨ���ǿͻ�
                    FCLSNAME1 = classNames[0], // �ͻ�
                    FOBJID1 = objIds[random.Next(2)], // ����ͻ�����
                    FOBJNAME1 = objNames[random.Next(2)], // ����ͻ�����

                    // ����ά��2 - ͨ���ǲ���
                    FCLSNAME2 = classNames[1], // ����
                    FOBJID2 = objIds[2 + random.Next(2)], // ������Ŵ���
                    FOBJNAME2 = objNames[2 + random.Next(2)], // �����������

                    // �ͻ����׺�
                    FTRANSID = $"TX{DateTime.Now:yyyyMMdd}{i + 1:D4}", // ���ɽ��ױ��

                    // ������Ϣ
                    FCYID = currency, // ʹ����ǰѡ��ı���
                    FEXCHRATE = exchangeRate, // ʹ����ǰ����Ļ���

                    // �������
                    FDC = isDC, // 0-�跽��1-����

                    // �����Ϣ
                    FFCYAMT = amount, // ԭ�ҽ��
                    FQTY = random.Next(1, 10), // ������1-10֮��
                    FPRICE = Math.Round(amount / (decimal)random.Next(1, 10), 2), // ����

                    // ���ݽ���������ý��
                    FDEBIT = isDC == 0 ? amount : 0, // �跽���
                    FCREDIT = isDC == 1 ? amount : 0, // �������

                    // ������Ϣ
                    FSETTLCODE = $"JS{random.Next(10000, 99999)}", // ���㷽ʽ����
                    FSETTLENO = $"JH{DateTime.Now:yyyyMMdd}{i + 1:D4}", // �����

                    // ������Ա��Ϣ
                    FPREPARE = handlers[random.Next(handlers.Length)], // �Ƶ���Ա
                    FPAY = handlers[random.Next(handlers.Length)], // ֧��
                    FCASH = handlers[random.Next(handlers.Length)], // �ֽ������
                    FPOSTER = handlers[random.Next(handlers.Length)], // ���˲�����
                    FCHECKER = handlers[random.Next(handlers.Length)], // ��˲�����

                    // ������Ϣ
                    FATTCHMENT = random.Next(1, 5), // ��������
                    FPOSTED = random.Next(0, 2), // ����״̬��0��1
                    FMODULE = "GL", // ģ�飬GLΪ����
                    FDELETED = false, // ��ɾ��
                    FSERIALNO = $"SN{DateTime.Now:yyyyMMdd}{i + 1:D6}", // ��ˮ��
                    FUNITNAME = "ƪ", // ��λ����
                    FREFERENCE = $"REF{i + 1:D6}", // �ο����
                    FCASHFLOW = $"CSF{random.Next(1000, 9999)}", // �ֽ������
                    FHANDLER = handlers[random.Next(handlers.Length)] // ������
                };

                result.Add(voucher);
            }

            return result;
        }

    }
}
