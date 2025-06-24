using Microsoft.EntityFrameworkCore;
using PowerLms.Data;
using PowerLmsWebApi.Dto;

namespace PowerLmsWebApi.Controllers
{
    #region 其它编码规则

    /// <summary>
    /// 恢复指定的被删除其它编码规则记录的功能参数封装类。
    /// </summary>
    public class RestoreOtherNumberRuleParamsDto : RestoreParamsDtoBase
    {
    }

    /// <summary>
    /// 恢复指定的被删除其它编码规则记录的功能返回值封装类。
    /// </summary>
    public class RestoreOtherNumberRuleReturnDto : RestoreReturnDtoBase
    {
    }

    /// <summary>
    /// 删除其它编码规则记录的功能参数封装类。
    /// </summary>
    public class RemoveOtherNumberRuleParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 删除其它编码规则记录的功能返回值封装类。
    /// </summary>
    public class RemoveOtherNumberRuleReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 修改其它编码规则记录的功能参数封装类。
    /// </summary>
    public class ModifyOtherNumberRuleParamsDto : ModifyParamsDtoBase<OtherNumberRule>
    {
    }

    /// <summary>
    /// 修改其它编码规则记录的功能返回值封装类。
    /// </summary>
    public class ModifyOtherNumberRuleReturnDto : ModifyReturnDtoBase
    {
    }

    /// <summary>
    /// 增加其它编码规则记录的功能参数封装类。
    /// </summary>
    public class AddOtherNumberRuleParamsDto : AddParamsDtoBase<OtherNumberRule>
    {
    }

    /// <summary>
    ///增加其它编码规则记录的功能返回值封装类。
    /// </summary>
    public class AddOtherNumberRuleReturnDto : AddReturnDtoBase
    {
    }

    /// <summary>
    /// 查询其它编码规则记录的功能返回值封装类。
    /// </summary>
    public class GetAllOtherNumberRuleReturnDto : PagingReturnDtoBase<OtherNumberRule>
    {
    }
    #endregion 其它编码规则

    #region 业务编码规则

    /// <summary>
    /// 恢复指定的被删除业务编码规则记录的功能参数封装类。
    /// </summary>
    public class RestoreJobNumberRuleParamsDto : RestoreParamsDtoBase
    {
    }

    /// <summary>
    /// 恢复指定的被删除业务编码规则记录的功能返回值封装类。
    /// </summary>
    public class RestoreJobNumberRuleReturnDto : RestoreReturnDtoBase
    {
    }

    /// <summary>
    /// 删除业务编码规则记录的功能参数封装类。
    /// </summary>
    public class RemoveJobNumberRuleParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 删除业务编码规则记录的功能返回值封装类。
    /// </summary>
    public class RemoveJobNumberRuleReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 修改业务编码规则记录的功能参数封装类。
    /// </summary>
    public class ModifyJobNumberRuleParamsDto : ModifyParamsDtoBase<JobNumberRule>
    {
    }

    /// <summary>
    /// 修改业务编码规则记录的功能返回值封装类。
    /// </summary>
    public class ModifyJobNumberRuleReturnDto : ModifyReturnDtoBase
    {
    }

    /// <summary>
    /// 增加业务编码规则记录的功能参数封装类。
    /// </summary>
    public class AddJobNumberRuleParamsDto : AddParamsDtoBase<JobNumberRule>
    {
    }

    /// <summary>
    ///增加业务编码规则记录的功能返回值封装类。
    /// </summary>
    public class AddJobNumberRuleReturnDto : AddReturnDtoBase
    {
    }

    /// <summary>
    /// 查询业务编码规则记录的功能返回值封装类。
    /// </summary>
    public class GetAllJobNumberRuleReturnDto : PagingReturnDtoBase<JobNumberRule>
    {
    }

    #endregion 业务编码规则

    #region 费用种类相关

    /// <summary>
    /// 恢复费用种类记录的功能参数封装类。
    /// </summary>
    public class RestoreFeesTypeParamsDto : RestoreParamsDtoBase
    {
    }

    /// <summary>
    /// 恢复费用种类记录的功能返回值封装类。
    /// </summary>
    public class RestoreFeesTypeReturnDto : RestoreReturnDtoBase
    {
    }

    /// <summary>
    /// 删除费用种类记录的功能参数封装类。
    /// </summary>
    public class RemoveFeesTypeParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 删除费用种类记录的功能返回值封装类。
    /// </summary>
    public class RemoveFeesTypeReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 修改费用种类记录的功能参数封装类。
    /// </summary>
    public class ModifyFeesTypeParamsDto : ModifyParamsDtoBase<FeesType>
    {
    }

    /// <summary>
    /// 修改费用种类记录的功能返回值封装类。
    /// </summary>
    public class ModifyFeesTypeReturnDto : ModifyReturnDtoBase
    {
    }

    /// <summary>
    /// 增加费用种类记录的功能参数封装类。
    /// </summary>
    public class AddFeesTypeParamsDto : AddParamsDtoBase<FeesType>
    {
        /// <summary>
        /// 是否同步到子公司/组织机构。对于超管复制到所有字典中，对于商户管理员视同为普通用户。
        /// </summary>
        public bool CopyToChildren { get; internal set; }
    }

    /// <summary>
    /// 增加费用种类记录的功能返回值封装类。
    /// </summary>
    public class AddFeesTypeReturnDto : AddReturnDtoBase
    {
    }

    /// <summary>
    /// 获取费用种类的功能返回值封装类。
    /// </summary>
    public class GetAllFeesTypeReturnDto : PagingReturnDtoBase<FeesType>
    {
    }

    #endregion 费用种类相关

    #region 基础数据字典及相关

    /// <summary>
    /// 复制简单数据字典功能的参数封装类。
    /// </summary>
    public class CopySimpleDataDicParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 源组织机构Id,省略或为null则以全局简单字典为源。
        /// </summary>
        public Guid? SrcOrgId { get; set; }

        /// <summary>
        /// 指定要复制的字典项目录代码的集合。为空则没有字典会被复制。
        /// </summary>
        public List<string> CatalogCodes { get; set; } = new List<string>();

        /// <summary>
        /// 目标组织机构Id。
        /// </summary>
        public Guid DestOrgId { get; set; }
    }

    /// <summary>
    /// 复制简单数据字典功能的返回值封装类。
    /// </summary>
    public class CopySimpleDataDicReturnDto : ReturnDtoBase
    {
    }

    /// <summary>
    /// 获取指定类别数据字典的全部内容的功能返回值封装类。
    /// </summary>
    public class GetAllDataDicReturnDto : PagingReturnDtoBase<SimpleDataDic>
    {
    }

    /// <summary>
    /// 删除简单数据字典中的一项的功能参数封装类。
    /// </summary>
    public class RemoveSimpleDataDicParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 删除简单数据字典中的一项的功能返回值封装类。
    /// </summary>
    public class RemoveSimpleDataDicReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 修改简单字典的项的功能参数封装类。
    /// </summary>
    public class ModifySimpleDataDicParamsDto : ModifyParamsDtoBase<SimpleDataDic>
    {
    }

    /// <summary>
    /// 修改简单字典的项的功能返回值封装类。
    /// </summary>
    public class ModifySimpleDataDicReturnDto : ModifyReturnDtoBase
    {
    }

    /// <summary>
    /// 给指定简单数据字典增加一项的功能参数封装类，
    /// </summary>
    public class AddSimpleDataDicParamsDto : AddParamsDtoBase<SimpleDataDic>
    {
        /// <summary>
        /// 是否同步到子公司/组织机构。对于超管复制到所有字典中，对于商户管理员复制到商户所有字典中。
        /// </summary>
        public bool CopyToChildren { get; set; }
    }

    /// <summary>
    /// 给指定简单数据字典增加一项的功能返回值封装类。
    /// </summary>
    public class AddSimpleDataDicReturnDto : AddReturnDtoBase
    {
    }

    /// <summary>
    /// 删除一个数据字典目录功能的参数封装类。
    /// </summary>
    public class RemoveDataDicCatalogParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 删除一个数据字典目录功能的返回值封装类。
    /// </summary>
    public class RemoveDataDicCatalogReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 修改数据字典目录功能的参数封装类。
    /// </summary>
    public class ModifyDataDicCatalogParamsDto : ModifyParamsDtoBase<DataDicCatalog>
    {
    }

    /// <summary>
    /// 修改数据字典目录功能的返回值封装类。
    /// </summary>
    public class ModifyDataDicCatalogReturnDto : ModifyReturnDtoBase
    {
    }

    /// <summary>
    /// 增加数据字典目录功能的参数封装类。
    /// </summary>
    public class AddDataDicCatalogParamsDto : AddParamsDtoBase<DataDicCatalog>
    {
        /// <summary>
        /// 是否同步到子公司/组织机构。对于超管复制到所有字典中，对于商户管理员复制到商户所有字典中。
        /// </summary>
        public bool CopyToChildren { get; set; }
    }

    /// <summary>
    /// 增加数据字典目录功能的返回值封装类。
    /// </summary>
    public class AddDataDicCatalogReturnDto : AddReturnDtoBase
    {
    }

    /// <summary>
    /// 返回数据字典目录功能的返回值封装类。
    /// </summary>
    public class GetAllDataDicCatalogReturnDto : PagingReturnDtoBase<DataDicCatalog>
    {
    }

    #endregion 基础数据字典及相关

    #region 换算单位及相关

    /// <summary>
    /// 恢复指定的被删除单位换算记录的功能参数封装类。
    /// </summary>
    public class RestoreUnitConversionParamsDto : RestoreParamsDtoBase
    {
    }

    /// <summary>
    /// 恢复指定的被删除单位换算记录的功能返回值封装类。
    /// </summary>
    public class RestoreUnitConversionReturnDto : RestoreReturnDtoBase
    {
    }

    /// <summary>
    /// 删除单位换算的记录的功能参数封装类。
    /// </summary>
    public class RemoveUnitConversionParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 删除单位换算的记录的功能返回值封装类。
    /// </summary>
    public class RemoveUnitConversionReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 修改单位换算记录的功能参数封装类。
    /// </summary>
    public class ModifyUnitConversionParamsDto : ModifyParamsDtoBase<UnitConversion>
    {
    }

    /// <summary>
    /// 修改单位换算记录的功能返回值封装类。
    /// </summary>
    public class ModifyUnitConversionReturnDto : ModifyReturnDtoBase
    {
    }

    /// <summary>
    /// 增加单位换算记录的功能参数封装类。
    /// </summary>
    public class AddUnitConversionParamsDto : AddParamsDtoBase<UnitConversion>
    {
    }

    /// <summary>
    /// 增加单位换算记录的功能返回值封装类。
    /// </summary>
    public class AddUnitConversionReturnDto : AddReturnDtoBase
    {
    }

    /// <summary>
    /// 获取单位换算的功能返回值封装类。
    /// </summary>
    public class GetAllUnitConversionReturnDto : PagingReturnDtoBase<UnitConversion>
    {
    }

    #endregion 换算单位及相关

    #region 汇率及相关

    /// <summary>
    /// 导入汇率的功能参数封装类。
    /// </summary>
    public class ImportPlExchangeRateParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 要引入的汇率对象的条件。符合该条件的汇率对象将被导入。
        /// </summary>
        public Dictionary<string, string> Conditional { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// 导入汇率的功能返回值封装类。
    /// </summary>
    public class ImportPlExchangeRateReturnDto : ReturnDtoBase
    {

    }

    /// <summary>
    /// 修改汇率项的功能参数封装类。
    /// </summary>
    public class ModifyPlExchangeRateParamsDto : ModifyParamsDtoBase<PlExchangeRate>
    {
    }

    /// <summary>
    /// 修改汇率项的功能返回值封装类。
    /// </summary>
    public class ModifyPlExchangeRateReturnDto : ModifyReturnDtoBase
    {
    }

    /// <summary>
    /// 增加一个汇率记录的功能参数封装类。
    /// </summary>
    public class AddPlExchangeRateParamsDto : AddParamsDtoBase<PlExchangeRate>
    {
    }

    /// <summary>
    /// 增加一个汇率记录的功能返回值封装类。
    /// </summary>
    public class AddPlExchangeRateReturnDto : AddReturnDtoBase
    {
    }

    /// <summary>
    /// 获取汇率功能的返回值封装类。
    /// </summary>
    public class GetAllPlExchangeRateReturnDto : PagingReturnDtoBase<PlExchangeRate>
    {
    }

    /// <summary>
    /// 扩展查询汇率接口返回值封装类。
    /// </summary>
    public class GetCurrentOrgExchangeRateReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 返回一组符合要求的汇率对象。
        /// </summary>
        public List<PlExchangeRate> Result { get; set; } = new List<PlExchangeRate>();
    }

    /// <summary>
    /// 扩展查询汇率接口参数封装类。
    /// </summary>
    public class GetCurrentOrgExchangeRateParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 起始的有效时间，省略则取当前时间。
        /// </summary>
        public DateTime? StartDateTime { get; set; }

        /// <summary>
        /// 终止的有效时间，省略则取当前时间。
        /// </summary>
        public DateTime? EndDateTime { get; set; }

        /// <summary>
        /// 业务类型Id.
        /// </summary>
        public Guid BusinessTypeId { get; set; }
    }

    #endregion 汇率及相关

    #region 日志相关

    /// <summary>
    /// 获取系统日志功能的返回值封装类。
    /// </summary>
    public class GetAllSystemLogReturnDto : PagingReturnDtoBase<ContainerKindCount>
    {
    }

    #endregion 日志相关

    #region 箱型相关

    /// <summary>
    /// 恢复箱型记录的功能参数封装类。
    /// </summary>
    public class RestoreShippingContainersKindParamsDto : RestoreParamsDtoBase
    {
    }

    /// <summary>
    /// 恢复箱型记录的功能返回值封装类。
    /// </summary>
    public class RestoreShippingContainersKindReturnDto : RestoreReturnDtoBase
    {
    }

    /// <summary>
    /// 删除箱型记录的功能参数封装类。
    /// </summary>
    public class RemoveShippingContainersKindParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 删除箱型记录的功能返回值封装类。
    /// </summary>
    public class RemoveShippingContainersKindReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 修改箱型记录的功能参数封装类。
    /// </summary>
    public class ModifyShippingContainersKindParamsDto : ModifyParamsDtoBase<ShippingContainersKind>
    {
    }

    /// <summary>
    /// 修改箱型记录的功能返回值封装类。
    /// </summary>
    public class ModifyShippingContainersKindReturnDto : ModifyReturnDtoBase
    {
    }

    /// <summary>
    /// 增加箱型记录的功能参数封装类。
    /// </summary>
    public class AddShippingContainersKindParamsDto : AddParamsDtoBase<ShippingContainersKind>
    {
    }

    /// <summary>
    /// 增加箱型记录的功能返回值封装类。
    /// </summary>
    public class AddShippingContainersKindReturnDto : AddReturnDtoBase
    {
    }

    /// <summary>
    /// 获取箱型的功能返回值封装类。
    /// </summary>
    public class GetAllShippingContainersKindReturnDto : PagingReturnDtoBase<ShippingContainersKind>
    {
    }

    #endregion 箱型相关

    #region 币种及相关

    /// <summary>
    /// 恢复币种条目参数封装类。
    /// </summary>
    public class RestorePlCurrencyParamsDto : RestoreParamsDtoBase
    {
    }

    /// <summary>
    /// 恢复币种条目返回值封装类。
    /// </summary>
    public class RestorePlCurrencyReturnDto : RestoreReturnDtoBase
    {
    }

    /// <summary>
    /// 删除币种条目参数封装类。
    /// </summary>
    public class RemovePlCurrencyParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 删除币种条目返回值封装类。
    /// </summary>
    public class RemovePlCurrencyReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 修改币种条目参数封装类。
    /// </summary>
    public class ModifyPlCurrencyParamsDto : ModifyParamsDtoBase<PlCurrency>
    {
    }

    /// <summary>
    /// 修改币种条目返回值封装类。
    /// </summary>
    public class ModifyPlCurrencyReturnDto : ModifyReturnDtoBase
    {
    }

    /// <summary>
    /// 增加币种条目参数封装类。
    /// </summary>
    public class AddPlCurrencyParamsDto : AddParamsDtoBase<PlCurrency>
    {
    }

    /// <summary>
    /// 增加币种条目返回值封装类。
    /// </summary>
    public class AddPlCurrencyReturnDto : AddReturnDtoBase
    {
    }

    /// <summary>
    /// 获取币种条目返回值封装类。
    /// </summary>
    public class GetAllPlCurrencyReturnDto : PagingReturnDtoBase<PlCurrency>
    {
    }
    #endregion 币种及相关

    #region 国家代码及相关

    /// <summary>
    /// 恢复国家条目参数封装类。
    /// </summary>
    public class RestorePlCountryParamsDto : RestoreParamsDtoBase
    {
    }

    /// <summary>
    /// 恢复国家条目返回值封装类。
    /// </summary>
    public class RestorePlCountryReturnDto : RestoreReturnDtoBase
    {
    }

    /// <summary>
    /// 删除国家条目参数封装类。
    /// </summary>
    public class RemovePlCountryParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 删除国家条目返回值封装类。
    /// </summary>
    public class RemovePlCountryReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 修改国家条目参数封装类。
    /// </summary>
    public class ModifyPlCountryParamsDto : ModifyParamsDtoBase<PlCountry>
    {
    }

    /// <summary>
    /// 修改国家条目返回值封装类。
    /// </summary>
    public class ModifyPlCountryReturnDto : ModifyReturnDtoBase
    {
    }

    /// <summary>
    /// 增加国家条目参数封装类。
    /// </summary>
    public class AddPlCountryParamsDto : AddParamsDtoBase<PlCountry>
    {
    }

    /// <summary>
    /// 增加国家条目返回值封装类。
    /// </summary>
    public class AddPlCountryReturnDto : AddReturnDtoBase
    {
    }

    /// <summary>
    /// 获取国家条目返回值封装类。
    /// </summary>
    public class GetAllPlCountryReturnDto : PagingReturnDtoBase<PlCountry>
    {
    }

    #endregion 国家代码及相关

}
