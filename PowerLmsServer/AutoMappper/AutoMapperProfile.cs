using AutoMapper;
using OW.Data;
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

            CreateMap<PlOrganization, PlOrganization>().IncludeAllDerived()
                .ForAllMembers(opt =>
                {
                    // 为所有成员设置一个前置条件
                    opt.PreCondition((src, context) =>
                    {
                        // 检查是否通过上下文的Items字典传入了要忽略的属性列表
                        if (context.Items.TryGetValue("IgnoreProps", out var value) &&
                            value is ISet<string> propsToIgnore)
                        {
                            // 如果当前目标属性的名称在忽略列表中，则PreCondition返回false
                            // 这会阻止AutoMapper继续处理这个属性（包括读取源属性的值）
                            return !propsToIgnore.Contains(opt.DestinationMember.Name);
                        }

                        // 如果没有提供忽略列表，或者当前属性不在列表中，则正常处理
                        return true;
                    });
                });
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
                // 购方信息映射
                .ForMember(dest => dest.BuyerName, opt => opt.MapFrom(src => src.BuyerTitle))
                .ForMember(dest => dest.BuyerPhone, opt => opt.MapFrom(src => src.Mobile))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Mail))

                // 销方信息映射
                .ForMember(dest => dest.SalerTaxNum, opt => opt.MapFrom(src => src.SellerTaxNum))
                .ForMember(dest => dest.SalerTel, opt => opt.MapFrom(src => src.SellerTel))
                .ForMember(dest => dest.SalerAddress, opt => opt.MapFrom(src => src.SellerAddress))
                .ForMember(dest => dest.SalerAccount, opt => opt.MapFrom(src => src.SellerAccount))

                // 需要转换的字段
                .ForMember(dest => dest.OrderNo, opt => opt.MapFrom(src =>
                    !string.IsNullOrEmpty(src.InvoiceSerialNum) ? src.InvoiceSerialNum : Guid.NewGuid().ToString("N").Substring(0, 20)))
                .ForMember(dest => dest.InvoiceDate, opt => opt.MapFrom(src =>
                    (src.ApplyDateTime ?? DateTime.Now).ToString("yyyy-MM-dd HH:mm:ss")))
                .ForMember(dest => dest.InvoiceType, opt => opt.MapFrom(src => src.IsRedLetter ? 2 : 1))
                .ForMember(dest => dest.InvoiceLine, opt => opt.MapFrom(src => src.InvoiceType.ToLower()))

                // 默认值
                .ForMember(dest => dest.Clerk, opt => opt.MapFrom(src => "系统开票"))
                .ForMember(dest => dest.PushMode, opt => opt.Ignore())
                .ForMember(dest => dest.InvoiceDetail, opt => opt.Ignore())
                .IncludeAllDerived();

            // TaxInvoiceInfoItem 到 NNInvoiceDetail 的映射
            CreateMap<TaxInvoiceInfoItem, NNInvoiceDetail>()
                // 商品信息映射
                .ForMember(dest => dest.GoodsName, opt => opt.MapFrom(src => src.GoodsName))
                .ForMember(dest => dest.Unit, opt => opt.MapFrom(src => !string.IsNullOrEmpty(src.Unit) ? src.Unit : string.Empty))
                .ForMember(dest => dest.SpecType, opt => opt.MapFrom(src => !string.IsNullOrEmpty(src.SpecType) ? src.SpecType : string.Empty))

                // 含税标志
                .ForMember(dest => dest.WithTaxFlag, opt => opt.MapFrom(src => 0)) // 不含税

                // 数量和单价映射
                .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.UnitPrice.ToString("0.00000000")))
                .ForMember(dest => dest.Num, opt => opt.MapFrom(src => src.Quantity.ToString("0.00000000")))

                // 税率映射
                .ForMember(dest => dest.TaxRate, opt => opt.MapFrom(src => src.TaxRate))

                // 金额映射 - 确保格式正确且计算准确
                .ForMember(dest => dest.TaxExcludedAmount, opt => opt.MapFrom(src =>
                    (src.UnitPrice * src.Quantity).ToString("0.00"))) // 不含税金额
                .ForMember(dest => dest.Tax, opt => opt.MapFrom(src =>
                    (src.UnitPrice * src.Quantity * src.TaxRate).ToString("0.00"))) // 税额
                .ForMember(dest => dest.TaxIncludedAmount, opt => opt.MapFrom(src =>
                    src.TaxInclusiveAmount.ToString("0.00"))) // 含税金额

                // 发票行属性和政策信息
                .ForMember(dest => dest.InvoiceLineProperty, opt => opt.MapFrom(src => "0")) // 正常行
                .ForMember(dest => dest.FavouredPolicyFlag, opt => opt.MapFrom(src => "0")) // 不使用优惠政策
                .ForMember(dest => dest.FavouredPolicyName, opt => opt.MapFrom(src => string.Empty))
                .ForMember(dest => dest.Deduction, opt => opt.MapFrom(src => "0")) // 非差额征税
                .ForMember(dest => dest.ZeroRateFlag, opt => opt.MapFrom(src => "0")) // 非零税率
                .IncludeAllDerived();
            #endregion
        }
    }
}
