/*
 * OwAppLogView.cs
 * ��Ȩ���� (c) 2023 PowerLms. ��������Ȩ����
 * ���ļ�����Ӧ����־��ͼʵ�嶨�塣
 * ����: OW
 * ��������: 2023-12-11
 * �޸�����: 2025-03-06
 */

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OW.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace PowerLms.Data
{
    /// <summary>
    /// Ӧ����־��ͼ��������OwAppLogStore��OwAppLogItemStore�����ݡ�
    /// </summary>
    [Keyless]
    public class OwAppLogView
    {
        /// <summary>
        /// Ӧ����־��ϸ��ϢId��
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// ��־����ID��
        /// </summary>
        public Guid TypeId { get; set; }

        /// <summary>
        /// ��־����
        /// Trace (0)����������ϸ��Ϣ����־�����ܰ����������ݣ�Ĭ�Ͻ��ã���Ӧ���������������á�
        /// Debug (1)�����ڿ��������еĽ���ʽ������־�������Ե������õ���Ϣ���޳��ڼ�ֵ��
        /// Information (2)������Ӧ�ó��򳣹�������־�����г��ڼ�ֵ��
        /// Warning (3)��ͻ����ʾ�쳣�������¼�����־�����ᵼ��Ӧ�ó���ֹͣ��
        /// Error (4)����ǰִ��������϶�ֹͣʱ����־��ָʾ��ǰ��еĹ��ϡ�
        /// Critical (5)���������ɻָ���Ӧ�ó���/ϵͳ��������Ҫ����ע��������Թ��ϵ���־��
        /// None (6)��������д����־��Ϣ��ָ����־��¼���Ӧд���κ���Ϣ����
        /// </summary>
        public LogLevel LogLevel { get; set; }

        /// <summary>
        /// ��ʽ�ַ�������"�û�{LoginName}��¼�ɹ�"��
        /// </summary>
        public string FormatString { get; set; }
        
        /// <summary>
        /// Json�ַ������洢�����ֵ䡣
        /// </summary>
        public string ParamstersJson { get; set; }
        
        /// <summary>
        /// ����Ŀ��¼�Ķ���Ķ�������Ϣ��
        /// </summary>
        public byte[] ExtraBytes { get; set; }
        
        /// <summary>
        /// ����־��Ŀ�Ĵ���UTCʱ�䡣
        /// </summary>
        public DateTime CreateUtc { get; set; }
        
        /// <summary>
        /// �����̻�Id��
        /// </summary>
        public Guid? MerchantId { get; set; }
        
        /// <summary>
        /// �����˵�¼����
        /// </summary>
        public string LoginName { get; set; }
        
        /// <summary>
        /// ��˾���ơ�
        /// </summary>
        public string CompanyName { get; set; }
        
        /// <summary>
        /// ��������ʾ���ơ�
        /// </summary>
        public string DisplayName { get; set; }
        
        /// <summary>
        /// ����IP��ַ��
        /// </summary>
        public string OperationIp { get; set; }
        
        /// <summary>
        /// �������͡�
        /// </summary>
        public string OperationType { get; set; }
        
        /// <summary>
        /// �ͻ������͡�
        /// </summary>
        public string ClientType { get; set; }

        private Dictionary<string, string> _ParamsDic;

        /// <summary>
        /// �����ֵ䡣
        /// </summary>
        [NotMapped]
        public Dictionary<string, string> ParamsDic
        {
            get
            {
                if (_ParamsDic == null && !string.IsNullOrEmpty(ParamstersJson))
                {
                    _ParamsDic = JsonSerializer.Deserialize<Dictionary<string, string>>(ParamstersJson);
                    _ParamsDic[nameof(CreateUtc)] = CreateUtc.ToString("s");
                }
                return _ParamsDic ?? new Dictionary<string, string>();
            }
        }

        private string _Message;

        /// <summary>
        /// ��־��Ŀ���ַ�����Ϣ��
        /// </summary>
        [NotMapped]
        public string Message
        {
            get
            {
                if (_Message is null)
                {
                    _Message = string.IsNullOrEmpty(FormatString) 
                        ? ParamstersJson 
                        : FormatString.FormatWith(ParamsDic);
                }
                return _Message;
            }
        }
    }
}
