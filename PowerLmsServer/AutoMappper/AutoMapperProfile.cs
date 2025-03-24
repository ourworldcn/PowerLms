using AutoMapper;
using PowerLms.Data;
using PowerLmsServer.Managers;

namespace PowerLmsServer.AutoMappper
{
    /// <summary>
    /// 配置本项目AutoMapper的特殊类。
    /// </summary>
    public class AutoMapperProfile : Profile
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public AutoMapperProfile()
        {
            #region 基础对象映射
            CreateMap<Account, Account>().IncludeAllDerived();
            CreateMap<PlOwnedAddress, PlOwnedAddress>().IncludeAllDerived();
            CreateMap<PlSimpleOwnedAddress, PlSimpleOwnedAddress>().IncludeAllDerived();
            CreateMap<PlOwnedName, PlOwnedName>().IncludeAllDerived();

            CreateMap<OwnedAirlines, OwnedAirlines>().IncludeAllDerived();
            CreateMap<PlOwnedContact, PlOwnedContact>().IncludeAllDerived();
            CreateMap<PlBillingInfo, PlBillingInfo>().IncludeAllDerived();

            CreateMap<PlOrganization, PlOrganization>().IncludeAllDerived();
            CreateMap<PlMerchant, PlMerchant>().IncludeAllDerived();
            CreateMap<PlFileInfo, PlFileInfo>().IncludeAllDerived();

            CreateMap<PlCustomer, PlCustomer>().IncludeAllDerived();
            CreateMap<PlTaxInfo, PlTaxInfo>().IncludeAllDerived();
            #endregion

            #region 工作与费用映射
            CreateMap<PlJob, PlJob>().IncludeAllDerived()
                .ForAllMembers(opt =>
                {
                    opt.PreCondition((ResolutionContext c) =>
                    {
                        return !c.TryGetItems(out var items) || !items.GetBooleanOrDefaut($"-{opt.DestinationMember.Name}");
                    });
                });

            CreateMap<DocFee, DocFee>().IncludeAllDerived()
                .ForAllMembers(opt =>
                 {
                     opt.PreCondition((ResolutionContext c) =>
                     {
                         return !c.TryGetItems(out var items) || !items.GetBooleanOrDefaut($"-{opt.DestinationMember.Name}");
                     });
                 });
            #endregion

            #region 文档映射
            CreateMap<PlEaDoc, PlEaDoc>().IncludeAllDerived()
                .ForAllMembers(opt =>
                {
                    opt.PreCondition((ResolutionContext c) =>
                    {
                        return !c.TryGetItems(out var items) || !items.GetBooleanOrDefaut($"-{opt.DestinationMember.Name}");
                    });
                });

            CreateMap<PlIaDoc, PlIaDoc>().IncludeAllDerived()
                .ForAllMembers(opt =>
                {
                    opt.PreCondition((ResolutionContext c) =>
                    {
                        return !c.TryGetItems(out var items) || !items.GetBooleanOrDefaut($"-{opt.DestinationMember.Name}");
                    });
                });

            CreateMap<PlEsDoc, PlEsDoc>().IncludeAllDerived()
                .ForAllMembers(opt =>
                {
                    opt.PreCondition((ResolutionContext c) =>
                    {
                        return !c.TryGetItems(out var items) || !items.GetBooleanOrDefaut($"-{opt.DestinationMember.Name}");
                    });
                });

            CreateMap<PlIsDoc, PlIsDoc>().IncludeAllDerived()
                .ForAllMembers(opt =>
                {
                    opt.PreCondition((ResolutionContext c) =>
                    {
                        return !c.TryGetItems(out var items) || !items.GetBooleanOrDefaut($"-{opt.DestinationMember.Name}");
                    });
                });
            #endregion

            #region 权限相关
            CreateMap<PlPermission, PlPermission>().IncludeAllDerived();
            CreateMap<PlRole, PlRole>().IncludeAllDerived();
            #endregion

            #region 诺诺发票映射
            // TaxInvoiceInfo 到 NNOrder 的映射
            CreateMap<TaxInvoiceInfo, NNOrder>()
                // 不同名属性映射
                .ForMember(dest => dest.BuyerName, opt => opt.MapFrom(src => src.BuyerTitle))
                .ForMember(dest => dest.BuyerPhone, opt => opt.MapFrom(src => src.Mobile))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Mail))

                // 需要转换的字段
                .ForMember(dest => dest.OrderNo, opt => opt.MapFrom(src =>
                    !string.IsNullOrEmpty(src.InvoiceSerialNum) ? src.InvoiceSerialNum : Guid.NewGuid().ToString("N").Substring(0, 20)))
                .ForMember(dest => dest.InvoiceDate, opt => opt.MapFrom(src => src.ApplyDateTime ?? DateTime.Now))
                .ForMember(dest => dest.InvoiceType, opt => opt.MapFrom(src => 1)) //TO DO 这里暂时仅考虑蓝票
                .ForMember(dest => dest.InvoiceLine, opt => opt.MapFrom(src => src.InvoiceType != null))

                // 默认值
                .ForMember(dest => dest.Clerk, opt => opt.MapFrom(src => "系统开票"))
                .ForMember(dest => dest.PushMode, opt => opt.Ignore())
                .ForMember(dest => dest.InvoiceDetail, opt => opt.Ignore())
                .IncludeAllDerived();

            // TaxInvoiceInfoItem 到 NNInvoiceDetail 的映射
            CreateMap<TaxInvoiceInfoItem, NNInvoiceDetail>()
                .ForMember(dest => dest.WithTaxFlag, opt => opt.MapFrom(src => 1)) // 含税
                .ForMember(dest => dest.Unit, opt => opt.MapFrom(src => string.Empty))
                .ForMember(dest => dest.SpecType, opt => opt.MapFrom(src => string.Empty))

                // 金额格式化
                .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.UnitPrice.ToString("0.00")))
                .ForMember(dest => dest.Num, opt => opt.MapFrom(src => src.Quantity.ToString("0.00000000")))

                // 计算金额 - 使用内联表达式替代方法调用
                .ForMember(dest => dest.TaxExcludedAmount, opt => opt.MapFrom(src =>
                    (src.UnitPrice * src.Quantity).ToString("0.00")))
                .ForMember(dest => dest.Tax, opt => opt.MapFrom(src =>
                    (src.UnitPrice * src.Quantity * src.TaxRate).ToString("0.00")))
                .ForMember(dest => dest.TaxIncludedAmount, opt => opt.MapFrom(src =>
                    (src.UnitPrice * src.Quantity * (1 + src.TaxRate)).ToString("0.00")))

                // 默认值
                .ForMember(dest => dest.InvoiceLineProperty, opt => opt.MapFrom(src => "0"))
                .ForMember(dest => dest.FavouredPolicyFlag, opt => opt.MapFrom(src => "0"))
                .ForMember(dest => dest.FavouredPolicyName, opt => opt.MapFrom(src => string.Empty))
                .ForMember(dest => dest.Deduction, opt => opt.MapFrom(src => "0"))
                .ForMember(dest => dest.ZeroRateFlag, opt => opt.MapFrom(src => "0"))
                .IncludeAllDerived();
            #endregion
        }
    }
}
