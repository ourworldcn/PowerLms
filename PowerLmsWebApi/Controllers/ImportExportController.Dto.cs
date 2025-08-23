using PowerLmsWebApi.Dto;
using System.ComponentModel.DataAnnotations;

namespace PowerLmsWebApi.Controllers
{
    #region 获取支持的表列表

    /// <summary>
    /// 获取支持的表列表功能的参数封装类
    /// </summary>
    public class GetSupportedTablesParamsDto : TokenDtoBase
    {
        // 无需额外参数，直接返回所有支持的表列表
    }

    /// <summary>
    /// 获取支持的表列表功能的返回值封装类
    /// </summary>
    public class GetSupportedTablesReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 支持的表列表
        /// </summary>
        public List<TableInfo> Tables { get; set; } = new();
    }

    /// <summary>
    /// 表信息
    /// </summary>
    public class TableInfo
    {
        /// <summary>
        /// 表名称
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// 显示名称
        /// </summary>
        public string DisplayName { get; set; }
    }

    #endregion

    #region 导出功能

    /// <summary>
    /// 导出功能的参数封装类
    /// </summary>
    public class ExportParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 表名称
        /// </summary>
        [Required]
        public string TableName { get; set; }
    }

    #endregion

    #region 导入功能

    /// <summary>
    /// 导入功能的参数封装类
    /// </summary>
    public class ImportParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 表名称
        /// </summary>
        [Required]
        public string TableName { get; set; }

        /// <summary>
        /// 是否删除已有记录。
        /// true=删除已有记录然后重新导入，false=采用更新模式（不删除，仅更新冲突记录）
        /// </summary>
        public bool DeleteExisting { get; set; } = false;
    }

    /// <summary>
    /// 导入功能的返回值封装类
    /// </summary>
    public class ImportReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 导入成功的记录数量
        /// </summary>
        public int ImportedCount { get; set; }
    }

    #endregion
}