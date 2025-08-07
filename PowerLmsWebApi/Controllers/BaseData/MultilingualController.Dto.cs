using PowerLms.Data;
using PowerLmsServer.EfData;
using PowerLmsWebApi.Dto;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// ��ȡ��������Դ�ķ���ֵ��װ�ࡣ
    /// </summary>
    public class MultilingualGetReturnDto
    {
        /// <summary>
        /// ���캯����
        /// </summary>
        public MultilingualGetReturnDto()
        {

        }

        /// <summary>
        /// ��������Դ�ļ��ϡ�
        /// </summary>
        public List<Multilingual> Multilinguals { get; set; } = new List<Multilingual>();
    }

    /// <summary>
    /// �޸Ļ�׷�Ӷ�������Դ���ܵĲ�����װ�ࡣ
    /// </summary>
    public class MultilingualSetParamsDto : TokenDtoBase
    {
        /// <summary>
        /// ׷�ӻ��޸ĵ����ݡ�
        /// ��Id��������׷�ӣ����Ѿ��������޸ġ�
        /// </summary>
        public List<Multilingual> AddOrUpdateDatas { get; set; } = new List<Multilingual>();

        /// <summary>
        /// Ҫɾ���Ķ�������Դ��Id���ϡ���Ҫɾ�������¼��
        /// </summary>
        public List<Guid> DeleteIds { get; set; } = new List<Guid> { };
    }

    /// <summary>
    /// �޸Ļ�׷�Ӷ�������Դ���ܵķ���ֵ��װ�ࡣ
    /// </summary>
    public class MultilingualSetReturnDto : ReturnDtoBase
    {
    }

    /// <summary>
    /// ��ȡ�����ֵ���ܷ���ֵ��װ�ࡣ
    /// </summary>
    public class GetLanguageDataDicReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// ���������ֵ䡣
        /// </summary>
        public List<LanguageDataDic> Results { get; set; } = new List<LanguageDataDic>();
    }
}