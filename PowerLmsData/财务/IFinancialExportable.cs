/*
 * 项目：PowerLms数据模型 | 模块：财务导出防重接口
 * 功能：定义财务数据导出防重机制的基础接口
 * 技术要点：
 * - 提供导出时间和导出用户的标准字段
 * - 支持多种导出类型（通过不同的字段组合实现）
 * - 空值表示未导出，非空表示已导出
 * 作者：zc | 创建：2025-01-31
 */
using System;
namespace PowerLms.Data.Finance
{
    /// <summary>
    /// 财务导出防重接口。
    /// 实现此接口的实体将具备导出防重功能，通过导出时间和导出用户ID标识数据是否已被导出。
    /// 
    /// <para>核心规则：</para>
    /// <list type="bullet">
    /// <item><description>导出前：过滤 ExportedDateTime == null 的数据</description></item>
    /// <item><description>导出后：设置 ExportedDateTime 和 ExportedUserId</description></item>
    /// <item><description>取消导出：重置 ExportedDateTime 和 ExportedUserId 为 null</description></item>
    /// </list>
    /// 
    /// <para>设计说明：</para>
    /// <list type="number">
    /// <item><description>对于单一导出类型的实体（如发票、OA申请单），直接实现此接口即可</description></item>
    /// <item><description>对于多导出类型的实体（如DocFee需要ARAB和APAB），需要多组字段分别实现</description></item>
    /// <item><description>字段为可空类型，null表示未导出，有值表示已导出</description></item>
    /// </list>
    /// </summary>
    public interface IFinancialExportable
    {
        /// <summary>
        /// 导出时间。
        /// 
        /// <para>规则：</para>
        /// <list type="bullet">
        /// <item><description>null: 未导出（默认值）</description></item>
        /// <item><description>非null: 已导出，记录导出的UTC时间</description></item>
        /// </list>
        /// 
        /// <para>用途：</para>
        /// <list type="bullet">
        /// <item><description>导出前过滤：WHERE ExportedDateTime IS NULL</description></item>
        /// <item><description>导出后标记：SET ExportedDateTime = GETUTCDATE()</description></item>
        /// <item><description>取消导出：SET ExportedDateTime = NULL</description></item>
        /// <item><description>审计查询：按导出时间范围查询已导出数据</description></item>
        /// </list>
        /// </summary>
        DateTime? ExportedDateTime { get; set; }
        /// <summary>
        /// 导出用户ID。
        /// 记录执行导出操作的用户，用于审计和权限验证。
        /// 
        /// <para>规则：</para>
        /// <list type="bullet">
        /// <item><description>null: 未导出（默认值）</description></item>
        /// <item><description>非null: 已导出，记录导出操作的用户ID</description></item>
        /// </list>
        /// 
        /// <para>用途：</para>
        /// <list type="bullet">
        /// <item><description>权限验证：只有导出人或管理员可以取消导出</description></item>
        /// <item><description>审计追踪：记录谁执行了导出操作</description></item>
        /// <item><description>问题排查：数据异常时可追溯到操作人</description></item>
        /// </list>
        /// </summary>
        Guid? ExportedUserId { get; set; }
    }
}
