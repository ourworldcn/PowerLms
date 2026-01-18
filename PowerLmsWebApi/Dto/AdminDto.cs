using PowerLms.Data;
namespace PowerLmsWebApi.Dto
{
    /// <summary>
    /// 获取系统资源列表功能的返回值封装类。
    /// </summary>
    public class GetSystemResourceReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 系统资源集合。
        /// </summary>
        public List<SystemResource> Resources { get; set; } = new List<SystemResource>();
    }
    /// <summary>
    /// 通用的数据字典功能返回值封装类。
    /// </summary>
    public class GetDataDicReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 返回的集合。
        /// </summary>
        public List<object> Result { get; set; } = new List<object>();
    }
    /// <summary>
    /// 通用的导入数据字典功能返回值封装类。
    /// </summary>
    public class ImportDataDicReturnDto : ReturnDtoBase
    {
    }
}
