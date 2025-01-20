﻿using AutoMapper;
using PowerLms.Data;

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

            CreateMap<PlJob, PlJob>().IncludeAllDerived();
            CreateMap<DocFee, DocFee>().IncludeAllDerived();

            CreateMap<PlEaDoc, PlEaDoc>().IncludeAllDerived();
            CreateMap<PlIaDoc, PlIaDoc>().IncludeAllDerived();
            CreateMap<PlEsDoc, PlEsDoc>().IncludeAllDerived();
            CreateMap<PlIsDoc, PlIsDoc>().IncludeAllDerived();

            #region 权限相关
            CreateMap<PlPermission, PlPermission>().IncludeAllDerived();
            CreateMap<PlRole, PlRole>().IncludeAllDerived();

            #endregion 权限相关
        }
    }
}
