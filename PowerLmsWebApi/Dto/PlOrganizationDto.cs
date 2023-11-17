
using PowerLms.Data;

namespace PowerLmsWebApi.Dto
{
    /// <summary>
    /// 获取组织机构功能参数封装类。
    /// </summary>
    public class GetOrgParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 根组织机构的Id。
        /// </summary>
        public Guid? RootId { get; set; }

        /// <summary>
        /// 是否包含子机构。
        /// </summary>
        public bool IncludeChildren { get; set; }
    }

    /// <summary>
    /// 获取组织机构功能返回值封装类。
    /// </summary>
    public class GetOrgReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 返回的组机构。
        /// </summary>
        public PlOrganization Result { get; set; }
    }

}