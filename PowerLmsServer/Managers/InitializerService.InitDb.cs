using AutoMapper;
using DotNetDBF;
using Microsoft.Data.Sql;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Primitives;
using NPOI.SS.Formula.Functions;
using NPOI.SS.UserModel;
using OW;
using OW.Data;
using OW.EntityFrameworkCore;
using PowerLms.Data;
using PowerLmsServer.EfData;
using System;
using System.Data;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Net.Mail;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace PowerLmsServer.Managers
{
    /// <summary>
    /// ��ʼ������
    /// </summary>
    public partial class InitializerService : BackgroundService
    {
        /// <summary>
        /// ��ʼ���������ݿ���������ݡ�
        /// </summary>
        /// <param name="svc"></param>
        private void InitDb(IServiceProvider svc)
        {
            var db = svc.GetRequiredService<PowerLmsUserDbContext>();

            #region ˰��Ʊͨ����ʼ����
            // ���ŵŵ��Ʊͨ���Ƿ��Ѵ��ڣ��粻���������
            var nuoNuoChannelId = typeof(NuoNuoManager).GUID;
            if (!db.TaxInvoiceChannels.Any(c => c.Id == nuoNuoChannelId))
            {
                db.TaxInvoiceChannels.Add(new TaxInvoiceChannel
                {
                    Id = nuoNuoChannelId,
                    DisplayName = "ŵŵ��Ʊ",
                    InvoiceChannel = nameof(NuoNuoManager),
                    InvoiceChannelParams = "{}",
                });
                _Logger.LogInformation("���ŵŵ��Ʊͨ������");
            }
            // ����ֹ���Ʊͨ���Ƿ��Ѵ��ڣ��粻���������
            var manualChannelId = typeof(ManualInvoicingManager).GUID;
            if (!db.TaxInvoiceChannels.Any(c => c.Id == manualChannelId))
            {
                db.TaxInvoiceChannels.Add(new TaxInvoiceChannel
                {
                    Id = manualChannelId,
                    DisplayName = "�ֹ���Ʊ",
                    InvoiceChannel = nameof(ManualInvoicingManager),
                    InvoiceChannelParams = "{}",
                });
                _Logger.LogInformation("����ֹ���Ʊͨ������");
            }
            #endregion ˰��Ʊͨ����ʼ����

            #region ��ʼ����Ŀ������Ϣ���ϸ�������ĵ���
            // ���ŵŵ��Ʊͨ���Ƿ��Ѵ��ڣ��粻���������
            nuoNuoChannelId = typeof(NuoNuoManager).GUID;
            if (!db.TaxInvoiceChannels.Any(c => c.Id == nuoNuoChannelId))
            {
                db.TaxInvoiceChannels.Add(new TaxInvoiceChannel
                {
                    Id = nuoNuoChannelId,
                    DisplayName = "ŵŵ��Ʊ",
                    InvoiceChannel = nameof(NuoNuoManager),
                    InvoiceChannelParams = "{}",
                });
                _Logger.LogInformation("���ŵŵ��Ʊͨ������");
            }

            // ����ֹ���Ʊͨ���Ƿ��Ѵ��ڣ��粻���������
            manualChannelId = typeof(ManualInvoicingManager).GUID;
            if (!db.TaxInvoiceChannels.Any(c => c.Id == manualChannelId))
            {
                db.TaxInvoiceChannels.Add(new TaxInvoiceChannel
                {
                    Id = manualChannelId,
                    DisplayName = "�ֹ���Ʊ",
                    InvoiceChannel = nameof(ManualInvoicingManager),
                    InvoiceChannelParams = "{}",
                });
                _Logger.LogInformation("����ֹ���Ʊͨ������");
            }
            #endregion ˰��Ʊͨ����ʼ����

            #region ��ʼ����Ŀ������Ϣ
            // PBI_SALES_REVENUE - ��Ӫҵ�������Ŀ����
            var salesRevenueId = Guid.Parse("{E8B5C4D7-3F1A-4C2E-8D9A-1B5E7F9C3A6D}");
            if (!db.SubjectConfigurations.Any(c => c.Id == salesRevenueId))
            {
                db.SubjectConfigurations.Add(new SubjectConfiguration
                {
                    Id = salesRevenueId,
                    Code = "PBI_SALES_REVENUE",
                    SubjectNumber = "6001",
                    DisplayName = "��Ӫҵ������",
                    VoucherGroup = "ת", // ת��ƾ֤
                    AccountingCategory = "�ͻ�", // �������Ϊ�ͻ�
                    Remark = "��Ʊ����ʹ�õ���Ӫҵ�������Ŀ�����ڼ�¼��Ʊ�������������˰�ϼƼ�ȥ˰�",
                    CreateBy = null, // ϵͳ��ʼ�����޾��崴����
                    CreateDateTime = OwHelper.WorldNow,
                    IsDelete = false
                });
                _Logger.LogInformation("���PBI��Ӫҵ�������Ŀ����");
            }
            else
            {
                _Logger.LogDebug("PBI��Ӫҵ�������Ŀ�����Ѵ��ڣ�������ʼ��");
            }

            // PBI_TAX_PAYABLE - Ӧ��˰���Ŀ����
            var taxPayableId = Guid.Parse("{F2A6D8E9-4B7C-5E3F-9A1B-2C6F8E0D4A7C}");
            if (!db.SubjectConfigurations.Any(c => c.Id == taxPayableId))
            {
                db.SubjectConfigurations.Add(new SubjectConfiguration
                {
                    Id = taxPayableId,
                    Code = "PBI_TAX_PAYABLE",
                    SubjectNumber = "2221",
                    DisplayName = "Ӧ��˰��",
                    VoucherGroup = "ת", // ת��ƾ֤
                    AccountingCategory = "�ͻ�", // �������Ϊ�ͻ�
                    Remark = "��Ʊ����ʹ�õ�Ӧ��˰���Ŀ�����ڼ�¼��Ʊ������˰���",
                    CreateBy = null, // ϵͳ��ʼ�����޾��崴����
                    CreateDateTime = OwHelper.WorldNow,
                    IsDelete = false
                });
                _Logger.LogInformation("���PBIӦ��˰���Ŀ����");
            }
            else
            {
                _Logger.LogDebug("PBIӦ��˰���Ŀ�����Ѵ��ڣ�������ʼ��");
            }

            // PBI_ACC_RECEIVABLE - Ӧ���˿��Ŀ����
            var accReceivableId = Guid.Parse("{A3B7E1F5-6C8D-7A2B-3E4F-9D1C5B8A7E6F}");
            if (!db.SubjectConfigurations.Any(c => c.Id == accReceivableId))
            {
                db.SubjectConfigurations.Add(new SubjectConfiguration
                {
                    Id = accReceivableId,
                    Code = "PBI_ACC_RECEIVABLE",
                    SubjectNumber = "1122",
                    DisplayName = "Ӧ���˿�",
                    VoucherGroup = "ת", // ת��ƾ֤
                    AccountingCategory = "�ͻ�", // �������Ϊ�ͻ�
                    Remark = "��Ʊ����ʹ�õ�Ӧ���˿��Ŀ�����ڼ�¼��Ʊ������Ӧ�տ����˰�ϼƣ�",
                    CreateBy = null, // ϵͳ��ʼ�����޾��崴����
                    CreateDateTime = OwHelper.WorldNow,
                    IsDelete = false
                });
                _Logger.LogInformation("���PBIӦ���˿��Ŀ����");
            }
            else
            {
                _Logger.LogDebug("PBIӦ���˿��Ŀ�����Ѵ��ڣ�������ʼ��");
            }

            bool hasNewConfigurations = false;

            #region ������/���̿�Ŀ
            // SETTLEMENT_RECEIPT - ʵ��
            if (!db.SubjectConfigurations.Any(c => c.Code == "SETTLEMENT_RECEIPT"))
            {
                db.SubjectConfigurations.Add(new SubjectConfiguration
                {
                    Id = Guid.Parse("{F5A6B7C8-9D0E-1F2A-3B4C-5D6E7F8A9B0C}"),
                    Code = "SETTLEMENT_RECEIPT",
                    SubjectNumber = "", // ����
                    DisplayName = "ʵ��",
                    VoucherGroup = "��",
                    AccountingCategory = "",
                    Preparer = "ϵͳ����",
                    Remark = "ʵ�ս��㵥������Ŀ����",
                    CreateBy = null,
                    CreateDateTime = OwHelper.WorldNow,
                    IsDelete = false
                });
                _Logger.LogInformation("���ʵ�տ�Ŀ����");
                hasNewConfigurations = true;
            }
            // SETTLEMENT_PAYMENT - ʵ��
            if (!db.SubjectConfigurations.Any(c => c.Code == "SETTLEMENT_PAYMENT"))
            {
                db.SubjectConfigurations.Add(new SubjectConfiguration
                {
                    Id = Guid.Parse("{A6B7C8D9-0E1F-2A3B-4C5D-6E7F8A9B0C1D}"),
                    Code = "SETTLEMENT_PAYMENT",
                    SubjectNumber = "", // ����
                    DisplayName = "ʵ��",
                    VoucherGroup = "��",
                    AccountingCategory = "",
                    Preparer = "ϵͳ����",
                    Remark = "ʵ�����㵥������Ŀ����",
                    CreateBy = null,
                    CreateDateTime = OwHelper.WorldNow,
                    IsDelete = false
                });
                _Logger.LogInformation("���ʵ����Ŀ����");
                hasNewConfigurations = true;
            }
            // ACCRUAL_TAX_REVENUE - ����˰����Ӫҵ������
            if (!db.SubjectConfigurations.Any(c => c.Code == "ACCRUAL_TAX_REVENUE"))
            {
                db.SubjectConfigurations.Add(new SubjectConfiguration
                {
                    Id = Guid.Parse("{A8B9C0D1-2E3F-4A5B-6C7D-8E9F0A1B2C3D}"),
                    Code = "ACCRUAL_TAX_REVENUE",
                    SubjectNumber = "", // ����
                    DisplayName = "����˰����Ӫҵ������",
                    VoucherGroup = "ת",
                    AccountingCategory = "",
                    Preparer = "ϵͳ����",
                    Remark = "����˰����Ӫҵ��������������",
                    CreateBy = null,
                    CreateDateTime = OwHelper.WorldNow,
                    IsDelete = false
                });
                _Logger.LogInformation("��Ӽ���˰����Ӫҵ����������");
                hasNewConfigurations = true;
            }
            #endregion

            #region ������Ŀ
            // AR_CODE - Ӧ���˿�
            if (!db.SubjectConfigurations.Any(c => c.Code == "AR_CODE"))
            {
                db.SubjectConfigurations.Add(new SubjectConfiguration
                {
                    Id = Guid.Parse("{C2D3E4F5-6A7B-8C9D-0E1F-2A3B4C5D6E7F}"),
                    Code = "AR_CODE",
                    SubjectNumber = "1131",
                    DisplayName = "Ӧ���˿�",
                    VoucherGroup = "",
                    AccountingCategory = "",
                    Preparer = "ϵͳ����",
                    Remark = "������Ŀ-Ӧ���˿�",
                    CreateBy = null,
                    CreateDateTime = OwHelper.WorldNow,
                    IsDelete = false
                });
                _Logger.LogInformation("��ӻ���Ӧ���˿��Ŀ����");
                hasNewConfigurations = true;
            }
            // REVENUE_CODE - ��Ӫҵ������
            if (!db.SubjectConfigurations.Any(c => c.Code == "REVENUE_CODE"))
            {
                db.SubjectConfigurations.Add(new SubjectConfiguration
                {
                    Id = Guid.Parse("{B1C2D3E4-5F6A-7B8C-9D0E-1F2A3B4C5D6E}"),
                    Code = "REVENUE_CODE",
                    SubjectNumber = "5101",
                    DisplayName = "��Ӫҵ������",
                    VoucherGroup = "",
                    AccountingCategory = "",
                    Preparer = "ϵͳ����",
                    Remark = "������Ŀ-��Ӫҵ������",
                    CreateBy = null,
                    CreateDateTime = OwHelper.WorldNow,
                    IsDelete = false
                });
                _Logger.LogInformation("��ӻ�����Ӫҵ�������Ŀ����");
                hasNewConfigurations = true;
            }
            // TAX_CODE - Ӧ��˰��
            if (!db.SubjectConfigurations.Any(c => c.Code == "TAX_CODE"))
            {
                db.SubjectConfigurations.Add(new SubjectConfiguration
                {
                    Id = Guid.Parse("{D3E4F5A6-7B8C-9D0E-1F2A-3B4C5D6E7F8A}"),
                    Code = "TAX_CODE",
                    SubjectNumber = "2171.01.05",
                    DisplayName = "Ӧ��˰��",
                    VoucherGroup = "",
                    AccountingCategory = "",
                    Preparer = "ϵͳ����",
                    Remark = "������Ŀ-Ӧ��˰��",
                    CreateBy = null,
                    CreateDateTime = OwHelper.WorldNow,
                    IsDelete = false
                });
                _Logger.LogInformation("��ӻ���Ӧ��˰���Ŀ����");
                hasNewConfigurations = true;
            }
            // BANK_CODE - ���д��
            if (!db.SubjectConfigurations.Any(c => c.Code == "BANK_CODE"))
            {
                db.SubjectConfigurations.Add(new SubjectConfiguration
                {
                    Id = Guid.Parse("{E4F5A6B7-8C9D-0E1F-2A3B-4C5D6E7F8A9B}"),
                    Code = "BANK_CODE",
                    SubjectNumber = "1002",
                    DisplayName = "���д��",
                    VoucherGroup = "",
                    AccountingCategory = "",
                    Preparer = "ϵͳ����",
                    Remark = "������Ŀ-���д��",
                    CreateBy = null,
                    CreateDateTime = OwHelper.WorldNow,
                    IsDelete = false
                });
                _Logger.LogInformation("��ӻ������д���Ŀ����");
                hasNewConfigurations = true;
            }
            // FPREPARE - �Ƶ���
            if (!db.SubjectConfigurations.Any(c => c.Code == "FPREPARE"))
            {
                db.SubjectConfigurations.Add(new SubjectConfiguration
                {
                    Id = Guid.Parse("{F7A8B9C0-1D2E-3F4A-5B6C-7D8E9F0A1B2C}"),
                    Code = "FPREPARE",
                    SubjectNumber = "", // �����޿�Ŀ
                    DisplayName = "�Ƶ���",
                    VoucherGroup = "",
                    AccountingCategory = "",
                    Preparer = "ϵͳ����",
                    Remark = "�Ƶ�������",
                    CreateBy = null,
                    CreateDateTime = OwHelper.WorldNow,
                    IsDelete = false
                });
                _Logger.LogInformation("����Ƶ�������");
                hasNewConfigurations = true;
            }
            #endregion

            #region A��Ӧ�ռ����Ŀ
            // ARAB_TOTAL - ������Ӧ��
            if (!db.SubjectConfigurations.Any(c => c.Code == "ARAB_TOTAL"))
            {
                db.SubjectConfigurations.Add(new SubjectConfiguration
                {
                    Id = Guid.Parse("{B7C8D9E0-1F2A-3B4C-5D6E-7F8A9B0C1D2E}"),
                    Code = "ARAB_TOTAL",
                    SubjectNumber = "531",
                    DisplayName = "������Ӧ��",
                    VoucherGroup = "ת",
                    AccountingCategory = "",
                    Preparer = "ϵͳ����",
                    Remark = "A��Ӧ�ռ���-��Ӧ�տ�Ŀ",
                    CreateBy = null,
                    CreateDateTime = OwHelper.WorldNow,
                    IsDelete = false
                });
                _Logger.LogInformation("���ARAB������Ӧ�տ�Ŀ����");
                hasNewConfigurations = true;
            }
            // ARAB_IN_CUS - ����Ӧ�չ���-�ͻ�
            if (!db.SubjectConfigurations.Any(c => c.Code == "ARAB_IN_CUS"))
            {
                db.SubjectConfigurations.Add(new SubjectConfiguration
                {
                    Id = Guid.Parse("{C8D9E0F1-2A3B-4C5D-6E7F-8A9B0C1D2E3F}"),
                    Code = "ARAB_IN_CUS",
                    SubjectNumber = "113.001.01",
                    DisplayName = "����Ӧ�չ���-�ͻ�",
                    VoucherGroup = "ת",
                    AccountingCategory = "�ͻ�",
                    Preparer = "ϵͳ����",
                    Remark = "A��Ӧ�ռ���-���ڿͻ���Ŀ",
                    CreateBy = null,
                    CreateDateTime = OwHelper.WorldNow,
                    IsDelete = false
                });
                _Logger.LogInformation("���ARAB����Ӧ�չ���-�ͻ���Ŀ����");
                hasNewConfigurations = true;
            }
            // ARAB_IN_TAR - ����Ӧ�չ���-��˰
            if (!db.SubjectConfigurations.Any(c => c.Code == "ARAB_IN_TAR"))
            {
                db.SubjectConfigurations.Add(new SubjectConfiguration
                {
                    Id = Guid.Parse("{D9E0F1A2-3B4C-5D6E-7F8A-9B0C1D2E3F4A}"),
                    Code = "ARAB_IN_TAR",
                    SubjectNumber = "113.001.02",
                    DisplayName = "����Ӧ�չ���-��˰",
                    VoucherGroup = "ת",
                    AccountingCategory = "�ͻ�",
                    Preparer = "ϵͳ����",
                    Remark = "A��Ӧ�ռ���-���ڹ�˰��Ŀ",
                    CreateBy = null,
                    CreateDateTime = OwHelper.WorldNow,
                    IsDelete = false
                });
                _Logger.LogInformation("���ARAB����Ӧ�չ���-��˰��Ŀ����");
                hasNewConfigurations = true;
            }
            // ARAB_OUT_CUS - ����Ӧ�չ���-�ͻ�
            if (!db.SubjectConfigurations.Any(c => c.Code == "ARAB_OUT_CUS"))
            {
                db.SubjectConfigurations.Add(new SubjectConfiguration
                {
                    Id = Guid.Parse("{E0F1A2B3-4C5D-6E7F-8A9B-0C1D2E3F4A5B}"),
                    Code = "ARAB_OUT_CUS",
                    SubjectNumber = "113.002",
                    DisplayName = "����Ӧ�չ���-�ͻ�",
                    VoucherGroup = "ת",
                    AccountingCategory = "�ͻ�",
                    Preparer = "ϵͳ����",
                    Remark = "A��Ӧ�ռ���-����ͻ���Ŀ",
                    CreateBy = null,
                    CreateDateTime = OwHelper.WorldNow,
                    IsDelete = false
                });
                _Logger.LogInformation("���ARAB����Ӧ�չ���-�ͻ���Ŀ����");
                hasNewConfigurations = true;
            }
            // ARAB_OUT_TAR - ����Ӧ�չ���-��˰
            if (!db.SubjectConfigurations.Any(c => c.Code == "ARAB_OUT_TAR"))
            {
                db.SubjectConfigurations.Add(new SubjectConfiguration
                {
                    Id = Guid.Parse("{F1A2B3C4-5D6E-7F8A-9B0C-1D2E3F4A5B6C}"),
                    Code = "ARAB_OUT_TAR",
                    SubjectNumber = "", // ������
                    DisplayName = "����Ӧ�չ���-��˰",
                    VoucherGroup = "ת",
                    AccountingCategory = "�ͻ�",
                    Preparer = "ϵͳ����",
                    Remark = "A��Ӧ�ռ���-�����˰��Ŀ���������Ŀ�ţ�",
                    CreateBy = null,
                    CreateDateTime = OwHelper.WorldNow,
                    IsDelete = false
                });
                _Logger.LogInformation("���ARAB����Ӧ�չ���-��˰��Ŀ����");
                hasNewConfigurations = true;
            }
            #endregion

            #region A��Ӧ�������Ŀ
            // APAB_TOTAL - ������Ӧ��
            if (!db.SubjectConfigurations.Any(c => c.Code == "APAB_TOTAL"))
            {
                db.SubjectConfigurations.Add(new SubjectConfiguration
                {
                    Id = Guid.Parse("{A2B3C4D5-6E7F-8A9B-0C1D-2E3F4A5B6C7D}"),
                    Code = "APAB_TOTAL",
                    SubjectNumber = "532",
                    DisplayName = "������Ӧ��",
                    VoucherGroup = "ת",
                    AccountingCategory = "",
                    Preparer = "ϵͳ����",
                    Remark = "A��Ӧ������-��Ӧ����Ŀ",
                    CreateBy = null,
                    CreateDateTime = OwHelper.WorldNow,
                    IsDelete = false
                });
                _Logger.LogInformation("���APAB������Ӧ����Ŀ����");
                hasNewConfigurations = true;
            }
            // APAB_IN_SUP - ����Ӧ������-��Ӧ��
            if (!db.SubjectConfigurations.Any(c => c.Code == "APAB_IN_SUP"))
            {
                db.SubjectConfigurations.Add(new SubjectConfiguration
                {
                    Id = Guid.Parse("{B3C4D5E6-7F8A-9B0C-1D2E-3F4A5B6C7D8E}"),
                    Code = "APAB_IN_SUP",
                    SubjectNumber = "203.001.01",
                    DisplayName = "����Ӧ������-��Ӧ��",
                    VoucherGroup = "ת",
                    AccountingCategory = "��Ӧ��",
                    Preparer = "ϵͳ����",
                    Remark = "A��Ӧ������-���ڹ�Ӧ�̿�Ŀ",
                    CreateBy = null,
                    CreateDateTime = OwHelper.WorldNow,
                    IsDelete = false
                });
                _Logger.LogInformation("���APAB����Ӧ������-��Ӧ�̿�Ŀ����");
                hasNewConfigurations = true;
            }
            // APAB_IN_TAR - ����Ӧ������-��˰
            if (!db.SubjectConfigurations.Any(c => c.Code == "APAB_IN_TAR"))
            {
                db.SubjectConfigurations.Add(new SubjectConfiguration
                {
                    Id = Guid.Parse("{C4D5E6F7-8A9B-0C1D-2E3F-4A5B6C7D8E9F}"),
                    Code = "APAB_IN_TAR",
                    SubjectNumber = "203.001.02",
                    DisplayName = "����Ӧ������-��˰",
                    VoucherGroup = "ת",
                    AccountingCategory = "��Ӧ��",
                    Preparer = "ϵͳ����",
                    Remark = "A��Ӧ������-���ڹ�˰��Ŀ",
                    CreateBy = null,
                    CreateDateTime = OwHelper.WorldNow,
                    IsDelete = false
                });
                _Logger.LogInformation("���APAB����Ӧ������-��˰��Ŀ����");
                hasNewConfigurations = true;
            }
            // APAB_OUT_SUP - ����Ӧ������-��Ӧ��
            if (!db.SubjectConfigurations.Any(c => c.Code == "APAB_OUT_SUP"))
            {
                db.SubjectConfigurations.Add(new SubjectConfiguration
                {
                    Id = Guid.Parse("{D5E6F7A8-9B0C-1D2E-3F4A-5B6C7D8E9F0A}"),
                    Code = "APAB_OUT_SUP",
                    SubjectNumber = "203.002",
                    DisplayName = "����Ӧ������-��Ӧ��",
                    VoucherGroup = "ת",
                    AccountingCategory = "��Ӧ��",
                    Preparer = "ϵͳ����",
                    Remark = "A��Ӧ������-���⹩Ӧ�̿�Ŀ",
                    CreateBy = null,
                    CreateDateTime = OwHelper.WorldNow,
                    IsDelete = false
                });
                _Logger.LogInformation("���APAB����Ӧ������-��Ӧ�̿�Ŀ����");
                hasNewConfigurations = true;
            }
            // APAB_OUT_TAR - ����Ӧ������-��˰
            if (!db.SubjectConfigurations.Any(c => c.Code == "APAB_OUT_TAR"))
            {
                db.SubjectConfigurations.Add(new SubjectConfiguration
                {
                    Id = Guid.Parse("{E6F7A8B9-0C1D-2E3F-4A5B-6C7D8E9F0A1B}"),
                    Code = "APAB_OUT_TAR",
                    SubjectNumber = "", // ������
                    DisplayName = "����Ӧ������-��˰",
                    VoucherGroup = "ת",
                    AccountingCategory = "��Ӧ��",
                    Preparer = "ϵͳ����",
                    Remark = "A��Ӧ������-�����˰��Ŀ���������Ŀ�ţ�",
                    CreateBy = null,
                    CreateDateTime = OwHelper.WorldNow,
                    IsDelete = false
                });
                _Logger.LogInformation("���APAB����Ӧ������-��˰��Ŀ����");
                hasNewConfigurations = true;
            }
            #endregion

            // ������������ã��򱣴����
            if (hasNewConfigurations)
            {
                db.SaveChanges();
                _Logger.LogInformation("��Ŀ���ó�ʼ����ɣ��ѱ���������������");
            }
            else
            {
                _Logger.LogDebug("���п�Ŀ���þ��Ѵ��ڣ�������ʼ��");
            }
            #endregion ��ʼ����Ŀ������Ϣ
        }
    }
}