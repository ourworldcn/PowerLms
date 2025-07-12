using PowerLms.Data;
using PowerLmsWebApi.Dto;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// ������־���ܵĲ�����װ�ࡣ
    /// </summary>
    public class ExportLoggerParamsDto : TokenDtoBase
    {
        /// <summary>
        /// ָ�������ļ������֡������Ժ�·����
        /// </summary>
        public string FileName { get; set; }
    }

    /// <summary>
    /// ������־���ܵķ���ֵ��װ�ࡣ
    /// </summary>
    public class ExportLoggerReturnDto
    {
    }

    /// <summary>
    /// �����־��ܵĲ�����װ�ࡣ
    /// </summary>
    public class RemoveAllLoggerItemParamsDto : TokenDtoBase
    {
    }

    /// <summary>
    /// �����־��ܵķ���ֵ��װ�ࡣ
    /// </summary>
    public class RemoveAllLoggerItemReturnDto : ReturnDtoBase
    {
    }

    /// <summary>
    /// ������־��ܵĲ�����װ�ࡣ
    /// </summary>
    public class GetAllAppLogItemParamsDto
    {
    }

    /// <summary>
    /// ������־��ܵķ���ֵ��װ�ࡣ
    /// </summary>
    public class GetAllAppLogItemReturnDto : PagingReturnDtoBase<OwAppLogView>
    {
    }

    /// <summary>
    /// ׷��һ����־��ܵĲ�����װ�ࡣ
    /// </summary>
    public class AddLoggerItemParamsDto
    {
    }

    /// <summary>
    /// ׷��һ����־��ܵķ���ֵ��װ�ࡣ
    /// </summary>
    public class AddLoggerItemReturnDto
    {
    }
}