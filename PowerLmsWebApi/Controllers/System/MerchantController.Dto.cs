using PowerLms.Data;
using PowerLmsWebApi.Dto;
namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// 标记删除商户功能的参数封装类。
    /// </summary>
    public class RemoveMerchantParamsDto : RemoveParamsDtoBase
    {
    }
    /// <summary>
    /// 标记删除商户功能的返回值封装类。
    /// </summary>
    public class RemoveMerchantReturnDto : RemoveReturnDtoBase
    {
    }
    /// <summary>
    /// 初始化商户的功能参数封装类。
    /// </summary>
    public class InitializeMerchantParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 初始化商户的Id。
        /// </summary>
        public Guid Id { get; set; }
    }
    /// <summary>
    /// 初始化商户的功能返回值封装类。
    /// </summary>
    public class InitializeMerchantReturnDto : ReturnDtoBase
    {
    }
    /// <summary>
    /// 获取所有商户功能的返回值封装类。
    /// </summary>
    public class GetAllMerchantReturnDto : PagingReturnDtoBase<PlMerchant>
    {
    }
    /// <summary>
    /// 增加新商户功能参数封装类。
    /// </summary>
    public class AddMerchantParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 新商户信息。其中Id可以是任何值，返回时会指定新值。
        /// </summary>
        public PlMerchant Merchant { get; set; }
    }
    /// <summary>
    /// 增加新商户功能返回值封装类。
    /// </summary>
    public class AddMerchantReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 如果成功添加，这里返回新商户的Id。
        /// </summary>
        public Guid Id { get; set; }
    }
    /// <summary>
    /// 修改商户信息功能参数封装类。
    /// </summary>
    public class ModifyMerchantParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 商户数据。
        /// </summary>
        public PlMerchant Merchant { get; set; }
    }
    /// <summary>
    /// 修改商户信息功能返回值封装类。
    /// </summary>
    public class ModifyMerchantReturnDto : ReturnDtoBase
    {
    }
    /// <summary>
    /// 获取指定机构下（含自身和子机构）的所有用户对象功能的参数封装类。
    /// </summary>
    public class GetUsersByOrgIdsParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 商户或机构Id的集合。
        /// </summary>
        public List<Guid> OrgOrMerchantIds { get; set; } = new List<Guid>();
    }
    /// <summary>
    /// 获取指定机构下（含自身和子机构）的所有用户对象功能的返回值封装类。
    /// </summary>
    public class GetUsersByOrgIdsReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 所有用户对象。无重复。
        /// </summary>
        public List<Account> Result { get; set; } = new List<Account>();
    }
}