/*
 * 项目：PowerLms | 模块：通用数据查询控制器DTO
 * 功能：通用数据查询控制器的数据传输对象定义
 * 技术要点：参数验证、返回结果封装、动态字段支持
 * 作者：zc | 创建：2025-01 | 修改：2025-01-27 重构为通用数据查询DTO，符合项目命名规范
 */
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
namespace PowerLmsWebApi.Dto
{
    /// <summary>
    /// 获取通用数据的返回结果。
    /// </summary>
    public class GetCommonDataReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public GetCommonDataReturnDto()
        {
            Records = new List<Dictionary<string, object>>();
            FieldNames = new List<string>();
        }
        /// <summary>
        /// 查询的表名。
        /// </summary>
        public string TableName { get; set; }
        /// <summary>
        /// 查询的字段名列表。
        /// </summary>
        public List<string> FieldNames { get; set; }
        /// <summary>
        /// 是否使用了去重查询。
        /// </summary>
        public bool IsDistinct { get; set; }
        /// <summary>
        /// 返回的记录总数。
        /// </summary>
        public int TotalCount { get; set; }
        /// <summary>
        /// 记录集合。每条记录是一个字典，包含指定字段的键值对。
        /// Key: 字段名，Value: 字段值（支持各种数据类型）
        /// 自动过滤所有字段都为空的记录，根据IsDistinct参数决定是否去重。
        /// </summary>
        public List<Dictionary<string, object>> Records { get; set; }
    }
}