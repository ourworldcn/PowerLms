using PowerLmsWebApi.Dto;
namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// 复制编码规则功能的参数封装类。
    /// </summary>
    public class CopyJobNumberRuleParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 指定要复制的规则的Code代码的集合。为空则没有字典会被复制。
        /// </summary>
        public List<string> Codes { get; set; } = new List<string>();
        /// <summary>
        /// 目标组织机构Id。
        /// </summary>
        public Guid DestOrgId { get; set; }
    }
    /// <summary>
    /// 复制编码规则功能的返回值封装类。
    /// </summary>
    public class CopyJobNumberRuleReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 新的编码规则的Id集合。
        /// </summary>
        public List<Guid> Result = new List<Guid>();
    }
    /// <summary>
    /// 用指定的编码规则生成一个新的其它编码的功能参数封装类。
    /// </summary>
    public class GeneratedOtherNumberParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 规则的Id.
        /// </summary>
        public Guid RuleId { get; set; }
    }
    /// <summary>
    /// 用指定的编码规则生成一个新的其它编码的功能返回值封装类。
    /// </summary>
    public class GeneratedOtherNumberReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 返回的业务码。
        /// </summary>
        public string Result { get; set; }
    }
    /// <summary>
    /// 用指定的编码规则生成一个新的编码的功能返回值封装类。
    /// </summary>
    public class GeneratedJobNumberReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 返回的业务码。
        /// </summary>
        public string Result { get; set; }
    }
    /// <summary>
    /// 用指定的编码规则生成一个新的编码的功能参数封装类。
    /// </summary>
    public class GeneratedJobNumberParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 规则的Id.
        /// </summary>
        public Guid RuleId { get; set; }
    }
}