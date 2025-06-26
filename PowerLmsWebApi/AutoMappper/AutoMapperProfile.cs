using AutoMapper;
using PowerLms.Data;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Controllers;
using PowerLmsWebApi.Dto;

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
            // 基本返回类型映射
            CreateMap<ReturnBase, ReturnDtoBase>().IncludeAllDerived();
            
            #region 权限相关
            #endregion 权限相关
        }
    }
}
