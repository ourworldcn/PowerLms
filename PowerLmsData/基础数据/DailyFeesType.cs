using Microsoft.EntityFrameworkCore;
using PowerLms.Data;
using System;
using System.ComponentModel.DataAnnotations;

namespace PowerLms.Data
{
    /// <summary>
    /// �ճ����������ֵ䡣����OA�������뵥�ķ��÷��࣬����÷ѡ��칫�ѡ��绰�ѵȡ�
    /// ����Ӫҵ��������ࣨFeesType���ֿ�����
    /// Code�Ƿ��ô��룬DisplayName�Ƿ������ƣ�ShortName��Ӣ�����ƣ�Remark�Ǹ���˵����
    /// </summary>
    [Comment("�ճ����������ֵ�")]
    public class DailyFeesType : NamedSpecialDataDicBase, IMarkDelete, ICloneable
    {
        /// <summary>
        /// ��ƿ�Ŀ���롣���ڲ������ͽ�����롣
        /// </summary>
        [Comment("��ƿ�Ŀ����")]
        [Unicode(false), MaxLength(32)]
        public string SubjectCode { get; set; }
    }

    /// <summary>
    /// �ճ����������ֵ���չ������
    /// </summary>
    public static class DailyFeesTypeExtensions
    {
        /// <summary>
        /// ��ȡ���������������ʾ���ơ�
        /// </summary>
        /// <param name="dailyFeesType">�ճ���������</param>
        /// <returns>��ʽ��"����-��ʾ����"</returns>
        public static string GetFullDisplayName(this DailyFeesType dailyFeesType)
        {
            if (string.IsNullOrEmpty(dailyFeesType.Code) || string.IsNullOrEmpty(dailyFeesType.DisplayName))
                return dailyFeesType.DisplayName ?? dailyFeesType.Code ?? string.Empty;

            return $"{dailyFeesType.Code}-{dailyFeesType.DisplayName}";
        }
    }
}