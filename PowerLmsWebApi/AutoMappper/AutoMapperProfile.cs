﻿using AutoMapper;
using PowerLms.Data;

namespace PowerLmsWebApi.AutoMappper
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
            CreateMap<PlOwnedAddress, PlOwnedAddress>().IncludeAllDerived();
            CreateMap<PlSimpleOwnedAddress, PlSimpleOwnedAddress>().IncludeAllDerived();
            CreateMap<PlOwnedName, PlOwnedName>().IncludeAllDerived();

            CreateMap<OwnedAirlines, OwnedAirlines>().IncludeAllDerived();
            CreateMap<PlOwnedContact, PlOwnedContact>().IncludeAllDerived();
            CreateMap<PlBillingInfo, PlBillingInfo>().IncludeAllDerived();

            CreateMap<PlOrganization, PlOrganization>().IncludeAllDerived();
            CreateMap<PlMerchant, PlMerchant>().IncludeAllDerived();
            CreateMap<PlCustomer, PlCustomer>().IncludeAllDerived();
            CreateMap<PlFileInfo, PlFileInfo>().IncludeAllDerived();
        }
    }
}
