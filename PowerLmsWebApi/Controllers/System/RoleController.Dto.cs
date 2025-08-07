using PowerLms.Data;
using PowerLmsWebApi.Dto;

namespace PowerLmsWebApi.Controllers
{
    #region ��ɫ
    /// <summary>
    /// ���ɾ����ɫ���ܵĲ�����װ�ࡣ
    /// </summary>
    public class RemovePlRoleParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// ���ɾ����ɫ���ܵķ���ֵ��װ�ࡣ
    /// </summary>
    public class RemovePlRoleReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// ��ȡ���н�ɫ���ܵķ���ֵ��װ�ࡣ
    /// </summary>
    public class GetAllPlRoleReturnDto : PagingReturnDtoBase<PlRole>
    {
    }

    /// <summary>
    /// �����½�ɫ���ܲ�����װ�ࡣʡ��PlRole.OrgId�Զ����Ϊ�����������̻�Id��
    /// </summary>
    public class AddPlRoleParamsDto : AddParamsDtoBase<PlRole>
    {
    }

    /// <summary>
    /// �����½�ɫ���ܷ���ֵ��װ�ࡣ
    /// </summary>
    public class AddPlRoleReturnDto : AddReturnDtoBase
    {
    }

    /// <summary>
    /// �޸Ľ�ɫ��Ϣ���ܲ�����װ�ࡣ
    /// </summary>
    public class ModifyPlRoleParamsDto : TokenDtoBase
    {
        /// <summary>
        /// ��ɫ���ݡ�
        /// </summary>
        public PlRole PlRole { get; set; }
    }

    /// <summary>
    /// �޸Ľ�ɫ��Ϣ���ܷ���ֵ��װ�ࡣ
    /// </summary>
    public class ModifyPlRoleReturnDto : ReturnDtoBase
    {
    }
    #endregion ��ɫ
}