using PowerLms.Data;
using PowerLmsWebApi.Dto;
namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// 删除机构父子关系的功能参数封装类。
    /// </summary>
    public class RemoveOrgRelationParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 父Id。
        /// </summary>
        public Guid ParentId { get; set; }
        /// <summary>
        /// 子Id。
        /// </summary>
        public Guid ChildId { get; set; }
    }
    /// <summary>
    /// 删除机构父子关系的功能返回值封装类。
    /// </summary>
    public class RemoveOrgRelationReturnDto : ReturnDtoBase
    {
    }
    /// <summary>
    /// 增加机构父子关系的功能参数封装类。
    /// </summary>
    public class AddOrgRelationParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 父Id。
        /// </summary>
        public Guid ParentId { get; set; }
        /// <summary>
        /// 子Id。
        /// </summary>
        public Guid ChildId { get; set; }
    }
    /// <summary>
    /// 增加机构父子关系的功能返回值封装类。
    /// </summary>
    public class AddOrgRelationReturnDto : ReturnDtoBase
    {
    }
    #region 开户行信息
    /// <summary>
    /// 标记删除开户行信息功能的参数封装类。
    /// </summary>
    public class RemoveBankInfoParamsDto : RemoveParamsDtoBase
    {
    }
    /// <summary>
    /// 标记删除开户行信息功能的返回值封装类。
    /// </summary>
    public class RemoveBankInfoReturnDto : RemoveReturnDtoBase
    {
    }
    /// <summary>
    /// 获取所有开户行信息功能的返回值封装类。
    /// </summary>
    public class GetAllBankInfoReturnDto : PagingReturnDtoBase<BankInfo>
    {
    }
    /// <summary>
    /// 增加新开户行信息功能参数封装类。
    /// </summary>
    public class AddBankInfoParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 新开户行信息信息。其中Id可以是任何值，返回时会指定新值。
        /// </summary>
        public BankInfo BankInfo { get; set; }
    }
    /// <summary>
    /// 增加新开户行信息功能返回值封装类。
    /// </summary>
    public class AddBankInfoReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 如果成功添加，这里返回新开户行信息的Id。
        /// </summary>
        public Guid Id { get; set; }
    }
    /// <summary>
    /// 修改开户行信息信息功能参数封装类。
    /// </summary>
    public class ModifyBankInfoParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 开户行信息数据。
        /// </summary>
        public BankInfo BankInfo { get; set; }
    }
    /// <summary>
    /// 修改开户行信息信息功能返回值封装类。
    /// </summary>
    public class ModifyBankInfoReturnDto : ReturnDtoBase
    {
    }
    #endregion 开户行信息
    /// <summary>
    /// 初始化机构的功能参数封装类。
    /// </summary>
    public class CopyDataDicParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 初始化机构的Id。
        /// </summary>
        public Guid Id { get; set; }
    }
    /// <summary>
    /// 初始化机构的功能返回值封装类。
    /// </summary>
    public class CopyDataDicReturnDto : ReturnDtoBase
    {
    }
    #region 用户和商户/组织机构的所属关系的CRUD
    /// <summary>
    /// 获取用户和商户/组织机构的所属关系返回值封装类。
    /// </summary>
    public class GetAllAccountPlOrganizationReturnDto : PagingReturnDtoBase<AccountPlOrganization>
    {
    }
    /// <summary>
    /// 删除用户和商户/组织机构的所属关系的功能参数封装类。
    /// </summary>
    public class RemoveAccountPlOrganizationParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 用户的Id。
        /// </summary>
        public Guid UserId { get; set; }
        /// <summary>
        /// 商户/组织机构的Id。
        /// </summary>
        public Guid OrgId { get; set; }
    }
    /// <summary>
    /// 删除用户和商户/组织机构的所属关系的功能返回值封装类。
    /// </summary>
    public class RemoveAccountPlOrganizationReturnDto : RemoveReturnDtoBase
    {
    }
    /// <summary>
    /// 增加用户和商户/组织机构的所属关系的功能参数封装类，
    /// </summary>
    public class AddAccountPlOrganizationParamsDto : AddParamsDtoBase<AccountPlOrganization>
    {
    }
    /// <summary>
    /// 增加用户和商户/组织机构的所属关系的功能返回值封装类。
    /// </summary>
    public class AddAccountPlOrganizationReturnDto : ReturnDtoBase
    {
    }
    /// <summary>
    /// 获取用户和商户/组织机构的所属关系功能的返回值封装类。
    /// </summary>
    public class GetAllAccountReturnDto : PagingReturnDtoBase<Account>
    {
    }
    #endregion 用户和商户/组织机构的所属关系的CRUD
    /// <summary>
    /// 通过组织机构Id获取所属的商户Id的功能返回值封装类。
    /// </summary>
    public class GetMerchantIdReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 商户Id。注意，理论上允许组织机构不属于任何商户，则此处返回null。
        /// </summary>
        public Guid? Result { get; set; }
    }
    /// <summary>
    /// 删除组织机构的功能参数封装类。
    /// </summary>
    public class RemoveOrgParamsDto : RemoveParamsDtoBase
    {
    }
    /// <summary>
    /// 删除组织机构的功能返回值封装类。
    /// </summary>
    public class RemoveOrgReturnDto : RemoveReturnDtoBase
    {
    }
    /// <summary>
    /// 增加一个组织机构的功能参数封装类。
    /// </summary>
    public class AddOrgParamsDto : AddParamsDtoBase<PlOrganization>
    {
        /// <summary>
        /// 是否给新加入的机构复制一份完整的数据字典。
        /// </summary>
        public bool IsCopyDataDic { get; set; }
    }
    /// <summary>
    /// 增加一个组织机构的功能返回值封装类。
    /// </summary>
    public class AddOrgReturnDto : AddReturnDtoBase
    {
    }
    /// <summary>
    /// 修改组织机构功能的参数封装类。
    /// </summary>
    public class ModifyOrgParamsDto : ModifyParamsDtoBase<PlOrganization>
    {
    }
    /// <summary>
    /// 修改组织机构功能的返回值封装类。
    /// </summary>
    public class ModifyOrgReturnDto : ModifyReturnDtoBase
    {
    }
}