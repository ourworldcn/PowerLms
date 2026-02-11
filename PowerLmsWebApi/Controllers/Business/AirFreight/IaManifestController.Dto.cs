/*
 * 项目：PowerLms | 模块：空运进口舱单DTO
 * 功能：空运进口舱单（IaManifest/IaManifestDetail，Ia=Import Air）的请求和响应数据传输对象 - Manifest为行业标准术语
 * 技术要点：数据传输对象封装
 * 作者：zc | 创建：2026-02-08 | 修改：2026-02-08 重命名为标准术语
 */

using PowerLms.Data;
using PowerLmsWebApi.Dto;

namespace PowerLmsWebApi.Controllers
{
    #region 空运进口舱单主表相关（IaManifest）

    /// <summary>
    /// 获取所有空运进口舱单功能的返回值封装类。
    /// </summary>
    public class GetAllManifestReturnDto : PagingReturnDtoBase<IaManifest>
    {
    }

    /// <summary>
    /// 获取空运进口舱单详情功能的参数封装类。
    /// </summary>
    public class GetManifestParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 舱单Id。
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 获取空运进口舱单详情功能的返回值封装类。
    /// </summary>
    public class GetManifestReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 舱单主表数据（IaManifest）。
        /// </summary>
        public IaManifest Manifest { get; set; }

        /// <summary>
        /// 舱单明细列表（IaManifestDetail），包含主单行和分单行。
        /// </summary>
        public List<IaManifestDetail> Details { get; set; }
    }

    /// <summary>
    /// 增加新空运进口舱单功能参数封装类。
    /// </summary>
    public class AddManifestParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 新空运进口舱单主表信息（IaManifest）。其中Id可以是任何值，返回时会指定新值。
        /// </summary>
        public IaManifest Manifest { get; set; }

        /// <summary>
        /// 舱单明细列表（IaManifestDetail），可选。
        /// </summary>
        public List<IaManifestDetail> Details { get; set; }
    }

    /// <summary>
    /// 增加新空运进口舱单功能返回值封装类。
    /// </summary>
    public class AddManifestReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 如果成功添加，这里返回新空运进口舱单的Id。
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 修改空运进口舱单信息功能参数封装类。
    /// </summary>
    public class ModifyManifestParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 空运进口舱单数据（IaManifest）。
        /// </summary>
        public IaManifest Manifest { get; set; }
    }

    /// <summary>
    /// 修改空运进口舱单信息功能返回值封装类。
    /// </summary>
    public class ModifyManifestReturnDto : ReturnDtoBase
    {
    }

    /// <summary>
    /// 标记删除空运进口舱单功能的参数封装类。
    /// </summary>
    public class RemoveManifestParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 标记删除空运进口舱单功能的返回值封装类。
    /// </summary>
    public class RemoveManifestReturnDto : RemoveReturnDtoBase
    {
    }

    #endregion 空运进口舱单主表相关（IaManifest）

    #region 空运进口舱单明细相关（IaManifestDetail）

    /// <summary>
    /// 获取所有空运进口舱单明细功能的参数封装类。
    /// </summary>
    public class GetAllManifestDetailParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 主表Id（可选）。用于查询指定舱单的明细。
        /// </summary>
        public Guid? ParentId { get; set; }

        /// <summary>
        /// 主单号（可选）。11位纯数字。
        /// </summary>
        public string MawbNo { get; set; }
    }

    /// <summary>
    /// 获取所有空运进口舱单明细功能的返回值封装类。
    /// </summary>
    public class GetAllManifestDetailReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 明细列表（IaManifestDetail）。
        /// </summary>
        public List<IaManifestDetail> Result { get; set; }

        /// <summary>
        /// 总记录数。
        /// </summary>
        public int Total { get; set; }
    }

    /// <summary>
    /// 增加新空运进口舱单明细功能参数封装类。
    /// </summary>
    public class AddManifestDetailParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 新空运进口舱单明细信息（IaManifestDetail）。其中Id可以是任何值，返回时会指定新值。
        /// </summary>
        public IaManifestDetail Detail { get; set; }
    }

    /// <summary>
    /// 增加新空运进口舱单明细功能返回值封装类。
    /// </summary>
    public class AddManifestDetailReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 如果成功添加，这里返回新空运进口舱单明细的Id。
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 修改空运进口舱单明细信息功能参数封装类。
    /// </summary>
    public class ModifyManifestDetailParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 空运进口舱单明细数据（IaManifestDetail）。
        /// </summary>
        public IaManifestDetail Detail { get; set; }
    }

    /// <summary>
    /// 修改空运进口舱单明细信息功能返回值封装类。
    /// </summary>
    public class ModifyManifestDetailReturnDto : ReturnDtoBase
    {
    }

    /// <summary>
    /// 标记删除空运进口舱单明细功能的参数封装类。
    /// </summary>
    public class RemoveManifestDetailParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 标记删除空运进口舱单明细功能的返回值封装类。
    /// </summary>
    public class RemoveManifestDetailReturnDto : RemoveReturnDtoBase
    {
    }

    #endregion 空运进口舱单明细相关（IaManifestDetail）
}
