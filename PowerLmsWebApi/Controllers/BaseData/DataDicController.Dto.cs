using PowerLms.Data;
using PowerLmsWebApi.Dto;

namespace PowerLmsWebApi.Controllers
{
    #region �ճ������������

    /// <summary>
    /// �ָ��ճ����������¼�Ĺ��ܲ�����װ�ࡣ
    /// </summary>
    public class RestoreDailyFeesTypeParamsDto : RestoreParamsDtoBase
    {
    }

    /// <summary>
    /// �ָ��ճ����������¼�Ĺ��ܷ���ֵ��װ�ࡣ
    /// </summary>
    public class RestoreDailyFeesTypeReturnDto : RestoreReturnDtoBase
    {
    }

    /// <summary>
    /// ɾ���ճ����������¼�Ĺ��ܲ�����װ�ࡣ
    /// </summary>
    public class RemoveDailyFeesTypeParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// ɾ���ճ����������¼�Ĺ��ܷ���ֵ��װ�ࡣ
    /// </summary>
    public class RemoveDailyFeesTypeReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// �޸��ճ����������¼�Ĺ��ܲ�����װ�ࡣ
    /// </summary>
    public class ModifyDailyFeesTypeParamsDto : ModifyParamsDtoBase<DailyFeesType>
    {
    }

    /// <summary>
    /// �޸��ճ����������¼�Ĺ��ܷ���ֵ��װ�ࡣ
    /// </summary>
    public class ModifyDailyFeesTypeReturnDto : ModifyReturnDtoBase
    {
    }

    /// <summary>
    /// �����ճ����������¼�Ĺ��ܲ�����װ�ࡣ
    /// </summary>
    public class AddDailyFeesTypeParamsDto : AddParamsDtoBase<DailyFeesType>
    {
        /// <summary>
        /// �Ƿ�ͬ�����ӹ�˾/��֯���������ڳ��ܸ��Ƶ������ֵ��У������̻�����Ա��ͬΪ��ͨ�û���
        /// </summary>
        public bool CopyToChildren { get; set; }
    }

    /// <summary>
    /// �����ճ����������¼�Ĺ��ܷ���ֵ��װ�ࡣ
    /// </summary>
    public class AddDailyFeesTypeReturnDto : AddReturnDtoBase
    {
    }

    /// <summary>
    /// ��ȡ�ճ���������Ĺ��ܷ���ֵ��װ�ࡣ
    /// </summary>
    public class GetAllDailyFeesTypeReturnDto : PagingReturnDtoBase<DailyFeesType>
    {
    }

    #endregion �ճ������������
}