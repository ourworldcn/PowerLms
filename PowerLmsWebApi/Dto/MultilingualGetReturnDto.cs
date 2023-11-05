using PowerLmsServer.EfData;

namespace PowerLmsWebApi.Dto
{
    /// <summary>
    /// 获取多语言资源的返回值封装类。
    /// </summary>
    public class MultilingualGetReturnDto
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public MultilingualGetReturnDto()
        {

        }

        /// <summary>
        /// 多语言资源的集合。
        /// </summary>
        public List<Multilingual> Multilinguals { get; set; } = new List<Multilingual>();
    }

    /// <summary>
    /// 修改或追加多语言资源功能的参数封装类。
    /// </summary>
    public class MultilingualSetParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 追加或修改的内容。
        /// 若Id不存在则追加，若已经存在则修改。
        /// </summary>
        public List<Multilingual> AddOrUpdateDatas { get; set; } = new List<Multilingual>();

        /// <summary>
        /// 要删除的多语言资源的Id集合。若要删除必须登录。
        /// </summary>
        public List<Guid> DeleteIds { get; set; } = new List<Guid> { };
    }

    /// <summary>
    /// 修改或追加多语言资源功能的返回值封装类。
    /// </summary>
    public class MultilingualSetReturnDto : ReturnDtoBase
    {
    }

    /// <summary>
    /// 获取语言字典表功能返回值封装类。
    /// </summary>
    public class GetLanguageDataDicReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 语言数据字典。
        /// </summary>
        public List<LanguageDataDic> Results { get; set; } = new List<LanguageDataDic>();
    }


}
