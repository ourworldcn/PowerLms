using PowerLmsWebApi.Dto;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// ���Ʊ�������ܵĲ�����װ�ࡣ
    /// </summary>
    public class CopyJobNumberRuleParamsDto : TokenDtoBase
    {
        /// <summary>
        /// ָ��Ҫ���ƵĹ����Code����ļ��ϡ�Ϊ����û���ֵ�ᱻ���ơ�
        /// </summary>
        public List<string> Codes { get; set; } = new List<string>();

        /// <summary>
        /// Ŀ����֯����Id��
        /// </summary>
        public Guid DestOrgId { get; set; }
    }

    /// <summary>
    /// ���Ʊ�������ܵķ���ֵ��װ�ࡣ
    /// </summary>
    public class CopyJobNumberRuleReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// �µı�������Id���ϡ�
        /// </summary>
        public List<Guid> Result = new List<Guid>();
    }

    /// <summary>
    /// ��ָ���ı����������һ���µ���������Ĺ��ܲ�����װ�ࡣ
    /// </summary>
    public class GeneratedOtherNumberParamsDto : TokenDtoBase
    {
        /// <summary>
        /// �����Id.
        /// </summary>
        public Guid RuleId { get; set; }
    }

    /// <summary>
    /// ��ָ���ı����������һ���µ���������Ĺ��ܷ���ֵ��װ�ࡣ
    /// </summary>
    public class GeneratedOtherNumberReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// ���ص�ҵ���롣
        /// </summary>
        public string Result { get; set; }
    }

    /// <summary>
    /// ��ָ���ı����������һ���µı���Ĺ��ܷ���ֵ��װ�ࡣ
    /// </summary>
    public class GeneratedJobNumberReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// ���ص�ҵ���롣
        /// </summary>
        public string Result { get; set; }
    }

    /// <summary>
    /// ��ָ���ı����������һ���µı���Ĺ��ܲ�����װ�ࡣ
    /// </summary>
    public class GeneratedJobNumberParamsDto : TokenDtoBase
    {
        /// <summary>
        /// �����Id.
        /// </summary>
        public Guid RuleId { get; set; }
    }
}