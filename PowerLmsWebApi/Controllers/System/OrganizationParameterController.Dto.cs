/*
 * 项目：PowerLmsWebApi | 模块：机构参数DTO
 * 功能：机构参数相关的数据传输对象
 * 技术要点：权限分离、参数验证
 * 作者：zc | 创建：2025-01 | 修改：2025-01-27 简化为基础CRUD功能
 */
using PowerLms.Data;
using PowerLmsWebApi.Dto;
using System.ComponentModel.DataAnnotations;
namespace PowerLmsWebApi.Controllers.System
{
    #region 查询相关DTO
    /// <summary>
    /// 获取机构参数列表的返回值。
    /// </summary>
    public class GetAllOrganizationParameterReturnDto : PagingReturnDtoBase<PlOrganizationParameter>
    {
    }
    #endregion 查询相关DTO
    #region 增加相关DTO
    /// <summary>
    /// 增加机构参数的参数封装类。
    /// </summary>
    public class AddOrganizationParameterParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 要添加的机构参数实体。
        /// </summary>
        [Required]
        public PlOrganizationParameter Item { get; set; }
    }
    /// <summary>
    /// 增加机构参数的返回值。
    /// </summary>
    public class AddOrganizationParameterReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 新增机构参数的机构ID。
        /// </summary>
        public Guid OrgId { get; set; }
    }
    #endregion 增加相关DTO
    #region 修改相关DTO
    /// <summary>
    /// 修改机构参数的参数封装类。
    /// </summary>
    public class ModifyOrganizationParameterParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 要修改的机构参数实体集合。
        /// </summary>
        [Required]
        public PlOrganizationParameter[] Items { get; set; }
    }
    /// <summary>
    /// 修改机构参数的返回值。
    /// </summary>
    public class ModifyOrganizationParameterReturnDto : ReturnDtoBase
    {
    }
    #endregion 修改相关DTO
    #region 删除相关DTO
    /// <summary>
    /// 删除机构参数的参数封装类。
    /// </summary>
    public class RemoveOrganizationParameterParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 要删除的机构ID。
        /// </summary>
        [Required]
        public Guid OrgId { get; set; }
    }
    /// <summary>
    /// 删除机构参数的返回值。
    /// </summary>
    public class RemoveOrganizationParameterReturnDto : ReturnDtoBase
    {
    }
    #endregion 删除相关DTO
}