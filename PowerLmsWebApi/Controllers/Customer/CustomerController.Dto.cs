using PowerLms.Data;
using PowerLmsWebApi.Dto;
using System.ComponentModel.DataAnnotations;

namespace PowerLmsWebApi.Controllers
{
    #region 装货地址
    /// <summary>
    /// 标记删除装货地址功能的参数封装类。
    /// </summary>
    public class RemovePlLoadingAddrParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 标记删除装货地址功能的返回值封装类。
    /// </summary>
    public class RemovePlLoadingAddrReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 获取所有装货地址功能的返回值封装类。
    /// </summary>
    public class GetAllPlLoadingAddrReturnDto : PagingReturnDtoBase<PlLoadingAddr>
    {
    }

    /// <summary>
    /// 增加新装货地址功能参数封装类。
    /// </summary>
    public class AddPlLoadingAddrParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 新装货地址信息。其中Id可以是任何值，返回时会指定新值。
        /// </summary>
        public PlLoadingAddr PlLoadingAddr { get; set; }
    }

    /// <summary>
    /// 增加新装货地址功能返回值封装类。
    /// </summary>
    public class AddPlLoadingAddrReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 如果成功添加，这里返回新装货地址的Id。
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 修改装货地址信息功能参数封装类。
    /// </summary>
    public class ModifyPlLoadingAddrParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 装货地址数据。
        /// </summary>
        public PlLoadingAddr PlLoadingAddr { get; set; }
    }

    /// <summary>
    /// 修改装货地址信息功能返回值封装类。
    /// </summary>
    public class ModifyPlLoadingAddrReturnDto : ReturnDtoBase
    {
    }
    #endregion 装货地址

    #region 黑名单
    /// <summary>
    /// 获取所有黑名单功能的返回值封装类。
    /// </summary>
    public class GetAllCustomerBlacklistReturnDto : PagingReturnDtoBase<CustomerBlacklist>
    {
    }

    /// <summary>
    /// 增加新黑名单功能参数封装类。
    /// </summary>
    public class AddCustomerBlacklistParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 新黑名单信息。其中Id可以是任何值，返回时会指定新值。
        /// </summary>
        public CustomerBlacklist CustomerBlacklist { get; set; }
    }

    /// <summary>
    /// 增加新黑名单功能返回值封装类。
    /// </summary>
    public class AddCustomerBlacklistReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 如果成功添加，这里返回新黑名单的Id。
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 删除黑名单功能参数封装类。
    /// </summary>
    public class RemoveCustomerBlacklistParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 指定的是客户Id(CustomerId)。
        /// </summary>
        public Guid CustomerId { get; set; }

        /// <summary>
        /// 删除的类型。3=移除超额，4=移除超期。
        /// </summary>
        [Range(3, 4)]
        public byte Kind { get; set; }

        /// <summary>
        /// 删除实体的注释。
        /// </summary>
        public string Remark { get; set; }

    }

    /// <summary>
    /// 删除新黑名单功能返回值封装类。
    /// </summary>
    public class RemoveCustomerBlacklistReturnDto
    {
        /// <summary>
        /// 新增的"冲红"实体。
        /// </summary>
        public CustomerBlacklist Result { get; set; }
    }

    #endregion 黑名单

    #region 提单
    /// <summary>
    /// 标记删除提单功能的参数封装类。
    /// </summary>
    public class RemovePlTidanParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 标记删除提单功能的返回值封装类。
    /// </summary>
    public class RemovePlTidanReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 获取所有提单功能的返回值封装类。
    /// </summary>
    public class GetAllPlTidanReturnDto : PagingReturnDtoBase<PlTidan>
    {
    }

    /// <summary>
    /// 增加新提单功能参数封装类。
    /// </summary>
    public class AddPlTidanParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 新提单信息。其中Id可以是任何值，返回时会指定新值。
        /// </summary>
        public PlTidan PlTidan { get; set; }
    }

    /// <summary>
    /// 增加新提单功能返回值封装类。
    /// </summary>
    public class AddPlTidanReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 如果成功添加，这里返回新提单的Id。
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 修改提单信息功能参数封装类。
    /// </summary>
    public class ModifyPlTidanParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 提单数据。
        /// </summary>
        public PlTidan PlTidan { get; set; }
    }

    /// <summary>
    /// 修改提单信息功能返回值封装类。
    /// </summary>
    public class ModifyPlTidanReturnDto : ReturnDtoBase
    {
    }
    #endregion 提单

    #region 开票信息
    /// <summary>
    /// 标记删除开票信息功能的参数封装类。
    /// </summary>
    public class RemovePlTaxInfoParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 标记删除开票信息功能的返回值封装类。
    /// </summary>
    public class RemovePlTaxInfoReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 获取所有开票信息功能的返回值封装类。
    /// </summary>
    public class GetAllPlTaxInfoReturnDto : PagingReturnDtoBase<PlTaxInfo>
    {
    }

    /// <summary>
    /// 增加新开票信息功能参数封装类。
    /// </summary>
    public class AddPlTaxInfoParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 新开票信息信息。其中Id可以是任何值，返回时会指定新值。
        /// </summary>
        public PlTaxInfo PlTaxInfo { get; set; }
    }

    /// <summary>
    /// 增加新开票信息功能返回值封装类。
    /// </summary>
    public class AddPlTaxInfoReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 如果成功添加，这里返回新开票信息的Id。
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 修改开票信息信息功能参数封装类。
    /// </summary>
    public class ModifyPlTaxInfoParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 开票信息数据。
        /// </summary>
        public PlTaxInfo PlTaxInfo { get; set; }
    }

    /// <summary>
    /// 修改开票信息信息功能返回值封装类。
    /// </summary>
    public class ModifyPlTaxInfoReturnDto : ReturnDtoBase
    {
    }
    #endregion 开票信息

    #region 业务负责人的所属关系的CRUD
    /// <summary>
    /// 获取业务负责人的所属关系返回值封装类。
    /// </summary>
    public class GetAllPlBusinessHeaderReturnDto : PagingReturnDtoBase<PlBusinessHeader>
    {
    }

    /// <summary>
    /// 删除业务负责人的所属关系的功能参数封装类。
    /// </summary>
    public class RemovePlBusinessHeaderParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 用户的Id。
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// 商户/组织机构的Id。
        /// </summary>
        public Guid CustomerId { get; set; }

        /// <summary>
        /// 负责的业务Id。连接业务种类字典。
        /// </summary>
        public Guid OrderTypeId { get; set; }

    }

    /// <summary>
    /// 删除业务负责人的所属关系的功能返回值封装类。
    /// </summary>
    public class RemovePlBusinessHeaderReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 增加业务负责人的所属关系的功能参数封装类，
    /// </summary>
    public class AddPlBusinessHeaderParamsDto : AddParamsDtoBase<PlBusinessHeader>
    {
    }

    /// <summary>
    /// 增加业务负责人的所属关系的功能返回值封装类。
    /// </summary>
    public class AddPlBusinessHeaderReturnDto : ReturnDtoBase
    {
    }

    #endregion 业务负责人的所属关系的CRUD

    #region 客户本体

    /// <summary>
    /// 查询的参数封装类。
    /// </summary>
    public class GetAllCustomer2ParamsDto : PagingParamsDtoBase
    {
    }

    /// <summary>
    /// 查询的返回值封装类。
    /// </summary>
    public class GetAllCustomer2ReturnDto : PagingReturnDtoBase<PlCustomer>
    {
    }

    /// <summary>
    /// 标记删除客户功能的参数封装类。
    /// </summary>
    public class RemoveCustomerParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 标记删除客户功能的返回值封装类。
    /// </summary>
    public class RemoveCustomerReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 获取所有客户功能的返回值封装类。
    /// </summary>
    public class GetAllCustomerReturnDto : PagingReturnDtoBase<PlCustomer>
    {
    }

    /// <summary>
    /// 增加新客户功能参数封装类。
    /// </summary>
    public class AddCustomerParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 新客户信息。其中Id可以是任何值，返回时会指定新值。
        /// </summary>
        public PlCustomer Customer { get; set; }
    }

    /// <summary>
    /// 增加新客户功能返回值封装类。
    /// </summary>
    public class AddCustomerReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 如果成功添加，这里返回新客户的Id。
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 修改客户信息功能参数封装类。
    /// </summary>
    public class ModifyCustomerParamsDto : ModifyParamsDtoBase<PlCustomer>
    {

    }

    /// <summary>
    /// 修改客户信息功能返回值封装类。
    /// </summary>
    public class ModifyCustomerReturnDto : ReturnDtoBase
    {
    }
    #endregion 客户本体

    #region 联系人
    /// <summary>
    /// 标记删除联系人功能的参数封装类。
    /// </summary>
    public class RemoveCustomerContactParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 标记删除联系人功能的返回值封装类。
    /// </summary>
    public class RemoveCustomerContactReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 获取所有联系人功能的返回值封装类。
    /// </summary>
    public class GetAllCustomerContactReturnDto : PagingReturnDtoBase<PlCustomerContact>
    {
    }

    /// <summary>
    /// 增加新联系人功能参数封装类。
    /// </summary>
    public class AddCustomerContactParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 新联系人信息。其中Id可以是任何值，返回时会指定新值。
        /// </summary>
        public PlCustomerContact CustomerContact { get; set; }
    }

    /// <summary>
    /// 增加新联系人功能返回值封装类。
    /// </summary>
    public class AddCustomerContactReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 如果成功添加，这里返回新联系人的Id。
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 修改联系人信息功能参数封装类。
    /// </summary>
    public class ModifyCustomerContactParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 联系人数据。
        /// </summary>
        public PlCustomerContact CustomerContact { get; set; }
    }

    /// <summary>
    /// 修改联系人信息功能返回值封装类。
    /// </summary>
    public class ModifyCustomerContactReturnDto : ReturnDtoBase
    {
    }
    #endregion 联系人

    #region 客户有效性管理

    /// <summary>
    /// 设置客户有效性状态功能参数封装类。
    /// </summary>
    public class SetCustomerValidityParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 客户Id。
        /// </summary>
        [Required]
        public Guid CustomerId { get; set; }

        /// <summary>
        /// 是否有效。true=启用，false=停用。
        /// </summary>
        [Required]
        public bool IsValid { get; set; }
    }

    /// <summary>
    /// 设置客户有效性状态功能返回值封装类。
    /// </summary>
    public class SetCustomerValidityReturnDto : ReturnDtoBase
    {
    }

    #endregion 客户有效性管理

}
