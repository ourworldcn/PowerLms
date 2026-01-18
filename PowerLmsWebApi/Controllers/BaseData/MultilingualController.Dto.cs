using PowerLms.Data;
using PowerLmsServer.EfData;
using PowerLmsWebApi.Dto;
namespace PowerLmsWebApi.Controllers
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
        /// 追加或修改的数据。
        /// 若联合主键(Key, LanguageTag)不存在则追加，若已经存在则修改。
        /// </summary>
        public List<Multilingual> AddOrUpdateDatas { get; set; } = new List<Multilingual>();
        /// <summary>
        /// 要删除的多语言资源集合。通过联合主键(Key, LanguageTag)进行匹配删除。
        /// </summary>
        public List<Multilingual> DeleteDatas { get; set; } = new List<Multilingual>();
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