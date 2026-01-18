/*
 * 项目：PowerLms物流管理系统 | 模块：通用导入导出DTO
 * 功能：批量多Sheet导入导出的数据传输对象定义
 * 包含：获取支持表列表、批量导出、批量导入等功能的DTO
 * 技术要点：
 * - 支持多表类型批量处理
 * - 统一的参数验证和错误处理
 * - 继承PowerLms框架的基础DTO类型
 * 作者：zc | 创建：2025-01-27
 */
using PowerLmsWebApi.Dto;
using System.ComponentModel.DataAnnotations;
namespace PowerLmsWebApi.Controllers
{
    #region 简单字典专用DTO
    #region 获取简单字典Catalog Code列表
    /// <summary>
    /// 获取简单字典Catalog Code列表参数DTO
    /// 用于查询ow_DataDicCatalogs表中的可用分类代码
    /// </summary>
    public class GetSimpleDictionaryCatalogCodesParamsDto : TokenDtoBase
    {
    }
    /// <summary>
    /// 获取简单字典Catalog Code列表返回DTO
    /// </summary>
    public class GetSimpleDictionaryCatalogCodesReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 简单字典分类代码列表
        /// 来源于ow_DataDicCatalogs表的Code和DisplayName字段
        /// </summary>
        public List<CatalogCodeInfo> CatalogCodes { get; set; } = new List<CatalogCodeInfo>();
    }
    /// <summary>
    /// 分类代码信息
    /// </summary>
    public class CatalogCodeInfo
    {
        /// <summary>
        /// 分类代码（ow_DataDicCatalogs.Code字段值）
        /// 示例：COUNTRY、PORT、CURRENCY等
        /// </summary>
        public string Code { get; set; }
        /// <summary>
        /// 显示名称（ow_DataDicCatalogs.DisplayName字段值）
        /// 示例：国家代码、港口代码、货币代码等
        /// </summary>
        public string DisplayName { get; set; }
    }
    #endregion
    #region 导出简单字典
    /// <summary>
    /// 导出简单字典参数DTO
    /// 支持批量导出多个Catalog Code到多Sheet Excel文件
    /// </summary>
    public class ExportSimpleDictionaryParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 要导出的Catalog Code列表
        /// 对应ow_DataDicCatalogs.Code字段值
        /// Excel中每个Catalog Code对应一个Sheet，Sheet名称为Catalog Code值
        /// </summary>
        public List<string> CatalogCodes { get; set; } = new List<string>();
    }
    #endregion
    #region 导入简单字典
    /// <summary>
    /// 导入简单字典参数DTO
    /// 自动识别Excel中的所有Sheet，根据Sheet名称匹配Catalog Code
    /// </summary>
    public class ImportSimpleDictionaryParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 是否删除现有数据（true=替换模式，false=更新模式）
        /// - true：删除指定Catalog Code下的所有现有ow_SimpleDataDics记录，然后导入新数据
        /// - false：保留现有记录，基于Code字段匹配进行更新或新增
        /// </summary>
        public bool DeleteExisting { get; set; } = false;
    }
    /// <summary>
    /// 导入简单字典返回DTO
    /// </summary>
    public class ImportSimpleDictionaryReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 总导入记录数
        /// 跨所有成功处理的Sheet的记录总数
        /// </summary>
        public int ImportedCount { get; set; }
        /// <summary>
        /// 处理的Sheet数量
        /// 成功识别并处理的Sheet数量（不包括因Catalog Code不存在而跳过的Sheet）
        /// </summary>
        public int ProcessedSheets { get; set; }
        /// <summary>
        /// 各Sheet的处理详情
        /// 包含每个Sheet的处理结果、成功/失败状态、错误信息等
        /// </summary>
        public List<string> Details { get; set; } = new List<string>();
    }
    #endregion
    #endregion
    #region 通用导入导出DTO
    #region 获取支持的表列表
    /// <summary>
    /// 获取支持的表列表参数DTO
    /// 用于查询所有支持批量导入导出的表类型
    /// </summary>
    public class GetSupportedTablesParamsDto : TokenDtoBase
    {
    }
    /// <summary>
    /// 获取支持的表列表返回DTO
    /// </summary>
    public class GetSupportedTablesReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 支持的表列表
        /// 包含独立字典表、客户资料主表、客户资料子表
        /// 不包含ow_SimpleDataDics（简单字典，有专门的分部控制器API）
        /// </summary>
        public List<TableInfo> Tables { get; set; } = new List<TableInfo>();
    }
    /// <summary>
    /// 表信息
    /// </summary>
    public class TableInfo
    {
        /// <summary>
        /// 表名称（实体类型名称）
        /// 示例：PlCountry、PlCustomer、PlCustomerContact等
        /// 对应Excel中的Sheet名称
        /// </summary>
        public string TableName { get; set; }
        /// <summary>
        /// 显示名称（中文名称）
        /// 来源于数据库表的Comment注释
        /// 示例：国家字典、客户资料、客户联系人等
        /// </summary>
        public string DisplayName { get; set; }
    }
    #endregion
    #region 批量导出功能
    /// <summary>
    /// 批量导出参数DTO（多表模式）
    /// 支持同时导出多个表类型到一个Excel文件的多个Sheet
    /// </summary>
    public class ExportMultipleTablesParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 表名称列表（实体类型名称）
        /// 支持的表类型：
        /// - 独立字典表：PlCountry、PlPort、PlCargoRoute、PlCurrency、FeesType、PlExchangeRate、UnitConversion、ShippingContainersKind
        /// - 客户资料主表：PlCustomer
        /// - 客户资料子表：PlCustomerContact、PlBusinessHeader、PlTidan、CustomerBlacklist、PlLoadingAddr
        /// 
        /// Excel文件结构：
        /// - 每个表对应一个Sheet，Sheet名称为实体类型名称
        /// - 列标题为实体属性名称（排除Id、OrgId系统字段）
        /// </summary>
        public List<string> TableNames { get; set; } = new List<string>();
    }
    #endregion
    #region 批量导入功能
    /// <summary>
    /// 批量导入参数DTO（多表模式）
    /// 自动识别Excel中的所有Sheet，根据Sheet名称匹配表类型进行导入
    /// </summary>
    public class ImportMultipleTablesParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 是否删除现有数据（true=替换模式，false=更新模式）
        /// - true：删除指定表的所有现有记录，然后导入新数据
        /// - false：保留现有记录，基于Code字段匹配进行更新或新增
        /// 
        /// 字段处理规则：
        /// - Id字段：自动生成新的GUID
        /// - OrgId字段：自动设置为当前登录用户的机构ID
        /// - 其他字段：根据Excel列标题与实体属性名称匹配
        /// </summary>
        public bool DeleteExisting { get; set; } = false;
    }
    /// <summary>
    /// 批量导入返回DTO（多表模式）
    /// </summary>
    public class ImportMultipleTablesReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 总导入记录数
        /// 跨所有成功处理的Sheet的记录总数
        /// </summary>
        public int ImportedCount { get; set; }
        /// <summary>
        /// 处理的Sheet数量
        /// 成功识别并处理的Sheet数量（不包括因表类型不支持而跳过的Sheet）
        /// </summary>
        public int ProcessedSheets { get; set; }
    }
    #endregion
    #endregion
}